using AssistPivot.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AssistPivot.Controllers
{
    public class AssistController : ApiController
    {
        public string Get(int CollegeId, int CourseId)
        {
            return "Right get function";
        }

    }
}