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

namespace MCSWebApp.Areas.DailyRecord.Controllers
{
    [Area("DailyRecord")]
    public class TidalWaveController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public TidalWaveController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Contractor];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.DailyRecord];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.TideandWave];
            ViewBag.BreadcrumbCode = WebAppMenu.TideandWave;

            return View();
        }

        public IActionResult Detail(string Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Contractor];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.DailyRecord];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.TideandWave];
            ViewBag.BreadcrumbCode = WebAppMenu.TideandWave;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "TidalWave.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Tidal Wave");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Date");
                row.CreateCell(1).SetCellValue("Tidal Wave Value");
                row.CreateCell(2).SetCellValue("Business Area");
                row.CreateCell(3).SetCellValue("Shift");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.tidalwave.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var business_area_code = "";
                    var business_area = dbFind.business_area.Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                        o.id == Value.business_area_id).FirstOrDefault();
                    if (business_area != null) business_area_code = business_area.business_area_code.ToString();

                    var shift_code = "";
                    var shift = dbFind.shift.Where(o => o.id == Value.shift_id).FirstOrDefault();
                    if (shift != null) shift_code = shift.shift_code.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(" " + Convert.ToDateTime(Value.date_time).ToString("yyyy-MM-dd"));
                    row.CreateCell(1).SetCellValue(PublicFunctions.Pecahan(Value.tidalwave_value));
                    row.CreateCell(2).SetCellValue(business_area_code);
                    row.CreateCell(3).SetCellValue(shift_code);

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
