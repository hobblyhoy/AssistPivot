using AssistPivot.DAL;
using AssistPivot.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AssistPivot.Managers
{
    public class ScraperManager
    {
        public static HttpClient client = new HttpClient();
        public const string welcomePageUrl = "http://www.assist.org/web-assist/welcome.html";
        public const string courseGroupSeperator = "--------------------------------------------------------------------------------";
        public string[] badRelationshipIndicators = 
            { "not articulated", "not available for articulation", "no comparable course", "no articulation", "no course articulated", "course denied" };
        public AssistManager assistMan = null;
        private AssistManager AssistMan()
        {
            if (assistMan == null) assistMan = new AssistManager();
            return assistMan;
        }

        public ScraperManager()
        {
            if (client.Timeout.Minutes != 15) client.Timeout = TimeSpan.FromMinutes(15);
        }

        public async Task<List<College>> GetCollegesFromDbOrScrape(AssistDbContext db)
        {
            // Pull in the existing colleges from the DB. If they're recently updated dont bother with the scrape.
            var dbColleges = db.Colleges.ToList();
            var oneWeekAgo = DateTimeOffset.Now.AddDays(-7);
            if (dbColleges.Count > 0 && dbColleges.Min(c => c.UpToDateAsOf) > oneWeekAgo)
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
                        var matchingDbCollege = dbColleges.FirstOrDefault(dbCol => dbCol.Equals(scrapedCollege));
                        if (matchingDbCollege != null)
                        {
                            // Make our "equal enough" db context object actually equal to what we just scrapped from the site.
                            // This will usually just be updating the UpToDateAsOf property but will also handle updates to 
                            // either college name or shorthand id.
                            matchingDbCollege.Patch(scrapedCollege);
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

        public async Task UpdateCourseRelationships(AssistDbContext db, College userSelectedCollege, Year year)
        {
            // Our datasets are relatively small so load up everything we plan to touch into memory
            var otherColleges = db.Colleges.Where(c => c.CollegeId != userSelectedCollege.CollegeId).ToList();
            var dbCourseSets = db.CourseSets.Include("College").Include("Year").ToList();
            var dbCourseRelationships = db.CourseRelationships.Include("ToCourseSet").Include("FromCourseSet").ToList();
            var dbKnownRequests = db.KnownRequests.ToList();

            foreach (var otherCollege in otherColleges)
            //foreach (var toCollege in new College[] { toColleges[0] }) //DEBUG
            {
                await UpdateCollegeYearBundleRelationships(db, userSelectedCollege, otherCollege, year, dbCourseSets, dbCourseRelationships, dbKnownRequests);
                await UpdateCollegeYearBundleRelationships(db, otherCollege, userSelectedCollege, year, dbCourseSets, dbCourseRelationships, dbKnownRequests);
            }
        }

        // This was written from the point of view of being provided a From college and finding argreements From this college To all others
        // However this was just to make dev easier, it can be reversed to 
        private async Task UpdateCollegeYearBundleRelationships(AssistDbContext db, College fromCollege, College toCollege, Year year 
            , List<CourseSet> dbCourseSets, List<CourseRelationship> dbCourseRelationships, List<KnownRequest> dbKnownRequests)
        {
            //Get this request from the DB
            var url = RequestUrl(fromCollege.Shorthand, toCollege.Shorthand, year.Name);
            var requestShell = new KnownRequest() { RequestTo = RequestSource.Main, Url = url };
            var knownRequest = dbKnownRequests.FirstOrDefault(r => r.LooseEquals(requestShell));
            if (knownRequest == null)
            {
                db.KnownRequests.Add(requestShell);
                dbKnownRequests.Add(requestShell);
                knownRequest = requestShell;
            }
            else
            {
                // If we know this request returns nothing we can skip it
                if (!knownRequest.IsValid() && knownRequest.UpToDateAsOf > DateTimeOffset.Now.AddDays(-14)) return;
            }

            string result;
            using (var response = await client.GetAsync(url))
            {
                using (var content = response.Content)
                {
                    result = await content.ReadAsStringAsync();
                    knownRequest.Update(result.Length);
                    db.SaveChanges();
                    //result = DebugManager.RequestAhcToCpp1516(); //DEBUG
                }
            }
            // Technically this is an html doc but the bit we care about is always going to be between the only set of PRE tags
            // so we'll skip the html doc overhead and do it old school
            var dirtyList = result.Between("<PRE>", "</PRE>").Trim().Split(courseGroupSeperator);
            var validCourseRelationships = new List<string>();
            var multiCoursesRegex = RegexManager.MultiCourseRegex();
            // Clean phase 1, remove single courses or non course data, add these to cleanList
            foreach (var potentialCourseRela in dirtyList)
            {
                // Regex match on multiple courses and exclude our known empty comparison cases
                if (multiCoursesRegex.IsMatch(potentialCourseRela) && !badRelationshipIndicators.Any(s => potentialCourseRela.ToLower().Contains(s)))
                {
                    validCourseRelationships.Add(potentialCourseRela);
                }

            }

            // Clean phase 2 + parse, all within each block of course relationships
            // parse the ones which do into to/from courses
            var courseRelaExtractedBucket = new List<CourseRelationship>();
            foreach (var courseRelaRaw in validCourseRelationships)
            //var courseRelaRaw = validCourseRelationships[0]; //DEBUG
            {
                using (StringReader reader = new StringReader(courseRelaRaw))
                {
                    var toPart = "";
                    var fromPart = "";

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // if the line doesn't contain a |, skip (notes, department headers, etc. Never course data)
                        if (line.IndexOf('|') == -1) continue;

                        // strip formatting
                        line = line.Replace("<B >", "").Replace("</B>", "");
                        line = line.Replace("<U >", "").Replace("</U>", "");

                        // break line into to/from parts (before/after the |  ...order matters!)
                        var lineParts = line.Split("|", 2);

                        toPart += lineParts[0] + System.Environment.NewLine;
                        fromPart += lineParts[1] + System.Environment.NewLine;
                    }
                    toPart = toPart.Trim();
                    fromPart = fromPart.Trim();
                    if (toPart.Length == 0 || fromPart.Length == 0) continue;

                    var toCourseSet = new CourseSet(toCollege, year, toPart);
                    var fromCourseSet = new CourseSet(fromCollege, year, fromPart);

                    //Prevent building dupe courses
                    var toCourseSetDb = dbCourseSets.FirstOrDefault(c => c.Equals(toCourseSet));
                    var fromCourseSetDb = dbCourseSets.FirstOrDefault(c => c.Equals(fromCourseSet));

                    var now = DateTimeOffset.Now;
                    if (toCourseSetDb != null)
                    {
                        toCourseSetDb.UpToDateAsOf = now;
                        toCourseSet = toCourseSetDb;
                    }
                    else
                    {
                        dbCourseSets.Add(toCourseSet);
                    }

                    if (fromCourseSetDb != null)
                    {
                        fromCourseSetDb.UpToDateAsOf = now;
                        fromCourseSet = fromCourseSetDb;
                    }
                    else
                    {
                        dbCourseSets.Add(fromCourseSet);
                    }


                    var courseRela = new CourseRelationship() { ToCourseSet = toCourseSet, FromCourseSet = fromCourseSet, UpToDateAsOf = DateTimeOffset.Now };
                    courseRelaExtractedBucket.Add(courseRela);
                }
            }

            var courseRelaDbBucket = dbCourseRelationships.Where(
                            cr => cr.FromCourseSet.College.CollegeId == fromCollege.CollegeId
                            && cr.ToCourseSet.College.CollegeId == toCollege.CollegeId
                            && cr.FromCourseSet.Year.YearId == year.YearId
                        ).ToList();


            //Compare what we found vs what the DB holds to figure out what to add/remove
            var relasToAdd = courseRelaExtractedBucket.Except(courseRelaDbBucket).ToList();
            var relasToRemove = courseRelaDbBucket.Except(courseRelaExtractedBucket).ToList();

            foreach (var rela in relasToAdd)
            {
                db.CourseRelationships.Add(rela);
                dbCourseRelationships.Add(rela);
            }
            foreach (var rela in relasToRemove)
            {
                db.CourseRelationships.Remove(rela);
                dbCourseRelationships.Remove(rela);
            }

            //Save after each toCollege has been processed.
            db.SaveChanges();
        }

    }
}