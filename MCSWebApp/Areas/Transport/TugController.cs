using System;
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

namespace MCSWebApp.Areas.Transport
{
    [Area("Transport")]
    public class TugController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public TugController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Transport];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Tug];
            ViewBag.BreadcrumbCode = WebAppMenu.Tug;

            return View();
        }

        public async Task<IActionResult> ExcelExport1()
        {
            string sFileName = "Tug.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            FilePath = Path.Combine(FilePath, sFileName);

            var mapper = new Npoi.Mapper.Mapper();
            mapper.Ignore<tug>(o => o.organization_);
            mapper.Put(dbContext.tug, "Tug", true);
            mapper.Save(FilePath);

            var memory = new MemoryStream();
            using (var stream = new FileStream(FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            //Throws Generated file to Browser
            try
            {
                return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
            }
            // Deletes the generated file
            finally
            {
                var path = Path.Combine(FilePath, sFileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "Tug.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Tug");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Tug Name");
                row.CreateCell(1).SetCellValue("Tug Id");
                row.CreateCell(2).SetCellValue("Owner");
                row.CreateCell(3).SetCellValue("Make");
                row.CreateCell(4).SetCellValue("Model");
                row.CreateCell(5).SetCellValue("Model Year");
                row.CreateCell(6).SetCellValue("Manufactured Year");
                row.CreateCell(7).SetCellValue("Is Active");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.tug.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var owner_name = "";
                    var contractor = dbFind.contractor.Where(o => o.id == Value.vendor_id).FirstOrDefault();
                    if (contractor != null) owner_name = contractor.business_partner_name.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(Value.vehicle_name);
                    row.CreateCell(1).SetCellValue(Value.vehicle_id);
                    row.CreateCell(2).SetCellValue(owner_name);
                    row.CreateCell(3).SetCellValue(Value.vehicle_make);
                    row.CreateCell(4).SetCellValue(Value.vehicle_model);
                    row.CreateCell(5).SetCellValue(Convert.ToDouble(Value.vehicle_model_year));
                    row.CreateCell(6).SetCellValue(Convert.ToDouble(Value.vehicle_manufactured_year));
                    row.CreateCell(7).SetCellValue(Convert.ToBoolean(Value.is_active));

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
