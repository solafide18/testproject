﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.Entity;
using MCSWebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using Common;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.IO;
using Npoi.Mapper;
using Npoi.Mapper.Attributes;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace MCSWebApp.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    public class AccountingPeriodController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public AccountingPeriodController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.AccountingManagement];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.AccountingPeriod];
            ViewBag.BreadcrumbCode = WebAppMenu.AccountingPeriod;

            return View();
        }

        public async Task<IActionResult> Detail(string Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.AccountingManagement];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.AccountingPeriod];
            ViewBag.BreadcrumbCode = WebAppMenu.AccountingPeriod;

            try
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    var svc = new AccountingPeriod(CurrentUserContext);
                    var record = await svc.GetByIdAsync(Id);
                    if (record != null)
                    {
                        ViewBag.Id = Id;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "AccountingPeriod.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("AccountingPeriod");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Accounting Period Name");
                row.CreateCell(1).SetCellValue("Start Date");
                row.CreateCell(2).SetCellValue("End Date");
                row.CreateCell(3).SetCellValue("Is Closed");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.accounting_period.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.accounting_period_name);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(baris.start_date).ToString("yyyy-MM-dd"));
                    row.CreateCell(2).SetCellValue(" " + Convert.ToDateTime(baris.end_date).ToString("yyyy-MM-dd"));
                    row.CreateCell(3).SetCellValue(Convert.ToBoolean(baris.is_closed));
                    RowCount++;
                }
                workbook.Write(fs);
                using (var stream = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                //Throws Generated file to Browser
                try
                {
                    return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
                }
                // Deletes the generated file from /wwwroot folder
                finally
                {
                    var path = Path.Combine(FilePath, sFileName);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
        }

    }
}
