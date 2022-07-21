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
    public class ProductionController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ProductionController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Production];
            ViewBag.BreadcrumbCode = WebAppMenu.Production;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "Production.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Production");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Date");
                row.CreateCell(2).SetCellValue("Shift");
                row.CreateCell(3).SetCellValue("Process Flow");
                row.CreateCell(4).SetCellValue("Transport");
                row.CreateCell(5).SetCellValue("Tare");
                row.CreateCell(6).SetCellValue("Source Location");
                row.CreateCell(7).SetCellValue("Destination Location");
                row.CreateCell(8).SetCellValue("Quantity");
                row.CreateCell(9).SetCellValue("Unit");
                row.CreateCell(10).SetCellValue("Product");
                row.CreateCell(11).SetCellValue("Distance");
                row.CreateCell(12).SetCellValue("Elevation");
                row.CreateCell(13).SetCellValue("Quality Sampling");
                row.CreateCell(14).SetCellValue("Equipment");
                row.CreateCell(15).SetCellValue("Despatch Order");
                row.CreateCell(16).SetCellValue("Contract Reference");
                row.CreateCell(17).SetCellValue("PIC");
                row.CreateCell(18).SetCellValue("Note");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.production_transaction.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    var process_flow_code = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == baris.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_code = process_flow.process_flow_code.ToString();

                    //var accounting_period_name = "";
                    //var accounting_period = dbFind.accounting_period.Where(o => o.id == baris.accounting_period_id).FirstOrDefault();
                    //if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var shift_code = "";
                    var shift = dbFind.shift.Where(o => o.id == baris.source_shift_id).FirstOrDefault();
                    if (shift != null) shift_code = shift.shift_code.ToString();

                    var source_location_code = "";
                    var source_location = dbFind.mine_location.Where(o => o.id == baris.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_code = source_location.mine_location_code.ToString();

                    var destination_location_code = "";
                    var destination_location = dbFind.stockpile_location.Where(o => o.id == baris.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_code = destination_location.stockpile_location_code.ToString();

                    var product_code = "";
                    var product = dbFind.product.Where(o => o.id == baris.product_id).FirstOrDefault();
                    if (product != null) product_code = product.product_code.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == baris.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();
                    
                    var vehicle_id = "";
                    var truck = dbFind.truck.Where(o => o.id == baris.transport_id).FirstOrDefault();
                    if (truck != null) vehicle_id = truck.vehicle_id.ToString();

                    var equipment_code = "";
                    var equipment = dbFind.equipment.Where(o => o.id == baris.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_code = equipment.equipment_code.ToString();

                    //var survey_number = "";
                    //var survey = dbFind.survey.Where(o => o.id == baris.survey_id).FirstOrDefault();
                    //if (survey != null) survey_number = survey.survey_number.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == baris.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var quality_sampling_number = "";
                    var quality_sampling = dbFind.quality_sampling.Where(o => o.id == baris.quality_sampling_id).FirstOrDefault();
                    if (quality_sampling != null) quality_sampling_number = quality_sampling.sampling_number.ToString();

                    var advance_contract_number1 = "";
                    var advance_contract1 = dbFind.advance_contract.Where(o => o.id == baris.advance_contract_id1).FirstOrDefault();
                    if (advance_contract1 != null) advance_contract_number1 = advance_contract1.advance_contract_number.ToString();

                    //var advance_contract_number2 = "";
                    //var advance_contract2 = dbFind.advance_contract.Where(o => o.id == baris.advance_contract_id2).FirstOrDefault();
                    //if (advance_contract2 != null) advance_contract_number2 = advance_contract2.advance_contract_number.ToString();

                    var employee_number = "";
                    var employee = dbFind.employee.Where(o => o.id == baris.pic).FirstOrDefault();
                    if (employee != null) employee_number = employee.employee_number.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(baris.unloading_datetime).ToString("yyyy-MM-dd HH:mm:ss"));
                    row.CreateCell(2).SetCellValue(shift_code);
                    row.CreateCell(3).SetCellValue(process_flow_code);
                    row.CreateCell(4).SetCellValue(vehicle_id);
                    row.CreateCell(5).SetCellValue(Convert.ToDouble(baris.tare));
                    row.CreateCell(6).SetCellValue(source_location_code);
                    row.CreateCell(7).SetCellValue(destination_location_code);
                    row.CreateCell(8).SetCellValue(Convert.ToDouble(baris.unloading_quantity));
                    row.CreateCell(9).SetCellValue(uom_symbol);
                    row.CreateCell(10).SetCellValue(product_code);
                    row.CreateCell(11).SetCellValue(Convert.ToDouble(baris.distance));
                    row.CreateCell(12).SetCellValue(Convert.ToDouble(baris.elevation));
                    row.CreateCell(13).SetCellValue(quality_sampling_number);
                    row.CreateCell(14).SetCellValue(equipment_code);
                    row.CreateCell(15).SetCellValue(despatch_order_number);
                    row.CreateCell(16).SetCellValue(advance_contract_number1);
                    row.CreateCell(17).SetCellValue(employee_number);
                    row.CreateCell(18).SetCellValue(baris.note);

                    RowCount++;
                    if (RowCount > 99) break;
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
