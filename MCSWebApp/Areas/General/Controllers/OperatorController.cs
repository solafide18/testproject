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
using Microsoft.EntityFrameworkCore;

namespace MCSWebApp.Areas.General.Controllers
{
    [Area("General")]
    public class OperatorController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public OperatorController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Operator];
            ViewBag.BreadcrumbCode = WebAppMenu.Operator;

            return View();
        }

        public async Task<IActionResult> Detail(String Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Operator];
            ViewBag.BreadcrumbCode = WebAppMenu.Operator;

            try
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    var timesheet = await dbContext.vw_timesheet.Where(o => o.id == Id).FirstOrDefaultAsync();

                    if (timesheet != null)
                    {
                        ViewBag.Timesheet = timesheet;
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
            string sFileName = "Operator.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Operator");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Operator Name");
                row.CreateCell(1).SetCellValue("NIK");
                row.CreateCell(2).SetCellValue("Versatility");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext._operator.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.operator_name);
                    row.CreateCell(1).SetCellValue(baris.nik);
                    row.CreateCell(2).SetCellValue(baris.versatility);
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
