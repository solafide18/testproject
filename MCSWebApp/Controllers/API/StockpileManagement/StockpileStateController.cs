using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using DataAccess.EFCore;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DataAccess.DTO;
using Omu.ValueInjecter;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using BusinessLogic;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace MCSWebApp.Controllers.API.StockpileManagement
{
    [Route("api/StockpileManagement/[controller]")]
    [ApiController]
    public class StockpileStateController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public StockpileStateController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_stock_state
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId), 
                loadOptions);
        }
        [HttpGet("DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGrid(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_stock_state
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin),
                    loadOptions);
            }

            //var dt1 = DateTime.Parse(tanggal1);
            //var dt2 = DateTime.Parse(tanggal2);
            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            logger.Debug($"dt1 = {dt1}");
            logger.Debug($"dt2 = {dt2}");

            return await DataSourceLoader.LoadAsync(dbContext.vw_stock_state
                .Where(o =>
                    o.transaction_datetime >= dt1
                    && o.transaction_datetime <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId
                    && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_stock_state.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpPost("UploadDocument")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UploadDocument([FromBody] dynamic FileDocument)
        {
            var result = new StandardResult();
            long size = 0;

            if (FileDocument == null)
            {
                return BadRequest("No file uploaded!");
            }

            string FilePath = configuration.GetSection("Path").GetSection("UploadBasePath").Value + PublicFunctions.ExcelFolder;
            if (!Directory.Exists(FilePath))  Directory.CreateDirectory(FilePath);

            var fileName = (string)FileDocument.filename;
            FilePath += $@"\{fileName}";

            string strfile = (string)FileDocument.data;
            byte[] arrfile = Convert.FromBase64String(strfile);

            await System.IO.File.WriteAllBytesAsync(FilePath, arrfile);
            size = fileName.Length;
            string sFileExt = Path.GetExtension(FilePath).ToLower();

            ISheet sheet;
            dynamic wb;
            if (sFileExt == ".xls")
            {
                FileStream stream = System.IO.File.OpenRead(FilePath);
                wb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats
                sheet = wb.GetSheetAt(0); //get first sheet from workbook
                stream.Close();
            }
            else
            {
                wb = new XSSFWorkbook(FilePath); //This will read 2007 Excel format
                sheet = wb.GetSheetAt(0); //get first sheet from workbook
            }

            string teks = "==>Sheet 1" + Environment.NewLine;
            bool gagal = false; string errormessage = "";

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 0;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var stock_location_id = "";
                    var stock_location = dbContext.stock_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()).FirstOrDefault();
                    if (stock_location != null) stock_location_id = stock_location.id.ToString();

                    var record = dbContext.stockpile_state
                        .Where(o => o.stockpile_location_id == stock_location_id
							&& o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        kol = 2;
                        record.transaction_datetime = PublicFunctions.Tanggal(row.GetCell(1)); kol++;
                        record.qty_opening = PublicFunctions.Bulat(row.GetCell(2)); kol++;
                        record.qty_in = PublicFunctions.Bulat(row.GetCell(3)); kol++;
                        record.qty_out = PublicFunctions.Bulat(row.GetCell(4)); kol++;
                        record.qty_adjustment = PublicFunctions.Bulat(row.GetCell(5)); kol++;
                        record.qty_closing = PublicFunctions.Bulat(row.GetCell(6)); kol++;
                        record.transaction_id = "0";

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new stockpile_state();
                        record.id = Guid.NewGuid().ToString("N");
                        record.created_by = CurrentUserContext.AppUserId;
                        record.created_on = DateTime.Now;
                        record.modified_by = null;
                        record.modified_on = null;
                        record.is_active = true;
                        record.is_default = null;
                        record.is_locked = null;
                        record.entity_id = null;
                        record.owner_id = CurrentUserContext.AppUserId;
                        record.organization_id = CurrentUserContext.OrganizationId;

                        kol = 1;
                        record.stockpile_location_id = stock_location_id; kol++;
                        record.transaction_datetime = PublicFunctions.Tanggal(row.GetCell(1)); kol++;
                        record.qty_opening = PublicFunctions.Bulat(row.GetCell(2)); kol++;
                        record.qty_in = PublicFunctions.Bulat(row.GetCell(3)); kol++;
                        record.qty_out = PublicFunctions.Bulat(row.GetCell(4)); kol++;
                        record.qty_adjustment = PublicFunctions.Bulat(row.GetCell(5)); kol++;
                        record.qty_closing = PublicFunctions.Bulat(row.GetCell(6)); kol++;
                        record.transaction_id = "0";

                        dbContext.stockpile_state.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 1, Line " + i + ", Column " + kol + " : " + Environment.NewLine;
                    }
                    else errormessage = ex.Message;

                    teks += errormessage + Environment.NewLine + Environment.NewLine;
                    gagal = true;
                }
            }
            wb.Close();
            if (gagal)
            {
                await transaction.RollbackAsync();
                HttpContext.Session.SetString("errormessage", teks);
                HttpContext.Session.SetString("filename", "StockpileState");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

    }
}
