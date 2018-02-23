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

        public async Task<string> UpdateCourseEquivalents(College college, Year year)
        {

            var url = RequestUrl(college.Shorthand, "CPP", year.Name);
            //using (var response = await client.GetAsync(url))
            //{
            //    using (var content = response.Content)
            {
                //var result = await content.ReadAsStringAsync();
                var result = DebugManager.RequestAhcToCpp1516();
                // Technically this is an html doc but the bit we care about is always going to be between the only set of PRE tags
                // so we'll skip the html doc overhead and do it old school
                var dirtyList = result.Between("<PRE>", "</PRE>").Trim().Split(courseGroupSeperator);
                var cleanList = new List<string>();
                var matchesTwoCourses = @"[(].*?[0-9].*?[)].*?[|].*?[(].*?[0-9].*?[)]";
                var twoCoursesRegex = new Regex(matchesTwoCourses, RegexOptions.Singleline);
                // Clean phase 1, remove single courses or non course data, add these to cleanList
                foreach (var potentialCourseRela in dirtyList)
                {
                    // Remove the entities that dont contain the pattern *(*)*|*(*)* 
                    // also exclude our known empty comparison cases
                    if (twoCoursesRegex.IsMatch(potentialCourseRela) && !emptySignifiers.Any(s => potentialCourseRela.Contains(s)))
                    {
                        cleanList.Add(potentialCourseRela);
                    }

                }

                // Clean phase 2 + parse, all within each block of course relationships
                // remove any whole lines which dont contain the verticle seperator "|" (notes, department headers, etc. Never course data)
                // parse the ones which do into to/from courses
                var courseList = new List<Course>();
                foreach (var courseRelaRaw in cleanList)
                {
                    using (StringReader reader = new StringReader(courseRelaRaw))
                    {
                        string line;
                        var fromCourses = new Course();
                        var toCourse = new Course();
                        while ((line = reader.ReadLine()) != null)
                        {
                            // if the line doesn't contain a |, skip (notes, department headers, etc. Never course data)
                            if (line.IndexOf('|') == -1) continue;
                            // break line into to/from parts (before/after the |)
                            line = line.Substring(16); //remove assist formatting while retaining indentation
                            line = "hell | oh |boy";
                            var lineParts = line.Split("|", 2);
                            // if linePart starts with a " " it's a continuation of description, add onto our current course.

                            //if not it's a new course. Save the old, create a new and parse for name, start of description, and credits
                        }
                    }
                }
                



                var test = cleanList.Stringify("\r\n>>>>>>>>\r\n");
            }
            //}

            return "donezo";
        }

    }
}