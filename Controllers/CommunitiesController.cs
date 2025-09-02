using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
    }
}