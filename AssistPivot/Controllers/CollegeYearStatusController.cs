using AssistPivot.DAL;
using AssistPivot.Models;
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
            // So this is pretty cool-- db.<Table>.Include("<col>") tells it to not lazy load the object and gets it as part of the initial DB request.
            // Since we need it to evaluate immediately (.ToList), lazy loading would mean making a request for every college and year in the list
            // EF sql is now being written to Output for confirmation. .Include results in a LEFT OUTER JOIN [dbo].[<table>]
            List<CollegeYearStatus> result = db.CollegeYearStatuses.Include("College").Include("Year").ToList();
            var dto = result.Select(status => new CollegeYearStatusDto(status));
            return new JsonResult() { Data = dto, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}