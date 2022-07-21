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

namespace MCSWebApp.Areas.StockpileManagement.Controllers
{
    [Area("StockpileManagement")]
    public class QualitySamplingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public QualitySamplingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.StockpileManagement];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.QualitySampling];
            ViewBag.BreadcrumbCode = WebAppMenu.QualitySampling;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "QualitySampling.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("QualitySampling");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Sampling Number");
                row.CreateCell(1).SetCellValue("Sampling DateTime");
                row.CreateCell(2).SetCellValue("Surveyor");
                row.CreateCell(3).SetCellValue("Stockpile Location");
                row.CreateCell(4).SetCellValue("Product");
                row.CreateCell(5).SetCellValue("Sampling Template");
                row.CreateCell(6).SetCellValue("Despatch Order");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.quality_sampling.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var surveyor_code = "";
                    var contractor = dbFind.contractor.Where(o => o.id == Value.surveyor_id).FirstOrDefault();
                    if (contractor != null) surveyor_code = contractor.business_partner_code.ToString();

                    var stockpile_location_code = "";
                    //var stockpile_location = dbFind.stockpile_location.Where(o => o.id == Value.stock_location_id).FirstOrDefault();
                    //if (stockpile_location != null) stockpile_location_code = stockpile_location.stockpile_location_code.ToString();

                    var stockpile = dbFind.vw_stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.id == Value.stock_location_id)
                        .Select(o => new { id = o.id, code = o.stockpile_location_code });
                    var barges = dbFind.barge
                                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                    .Select(o => new { id = o.id, code = o.vehicle_id });
                    var vessels = dbFind.vessel
                                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                    .Select(o => new { id = o.id, code = o.vehicle_id });
                    var lookup = stockpile.Union(barges).Union(vessels)
                        .Where(o => o.id == Value.stock_location_id)
                        .FirstOrDefault();
                    if (lookup != null) stockpile_location_code = lookup.code.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == Value.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var sampling_template_code = "";
                    var sampling_template = dbFind.sampling_template.Where(o => o.id == Value.sampling_template_id).FirstOrDefault();
                    if (sampling_template != null) sampling_template_code = sampling_template.sampling_template_code.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == Value.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(" " + Value.sampling_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(Value.sampling_datetime).ToString("yyyy-MM-dd HH:mm"));
                    row.CreateCell(2).SetCellValue(surveyor_code);
                    row.CreateCell(3).SetCellValue(stockpile_location_code);
                    row.CreateCell(4).SetCellValue(product_code);
                    row.CreateCell(5).SetCellValue(sampling_template_code);
                    row.CreateCell(6).SetCellValue(despatch_order_number);
                    RowCount++;
                }

                ///**************************************** detail
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Detail");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Sampling Number");
                row.CreateCell(1).SetCellValue("Analyte");
                row.CreateCell(2).SetCellValue("Unit");
                row.CreateCell(3).SetCellValue("Value");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail = dbContext.quality_sampling_analyte;
                // Inserting values to table
                foreach (var isi in detail)
                {
                    var sampling_number = "";
                    var quality_sampling = dbFind.quality_sampling.Where(o => o.id == isi.quality_sampling_id).FirstOrDefault();
                    if (quality_sampling != null) sampling_number = quality_sampling.sampling_number.ToString();

                    var analyte_symbol = "";
                    var analyte = dbFind.analyte.Where(o => o.id == isi.analyte_id).FirstOrDefault();
                    if (analyte != null) analyte_symbol = analyte.analyte_symbol.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == isi.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(sampling_number);
                    row.CreateCell(1).SetCellValue(analyte_symbol);
                    row.CreateCell(2).SetCellValue(uom_symbol);
                    row.CreateCell(3).SetCellValue(Convert.ToDouble(isi.analyte_value));
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
