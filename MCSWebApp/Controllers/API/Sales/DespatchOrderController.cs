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

namespace MCSWebApp.Controllers.API.Sales
{
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class DespatchOrderController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public DespatchOrderController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                    dbContext.vw_despatch_order.Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .OrderByDescending(o => o.created_on),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DataGrid/{tanggal1}/{tanggal2}")]
        public async Task<object> DataGrid(string tanggal1, string tanggal2, DataSourceLoadOptions loadOptions)
        {
            logger.Debug($"tanggal1 = {tanggal1}");
            logger.Debug($"tanggal2 = {tanggal2}");

            if (tanggal1 == null || tanggal2 == null)
            {
                return await DataSourceLoader.LoadAsync(dbContext.vw_despatch_order
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

            return await DataSourceLoader.LoadAsync(dbContext.vw_despatch_order
                .Where(o =>
                    o.despatch_order_date >= dt1
                    && o.despatch_order_date <= dt2
                    && o.organization_id == CurrentUserContext.OrganizationId
                    && (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<StandardResult> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            var result = new StandardResult();
            try
            {
                var record = await dbContext.vw_despatch_order
                    .Where(o => o.id == Id).FirstOrDefaultAsync();
                result.Data = record;
                result.Success = record != null ? true : false;
                result.Message = result.Success ? "Ok" : "Record not found";

            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                result.Success = false;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return result;
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(despatch_order),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new despatch_order();
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
                                cmd.CommandText = $"SELECT nextval('seq_despatch_order_number')";
                                var r = await cmd.ExecuteScalarAsync();
                                r = Convert.ToInt32(r).ToString("D3");
                                record.despatch_order_number = $"DO-{DateTime.Now:yyyyMMdd}-{r}";
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.ToString());
                                return BadRequest(ex.Message);
                            }
                        }
                    }
                    #endregion

                    if (record.loading_rate != null && record.required_quantity != null && 
                        record.loading_rate > 0 && record.required_quantity > 0)
                    {
                        record.laytime_duration = (record.required_quantity / record.loading_rate) * 86400;   
                    }

                    dbContext.despatch_order.Add(record);
                    await dbContext.SaveChangesAsync();

                    return Ok(record);
				}
				else
				{
					return BadRequest("User is not authorized.");
				}
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] despatch_order Record)
        {
            try
            {
                var record = dbContext.despatch_order
                    .Where(o => o.id == Record.id)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);
                    record.InjectFrom(Record);
                    record.modified_by = CurrentUserContext.AppUserId;
                    record.modified_on = DateTime.Now;
                    record.owner_id = (record.owner_id ?? CurrentUserContext.AppUserId);

                    await dbContext.SaveChangesAsync();
                    return Ok(record);
                }
                else
                {
                    record = new despatch_order();
                    record.InjectFrom(Record);

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
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
                                cmd.CommandText = $"SELECT nextval('seq_despatch_order_number')";
                                var r = await cmd.ExecuteScalarAsync();
                                r = Convert.ToInt32(r).ToString("D3");
                                record.despatch_order_number = $"DO-{DateTime.Now:yyyyMMdd}-{r}";
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.ToString());
                                return BadRequest(ex.Message);
                            }
                        }
                    }
                    #endregion

                    if (record.loading_rate != null && record.required_quantity != null &&
                        record.loading_rate > 0 && record.required_quantity > 0)
                    {
                        record.laytime_duration = (record.required_quantity / record.loading_rate) * 86400;
                    }

                    dbContext.despatch_order.Add(record);
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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.despatch_order
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

                    if (record.loading_rate != null && record.required_quantity != null &&
                        record.loading_rate > 0 && record.required_quantity > 0)
                    {
                        record.laytime_duration = (record.required_quantity / record.loading_rate) * 86400;
                    }

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
                var sales_invoice = dbContext.sales_invoice.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.despatch_order_id == key).FirstOrDefault();
                if (sales_invoice != null) return BadRequest("Can not be deleted since it is already have one or more transactions.");

                var record = dbContext.despatch_order
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.despatch_order.Remove(record);
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

        [HttpGet("DespatchOrderIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DespatchOrderIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.vw_despatch_order
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

        [HttpGet("SalesOrderIdLookup")]
        public async Task<object> SalesOrderIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.sales_order
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.sales_order_number });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
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

        [HttpGet("CurrencyIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> CurrencyIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.currency
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .OrderBy(o => o.currency_name)
                    .Select(o => new { Value = o.id, Text = o.currency_code });
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
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    string sales_contract_id = "";
                    var sales_contract = dbContext.sales_contract
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.sales_contract_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower())
                        .FirstOrDefault();
                    if (sales_contract != null) sales_contract_id = sales_contract.id.ToString();

                    string contract_term_id = "";
                    var sales_contract_term = dbContext.sales_contract_term
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.contract_term_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(4)).ToLower())
                        .FirstOrDefault();
                    if (sales_contract_term != null) contract_term_id = sales_contract_term.id.ToString();

                    string despatch_plan_id = "";
                    var sales_contract_despatch_plan = dbContext.sales_contract_despatch_plan
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_plan_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower())
                        .FirstOrDefault();
                    if (sales_contract_despatch_plan != null) despatch_plan_id = sales_contract_despatch_plan.id.ToString();

                    string seller_id = "";
                    var organization = dbContext.organization
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.organization_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).ToLower())
                        .FirstOrDefault();
                    if (organization != null) seller_id = organization.id.ToString();

                    string buyer_id = "";
                    var customer = dbContext.customer
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(7)).ToLower())
                        .FirstOrDefault();
                    if (customer != null) buyer_id = customer.id.ToString();

                    string shipto = null;
                    var customer2 = dbContext.customer
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(8)).ToLower())
                        .FirstOrDefault();
                    if (customer2 != null) shipto = customer2.id.ToString();

                    string contract_product_id = "";
                    var contract_product = dbContext.sales_contract_product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.contract_product_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(9)).ToLower())
                        .FirstOrDefault();
                    if (contract_product != null) contract_product_id = contract_product.id.ToString();

                    string uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol.ToLower() == PublicFunctions.IsNullCell(row.GetCell(13)).ToLower())
                        .FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    string fulfilment_type_id = "";
                    var master_list = dbContext.master_list
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.item_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(14)).ToLower())
                        .FirstOrDefault();
                    if (master_list != null) fulfilment_type_id = master_list.id.ToString();

                    string del_term_id = "";
                    var master_list2 = dbContext.master_list
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.item_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(15)).ToLower())
                        .FirstOrDefault();
                    if (master_list2 != null) del_term_id = master_list2.id.ToString();

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
                                    cmd.CommandText = $"SELECT nextval('seq_despatch_order_number')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    r = Convert.ToInt32(r).ToString("D3");
                                    TransactionNumber = $"DO-{DateTime.Now:yyyyMMdd}-{r}";
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

                    var record = dbContext.despatch_order
                        .Where(o => o.despatch_order_number.ToLower() == TransactionNumber.ToLower()
							&& o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.despatch_order_date = PublicFunctions.Tanggal(row.GetCell(1));
                        record.planned_despatch_date = PublicFunctions.Tanggal(row.GetCell(2));
                        record.sales_order_id = sales_contract_id;
                        record.contract_term_id = contract_term_id;
                        record.despatch_plan_id = despatch_plan_id;
                        record.seller_id = seller_id;
                        record.customer_id = buyer_id;
                        record.ship_to = shipto;
                        record.contract_product_id = contract_product_id;
                        record.required_quantity = PublicFunctions.Desimal(row.GetCell(10));
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(11));
                        record.quantity = PublicFunctions.Desimal(row.GetCell(12));
                        record.uom_id = uom_id;
                        record.fulfilment_type_id = fulfilment_type_id;
                        record.delivery_term_id = del_term_id;
                        record.notes = PublicFunctions.IsNullCell(row.GetCell(16));

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new despatch_order();
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

                        record.despatch_order_number = TransactionNumber;
                        record.despatch_order_date = PublicFunctions.Tanggal(row.GetCell(1));
                        record.planned_despatch_date = PublicFunctions.Tanggal(row.GetCell(2));
                        record.sales_order_id = sales_contract_id;
                        record.contract_term_id = contract_term_id;
                        record.despatch_plan_id = despatch_plan_id;
                        record.seller_id = seller_id;
                        record.customer_id = buyer_id;
                        record.ship_to = shipto;
                        record.contract_product_id = contract_product_id;
                        record.required_quantity = PublicFunctions.Desimal(row.GetCell(10));
                        record.final_quantity = PublicFunctions.Desimal(row.GetCell(11));
                        record.quantity = PublicFunctions.Desimal(row.GetCell(12));
                        record.uom_id = uom_id;
                        record.fulfilment_type_id = fulfilment_type_id;
                        record.delivery_term_id = del_term_id;
                        record.notes = PublicFunctions.IsNullCell(row.GetCell(16));

                        dbContext.despatch_order.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 1, Line " + i + ": " + Environment.NewLine;
                    }
                    else errormessage = ex.Message;

                    teks += errormessage + Environment.NewLine + Environment.NewLine;
                    gagal = true;
                }
            }

            sheet = wb.GetSheetAt(1); //*** detail sheet
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    string despatch_order_id = null;
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.despatch_order_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    string delay_category_id = null;
                    var delay_category = dbContext.delay_category
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.delay_category_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).ToLower()).FirstOrDefault();
                    if (delay_category != null) delay_category_id = delay_category.id.ToString();

                    string uom_id = null;
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(6))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var record = dbContext.despatch_order_delay
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.delay_category_id == delay_category_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.demurrage_percent = PublicFunctions.Desimal(row.GetCell(2));
                        record.despatch_percent = PublicFunctions.Desimal(row.GetCell(3));

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new despatch_order_delay();
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

                        record.despatch_order_id = despatch_order_id;
                        record.delay_category_id = delay_category_id;
                        record.demurrage_percent = PublicFunctions.Desimal(row.GetCell(2));
                        record.despatch_percent = PublicFunctions.Desimal(row.GetCell(3));

                        dbContext.despatch_order_delay.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 2, Line " + i + ": " + Environment.NewLine;
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
                HttpContext.Session.SetString("filename", "DespatchOrder");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public string secondsToDhms(decimal seconds)
        {
            var d = Math.Floor(seconds / (3600 * 24));
            var h = Math.Floor(seconds % (3600 * 24) / 3600);
            var m = Math.Floor(seconds % 3600 / 60);
            var s = Math.Floor(seconds % 60);

            var dDisplay = d > 0 ? d + (d == 1 ? " Day " : " Days ") : "";
            var hDisplay = h > 0 ? h + (h == 1 ? " Hour " : " Hours ") : "";
            var mDisplay = m > 0 ? m + (m == 1 ? " Minute " : " Minutes ") : "";
            return dDisplay + hDisplay + mDisplay;
        }

        [HttpGet("EndUserIdLookup")]
        public async Task<object> EndUserIdLookup(DataSourceLoadOptions loadOptions, string SalesContractId)
        {
            try
            {
                var lookup = dbContext.vw_sales_contract_end_user
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.sales_contract_id == SalesContractId)
                    .Select(o => new { Value = o.id, Text = o.business_partner_name });
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
