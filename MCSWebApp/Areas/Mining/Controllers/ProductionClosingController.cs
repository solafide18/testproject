﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.Entity;
using MCSWebApp.Controllers;
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

namespace MCSWebApp.Areas.Mining.Controllers
{
    [Area("Mining")]
    public class ProductionClosingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ProductionClosingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionClosing];
            ViewBag.BreadcrumbCode = WebAppMenu.ProductionClosing;

            return View();
        }

        public IActionResult Detail(string Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionClosing];
            ViewBag.BreadcrumbCode = WebAppMenu.ProductionClosing;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "ProductionClosing.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("ProductionClosing");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Transaction Date");
                row.CreateCell(2).SetCellValue("Accounting Period");
                row.CreateCell(3).SetCellValue("Advance Contract");
                row.CreateCell(4).SetCellValue("Advance Contract Reference");
                row.CreateCell(5).SetCellValue("From Date");
                row.CreateCell(6).SetCellValue("To Date");
                row.CreateCell(7).SetCellValue("Volume");
                row.CreateCell(8).SetCellValue("Distance");
                row.CreateCell(9).SetCellValue("Note");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.production_closing.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    var accounting_period_name = "";
                    var accounting_period = dbFind.accounting_period
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.id == baris.accounting_period_id).FirstOrDefault();
                    if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var advance_contract_number = "";
                    var advance_contract = dbFind.advance_contract
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.id == baris.advance_contract_id).FirstOrDefault();
                    if (advance_contract != null) advance_contract_number = advance_contract.advance_contract_number.ToString();

                    var advance_contract_reference_name = "";
                    var advance_contract_reference = dbFind.advance_contract_reference
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.id == baris.advance_contract_reference_id).FirstOrDefault();
                    if (advance_contract_reference != null) advance_contract_reference_name = advance_contract_reference.name.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(baris.transaction_date).ToString("yyyy-MM-dd"));
                    row.CreateCell(2).SetCellValue(accounting_period_name);
                    row.CreateCell(3).SetCellValue(advance_contract_number);
                    row.CreateCell(4).SetCellValue(advance_contract_reference_name);
                    row.CreateCell(5).SetCellValue(" " + Convert.ToDateTime(baris.from_date).ToString("yyyy-MM-dd"));
                    row.CreateCell(6).SetCellValue(" " + Convert.ToDateTime(baris.to_date).ToString("yyyy-MM-dd"));
                    row.CreateCell(7).SetCellValue(PublicFunctions.Pecahan(baris.volume));
                    row.CreateCell(8).SetCellValue(PublicFunctions.Pecahan(baris.distance));
                    row.CreateCell(9).SetCellValue(baris.note);
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
