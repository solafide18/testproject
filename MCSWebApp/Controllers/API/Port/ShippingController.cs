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

namespace MCSWebApp.Controllers.API.Port
{
    [Route("api/Port/[controller]")]
    [ApiController]
    public class ShippingController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();        
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly mcsContext dbContext;

        public ShippingController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
            IBackgroundJobClient backgroundJobClient)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        //[HttpGet("Loading/DataGrid")]
        //public async Task<object> DataGridLoading(DataSourceLoadOptions loadOptions)
        //{
        //    return await DataSourceLoader.LoadAsync(
        //        dbContext.vw_shipping_transaction
        //            .Where(o => o.organization_id == CurrentUserContext.OrganizationId
        //                && o.is_loading == true),
        //        loadOptions);
        //}

        [HttpGet("Loading/DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGridLoading(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_shipping_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_loading == true
                        && CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin),
                    loadOptions);
            }

            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            return await DataSourceLoader.LoadAsync(dbContext.vw_shipping_transaction
                .Where(o =>
                    o.start_datetime >= dt1
                    && o.start_datetime <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId
                    && o.is_loading == true
                    && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        //[HttpGet("Unloading/DataGrid")]
        //public async Task<object> DataGridUnloading(DataSourceLoadOptions loadOptions)
        //{
        //    return await DataSourceLoader.LoadAsync(
        //        dbContext.vw_shipping_transaction
        //            .Where(o => o.organization_id == CurrentUserContext.OrganizationId
        //                && o.is_loading == false),
        //        loadOptions);
        //}

        [HttpGet("Unloading/DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGridUnloading(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_shipping_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_loading == false
                        && CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin),
                    loadOptions);
            }

            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            return await DataSourceLoader.LoadAsync(dbContext.vw_shipping_transaction
                .Where(o =>
                    o.start_datetime >= dt1
                    && o.start_datetime <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId
                    && o.is_loading == false
                    && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        //[HttpGet("DataGrid")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        //{
        //    return await DataSourceLoader.LoadAsync(
        //        dbContext.vw_shipping_transaction
        //            .Where(o => o.organization_id == CurrentUserContext.OrganizationId), 
        //        loadOptions);
        //}

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_shipping_transaction.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpGet("ShippingInstructionIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ShippingInstructionIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.shipping_instruction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.si_number != null)
                    .Select(o => new
                    {
                        Value = o.id,
                        Text = o.si_number
                    });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("Loading/InsertData")]
        public async Task<IActionResult> InsertDataLoading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var record = new shipping_transaction();

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(shipping_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

                        JsonConvert.PopulateObject(values, record);

                        if (record.berth_datetime <= record.arrival_datetime)
                            return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                        if (record.start_datetime <= record.berth_datetime)
                            return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");
                        if (record.end_datetime <= record.start_datetime)
                            return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                        if (record.unberth_datetime <= record.end_datetime)
                            return BadRequest("Cast Off DateTime must newer than Completed Loading DateTime.");
                        if (record.departure_datetime <= record.unberth_datetime)
                            return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

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
                        record.is_loading = true;
                        record.quantity = 0;
                        if (record.initial_draft_survey <= record.final_draft_survey);

                        #endregion

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
                                    record.transaction_number = $"SHL-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.shipping_transaction.Add(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    else
                    {
                        logger.Debug("User is not authorized.");
                        return Unauthorized();
                    }
                }
                catch (Exception ex)
				{
					logger.Error(ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            return Ok(record);
        }

        [HttpPost("Unloading/InsertData")]
        public async Task<IActionResult> InsertDataUnloading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var record = new shipping_transaction();

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(shipping_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

                        JsonConvert.PopulateObject(values, record);

                        if (record.berth_datetime <= record.arrival_datetime)
                            return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                        if (record.start_datetime <= record.berth_datetime)
                            return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");
                        if (record.end_datetime <= record.start_datetime)
                            return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                        if (record.unberth_datetime <= record.end_datetime)
                            return BadRequest("Cast Off DateTime must be newer than Completed Loading DateTime.");
                        if (record.departure_datetime <= record.unberth_datetime)
                            return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

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
                        record.is_loading = false;
                        record.quantity = 0;

                        #endregion

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
                                    record.transaction_number = $"SHU-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.shipping_transaction.Add(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    else
                    {
                        logger.Debug("User is not authorized.");
                        return Unauthorized();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            }

            return Ok(record);
        }

        [HttpPut("Loading/UpdateData")]
        public async Task<IActionResult> UpdateDataLoading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            shipping_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.shipping_transaction
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);

                            JsonConvert.PopulateObject(values, record);

                            if (record.berth_datetime <= record.arrival_datetime)
                                return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                            if (record.start_datetime <= record.berth_datetime)
                                return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");
                            if (record.end_datetime <= record.start_datetime)
                                return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                            if (record.unberth_datetime <= record.end_datetime)
                                return BadRequest("Cast Off DateTime must be newer than Completed Loading DateTime.");
                            if (record.departure_datetime <= record.unberth_datetime)
                                return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;
                            record.is_loading = true;

                            #region Validation

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == record.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            #endregion

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                        }
                        else
                        {
                            logger.Debug("User is not authorized.");
                            return Unauthorized();
                        }
                    }
                    else
                    {
                        logger.Debug("Record is not found.");
                        return NotFound();
                    }
                }
                catch (Exception ex)
				{
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            return Ok(record);
        }

        [HttpPut("Unloading/UpdateData")]
        public async Task<IActionResult> UpdateDataUnloading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            shipping_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.shipping_transaction
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);

                            JsonConvert.PopulateObject(values, record);

                            if (record.berth_datetime <= record.arrival_datetime)
                                return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                            if (record.start_datetime <= record.berth_datetime)
                                return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");
                            if (record.end_datetime <= record.start_datetime)
                                return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                            if (record.unberth_datetime <= record.end_datetime)
                                return BadRequest("Cast Off DateTime must be newer than Completed Loading DateTime.");
                            if (record.departure_datetime <= record.unberth_datetime)
                                return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;
                            record.is_loading = false;

                            #region Validation

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == record.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            #endregion

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                        }
                        else
                        {
                            logger.Debug("User is not authorized.");
                            return Unauthorized();
                        }
                    }
                    else
                    {
                        logger.Debug("Record is not found.");
                        return NotFound();
                    }
                }
                catch (Exception ex)
				{
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            return Ok(record);
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.shipping_transaction
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.shipping_transaction.Remove(record);
                            await dbContext.SaveChangesAsync();

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                        }
                        else
                        {
                            logger.Debug("User is not authorized.");
                            return Unauthorized();
                        }
                    }
                }
				catch (Exception ex)
				{
					logger.Error(ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            return Ok();
        }

        [HttpDelete("Loading/DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> LoadingDeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var despatch_order_id = "";

                    var shipping_transaction = dbContext.shipping_transaction.Where(o => o.id == key ).FirstOrDefault();

                    if (shipping_transaction != null)
                    {
                        despatch_order_id = shipping_transaction.despatch_order_id;
                        var unloading = dbContext.shipping_transaction.Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == false).FirstOrDefault();
                        if (unloading != null) {
                            return BadRequest("Can not be deleted since it is already have one or more transactions.");
                        }
                        
                    }

                    var record = dbContext.shipping_transaction
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.shipping_transaction.Remove(record);
                            await dbContext.SaveChangesAsync();

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                        }
                        else
                        {
                            logger.Debug("User is not authorized.");
                            return Unauthorized();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            }

            return Ok();
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
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Where(o => o.process_flow_category == Common.ProcessFlowCategory.SHIPPING)
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

        [HttpGet("SourceShiftIdLookup")]
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

        [HttpGet("DestinationShiftIdLookup")]
        public async Task<object> DestinationShiftIdLookup(DataSourceLoadOptions loadOptions)
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

        [HttpGet("SurveyIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SurveyIdLookup(string LocationId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"Location Id = {LocationId}");

            try
            {
                if (string.IsNullOrEmpty(LocationId))
                {
                    var lookup = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.is_draft_survey == true)
                        .Select(o => new { Value = o.id, Text = o.survey_number });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.survey.FromSqlRaw(
                        " SELECT s.* FROM survey s "
                        + " INNER JOIN stock_location sl ON sl.id = s.stock_location_id "
                        + " WHERE COALESCE(s.is_draft_survey, FALSE) = TRUE "
                        + " AND sl.id = {0} ", LocationId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.survey_number,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("TransportIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> TransportIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.transport
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.vehicle_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("EquipmentIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> EquipmentIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.equipment
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.equipment_code });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SourceLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.barge
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.barge.FromSqlRaw(
                        " SELECT l.* FROM barge l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("Loading/SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> LoadingSourceLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                var lookup = dbContext.port_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.stock_location_name,
                            o.product_id
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("ShipLocationIdLookup")]
        public async Task<object> ShipLocationIdLookup(string DespatchOrderId, DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"DespatchOrderId = {DespatchOrderId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                //if (!string.IsNullOrEmpty(DespatchOrderId))
                //{
                //    var lookup = dbContext.vw_shipping_transaction
                //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                //            && o.despatch_order_id == DespatchOrderId)
                //        .Select(o =>
                //            new
                //            {
                //                value = o.id,
                //                text = o.ship_location_name
                //            });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
                //else
                //{
                //    var lookup = dbContext.vw_shipping_transaction
                //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                //        .Select(o =>
                //            new
                //            {
                //                value = o.id,
                //                text = o.ship_location_name
                //            });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}

                var lookup = dbContext.vessel
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.vehicle_name
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DraftSurveyIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DraftSurveyIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.vw_draft_survey
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.survey_number });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /*
        [HttpGet("Loading/SourceLocationIdLookup")]
        public async Task<object> LoadingSourceLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.vessel
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.barge.FromSqlRaw(
                        " SELECT l.* FROM barge l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }

            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("Unloading/SourceLocationIdLookup")]
        public async Task<object> UnloadingSourceLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.vessel
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.vessel.FromSqlRaw(
                        " SELECT l.* FROM vessel l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }

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
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.vessel
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.vessel.FromSqlRaw(
                        " SELECT l.* FROM vessel l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("Loading/DestinationLocationIdLookup")]
        public async Task<object> LoadingDestinationLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.vessel
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.vessel.FromSqlRaw(
                        " SELECT l.* FROM vessel l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("Unloading/DestinationLocationIdLookup")]
        public async Task<object> UnloadingDestinationLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.port_location
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.port_location.FromSqlRaw(
                        " SELECT l.* FROM port_location l "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name,
                                o.product_id
                            });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }
        */

        [HttpGet("ProductIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProductIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");

            try
            {
                var lookup = dbContext.product
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.product_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);

                /*
                if (string.IsNullOrEmpty(ProcessFlowId))
                {
                    var lookup = dbContext.product.Select(o => new { Value = o.id, Text = o.product_name });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.product.FromSqlRaw(
                        " SELECT p.* FROM product p "
                        + " INNER JOIN stockpile_location l ON l.product_id = p.id "
                        + " WHERE l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0 "
                        + "         AND pf.id = {0} "
                        + " ) ", ProcessFlowId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.product_name
                            })
                        .Distinct();

                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                */
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

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.shipping_transaction
                    .Where(o => o.id == Id).FirstOrDefaultAsync();
                return Ok(record);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] shipping_transaction Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.shipping_transaction
                        .Where(o => o.id == Record.id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);
                        record.InjectFrom(Record);
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        #region Update stockpile state

                        var ss1 = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.ship_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (ss1 != null)
                        {
                            ss1.modified_by = CurrentUserContext.AppUserId;
                            ss1.modified_on = DateTime.Now;
                            ss1.qty_adjustment = 0;
                            ss1.transaction_datetime = record.arrival_datetime ?? record.start_datetime.Value;
                        }
                        else
                        {
                            ss1 = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.ship_location_id,
                                transaction_id = record.id,
                                qty_adjustment = 0,
                                transaction_datetime = record.arrival_datetime ?? record.start_datetime.Value
                            };

                            dbContext.stockpile_state.Add(ss1);
                        }

                        #endregion

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            ss.Update(record.ship_location_id, record.id);
                        }).Forget();

                        return Ok(record);
                    }
                    else
                    {
                        #region Add record

                        record = new shipping_transaction();
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

                        #endregion

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
                                    record.transaction_number = $"HA-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        #region Add to stockpile state

                        var ss1 = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.ship_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (ss1 == null)
                        {
                            ss1 = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.ship_location_id,
                                transaction_id = record.id,
                                qty_adjustment = 0,
                                transaction_datetime = record.arrival_datetime ?? record.start_datetime.Value
                            };

                            dbContext.stockpile_state.Add(ss1);
                        }

                        #endregion

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            ss.Update(record.ship_location_id, record.id);
                        }).Forget();

                        return Ok(record);
                    }
                }
				catch (Exception ex)
				{
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }
        }

        [HttpDelete("DeleteById/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Debug($"string Id = {Id}");

            using(var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.shipping_transaction
                        .Where(o => o.id == Id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        #region Delete stockpile state

                        var ss1 = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.ship_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (ss1 != null)
                        {
                            ss1.qty_in = null;
                            ss1.qty_out = null;
                            ss1.qty_adjustment = null;
                        }

                        var details = await dbContext.shipping_transaction_detail
                            .Where(o => o.shipping_transaction_id == record.id)
                            .ToListAsync();
                        foreach (var detail in details)
                        {
                            var ss2 = await dbContext.stockpile_state
                                .Where(o => (o.stockpile_location_id == record.ship_location_id
                                    || o.stockpile_location_id == detail.detail_location_id)
                                    && o.transaction_id == detail.id)
                                .FirstOrDefaultAsync();
                            if (ss2 != null)
                            {
                                ss2.qty_in = null;
                                ss2.qty_out = null;
                                ss2.qty_adjustment = null;
                            }
                        }

                        #endregion

                        dbContext.shipping_transaction.Remove(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            foreach (var detail in details)
                            {
                                ss.Update(record.ship_location_id, detail.id);
                                ss.Update(detail.detail_location_id, detail.id);
                            }
                        }).Forget();
                    }

                    return Ok();
                }
				catch (Exception ex)
				{
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }
        }

        [HttpPost("Loading/UploadDocument")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> LoadingUploadDocument([FromBody] dynamic FileDocument)
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
                bool isError = false;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    /*var accounting_period_id = "";
                    var accounting_period = dbContext.accounting_period
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.accounting_period_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).ToLower()).FirstOrDefault();
                    if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();*/

                    var process_flow_id = "";
                    var process_flow = dbContext.process_flow
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.process_flow_code.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower().Trim()).FirstOrDefault();
                    if (process_flow != null) process_flow_id = process_flow.id.ToString();

                    var source_location_id = "";
                    var barge = dbContext.barge
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.vehicle_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower().Trim()).FirstOrDefault();
                    if (barge != null) source_location_id = barge.id.ToString();

                    var ship_location_id = "";
                    var vessel = dbContext.vessel
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.vehicle_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower().Trim()).FirstOrDefault();
                    if (vessel != null)
                    {
                        ship_location_id = vessel.id.ToString();
                    }
                    /*else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Vessel Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }*/

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(5))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(8))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_code == PublicFunctions.IsNullCell(row.GetCell(6))).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    /*var survey_id = "";
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number == PublicFunctions.IsNullCell(row.GetCell(14))).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString();*/

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_order_number == PublicFunctions.IsNullCell(row.GetCell(1))).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var TransactionNumber = "";
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
                                    cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    TransactionNumber = $"SHL-{DateTime.Now:yyyyMMdd}-{r}";
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
                        TransactionNumber = PublicFunctions.IsNullCell(row.GetCell(0));

                    var record = dbContext.shipping_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                            && o.transaction_number.ToLower() == TransactionNumber.ToLower()
                            && o.is_loading == true)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.ship_location_id = ship_location_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        //record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                       // record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(19)); kol++;
                        record.draft_survey_number = PublicFunctions.IsNullCell(row.GetCell(20)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(9), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(10), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(10)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(12), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(13), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(14), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(18)); kol++;

                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.start_datetime <= record.berth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shipping_transaction();
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
                        record.is_loading = true;

                        record.transaction_number = TransactionNumber; kol++;
                       // record.accounting_period_id = accounting_period_id; kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.ship_location_id = ship_location_id; kol++;
                        record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                       //record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(19)); kol++;
                        record.draft_survey_number = PublicFunctions.IsNullCell(row.GetCell(20)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(9), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(10), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(10)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(12), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(13), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(14), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Loading Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(18)); kol++;

                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }

                        if (record.start_datetime <= record.berth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }

                        dbContext.shipping_transaction.Add(record);
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

            sheet = wb.GetSheetAt(1); //****************** detail sheet
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var shipping_transaction_id = "";
                    var shipping_transaction = dbContext.shipping_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
                    if (shipping_transaction != null)
                        shipping_transaction_id = shipping_transaction.id.ToString();
                    else continue;

                    var detail_location_id = "";
                    var barge = dbContext.barge
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).ToLower()).FirstOrDefault();
                    if (barge != null) detail_location_id = barge.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(6))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    string equipment_id = null;
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(7)).ToLower()).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    string survey_id = null;
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(9)).ToLower()).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString();

                    var record = dbContext.shipping_transaction_detail
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number == PublicFunctions.IsNullCell(row.GetCell(0)))
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.shipping_transaction_id = shipping_transaction_id; kol++;
                        record.detail_location_id = detail_location_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;
                        record.start_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.end_datetime = PublicFunctions.Tanggal(row.GetCell(4)); kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++; ; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shipping_transaction_detail();
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

                        record.transaction_number = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.shipping_transaction_id = shipping_transaction_id; kol++;
                        record.detail_location_id = detail_location_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;;
                        record.start_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.end_datetime = PublicFunctions.Tanggal(row.GetCell(4)); kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++; ; kol++;

                        dbContext.shipping_transaction_detail.Add(record);
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
                HttpContext.Session.SetString("filename", "ShippingLoading");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpPost("Unloading/UploadDocument")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UnloadingUploadDocument([FromBody] dynamic FileDocument)
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
                bool isError=false;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var process_flow_id = "";
                    var process_flow = dbContext.process_flow
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.process_flow_code == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()).FirstOrDefault();
                    if (process_flow != null) process_flow_id = process_flow.id.ToString();

                    var ship_location_id = "";
                    var vessel = dbContext.vessel
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.vehicle_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                    if (vessel != null)
                    {
                        ship_location_id = vessel.id.ToString();
                    }
                    /*else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Vessel Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }*/

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (product != null)product_id = product.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(7))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_code == PublicFunctions.IsNullCell(row.GetCell(5))).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_order_number == PublicFunctions.IsNullCell(row.GetCell(1))).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var destination_location_id = "";
                    var port_location = dbContext.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(17)).ToLower().Trim())
                        .FirstOrDefault();
                    if (port_location != null) destination_location_id = port_location.id.ToString();

                    var TransactionNumber = "";
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
                                    cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    TransactionNumber = $"SHU-{DateTime.Now:yyyyMMdd}-{r}";
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
                        TransactionNumber = PublicFunctions.IsNullCell(row.GetCell(0));

                    var record = dbContext.shipping_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                            && o.transaction_number.ToLower() == TransactionNumber.ToLower()
                            && o.is_loading == false)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.ship_location_id = ship_location_id; kol++;
                        //record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        //record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(10)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(19)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(9), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(11), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(12), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(13), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(14), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        record.destination_location_id = destination_location_id; kol++;
                        record.distance = PublicFunctions.Desimal(row.GetCell(18)); kol++;

                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.start_datetime <= record.berth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shipping_transaction();
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
                        record.is_loading = false;

                        record.transaction_number = TransactionNumber; kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.ship_location_id = ship_location_id; kol++;
                        //record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(6)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        //record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(10)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(19)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(9), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(9)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(11), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(12), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(12)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(13), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(13)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(14), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(14)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        record.destination_location_id = destination_location_id; kol++;
                        record.distance = PublicFunctions.Desimal(row.GetCell(18)); kol++;

                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.start_datetime <= record.berth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }

                        dbContext.shipping_transaction.Add(record);
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

            sheet = wb.GetSheetAt(1); //****************** detail sheet
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var shipping_transaction_id = "";
                    var shipping_transaction = dbContext.shipping_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
                    if (shipping_transaction != null) shipping_transaction_id = shipping_transaction.id.ToString();

                    var detail_location_id = "";
                    var port_location = dbContext.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).ToLower()).FirstOrDefault();
                    if (port_location != null) detail_location_id = port_location.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(6))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    string equipment_id = null;
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(7)).ToLower()).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    string survey_id = null;
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(9)).ToLower()).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString();

                    var record = dbContext.shipping_transaction_detail
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number == PublicFunctions.IsNullCell(row.GetCell(0)))
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.shipping_transaction_id = shipping_transaction_id; kol++;
                        record.detail_location_id = detail_location_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;
                        record.start_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.end_datetime = PublicFunctions.Tanggal(row.GetCell(4)); kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++; ; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shipping_transaction_detail();
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

                        record.transaction_number = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.shipping_transaction_id = shipping_transaction_id; kol++;
                        record.detail_location_id = detail_location_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(2)); kol++; ;
                        record.start_datetime = PublicFunctions.Tanggal(row.GetCell(3)); kol++;
                        record.end_datetime = PublicFunctions.Tanggal(row.GetCell(4)); kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(8)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(11)); kol++; ; kol++;

                        dbContext.shipping_transaction_detail.Add(record);
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
                HttpContext.Session.SetString("filename", "ShippingUnloading");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpGet("QualitySamplingIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> QualitySamplingIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                //var lookup = dbContext.quality_sampling
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                //    .Select(o => new { Value = o.id, Text = o.sampling_number });

                var lookup = dbContext.quality_sampling.FromSqlRaw(
                        "select * from quality_sampling where id not in " +
                        "(select quality_sampling_id from shipping_transaction where quality_sampling_id is not null)"
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

        [HttpGet("ByShippingTransactionId/{Id}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ByShippingTransactionId(string Id, DataSourceLoadOptions loadOptions)
        {
            var record = dbContext.vw_shipping_transaction.Where(o => o.id == Id).FirstOrDefault();
            var quality_sampling_id = "";
            if (record != null) quality_sampling_id = record.quality_sampling_id;

            return await DataSourceLoader.LoadAsync(dbContext.vw_quality_sampling_analyte
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.quality_sampling_id == quality_sampling_id),
                loadOptions);
        }

        [HttpGet("Unloading/DestinationLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UnloadingDestinationLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.port_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.stock_location_name
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

    }
}
