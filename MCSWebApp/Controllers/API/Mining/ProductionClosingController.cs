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
using Microsoft.EntityFrameworkCore;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DataAccess.DTO;
using BusinessLogic;
using Omu.ValueInjecter;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Common;

namespace MCSWebApp.Controllers.API.Mining
{
    [Route("api/Mining/[controller]")]
    [ApiController]
    public class ProductionClosingController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ProductionClosingController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_production_closing
                .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("ProductionByTN")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProductionByTN(string FromDate, string ToDate, DataSourceLoadOptions loadOptions)
        {
            if (FromDate == "null" || ToDate == "null")
                return null;

            FromDate = FromDate.Replace("T", " ");
            ToDate = ToDate.Replace("T", " ");

            var dt1 = DateTime.Parse(FromDate);
            var dt2 = DateTime.Parse(ToDate);

            return await DataSourceLoader.LoadAsync(dbContext.production_transaction
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                       o.unloading_datetime >= Convert.ToDateTime(dt1) &&
                       o.unloading_datetime <= Convert.ToDateTime(dt2)),
                loadOptions);
        }

        [HttpGet("ProductionTotal")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProductionTotal(DataSourceLoadOptions loadOptions, string FromDate, string ToDate)
        {
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (FromDate == "null" || ToDate == "null")
                    return null;

                var dt1 = DateTime.Parse(FromDate);
                var dt2 = DateTime.Parse(ToDate);

                var lookup = dbContext.production_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                           o.unloading_datetime >= Convert.ToDateTime(dt1) &&
                           o.unloading_datetime <= Convert.ToDateTime(dt2))
                    .GroupBy(o => o.organization_id)
                    .Select(o =>
                    new
                    {
                        unloading_quantity = o.Sum(p => p.unloading_quantity),
                        distance = o.Average(p => p.distance)
                    });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.production_closing
                .Where(o => o.id == Id &&
                    (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(production_closing),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        var record = new production_closing();
                        JsonConvert.PopulateObject(values, record);

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

                        dbContext.production_closing.Add(record);
                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                        return Ok(record);
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        return BadRequest(ex.InnerException.Message);
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.production_closing
                        .Where(o => o.id == key)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);
                            JsonConvert.PopulateObject(values, record);

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok(record);
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else
                    {
                        return BadRequest("Record does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        return BadRequest(ex.InnerException.Message);
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Trace($"string key = {key}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.production_closing
                        .Where(o => o.id == key)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.production_closing.Remove(record);
                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok();
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else
                    {
                        return BadRequest("Record does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        return BadRequest(ex.InnerException.Message);
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpGet("SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SourceLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                var lookup = dbContext.vw_production_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .GroupBy(p => p.source_location_id)
                    .Select(o =>
                        new
                        {
                            value = o.Max(p => p.source_location_id),
                            text = o.Max(p => p.source_location_name)
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DestinationLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DestinationLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                var lookup = dbContext.vw_production_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .GroupBy(p => p.destination_location_id)
                    .Select(o =>
                        new
                        {
                            value =  o.Max(p => p.destination_location_id),
                            text = o.Max(p => p.destination_location_name)
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
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
            if (!Directory.Exists(FilePath)) Directory.CreateDirectory(FilePath);

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

            string teks = "";
            bool gagal = false; string errormessage = "";

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var accounting_period_id = "";
                    var accounting_period = dbContext.accounting_period
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.accounting_period_name.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).Trim().ToLower()).FirstOrDefault();
                    if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();

                    var advance_contract_id = "";
                    var advance_contract = dbContext.advance_contract
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.advance_contract_number.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).Trim().ToLower())
                        .FirstOrDefault();
                    if (advance_contract != null) advance_contract_id = advance_contract.id.ToString();

                    var advance_contract_reference_id = "";
                    var advance_contract_reference = dbContext.advance_contract_reference
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.name.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(4)).Trim().ToLower()).FirstOrDefault();
                    if (advance_contract_reference != null) advance_contract_reference_id = advance_contract_reference.id.ToString();

                    var record = dbContext.production_closing
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.transaction_number.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).Trim().ToLower())
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        //record.date_time = Convert.ToDateTime(row.GetCell(2).ToString()); kol++;
                        //record.hm_start = PublicFunctions.Desimal(row.GetCell(3)); kol++;
                        //record.hm_end = PublicFunctions.Desimal(row.GetCell(4)); kol++;
                        //record.hm_duration = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new production_closing();
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

                        record.transaction_number = PublicFunctions.IsNullCell(row.GetCell(0)).Trim().ToLower(); kol++;
                        //record.equipment_id = equipment_id; kol++;
                        record.accounting_period_id = accounting_period_id; kol++;
                        //record.date_time = Convert.ToDateTime(row.GetCell(2).ToString()); kol++;
                        //record.hm_start = PublicFunctions.Desimal(row.GetCell(3)); kol++;
                        //record.hm_end = PublicFunctions.Desimal(row.GetCell(4)); kol++;
                        //record.hm_duration = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;

                        dbContext.production_closing.Add(record);
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
                HttpContext.Session.SetString("filename", "DayWork");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpGet("List")]
        public async Task<IActionResult> List([FromQuery] string q)
        {
            try
            {
                var rows = dbContext.vw_production_closing.AsNoTracking();
                rows = rows.Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin);
                if (!string.IsNullOrEmpty(q))
                {
                    rows.Where(o => o.note.Contains(q));
                }

                return Ok(await rows.ToListAsync());
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.vw_production_closing
                    .Where(o => o.id == Id)
                    .FirstOrDefaultAsync();
                if (record != null)
                {
                    if (await mcsContext.CanRead(dbContext, Id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        return Ok(record);
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                else
                {
                    return BadRequest("Record does not exist.");
                }
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] production_closing Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.production_closing
                        .Where(o => o.id == Record.id)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);
                            record.InjectFrom(Record);
                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok(record);
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else if (await mcsContext.CanCreate(dbContext, nameof(production_closing),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        record = new production_closing();
                        record.InjectFrom(Record);

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

                        dbContext.production_closing.Add(record);
                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                        return Ok(record);
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        return BadRequest(ex.InnerException.Message);
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        return BadRequest(ex.Message);
                    }
                }
            }
        }

        [HttpDelete("DeleteById/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Trace($"string Id = {Id}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.production_closing
                        .Where(o => o.id == Id)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, Id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.production_closing.Remove(record);
                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok();
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else
                    {
                        return BadRequest("Record does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        return BadRequest(ex.InnerException.Message);
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        return BadRequest(ex.Message);
                    }
                }
            }
        }
    }
}
