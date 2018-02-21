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
            List<CollegeYearStatus> result = db.CollegeYearStatuses.ToList();
            //okay cool so this works now but I have next steps thoughts:
            // (1) in order to extract the id from each one is going to require a db hit for each one (I think) and I dont actually need
            // the full object downstream. In fact it's a ton of redundant data I have to weed out so def want to be able to get JUST the key.
            // TODO figure out how to check how many DB calls are being made
            // (2) Not needed right this sec but I will have to build some kind of mapper function to take care of these dates on the FE
            return new JsonResult() { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}