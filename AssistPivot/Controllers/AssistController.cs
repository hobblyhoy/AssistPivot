using AssistPivot.DAL;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class AssistController : ApiController
    {
        public AssistDbContext db = new AssistDbContext();

        public JsonResult Get(int collegeId, int yearId, bool updateRequest)
        {
            // Get or create our CollegeYearStatus object
            var status = db.CollegeYearStatuses
                    .FirstOrDefault(s => s.College.CollegeId == collegeId && s.Year.YearId == yearId);

            if (status == null)
            {
                //status = new CollegeYearStatus(collegeId, yearId, db);
                status = new CollegeYearStatus()
                {
                    College = db.Colleges.FirstOrDefault(c => c.CollegeId == collegeId)
                    , Year = db.Years.FirstOrDefault(y => y.YearId == yearId)
                    , UpdateStatus = UpdateStatusTypes.None
                };
                db.CollegeYearStatuses.Add(status);
                db.SaveChanges();
            }


            return new JsonResult() { Data = status, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}