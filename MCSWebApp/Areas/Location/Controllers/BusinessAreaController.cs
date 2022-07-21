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

namespace MCSWebApp.Areas.Location.Controllers
{
    [Area("Location")]
    public class BusinessAreaController : BaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly mcsContext dbContext;

        public BusinessAreaController(IConfiguration Configuration)
            : base(Configuration)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        public IActionResult Index()
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Modelling];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Location];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BusinessArea];
            ViewBag.BreadcrumbCode = WebAppMenu.BusinessArea;

            return View();
        }

        public async Task<IActionResult> Detail(string Id)
        {
            ViewBag.WebAppName = WebAppName;
            ViewBag.RootBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.ProductionLogistics];
            ViewBag.AreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Modelling];
            ViewBag.SubAreaBreadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.Location];
            ViewBag.Breadcrumb = WebAppMenu.BreadcrumbText[WebAppMenu.BusinessArea];
            ViewBag.BreadcrumbCode = WebAppMenu.BusinessArea;

            try
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    var svc = new BusinessLogic.Entity.BusinessArea(CurrentUserContext);
                    var record = await svc.GetByIdAsync(Id);
                    if (record != null)
                    {
                        ViewBag.Id = Id;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            return View();
        }

        public async Task<IActionResult> ExcelExport()
        {
            string sFileName = "BusinessArea.xlsx";
            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            FileInfo file = new FileInfo(Path.Combine(FilePath, sFileName));
            var memory = new MemoryStream();
            try
            {
                using (var fs = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Create, FileAccess.Write))
                {
                    int RowCount = 1;
                    IWorkbook workbook;
                    workbook = new XSSFWorkbook();
                    ISheet excelSheet = workbook.CreateSheet("Business Area");
                    IRow row = excelSheet.CreateRow(0);
                    // Setting Cell Heading
                    row.CreateCell(0).SetCellValue("Parent Business Area Code");
                    row.CreateCell(1).SetCellValue("Business Area Code");
                    row.CreateCell(2).SetCellValue("Business Area");

                    excelSheet.DefaultColumnWidth = PublicFunctions.ExcelDefaultColumnWidth;

                    mcsContext dbFind = new mcsContext(DbOptionBuilder.Options);

                    var tabledata = dbContext.business_area.Where(o => o.organization_id == CurrentUserContext.OrganizationId);
                    // Inserting values to table
                    foreach (var Value in tabledata)
                    {
                        var parent_business_area_code = "";
                        var business_area = dbFind.business_area
                            .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                && o.id == Value.parent_business_area_id)
                            .FirstOrDefault();
                        if (business_area != null)
                        {
                            parent_business_area_code = business_area.business_area_code.ToString();
                        }

                        row = excelSheet.CreateRow(RowCount);
                        row.CreateCell(0).SetCellValue(parent_business_area_code);
                        row.CreateCell(1).SetCellValue(Value.business_area_code);
                        row.CreateCell(2).SetCellValue(Value.business_area_name);
                        RowCount++;
                    }
                    workbook.Write(fs);
                    using (var stream = new FileStream(Path.Combine(FilePath, sFileName), FileMode.Open))
                    {
                        await stream.CopyToAsync(memory);
                    }
                    memory.Position = 0;
                    //Throws Generated file to Browser
                }
                return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            // Deletes the generated file from /wwwroot folder
            finally
            {
                var path = Path.Combine(FilePath, sFileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            return null;
        }

    }
}
