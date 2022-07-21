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

namespace MCSWebApp.Areas.Transport
{
    [Area("Transport")]
    public class TruckController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public TruckController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.MasterData];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Transport];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Truck];
            ViewBag.BreadcrumbCode = WebAppMenu.Truck;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            string sFileName = "Truck.xlsx";

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Truck");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Truck Name");
                row.CreateCell(1).SetCellValue("Truck Id");
                row.CreateCell(2).SetCellValue("Capacity");
                row.CreateCell(3).SetCellValue("Capacity Unit");
                row.CreateCell(4).SetCellValue("Owner");
                row.CreateCell(5).SetCellValue("Make");
                row.CreateCell(6).SetCellValue("Model");
                row.CreateCell(7).SetCellValue("Model Year");
                row.CreateCell(8).SetCellValue("Manufactured Year");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var header = dbContext.truck.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in header)
                {
                    var capacity_unit = "";
                    var uom = dbFind.uom.Where(o => o.id == isi.capacity_uom_id).FirstOrDefault();
                    if (uom != null) capacity_unit = uom.uom_symbol.ToString();

                    var owner = "";
                    var business_partner = dbFind.contractor.Where(o => o.id == isi.vendor_id).FirstOrDefault();
                    if (business_partner != null) owner = business_partner.business_partner_name.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(isi.vehicle_name);
                    row.CreateCell(1).SetCellValue(isi.vehicle_id);
                    row.CreateCell(2).SetCellValue(Convert.ToDouble(isi.capacity));
                    row.CreateCell(3).SetCellValue(capacity_unit);
                    row.CreateCell(4).SetCellValue(owner);
                    row.CreateCell(5).SetCellValue(isi.vehicle_make);
                    row.CreateCell(6).SetCellValue(isi.vehicle_model);
                    row.CreateCell(7).SetCellValue(Convert.ToInt32(isi.vehicle_model_year));
                    row.CreateCell(8).SetCellValue(Convert.ToInt32(isi.vehicle_manufactured_year));
                    RowCount++;
                }

                //***** detail
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Truck Cost Rate");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Truck Name");
                row.CreateCell(1).SetCellValue("Accounting Period");
                row.CreateCell(2).SetCellValue("Currency");
                row.CreateCell(3).SetCellValue("Hourly Rate");
                row.CreateCell(4).SetCellValue("Trip Rate");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail = dbContext.truck_cost_rate.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in detail)
                {
                    var vehicle_name = "";
                    var truck = dbFind.truck.Where(o => o.id == isi.truck_id).FirstOrDefault();
                    if (truck != null) vehicle_name = truck.vehicle_name.ToString();

                    var accounting_period_name = "";
                    var accounting_period = dbFind.accounting_period.Where(o => o.id == isi.accounting_period_id).FirstOrDefault();
                    if (accounting_period != null) accounting_period_name = accounting_period.accounting_period_name.ToString();

                    var currency_code = "";
                    var currency = dbFind.currency.Where(o => o.id == isi.currency_id).FirstOrDefault();
                    if (currency != null) currency_code = currency.currency_code.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(vehicle_name);
                    row.CreateCell(1).SetCellValue(accounting_period_name);
                    row.CreateCell(2).SetCellValue(currency_code);
                    row.CreateCell(3).SetCellValue(Convert.ToDouble(isi.hourly_rate));
                    row.CreateCell(4).SetCellValue(Convert.ToDouble(isi.trip_rate));
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
