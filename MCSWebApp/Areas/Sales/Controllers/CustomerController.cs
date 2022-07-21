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

namespace MCSWebApp.Areas.Sales.Controllers
{
    [Area("Sales")]
    public class CustomerController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public CustomerController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.SalesMarketing];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Customer];
            ViewBag.BreadcrumbCode = WebAppMenu.Customer;

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "Customer.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
            {
                int RowCount = 1;
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("Customer");
                IRow row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Customer Name");
                row.CreateCell(1).SetCellValue("Customer Code");
                row.CreateCell(2).SetCellValue("Customer Type");
                row.CreateCell(3).SetCellValue("Primary Address");
                row.CreateCell(4).SetCellValue("Country");
                row.CreateCell(5).SetCellValue("Primary Contact Name");
                row.CreateCell(6).SetCellValue("Primary Contact Email");
                row.CreateCell(7).SetCellValue("Primary Contact Phone");
                row.CreateCell(8).SetCellValue("Secondary Contact Name");
                row.CreateCell(9).SetCellValue("Secondary Contact Email");
                row.CreateCell(10).SetCellValue("Secondary Contact Phone");
                row.CreateCell(11).SetCellValue("Additional Information");
                row.CreateCell(12).SetCellValue("Bank");
                row.CreateCell(13).SetCellValue("Bank Account");
                row.CreateCell(14).SetCellValue("Currency");
                row.CreateCell(15).SetCellValue("Taxable");
                row.CreateCell(2).SetCellValue("Is Active");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                var tabledata = dbContext.customer.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var baris in tabledata)
                {
                    var customer_type_name = "";
                    var customer_type = dbFind.customer_type.Where(o => o.id == baris.customer_type_id).FirstOrDefault();
                    if (customer_type != null) customer_type_name = customer_type.customer_type_name.ToString();

                    var country_code = "";
                    var country = dbFind.country.Where(o => o.id == baris.country_id).FirstOrDefault();
                    if (country != null) country_code = country.country_code.ToString();

                    var bank_code = ""; var account_number = ""; var currency_code = "";
                    var bank_account = dbFind.vw_bank_account.Where(o => o.id == baris.bank_account_id).FirstOrDefault();
                    if (bank_account != null)
                    {
                        account_number = bank_account.account_number.ToString();
                        bank_code = bank_account.bank_code.ToString();
                        currency_code = bank_account.currency_code.ToString();
                    }

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(baris.business_partner_name);
                    row.CreateCell(1).SetCellValue(baris.business_partner_code);
                    row.CreateCell(2).SetCellValue(customer_type_name);
                    row.CreateCell(3).SetCellValue(baris.primary_address);
                    row.CreateCell(4).SetCellValue(country_code);
                    row.CreateCell(5).SetCellValue(baris.primary_contact_name);
                    row.CreateCell(6).SetCellValue(baris.primary_contact_email);
                    row.CreateCell(7).SetCellValue(baris.primary_contact_phone);
                    row.CreateCell(8).SetCellValue(baris.secondary_contact_name);
                    row.CreateCell(9).SetCellValue(baris.secondary_contact_email);
                    row.CreateCell(10).SetCellValue(baris.secondary_contact_phone);
                    row.CreateCell(11).SetCellValue(baris.additional_information);
                    row.CreateCell(12).SetCellValue(bank_code);
                    row.CreateCell(13).SetCellValue(account_number);
                    row.CreateCell(14).SetCellValue(currency_code);
                    row.CreateCell(15).SetCellValue(Convert.ToBoolean(baris.is_taxable));
                    row.CreateCell(16).SetCellValue(Convert.ToBoolean(baris.is_active));
                    RowCount++;
                }

                //*********** #2
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Attachment");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Customer ID");
                row.CreateCell(1).SetCellValue("File Name");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail1 = dbContext.customer_attachment.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in detail1)
                {
                    var customer_code = "";
                    var customer = dbFind.customer.Where(o => o.id == isi.customer_id).FirstOrDefault();
                    if (customer != null) customer_code = customer.business_partner_code.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(customer_code);
                    row.CreateCell(1).SetCellValue(isi.filename.ToString());
                    RowCount++;
                }
                //*********** #3
                RowCount = 1;
                excelSheet = workbook.CreateSheet("Contact");
                row = excelSheet.CreateRow(0);
                // Setting Cell Heading
                row.CreateCell(0).SetCellValue("Customer ID");
                row.CreateCell(1).SetCellValue("Contact Name");
                row.CreateCell(2).SetCellValue("Contact Email");
                row.CreateCell(3).SetCellValue("Phone");
                row.CreateCell(4).SetCellValue("Position");

                excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                var detail2 = dbContext.contact.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                // Inserting values to table
                foreach (var isi in detail2)
                {
                    var customer_code = "";
                    var customer = dbFind.customer.Where(o => o.id == isi.business_partner_id).FirstOrDefault();
                    if (customer != null) customer_code = customer.business_partner_code.ToString();

                    row = excelSheet.CreateRow(RowCount);
                    row.CreateCell(0).SetCellValue(customer_code);
                    row.CreateCell(1).SetCellValue(isi.contact_name);
                    row.CreateCell(2).SetCellValue(isi.contact_email);
                    row.CreateCell(3).SetCellValue(isi.contact_phone);
                    row.CreateCell(4).SetCellValue(isi.contact_position);
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
