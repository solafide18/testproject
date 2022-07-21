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

namespace MCSWebApp.Controllers.API.Planning
{
    [Route("api/Planning/[controller]")]
    [ApiController]
    public class BargingPlanController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public BargingPlanController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                    dbContext.vw_barging_plan.Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.barging_plan.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            using(var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
					if (await mcsContext.CanCreate(dbContext, nameof(barging_plan),
						CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
					{
                        var record = new barging_plan();
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

                        dbContext.barging_plan.Add(record);
                        await dbContext.SaveChangesAsync();
                        
                        if (string.IsNullOrEmpty(record.master_list_id))
                            return BadRequest("Year cannot be empty.");

                        var yearRecord = dbContext.master_list
                            .FirstOrDefault(o => o.organization_id == CurrentUserContext.OrganizationId && o.id == record.master_list_id);

                        #region Barging Monthly
                        for (var i = 1; i <= 12; i++)
                        {
                            decimal totalShipmentQty = dbContext.shipment_plan
                                .Where(r => r.shipment_year == record.master_list_id && r.month_id == i)
                                .Select(t => t.qty_sp ?? 0).Sum();

                            var monthlyPlanRecord = new barging_plan_monthly();
                            monthlyPlanRecord.id = Guid.NewGuid().ToString("N");
                            monthlyPlanRecord.created_by = CurrentUserContext.AppUserId;
                            monthlyPlanRecord.created_on = DateTime.Now;
                            monthlyPlanRecord.modified_by = null;
                            monthlyPlanRecord.modified_on = null;
                            monthlyPlanRecord.is_active = true;
                            monthlyPlanRecord.is_default = null;
                            monthlyPlanRecord.is_locked = null;
                            monthlyPlanRecord.entity_id = null;
                            monthlyPlanRecord.owner_id = CurrentUserContext.AppUserId;
                            monthlyPlanRecord.organization_id = CurrentUserContext.OrganizationId;

                            monthlyPlanRecord.barging_plan_id = record.id;
                            monthlyPlanRecord.month_id = i;
                            monthlyPlanRecord.quantity = (decimal?)totalShipmentQty;

                            dbContext.barging_plan_monthly.Add(monthlyPlanRecord);
                            await dbContext.SaveChangesAsync();

                            #region Barging Daily
                            int days = DateTime.DaysInMonth(int.Parse(yearRecord.item_in_coding), i);
                            for (var j = 1; j <= days; j++)
                            {
                                var dailyPlanRecord = new barging_plan_daily();
                                dailyPlanRecord.id = Guid.NewGuid().ToString("N");
                                dailyPlanRecord.created_by = CurrentUserContext.AppUserId;
                                dailyPlanRecord.created_on = DateTime.Now;
                                dailyPlanRecord.modified_by = null;
                                dailyPlanRecord.modified_on = null;
                                dailyPlanRecord.is_active = true;
                                dailyPlanRecord.is_default = null;
                                dailyPlanRecord.is_locked = null;
                                dailyPlanRecord.entity_id = null;
                                dailyPlanRecord.owner_id = CurrentUserContext.AppUserId;
                                dailyPlanRecord.organization_id = CurrentUserContext.OrganizationId;

                                dailyPlanRecord.barging_plan_monthly_id = monthlyPlanRecord.id;
                                dailyPlanRecord.daily_date = new DateTime(int.Parse(yearRecord.item_in_coding), i, j);
                                dailyPlanRecord.quantity = totalShipmentQty > 0 ? (totalShipmentQty / days) : 0;
                                dailyPlanRecord.operational_hours = 20;
                                dailyPlanRecord.loading_rate = dailyPlanRecord.quantity > 0 ? (dailyPlanRecord.quantity / dailyPlanRecord.operational_hours) : 0;

                                dbContext.barging_plan_daily.Add(dailyPlanRecord);
                                await dbContext.SaveChangesAsync();
                            }
                            #endregion
                        }
                        #endregion


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
                    await tx.RollbackAsync();
                    logger.Error(ex.ToString());
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpPut("UpdateData")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.barging_plan
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
                var record = dbContext.barging_plan
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.barging_plan.Remove(record);
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

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.barging_plan
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
        public async Task<IActionResult> SaveData([FromBody] barging_plan Record)
        {
            try
            {
                var record = dbContext.barging_plan
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

                    await dbContext.SaveChangesAsync();
                    return Ok(record);
                }
                else
                {
                    record = new barging_plan();
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

                    dbContext.barging_plan.Add(record);
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

        [HttpDelete("DeleteById/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Debug($"string Id = {Id}");

            try
            {
                var record = dbContext.barging_plan
                    .Where(o => o.id == Id)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.barging_plan.Remove(record);
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

        [HttpGet("SalesPlanYearIdLookup/{YearId}")]
        public async Task<IActionResult> SalesPlanYearIdLookup(string YearId)
        {
            try
            {
                var record = await dbContext.barging_plan
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                        && o.master_list_id == YearId).FirstOrDefaultAsync();
                return Ok(record);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
        
        [HttpGet("MasterListYearIdLookup")]
        public async Task<object> MasterListYearIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.master_list
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                        && o.item_group == "years")
                    .Select(o => new { Value = o.id, Text = o.item_name, o.item_group, o.item_in_coding });
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
