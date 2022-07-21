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

namespace MCSWebApp.Areas.Mining.Controllers
{
    [Area("Mining")]
    public class HaulingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public HaulingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Hauling];
            ViewBag.BreadcrumbCode = WebAppMenu.Hauling;

            return View();
        }

        public async Task<IActionResult> ExcelExport1()
        {
            string sFileName = "Hauling.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            FilePath = Path.Combine(FilePath, sFileName);

            var mapper = new Npoi.Mapper.Mapper();
            mapper.Ignore<hauling_transaction>(o => o.organization_);
            mapper.Put(dbContext.hauling_transaction, "Hauling", true);
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
            string sFileName = "Hauling.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Hauling");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("DateTime In");
                row.CreateCell(2).SetCellValue("DateTime Out");
                row.CreateCell(3).SetCellValue("Shift"); 
                row.CreateCell(4).SetCellValue("Process Flow"); 
                row.CreateCell(5).SetCellValue("Transport");
                row.CreateCell(6).SetCellValue("Source");
                row.CreateCell(7).SetCellValue("Destination");
                row.CreateCell(8).SetCellValue("Gross"); 
                row.CreateCell(9).SetCellValue("Tare");
                row.CreateCell(10).SetCellValue("Net Quantity");
                row.CreateCell(11).SetCellValue("Unit");
                row.CreateCell(12).SetCellValue("Product");// ("Destination Shift");
                row.CreateCell(13).SetCellValue("Distance (meter)");
                row.CreateCell(14).SetCellValue("Quality Sampling"); //("Quality Survey");
                row.CreateCell(15).SetCellValue("Despatch Order");
                row.CreateCell(16).SetCellValue("Contract Refference");
                row.CreateCell(17).SetCellValue("PIC"); //("Progress Claim");
                row.CreateCell(18).SetCellValue("Note");
               // row.CreateCell(19).SetCellValue

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.hauling_transaction.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    var process_flow_name = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == baris.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_name = process_flow.process_flow_name.ToString();

                    var transport_name = "";
                    var truck = dbFind.truck.Where(o => o.id == baris.transport_id).FirstOrDefault();
                    if (truck != null) transport_name = truck.vehicle_name.ToString();

                    //var accounting_period_name = "";
                    //var accounting_period = dbFind.accounting_period.Where(o => o.id == baris.accounting_period_id).FirstOrDefault();
                    //if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var source_location_name = "";
                    var source_location = dbFind.stock_location.Where(o => o.id == baris.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_name = source_location.stock_location_name.ToString();

                    var source_shift_name = "";
                    var shift = dbFind.shift.Where(o => o.id == baris.source_shift_id).FirstOrDefault();
                    if (shift != null) source_shift_name = shift.shift_name.ToString();

                    var product_name = "";
                    var product = dbFind.product.Where(o => o.id == baris.product_id).FirstOrDefault();
                    if (product != null) product_name = product.product_name.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == baris.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    var destination_location_name = "";
                    var destination_location = dbFind.vw_stock_location.Where(o => o.id == baris.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_name = destination_location.stock_location_name.ToString();

                    //var destination_shift_name = "";
                    //var destination_shift = dbFind.shift.Where(o => o.id == baris.destination_shift_id).FirstOrDefault();
                    //if (destination_shift != null) destination_shift_name = destination_shift.shift_name.ToString();

                    //var survey_number = "";
                   // var survey = dbFind.survey.Where(o => o.id == baris.survey_id).FirstOrDefault();
                    //if (survey != null) survey_number = survey.survey_number.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == baris.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var sampling_number = "";
                    var quality_sampling = dbFind.quality_sampling.Where(o => o.id == baris.quality_sampling_id).FirstOrDefault();
                    if (quality_sampling != null) sampling_number = quality_sampling.sampling_number.ToString();

                    //var progress_claim_name = "";
                    //var progress_claim = dbFind.progress_claim.Where(o => o.id == baris.progress_claim_id).FirstOrDefault();
                    //if (progress_claim != null) progress_claim_name = progress_claim.progress_claim_name.ToString();

                    var employee_number = "";
                    var employee = dbFind.employee.Where(o => o.id == baris.pic).FirstOrDefault();
                    if (employee != null) employee_number = employee.employee_number.ToString();

                    var advance_contract_number1 = "";
                    var advance_contract1 = dbFind.advance_contract.Where(o => o.id == baris.advance_contract_id).FirstOrDefault();
                    if (advance_contract1 != null) advance_contract_number1 = advance_contract1.advance_contract_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(baris.loading_datetime).ToString("yyyy-MM-dd HH:mm:ss")); 
                    row.CreateCell(2).SetCellValue(" " + Convert.ToDateTime(baris.unloading_datetime).ToString("yyyy-MM-dd HH:mm:ss"));
                    row.CreateCell(3).SetCellValue(source_shift_name);//(accounting_period_name);
                    row.CreateCell(4).SetCellValue(process_flow_name);
                    row.CreateCell(5).SetCellValue(transport_name);
                    row.CreateCell(6).SetCellValue(source_location_name);
                    row.CreateCell(7).SetCellValue(destination_location_name); 
                    row.CreateCell(8).SetCellValue(Convert.ToDouble(baris.gross));
                    row.CreateCell(9).SetCellValue(Convert.ToDouble(baris.tare)); 
                    row.CreateCell(10).SetCellValue(Convert.ToDouble(baris.loading_quantity));
                    row.CreateCell(11).SetCellValue(uom_symbol);
                    row.CreateCell(12).SetCellValue(product_name);//(destination_shift_name);
                    row.CreateCell(13).SetCellValue(Convert.ToDouble(baris.distance)); //(Convert.ToDouble(baris.unloading_quantity));
                    row.CreateCell(14).SetCellValue(sampling_number);//(survey_number);
                    row.CreateCell(15).SetCellValue(despatch_order_number);
                    row.CreateCell(16).SetCellValue(advance_contract_number1);
                    row.CreateCell(17).SetCellValue(employee_number);//(progress_claim_name);
                    row.CreateCell(18).SetCellValue(baris.note);
                   
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
