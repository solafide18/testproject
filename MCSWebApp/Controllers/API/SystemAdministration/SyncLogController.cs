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
using Microsoft.EntityFrameworkCore;

namespace MCSWebApp.Controllers.API.SystemAdministration
{
    [Route("api/SystemAdministration/[controller]")]
    [ApiController]
    public class SyncLogController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public SyncLogController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGrid(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_sync_log
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                    loadOptions);
            }

            var dt1 = Convert.ToDateTime(tanggal1);
            var dt2 = Convert.ToDateTime(tanggal2);

            return await DataSourceLoader.LoadAsync(dbContext.vw_sync_log
                .Where(o =>
                    o.date_time >= dt1
                    && o.date_time <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_sync_log.Where(o => o.id == Id),
                loadOptions);
        }

   //     [HttpPost("InsertData")]
   //     [ApiExplorerSettings(IgnoreApi = true)]
   //     public async Task<IActionResult> InsertData([FromForm] string values)
   //     {
   //         logger.Trace($"string values = {values}");

   //         try
   //         {
			//	if (await mcsContext.CanCreate(dbContext, nameof(sync_log),
			//		CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
			//	{
   //                 var record = new sync_log();
   //                 JsonConvert.PopulateObject(values, record);

   //                 record.id = Guid.NewGuid().ToString("N");
   //                 record.created_by = CurrentUserContext.AppUserId;
   //                 record.created_on = DateTime.Now;
   //                 record.modified_by = null;
   //                 record.modified_on = null;
   //                 record.is_active = true;
   //                 record.is_default = null;
   //                 record.is_locked = null;
   //                 record.entity_id = null;
   //                 record.owner_id = CurrentUserContext.AppUserId;
   //                 record.organization_id = CurrentUserContext.OrganizationId;

   //                 dbContext.sync_log.Add(record);
   //                 await dbContext.SaveChangesAsync();

   //                 return Ok(record);
			//	}
			//	else
			//	{
			//		return BadRequest("User is not authorized.");
			//	}
   //         }
			//catch (Exception ex)
			//{
			//	logger.Error(ex.InnerException ?? ex);
   //             return BadRequest(ex.InnerException?.Message ?? ex.Message);
			//}
   //     }

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            try
            {
                var record = dbContext.sync_log
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);

                    JsonConvert.PopulateObject(values, record);

                    record.InjectFrom(e);

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
                var record = dbContext.sync_log
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.sync_log.Remove(record);
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

        [HttpGet("BusinessAreaIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> BusinessAreaIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.vw_business_area
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_area_name });
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
            if (await mcsContext.CanRead(dbContext, Id, CurrentUserContext.AppUserId)
                || CurrentUserContext.IsSysAdmin)
            {
                try
                {
                    var record = await dbContext.vw_sync_log
                        .Where(o => o.id == Id)
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

   //     [HttpPost("SaveData")]
   //     public async Task<IActionResult> SaveData([FromBody] sync_log Record)
   //     {
   //         try
   //         {
   //             var record = dbContext.sync_log
   //                 .Where(o => o.id == Record.id)
   //                 .FirstOrDefault();
   //             if (record != null)
   //             {
   //                 if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
   //                     || CurrentUserContext.IsSysAdmin)
   //                 {
   //                     var e = new entity();
   //                     e.InjectFrom(record);
   //                     record.InjectFrom(Record);
   //                     record.InjectFrom(e);
   //                     record.modified_by = CurrentUserContext.AppUserId;
   //                     record.modified_on = DateTime.Now;

   //                     await dbContext.SaveChangesAsync();
   //                     return Ok(record);
   //                 }
   //                 else
   //                 {
   //                     return BadRequest("User is not authorized.");
   //                 }
   //             }
   //             else if (await mcsContext.CanCreate(dbContext, nameof(sync_log),
   //                 CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
   //             {
   //                 record = new sync_log();
   //                 record.InjectFrom(Record);

   //                 record.id = Guid.NewGuid().ToString("N");
   //                 record.created_by = CurrentUserContext.AppUserId;
   //                 record.created_on = DateTime.Now;
   //                 record.modified_by = null;
   //                 record.modified_on = null;
   //                 record.is_active = true;
   //                 record.is_default = null;
   //                 record.is_locked = null;
   //                 record.entity_id = null;
   //                 record.owner_id = CurrentUserContext.AppUserId;
   //                 record.organization_id = CurrentUserContext.OrganizationId;

   //                 dbContext.sync_log.Add(record);
   //                 await dbContext.SaveChangesAsync();

   //                 return Ok(record);
   //             }
   //             else
   //             {
   //                 return BadRequest("User is not authorized.");
   //             }
   //         }
			//catch (Exception ex)
			//{
			//	logger.Error(ex.InnerException ?? ex);
   //             return BadRequest(ex.InnerException?.Message ?? ex.Message);
			//}
   //     }

        [HttpDelete("DeleteById/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Trace($"string Id = {Id}");

            if (await mcsContext.CanDelete(dbContext, Id, CurrentUserContext.AppUserId)
                || CurrentUserContext.IsSysAdmin)
            {
                try
                {
                    var record = dbContext.sync_log
                        .Where(o => o.id == Id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        dbContext.sync_log.Remove(record);
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
            else
            {
                return BadRequest("User is not authorized.");
            }
        }
    }
}
