using AssistPivot.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class CollegeYearStatusController : ApiController
    {
        public AssistDbContext db = new AssistDbContext();

        public JsonResult Get()
        {
            var result = db.CollegeYearStatuses;
            return new JsonResult() { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}