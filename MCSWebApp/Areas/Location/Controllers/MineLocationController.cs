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
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Npoi.Mapper;
using Npoi.Mapper.Attributes;

namespace MCSWebApp.Areas.Location.Controllers
{
    [Area("Location")]
    public class MineLocationController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public MineLocationController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Modelling];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Location];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MineLocation];
            ViewBag.BreadcrumbCode = WebAppMenu.MineLocation;

            return View();
        }

        public IActionResult Detail(string Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Modelling];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Location];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MineLocation];
            ViewBag.BreadcrumbCode = WebAppMenu.MineLocation;

            return View();
        }

        public async Task<IActionResult> ExcelExport1()
        {
            string sFileName = "MineLocation.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            FilePath = Path.Combine(FilePath, sFileName);

            var mapper = new Mapper();
            mapper.Ignore<mine_location>(o => o.organization_).Ignore<mine_location>(o => o.exposed_coal);
            mapper.Put(dbContext.mine_location, "Mine Location", true);

            mapper.Ignore<exposed_coal>(o => o.organization_).Ignore<exposed_coal>(o => o.mine_location_);
            mapper.Put(dbContext.exposed_coal, "Exposed Coal", false);

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
            string sFileName = "MineLocation.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Mine Location");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Business Area Code");
                row.CreateCell(1).SetCellValue("Mine Location Code");
                row.CreateCell(2).SetCellValue("Mine Location Name");
                row.CreateCell(3).SetCellValue("Product");
                row.CreateCell(4).SetCellValue("Unit");
                row.CreateCell(5).SetCellValue("Opening Date");
                row.CreateCell(6).SetCellValue("Closing Date");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.mine_location.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var business_area_code = "";
                    var business_area = dbFind.business_area.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.business_area_id).FirstOrDefault();
                    if (business_area != null) business_area_code = business_area.business_area_code.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(business_area_code);
                    row.CreateCell(1).SetCellValue(Value.mine_location_code);
                    row.CreateCell(2).SetCellValue(Value.stock_location_name);
                    row.CreateCell(3).SetCellValue(product_code);
                    row.CreateCell(4).SetCellValue(uom_symbol);
                    row.CreateCell(5).SetCellValue(Value.opening_date.ToString());
                    row.CreateCell(6).SetCellValue(Value.closing_date.ToString());
                    RowCount++;
                }

                //***** detail
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Exposed Coal");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Mine Location Code");
                row.CreateCell(1).SetCellValue("Transaction Date");
                row.CreateCell(2).SetCellValue("Quantity");
                row.CreateCell(3).SetCellValue("Unit");
                row.CreateCell(4).SetCellValue("Survey");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail = dbContext.exposed_coal;
                // Inserting values to table
                foreach (var Value in detail)
                {
                    var mine_location_code = "";
                    var mine_location = dbFind.mine_location.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.mine_location_id).FirstOrDefault();
                    if (mine_location != null) mine_location_code = mine_location.mine_location_code.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    var survey_number = "";
                    var survey = dbFind.survey.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Value.survey_id).FirstOrDefault();
                    if (survey != null) survey_number = survey.survey_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(mine_location_code);
                    row.CreateCell(1).SetCellValue(Value.transaction_date.ToString("yyyy-MM-dd"));
                    row.CreateCell(2).SetCellValue(Convert.ToDouble(Value.quantity));
                    row.CreateCell(3).SetCellValue(uom_symbol);
                    row.CreateCell(4).SetCellValue(survey_number);
                    RowCount++;
                }
                //****************

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
