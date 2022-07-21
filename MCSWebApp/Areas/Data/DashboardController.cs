using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MCSWebApp.Areas.Data
{
    [Area("Data")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.WebAppName = "Smart Mining";
            ViewBag.RootBreadcrumb = "Smart Mining";
            ViewBag.AreaBreadcrumb = "Data";
            ViewBag.Breadcrumb = "Dashboard";

            return View();
        }
    }
}
