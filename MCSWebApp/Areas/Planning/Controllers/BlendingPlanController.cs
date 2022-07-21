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

namespace MCSWebApp.Areas.Planning.Controllers
{
    [Area("Planning")]
    public class BlendingPlanController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public BlendingPlanController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Planning];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BlendingPlan];
            ViewBag.BreadcrumbCode = WebAppMenu.BlendingPlan;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            string sFileName = "BlendingPlan.xlsx";

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Blending Plan");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Planning Category");
                row.CreateCell(2).SetCellValue("Product");
                row.CreateCell(3).SetCellValue("Plan Date");
                row.CreateCell(4).SetCellValue("Shift");
                row.CreateCell(5).SetCellValue("Despatch Order");
                row.CreateCell(6).SetCellValue("Destination Location");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var header = dbContext.blending_plan.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in header)
                {
                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == isi.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var shift_code = "";
                    var shift = dbFind.shift.Where(o => o.id == isi.source_shift_id).FirstOrDefault();
                    if (shift != null) shift_code = shift.shift_code.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == isi.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var destination_location_name = "";
                    //var stock_location = dbFind.stock_location.Where(o => o.id == isi.destination_location_id).FirstOrDefault();

                    var minelocation = dbFind.mine_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, location_name = o.mine_location_code });
                    var vessel = dbFind.vessel
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, location_name = o.vehicle_id });
                    var stockpileocation = dbFind.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        //.Select(o => new { id = o.id, location_name = o.business_area_name + " > " + o.stock_location_name });
                        .Select(o => new { id = o.id, location_name = o.stockpile_location_code });
                    var portlocation = dbFind.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, location_name = o.port_location_code });

                    var location = minelocation.Union(vessel).Union(stockpileocation).Union(portlocation)
                        .Where(o => o.id == isi.destination_location_id)
                        .FirstOrDefault();
                    if (location != null) destination_location_name = location.location_name.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(isi.transaction_number);
                    row.CreateCell(1).SetCellValue(isi.planning_category);
                    row.CreateCell(2).SetCellValue(product_code);
                    row.CreateCell(3).SetCellValue(" " + (isi.unloading_datetime != null ? Convert.ToDateTime(isi.unloading_datetime).ToString("yyyy-MM-dd") : ""));
                    row.CreateCell(4).SetCellValue(shift_code);
                    row.CreateCell(5).SetCellValue(despatch_order_number);
                    row.CreateCell(6).SetCellValue(destination_location_name);
                    RowCount++;
                }

                //***** detail
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Detail");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("SPEC TS");
                row.CreateCell(2).SetCellValue("Source Location");
                row.CreateCell(3).SetCellValue("Note");
                row.CreateCell(4).SetCellValue("Volume");
                row.CreateCell(5).SetCellValue("TM");
                row.CreateCell(6).SetCellValue("IM");
                row.CreateCell(7).SetCellValue("AC");
                row.CreateCell(8).SetCellValue("VM");
                row.CreateCell(9).SetCellValue("FC");
                row.CreateCell(10).SetCellValue("TS");
                row.CreateCell(11).SetCellValue("CV(adb)");
                row.CreateCell(12).SetCellValue("CV(ar)");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail = dbContext.blending_plan_source.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in detail)
                {
                    var transaction_number = "";
                    var blending_plan = dbFind.blending_plan.Where(o => o.id == isi.blending_plan_id).FirstOrDefault();
                    if (blending_plan != null) transaction_number = blending_plan.transaction_number.ToString();

                    var source_location_code = "";
                    //var stock_location = dbFind.stock_location.Where(o => o.id == isi.source_location_id).FirstOrDefault();
                    //if (stock_location != null) source_location_name = stock_location.stock_location_name.ToString();

                    var minelocation = dbFind.mine_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.mine_location_code });
                    var stockpileocation = dbFind.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.stockpile_location_code });
                    var portlocation = dbFind.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.port_location_code });

                    var location = minelocation.Union(stockpileocation).Union(portlocation)
                        .Where(o => o.id == isi.source_location_id)
                        .FirstOrDefault();
                    if (location != null) source_location_code = location.code.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(transaction_number);
                    row.CreateCell(1).SetCellValue(Convert.ToDouble(isi.spec_ts));
                    row.CreateCell(2).SetCellValue(source_location_code);
                    row.CreateCell(3).SetCellValue(isi.note);
                    row.CreateCell(4).SetCellValue(Convert.ToDouble(isi.volume));
                    row.CreateCell(5).SetCellValue(Convert.ToDouble(isi.analyte_1));
                    row.CreateCell(6).SetCellValue(Convert.ToDouble(isi.analyte_2));
                    row.CreateCell(7).SetCellValue(Convert.ToDouble(isi.analyte_3));
                    row.CreateCell(8).SetCellValue(Convert.ToDouble(isi.analyte_4));
                    row.CreateCell(9).SetCellValue(Convert.ToDouble(isi.analyte_5));
                    row.CreateCell(10).SetCellValue(Convert.ToDouble(isi.analyte_6));
                    row.CreateCell(11).SetCellValue(Convert.ToDouble(isi.analyte_7));
                    row.CreateCell(12).SetCellValue(Convert.ToDouble(isi.analyte_8));
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

                finally
                {
                    var path = Path.Combine(FilePath, sFileName);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
        }

    }
}
