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
using Common;
using Hangfire;

namespace MCSWebApp.Controllers.API.StockpileManagement
{
    [Route("api/Planning/[controller]")]
    [ApiController]
    public class BlendingPlanController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public BlendingPlanController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,IBackgroundJobClient backgroundJobClient)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_blending_plan
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
                return await DataSourceLoader.LoadAsync(dbContext.vw_blending_plan
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

            return await DataSourceLoader.LoadAsync(dbContext.vw_blending_plan
                .Where(o =>
                    o.unloading_datetime >= dt1
                    && o.unloading_datetime <= dt2
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
                dbContext.vw_blending_plan.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            blending_plan record;

            using (var tx = await dbContext.Database.BeginTransactionAsync()) 
            { 
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(blending_plan),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        record = new blending_plan();
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
                                    cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    record.transaction_number = $"BP-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.blending_plan.Add(record);
                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        success = true;
                    }
                    else
                    {
                        //return BadRequest("User is not authorized.");
                        logger.Debug("User is not authorized.");
                        return Unauthorized();
                    }
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.Error(ex.ToString());
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
        }
            if (success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.blending_plan();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    //_backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BlendingPlan.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpGet("QualitySamplingIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> QualitySamplingIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.quality_sampling.FromSqlRaw(
                        "select * from quality_sampling where id not in " +
                            "(select survey_id from blending_plan where survey_id is not null)"
                    )
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.sampling_number });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
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

            var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var record = dbContext.blending_plan
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

                    await tx.CommitAsync();
                    return Ok(record);
                }
                else
                {
                    await tx.RollbackAsync();
                    return BadRequest("No default organization");
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger.Error(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("SourceShiftIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SourceShiftIdLookup(DataSourceLoadOptions loadOptions)
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

        [HttpGet("DespatchOrderIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DespatchOrderIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.despatch_order
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.despatch_order_number });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
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
                var record = dbContext.blending_plan
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.blending_plan.Remove(record);
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

        [HttpGet("AccountingPeriodIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> AccountingPeriodIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.accounting_period
                    .Where(o => o.is_closed == null || o.is_closed == false)
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.accounting_period_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("ProcessFlowIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProcessFlowIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.process_flow
                    .Where(o => o.process_flow_category == Common.ProcessFlowCategory.BLENDING)
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                    new
                    {
                        Value = o.id,
                        Text = o.process_flow_name
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
        public async Task<object> DestinationLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                //var lookup = dbContext.stock_location
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                //        o.stock_location_name != "")
                //    .Select(o => new { Value = o.id, Text = o.stock_location_name });

                var minelocation = dbContext.mine_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.stock_location_name });
                var vessel = dbContext.vessel
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.vehicle_name });
                var stockpileocation = dbContext.vw_stockpile_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });
                var portlocation = dbContext.port_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.stock_location_name });

                var lookup = minelocation.Union(vessel).Union(stockpileocation).Union(portlocation)
                    .OrderBy(o => o.Text);

                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("UomIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UomIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.uom
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.uom_symbol });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("ProductIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProductIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.product
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.product_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("SurveyIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SurveyIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.survey
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && (o.is_draft_survey == null || o.is_draft_survey == false))
                    .Select(o => new { Value = o.id, Text = o.survey_number });
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

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(2))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var shift_id = "";
                    var shift = dbContext.shift
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.shift_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (shift != null) shift_id = shift.id.ToString();

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_order_number == PublicFunctions.IsNullCell(row.GetCell(5))).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var destination_location_id = "";
                    //var stock_location = dbContext.stock_location
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                    //        o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower()).FirstOrDefault();
                    //if (stock_location != null) destination_location_id = stock_location.id.ToString();
                    var mine_location = dbContext.mine_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.mine_location_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower()).FirstOrDefault();
                    if (mine_location != null) destination_location_id = mine_location.id.ToString();
                    else
                    {
                        var vessel = dbContext.vessel
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.vehicle_id.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower()).FirstOrDefault();
                        if (vessel != null) destination_location_id = vessel.id.ToString();
                        else
                        {
                            var stockpile_location = dbContext.stockpile_location
                            .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                                o.stockpile_location_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower())
                            .FirstOrDefault();
                            if (stockpile_location != null) destination_location_id = stockpile_location.id.ToString();
                            else
                            {
                                var port_location = dbContext.port_location
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                                    o.port_location_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower())
                                .FirstOrDefault();
                                if (port_location != null) destination_location_id = port_location.id.ToString();
                            }
                        }
                    }


                    #region Get transaction number
                    var TransactionNumber = "";
                    if (PublicFunctions.IsNullCell(row.GetCell(0)) == "")
                    {
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
                                    TransactionNumber = $"BP-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }
                    }
                    else
                        TransactionNumber = PublicFunctions.IsNullCell(row.GetCell(0));
                    #endregion
                    
                    var record = dbContext.blending_plan
                        .Where(o => o.transaction_number.ToLower() == TransactionNumber.ToLower()
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
                        record.planning_category = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.product_id = product_id; kol++;
                        record.unloading_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.source_shift_id = shift_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new blending_plan();
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

                        record.transaction_number = TransactionNumber; kol++;
                        record.planning_category = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.product_id = product_id; kol++;
                        record.unloading_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.source_shift_id = shift_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;

                        dbContext.blending_plan.Add(record);
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

            sheet = wb.GetSheetAt(1); //*** detail sheet
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {   
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var blending_plan_id = "";
                    var blending_plan = dbContext.blending_plan
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number.Trim() == PublicFunctions.IsNullCell(row.GetCell(0)).Trim())
                        .FirstOrDefault();
                    if (blending_plan != null) blending_plan_id = blending_plan.id.ToString();
                        
                    var source_location_id = "";
                    //var stock_location = dbContext.stock_location
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                    //        o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).ToLower()).FirstOrDefault();

                    var minelocation = dbContext.mine_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.mine_location_code });
                    var stockpileocation = dbContext.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.stockpile_location_code });
                    var portlocation = dbContext.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { id = o.id, code = o.port_location_code });

                    var location = minelocation.Union(stockpileocation).Union(portlocation)
                        .Where(o => o.code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower())
                        .FirstOrDefault();
                    if (location != null) source_location_id = location.id.ToString();

                    var record = dbContext.blending_plan_source
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.blending_plan_id == blending_plan_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.spec_ts = PublicFunctions.Desimal(row.GetCell(1)); kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.volume = PublicFunctions.Desimal(row.GetCell(4)); kol++;
                        record.analyte_1 = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.analyte_2 = PublicFunctions.Desimal(row.GetCell(6)); kol++;
                        record.analyte_3 = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.analyte_4 = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.analyte_5 = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.analyte_6 = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.analyte_7 = PublicFunctions.Desimal(row.GetCell(11)); kol++;
                        record.analyte_8 = PublicFunctions.Desimal(row.GetCell(12)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new blending_plan_source();
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

                        record.blending_plan_id = blending_plan_id; kol++;
                        record.spec_ts = PublicFunctions.Desimal(row.GetCell(1)); kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.volume = PublicFunctions.Desimal(row.GetCell(4)); kol++;
                        record.analyte_1 = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.analyte_2 = PublicFunctions.Desimal(row.GetCell(6)); kol++;
                        record.analyte_3 = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.analyte_4 = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.analyte_5 = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.analyte_6 = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.analyte_7 = PublicFunctions.Desimal(row.GetCell(11)); kol++;
                        record.analyte_8 = PublicFunctions.Desimal(row.GetCell(12)); kol++;

                        dbContext.blending_plan_source.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 2, Line " + i + ", Column " + kol + " : " + Environment.NewLine;
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
                HttpContext.Session.SetString("filename", "BlendingPlan");
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
