using System.Web.Mvc;

namespace SkillSwap.Controllers
{
    public class CommunitiesController : Controller
    {
        // GET: Communities
        public ActionResult Index()
        {
            return View();
        }

        // GET: Communities/{id}
        public ActionResult Skill(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            ViewBag.SkillName = id;
            return View();
        }
    }
}
