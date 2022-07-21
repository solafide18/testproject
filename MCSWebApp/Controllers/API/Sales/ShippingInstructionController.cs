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
using SelectPdf;
using System.Globalization;

namespace MCSWebApp.Controllers.API.Sales
{
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class ShippingInstructionController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ShippingInstructionController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            try
            {
                return await DataSourceLoader.LoadAsync(
                    dbContext.vw_shipping_instruction.Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .OrderByDescending(o => o.created_on),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DespatchOrderIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DespatchOrderIdLookup([FromQuery] string SalesOrderId, DataSourceLoadOptions loadOptions)
        {
            try
            {
                if (string.IsNullOrEmpty(SalesOrderId))
                {
                    logger.Debug($"SalesOrderId in DespatchOrderIdLookup is null or empty");
                    var lookup = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { Value = o.id, Text = o.despatch_order_number, o.sales_order_id });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    logger.Debug($"SalesOrderId in DespatchOrderIdLookup is {SalesOrderId}");
                    var lookup = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.sales_order_id == SalesOrderId)
                        .Select(o => new { Value = o.id, Text = o.despatch_order_number, o.sales_order_id });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<StandardResult> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            var result = new StandardResult();
            try
            {
                var record = await dbContext.vw_shipping_instruction
                    .Where(o => o.id == Id).FirstOrDefaultAsync();
                result.Data = record;
                result.Success = record != null ? true : false;
                result.Message = result.Success ? "Ok" : "Record not found";

            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                result.Success = false;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return result;
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                if (await mcsContext.CanCreate(dbContext, nameof(shipping_instruction),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    var record = new shipping_instruction();
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

                    #region Get transaction number
                    var conn = dbContext.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync();
                    }
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            try
                            {
                                
                                cmd.CommandText = $"SELECT nextval('shipping_instruction_number_seq')";
                                var r = await cmd.ExecuteScalarAsync();
                                var s = new string('0', 4 - r.ToString().Length);
                                record.shipping_instruction_number = $"SI-{DateTime.Now:yyyy}{DateTime.Now:MM}{DateTime.Now:dd}-{s}{r}";
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.ToString());
                                return BadRequest(ex.Message);
                            }
                        }
                    }
                    #endregion


                    dbContext.shipping_instruction.Add(record);
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

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] shipping_instruction Record)
        {
            try
            {
                var record = dbContext.shipping_instruction
                    .Where(o => o.id == Record.id)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);
                    record.InjectFrom(Record);
                    record.modified_by = CurrentUserContext.AppUserId;
                    record.modified_on = DateTime.Now;
                    record.owner_id = (record.owner_id ?? CurrentUserContext.AppUserId);

                    await dbContext.SaveChangesAsync();
                    return Ok(record);
                }
                else
                {
                    record = new shipping_instruction();
                    record.InjectFrom(Record);

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
                    record.is_default = null;
                    record.is_locked = null;
                    record.entity_id = null;
                    record.owner_id = CurrentUserContext.AppUserId;
                    record.organization_id = CurrentUserContext.OrganizationId;

                    #region Get transaction number
                    var conn = dbContext.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync();
                    }
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            try
                            {
                                cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                var r = await cmd.ExecuteScalarAsync();
                                record.shipping_instruction_number = $"{r}/IC-SI/{DateTime.Now:MM}/{DateTime.Now:yyyy}";
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.ToString());
                                return BadRequest(ex.Message);
                            }
                        }
                    }
                    #endregion

                    dbContext.shipping_instruction.Add(record);
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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.shipping_instruction
                    .Where(o => o.id == key)
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
            logger.Debug($"string key = {key}");

            try
            {
                var record = dbContext.shipping_instruction
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.shipping_instruction.Remove(record);
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

        [HttpGet("SamplingTemplateIdLookup")]
        public async Task<object> SamplingTemplateIdLookup(DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                var lookup = dbContext.sampling_template
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                .Select(o =>
                    new
                    {
                        value = o.id,
                        text = o.sampling_template_name
                    });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DetailSurveyByShippingInstructionId/{id}")]
        public async Task<object> DetailSurveyByShippingInstructionId(string id)
        {
            try
            {
                var lookup = await dbContext.vw_shipping_instruction_detail_survey
                    .Where(o => o.shipping_instruction_id == id).FirstOrDefaultAsync();
                return Ok(lookup);
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
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    string despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.despatch_order_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower())
                        .FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    //var barge_id = "";
                    //var barge = dbContext.barge
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                    //        o.vehicle_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(18)).ToLower().Trim()).FirstOrDefault();

                    //var tug_id = "";
                    //var tug = dbContext.tug
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                    //        o.vehicle_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(19)).ToLower().Trim()).FirstOrDefault();
                    
                    //var vendor_id = "";
                    //var vendor = dbContext.contractor
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                    //        o.business_partner_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(20)).ToLower().Trim()).FirstOrDefault();

                    string sampling_template_id = "";
                    var sampling_template = dbContext.sampling_template
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.sampling_template_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower())
                        .FirstOrDefault();
                    if (sampling_template != null) sampling_template_id = sampling_template.id.ToString();

                    string ShippingInstructionNumber = "";
                    if (PublicFunctions.IsNullCell(row.GetCell(0)) == "")
                    {
                        #region Get transaction number
                        var conn = dbContext.Database.GetDbConnection();
                        if (conn.State != System.Data.ConnectionState.Open)
                        {
                            await conn.OpenAsync();
                        }
                        if (conn.State == System.Data.ConnectionState.Open)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                try
                                {
                                    cmd.CommandText = $"SELECT nextval('shipping_instruction_number_seq')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    var s = new string('0', 4 - r.ToString().Length);
                                    ShippingInstructionNumber = $"SI-{DateTime.Now:yyyy}{DateTime.Now:MM}{DateTime.Now:dd}-{s}{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }
                        #endregion
                    }
                    else
                        ShippingInstructionNumber = PublicFunctions.IsNullCell(row.GetCell(0)).Trim();


                    var record = dbContext.shipping_instruction
                        .Where(o => o.shipping_instruction_number == ShippingInstructionNumber
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record == null)
                    {
                        record = new shipping_instruction();
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

                        record.shipping_instruction_number = ShippingInstructionNumber;
                        record.shipping_instruction_date = PublicFunctions.Tanggal(row.GetCell(1));
                        record.despatch_order_id = despatch_order_id;
                        //record.to_other = PublicFunctions.IsNullCell(row.GetCell(3));
                        record.notify_party = PublicFunctions.IsNullCell(row.GetCell(3));
                        record.cargo_description = PublicFunctions.IsNullCell(row.GetCell(4));
                        //record.cc = PublicFunctions.IsNullCell(row.GetCell(6));
                        record.sampling_template_id = sampling_template_id;
                        record.marked = PublicFunctions.IsNullCell(row.GetCell(6));
                        record.issued_date = PublicFunctions.Tanggal(row.GetCell(7));
                        record.placed = PublicFunctions.IsNullCell(row.GetCell(8));
                        record.shipping_instruction_created_by = PublicFunctions.IsNullCell(row.GetCell(9));
                        record.lampiran1 = PublicFunctions.IsNullCell(row.GetCell(10));
                        record.lampiran2 = PublicFunctions.IsNullCell(row.GetCell(11));
                        record.lampiran3 = PublicFunctions.IsNullCell(row.GetCell(12));
                        record.lampiran4 = PublicFunctions.IsNullCell(row.GetCell(13));
                        record.lampiran5 = PublicFunctions.IsNullCell(row.GetCell(14));
                        record.si_number = PublicFunctions.IsNullCell(row.GetCell(15));
                        record.notify_party_address = PublicFunctions.IsNullCell(row.GetCell(16));
                        record.hs_code = PublicFunctions.IsNullCell(row.GetCell(17));
                        //record.barge_id = barge_id;
                        //record.tug_id = tug_id;
                        // record.vendor_id = vendor_id;

                        dbContext.shipping_instruction.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.shipping_instruction_date = PublicFunctions.Tanggal(row.GetCell(1));
                        record.despatch_order_id = despatch_order_id;
                        //record.to_other = PublicFunctions.IsNullCell(row.GetCell(3));
                        record.notify_party = PublicFunctions.IsNullCell(row.GetCell(3));
                        record.cargo_description = PublicFunctions.IsNullCell(row.GetCell(4));
                        //record.cc = PublicFunctions.IsNullCell(row.GetCell(6));
                        record.sampling_template_id = sampling_template_id;
                        record.marked = PublicFunctions.IsNullCell(row.GetCell(6));
                        record.issued_date = PublicFunctions.Tanggal(row.GetCell(7));
                        record.placed = PublicFunctions.IsNullCell(row.GetCell(8));
                        record.shipping_instruction_created_by = PublicFunctions.IsNullCell(row.GetCell(9));
                        record.lampiran1 = PublicFunctions.IsNullCell(row.GetCell(10));
                        record.lampiran2 = PublicFunctions.IsNullCell(row.GetCell(11));
                        record.lampiran3 = PublicFunctions.IsNullCell(row.GetCell(12));
                        record.lampiran4 = PublicFunctions.IsNullCell(row.GetCell(13));
                        record.lampiran5 = PublicFunctions.IsNullCell(row.GetCell(14));
                        record.si_number = PublicFunctions.IsNullCell(row.GetCell(15));
                        record.notify_party_address = PublicFunctions.IsNullCell(row.GetCell(16));
                        record.hs_code = PublicFunctions.IsNullCell(row.GetCell(17));
                        //record.barge_id = barge_id;
                        //record.tug_id = tug_id;

                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 1, Line " + i + ": " + Environment.NewLine;
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
                HttpContext.Session.SetString("filename", "DespatchOrder");
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
