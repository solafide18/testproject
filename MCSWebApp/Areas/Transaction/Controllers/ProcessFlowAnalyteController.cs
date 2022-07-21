﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.Entity;
using MCSWebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.IO;
using Npoi.Mapper;
using Npoi.Mapper.Attributes;

namespace MCSWebApp.Areas.Transaction.Controllers
{
    [Area("Transaction")]
    public class ProcessFlowAnalyteController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ProcessFlowAnalyteController(IConfiguration Configuration)
            : base(Configuration)
        {
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.AreaBreadcrumb = "Transaction";
            ViewBag.Breadcrumb = "Process Flow Analyte";

            return View();
        }
    }
}
