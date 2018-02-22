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
            List<College> result = await scraperMan.GetCollegesFromDbOrScrape(db);
            return new JsonResult() { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }

}