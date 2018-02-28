using AssistPivot.DAL;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AssistPivot.Managers
{
    public class AssistManager
    {
        private ScraperManager scraperMan = null;
        private ScraperManager ScraperMan()
        {
            if (scraperMan == null) scraperMan = new ScraperManager();
            return scraperMan;
        }

        public async Task<AssistDto> GetAndUpdate(AssistDbContext db, College usersRequestedCollege, Year usersRequestedYear, bool updateRequest)
        {
            var response = new AssistDto();
            var allStatusesFromDb = db.CollegeYearStatuses.Include("College").Include("Year").ToList();
            var thisRequestsStatus = allStatusesFromDb.FirstOrDefault(s => s.College == usersRequestedCollege && s.Year == usersRequestedYear);
            var inFlightRequestStatus = allStatusesFromDb.FirstOrDefault(s => s.UpdateStatus == UpdateStatusTypes.InFlight);

            if (thisRequestsStatus == null)
            {
                thisRequestsStatus = new CollegeYearStatus(db, usersRequestedCollege.CollegeId, usersRequestedYear.YearId);
                db.CollegeYearStatuses.Add(thisRequestsStatus);
                db.SaveChanges();
            }

            var twoWeeksAgo = DateTimeOffset.Now.AddDays(-14);
            if (thisRequestsStatus.UpToDateAsOf != null && thisRequestsStatus.UpToDateAsOf > twoWeeksAgo)
            {
                updateRequest = false; //Enforced on frontend but why trust users?
            }

            // Requests to Assist are VERY painful. They can take up to 60 seconds and we have to make over 150 requests on updates.
            // Since we like Assist and dont want to kill their servers we only let one user at a time actually make requests.
            // But we can serve cached data to anyone at any time, except for the college/year request thats in flight since the data
            // will be in a funky state.
            switch (thisRequestsStatus.UpdateStatus)
            {
                case UpdateStatusTypes.Completed:
                    response.CourseRelationships = GetCourseRelationships(db, usersRequestedCollege, usersRequestedYear);
                    break;
                case UpdateStatusTypes.InFlight:
                    var notiText = "Sorry, an update request for this college and year is already in progress."
                        + " Data will not be available until it completes";
                    response.SetNotification(notiText, NotificationType.Notice);
                    return response;
                case UpdateStatusTypes.Error:
                case UpdateStatusTypes.None:
                    updateRequest = true; //Enforced on frontend but why trust users?
                    break;
            }

            // Handle update requests
            if (updateRequest && inFlightRequestStatus != null)
            {
                var notiText = "Sorry, an update request is already in progress."
                    + " Only one request may occur at a time. Any existing cached data will appear below. Please try again later.";
                response.SetNotification(notiText, NotificationType.Notice);
            }
            else if (false && updateRequest)
            {
                thisRequestsStatus.UpdateStatus = UpdateStatusTypes.InFlight;
                db.SaveChanges();

                try
                {
                    await ScraperMan().UpdateCourseRelationships(db, usersRequestedCollege, usersRequestedYear);
                    response.CourseRelationships = GetCourseRelationships(db, usersRequestedCollege, usersRequestedYear);
                    thisRequestsStatus.UpdateStatus = UpdateStatusTypes.Completed;
                }
                catch (Exception e)
                {
                    //todo a notification to myself
                    var notiText = "Failure while trying to update this college/year."
                        + " Assist's servers may be too busy to handle this request right now. Please try again later.";
                    response.SetNotification(notiText, NotificationType.Error);
                    thisRequestsStatus.UpdateStatus = UpdateStatusTypes.Error;
                }
                finally
                {
                    db.SaveChanges();
                }
            }

            return response;
        }

        public List<Course> GetCourses(AssistDbContext db, College college, Year year)
        {
            var ret = db.Courses
                    .Include("College").Include("Year")
                    .Where(course => course.College.CollegeId == college.CollegeId && course.Year.YearId == year.YearId)
                    .ToList();

            return ret;
        }

        public List<CourseRelationship> GetCourseRelationships(AssistDbContext db, College college, Year year)
        {
            var courses = GetCourses(db, college, year);
            return GetCourseRelationships(db, courses);
        }

        public List<CourseRelationship> GetCourseRelationships(AssistDbContext db, List<Course> courses)
        {
            var coursesIds = courses.Select(c => c.CourseId);
            //get the list of relationships which match any one of these courses on the "from" side
            return db.CourseRelationships
                    .Where(rela => rela.FromCourses.Any(course => coursesIds.Contains(course.CourseId)))
                    .ToList();
        }

        public enum NotificationType { None, Notice, Error }
        public class AssistDto
        {
            //turns out you can only send back public members of a JsonResult :[
            public List<CourseRelationship> CourseRelationships { get; set; }
            public string Notification { get; set; }
            public NotificationType NotificationType { get; set; }

            public void SetNotification(string notification, NotificationType notificationType)
            {
                this.Notification = notification;
                this.NotificationType = notificationType;
            }
        }


    }

}