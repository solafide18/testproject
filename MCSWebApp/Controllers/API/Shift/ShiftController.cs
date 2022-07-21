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
using Microsoft.EntityFrameworkCore;
using DevExtreme.AspNet.Data.ResponseModel;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using BusinessLogic;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using MCSWebApp.Models;

namespace MCSWebApp.Controllers.API.General
{
    [Route("api/Shift/[controller]")]
    [ApiController]
    public class ShiftController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ShiftController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_shift
                .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("DataGridByLatest")]
        public async Task<object> DataGridByLatest(DataSourceLoadOptions loadOptions, string latestUpdate)
        {
            if(string.IsNullOrEmpty(latestUpdate) )
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_shift
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId), loadOptions);
            }
            else
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_shift
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.modified_on > DateTime.Parse(latestUpdate) 
                || (o.modified_on == null && o.created_on > DateTime.Parse(latestUpdate)))
                , loadOptions);
            }
        }

        [HttpGet("DataDetail")]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.shift.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(shift),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new shift();
                    JsonConvert.PopulateObject(values, record);

                    var cekdata = dbContext.shift
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.shift_code.ToLower() == record.shift_code.ToLower())
                        .FirstOrDefault();
                    if (cekdata != null) return BadRequest("Duplicate Name field.");

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
                    //record.is_active = true;
                    record.is_default = null;
                    record.is_locked = null;
                    record.entity_id = null;
                    record.owner_id = CurrentUserContext.AppUserId;
                    record.organization_id = CurrentUserContext.OrganizationId;
                    record.shift_code = record.shift_code.ToUpper();

                    dbContext.shift.Add(record);
                    await dbContext.SaveChangesAsync();

                    return Ok(record);
				}
				else
				{
					return BadRequest("User is not authorized.");
				}
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpPut("UpdateData")]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        JsonConvert.PopulateObject(values, record);
                        var is_active = record.is_active;
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        record.is_active = is_active;

                        await dbContext.SaveChangesAsync();
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

        [HttpDelete("DeleteData")]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            try
            {
                var record = dbContext.shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        dbContext.shift.Remove(record);
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                return Ok();
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("ShiftIdLookup")]
        public async Task<object> ShiftIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.shift_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }


        [HttpGet("ShiftCategoryIdLookup")]
        public async Task<object> ShiftCategoryIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.shift_category
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.shift_category_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("TimeLookup")]
        public IActionResult TimeLookup()
        {
            var data = new List<object>();

            try
            {                
                for(var i = 0; i < 24; i++)
                {
                    for(var j = 0; j < 12; j++)
                    {
                        var val = $"{i.ToString().PadLeft(2, '0')}:{(5*j).ToString().PadLeft(2, '0')}:00";
                        data.Add(new { value = val, text = val });
                    }
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}

            return new JsonResult(data);
        }

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.vw_shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Id).FirstOrDefaultAsync();
                return Ok(record);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] shift Record)
        {
            try
            {
                var record = dbContext.shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Record.id)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);
                    record.InjectFrom(Record);
                    record.InjectFrom(e);
                    record.modified_by = CurrentUserContext.AppUserId;
                    record.modified_on = DateTime.Now;

                    await dbContext.SaveChangesAsync();
                    return Ok(record);
                }
                else
                {
                    record = new shift();
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

                    dbContext.shift.Add(record);
                    await dbContext.SaveChangesAsync();

                    return Ok(record);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpDelete("Delete/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Debug($"string Id = {Id}");

            try
            {
                var record = dbContext.shift
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Id)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.shift.Remove(record);
                    await dbContext.SaveChangesAsync();
                }

                return Ok();
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

                    var shift_category_id = "";
                    var shift_category = dbContext.shift_category
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.shift_category_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower())
                        .FirstOrDefault();
                    if (shift_category != null) shift_category_id = shift_category.id.ToString();

                    var record = dbContext.shift
                        .Where(o => o.shift_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()
							&& o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.shift_category_id = shift_category_id; kol++;
                        record.shift_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;

                        if (row.GetCell(3).ToString() != "")
                            record.start_time = TimeSpan.Parse(row.GetCell(3).ToString());
                        else
                            record.start_time = TimeSpan.Parse("00:00");
                        kol++;

                        if (row.GetCell(4).ToString() != "")
                            record.end_time = TimeSpan.Parse(row.GetCell(4).ToString());
                        else
                            record.end_time = TimeSpan.Parse("00:00");
                        kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(5)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shift();
                        record.id = Guid.NewGuid().ToString("N");
                        record.created_by = CurrentUserContext.AppUserId;
                        record.created_on = DateTime.Now;
                        record.modified_by = null;
                        record.modified_on = null;
                        //record.is_active = true;
                        record.is_default = null;
                        record.is_locked = null;
                        record.entity_id = null;
                        record.owner_id = CurrentUserContext.AppUserId;
                        record.organization_id = CurrentUserContext.OrganizationId;

                        record.shift_category_id = shift_category_id; kol++;
                        record.shift_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.shift_code = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;

                        if (row.GetCell(3).ToString() != "")
                            record.start_time = TimeSpan.Parse(row.GetCell(3).ToString());
                        else 
                            record.start_time = TimeSpan.Parse("00:00");
                        kol++;

                        if (row.GetCell(4).ToString() != "")
                            record.end_time = TimeSpan.Parse(row.GetCell(4).ToString());
                        else 
                            record.end_time = TimeSpan.Parse("00:00");
                        kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(5)); kol++;

                        dbContext.shift.Add(record);
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
                HttpContext.Session.SetString("filename", "Shift");
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
