using AssistPivot.DAL;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class YearController : ApiController
    {
        public AssistDbContext db = new AssistDbContext();

        public async Task<JsonResult> Get()
        {
            List<Year> yearList = db.Years.ToList();
            var currentYear = DateTimeOffset.Now.Year;
            var latestYear = yearList.Select(y => y.FirstYearExpanded).DefaultIfEmpty(0).Max();
            var latestExpectedYear = currentYear - 2; //because they run a bit behind..
            if (latestExpectedYear > latestYear)
            {
                //The year list is out of date, time to update
                var startYear = (latestYear >= 1985) ? latestYear+1 : 1985;

                for (int year = startYear; year <= latestExpectedYear; year++)
                {
                    var nextYear = year + 1; //increment here to be OK with 1999 => 2000 rollover
                    var thisYearLastDigits = Convert.ToString(year % 100, 10).PadLeft(2,'0'); //e.g. 2005 => "05"
                    var nextYearLastDigits = Convert.ToString(nextYear % 100, 10).PadLeft(2,'0');
                    var name = thisYearLastDigits + "-" + nextYearLastDigits;

                    var yearToAdd = new Year() { FirstYearExpanded = year, Name = name };
                    db.Years.Add(yearToAdd);
                    // Keep our own copy up-to-date so the return list doesn't have to make another db trip
                    yearList.Add(yearToAdd);
                }

                // We want the full objects on the front-end including the DB ids. Thankfully the objects we pushed
                // to our list stay in the DB scope and are automatically updated after saving. However it does mean we 
                // cant fire and forget.
                db.SaveChanges();
            }

            return new JsonResult() { Data = yearList, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}