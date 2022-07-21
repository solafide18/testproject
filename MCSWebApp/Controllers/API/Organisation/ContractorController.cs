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

namespace MCSWebApp.Controllers.API.Organisation
{
    [Route("api/Organisation/[controller]")]
    [ApiController]
    public class ContractorController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ContractorController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                return await DataSourceLoader.LoadAsync(dbContext.vw_contractor
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

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_contractor.Where(o => o.id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(contractor),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new contractor();
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
                    record.is_contractor = true;
                    record.is_government = null;
                    record.is_vendor = null;

                    dbContext.contractor.Add(record);
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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                var record = dbContext.contractor
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
                        var is_active = record.is_active;
                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        record.is_active = is_active;
                        record.is_contractor = true;
                        record.is_government = null;
                        record.is_vendor = null;

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
                var record = dbContext.contractor
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
                        dbContext.contractor.Remove(record);
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


        [HttpGet("ContractorIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ContractorIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.contractor
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

        [HttpGet("ContractorIdLookupByIsEquipmentOwner")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ContractorIdLookupByIsEquipmentOwner(DataSourceLoadOptions loadOptions, bool isEquipmentOwner)
        {
            try
            {
                var lookup = dbContext.contractor
                    .Where(o => o.is_equipment_owner ?? false == isEquipmentOwner)
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

        [HttpGet("ContractorIdLookupByIsSurveyor")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ContractorIdLookupByIsSurveyor(DataSourceLoadOptions loadOptions, bool isSurveyor)
        {
            try
            {
                var lookup = dbContext.contractor
                    .Where(o => o.is_surveyor ?? false == isSurveyor)
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

                    string contractor_type_id = "";
                    var contractor_type = dbContext.contractor_type
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.contractor_type_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).ToLower()).FirstOrDefault();
                    if (contractor_type != null) contractor_type_id = contractor_type.id.ToString();

                    string country_id = "";
                    var country = dbContext.country
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.country_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (country != null) country_id = country.id.ToString();

                    string bank_account_id = ""; 
                    string currency_id = "";
                    var bank_account = dbContext.bank_account
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.account_number == PublicFunctions.IsNullCell(row.GetCell(9))).FirstOrDefault();
                    if (bank_account != null)
                    {
                        bank_account_id = bank_account.id.ToString();
                        currency_id = bank_account.currency_id.ToString();
                    }

                    var record = dbContext.contractor
                        .Where(o => o.business_partner_code == PublicFunctions.IsNullCell(row.GetCell(1))
							&& o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.business_partner_name = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.contractor_type_id = contractor_type_id; kol++;
                        record.primary_address = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.country_id = country_id; kol++;
                        record.primary_contact_name = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;
                        record.primary_contact_email = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.primary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(7)); kol++;
                        record.bank_account_id = bank_account_id; kol++;
                        record.currency_id = currency_id; kol++;
                        record.is_taxable = PublicFunctions.BenarSalah(row.GetCell(11)); kol++;
                        record.is_equipment_owner = PublicFunctions.BenarSalah(row.GetCell(12)); kol++;
                        record.is_truck_owner = PublicFunctions.BenarSalah(row.GetCell(13)); kol++;
                        record.is_barge_owner = PublicFunctions.BenarSalah(row.GetCell(14)); kol++;
                        record.is_tug_owner = PublicFunctions.BenarSalah(row.GetCell(15)); kol++;
                        record.is_vessel_owner = PublicFunctions.BenarSalah(row.GetCell(16)); kol++;
                        record.is_train_owner = PublicFunctions.BenarSalah(row.GetCell(17)); kol++;
                        record.is_surveyor = PublicFunctions.BenarSalah(row.GetCell(18)); kol++;
                        record.is_other = PublicFunctions.BenarSalah(row.GetCell(19)); kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(20)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new contractor();
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
                        record.business_partner_code = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.contractor_type_id = contractor_type_id; kol++;
                        record.primary_address = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.country_id = country_id; kol++;
                        record.primary_contact_name = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;
                        record.primary_contact_email = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.primary_contact_phone = PublicFunctions.IsNullCell(row.GetCell(7)); kol++;
                        record.bank_account_id = bank_account_id; kol++;
                        record.currency_id = currency_id; kol++;
                        record.is_taxable = PublicFunctions.BenarSalah(row.GetCell(11)); kol++;
                        record.is_equipment_owner = PublicFunctions.BenarSalah(row.GetCell(12)); kol++;
                        record.is_truck_owner = PublicFunctions.BenarSalah(row.GetCell(13)); kol++;
                        record.is_barge_owner = PublicFunctions.BenarSalah(row.GetCell(14)); kol++;
                        record.is_tug_owner = PublicFunctions.BenarSalah(row.GetCell(15)); kol++;
                        record.is_vessel_owner = PublicFunctions.BenarSalah(row.GetCell(16)); kol++;
                        record.is_train_owner = PublicFunctions.BenarSalah(row.GetCell(17)); kol++;
                        record.is_surveyor = PublicFunctions.BenarSalah(row.GetCell(18)); kol++;
                        record.is_other = PublicFunctions.BenarSalah(row.GetCell(19)); kol++;
                        record.is_active = PublicFunctions.BenarSalah(row.GetCell(20)); kol++;

                        dbContext.contractor.Add(record);
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

                    string contractor_id = "";
                    var contractor = dbContext.contractor
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
                    if (contractor != null) contractor_id = contractor.id.ToString();

                    var record = dbContext.contractor_document
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.contractor_id == contractor_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.file_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new contractor_document();
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

                        record.contractor_id = contractor_id; kol++;
                        record.file_name = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;

                        dbContext.contractor_document.Add(record);
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

                    string business_partner_id = "";
                    var customer = dbContext.contractor
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
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
                        kol++;
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

                        record.business_partner_id = business_partner_id; kol++;
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
                HttpContext.Session.SetString("filename", "Contractor");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] contractor Record)
        {
            try
            {
                var record = dbContext.contractor
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
                    record = new contractor();
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

                    dbContext.contractor.Add(record);
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

    }
}
