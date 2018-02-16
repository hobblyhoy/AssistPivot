using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class CollegeController : ApiController
    {
        public JsonResult Get()
        {
            var ret = new List<CollegeDto>();
            ret.Add(new CollegeDto() { CollegeId = 0, Name = "Allan Hancock College", Shorthand = "AHC" });
            ret.Add(new CollegeDto() { CollegeId = 1, Name = "American River College", Shorthand = "ARC" });
            ret.Add(new CollegeDto() { CollegeId = 2, Name = "Antelope Valley College", Shorthand = "AVC" });
            return new JsonResult() { Data = ret, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public JsonResult Get(string id)
        {
            var ret = new List<CourseDto>();
            ret.Add(new CourseDto() { CourseId = 0, CollegeId = 0, Name = "Psychology 100 Intro to Psychology", Shorthand = "PSYC 100" });
            ret.Add(new CourseDto() { CourseId = 1, CollegeId = 0, Name = "Calculus III", Shorthand = "MATH 170" });
            return new JsonResult() { Data = ret, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public class CollegeDto //todo eventually this will be replaced w/ a DB object
        {
            public int CollegeId { get; set; }
            public string Name { get; set; }
            public string Shorthand { get; set; }
        }

        public class CourseDto //todo eventually this will be replaced w/ a DB object
        {
            public int CourseId { get; set; }
            public int CollegeId { get; set; }
            public string Name { get; set; }
            public string Shorthand { get; set; }
            public string Year { get; set; }
            public float Units { get; set; }
        }
    }

}