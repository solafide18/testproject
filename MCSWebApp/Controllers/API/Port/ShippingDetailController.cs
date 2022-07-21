using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class ShippingDetailController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ShippingDetailController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
            IBackgroundJobClient backgroundJobClient)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpGet("ByShippingId/{Id}")]
        public async Task<object> ByShippingIdLoading(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_shipping_transaction_detail
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.shipping_transaction_id == Id),
                loadOptions);
        }

        [HttpPost("Loading/InsertData")]
        public async Task<IActionResult> InsertDataLoading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            shipping_transaction_detail record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(shipping_transaction_detail),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

                        record = new shipping_transaction_detail();
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

                        var shippingTx = await dbContext.shipping_transaction
                            .Where(o => o.id == record.shipping_transaction_id)
                            .FirstOrDefaultAsync();
                        if (shippingTx != null)
                        {
                            if (!shippingTx.is_loading)
                            {
                                return BadRequest("Invalid ship loading transaction");
                            }
                        }
                        else
                        {
                            return BadRequest("Invalid ship loading transaction");
                        }

                        #endregion

                        #region Validation

                        // Must be in open accounting period
                        var ap1 = await dbContext.accounting_period
                            .Where(o => o.id == shippingTx.accounting_period_id)
                            .FirstOrDefaultAsync();
                        if (ap1 != null && (ap1?.is_closed ?? false))
                        {
                            return BadRequest("Data update is not allowed");
                        }

                        // Source location != destination location
                        if (record.detail_location_id == shippingTx.ship_location_id)
                        {
                            return BadRequest("Source location must be different from destination location");
                        }

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

                        dbContext.shipping_transaction_detail.Add(record);
                        await dbContext.SaveChangesAsync();

                        #region Add sum to shipping_transaction

                        var sum = await dbContext.shipping_transaction_detail
                            .Where(o => o.shipping_transaction_id == shippingTx.id)
                            .SumAsync(o => o.quantity);
                        shippingTx.quantity = sum;
                        await dbContext.SaveChangesAsync();

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
                    var _record = new DataAccess.Repository.shipping_transaction_detail();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ShippingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPut("Loading/UpdateData")]
        public async Task<IActionResult> UpdateDataLoading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            shipping_transaction_detail record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.shipping_transaction_detail
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            JsonConvert.PopulateObject(values, record);

                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            var shippingTx = await dbContext.shipping_transaction
                                .Where(o => o.id == record.shipping_transaction_id)
                                .FirstOrDefaultAsync();
                            if (!shippingTx.is_loading)
                            {
                                return BadRequest("Invalid ship loading transaction");
                            }

                            #region Validation

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == shippingTx.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            // Source location != destination location
                            if (record.detail_location_id == shippingTx.ship_location_id)
                            {
                                return BadRequest("Source location must be different from destination location");
                            }

                            #endregion

                            #region Get transaction number

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
                                            cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                            var r = await cmd.ExecuteScalarAsync();
                                            record.transaction_number = $"SH-{DateTime.Now:yyyyMMdd}-{r}";
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

                            #region Add sum to shipping_transaction

                            var sum = await dbContext.shipping_transaction_detail
                                .Where(o => o.shipping_transaction_id == shippingTx.id)
                                .SumAsync(o => o.quantity);
                            shippingTx.quantity = sum;
                            await dbContext.SaveChangesAsync();

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
                    var _record = new DataAccess.Repository.shipping_transaction_detail();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ShippingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPost("Unloading/InsertData")]
        public async Task<IActionResult> InsertDataUnloading([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            shipping_transaction_detail record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (await mcsContext.CanCreate(dbContext, nameof(shipping_transaction_detail),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        #region Add record

                        record = new shipping_transaction_detail();
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

                        var shippingTx = await dbContext.shipping_transaction
                            .Where(o => o.id == record.shipping_transaction_id)
                            .FirstOrDefaultAsync();
                        if (shippingTx.is_loading)
                        {
                            return BadRequest("Invalid ship unloading transaction");
                        }

                        #endregion

                        #region Validation

                        // Must be in open accounting period
                        var ap1 = await dbContext.accounting_period
                            .Where(o => o.id == shippingTx.accounting_period_id)
                            .FirstOrDefaultAsync();
                        if (ap1 != null && (ap1?.is_closed ?? false))
                        {
                            return BadRequest("Data update is not allowed");
                        }

                        // Source location != destination location
                        if (record.detail_location_id == shippingTx.ship_location_id)
                        {
                            return BadRequest("Source location must be different from destination location");
                        }

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

                        dbContext.shipping_transaction_detail.Add(record);
                        await dbContext.SaveChangesAsync();

                        #region Add sum to shipping_transaction

                        var sum = await dbContext.shipping_transaction_detail
                            .Where(o => o.shipping_transaction_id == shippingTx.id)
                            .SumAsync(o => o.quantity);
                        shippingTx.quantity = sum;
                        await dbContext.SaveChangesAsync();

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
                    var _record = new DataAccess.Repository.shipping_transaction_detail();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ShippingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPut("Unloading/UpdateData")]
        public async Task<IActionResult> UpdateDataUnloading([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            shipping_transaction_detail record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.shipping_transaction_detail
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            JsonConvert.PopulateObject(values, record);

                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            var shippingTx = await dbContext.shipping_transaction
                                .Where(o => o.id == record.shipping_transaction_id)
                                .FirstOrDefaultAsync();
                            if (shippingTx.is_loading)
                            {
                                return BadRequest("Invalid ship unloading transaction");
                            }

                            #region Validation

                            // Must be in open accounting period
                            var ap1 = await dbContext.accounting_period
                                .Where(o => o.id == shippingTx.accounting_period_id)
                                .FirstOrDefaultAsync();
                            if (ap1 != null && (ap1?.is_closed ?? false))
                            {
                                return BadRequest("Data update is not allowed");
                            }

                            // Source location != destination location
                            if (record.detail_location_id == shippingTx.ship_location_id)
                            {
                                return BadRequest("Source location must be different from destination location");
                            }

                            #endregion

                            #region Get transaction number

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
                                            cmd.CommandText = $"SELECT nextval('seq_transaction_number')";
                                            var r = await cmd.ExecuteScalarAsync();
                                            record.transaction_number = $"SH-{DateTime.Now:yyyyMMdd}-{r}";
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

                            #region Add sum to shipping_transaction

                            var sum = await dbContext.shipping_transaction_detail
                                .Where(o => o.shipping_transaction_id == shippingTx.id)
                                .SumAsync(o => o.quantity);
                            shippingTx.quantity = sum;
                            await dbContext.SaveChangesAsync();

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
                    var _record = new DataAccess.Repository.shipping_transaction_detail();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ShippingTransaction.UpdateStockState(connectionString, _record));
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
            shipping_transaction_detail record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                     record = dbContext.shipping_transaction_detail
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.shipping_transaction_detail.Remove(record);
                            await dbContext.SaveChangesAsync();

                            #region Update sum of shipping_transaction

                            var shippingTx = await dbContext.shipping_transaction
                                .Where(o => o.id == record.shipping_transaction_id)
                                .FirstOrDefaultAsync();
                            if (shippingTx != null)
                            {
                                var sum = await dbContext.shipping_transaction_detail
                                    .Where(o => o.shipping_transaction_id == shippingTx.id)
                                    .SumAsync(o => o.quantity);
                                shippingTx.quantity = sum;
                                await dbContext.SaveChangesAsync();
                            }

                            #endregion

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
                    var _record = new DataAccess.Repository.shipping_transaction_detail();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ShippingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok();
        }

        //     [HttpGet("Loading/SourceLocationIdLookup")]
        //     public async Task<object> LoadingSourceLocationIdLookup(string ProcessFlowId,
        //         DataSourceLoadOptions loadOptions)
        //     {
        //         logger.Trace($"ProcessFlowId = {ProcessFlowId}");
        //         logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

        //         try
        //         {
        //             var lookup = from b in dbContext.barge
        //                          join bt in dbContext.vw_barging_transaction
        //                          on b.id equals bt.destination_location_id
        //                          select new
        //                          {
        //                              value = b.id,
        //                              text = b.vehicle_name,
        //                          };
        //             return await DataSourceLoader.LoadAsync(lookup, loadOptions);

        //             //if (string.IsNullOrEmpty(ProcessFlowId))
        //             //{
        //             //    var lookup = dbContext.barge
        //             //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
        //             //        .Select(o =>
        //             //            new
        //             //            {
        //             //                value = o.id,
        //             //                text = o.stock_location_name
        //             //            });
        //             //    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
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
        //             //                text = o.stock_location_name
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

        [HttpGet("Loading/SourceLocationIdLookup")]
        public async Task<object> LoadingSourceLocationIdLookup(string DespatchOrderId, DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"DespatchOrderId = {DespatchOrderId}");
            logger.Trace($"loadOptions = {JsonConvert.SerializeObject(loadOptions)}");

            try
            {
                var lookup = from b in dbContext.barge
                             join bt in dbContext.vw_barging_transaction
                             on b.id equals bt.destination_location_id
                             where b.organization_id == CurrentUserContext.OrganizationId
                                && bt.despatch_order_id == DespatchOrderId
                             select new
                             {
                                 value = b.id,
                                 text = b.vehicle_name,
                             };
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);

                //if (!string.IsNullOrEmpty(DespatchOrderId))
                //{
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
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name
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
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                        + "         AND pf.id = {1} "
                        + " ) ", 
                          CurrentUserContext.OrganizationId, ProcessFlowId
                        )
                        .Select(o =>
                            new
                            {
                                value = o.id,
                                text = o.stock_location_name
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

        [HttpGet("ShipShiftIdLookup")]
        public async Task<object> ShipShiftIdLookup(DataSourceLoadOptions loadOptions)
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

        [HttpGet("DetailShiftIdLookup")]
        public async Task<object> DetailShiftIdLookup(DataSourceLoadOptions loadOptions)
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
                        .Select(o => new { value = o.id, text = o.survey_number });
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
                                text = o.survey_number
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
    }
}
