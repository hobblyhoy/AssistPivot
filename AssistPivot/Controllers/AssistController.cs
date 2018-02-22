using AssistPivot.DAL;
using AssistPivot.Managers;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class AssistController : ApiController
    {
        public AssistDbContext db = new AssistDbContext();
        public ScraperManager scraperMan = new ScraperManager();

        public async Task<JsonResult> Get(int collegeId, int yearId, bool updateRequest)
        {
            // Get or create our CollegeYearStatus object
            var status = db.CollegeYearStatuses
                    .FirstOrDefault(s => s.College.CollegeId == collegeId && s.Year.YearId == yearId);

            if (status == null)
            {
                status = new CollegeYearStatus(db, collegeId, yearId);
                db.CollegeYearStatuses.Add(status);
                db.SaveChanges();
            }

            // TODO handle already inflight jobs
            // (this is to prevent multi scrapes and potential data mismatch if two different users request 
            // data at the same time)

            if (updateRequest)
            {
                status.UpdateStatus = UpdateStatusTypes.InFlight;
                db.SaveChanges();

                var college = db.Colleges.Find(collegeId);
                var year = db.Years.Find(yearId);
                var result = await scraperMan.UpdateCourseEquivalents(college, year);

            }

            //get the list of all the courses on this collegeYear

            //get the list of relationships which match any one of these CourseIds

            //return rela list


            var dto = new CollegeYearStatusDto(status); //just temporary so I know when I get something back
            return new JsonResult() { Data = dto, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}