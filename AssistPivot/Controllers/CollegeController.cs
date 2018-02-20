using AssistPivot.DAL;
using AssistPivot.Managers;
using AssistPivot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class CollegeController : ApiController
    {
        public ScraperManager scraperMan = new ScraperManager();
        public AssistDbContext db = new AssistDbContext();

        public async Task<JsonResult> Get()
        {
            List<College> result = await scraperMan.GetCollegesFromDbOrScrape();
            return new JsonResult() { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        //public JsonResult Get(string id)
        //{
        //    var ret = new List<CourseDto>();
        //    ret.Add(new CourseDto() { CourseId = 0, CollegeId = 0, Name = "Psychology 100 Intro to Psychology", Shorthand = "PSYC 100" });
        //    ret.Add(new CourseDto() { CourseId = 1, CollegeId = 0, Name = "Calculus III", Shorthand = "MATH 170" });
        //    return new JsonResult() { Data = ret, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        //}

        //public class CourseDto //todo eventually this will be replaced w/ a DB object
        //{
        //    public int CourseId { get; set; }
        //    public int CollegeId { get; set; }
        //    public string Name { get; set; }
        //    public string Shorthand { get; set; }
        //    public string Year { get; set; }
        //    public float Units { get; set; }
        //}
    }

}