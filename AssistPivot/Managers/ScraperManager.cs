using AssistPivot.DAL;
using AssistPivot.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AssistPivot.Managers
{
    public class ScraperManager
    {
        public static HttpClient client = new HttpClient();
        public const string welcomePageUrl = "http://www.assist.org/web-assist/welcome.html";
        public AssistDbContext db = new AssistDbContext();

        public async Task<List<College>> GetCollegesFromDbOrScrape()
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
    }
}