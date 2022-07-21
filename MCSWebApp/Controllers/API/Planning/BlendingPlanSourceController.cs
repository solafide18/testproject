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

namespace MCSWebApp.Controllers.API.StockpileManagement
{
    [Route("api/Planning/[controller]")]
    [ApiController]
    public class BlendingPlanSourceController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public BlendingPlanSourceController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("ByBlendingPlanId/{Id}")]
        public async Task<object> ByBlendingPlanId(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_blending_plan_source
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.blending_plan_id == Id),
                loadOptions);
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_blending_plan_source
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_blending_plan_source.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var tbl = new blending_plan_source();
            JsonConvert.PopulateObject(values, tbl);

            /*decimal headerQty = 0;
            var header = dbContext.blending_plan
                .Where(o => o.id == tbl.blending_plan_id).FirstOrDefault();
            if (header != null) headerQty = (decimal)header.unloading_quantity;

           decimal detailQty = dbContext.blending_plan_source
                .Where(o => o.blending_plan_id == tbl.blending_plan_id)
                .Sum(o => o.loading_quantity);

            decimal maxQty = headerQty - detailQty;

            detailQty = detailQty + tbl.loading_quantity;
            if (detailQty > headerQty)
                return BadRequest("Maximum Quantity is " + maxQty + ".");*/

            var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(blending_plan_source),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new blending_plan_source();
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

                    dbContext.blending_plan_source.Add(record);
                    await dbContext.SaveChangesAsync();

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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            //--- check qty input
            var editData = new blending_plan_source();
            JsonConvert.PopulateObject(values, editData);

            /*var blending_plan_id = dbContext.blending_plan_source
                .Where(o => o.id == key).FirstOrDefault().blending_plan_id;
            if (blending_plan_id == null) blending_plan_id = "";

            decimal headerQty = 0;
            var header = dbContext.blending_plan
                .Where(o => o.id == blending_plan_id).FirstOrDefault();
            if (header != null) headerQty = (decimal)header.unloading_quantity;

            decimal detailQty = dbContext.blending_plan_source
                .Where(o => o.blending_plan_id == blending_plan_id && o.id != key)
                .Sum(o => o.loading_quantity);

            decimal maxQty = headerQty - detailQty;

            if (editData.loading_quantity > maxQty)
                return BadRequest("Maximum Quantity is " + maxQty + ".");*/
            //------

            var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var record = dbContext.blending_plan_source
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

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            try
            {
                var record = dbContext.blending_plan_source
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.blending_plan_source.Remove(record);
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

        [HttpGet("SourceLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SourceLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                //var lookup = dbContext.vw_stock_location
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                //    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });
                var mine_location = dbContext.vw_mine_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });
                var stockpile_location = dbContext.vw_stockpile_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });
                var port_location = dbContext.vw_port_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name + " > " + o.stock_location_name });

                var lookup = mine_location.Union(stockpile_location).Union(port_location)
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
    }
}
