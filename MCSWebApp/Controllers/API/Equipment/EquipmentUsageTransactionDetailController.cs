﻿using System;
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
using Omu.ValueInjecter;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using BusinessLogic;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using DataAccess.Select2;
using BusinessLogic.Entity;
using Common;

namespace MCSWebApp.Controllers.API.Equipment
{
    [Route("api/Equipment/[controller]")]
    [ApiController]
    public class EquipmentUsageTransactionDetailController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public EquipmentUsageTransactionDetailController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("GetByEquipmentUsageTransactionId")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ApiResponse<List<vw_equipment_usage_transaction_detail>>> GetByEquipmentUsageTransactionId([FromQuery] string recordId)
        {
            var result = new ApiResponse<List<vw_equipment_usage_transaction_detail>>();
            result.Status.Success = true;
            try
            {
                result.Data = await dbContext.vw_equipment_usage_transaction_detail
                    .Where(o => o.equipment_usage_transaction_id == recordId
                        && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                result.Status.Success = false;
                result.Status.Message = ex.Message;
                logger.Error(ex);
            }
            return result;
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_equipment_usage_transaction.Where(o =>
                    o.organization_id == CurrentUserContext.OrganizationId
                    && o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");
            var status = true;
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(equipment_usage_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        var record = new equipment_usage_transaction();
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

                        dbContext.equipment_usage_transaction.Add(record);
                        await dbContext.SaveChangesAsync();

                        if (status) await tx.CommitAsync();
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

        [HttpPost("Save")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ApiResponse> Save(equipment_usage_transaction_detail record)
        {
            var result = new ApiResponse();
            result.Status.Success = true;
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(equipment_usage_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        
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

                        dbContext.equipment_usage_transaction_detail.Add(record);
                        await dbContext.SaveChangesAsync();

                        if (result.Status.Success) await tx.CommitAsync();

                        result.Data = record;
                        result.Status.Message = "Data has been added.";
                        return result;
                    }
                    else
                    {
                        result.Status.Message = "User is not authorized.";
                        result.Status.Success = false;
                    }
                }
                catch (Exception ex)
                {
                    result.Status.Success = false;
                    if (ex.InnerException != null)
                    {
                        logger.Error(ex.InnerException.Message);
                        result.Status.Message = ex.InnerException.Message;
                    }
                    else
                    {
                        logger.Error(ex.ToString());
                        result.Status.Message = ex.Message;
                    }
                }
            }
            return result;
        }

        [HttpDelete("DeleteById")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ApiResponse> DeleteById([FromQuery] string recordId)
        {
            var result = new ApiResponse();
            result.Status.Success = true;
            try
            {
                var record = dbContext.equipment_usage_transaction_detail
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.id == recordId)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.equipment_usage_transaction_detail.Remove(record);
                    await dbContext.SaveChangesAsync();
                    result.Status.Message = "Delete successfully";
                } 
                else
                {
                    result.Status.Success = false;
                    result.Status.Message = "There is no record to be delete.";
                }

            }
            catch (Exception ex)
            {
                result.Status.Success = false;
                if (ex.InnerException != null)
                {
                    logger.Error(ex.InnerException.Message);
                    result.Status.Message = ex.InnerException.Message;
                }
                else
                {
                    logger.Error(ex.ToString());
                    result.Status.Message = ex.Message;
                }
            }
            return result;
        }

        [HttpGet("select2")]
        public async Task<object> Select2([FromQuery] string q)
        {
            var result = new Select2Response();

            try
            {
                var s2Request = new Select2Request()
                {
                    q = q
                };
                if (s2Request != null)
                {
                    //var record = await dbContext.vw_equipment_usage_transaction.FindAsync();
                    var temp = await dbContext.vw_equipment_usage_transaction_detail_lookup.Where(r => r.organization_id == CurrentUserContext.OrganizationId).ToListAsync();
                    if (temp.Count > 0)
                    {
                        result.results = new List<Select2Item>();
                        result.count = temp.Count;
                        var id = 0;
                        foreach (var item in temp)
                        {
                            id++;
                            var selectItem = new Select2Item();
                            selectItem.id = id.ToString();
                            selectItem.text = item.equipment_name;
                            selectItem.data = item;
                            selectItem.disabled = false;
                            selectItem.selected = true;
                            result.results.Add(selectItem);
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            return new JsonResult(result);
        }

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.equipment_usage_transaction
                    .Where(o =>
                        o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);

                    JsonConvert.PopulateObject(values, record);

                    record.InjectFrom(e);
                    record.modified_by = CurrentUserContext.AppUserId;
                    record.modified_on = DateTime.Now;

                    await dbContext.SaveChangesAsync();
                    return Ok(record);
                }
                else
                {
                    return BadRequest("No default organization");
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Trace($"string key = {key}");

            try
            {
                var record = dbContext.equipment_usage_transaction
                    .Where(o =>
                        o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.equipment_usage_transaction.Remove(record);
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

        [HttpPost("UploadDocument1")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UploadDocument1([FromBody] dynamic FileDocument)
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

            IWorkbook workbook;
            using (FileStream file = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                workbook = WorkbookFactory.Create(file);
            }

            string teks = "==>Sheet 1" + Environment.NewLine;
            int i = 0; bool gagal = false; string errormessage = "";
            var importer = new Npoi.Mapper.Mapper(workbook);
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            var sheet = importer.Take<equipment_usage_transaction>(0);
            foreach (var item in sheet)
            {
                var row = item.Value;
                i++;
                try
                {
                    var record = dbContext.equipment_usage_transaction.Where(o => o.equipment_usage_number == row.equipment_usage_number).FirstOrDefault();
                    if (record == null)
                    {
                        row.id = Guid.NewGuid().ToString("N");
                        dbContext.equipment_usage_transaction.Add(row);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        record.InjectFrom(row);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        dbContext.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Line " + i + ", row id: " + row.id + Environment.NewLine;
                    }
                    else errormessage = ex.Message;

                    teks += errormessage + Environment.NewLine + Environment.NewLine;
                    gagal = true;
                }
            }
            if (gagal)
            {
                await transaction.RollbackAsync();
                HttpContext.Session.SetString("errormessage", teks);
                HttpContext.Session.SetString("filename", "EquipmentUsage");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
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

                    var equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                                    o.equipment_code == row.GetCell(0).ToString()).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    var accounting_period_id = "";
                    var accounting_period = dbContext.accounting_period
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                                    o.accounting_period_name.ToLower() == row.GetCell(1).ToString().ToLower()).FirstOrDefault();
                    if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();

                    //var record = dbContext.equipment_usage_transaction
                    //    .Where(o => o.equipment_id == equipment_id && o.organization_id == CurrentUserContext.OrganizationId)
                    //    .FirstOrDefault();
                    //if (record != null)
                    //{
                    //    var e = new entity();
                    //    e.InjectFrom(record);

                    //    record.InjectFrom(e);
                    //    record.modified_by = CurrentUserContext.AppUserId;
                    //    record.modified_on = DateTime.Now;
                    //    kol++;
                    //    record.accounting_period_id = accounting_period_id; kol++;
                    //    record.hour_usage = PublicFunctions.Desimal(row.GetCell(2)); kol++;

                    //    await dbContext.SaveChangesAsync();
                    //}
                    //else
                    //{
                    //    record = new equipment_usage_transaction();
                    //    record.id = Guid.NewGuid().ToString("N");
                    //    record.created_by = CurrentUserContext.AppUserId;
                    //    record.created_on = DateTime.Now;
                    //    record.modified_by = null;
                    //    record.modified_on = null;
                    //    record.is_active = true;
                    //    record.is_default = null;
                    //    record.is_locked = null;
                    //    record.entity_id = null;
                    //    record.owner_id = CurrentUserContext.AppUserId;
                    //    record.organization_id = CurrentUserContext.OrganizationId;

                    //    record.equipment_id = equipment_id; kol++;
                    //    record.accounting_period_id = accounting_period_id; kol++;
                    //    record.hour_usage = PublicFunctions.Desimal(row.GetCell(2)); kol++;
                    //    record.transaction_number = "";

                    //    dbContext.equipment_usage_transaction.Add(record);
                    //    await dbContext.SaveChangesAsync();
                    //}
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
                HttpContext.Session.SetString("filename", "EquipmentUsage");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }


        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            if (await mcsContext.CanRead(dbContext, Id, CurrentUserContext.AppUserId)
                || CurrentUserContext.IsSysAdmin)
            {
                try
                {
                    var record = await dbContext.vw_equipment_usage_transaction
                        .Where(o => o.id == Id
                            && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                                || CurrentUserContext.IsSysAdmin))
                        .FirstOrDefaultAsync();
                    return Ok(record);
                }
				catch (Exception ex)
				{
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }
            else
            {
                return BadRequest("User is not authorized.");
            }
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] equipment_usage_transaction Record)
        {
            try
            {
                var record = dbContext.equipment_usage_transaction
                    .Where(o => o.id == Record.id)
                    .FirstOrDefault();
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
                        return Ok(record);
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                else if (await mcsContext.CanCreate(dbContext, nameof(equipment_usage_transaction),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    record = new equipment_usage_transaction();
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

                    dbContext.equipment_usage_transaction.Add(record);
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
    }
}
