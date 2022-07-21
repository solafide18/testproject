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

namespace MCSWebApp.Controllers.API.Sales
{
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class PartyController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public PartyController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                    dbContext.sales_price_type.Where(o => o.organization_id == CurrentUserContext.OrganizationId),
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
                dbContext.sales_price_type.Where(o => o.id == Id),
                loadOptions);
        }        

        [HttpGet("PartyIdLookup")]
        public async Task<object> PartyIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = GetDataUnion();
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }


        private IQueryable<object> GetDataUnion()
        {
            var customers = dbContext.customer
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_partner_name });
            var organizations = dbContext.organization
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.organization_name });
            return customers.Union(organizations);
        }
    }
}
