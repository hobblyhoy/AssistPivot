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
                updateRequest = false; //Also enforced on frontend
            }

            // Requests to Assist are VERY painful. They can take up to 60 seconds and we have to make over 150 requests on updates.
            // Since we like Assist and dont want to kill their servers we only let one user at a time actually make requests.
            // But we can serve cached data to anyone at any time, except for the college/year request thats in flight since the data
            // will be in a funky state.
            switch (thisRequestsStatus.UpdateStatus)
            {
                case UpdateStatusTypes.Completed:
                    var courses = GetCourses(db, usersRequestedCollege, usersRequestedYear);
                    response.Courses = courses;
                    response.CourseRelationships = GetCourseRelationships(db, courses);
                    var notiTextCompleted = $"Finished getting course relationships. We found {response.Courses.Count} Courses totalling {response.CourseRelationships.Count} Relationships";
                    response.SetNotification(notiTextCompleted, NotificationType.Notice);
                    break;
                case UpdateStatusTypes.InFlight:
                    var notiTextInFlight = "Sorry, an update request for this college and year is already in progress."
                        + " Data will not be available until it completes";
                    response.SetNotification(notiTextInFlight, NotificationType.Notice);
                    return response;
                case UpdateStatusTypes.Error:
                case UpdateStatusTypes.None:
                    updateRequest = true; //Also enforced on frontend
                    break;
            }

            // Handle update requests
            if (updateRequest && inFlightRequestStatus != null)
            {
                var notiText = "Sorry, an update request is already in progress."
                    + " Only one request may occur at a time. Any existing cached data will appear below. Please try again later.";
                response.SetNotification(notiText, NotificationType.Notice);
            }
            else if (updateRequest)
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

        public List<CourseSet> GetCourses(AssistDbContext db, College college, Year year)
        {
            var ret = db.CourseSets
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

        public List<CourseRelationship> GetCourseRelationships(AssistDbContext db, List<CourseSet> courses)
        {
            return new List<CourseRelationship>();
            if (courses.Count == 0) return new List<CourseRelationship>();

            var coursesIds = courses.Select(c => c.CourseSetId);
            //get the list of relationships which match any one of these courses on the "from" side
            //return db.CourseRelationships
            //        .Where(rela => rela.FromCourseSet.Any(course => coursesIds.Contains(course.CourseSetId)))
            //        .ToList();
        }

        public enum NotificationType { None, Notice, Error }
        public class AssistDto
        {
            //turns out you can only send back public members of a JsonResult :[
            public List<CourseRelationship> CourseRelationships { get; set; }
            public List<CourseSet> Courses { get; set; }
            public string NotificationText { get; set; }
            public NotificationType NotificationType { get; set; }

            public void SetNotification(string notification, NotificationType notificationType)
            {
                this.NotificationText = notification;
                this.NotificationType = notificationType;
            }
        }


    }

}