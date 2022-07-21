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
    public class ProcessingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ProcessingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Processing];
            ViewBag.BreadcrumbCode = WebAppMenu.Processing;

            return View();
        }

        public async Task<IActionResult> ExcelExport1()
        {
            string sFileName = "Processing.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            FilePath = Path.Combine(FilePath, sFileName);

            var mapper = new Npoi.Mapper.Mapper();
            mapper.Ignore<processing_transaction>(o => o.organization_);
            mapper.Put(dbContext.processing_transaction, "Processing", true);
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
            string sFileName = "Processing.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Processing");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Date");
                row.CreateCell(2).SetCellValue("Shift"); //("Processing Category");
                row.CreateCell(3).SetCellValue("Process Flow"); //("Transport");
                row.CreateCell(4).SetCellValue("Equipment"); //("Accounting Period");
                row.CreateCell(5).SetCellValue("Source");
                row.CreateCell(6).SetCellValue("Destination");
                row.CreateCell(7).SetCellValue("Quantity");
                row.CreateCell(8).SetCellValue("Unit");
                row.CreateCell(9).SetCellValue("Product");
                row.CreateCell(10).SetCellValue("Quality Sampling");
                row.CreateCell(11).SetCellValue("Despatch Order");//("Unloading Date Time");
                row.CreateCell(12).SetCellValue("Contract Reference");
                row.CreateCell(13).SetCellValue("PIC");  //("Destination Shift");
                row.CreateCell(14).SetCellValue("Note");//("Destination Product");
                //row.CreateCell(15).SetCellValue("Unloading Qty");
                //row.CreateCell(16).SetCellValue("Destination Unit");
                //row.CreateCell(17).SetCellValue("Quality Survey");
                //row.CreateCell(18).SetCellValue
                //row.CreateCell(19).SetCellValue
                //row.CreateCell(20).SetCellValue("Progress Claim");
                //row.CreateCell(21).SetCellValue
                //row.CreateCell(22).SetCellValue
                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.processing_transaction.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var process_flow_name = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == Value.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_name = process_flow.process_flow_name.ToString();

                    var equipment_code = "";
                    var equipment = dbFind.equipment.Where(o => o.id == Value.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_code = equipment.equipment_code.ToString();

                    /*var processing_category_name = "";
                    var processing_category = dbFind.processing_category.Where(o => o.id == Value.processing_category_id).FirstOrDefault();
                    if (processing_category != null) processing_category_name = processing_category.processing_category_name.ToString();

                    var transport_name = "";
                    var truck = dbFind.transport.Where(o => o.id == Value.transport_id).FirstOrDefault();
                    if (truck != null) transport_name = truck.vehicle_name.ToString();

                    var accounting_period_name = "";
                    var accounting_period = dbFind.accounting_period.Where(o => o.id == Value.accounting_period_id).FirstOrDefault();
                    if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();*/

                    var advance_contract_number1 = "";
                    var advance_contract1 = dbFind.advance_contract.Where(o => o.id == Value.advance_contract_id1).FirstOrDefault();
                    if (advance_contract1 != null) advance_contract_number1 = advance_contract1.advance_contract_number.ToString();

                    var source_location_code = "";
                    var source_location = dbFind.stockpile_location.Where(o => o.id == Value.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_code = source_location.stockpile_location_code.ToString();

                    var source_shift_name = "";
                    var source_shift = dbFind.shift.Where(o => o.id == Value.source_shift_id).FirstOrDefault();
                    if (source_shift != null) source_shift_name = source_shift.shift_name.ToString();

                    var source_product_name = "";
                    var source_product = dbFind.product.Where(o => o.id == Value.source_product_id).FirstOrDefault();
                    if (source_product != null) source_product_name = source_product.product_name.ToString();

                    var source_unit_symbol = "";
                    var source_unit = dbFind.uom.Where(o => o.id == Value.source_uom_id).FirstOrDefault();
                    if (source_unit != null) source_unit_symbol = source_unit.uom_symbol.ToString();

                    var destination_location_code = "";
                    var destination_location = dbFind.stockpile_location.Where(o => o.id == Value.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_code = destination_location.stockpile_location_code.ToString();

                   /* var destination_shift_name = "";
                    var destination_shift = dbFind.shift.Where(o => o.id == Value.destination_shift_id).FirstOrDefault();
                    if (destination_shift != null) destination_shift_name = destination_shift.shift_name.ToString();

                    var destination_unit_symbol = "";
                    var destination_unit = dbFind.uom.Where(o => o.id == Value.destination_uom_id).FirstOrDefault();
                    if (source_unit != null) destination_unit_symbol = destination_unit.uom_symbol.ToString();

                    var survey_number = "";
                    var survey = dbFind.survey.Where(o => o.id == Value.survey_id).FirstOrDefault();
                    if (survey != null) survey_number = survey.survey_number.ToString();

                    var destination_product_name = "";
                    var destination_product = dbFind.product.Where(o => o.id == Value.destination_product_id).FirstOrDefault();
                    if (destination_product != null) destination_product_name = destination_product.product_name.ToString();
                   */
                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == Value.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var sampling_number = "";
                    var quality_sampling = dbFind.quality_sampling.Where(o => o.id == Value.quality_sampling_id).FirstOrDefault();
                    if (quality_sampling != null) sampling_number = quality_sampling.sampling_number.ToString();

                    var employee_number = "";
                    var employee = dbFind.employee.Where(o => o.id == Value.pic).FirstOrDefault();
                    if (employee != null) employee_number = employee.employee_number.ToString();
                    /* var progress_claim_name = "";
                     var progress_claim = dbFind.progress_claim.Where(o => o.id == Value.progress_claim_id).FirstOrDefault();
                     if (progress_claim != null) progress_claim_name = progress_claim.progress_claim_name.ToString();*/

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(Value.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(Value.loading_datetime).ToString("yyyy-MM-dd HH:mm:ss"));
                    row.CreateCell(2).SetCellValue(source_shift_name);//(processing_category_name);
                    row.CreateCell(3).SetCellValue(process_flow_name);//(transport_name);
                    row.CreateCell(4).SetCellValue(equipment_code);//(accounting_period_name);
                    row.CreateCell(5).SetCellValue(source_location_code);
                    row.CreateCell(6).SetCellValue(destination_location_code);
                    row.CreateCell(7).SetCellValue(Convert.ToDouble(Value.loading_quantity));
                    row.CreateCell(8).SetCellValue(source_unit_symbol);
                    row.CreateCell(9).SetCellValue(source_product_name);
                    row.CreateCell(10).SetCellValue(sampling_number);
                    row.CreateCell(11).SetCellValue(despatch_order_number);//(" " + Convert.ToDateTime(Value.unloading_datetime).ToString("yyyy-MM-dd HH:mm:ss"));
                    row.CreateCell(12).SetCellValue(advance_contract_number1);
                    row.CreateCell(13).SetCellValue(employee_number);//(destination_shift_name);
                    row.CreateCell(14).SetCellValue (Value.note);//(destination_product_name);
                   // row.CreateCell(15).SetCellValue//(Convert.ToDouble(Value.unloading_quantity));
                   //row.CreateCell(16).SetCellValue//(destination_unit_symbol);
                   // row.CreateCell(17).SetCellValue//(survey_number);
                   // row.CreateCell(18).SetCellValue
                    //row.CreateCell(19).SetCellValue
                    //row.CreateCell(20).SetCellValue//(progress_claim_name);
                    //row.CreateCell(21).SetCellValue
                    //row.CreateCell(22).SetCellValue
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
