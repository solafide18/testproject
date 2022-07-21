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
    public class WasteRemovalController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public WasteRemovalController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Mining];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.WasteRemoval];
            ViewBag.BreadcrumbCode = WebAppMenu.WasteRemoval;

            return View();
        }

        public async Task<IActionResult> ExcelExport1()
        {
            string sFileName = "WasteRemoval.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            FilePath = Path.Combine(FilePath, sFileName);

            var mapper = new Npoi.Mapper.Mapper();
            mapper.Ignore<waste_removal>(o => o.organization_);
            mapper.Put(dbContext.waste_removal, "WasteRemoval", true);
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
            string sFileName = "WasteRemoval.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("WasteRemoval");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Transaction Number");
                row.CreateCell(1).SetCellValue("Date Time");
                row.CreateCell(2).SetCellValue("Accounting Period");
                row.CreateCell(3).SetCellValue("Process Flow");
                row.CreateCell(4).SetCellValue("Shift");
                row.CreateCell(5).SetCellValue("Source Location");
                row.CreateCell(6).SetCellValue("Destination Location");
                row.CreateCell(7).SetCellValue("Waste");
                row.CreateCell(8).SetCellValue("Quantity");
                row.CreateCell(9).SetCellValue("Unit");
                row.CreateCell(10).SetCellValue("Transport");
                row.CreateCell(11).SetCellValue("Equipment");
                row.CreateCell(12).SetCellValue("Distance");
                row.CreateCell(13).SetCellValue("Elevation");
                row.CreateCell(14).SetCellValue("Despatch Order");
                row.CreateCell(15).SetCellValue("Progress Claim");
                row.CreateCell(16).SetCellValue("Note");
                row.CreateCell(17).SetCellValue("PIC");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.waste_removal.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    var process_flow_name = "";
                    var process_flow = dbFind.process_flow.Where(o => o.id == baris.process_flow_id).FirstOrDefault();
                    if (process_flow != null) process_flow_name = process_flow.process_flow_name.ToString();

                    var accounting_period_name = "";
                    var accounting_period = dbFind.accounting_period.Where(o => o.id == baris.accounting_period_id).FirstOrDefault();
                    if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var shift_name = "";
                    var shift = dbFind.shift.Where(o => o.id == baris.source_shift_id).FirstOrDefault();
                    if (shift != null) shift_name = shift.shift_name.ToString();

                    var source_location_code = "";
                    var source_location = dbFind.mine_location.Where(o => o.id == baris.source_location_id).FirstOrDefault();
                    if (source_location != null) source_location_code = source_location.mine_location_code.ToString();

                    var destination_location_code = "";
                    var destination_location = dbFind.waste_location.Where(o => o.id == baris.destination_location_id).FirstOrDefault();
                    if (destination_location != null) destination_location_code = destination_location.waste_location_code.ToString();

                    var waste_name = "";
                    var waste = dbFind.waste.Where(o => o.id == baris.waste_id).FirstOrDefault();
                    if (waste != null) waste_name = waste.waste_name.ToString();

                    var uom_symbol = "";
                    var uom = dbFind.uom.Where(o => o.id == baris.uom_id).FirstOrDefault();
                    if (uom != null) uom_symbol = uom.uom_symbol.ToString();

                    var transport_name = "";
                    var truck = dbFind.truck.Where(o => o.id == baris.transport_id).FirstOrDefault();
                    if (truck != null) transport_name = truck.vehicle_name.ToString();

                    var equipment_name = "";
                    var equipment = dbFind.equipment.Where(o => o.id == baris.equipment_id).FirstOrDefault();
                    if (equipment != null) equipment_name = equipment.equipment_name.ToString();

                    var despatch_order_number = "";
                    var despatch_order = dbFind.despatch_order.Where(o => o.id == baris.despatch_order_id).FirstOrDefault();
                    if (despatch_order != null) despatch_order_number = despatch_order.despatch_order_number.ToString();

                    var progress_claim_name = "";
                    var progress_claim = dbFind.progress_claim.Where(o => o.id == baris.progress_claim_id).FirstOrDefault();
                    if (progress_claim != null) progress_claim_name = progress_claim.progress_claim_name.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.transaction_number);
                    row.CreateCell(1).SetCellValue(" " + Convert.ToDateTime(baris.unloading_datetime).ToString("yyyy-MM-dd HH:mm:ss"));
                    row.CreateCell(2).SetCellValue(accounting_period_name);
                    row.CreateCell(3).SetCellValue(process_flow_name);
                    row.CreateCell(4).SetCellValue(shift_name);
                    row.CreateCell(5).SetCellValue(source_location_code);
                    row.CreateCell(6).SetCellValue(destination_location_code);
                    row.CreateCell(7).SetCellValue(waste_name);
                    row.CreateCell(8).SetCellValue(Convert.ToDouble(baris.unloading_quantity));
                    row.CreateCell(9).SetCellValue(uom_symbol);
                    row.CreateCell(10).SetCellValue(transport_name);
                    row.CreateCell(11).SetCellValue(equipment_name);
                    row.CreateCell(12).SetCellValue(Convert.ToDouble(baris.distance));
                    row.CreateCell(13).SetCellValue(Convert.ToDouble(baris.elevation));
                    row.CreateCell(14).SetCellValue(despatch_order_number);
                    row.CreateCell(15).SetCellValue(progress_claim_name);
                    row.CreateCell(16).SetCellValue(baris.note);
                    row.CreateCell(17).SetCellValue(baris.pic);
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
