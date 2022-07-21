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
using Common;
using Microsoft.AspNetCore.Hosting;
using BusinessLogic;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Hangfire;

namespace MCSWebApp.Controllers.API.Port
{
    [Route("api/Port/[controller]")]
    [ApiController]
    public class BargingController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly mcsContext dbContext;

        public BargingController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
            IBackgroundJobClient backgroundJobClient)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
            _backgroundJobClient = backgroundJobClient;
        }

        //[HttpGet("Loading/DataGrid")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<object> DataGridLoading(DataSourceLoadOptions loadOptions)
        //{
        //    return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
        //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
        //            && o.is_loading == true), 
        //        loadOptions);
        //}

        [HttpGet("Loading/DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGridLoading(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_loading == true
                        && CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin),
                    loadOptions);
            }

            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
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
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<object> DataGridUnloading(DataSourceLoadOptions loadOptions)
        //{
        //    return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
        //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
        //            && o.is_loading == false),
        //        loadOptions);
        //}

        [HttpGet("Unloading/DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGridUnloading(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_loading == false
                        && CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin),
                    loadOptions);
            }

            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            return await DataSourceLoader.LoadAsync(dbContext.vw_barging_transaction
                .Where(o =>
                    o.start_datetime >= dt1
                    && o.start_datetime <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId
                    && o.is_loading == false
                    && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.barging_transaction.Where(o =>
                    o.organization_id == CurrentUserContext.OrganizationId
                    && o.id == Id),
                loadOptions);
        }

        [HttpPost("Loading/InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertDataLoading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            var record = new barging_transaction();

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(barging_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

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
                        record.is_loading = true;

                        #endregion

                        #region Validation

                        if (record.quantity <= 0)
                        {
                            return BadRequest("Draft Survey Quantity must be more than zero.");
                        }

                        // Source location != destination location
                        if (record.source_location_id == record.destination_location_id)
                        {
                            return BadRequest("Source location must be different from destination location");
                        }

                        // Capacity
                        if (record.transport_id != null)
                        {
                            var tr1 = await dbContext.transport
                                .Where(o => o.id == record.transport_id)
                                .FirstOrDefaultAsync();
                            if (tr1 != null)
                            {
                                if ((decimal)(tr1?.capacity ?? 0) < record.quantity)
                                {
                                    return BadRequest("Transport capacity is less than loading quantity");
                                }
                            }
                        }

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
                                    record.transaction_number = $"BGL-{DateTime.Now:yyyyMMdd}-{r}";

                                    cmd.CommandText = $" UPDATE barge SET barge_status = {(int)Common.Constants.BargeStatus.Cargo_On_Water} "
                                        + $" WHERE id = '{record.destination_location_id}' ";
                                    await cmd.ExecuteScalarAsync();
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.barging_transaction.Add(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                        success = true;
                    }
                    else
                    {
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
                    var _record = new DataAccess.Repository.barging_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BargingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPost("Unloading/InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertDataUnloading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            var record = new barging_transaction();

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(barging_transaction),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

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
                        record.is_loading = false;

                        #endregion

                        #region Validation

                        if (record.quantity <= 0)
                        {
                            return BadRequest("Draft Survey Quantity must be more than zero.");
                        }

                        // Source location != destination location
                        if (record.source_location_id == record.destination_location_id)
                        {
                            return BadRequest("Source location must be different from destination location");
                        }

                        // Capacity
                        if (record.transport_id != null)
                        {
                            var tr1 = await dbContext.transport
                                .Where(o => o.id == record.transport_id)
                                .FirstOrDefaultAsync();
                            if (tr1 != null)
                            {
                                if ((decimal)(tr1?.capacity ?? 0) < record.quantity)
                                {
                                    return BadRequest("Transport capacity is less than unloading quantity");
                                }
                            }
                        }

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
                                    record.transaction_number = $"BGU-{DateTime.Now:yyyyMMdd}-{r}";

                                    cmd.CommandText = $" UPDATE barge SET barge_status = {(int)Common.Constants.BargeStatus.Cargo_On_Water} "
                                        + $" WHERE id = '{record.destination_location_id}' ";
                                    await cmd.ExecuteScalarAsync();
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.barging_transaction.Add(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                        success = true;
                    }
                    else
                    {
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
                    var _record = new DataAccess.Repository.barging_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BargingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPut("Loading/UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateDataLoading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            barging_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.barging_transaction
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

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;
                            record.is_loading = true;

                            #region Validation

                            if (record.quantity <= 0)
                            {
                                return BadRequest("Draft Survey Quantity must be more than zero.");
                            }

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == record.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            // Source location != destination location
                            if (record.source_location_id == record.destination_location_id)
                            {
                                return BadRequest("Source location must be different from destination location");
                            }

                            // Capacity
                            if (record.transport_id != null)
                            {
                                var tr1 = await dbContext.transport
                                    .Where(o => o.id == record.transport_id)
                                    .FirstOrDefaultAsync();
                                if (tr1 != null)
                                {
                                    if ((decimal)(tr1?.capacity ?? 0) < record.quantity)
                                    {
                                        return BadRequest("Transport capacity is less than loading quantity");
                                    }
                                }
                            }

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

                            #endregion

                            #region Set barge status

                            var conn = dbContext.Database.GetDbConnection();
                            if (conn.State != System.Data.ConnectionState.Open)
                            {
                                await conn.OpenAsync();
                                if (conn.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        try
                                        {
                                            cmd.CommandText = $" UPDATE barge SET barge_status = {(int)Common.Constants.BargeStatus.Cargo_On_Water} "
                                                + $" WHERE id = '{record.destination_location_id}' ";
                                            await cmd.ExecuteScalarAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Error(ex.ToString());
                                            return BadRequest(ex.Message);
                                        }
                                    }
                                }
                            }

                            #endregion

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            success = true;
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
                    await tx.RollbackAsync();
                    logger.Error(ex.ToString());
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            if (success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.barging_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BargingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPut("Unloading/UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateDataUnloading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            barging_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.barging_transaction
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

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;
                            record.is_loading = false;

                            #region Validation

                            if (record.quantity <= 0)
                            {
                                return BadRequest("Draft Survey Quantity must be more than zero.");
                            }

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == record.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            // Source location != destination location
                            if (record.source_location_id == record.destination_location_id)
                            {
                                return BadRequest("Source location must be different from destination location");
                            }

                            // Capacity
                            if (record.transport_id != null)
                            {
                                var tr1 = await dbContext.transport
                                    .Where(o => o.id == record.transport_id)
                                    .FirstOrDefaultAsync();
                                if (tr1 != null)
                                {
                                    if ((decimal)(tr1?.capacity ?? 0) < record.quantity)
                                    {
                                        return BadRequest("Transport capacity is less than unloading quantity");
                                    }
                                }
                            }

                            if (record.berth_datetime <= record.arrival_datetime)
                                return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                            if (record.start_datetime <= record.berth_datetime)
                                return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");
                            if (record.end_datetime <= record.start_datetime)
                                return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                            if (record.unberth_datetime <= record.end_datetime)
                                return BadRequest("Cast Off DateTime must be  newer than Completed Loading DateTime.");
                            if (record.departure_datetime <= record.unberth_datetime)
                                return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

                            #endregion

                            #region Set barge status

                            var conn = dbContext.Database.GetDbConnection();
                            if (conn.State != System.Data.ConnectionState.Open)
                            {
                                await conn.OpenAsync();
                                if (conn.State == System.Data.ConnectionState.Open)
                                {
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        try
                                        {
                                            cmd.CommandText = $" UPDATE barge SET barge_status = {(int)Common.Constants.BargeStatus.Cargo_On_Water} "
                                                + $" WHERE id = '{record.destination_location_id}' ";
                                            await cmd.ExecuteScalarAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Error(ex.ToString());
                                            return BadRequest(ex.Message);
                                        }
                                    }
                                }
                            }

                            #endregion

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            success = true;
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
                    await tx.RollbackAsync();
                    logger.Error(ex.ToString());
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}                
            }

            if (success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.barging_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BargingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            var success = false;
            barging_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.barging_transaction
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.barging_transaction.Remove(record);

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            success = true;
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
                    await tx.RollbackAsync();
                    logger.Error(ex.ToString());
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }

            if (success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.barging_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.BargingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok();
        }

        [HttpGet("ProcessFlowIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProcessFlowIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.process_flow
                    .Where(o => o.process_flow_category == Common.ProcessFlowCategory.BARGING
                        && o.organization_id == CurrentUserContext.OrganizationId)
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
				logger.Error(ex.ToString());
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
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


        [HttpGet("DestinationShiftIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
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
                        + " AND s.organization_id = {0} "
                        + " AND sl.id = {1} ", 
                            CurrentUserContext.OrganizationId, LocationId
                        )
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

        [HttpGet("Loading/SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> LoadingSourceLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                //if (string.IsNullOrEmpty(ProcessFlowId))
                //{
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
                //}
                //else
                //{
                //    var lookup = dbContext.port_location.FromSqlRaw(
                //          " SELECT l.* FROM port_location l "
                //        + " WHERE l.organization_id = {0} "
                //        + " AND l.business_area_id IN ( "
                //        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                //        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                //        + "         AND pf.id = {1} "
                //        + " ) ", 
                //          CurrentUserContext.OrganizationId, ProcessFlowId
                //        )
                //        .Select(o =>
                //            new
                //            {
                //                value = o.id,
                //                text = o.stock_location_name,
                //                o.product_id
                //            });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        //     [HttpGet("Unloading/SourceLocationIdLookup")]
        //     [ApiExplorerSettings(IgnoreApi = true)]
        //     public async Task<object> UnloadingSourceLocationIdLookup(string ProcessFlowId,
        //         DataSourceLoadOptions loadOptions)
        //     {
        //         logger.Trace($"ProcessFlowId = {ProcessFlowId}");
        //         logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

        //         try
        //         {
        //             //if (string.IsNullOrEmpty(ProcessFlowId))
        //             //{
        //             //var lookup = from b in dbContext.barge
        //             //             join bt in dbContext.vw_barging_transaction
        //             //             on b.id equals bt.destination_location_id
        //             //             select new
        //             //             {
        //             //                 value = b.id,
        //             //                 text = b.vehicle_name,
        //             //             };
        //             var lookup = from b in dbContext.barge
        //                          join bt in dbContext.vw_barging_transaction
        //                          on b.id equals bt.destination_location_id
        //                          select new
        //                          {
        //                              value = b.id,
        //                              text = b.vehicle_name,
        //                          };
        //             return await DataSourceLoader.LoadAsync(lookup, loadOptions);

        //             //}
        //             //else
        //             //{
        //             //    var lookup = dbContext.barge.FromSqlRaw(
        //             //          " SELECT l.* FROM barge l "
        //             //        + " WHERE l.organization_id = {0} "
        //             //        + " AND l.business_area_id IN ( "
        //             //        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
        //             //        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
        //             //        + "         AND pf.id = {1} "
        //             //        + " ) ", 
        //             //          CurrentUserContext.OrganizationId, ProcessFlowId
        //             //        )
        //             //        .Select(o =>
        //             //            new
        //             //            {
        //             //                value = o.id,
        //             //                text = o.stock_location_name,
        //             //                o.product_id
        //             //            });
        //             //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
        //             //}

        //         }
        //         catch (Exception ex)
        //{
        //	logger.Error(ex.InnerException ?? ex);
        //             return BadRequest(ex.InnerException?.Message ?? ex.Message);
        //}
        //     }

        [HttpGet("Unloading/SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> UnloadingSourceLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");
            //public async Task<object> UnloadingSourceLocationIdLookup(string DespatchOrderId,
            //  DataSourceLoadOptions loadOptions)
            //{
            //  logger.Trace($"DespatchOrderId = {DespatchOrderId}");
            //logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {

                var lookup = dbContext.barge
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.vehicle_name,
                            o.product_id
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                /*var lookup = from b in dbContext.barge
                             join bt in dbContext.vw_barging_transaction
                             on b.id equals bt.destination_location_id
                             where b.organization_id == CurrentUserContext.OrganizationId
                                && bt.despatch_order_id == DespatchOrderId
                             select new
                             {
                                 value = b.id,
                                 text = b.vehicle_name,
                             };*/
                //vessel
                /*var lookup = dbContext.vessel
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.vehicle_name
                        });*/


                //1
                /*
                var barge = dbContext.barge
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.vehicle_name });
                var vessel = dbContext.vessel
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.vehicle_name });
                var lookup = vessel.Union(barge).OrderBy(o => o.Text);
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);

                */

                //if (!string.IsNullOrEmpty(DespatchOrderId))
                //    {
                //    var lookup = dbContext.vw_barging_transaction
                //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                //            && o.despatch_order_id == DespatchOrderId
                //            && String.IsNullOrEmpty(o.source_location_name) == false)
                //        .Select(o => new
                //        {
                //            value = o.id,
                //            text = o.source_location_name,
                //        });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
                //else
                //{
                //    var lookup = dbContext.vw_barging_transaction
                //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                //            && String.IsNullOrEmpty(o.source_location_name) == false)
                //        .Select(o => new
                //        {
                //            value = o.id,
                //            text = o.source_location_name,
                //        });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("Loading/DestinationLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> LoadingDestinationLocationIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                //if (string.IsNullOrEmpty(ProcessFlowId))
                //{
                var lookup = dbContext.barge
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text = o.vehicle_name,
                            o.product_id
                        });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
                //else
                //{
                //    var lookup = dbContext.barge.FromSqlRaw(
                //          " SELECT l.* FROM barge l "
                //        + " WHERE l.organization_id = {0} "
                //        + " AND l.business_area_id IN ( "
                //        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                //        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                //        + "         AND pf.id = {1} "
                //        + " ) ", 
                //          CurrentUserContext.OrganizationId, ProcessFlowId
                //        )
                //        .Select(o =>
                //            new
                //            {
                //                value = o.id,
                //                text = o.stock_location_name,
                //                o.product_id
                //            });
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("Unloading/DestinationLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
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
                    var lookup = dbContext.port_location.FromSqlRaw(
                          " SELECT l.* FROM port_location l "
                        + " WHERE l.organization_id = {0} "
                        + " AND l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {1} "
                        + " ) ", 
                          CurrentUserContext.OrganizationId, ProcessFlowId
                        )
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

        [HttpGet("ProductIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProductIdLookup(string ProcessFlowId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"ProcessFlowId = {ProcessFlowId}");

            try
            {
                //if (string.IsNullOrEmpty(ProcessFlowId))
                //{
                var lookup = dbContext.product
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.product_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
                //else
                //{
                //    var lookup = dbContext.product.FromSqlRaw(
                //          " SELECT p.* FROM product p "
                //        + " INNER JOIN stockpile_location l ON l.product_id = p.id "
                //        + " WHERE p.organization_id = {0} "
                //        + " AND l.business_area_id IN ( "
                //        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                //        + "     WHERE (position(pf.source_location_id in ba.id_path) > 0 "
                //        + "         OR position(pf.destination_location_id in ba.id_path) > 0) "
                //        + "         AND pf.id = {0} "
                //        + " ) ", 
                //          CurrentUserContext.OrganizationId, ProcessFlowId
                //        )
                //        .Select(o =>
                //            new
                //            {
                //                value = o.id,
                //                text = o.product_name
                //            })
                //        .Distinct();
                //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                //}
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
                var record = await dbContext.vw_barging_transaction
                    .Where(o => o.id == Id
                        && o.organization_id == CurrentUserContext.OrganizationId)
                    .FirstOrDefaultAsync();
                return Ok(record);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] barging_transaction Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.barging_transaction
                        .Where(o => o.id == Record.id
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);
                        record.InjectFrom(Record);
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        #region Validation

                        if (record.quantity <= 0)
                        {
                            return BadRequest("Draft Survey Quantity must be more than zero.");
                        }

                        // Source location != destination location
                        if (record.source_location_id == record.destination_location_id)
                        {
                            return BadRequest("Source location must be different from destination location");
                        }

                        // Capacity
                        if (record.transport_id != null)
                        {
                            var tr1 = await dbContext.transport
                                .Where(o => o.id == record.transport_id)
                                .FirstOrDefaultAsync();
                            if (tr1 != null)
                            {
                                if ((decimal)(tr1?.capacity ?? 0) < record.quantity)
                                {
                                    return BadRequest("Transport capacity is less than quantity");
                                }
                            }
                        }

                        #endregion

                        #region Update stockpile state

                        var qtyOut = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.source_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyOut != null)
                        {
                            qtyOut.modified_by = CurrentUserContext.AppUserId;
                            qtyOut.modified_on = DateTime.Now;
                            qtyOut.qty_out = record.quantity;
                            qtyOut.transaction_datetime = record.end_datetime ?? record.start_datetime;
                        }
                        else
                        {
                            qtyOut = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.source_location_id,
                                transaction_id = record.id,
                                qty_out = record.quantity,
                                transaction_datetime = record.end_datetime ?? record.start_datetime
                            };

                            dbContext.stockpile_state.Add(qtyOut);
                        }

                        var qtyIn = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.destination_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyIn != null)
                        {
                            qtyIn.modified_by = CurrentUserContext.AppUserId;
                            qtyIn.modified_on = DateTime.Now;
                            qtyIn.qty_in = record.quantity;
                            qtyIn.transaction_datetime = record.end_datetime ?? record.start_datetime;
                        }
                        else
                        {
                            qtyIn = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.destination_location_id,
                                transaction_id = record.id,
                                qty_in = record.quantity,
                                transaction_datetime = record.end_datetime ?? record.start_datetime
                            };

                            dbContext.stockpile_state.Add(qtyIn);
                        }

                        #endregion

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            ss.Update(record.source_location_id, record.id);
                            ss.Update(record.destination_location_id, record.id);
                        }).Forget();

                        return Ok(record);
                    }
                    else
                    {
                        #region Add record

                        record = new barging_transaction();
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

                        dbContext.barging_transaction.Add(record);

                        #region Add to stockpile state

                        var qtyOut = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.source_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyOut == null)
                        {
                            qtyOut = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.source_location_id,
                                transaction_id = record.id,
                                qty_out = record.quantity,
                                transaction_datetime = record.end_datetime ?? record.start_datetime
                            };

                            dbContext.stockpile_state.Add(qtyOut);
                        }

                        var qtyIn = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.destination_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyIn == null)
                        {
                            qtyIn = new stockpile_state
                            {
                                id = Guid.NewGuid().ToString("N"),
                                created_by = CurrentUserContext.AppUserId,
                                created_on = DateTime.Now,
                                is_active = true,
                                owner_id = CurrentUserContext.AppUserId,
                                organization_id = CurrentUserContext.OrganizationId,
                                stockpile_location_id = record.destination_location_id,
                                transaction_id = record.id,
                                qty_in = record.quantity,
                                transaction_datetime = record.end_datetime ?? record.start_datetime
                            };

                            dbContext.stockpile_state.Add(qtyOut);
                        }

                        #endregion

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            ss.Update(record.source_location_id, record.id);
                            ss.Update(record.destination_location_id, record.id);
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

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.barging_transaction
                        .Where(o => o.id == Id
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefaultAsync();
                    if (record != null)
                    {
                        #region Delete stockpile state

                        var qtyOut = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.source_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyOut != null)
                        {
                            qtyOut.qty_in = null;
                            qtyOut.qty_out = null;
                            qtyOut.qty_adjustment = null;
                        }

                        var qtyIn = await dbContext.stockpile_state
                            .Where(o => o.stockpile_location_id == record.destination_location_id
                                && o.transaction_id == record.id)
                            .FirstOrDefaultAsync();
                        if (qtyIn != null)
                        {
                            qtyIn.qty_in = null;
                            qtyIn.qty_out = null;
                            qtyIn.qty_adjustment = null;
                        }

                        #endregion

                        dbContext.barging_transaction.Remove(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        Task.Run(() =>
                        {
                            var ss = new BusinessLogic.Entity.StockpileState(CurrentUserContext);
                            ss.Update(record.source_location_id, record.id);
                            ss.Update(record.destination_location_id, record.id);
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
            /*string currentBarging = string.Empty;

            var barges = dbContext.barge
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                .Select(o => new { Id = o.id, Text = o.vehicle_name });*/

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                bool isError = false;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var process_flow_id = "";
                    var process_flow = dbContext.process_flow
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.process_flow_code == PublicFunctions.IsNullCell(row.GetCell(3))).FirstOrDefault();
                    if (process_flow != null)
                    {
                        process_flow_id = process_flow.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Process Flow Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    /* var accounting_period_id = "";
                     var accounting_period = dbContext.accounting_period
                         .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                             o.accounting_period_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                     if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();*/

                    var source_location_id = "";
                    var stockpile_location = dbContext.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.port_location_code.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower().Trim()).FirstOrDefault();
                    if (stockpile_location != null)
                    {
                        source_location_id = stockpile_location.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Source Location Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    var destination_location_id = "";
                    var barge = dbContext.barge
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            //o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower()).FirstOrDefault();
                            o.vehicle_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower().Trim()).FirstOrDefault();
                    if (barge != null ) {
                        destination_location_id = barge.id.ToString();
                    }
                    else 
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Barge Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }
                    /*if (barge != null)
                    {
                        destination_location_id = barge.id.ToString();
                        currentBarging = destination_location_id;
                    }
                    else
                    {
                        teks += "==>Error Line " + (i + 1) + ", Column Barge  Not Found : " + barge + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        break;

                    }*/

                    /*var shift_id = "";
                    var shift = dbContext.shift
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.shift_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(8)).ToLower()).FirstOrDefault();
                    if (shift != null) shift_id = shift.id.ToString();*/

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(11))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(10)).ToLower().Trim()).FirstOrDefault();
                    if (uom != null)
                    { 
                        uom_id = uom.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Unit Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    var equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_code == PublicFunctions.IsNullCell(row.GetCell(12))).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    var survey_id = "";
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number == PublicFunctions.IsNullCell(row.GetCell(2))).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString();

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
                                    TransactionNumber = $"BGL-{DateTime.Now:yyyyMMdd}-{r}";
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

                    var record = dbContext.barging_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower())
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        record.is_loading = true;
                        kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;
                        //record.source_shift_id = shift_id; kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(13)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(22)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(7), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;

                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }
                        
                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(18), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(19), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(20), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(21)); kol++;

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
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
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

                        /*
                        if (record.berth_datetime <= record.arrival_datetime)
                        {
                            teks += "Error in Line : " + (i + 1)+" ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }
                            //return BadRequest("Alongside DateTime must be newer than Arrival DateTime.");
                        if (record.start_datetime <= record.berth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }*/
                        //return BadRequest("Commenced Loading DateTime must be newer than Alongside DateTime.");

                        /*
                        //return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }
                        //return BadRequest("Cast Off DateTime must be  newer than Completed Loading DateTime.");
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }
                        */
                        //return BadRequest("Departure DateTime must be newer than Cast Off DateTime.");

                        await dbContext.SaveChangesAsync(); kol++;
                    }
                    else
                    {
                        record = new barging_transaction();
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

                        //record.transaction_number = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.transaction_number = TransactionNumber; kol++;

                        record.process_flow_id = process_flow_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        //record.source_shift_id = shift_id; kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(13)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(22)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        // record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(7), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;

                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;

                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;

                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                            if (record.berth_datetime <= record.arrival_datetime)
                            {
                                teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                                teks += errormessage + Environment.NewLine + Environment.NewLine;
                                gagal = true;
                                isError = true;
                            }
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                            if (record.start_datetime <= record.berth_datetime)
                            {
                                teks += "Error in Line : " + (i + 1) + " ==> Commenced Loading DateTime must be newer than Alongside DateTime" + Environment.NewLine;
                                teks += errormessage + Environment.NewLine + Environment.NewLine;
                                gagal = true;
                                isError = true;
                            }
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(18), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                            if (record.unberth_datetime <= record.end_datetime)
                            {
                                teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
                                teks += errormessage + Environment.NewLine + Environment.NewLine;
                                gagal = true;
                                isError = true;
                            }
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(19), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                            if (record.berth_datetime <= record.arrival_datetime)
                            {
                                teks += "Error in Line : " + (i + 1) + " ==> Alongside DateTime must be newer than Arrival DateTime" + Environment.NewLine;
                                teks += errormessage + Environment.NewLine + Environment.NewLine;
                                gagal = true;
                                isError = true;
                            }
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(20), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;
                            if (record.departure_datetime <= record.unberth_datetime)
                            {
                                teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                                teks += errormessage + Environment.NewLine + Environment.NewLine;
                                gagal = true;
                                isError = true;
                            }
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(21)); kol++;

                        /*
                        //return BadRequest("Completed Loading DateTime must be newer than Commenced Loading DateTime.");
                        if (record.unberth_datetime <= record.end_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }
                        //return BadRequest("Cast Off DateTime must be  newer than Completed Loading DateTime.");
                        if (record.departure_datetime <= record.unberth_datetime)
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Departure DateTime must be newer than Cast Off DateTime" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            //break;
                            isError = true;
                        }*/

                        dbContext.barging_transaction.Add(record);
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
                if (isError) break;
            }
            wb.Close();
            if (gagal)
            {
                await transaction.RollbackAsync();
                HttpContext.Session.SetString("errormessage", teks);
                HttpContext.Session.SetString("filename", "BargeLoading");
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
                bool isError = false;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var process_flow_id = "";
                    var process_flow = dbContext.process_flow
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                        o.process_flow_code.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower().Trim()).FirstOrDefault();
                    if (process_flow != null)
                    {
                        process_flow_id = process_flow.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Process Flow Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    /* var accounting_period_id = "";
                     var accounting_period = dbContext.accounting_period
                         .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                             o.accounting_period_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                     if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();*/

                    var source_location_id = "";
                    var stockpile_location = dbContext.barge
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower().Trim()).FirstOrDefault();
                    if (stockpile_location != null)
                    {
                        source_location_id = stockpile_location.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Barge Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    var destination_location_id = "";
                    var port_location = dbContext.port_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.port_location_code.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower().Trim()).FirstOrDefault();
                    if (port_location != null)
                    {
                        destination_location_id = port_location.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Destination Location Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    /* var shift_id = "";
                     var shift = dbContext.shift
                         .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                             o.shift_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(8)).ToLower()).FirstOrDefault();
                     if (shift != null) shift_id = shift.id.ToString();*/

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(11))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol.ToLower().Trim() == PublicFunctions.IsNullCell(row.GetCell(10)).ToLower().Trim()).FirstOrDefault();
                    if (uom != null)
                    {
                        uom_id = uom.id.ToString();
                    }
                    else
                    {
                        teks += "Error in Line : " + (i + 1) + " ==> Unit Not Found" + Environment.NewLine;
                        teks += errormessage + Environment.NewLine + Environment.NewLine;
                        gagal = true;
                        // break;
                        isError = true;

                    }

                    var equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.equipment_code == PublicFunctions.IsNullCell(row.GetCell(12))).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    var survey_id = "";
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number == PublicFunctions.IsNullCell(row.GetCell(2))).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString();

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
                                    TransactionNumber = $"BGU-{DateTime.Now:yyyyMMdd}-{r}";
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

                    var record = dbContext.barging_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.transaction_number.ToLower() == TransactionNumber.ToLower())
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        record.is_loading = true;
                        kol++;
                        record.process_flow_id = process_flow_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        //record.source_shift_id = shift_id; kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(13)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(22)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        //record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;

                        if (PublicFunctions.IsNullCell(row.GetCell(7), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(18), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(19), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(20), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(21)); kol++;

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
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
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

                        await dbContext.SaveChangesAsync(); kol++;
                    }
                    else
                    {
                        record = new barging_transaction();
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
                        record.process_flow_id = process_flow_id; kol++;
                        record.reference_number = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.destination_location_id = destination_location_id; kol++;
                        //record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        //record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        //record.source_shift_id = shift_id; kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(9)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.equipment_id = equipment_id; kol++;
                        record.hour_usage = PublicFunctions.Desimal(row.GetCell(13)); kol++;
                        record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        record.ref_work_order = PublicFunctions.IsNullCell(row.GetCell(22)); kol++;
                        //record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        //record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        // record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        //record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        //record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        //record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;
                        if (PublicFunctions.IsNullCell(row.GetCell(7), "") == "")
                        {
                            record.initial_draft_survey = null;
                        }
                        else
                        {
                            record.initial_draft_survey = PublicFunctions.Tanggal(row.GetCell(7)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(8), "") == "")
                        {
                            record.final_draft_survey = null;
                        }
                        else
                        {
                            record.final_draft_survey = PublicFunctions.Tanggal(row.GetCell(8)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(15), "") == "")
                        {
                            record.arrival_datetime = null;
                        }
                        else
                        {
                            record.arrival_datetime = PublicFunctions.Tanggal(row.GetCell(15)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(16), "") == "")
                        {
                            record.berth_datetime = null;
                        }
                        else
                        {
                            record.berth_datetime = PublicFunctions.Tanggal(row.GetCell(16)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(17), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Commenced Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.start_datetime = PublicFunctions.Tanggal(row.GetCell(17)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(18), "") == "")
                        {
                            teks += "Error in Line : " + (i + 1) + " ==> Completed Datetime Not Found" + Environment.NewLine;
                            teks += errormessage + Environment.NewLine + Environment.NewLine;
                            gagal = true;
                            isError = true;
                        }
                        else
                        {
                            record.end_datetime = PublicFunctions.Tanggal(row.GetCell(18)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(19), "") == "")
                        {
                            record.unberth_datetime = null;
                        }
                        else
                        {
                            record.unberth_datetime = PublicFunctions.Tanggal(row.GetCell(19)); kol++;
                        }

                        if (PublicFunctions.IsNullCell(row.GetCell(20), "") == "")
                        {
                            record.departure_datetime = null;
                        }
                        else
                        {
                            record.departure_datetime = PublicFunctions.Tanggal(row.GetCell(20)); kol++;
                        }

                        record.distance = PublicFunctions.Desimal(row.GetCell(21)); kol++;

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
                            teks += "Error in Line : " + (i + 1) + " ==> Cast Off DateTime must be  newer than Completed Loading DateTime" + Environment.NewLine;
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

                        dbContext.barging_transaction.Add(record);
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
                HttpContext.Session.SetString("filename", "BargeLoading");
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
                var lookup = dbContext.quality_sampling.FromSqlRaw(
                        "select * from quality_sampling where id not in " +
                        "(select quality_sampling_id from barging_transaction where quality_sampling_id is not null)"
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

        [HttpGet("ByBargingTransactionId/{Id}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ByBargingTransactionId(string Id, DataSourceLoadOptions loadOptions)
        {
            var record = dbContext.vw_barging_transaction.Where(o => o.id == Id).FirstOrDefault();
            var quality_sampling_id = "";
            if (record != null) quality_sampling_id = record.quality_sampling_id;

            return await DataSourceLoader.LoadAsync(dbContext.vw_quality_sampling_analyte
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.quality_sampling_id == quality_sampling_id),
                loadOptions);
        }

        [HttpGet("GetBargingTransactionLoading/{despatchOrderId}/{destinationLocationId}")]
        public async Task<ApiResponse> GetBargingTransactionLoadingByDespatchOrderId(string despatchOrderId, string destinationLocationId)
        {
            var result = new ApiResponse();
            result.Status.Success = true;
            try
            {
                var record = await dbContext.barging_transaction
                    .Where(r => r.organization_id == CurrentUserContext.OrganizationId && 
                                r.despatch_order_id == despatchOrderId &&
                                r.destination_location_id == destinationLocationId &&
                                r.is_loading == true).FirstOrDefaultAsync();
                result.Data = record;
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                result.Status.Success = false;
                result.Status.Message = ex.Message;
            }
            return result;
        }

        [HttpGet("GetBargingTransactionUnloading/{despatchOrderId}/{destinationLocationId}")]
        public async Task<ApiResponse> GetBargingTransactionUnloadingByDespatchOrderId(string despatchOrderId, string destinationLocationId)
        {
            var result = new ApiResponse();
            result.Status.Success = true;
            try
            {
                var record = await dbContext.barging_transaction
                    .Where(r => r.organization_id == CurrentUserContext.OrganizationId &&
                                r.despatch_order_id == despatchOrderId &&
                                r.destination_location_id == destinationLocationId
                                ).FirstOrDefaultAsync();
                result.Data = record;
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                result.Status.Success = false;
                result.Status.Message = ex.Message;
            }
            return result;
        }

        [HttpGet("FetchBargingTransactionLoadingIntoShppingTransactionDetail/{despatchOrderId}/{shippingTransactionId}")]
        public async Task<ApiResponse> FetchBargingTransactionLoadingIntoShppingTransactionDetail(string despatchOrderId, string shippingTransactionId)
        {
            var result = new ApiResponse();
            result.Status.Success = true;
            try
            {
                var bargingTransactions = await dbContext.barging_transaction
                    .Where(r => r.organization_id == CurrentUserContext.OrganizationId &&
                                r.despatch_order_id == despatchOrderId &&
                                r.is_loading == true).ToListAsync();

                if (bargingTransactions == null || bargingTransactions.Count <= 0)
                {

                }


                using (var tx = await dbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var item in bargingTransactions)
                        {
                            if (result.Status.Success)
                            {
                                var checkDataByBargingTransactionId = await dbContext.shipping_transaction_detail
                                    .Where(r => r.shipping_transaction_id == shippingTransactionId && r.barging_transaction_id == item.id).ToListAsync();
                                if (checkDataByBargingTransactionId.Count == 0)
                                {
                                    var newRecord = new shipping_transaction_detail();
                                
                                    if (await mcsContext.CanCreate(dbContext, nameof(barging_transaction),
                                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                                    {
                                        #region Add record

                                        newRecord.id = Guid.NewGuid().ToString("N");
                                        newRecord.created_by = CurrentUserContext.AppUserId;
                                        newRecord.created_on = DateTime.Now;
                                        newRecord.modified_by = null;
                                        newRecord.modified_on = null;
                                        newRecord.is_active = true;
                                        newRecord.is_default = null;
                                        newRecord.is_locked = null;
                                        newRecord.entity_id = null;
                                        newRecord.owner_id = CurrentUserContext.AppUserId;
                                        newRecord.organization_id = CurrentUserContext.OrganizationId;

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
                                                    newRecord.transaction_number = $"SHL-{DateTime.Now:yyyyMMdd}-{r}";
                                                }
                                                catch (Exception ex)
                                                {
                                                    await tx.RollbackAsync();
                                                    result.Status.Success = false;
                                                    logger.Error(ex.ToString());
                                                    result.Status.Message = ex.Message;
                                                }
                                            }
                                        }

                                        #endregion

                                        newRecord.shipping_transaction_id = shippingTransactionId;
                                        newRecord.uom_id = item.uom_id;
                                        newRecord.quantity = item.quantity;
                                        newRecord.detail_location_id = item.destination_location_id;
                                        newRecord.barging_transaction_id = item.id;
                                        dbContext.shipping_transaction_detail.Add(newRecord);

                                        await dbContext.SaveChangesAsync();
                                        result.Status.Success &= true;
                                    }
                                    else
                                    {
                                        result.Status.Success = false;
                                        result.Status.Message = "User is not authorized.";
                                    }
                                }
                            }
                        }



                        if (result.Status.Success)
                        {
                            await tx.CommitAsync();
                            result.Status.Message = "Ok";
                        }
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        logger.Error(ex.ToString());
                        result.Status.Success = false;
                        result.Status.Message = ex.InnerException?.Message ?? ex.Message;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                result.Status.Success = false;
                result.Status.Message = ex.Message;
            }
            return result;
        }

    }
}
