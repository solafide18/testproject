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
    public class RehandlingController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public RehandlingController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Rehandling];
            ViewBag.BreadcrumbCode = WebAppMenu.Rehandling;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "Rehandling.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Rehandling");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("DateTime");
                row.CreateCell(2).SetCellValue("Shift");
                row.CreateCell(3).SetCellValue("Process Flow"); //("Accounting Period");
                row.CreateCell(4).SetCellValue("Transport");
                row.CreateCell(5).SetCellValue("Equipment");
                row.CreateCell(6).SetCellValue("Source");
                row.CreateCell(7).SetCellValue("Destination");
                row.CreateCell(8).SetCellValue("Net Quantity");
                row.CreateCell(9).SetCellValue("Unit");
                row.CreateCell(10).SetCellValue("Product");
                row.CreateCell(11).SetCellValue("Distance");
                row.CreateCell(12).SetCellValue("Quality Sampling");
                row.CreateCell(13).SetCellValue("Despatch Order"); //("Destination Shift");
                row.CreateCell(14).SetCellValue("Contract Reference"); //("Unloading Qty");
                row.CreateCell(15).SetCellValue("PIC");// ("Quality Survey");
                row.CreateCell(16).SetCellValue("Note");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.rehandling_transaction.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var Value in tabledata)
                {
                    var process_flow_code = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == Value.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_code = process_flow.process_flow_code.ToString();

                    var truck_code = "";
                    var truck = dbFind.truck.Where(o => o.id == Value.transport_id).FirstOrDefault();
                    if (truck != null) truck_code = truck.vehicle_id.ToString();

                    /*var accounting_period_name = "";
                    var accounting_period = dbFind.accounting_period.Where(o => o.id == Value.accounting_period_id).FirstOrDefault();
                    if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();
                    */
                    var source_location_code = "";
                    var source_location = dbFind.stockpile_location.Where(o => o.id == Value.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_code = source_location.stockpile_location_code.ToString();

                    var source_shift_code = "";
                    var source_shift = dbFind.shift.Where(o => o.id == Value.source_shift_id).FirstOrDefault();
                    if (source_shift != null) source_shift_code = source_shift.shift_code.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == Value.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var unit_symbol = "";
                    var unit = dbFind.uom.Where(o => o.id == Value.uom_id).FirstOrDefault();
                    if (unit != null) unit_symbol = unit.uom_symbol.ToString();

                    var destination_location_code = "";
                    var destination_location = dbFind.stockpile_location.Where(o => o.id == Value.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_code = destination_location.stockpile_location_code.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == Value.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var equipment_code = "";
                    var equipment = dbFind.equipment.Where(o => o.id == Value.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_code = equipment.equipment_code.ToString();

                    var sampling_number = "";
                    var quality_sampling = dbFind.quality_sampling.Where(o => o.id == Value.quality_sampling_id).FirstOrDefault();
                    if (quality_sampling != null) sampling_number = quality_sampling.sampling_number.ToString();

                    var advance_contract_number1 = "";
                    var advance_contract1 = dbFind.advance_contract.Where(o => o.id == Value.advance_contract_id1).FirstOrDefault();
                    if (advance_contract1 != null) advance_contract_number1 = advance_contract1.advance_contract_number.ToString();

                    var employee_number = "";
                    var employee = dbFind.employee.Where(o => o.id == Value.pic).FirstOrDefault();
                    if (employee != null) employee_number = employee.employee_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(Value.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(Value.loading_datetime).ToString("yyyy-MM-dd HH:mm"));
                    row.CreateCell(2).SetCellValue(source_shift_code);
                    row.CreateCell(3).SetCellValue(process_flow_code);//(accounting_period_name);
                    row.CreateCell(4).SetCellValue(truck_code);
                    row.CreateCell(5).SetCellValue(equipment_code);
                    row.CreateCell(6).SetCellValue(source_location_code);
                    row.CreateCell(7).SetCellValue(destination_location_code);
                    row.CreateCell(8).SetCellValue(Convert.ToDouble(Value.loading_quantity));
                    row.CreateCell(9).SetCellValue(unit_symbol);
                    row.CreateCell(10).SetCellValue(product_code);
                    row.CreateCell(11).SetCellValue(Convert.ToDouble(Value.distance));
                    row.CreateCell(12).SetCellValue(sampling_number);
                    row.CreateCell(13).SetCellValue(despatch_order_number); //(destination_shift_name);
                    row.CreateCell(14).SetCellValue(advance_contract_number1);
                    row.CreateCell(15).SetCellValue(employee_number); //(survey_number);
                    row.CreateCell(16).SetCellValue(Value.note);

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
