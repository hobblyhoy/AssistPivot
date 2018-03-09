using AssistPivot.DAL;
using AssistPivot.Managers;
using AssistPivot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace AssistPivot.Controllers
{
    public class AssistController : ApiController
    {
        public AssistDbContext db = new AssistDbContext();
        public AssistManager assistMan = new AssistManager();


        public async Task<JsonResult> Get(int collegeId, int yearId, bool updateRequest)
        {
            var usersRequestedCollege = db.Colleges.Find(collegeId);
            var usersRequestedYear = db.Years.Find(yearId);

            AssistManager.AssistDto dto = new AssistManager.AssistDto();
            try
            {

                dto = await assistMan.GetAndUpdate(db, usersRequestedCollege, usersRequestedYear, updateRequest);
            }
            catch
            {
                // TODO If I keep iterating on this.. send me some kind of notification when we hit this
                dto.SetNotification("Server Error 😭 Please try again later", AssistManager.NotificationType.Error);
            }

            return AssistReturn(dto);
        }


        public JsonResult AssistReturn(AssistManager.AssistDto response)
        {
            return new JsonResult() { Data = response, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }


    }
}