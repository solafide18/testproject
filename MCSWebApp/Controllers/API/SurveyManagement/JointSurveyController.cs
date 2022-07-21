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

namespace MCSWebApp.Controllers.API.SurveyManagement
{
    [Route("api/SurveyManagement/[controller]")]
    [ApiController]
    public class JointSurveyController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public JointSurveyController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
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
            return await DataSourceLoader.LoadAsync(dbContext.vw_joint_survey
                .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_joint_survey.Where(o => o.id == Id
                    && o.organization_id == CurrentUserContext.OrganizationId),
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
					if (await mcsContext.CanCreate(dbContext, nameof(joint_survey),
						CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
					{
                        var record = new joint_survey();
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

                        if (string.IsNullOrEmpty(record.join_survey_number))
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
                                        record.join_survey_number = $"JS-{DateTime.Now:yyyyMMdd}-{r}";
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

                        dbContext.joint_survey.Add(record);

                        await dbContext.SaveChangesAsync();
                        await tx.CommitAsync();

                        return Ok(record);
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
                    var record = dbContext.joint_survey
                        .Where(o => o.id == key
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            if (record.approved_by != null)
                            {
                                return BadRequest("Record cannot be update. Status is Closed.");
                            }


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
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            var success = false;
            joint_survey record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.joint_survey
                        .Where(o => o.id == key
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            if (record.approved_by != null)
                            {
                                return BadRequest("Record cannot be updated. Status is Closed.");
                            }

                            dbContext.joint_survey.Remove(record);
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
                    var _record = new DataAccess.Repository.joint_survey();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.JointSurvey.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok();
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

        [HttpGet("ProgressClaimIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ProgressClaimIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.progress_claim
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.progress_claim_name, o.advance_contract_id });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("AdvanceContractReferenceIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> AdvanceContractReferenceIdLookup(string AdvanceContractId, DataSourceLoadOptions loadOptions)
        {
            try
            {
                if (string.IsNullOrEmpty(AdvanceContractId))
                {
                    var lookup = dbContext.advance_contract_reference
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.name != null)
                        .Select(o => new { Value = o.id, Text = o.name + (" - " + o.progress_claim_name ?? "") });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.advance_contract_reference
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.advance_contract_id == AdvanceContractId
                            && o.name != null)
                        .Select(o => new { Value = o.id, Text = o.name + (" - " + o.progress_claim_name ?? "") });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("SurveyorIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SurveyorIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.contractor
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_surveyor == true)
                    .Select(o => new { Value = o.id, Text = o.business_partner_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("TransportModelIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> TransportModelIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.master_list
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.item_group == "transport-model")
                    .Select(o => new { Value = o.id, Text = o.item_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
				logger.Error(ex.InnerException ?? ex);
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
        [HttpGet("LocationIDLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> LocationIDLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var minelocation = dbContext.mine_location
                      .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.stock_location_name });
                var wastelocation = dbContext.waste_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.stock_location_name });
                var stockpileocation = dbContext.vw_stockpile_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });
                var portlocation = dbContext.port_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.stock_location_name });

                var lookup = minelocation.Union(wastelocation).Union(stockpileocation).Union(portlocation);

                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("List")]
        public async Task<IActionResult> List([FromQuery] string q)
        {
            try
            {
                var rows = dbContext.vw_joint_survey.AsNoTracking();
                rows = rows.Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin);
                if (!string.IsNullOrEmpty(q))
                {
                    rows.Where(o => o.join_survey_number.Contains(q));
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
            if (await mcsContext.CanRead(dbContext, Id, CurrentUserContext.AppUserId)
                || CurrentUserContext.IsSysAdmin)
            {
                try
                {
                    var record = await dbContext.vw_joint_survey
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
        public async Task<IActionResult> SaveData([FromBody] advance_contract Record)
        {
            try
            {
                var record = dbContext.joint_survey
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
                        logger.Debug("User is not authorized.");
                        return Unauthorized();
                    }
                }
                else if (await mcsContext.CanCreate(dbContext, nameof(joint_survey),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    record = new joint_survey();
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

                    dbContext.joint_survey.Add(record);
                    await dbContext.SaveChangesAsync();

                    return Ok(record);
                }
                else
                {
                    logger.Debug("User is not authorized.");
                    return Unauthorized();
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
                var record = dbContext.joint_survey
                    .Where(o => o.id == Id)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanDelete(dbContext, Id, CurrentUserContext.AppUserId)
                                || CurrentUserContext.IsSysAdmin)
                    {
                        dbContext.joint_survey.Remove(record);
                        await dbContext.SaveChangesAsync();
                        return Ok();
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
                logger.Error(ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost("Approve")]
        public async Task<IActionResult> Approve(string Id)
        {
            var result = new StandardResult();
            joint_survey record = null;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.joint_survey
                        .Where(o => o.id == Id && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanUpdate(dbContext, Id, CurrentUserContext.AppUserId)
                                    || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);

                            //JsonConvert.PopulateObject(values, record);

                            record.InjectFrom(e);
                            record.approved_by = CurrentUserContext.AppUserId;
                            record.approved_on = DateTime.Now;

                            await dbContext.SaveChangesAsync();
                            result.Success = true;
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
                    result.Message = ex.InnerException?.Message ?? ex.Message;
                }
            }

            if(result.Success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.joint_survey();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.JointSurvey.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return new JsonResult(result);
        }
    }
}
