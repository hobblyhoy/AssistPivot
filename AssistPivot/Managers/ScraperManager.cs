using AssistPivot.DAL;
using AssistPivot.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssistPivot.Managers
{
    public class ScraperManager
    {
        public static HttpClient client = new HttpClient();
        public const string welcomePageUrl = "http://www.assist.org/web-assist/welcome.html";
        public const string courseGroupSeperator = "--------------------------------------------------------------------------------";
        public string[] emptySignifiers = { "Not Articulated", "No Comparable Course" };
        public AssistManager assistMan = null;
        private AssistManager AssistMan()
        {
            if (assistMan == null) assistMan = new AssistManager();
            return assistMan;
        }

        public async Task<List<College>> GetCollegesFromDbOrScrape(AssistDbContext db)
        {
            // Pull in the existing colleges from the DB. If they're recently updated dont bother with the scrape.
            var dbColleges = db.Colleges.ToList();
            var oneWeekAgo = DateTimeOffset.Now.AddDays(-7);
            if (dbColleges.Min(c => c.UpToDateAsOf) > oneWeekAgo)
            {
                return dbColleges;
            }

            using (var response = await client.GetAsync(welcomePageUrl))
            {
                using (var content = response.Content)
                {
                    // Grab the html from the page
                    var result = await content.ReadAsStringAsync();
                    var document = new HtmlDocument();
                    document.LoadHtml(result);
                    
                    // Start weeding it down to just the content we want
                    var selectElement = document.DocumentNode.SelectSingleNode("//select");
                    var filteredOptions = selectElement.ChildNodes.Where(node => node.Name == "option").ToList();
                    filteredOptions.RemoveAt(0); //first element is a "click here to select blah blah"
                    var scrapedColleges = new List<College>();
                    var now = DateTimeOffset.Now;

                    // FYI The college name is on the node but the college shorthand name is in an array within the node's attributes.
                    foreach (var node in filteredOptions)
                    {
                        var shorthand = node.Attributes.FirstOrDefault(n => n.Name == "value").Value.Replace(".html","");
                        var collegeName = node.InnerText;
                        var scrapedCollege = new College { Name = collegeName, Shorthand = shorthand, UpToDateAsOf = now };

                        //get the "equal" DB object
                        var matchingDbCollege = dbColleges.FirstOrDefault(dbCol => dbCol.NonStrictEquals(scrapedCollege));
                        if (matchingDbCollege != null)
                        {
                            // Make our "equal enough" db context object actually equal to what we just scrapped from the site.
                            // This will usually just be updating the UpToDateAsOf property but will also handle updates to 
                            // either college name or shorthand id.
                            matchingDbCollege.MakeEqual(scrapedCollege);
                        }
                        else
                        {
                            // No db object exists, create one
                            db.Colleges.Add(scrapedCollege);
                            // Keep our own copy up-to-date so the return list doesn't have to make another db trip
                            dbColleges.Add(scrapedCollege);
                        }
                        // Keen eyed observers will notice this does not handle the case where the DB object exists but the scraped object
                        // does not. This is intentional. I'm not going to risk ruining a bunch of downstream data depending on these
                        // CollegeId's on something I'm pulling in from an external resource.
                    }

                    // We want the full objects on the front-end including the DB ids. Thankfully the objects we pushed
                    // to our list stay in the DB scope and are automatically updated after saving. However it does mean we 
                    // cant fire and forget.
                    db.SaveChanges();

                    return dbColleges;
                }
            }
        }

        private string RequestUrl(string fromCollegeShorthand, string toCollegeShorthand, string yearName)
        {
            return $"http://web2.assist.org/cgi-bin/REPORT_2/Rep2.pl?aay={yearName}&ay={yearName}&swap=1&ria={toCollegeShorthand}"
                + $"&ia={fromCollegeShorthand}&dir=1&oia={toCollegeShorthand}&event=18&agreement=aa"
                + $"&sia={fromCollegeShorthand}&&sidebar=false&rinst=left&mver=2&kind=5&dt=2";
        }

        public async Task UpdateCourseRelationships(AssistDbContext db, College fromCollege, Year year)
        {
            var toColleges = db.Colleges.Where(c => c != fromCollege).ToList();
            var allCoursesFromDb = db.Courses.Include("College").Include("Year").ToList();
            var allCourseRelationsipsFromDb = db.CourseRelationships.Include("ToCourses").Include("FromCourses").ToList();

            // Clear out any existing data we're about to grab
            // Note that a particular course can apply to relationships spanning multiple fromColleges so once created we NEVER delete those
            var courseRelasToDelete = AssistMan().GetCourseRelationships(db, fromCollege, year);
            foreach (var courseRela in courseRelasToDelete)
            {
                db.CourseRelationships.Remove(courseRela);
            }
            db.SaveChanges();

            foreach (var toCollege in toColleges)
            {
                //var debugTestCollege = toColleges.First(c => c.Shorthand == "CPP");
                var url = RequestUrl(fromCollege.Shorthand, toCollege.Shorthand, year.Name);
                string result;
                using (var response = await client.GetAsync(url))
                {
                    using (var content = response.Content)
                    {
                        result = await content.ReadAsStringAsync();
                        //result = DebugManager.RequestAhcToCpp1516();
                    }
                }
                // Technically this is an html doc but the bit we care about is always going to be between the only set of PRE tags
                // so we'll skip the html doc overhead and do it old school
                var dirtyList = result.Between("<PRE>", "</PRE>").Trim().Split(courseGroupSeperator);
                var validCourseRelationships = new List<string>();
                var matchesTwoCourses = @"[(].*?[0-9].*?[)].*?[|].*?[(].*?[0-9].*?[)]";
                var twoCoursesRegex = new Regex(matchesTwoCourses, RegexOptions.Singleline);
                // Clean phase 1, remove single courses or non course data, add these to cleanList
                foreach (var potentialCourseRela in dirtyList)
                {
                    // Remove the entities that dont contain the pattern *(*)*|*(*)* 
                    // also exclude our known empty comparison cases
                    if (twoCoursesRegex.IsMatch(potentialCourseRela) && !emptySignifiers.Any(s => potentialCourseRela.Contains(s)))
                    {
                        validCourseRelationships.Add(potentialCourseRela);
                    }

                }

                // Clean phase 2 + parse, all within each block of course relationships
                // remove any whole lines which dont contain the verticle seperator "|" (notes, department headers, etc. Never course data)
                // parse the ones which do into to/from courses
                foreach (var courseRelaRaw in validCourseRelationships)
                {
                    using (StringReader reader = new StringReader(courseRelaRaw))
                    {
                        var toProcessLineObj = new ProcessLineObj();
                        var fromProcessLineObj = new ProcessLineObj();

                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // if the line doesn't contain a |, skip (notes, department headers, etc. Never course data)
                            if (line.IndexOf('|') == -1) continue;
                            // break line into to/from parts (before/after the |  ...order matters!)
                            line = line.Substring(16); //remove assist formatting while retaining indentation
                            var lineParts = line.Split("|", 2);

                            ProcessLine(toProcessLineObj, lineParts[0]);
                            ProcessLine(fromProcessLineObj, lineParts[1]);
                        }
                        if (toProcessLineObj.Course != null) toProcessLineObj.Courses.Add(toProcessLineObj.Course);
                        if (fromProcessLineObj.Course != null) fromProcessLineObj.Courses.Add(fromProcessLineObj.Course);

                        if (toProcessLineObj.Courses.Count > 0 && fromProcessLineObj.Courses.Count > 0)
                        {
                            // Now that we've confirmed we have a good one lets fill in the missing parts to our Course / CourseRela objects
                            toProcessLineObj.RelationshipType = (toProcessLineObj.RelationshipType == CourseRelationshipType.Unset) ? CourseRelationshipType.None : toProcessLineObj.RelationshipType;
                            fromProcessLineObj.RelationshipType = (fromProcessLineObj.RelationshipType == CourseRelationshipType.Unset) ? CourseRelationshipType.None : fromProcessLineObj.RelationshipType;

                            var now = DateTimeOffset.Now;
                            var toCourseUpdateTemplate = new Course() { College = toCollege, Year = year, UpToDateAsOf = now };
                            var fromCourseUpdateTemplate = new Course() { College = fromCollege, Year = year, UpToDateAsOf = now };
                            UpdateCourseListAndReRefToDbObjectsIfTheyExist(db, allCoursesFromDb, toProcessLineObj.Courses, toCourseUpdateTemplate);
                            UpdateCourseListAndReRefToDbObjectsIfTheyExist(db, allCoursesFromDb, fromProcessLineObj.Courses, fromCourseUpdateTemplate);

                            //The big daddy and gold of the app, course relationships
                            var resultantCourseRela = new CourseRelationship()
                            {
                                ToCourses = toProcessLineObj.Courses,
                                FromCourses = fromProcessLineObj.Courses,
                                ToRelationshipType = toProcessLineObj.RelationshipType,
                                FromRelationshipType = fromProcessLineObj.RelationshipType,
                                UpToDateAsOf = now,
                            };

                            // Try and find a db equivalent
                            // Since our Course objects have a custom Equals we can skirt the need for saving to db first
                            var dbObj = allCourseRelationsipsFromDb.FirstOrDefault(dbCourseRela => dbCourseRela.Equals(resultantCourseRela));
                            if (dbObj == null)
                            {
                                db.CourseRelationships.Add(resultantCourseRela);
                                allCourseRelationsipsFromDb.Add(resultantCourseRela);
                            }
                            else
                            {
                                dbObj.UpToDateAsOf = now;
                            }

                        }
                    }
                }

                //Save after each toCollege has been processed.
                db.SaveChanges();
            }
        }

        private void UpdateCourseListAndReRefToDbObjectsIfTheyExist(AssistDbContext db, List<Course> allCoursesFromDb, List<Course> coursesToAdd, Course updateTemplate)
        {
            for (int i=0; i < coursesToAdd.Count; i++)
            {
                coursesToAdd[i].PatchFromTemplate(updateTemplate);
                var dbObj = allCoursesFromDb.FirstOrDefault(dbCourse => dbCourse.Equals(coursesToAdd[i]));
                if (dbObj == null)
                {
                    db.Courses.Add(coursesToAdd[i]);
                    allCoursesFromDb.Add(coursesToAdd[i]);
                }
                else
                {
                    coursesToAdd[i] = dbObj;
                    coursesToAdd[i].PatchFromTemplate(updateTemplate);
                }
            }
        }

        //This needs to be able to operate on the details of a particular course through multiple calls so we pass it
        // a custom class to hold onto that can be easily be worked on without worrying about passing stuff back and forth.
        private void ProcessLine(ProcessLineObj processLineObj, string courseLine)
        {
            if (courseLine.Substring(0, 1) != " ")
            {
                //we've reached a new (or the first) course. push the existing one onto our list and start a new
                if (!processLineObj.Course.IsEmpty())
                {
                    processLineObj.Course.UpToDateAsOf = DateTimeOffset.Now;
                    processLineObj.Courses.Add(processLineObj.Course);
                }
                processLineObj.Course = new Course();
                // Get the course name
                var matchesFirstTwoWords = @"[^\s]+\s+[^\s]+";
                var courseNameRegex = new Regex(matchesFirstTwoWords);
                var match = courseNameRegex.Match(courseLine);
                processLineObj.Course.Name = match.Value;
                // Check for & and ORs relationships. Could regex this but it's only two strings so w/e
                var andSignifier = "<B ><U >&</B></U>";
                var orSignifier = "<B ><U >OR</B></U>";
                if (courseLine.Contains(andSignifier))
                {
                    processLineObj.RelationshipType = CourseRelationshipType.And;
                    courseLine = courseLine.Replace(andSignifier, "");
                }
                else if (courseLine.Contains(orSignifier))
                {
                    processLineObj.RelationshipType = CourseRelationshipType.Or;
                    courseLine = courseLine.Replace(orSignifier, "");
                }
                //Get the credits (2nd to last character)
                int parseResult;
                var credits = courseLine.Substring(courseLine.Length - 2, 1);
                if (int.TryParse(credits, out parseResult)) processLineObj.Course.Credits = parseResult;
                //Get the description (or at least the start of it)
                processLineObj.Course.Description = courseLine.Substring(processLineObj.Course.Name.Length, courseLine.Length - processLineObj.Course.Name.Length - 3).Trim();
            }
            else
            {
                // If linePart starts with a " " it's a continuation of description
                processLineObj.Course.Description += " " + courseLine.Trim();
            }
        }

        private class ProcessLineObj
        {
            public List<Course> Courses { get; set; }
            public Course Course { get; set; }
            public CourseRelationshipType RelationshipType { get; set; }

            public ProcessLineObj()
            {
                Courses = new List<Course>();
                Course = new Course();
                RelationshipType = CourseRelationshipType.Unset;
            }
        }
    }
}