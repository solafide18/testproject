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

namespace MCSWebApp.Controllers.API.Mining
{
    [Route("api/Mining/[controller]")]
    [ApiController]
    public class ProcessingController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ProcessingController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
            IBackgroundJobClient backgroundJobClient)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.processing_transaction
                .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
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
                return await DataSourceLoader.LoadAsync(dbContext.processing_transaction
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

            return await DataSourceLoader.LoadAsync(dbContext.processing_transaction
                .Where(o =>
                    o.loading_datetime >= dt1
                    && o.loading_datetime <= dt2
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
                dbContext.processing_transaction
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            processing_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
					if (await mcsContext.CanCreate(dbContext, nameof(processing_transaction),
						CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
					{
                        record = new processing_transaction();
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

                        #region Validation

                        if (record.loading_datetime > record.unloading_datetime)
                            return BadRequest("Loading Date tidak boleh melampaui Unloading Date");

                      //  if (record.source_uom_id != record.destination_uom_id)
                            //return BadRequest("The Source Unit must be the same as the Destination Unit");

                        //if (record.unloading_quantity > record.loading_quantity)
                           // return BadRequest("The Unloading Quantity must not exceed the Loading Quantity");

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
                                if (record.loading_quantity != null)
                                {
                                    if ((decimal)(tr1?.capacity ?? 0) < record.loading_quantity)
                                    {
                                        return BadRequest("Transport capacity is less than loading quantity");
                                    }
                                }

                                if (record.unloading_quantity != null)
                                {
                                    if ((decimal)(tr1?.capacity ?? 0) < record.unloading_quantity)
                                    {
                                        return BadRequest("Transport capacity is less than unloading quantity");
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Get transcation number

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
                                    record.transaction_number = $"PC-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.processing_transaction.Add(record);

                        #region Calculate actual progress claim

                        if (!string.IsNullOrEmpty(record.progress_claim_id))
                        {
                            var pc = await dbContext.progress_claim
                                .Where(o => o.id == record.progress_claim_id)
                                .FirstOrDefaultAsync();
                            if (pc != null)
                            {
                                var actualQty = await dbContext.processing_transaction
                                    .Where(o => o.progress_claim_id == pc.id)
                                    .SumAsync(o => o.unloading_quantity);
                                pc.actual_quantity = actualQty;
                            }
                        }

                        #endregion

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                        success = true;
					}
					else
					{
						return BadRequest("User is not authorized.");
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
                    var _record = new DataAccess.Repository.processing_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ProcessingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok(record);
        }

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var success = false;
            processing_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.processing_transaction
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

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            #region Validation
                            if (record.loading_datetime > record.unloading_datetime)
                                return BadRequest("Loading Date tidak boleh melampaui Unloading Date");

                            //if (record.source_uom_id != record.destination_uom_id)
                              //  return BadRequest("The Source Unit must be the same as the Destination Unit");

                            if (record.source_uom_id != record.destination_uom_id)
                                return BadRequest("The Source Unit must be the same as the Destination Unit");

                           // if (record.unloading_quantity > record.loading_quantity)
                             //   return BadRequest("The Unloading Quantity must not exceed the Loading Quantity");

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
                                    if (record.loading_quantity != null)
                                    {
                                        if ((decimal)(tr1?.capacity ?? 0) < record.loading_quantity)
                                        {
                                            return BadRequest("Transport capacity is less than loading quantity");
                                        }
                                    }

                                    if (record.unloading_quantity != null)
                                    {
                                        if ((decimal)(tr1?.capacity ?? 0) < record.unloading_quantity)
                                        {
                                            return BadRequest("Transport capacity is less than unloading quantity");
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Calculate actual progress claim

                            if (!string.IsNullOrEmpty(record.progress_claim_id))
                            {
                                var pc = await dbContext.progress_claim
                                    .Where(o => o.id == record.progress_claim_id)
                                    .FirstOrDefaultAsync();
                                if (pc != null)
                                {
                                    var actualQty = await dbContext.processing_transaction
                                        .Where(o => o.progress_claim_id == pc.id)
                                        .SumAsync(o => o.unloading_quantity);
                                    pc.actual_quantity = actualQty;
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
                    var _record = new DataAccess.Repository.processing_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ProcessingTransaction.UpdateStockState(connectionString, _record));
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
            processing_transaction record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.processing_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.processing_transaction.Remove(record);

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
                    var _record = new DataAccess.Repository.processing_transaction();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.ProcessingTransaction.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
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
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.process_flow_category == Common.ProcessFlowCategory.PROCESSING)
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
        public async Task<object> SurveyIdLookup(string DestinationLocationId,
            DataSourceLoadOptions loadOptions)
        {
            logger.Trace($"DestinationLocationId = {DestinationLocationId}");

            try
            {
                if (string.IsNullOrEmpty(DestinationLocationId))
                {
                    var lookup = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && (o.is_draft_survey == null || o.is_draft_survey == false))
                        .Select(o => new { Value = o.id, Text = o.survey_number });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.survey.FromSqlRaw(
                          " SELECT s.* FROM survey s "
                        + " INNER JOIN stock_location sl ON sl.id = s.stock_location_id "
                        + " WHERE COALESCE(s.is_draft_survey, FALSE) = FALSE "
                        + " AND s.organization_id = {0} "                        
                        + " AND sl.id = {1} ",
                          CurrentUserContext.OrganizationId, DestinationLocationId
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

        [HttpGet("ProcessingCategoryIdLookup")]
        public async Task<object> ProcessingCategoryIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.processing_category
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.processing_category_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
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
                var lookup = dbContext.truck
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.vehicle_id });
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
                    var lookup = dbContext.stockpile_location
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
                    var lookup = dbContext.stockpile_location.FromSqlRaw(
                          " SELECT l.* FROM stockpile_location l "
                        + " WHERE l.organization_id = {0} "
                        + " AND l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0"
                        //+ "         AND pf.id = {1} "
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
                    var lookup = dbContext.stockpile_location
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
                    var lookup = dbContext.stockpile_location.FromSqlRaw(
                          " SELECT l.* FROM stockpile_location l "
                        + " WHERE l.organization_id = {0} "
                        + " AND l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0"
                        //+ "         AND pf.id = {1} "
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

        [HttpGet("SourceProductIdLookup")]
        public async Task<object> SourceProductIdLookup(string ProcessFlowId,
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
                    var lookup = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { Value = o.id, Text = o.product_name });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.product.FromSqlRaw(
                          " SELECT p.* FROM product p "
                        + " INNER JOIN stockpile_location l ON l.product_id = p.id "
                        + " WHERE p.organization_id = {0} "
                        + " AND l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.source_location_id in ba.id_path) > 0 "
                        + "         AND pf.id = {1} "
                        + " ) ", 
                            CurrentUserContext.OrganizationId, ProcessFlowId
                        )
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

        [HttpGet("DestinationProductIdLookup")]
        public async Task<object> DestinationProductIdLookup(string ProcessFlowId,
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
                    var lookup = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { Value = o.id, Text = o.product_name });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.product.FromSqlRaw(
                          " SELECT p.* FROM product p "
                        + " INNER JOIN stockpile_location l ON l.product_id = p.id "
                        + " WHERE p.organization_id = {0} "
                        + " AND l.business_area_id IN ( "
                        + "     SELECT ba.id FROM vw_business_area_structure ba, process_flow pf "
                        + "     WHERE position(pf.destination_location_id in ba.id_path) > 0 "
                        + "         AND pf.id = {0} "
                        + " ) ", 
                          CurrentUserContext.OrganizationId, ProcessFlowId
                        )
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

        [HttpGet("SourceUomIdLookup")]
        public async Task<object> SourceUomIdLookup(DataSourceLoadOptions loadOptions)
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

        [HttpGet("DestinationUomIdLookup")]
        public async Task<object> DestinationUomIdLookup(DataSourceLoadOptions loadOptions)
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

        [HttpGet("QualitySamplingIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> QualitySamplingIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.quality_sampling
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

        [HttpGet("ProgressClaimIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProgressClaimIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.progress_claim
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.progress_claim_name });
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
                var record = await dbContext.vw_processing_transaction
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.id == Id)
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
        public async Task<IActionResult> SaveData([FromBody] processing_transaction Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.processing_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.id == Record.id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (string.IsNullOrEmpty(record.transaction_number))
                        {
                            #region Get transcation number

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
                                        record.transaction_number = $"PC-{DateTime.Now:yyyyMMdd}-{r}";
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

                        #region Update record

                        var e = new entity();
                        e.InjectFrom(record);
                        record.InjectFrom(Record);
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

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
                            qtyOut.qty_out = record.loading_quantity;
                            qtyOut.transaction_datetime = 
                                (record.loading_datetime ?? record.unloading_datetime) ?? DateTime.Now;
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
                                qty_out = record.loading_quantity,
                                transaction_datetime =
                                    (record.loading_datetime ?? record.unloading_datetime) ?? DateTime.Now
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
                            qtyIn.qty_in = record.unloading_quantity;
                            qtyIn.transaction_datetime = 
                                (record.unloading_datetime ?? record.loading_datetime) ?? DateTime.Now;
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
                                qty_in = record.unloading_quantity,
                                transaction_datetime =
                                    (record.unloading_datetime ?? record.loading_datetime) ?? DateTime.Now
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

                        record = new processing_transaction();
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

                        #region Get transcation number

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
                                    record.transaction_number = $"PC-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }

                        #endregion

                        dbContext.processing_transaction.Add(record);

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
                                qty_out = record.loading_quantity,
                                transaction_datetime =
                                    (record.loading_datetime ?? record.unloading_datetime) ?? DateTime.Now
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
                                qty_in = record.unloading_quantity,
                                transaction_datetime = 
                                    (record.unloading_datetime ?? record.loading_datetime) ?? DateTime.Now
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
                    var record = dbContext.processing_transaction
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.id == Id)
                        .FirstOrDefault();
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

                        dbContext.processing_transaction.Remove(record);

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
            var sheet = importer.Take<processing_transaction>(0);
            foreach (var item in sheet)
            {
                var row = item.Value;
                i++;
                try
                {
                    var record = dbContext.processing_transaction.Where(o => o.organization_id == row.organization_id
                    && o.transaction_number == row.transaction_number).FirstOrDefault();
                    if (record == null)
                    {
                        row.id = Guid.NewGuid().ToString("N");
                        dbContext.processing_transaction.Add(row);
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
                HttpContext.Session.SetString("filename", "Processing");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }
        [HttpGet("ContractRefIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ContractRefIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.advance_contract
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.advance_contract_number });
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
                int kol = 2;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var process_flow_id = "";
                    var process_flow = dbContext.process_flow
                        .Where(o => o.process_flow_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                    if (process_flow != null) process_flow_id = process_flow.id.ToString();

                    string equipment_id = "";
                    var equipment = dbContext.equipment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.equipment_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower()).FirstOrDefault();
                    if (equipment != null) equipment_id = equipment.id.ToString();

                    /*var processing_category_id = "";
                    var processing_category = dbContext.processing_category
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                                    o.processing_category_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()).FirstOrDefault();
                    if (processing_category != null) processing_category_id = processing_category.id.ToString();

                    var transport_id = "";
                    var transport = dbContext.transport
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                                    o.vehicle_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                    if (transport != null) transport_id = transport.id.ToString();

                    var accounting_period_id = "";
                    var accounting_period = dbContext.accounting_period
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.accounting_period_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower()).FirstOrDefault();
                    if (accounting_period != null) accounting_period_id = accounting_period.id.ToString();*/

                    var source_location_id = "";
                    var source_location = dbContext.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stockpile_location_code == PublicFunctions.IsNullCell(row.GetCell(5))).FirstOrDefault();
                    if (source_location != null) source_location_id = source_location.id.ToString();

                    var source_shift_id = "";
                    var shift = dbContext.shift
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.shift_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()).FirstOrDefault();
                    if (shift != null) source_shift_id = shift.id.ToString();

                    var source_product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(9)).ToLower()).FirstOrDefault();
                    if (product != null) source_product_id = product.id.ToString();

                    var source_uom_id = "";
                    var source_uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol.ToLower() == PublicFunctions.IsNullCell(row.GetCell(8)).ToLower()).FirstOrDefault();
                    if (source_uom != null) source_uom_id = source_uom.id.ToString();

                    var destination_location_id = "";
                    var destination_location = dbContext.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stockpile_location_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower()).FirstOrDefault();
                    if (destination_location != null) destination_location_id = destination_location.id.ToString();

                   /* var destination_shift_id = "";
                    var destination_shift = dbContext.shift
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.shift_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(13)).ToLower()).FirstOrDefault();
                    if (destination_shift != null) destination_shift_id = destination_shift.id.ToString();

                    var destination_product_id = "";
                    var destination_product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_name == PublicFunctions.IsNullCell(row.GetCell(14))).FirstOrDefault();
                    if (destination_product != null) destination_product_id = destination_product.id.ToString();

                    var destination_uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(16))).FirstOrDefault();
                    if (uom != null) destination_uom_id = uom.id.ToString();

                    var survey_id = "";
                    var survey = dbContext.survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(17)).ToLower()).FirstOrDefault();
                    if (survey != null) survey_id = survey.id.ToString(); */

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_order_number == PublicFunctions.IsNullCell(row.GetCell(11))).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var quality_sampling_id = "";
                    var quality_sampling = dbContext.quality_sampling
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.sampling_number == PublicFunctions.IsNullCell(row.GetCell(10))).FirstOrDefault();
                    if (quality_sampling != null) quality_sampling_id = quality_sampling.id.ToString();

                    string employee_id = "";
                    var employee = dbContext.employee
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.employee_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(13)).ToLower()).FirstOrDefault();
                    if (employee != null) employee_id = employee.id.ToString();


                    /*var progress_claim_id = "";
                    var progress_claim = dbContext.progress_claim
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.progress_claim_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(19)).ToLower()).FirstOrDefault();
                    if (progress_claim != null) progress_claim_id = progress_claim.id.ToString();*/

                    var record = dbContext.processing_transaction
                        .Where(o => o.transaction_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()
							&& o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.process_flow_id = process_flow_id; kol++;
                        //record.processing_category_id = processing_category_id; kol++;
                        //record.transport_id = transport_id; kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.loading_datetime = PublicFunctions.Tanggal(row.GetCell(1)); kol++;
                        record.source_shift_id = source_shift_id; kol++;
                        record.source_product_id = source_product_id; kol++;
                        record.loading_quantity = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.source_uom_id = source_uom_id; kol++;
                        //record.unloading_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        record.destination_location_id = destination_location_id; kol++;
                        //record.destination_shift_id = destination_shift_id; kol++;
                        //record.destination_product_id = destination_product_id; kol++;
                        //record.unloading_quantity = PublicFunctions.Desimal(row.GetCell(15)); kol++;
                        //record.destination_uom_id = destination_uom_id; kol++;
                        //record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.quality_sampling_id = quality_sampling_id; kol++;
                        //record.progress_claim_id = progress_claim_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        //record.pic = PublicFunctions.IsNullCell(row.GetCell(21)); kol++;
                        record.pic = employee_id; kol++;
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new processing_transaction();
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

                        //record.transaction_number = PublicFunctions.IsNullCell(row.GetCell(0));
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
                                    record.transaction_number = $"PD-{DateTime.Now:yyyyMMdd}-{r}";
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString());
                                    return BadRequest(ex.Message);
                                }
                            }
                        }
                        #endregion

                        record.process_flow_id = process_flow_id; kol++;
                        //record.processing_category_id = processing_category_id; kol++;
                        //record.transport_id = transport_id; kol++;
                        //record.accounting_period_id = accounting_period_id; kol++;
                        record.source_location_id = source_location_id; kol++;
                        record.loading_datetime = PublicFunctions.Tanggal(row.GetCell(1)); kol++;
                        record.source_shift_id = source_shift_id; kol++;
                        record.source_product_id = source_product_id; kol++;
                        record.loading_quantity = PublicFunctions.Desimal(row.GetCell(7)); kol++;
                        record.source_uom_id = source_uom_id; kol++;
                        //record.unloading_datetime = PublicFunctions.Tanggal(row.GetCell(11)); kol++;
                        record.destination_location_id = destination_location_id; kol++;
                       // record.destination_shift_id = destination_shift_id; kol++;
                        //record.destination_product_id = destination_product_id; kol++;
                       // record.unloading_quantity = PublicFunctions.Desimal(row.GetCell(15)); kol++;
                       // record.destination_uom_id = destination_uom_id; kol++;
                       // record.survey_id = survey_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;
                        record.quality_sampling_id = quality_sampling_id; kol++;
                        //record.progress_claim_id = progress_claim_id; kol++;
                        record.note = PublicFunctions.IsNullCell(row.GetCell(14)); kol++;
                        record.pic = employee_id; kol++;

                        dbContext.processing_transaction.Add(record);
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
                HttpContext.Session.SetString("filename", "Processing");
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
