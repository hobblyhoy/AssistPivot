using AssistPivot.DAL;
using AssistPivot.Managers;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var response = new AssistDto();
            var allStatusesFromDb = db.CollegeYearStatuses.Include("College").Include("Year").ToList();
            var thisRequestsStatus = allStatusesFromDb.FirstOrDefault(s => s.College.CollegeId == collegeId && s.Year.YearId == yearId);
            var inFlightRequestStatus = allStatusesFromDb.FirstOrDefault(s => s.UpdateStatus == UpdateStatusTypes.InFlight);

            var usersRequestedCollege = db.Colleges.Find(collegeId);
            var usersRequestedYear = db.Years.Find(yearId);

            if (thisRequestsStatus == null)
            {
                thisRequestsStatus = new CollegeYearStatus(db, collegeId, yearId);
                db.CollegeYearStatuses.Add(thisRequestsStatus);
                db.SaveChanges();
            }

            // TODO if the data is less than a week old force updateRequest = false
            if ()

            // Requests to Assist are VERY painful. They can take up to 60 seconds and we have to make over 150 requests on updates.
            // Since we like Assist and dont want to kill their servers we only let one user at a time actually make requests.
            // But we can serve cached data to anyone at any time, except for the particular requests in midflight since the data
            // will be in a funky state.
            switch (thisRequestsStatus.UpdateStatus)
            {
                case UpdateStatusTypes.Completed:
                    var courses = db.Courses
                            .Include("College").Include("Year")
                            .Where(course => course.College == usersRequestedCollege && course.Year == usersRequestedYear)
                            .ToList();
                    //get the list of relationships which match any one of these CourseIds
                    var courseRelas = db.CourseRelationships
                            .Where(rela => rela.FromCourses.Any(course => courses.Contains(course)))
                            .ToList();
                    response.CourseRelationships = courseRelas;
                    break;
                case UpdateStatusTypes.InFlight:
                    response.Notification = "Sorry, a request for this college and year is already in progress. Data will not be available until it completes";
                    return AssistReturn(response);
                case UpdateStatusTypes.Error:
                case UpdateStatusTypes.None:
                    updateRequest = true; //This is enforced on frontend but why trust users?
                    break;
            }

            //Deal with updates
            if (updateRequest && inFlightRequestStatus != null)
            {
                response.Notification = "Sorry, an update request for another college/year is already in progress. Please try again later.";
            }
            else if (false && updateRequest)
            {
                thisRequestsStatus.UpdateStatus = UpdateStatusTypes.InFlight;
                db.SaveChanges();

                try
                {
                    var result = await scraperMan.UpdateCourseEquivalents(db, usersRequestedCollege, usersRequestedYear);
                    thisRequestsStatus.UpdateStatus = UpdateStatusTypes.Completed;
                }
                catch (Exception e)
                {
                    //todo a notification to myself
                    response.Notification = "FAILURE- There was a failure while trying to update this college/year. Assist's servers may be busy. Please try again later.";
                    thisRequestsStatus.UpdateStatus = UpdateStatusTypes.Error;
                }

                db.SaveChanges();
            }

            return AssistReturn(response);
        }

        private JsonResult AssistReturn(AssistDto response)
        {
            return new JsonResult() { Data = response, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public class AssistDto
        {
            public List<CourseRelationship> CourseRelationships { get; set; }
            public string Notification { get; set; }
        }

    }
}