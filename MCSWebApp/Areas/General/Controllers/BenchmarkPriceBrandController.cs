using Common;
using MCSWebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCSWebApp.Areas.General.Controllers
{
    [Area("General")]
    public class BenchmarkPriceBrand : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BenchmarkPriceBrand(IConfiguration Configuration)
            : base(Configuration)
        {
        }

        public IActionResult Index()
        {
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BenchmarkPriceBrand];
            ViewBag.BreadcrumbCode = WebAppMenu.BenchmarkPriceBrand;

            return View();
        }
    }
}
