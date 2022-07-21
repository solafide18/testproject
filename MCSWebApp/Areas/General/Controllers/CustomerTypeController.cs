﻿using BusinessLogic.Entity;
using Common;
using MCSWebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.IO;
using Npoi.Mapper;
using Npoi.Mapper.Attributes;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace MCSWebApp.Areas.General.Controllers
{
    [Area("General")]
    public class CustomerTypeController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public CustomerTypeController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.CustomerType];
            ViewBag.BreadcrumbCode = WebAppMenu.CustomerType;

            return View();
        }

        public async Task<IActionResult> Detail(string Id)
        {
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.CustomerType];
            ViewBag.BreadcrumbCode = WebAppMenu.CustomerType;

            try
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    var svc = new CustomerType(CurrentUserContext);
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
            string sFileName = "CustomerType.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("CustomerType");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Customer Type Code");
                row.CreateCell(1).SetCellValue("Customer Type Name");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.customer_type.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.customer_type_code);
                    row.CreateCell(1).SetCellValue(baris.customer_type_name);
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
