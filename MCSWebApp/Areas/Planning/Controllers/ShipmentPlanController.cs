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
    public class ShipmentPlanController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ShipmentPlanController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Planning];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ShipmentPlan];
            ViewBag.BreadcrumbCode = WebAppMenu.ShipmentPlan;

            return View();
        }

        public IActionResult Detail()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Planning];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ShipmentPlan];
            ViewBag.BreadcrumbCode = WebAppMenu.ShipmentPlan;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);
            string sFileName = "ShipmentPlan.xlsx";

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("ShipmentPlan");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Contract Number");
                row.CreateCell(1).SetCellValue("Year");
                row.CreateCell(2).SetCellValue("Month");
                row.CreateCell(3).SetCellValue("Destination");
                row.CreateCell(4).SetCellValue("Customer");
                row.CreateCell(5).SetCellValue("Shipment Number");
                row.CreateCell(6).SetCellValue("Incoterm");
                row.CreateCell(7).SetCellValue("Vessel");
                row.CreateCell(8).SetCellValue("Laycan");
                row.CreateCell(9).SetCellValue("ETA");
                row.CreateCell(10).SetCellValue("Qty SP");
                row.CreateCell(11).SetCellValue("Remark");
                row.CreateCell(12).SetCellValue("Traffic Officer");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var header = dbContext.shipment_plan.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in header)
                {
                    var sales_contract_name = "";
                    var sales_contract = dbFind.sales_contract.Where(o => o.id == isi.sales_contract_id).FirstOrDefault();
                    if (sales_contract != null) sales_contract_name = sales_contract.sales_contract_name.ToString().Trim();

                    var business_partner_code = "";
                    var customer = dbFind.customer.Where(o => o.id == isi.customer_id).FirstOrDefault();
                    if (customer != null) business_partner_code = (customer.business_partner_code != null ? customer.business_partner_code.ToString().Trim() : "");

                    var vehicle_name = "";
                    var vessel = dbFind.vessel.Where(o => o.id == isi.transport_id).FirstOrDefault();
                    if (vessel != null)
                        vehicle_name = vessel.vehicle_name.ToString().Trim();
                    else
                    {
                        var barge = dbFind.barge.Where(o => o.id == isi.transport_id).FirstOrDefault();
                        if (barge != null) vehicle_name = barge.vehicle_name.ToString();
                        vehicle_name = barge.vehicle_name.ToString().Trim();
                    }

                    var employee_number = "";
                    var employee = dbFind.employee.Where(o => o.id == isi.traffic_officer_id).FirstOrDefault();
                    if (employee != null) employee_number = employee.employee_number.ToString().Trim();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(sales_contract_name);
                    row.CreateCell(1).SetCellValue(isi.shipment_year.ToString());
                    row.CreateCell(2).SetCellValue(isi.month_id.ToString());
                    row.CreateCell(3).SetCellValue(isi.destination);
                    row.CreateCell(4).SetCellValue(business_partner_code);
                    row.CreateCell(5).SetCellValue(isi.shipment_number);
                    row.CreateCell(6).SetCellValue(isi.incoterm);
                    row.CreateCell(7).SetCellValue(vehicle_name);
                    row.CreateCell(8).SetCellValue(isi.laycan);
                    row.CreateCell(9).SetCellValue(" " + Convert.ToDateTime(isi.eta).ToString("yyyy-MM-dd"));
                    row.CreateCell(10).SetCellValue(Convert.ToDouble(isi.qty_sp));
                    row.CreateCell(11).SetCellValue(isi.remarks);
                    row.CreateCell(12).SetCellValue(employee_number);

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
