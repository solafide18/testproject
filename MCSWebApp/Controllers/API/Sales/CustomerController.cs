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
    public class CustomerController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public CustomerController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                return await DataSourceLoader.LoadAsync(dbContext.vw_customer
                    .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DataGridCL")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGridCL(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var datagridCL = dbContext.vw_customer
                                    .Where(o => CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                                            || CurrentUserContext.IsSysAdmin)
                                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId);

                var arrayDataGridCL = await datagridCL.ToArrayAsync();
                foreach (vw_customer item in arrayDataGridCL)
                {
                    var remainedcl = RemainedCreditLimit(item.id, loadOptions);
                    var tstring = remainedcl.ToString();
                    item.remained_credit_limit = decimal.Parse(tstring);

                    var temp1 = new vw_customer { id = item.id, remained_credit_limit=item.remained_credit_limit };
                    var entry1 = dbContext.Entry(temp1);
                    entry1.Property(p => p.remained_credit_limit).IsModified = true;
                    dbContext.SaveChanges();

                }
                return await DataSourceLoader.LoadAsync(datagridCL,
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
                dbContext.vw_customer.Where(o => o.id == Id),                    
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(customer),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new customer();
                    JsonConvert.PopulateObject(values, record);

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
                    //record.is_active = true;
                    record.is_default = null;
                    record.is_locked = null;
                    record.entity_id = null;
                    record.owner_id = CurrentUserContext.AppUserId;
                    record.organization_id = CurrentUserContext.OrganizationId;
                    record.is_customer = true;
                    record.is_government = null;
                    record.is_vendor = null;

                    //var conn = dbContext.Database.GetDbConnection();
                    //if (conn.State != System.Data.ConnectionState.Open)
                    //{
                    //    await conn.OpenAsync();
                    //}
                    //if (conn.State == System.Data.ConnectionState.Open)
                    //{
                    //    using (var cmd = conn.CreateCommand())
                    //    {
                    //        try
                    //        {
                    //            cmd.CommandText = $"SELECT nextval('seq_customer_number')";
                    //            var r = await cmd.ExecuteScalarAsync();
                    //            record.business_partner_code = Convert.ToInt64(r).ToString("D6");
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            logger.Error(ex.ToString());
                    //            return BadRequest(ex.Message);
                    //        }
                    //    }
                    //}

                    dbContext.customer.Add(record);
                    await dbContext.SaveChangesAsync();

                    var creditLimitActivation = record.credit_limit_activation ?? false;
                    string sql = $"UPDATE sales_contract SET credit_limit_activation = {creditLimitActivation} WHERE " +
                            $"customer_id = '{record.id}' ";
                    await dbContext.Database.ExecuteSqlRawAsync(sql);


                    var recordCreditLimitHistory = new credit_limit_history();
                    JsonConvert.PopulateObject(values, recordCreditLimitHistory);

                    recordCreditLimitHistory.id = Guid.NewGuid().ToString("N");
                    recordCreditLimitHistory.created_by = CurrentUserContext.AppUserId;
                    recordCreditLimitHistory.created_on = System.DateTime.Now;
                    recordCreditLimitHistory.modified_by = null;
                    recordCreditLimitHistory.modified_on = null;
                    recordCreditLimitHistory.is_active = true;
                    recordCreditLimitHistory.is_default = null;
                    recordCreditLimitHistory.is_locked = null;
                    recordCreditLimitHistory.entity_id = null;
                    recordCreditLimitHistory.owner_id = CurrentUserContext.AppUserId;
                    recordCreditLimitHistory.organization_id = CurrentUserContext.OrganizationId;
                    recordCreditLimitHistory.credit_limit_value = record.credit_limit;
                    recordCreditLimitHistory.customer_id = record.id;
                    dbContext.credit_limit_history.Add(recordCreditLimitHistory);

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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.customer
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        var currentCreditLimit = record.credit_limit;
                        var e = new entity();
                        e.InjectFrom(record);

                        JsonConvert.PopulateObject(values, record);
                        var is_active = record.is_active;
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        record.is_active = is_active;
                        record.is_customer = true;
                        record.is_government = null;
                        record.is_vendor = null;

                        if (currentCreditLimit!=record.credit_limit)
                        {
                            var recordCreditLimitHistory = new credit_limit_history();
                            JsonConvert.PopulateObject(values, recordCreditLimitHistory);

                            recordCreditLimitHistory.id = Guid.NewGuid().ToString("N");
                            recordCreditLimitHistory.created_by = CurrentUserContext.AppUserId;
                            recordCreditLimitHistory.created_on = System.DateTime.Now;
                            recordCreditLimitHistory.modified_by = null;
                            recordCreditLimitHistory.modified_on = null;
                            recordCreditLimitHistory.is_active = true;
                            recordCreditLimitHistory.is_default = null;
                            recordCreditLimitHistory.is_locked = null;
                            recordCreditLimitHistory.entity_id = null;
                            recordCreditLimitHistory.owner_id = CurrentUserContext.AppUserId;
                            recordCreditLimitHistory.organization_id = CurrentUserContext.OrganizationId;
                            recordCreditLimitHistory.credit_limit_value = record.credit_limit;
                            recordCreditLimitHistory.customer_id = record.id;
                            dbContext.credit_limit_history.Add(recordCreditLimitHistory);
                        }

                        var creditLimitActivation = record.credit_limit_activation ?? false;
                        string sql = $"UPDATE sales_contract SET credit_limit_activation = {creditLimitActivation} WHERE " +
                                $"customer_id = '{key}' ";
                        await dbContext.Database.ExecuteSqlRawAsync(sql);

                        await dbContext.SaveChangesAsync();
                        return Ok(record);
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                else
                {
                    return BadRequest("Record does not exist.");
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
                var record = dbContext.customer
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        dbContext.customer.Remove(record);
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        return BadRequest("User is not authorized.");
                    }
                }
                return Ok();
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }
        

        [HttpGet("CustomerIdLookup")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> CustomerIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.customer
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.business_partner_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("CustomerIdDespatchOrderBasedLookup")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> CustomerIdDespatchOrderBasedLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var customerIds = await dbContext.despatch_order
                    .Where(x => x.organization_id == CurrentUserContext.OrganizationId)
                    .Select(x => x.customer_id)
                    .Distinct().ToListAsync();

                var lookup = dbContext.customer
                            .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                            .Select(o => new { Value = o.id, Text = o.business_partner_name })
                            .Where(e => customerIds.Contains(e.Value));

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
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var customer_type_id = "";
                    var customer_type = dbContext.customer_type
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.customer_type_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()).FirstOrDefault();
                    if (customer_type != null) customer_type_id = customer_type.id.ToString();

                    var country_id = "";
                    var country = dbContext.country
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.country_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (country != null) country_id = country.id.ToString();

                    var bank_account_id = ""; var currency_id = "";
                    var bank_account = dbContext.bank_account
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.account_number == PublicFunctions.IsNullCell(row.GetCell(13))).FirstOrDefault();
                    if (bank_account != null)
                    {
                        bank_account_id = bank_account.id.ToString();
                        currency_id = bank_account.currency_id.ToString();
                    }

                    var CustomerCode = "";
                    if (PublicFunctions.IsNullCell(row.GetCell(1)) == "")
                    {
                        #region Get customer number
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
                                    cmd.CommandText = $"SELECT nextval('seq_customer_number')";
                                    var r = await cmd.ExecuteScalarAsync();
                                    CustomerCode = Convert.ToInt64(r).ToString("D6");
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
                        CustomerCode = PublicFunctions.IsNullCell(row.GetCell(1));

                    var record = dbContext.customer
                        .Where(o => o.business_partner_code == CustomerCode
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.business_partner_name = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        kol++;
                        record.customer_type_id = customer_type_id; kol++;
                        record.primary_address = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.country_id = country_id; kol++;
                        record.primary_contact_name = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;
                        record.primary_contact_email = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.primary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(7)); kol++;
                        record.secondary_contact_name = PublicFunctions.IsNullCell(row.GetCell(8)); kol++;
                        record.secondary_contact_email = PublicFunctions.IsNullCell(row.GetCell(9)); kol++;
                        record.secondary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(10)); kol++;
                        record.additional_information = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.bank_account_id = bank_account_id; kol++;
                        record.currency_id = currency_id; kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(16)); kol++;

                        await dbContext.SaveChangesAsync();

                        var creditLimitActivation = record.credit_limit_activation ?? false;
                        string sql = $"UPDATE sales_contract SET credit_limit_activation = {creditLimitActivation} WHERE " +
                                $"customer_id = '{record.id}' ";
                        await dbContext.Database.ExecuteSqlRawAsync(sql);
                    }
                    else
                    {
                        record = new customer();
                        record.id = Guid.NewGuid().ToString("N");
                        record.created_by = CurrentUserContext.AppUserId;
                        record.created_on = DateTime.Now;
                        record.modified_by = null;
                        record.modified_on = null;
                        //record.is_active = true;
                        record.is_default = null;
                        record.is_locked = null;
                        record.entity_id = null;
                        record.owner_id = CurrentUserContext.AppUserId;
                        record.organization_id = CurrentUserContext.OrganizationId;

                        record.business_partner_name = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.business_partner_code = CustomerCode; kol++;
                        record.customer_type_id = customer_type_id; kol++;
                        record.primary_address = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.country_id = country_id; kol++;
                        record.primary_contact_name = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;
                        record.primary_contact_email = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.primary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(7)); kol++;
                        record.secondary_contact_name = PublicFunctions.IsNullCell(row.GetCell(8)); kol++;
                        record.secondary_contact_email = PublicFunctions.IsNullCell(row.GetCell(9)); kol++;
                        record.secondary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(10)); kol++;
                        record.additional_information = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.bank_account_id = bank_account_id; kol++;
                        record.currency_id = currency_id; kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(16)); kol++;

                        dbContext.customer.Add(record);
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

            sheet = wb.GetSheetAt(1); //***** sheet 2
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var customer_id = "";
                    var customer = dbContext.customer
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()).FirstOrDefault();
                    if (customer != null) customer_id = customer.id.ToString();

                    var record = dbContext.customer_attachment
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.customer_id == customer_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.filename = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new customer_attachment();
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

                        record.customer_id = customer_id; kol++;
                        record.filename = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;

                        dbContext.customer_attachment.Add(record);
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

            sheet = wb.GetSheetAt(2); //***** sheet 3
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var business_partner_id = "";
                    var customer = dbContext.customer
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()).FirstOrDefault();
                    if (customer != null) business_partner_id = customer.id.ToString();

                    var record = dbContext.contact
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_id.ToLower() == business_partner_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        record.contact_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.contact_email = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;
                        record.contact_phone = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.contact_position = PublicFunctions.IsNullCell(row.GetCell(4)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new contact();
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

                        record.business_partner_id = business_partner_id;
                        record.contact_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.contact_email = PublicFunctions.IsNullCell(row.GetCell(2)); kol++;
                        record.contact_phone = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.contact_position = PublicFunctions.IsNullCell(row.GetCell(4)); kol++;

                        dbContext.contact.Add(record);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        errormessage = ex.InnerException.Message;
                        teks += "==>Error Sheet 3, Line " + i + ", Column " + kol + " : " + Environment.NewLine;
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
                HttpContext.Session.SetString("filename", "Customer");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] customer Record)
        {
            try
            {
                var record = dbContext.customer
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
                    record = new customer();
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

                    dbContext.customer.Add(record);
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

        [HttpGet("CreditLimitHistory")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> CreditLimitHistory(string customer_id, DataSourceLoadOptions loadOptions)
        {
            try
            {
                return await DataSourceLoader.LoadAsync(
                    dbContext.vw_credit_limit_history.Where(o => o.customer_id == customer_id),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<object> CountCreditLimit(string customer_id)
        {
            List<CreditLimitData> retval = new List<CreditLimitData>();
            double remainedCreditLimit = 0.0;
            double customerCreditLimit = 0.0;
            try
            {
                var customer_data = dbContext.customer
                    .Where(o => o.id == customer_id)
                    .Select(o => o.credit_limit)
                    ;
                foreach (decimal v in customer_data)
                {
                    customerCreditLimit += (double)v;
                }

                // find all sales contract within the same customer
                var vw_sales_invoice_custid = dbContext.vw_sales_invoice
                    .Where(o => o.customer_id == customer_id)
                    ;
                var array_vw_sales_invoice_custid = await vw_sales_invoice_custid.ToArrayAsync();
                double totalPaymentfromAllInvoice = 0.0;

                double totalPricefromAllInvoice = 0.0;
                if (array_vw_sales_invoice_custid.Length > 0)
                {
                    foreach (vw_sales_invoice item1 in array_vw_sales_invoice_custid)
                    {
                        if (item1.total_price != null)
                        {
                            totalPricefromAllInvoice += (double)item1.total_price;
                        }
                        var vw_sales_invoice_payment_data = dbContext.vw_sales_invoice_payment.Where(o => o.sales_invoice_number == item1.invoice_number);
                        var array_vw_sales_invoice_payment_data = await vw_sales_invoice_payment_data.ToArrayAsync();
                        if (array_vw_sales_invoice_payment_data.Length > 0)
                        {
                            foreach (vw_sales_invoice_payment itemX in array_vw_sales_invoice_payment_data)
                            {
                                if (itemX.payment_value != null)
                                {
                                    totalPaymentfromAllInvoice += (double)itemX.payment_value;
                                }
                            }
                        }
                    }
                }
                remainedCreditLimit = customerCreditLimit - totalPricefromAllInvoice + totalPaymentfromAllInvoice;
                retval.Add(new CreditLimitData(customerCreditLimit, remainedCreditLimit));
                return retval;

            }
            catch (Exception ex)
            {
                logger.Debug($"exception error message = {ex.Message}");
                return retval;
            }
        }

        [HttpGet("RemainedCreditLimit")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public object RemainedCreditLimit(string customer_id, DataSourceLoadOptions loadOptions)
        {
            try
            {
                List<CreditLimitData> varCreditLimitData = (List<CreditLimitData>)CountCreditLimit(customer_id).Result;
                double remainedCreditLimit = varCreditLimitData[0].RemainedCreditLimit;
                return Ok(remainedCreditLimit);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("CreditLimitAlert")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public object CreditLimitAlert(string customer_id, DataSourceLoadOptions loadOptions)
        {
            List<AlertCL> retval = new List<AlertCL>();
            try
            {
                List<CreditLimitData> varCreditLimitData = (List<CreditLimitData>)CountCreditLimit(customer_id).Result;
                double remainedCreditLimit = varCreditLimitData[0].RemainedCreditLimit;
                double customerCreditLimit = varCreditLimitData[0].InitialCreditLimit;

                var percentage = 1.0;

                if (customerCreditLimit > 0)
                {
                    percentage = remainedCreditLimit / customerCreditLimit;
                }
                var color = "green";
                var status = "safe";

                if (percentage < 0.2)
                {
                    color = "red";
                    status = "limit";
                }

                if (percentage < 0.5)
                {
                    color = "yellow";
                    status = "warning";
                }

                percentage = percentage * 100;

                retval.Add(new AlertCL(status, color, percentage));
                return Ok(retval);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("CustomerTransactionHistory")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> CustomerTransactionHistory(string Id, DataSourceLoadOptions loadOptions)
        {
            List<CustomerInvoicePaymentHistory> retval = new List<CustomerInvoicePaymentHistory>();
            try
            {
                var invoice_data = dbContext.vw_details_customer_invoice_history
                    .Where(o => o.customer_id == Id )
                    ;
                var array_invoice_data = await invoice_data.ToArrayAsync();
                decimal? currentCreditLimit = 0;
                if (array_invoice_data.Length > 0)
                {
                    currentCreditLimit = array_invoice_data[0].credit_limit;
                }
                foreach (vw_details_customer_invoice_history v in array_invoice_data)
                {
                    var temp1 = new CustomerInvoicePaymentHistory(v);
                    currentCreditLimit = currentCreditLimit - temp1.billing + temp1.receipt;
                    temp1.outstanding = currentCreditLimit;
                    retval.Add(temp1);

                    var payment_data = dbContext.vw_details_customer_payment_history
                        .Where(o => o.invoice_number == v.invoice_number)
                        ;
                    var array_payment_data = await payment_data.ToArrayAsync();

                    foreach (vw_details_customer_payment_history w in array_payment_data)
                    {
                        temp1 = new CustomerInvoicePaymentHistory(w);
                        currentCreditLimit = currentCreditLimit - temp1.billing + temp1.receipt;
                        temp1.outstanding = currentCreditLimit;
                        retval.Add(temp1);
                    }
                }
                return retval;

            }
            catch (Exception ex)
            {
                logger.Trace(ex.Message);
                return retval;
            }

            //return await DataSourceLoader.LoadAsync(
            //    dbContext.vw_customer_transaction_history.Where(o => o.id == Id),
            //    loadOptions);
        }

    }

    public class AlertCL
    {
        public double Persentase { get; set; }
        public string Status { get; set; }
        public string Color { get; set; }
        public AlertCL()
        {
            Status = "name";
            Persentase = 0.0;
            Color = "code";
        }
        public AlertCL(string vStatus, string vColor, double vPercen)
        {
            Status = vStatus;
            Persentase = vPercen;
            Color = vColor;
        }
    }

    public partial class CustomerInvoicePaymentHistory
    {
        public string customer_id { get; set; }
        public string business_partner_name { get; set; }
        public DateTime? tdate { get; set; }
        public string invoice_number { get; set; }
        public decimal? billing { get; set; }
        public decimal? receipt { get; set; }
        public decimal? outstanding { get; set; }
        public string currency { get; set; }
        public string despatch_order_number { get; set; }
        public string sales_contract_name { get; set; }
        public string contract_term_name { get; set; }
        public string despatch_plan_name { get; set; }
        public decimal? credit_limit { get; set; }

        public CustomerInvoicePaymentHistory(vw_details_customer_payment_history input)
        {
            customer_id = input.customer_id;
            business_partner_name = input.business_partner_name;
            tdate = input.tdate;
            invoice_number = input.invoice_number;
            billing = (decimal)input.billing;
            receipt = (decimal)input.receipt;
            outstanding = (decimal)input.outstanding;
            currency = input.currency;
            despatch_order_number = input.despatch_order_number;
            sales_contract_name = input.sales_contract_name;
            contract_term_name = input.contract_term_name;
            despatch_plan_name = input.despatch_plan_name;
            credit_limit = input.credit_limit;
        }

        public CustomerInvoicePaymentHistory(vw_details_customer_invoice_history input)
        {
            customer_id = input.customer_id;
            business_partner_name = input.business_partner_name;
            tdate = input.tdate;
            invoice_number = input.invoice_number;
            billing = (decimal)input.billing;
            receipt = (decimal)input.receipt;
            outstanding = (decimal)input.outstanding;
            currency = input.currency;
            despatch_order_number = input.despatch_order_number;
            sales_contract_name = input.sales_contract_name;
            contract_term_name = input.contract_term_name;
            despatch_plan_name = input.despatch_plan_name;
            credit_limit = input.credit_limit;
        }
    }
}
