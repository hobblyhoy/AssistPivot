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

            // Requests to Assist are VERY painful. They can take up to 3 minutes and we have to make over 300 requests on updates.
            // Since we like Assist and dont want to kill their servers we only let one user at a time actually make requests.
            // But we can serve cached data to anyone at any time, except for the college/year request thats in flight since the data
            // will be incomplete.
            switch (thisRequestsStatus.UpdateStatus)
            {
                case UpdateStatusTypes.Completed:
                    var courses = GetCourses(db, usersRequestedCollege, usersRequestedYear);
                    response.Courses = ExtractCourseNames(courses, usersRequestedCollege);
                    response.CourseRelationships = GetCourseRelationships(db, courses);
                    var notiTextCompleted = $"Finished getting course relationships. We found {response.Courses.Count} Courses totaling {response.CourseRelationships.Count} Relationships.";
                    response.SetNotification(notiTextCompleted, NotificationType.Notice);
                    break;
                case UpdateStatusTypes.InFlight:
                    var notiTextInFlight = "Sorry, an update request for this college and year is already in progress."
                        + " Data will not be available until it completes.";
                    response.SetNotification(notiTextInFlight, NotificationType.Notice);
                    return response;
                case UpdateStatusTypes.Error: //intentional switch fallthrough
                case UpdateStatusTypes.None:
                    updateRequest = true; //Also enforced on frontend
                    break;
            }

            // Handle update requests
            if (updateRequest && inFlightRequestStatus != null)
            {
                var notiText = "Sorry, an update request is already in progress."
                    + " Only one request may occur at a time. If we have any previously stored data it will appear below. Try again later.";
                response.SetNotification(notiText, NotificationType.Notice);
            }
            else if (updateRequest)
            {
                thisRequestsStatus.SetStatus(UpdateStatusTypes.InFlight);
                db.SaveChanges();

                try
                {
                    await ScraperMan().UpdateCourseRelationships(db, usersRequestedCollege, usersRequestedYear);
                    thisRequestsStatus.SetStatus(UpdateStatusTypes.Completed);

                    var courses = GetCourses(db, usersRequestedCollege, usersRequestedYear);
                    response.Courses = ExtractCourseNames(courses, usersRequestedCollege);
                    response.CourseRelationships = GetCourseRelationships(db, courses);
                }
                catch (Exception e)
                {
                    // TODO If I keep iterating on this.. send me some kind of notification when we hit this
                    var notiText = "Failure while trying to update this college/year."
                        + " Assist's servers may be too busy to handle this request right now. Please try again later.";
                    response.SetNotification(notiText, NotificationType.Error);
                    thisRequestsStatus.SetStatus(UpdateStatusTypes.Error);
                }
                finally
                {
                    db.SaveChanges();
                }

                // Updates to Assist data will result in courses that are no longer bound to any courseRelationships.
                // These orphaned courseSets aren't hurting anyone but DB space is a whopping 32MB right now so 
                // they need to go.
                var allMatchedCourseSetIds = db.CourseRelationships
                            .Select(cr => new CourseSet[] { cr.FromCourseSet, cr.ToCourseSet })
                            .SelectMany(cia => cia)
                            .Distinct();

                var orphanedCourseSets = db.CourseSets.Except(allMatchedCourseSetIds);
                db.CourseSets.RemoveRange(orphanedCourseSets);
                db.SaveChanges();

                //Attach the status so the frontend can update the last updated value
                response.Status = new CollegeYearStatusDto(thisRequestsStatus);
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

        public List<CourseRelationship> GetCourseRelationships(AssistDbContext db, List<CourseSet> courseSets)
        {
            if (courseSets.Count == 0) return new List<CourseRelationship>();

            var coursesIds = courseSets.Select(c => c.CourseSetId).ToList();
            var ret = db.CourseRelationships
                .Include("FromCourseSet").Include("ToCourseSet")
                .Where(rela => coursesIds.Contains(rela.FromCourseSet.CourseSetId) || coursesIds.Contains(rela.ToCourseSet.CourseSetId))
                .ToList();
            return ret;
        }

        public List<string> ExtractCourseNames(List<CourseSet> courseSets, College targetCollege)
        {
            return courseSets
                .Where(c => c.College.Equals(targetCollege))
                .SelectMany(c => c.CommaDelimitedCourseNames.Split(','))
                .Distinct()
                .ToList();
        }


        public enum NotificationType { None, Notice, Error }
        public class AssistDto
        {
            //turns out you can only send back public members of a JsonResult :[
            public List<CourseRelationship> CourseRelationships { get; set; }
            public List<string> Courses { get; set; }
            public string NotificationText { get; set; }
            public NotificationType NotificationType { get; set; }
            public CollegeYearStatusDto Status { get; set; } 

            public void SetNotification(string notification, NotificationType notificationType)
            {
                this.NotificationText = notification;
                this.NotificationType = notificationType;
            }
        }


    }

}