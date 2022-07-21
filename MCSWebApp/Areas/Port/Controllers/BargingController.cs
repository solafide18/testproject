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

namespace MCSWebApp.Areas.Port.Controllers
{
    [Area("Port")]
    public class BargingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public BargingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Port];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.PortBarging];
            ViewBag.BreadcrumbCode = WebAppMenu.Barging;

            return View();
        }

        public IActionResult Loading()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Port];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.PortBarging];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BargeLoading];
            ViewBag.BreadcrumbCode = WebAppMenu.BargeLoading;

            return View();
        }

        public IActionResult Unloading()
        {
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Port];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.PortBarging];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BargeUnloading];
            ViewBag.BreadcrumbCode = WebAppMenu.BargeUnloading;

            return View();
        }

        public async Task<IActionResult> LoadingExcelExport()
        {
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            string sFileName = "BargeLoading.xlsx";

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("BargeLoading");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Despatch Order"); 
                row.CreateCell(2).SetCellValue("Quality Survey");
                row.CreateCell(3).SetCellValue("Process Flow");
                row.CreateCell(4).SetCellValue("Source Location");
                row.CreateCell(5).SetCellValue("Barge");
                row.CreateCell(6).SetCellValue("Reference Number");
                row.CreateCell(7).SetCellValue("Initial Draft Survey");
                row.CreateCell(8).SetCellValue("Final Draft Survey");
                row.CreateCell(9).SetCellValue("Quantity");
                row.CreateCell(10).SetCellValue("Unit"); 
                row.CreateCell(11).SetCellValue("Product");
                row.CreateCell(12).SetCellValue("Equipment");
                row.CreateCell(13).SetCellValue("Hour Usage");
                row.CreateCell(14).SetCellValue("Note");
                row.CreateCell(15).SetCellValue("Arrival DateTime");
                row.CreateCell(16).SetCellValue("Alongside DateTime");
                row.CreateCell(17).SetCellValue("Commenced Loading DateTime");
                row.CreateCell(18).SetCellValue("Completed Loading DateTime");
                row.CreateCell(19).SetCellValue("Cast Off DateTime");
                row.CreateCell(20).SetCellValue("Departure DateTime");
                row.CreateCell(21).SetCellValue("Distance");
                row.CreateCell(22).SetCellValue("Ref. Work Order");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var header = dbContext.barging_transaction.Where(o => o.is_loading == true);
                // Inserting values to table
                foreach (var baris in header)
                {
                    var process_flow_code = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == baris.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_code = process_flow.process_flow_code.ToString();

                    var source_location_code = "";
                    var source_location = dbFind.port_location.Where(o => o.id == baris.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_code = source_location.port_location_code.ToString();

                    var destination_location_name = "";
                    var destination_location = dbFind.barge.Where(o => o.id == baris.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_name = destination_location.vehicle_name.ToString();

                    //var shift_name = "";
                    //var shift = dbFind.shift.Where(o => o.id == baris.source_shift_id).FirstOrDefault();
                    //if (shift != null) shift_name = shift.shift_name.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == baris.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == baris.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    var equipment_code = "";
                    var equipment = dbFind.equipment.Where(o => o.id == baris.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_code = equipment.equipment_code.ToString();

                    var survey_number = "";
                    var survey = dbFind.survey.Where(o => o.id == baris.survey_id && o.is_draft_survey == true).FirstOrDefault();
                    if (survey != null) survey_number = survey.survey_number.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == baris.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(despatch_order_number);
                    row.CreateCell(2).SetCellValue(survey_number);
                    row.CreateCell(3).SetCellValue(process_flow_code); 
                    row.CreateCell(4).SetCellValue(source_location_code);
                    row.CreateCell(5).SetCellValue(destination_location_name);
                    row.CreateCell(6).SetCellValue(baris.reference_number); 
                    row.CreateCell(7).SetCellValue(" " + (baris.initial_draft_survey != null ? Convert.ToDateTime(baris.initial_draft_survey).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(8).SetCellValue(" " + (baris.final_draft_survey != null ? Convert.ToDateTime(baris.final_draft_survey).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(9).SetCellValue(Convert.ToDouble(baris.quantity));
                    row.CreateCell(10).SetCellValue(uom_symbol);
                    row.CreateCell(11).SetCellValue(product_code);
                    row.CreateCell(12).SetCellValue(equipment_code);
                    row.CreateCell(13).SetCellValue(Convert.ToDouble(baris.hour_usage));
                    row.CreateCell(14).SetCellValue(baris.note);
                    row.CreateCell(15).SetCellValue(" " + (baris.arrival_datetime != null ? Convert.ToDateTime(baris.arrival_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(16).SetCellValue(" " + (baris.berth_datetime != null ? Convert.ToDateTime(baris.berth_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(17).SetCellValue(" " + (baris.start_datetime != null ? Convert.ToDateTime(baris.start_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(18).SetCellValue(" " + (baris.end_datetime != null ? Convert.ToDateTime(baris.end_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(19).SetCellValue(" " + (baris.unberth_datetime != null ? Convert.ToDateTime(baris.unberth_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(20).SetCellValue(" " + (baris.departure_datetime != null ? Convert.ToDateTime(baris.departure_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(21).SetCellValue(Convert.ToDouble(baris.distance));
                    row.CreateCell(22).SetCellValue(baris.ref_work_order);

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

                finally
                {
                    var path = Path.Combine(FilePath, sFileName);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
        }

        public async Task<IActionResult> UnloadingExcelExport()
        {
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            string sFileName = "BargeUnloading.xlsx";

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("BargeUnloading");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Despatch Order");
                row.CreateCell(2).SetCellValue("Quality Survey");
                row.CreateCell(3).SetCellValue("Process Flow");
                row.CreateCell(4).SetCellValue("Barge"); 
                row.CreateCell(5).SetCellValue("Destination Location");
                row.CreateCell(6).SetCellValue("Reference Number");
                row.CreateCell(7).SetCellValue("Initial Draft Survey");
                row.CreateCell(8).SetCellValue("Final Draft Survey");
                row.CreateCell(9).SetCellValue("Quantity");
                row.CreateCell(10).SetCellValue("Unit");
                row.CreateCell(11).SetCellValue("Product");
                row.CreateCell(12).SetCellValue("Equipment");
                row.CreateCell(13).SetCellValue("Hour Usage");
                row.CreateCell(14).SetCellValue("Note");
                row.CreateCell(15).SetCellValue("Arrival DateTime");
                row.CreateCell(16).SetCellValue("Alongside DateTime");
                row.CreateCell(17).SetCellValue("Commenced Unloading DateTime");
                row.CreateCell(18).SetCellValue("Completed Unloading DateTime");
                row.CreateCell(19).SetCellValue("Cast Off DateTime");
                row.CreateCell(20).SetCellValue("Departure DateTime");
                row.CreateCell(21).SetCellValue("Distance");
                row.CreateCell(22).SetCellValue("Ref. Work Order");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var header = dbContext.barging_transaction.Where(o => o.is_loading == false);
                // Inserting values to table
                foreach (var baris in header)
                {
                    var process_flow_code = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == baris.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_code = process_flow.process_flow_code.ToString();

                    //var accounting_period_name = "";
                    //var accounting_period = dbFind.accounting_period.Where(o => o.id == baris.accounting_period_id).FirstOrDefault();
                    //if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var source_location_name = "";
                    var source_location = dbFind.barge.Where(o => o.id == baris.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_name = source_location.vehicle_name.ToString();

                    var destination_location_code = "";
                    var destination_location = dbFind.port_location.Where(o => o.id == baris.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_code = destination_location.port_location_code.ToString();

                    //var shift_name = "";
                    //var shift = dbFind.shift.Where(o => o.id == baris.source_shift_id).FirstOrDefault();
                    //if (shift != null) shift_name = shift.shift_name.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == baris.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == baris.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    var equipment_code = "";
                    var equipment = dbFind.equipment.Where(o => o.id == baris.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_code = equipment.equipment_code.ToString();

                    var survey_number = "";
                    var survey = dbFind.survey.Where(o => o.id == baris.survey_id && o.is_draft_survey == true).FirstOrDefault();
                    if (survey != null) survey_number = survey.survey_number.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == baris.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(despatch_order_number);
                    row.CreateCell(2).SetCellValue(survey_number);
                    row.CreateCell(3).SetCellValue(process_flow_code);
                    row.CreateCell(4).SetCellValue(source_location_name);
                    row.CreateCell(5).SetCellValue(destination_location_code);
                    row.CreateCell(6).SetCellValue(baris.reference_number);
                    row.CreateCell(7).SetCellValue(" " + (baris.initial_draft_survey != null ? Convert.ToDateTime(baris.initial_draft_survey).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(8).SetCellValue(" " + (baris.final_draft_survey != null ? Convert.ToDateTime(baris.final_draft_survey).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(9).SetCellValue(Convert.ToDouble(baris.quantity));
                    row.CreateCell(10).SetCellValue(uom_symbol);
                    row.CreateCell(11).SetCellValue(product_code);
                    row.CreateCell(12).SetCellValue(equipment_code);
                    row.CreateCell(13).SetCellValue(Convert.ToDouble(baris.hour_usage));
                    row.CreateCell(14).SetCellValue(baris.note);
                    row.CreateCell(15).SetCellValue(" " + (baris.arrival_datetime != null ? Convert.ToDateTime(baris.arrival_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(16).SetCellValue(" " + (baris.berth_datetime != null ? Convert.ToDateTime(baris.berth_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(17).SetCellValue(" " + (baris.start_datetime != null ? Convert.ToDateTime(baris.start_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(18).SetCellValue(" " + (baris.end_datetime != null ? Convert.ToDateTime(baris.end_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(19).SetCellValue(" " + (baris.unberth_datetime != null ? Convert.ToDateTime(baris.unberth_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(20).SetCellValue(" " + (baris.departure_datetime != null ? Convert.ToDateTime(baris.departure_datetime).ToString("yyyy-MM-dd HH:mm") : ""));
                    row.CreateCell(21).SetCellValue(Convert.ToDouble(baris.distance));
                    row.CreateCell(22).SetCellValue(baris.ref_work_order);

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

                finally
                {
                    var path = Path.Combine(FilePath, sFileName);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
        }

    }
}
