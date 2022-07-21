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
using DataAccess.Select2;
using BusinessLogic.Entity;
using Hangfire;

namespace MCSWebApp.Controllers.API.SurveyManagement
{
    [Route("api/SurveyManagement/[controller]")]
    [ApiController]
    public class COWController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly mcsContext dbContext;

        public COWController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
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
            return await DataSourceLoader.LoadAsync(
                dbContext.draft_survey.Where(o => o.organization_id == CurrentUserContext.OrganizationId), 
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.draft_survey.Where(o => o.id == Id
                    && o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(draft_survey),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new draft_survey();
                    JsonConvert.PopulateObject(values, record);

                    //********** Validasi
                    if (!string.IsNullOrEmpty(record.despatch_order_id))
                    {
                        var tempDraftSuvey = dbContext.draft_survey
                            .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                && o.despatch_order_id == record.despatch_order_id)
                            .FirstOrDefault();
                        if (tempDraftSuvey != null)
                        {
                            return BadRequest($"Despatch Order has been use on other COW. Cannot use this Despatch Order anymore.");
                        }
                    }
                    //***************

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

                    dbContext.draft_survey.Add(record);
                    await dbContext.SaveChangesAsync();

                    #region Insert draft_survey analytes

                    if (!string.IsNullOrEmpty(record.sampling_template_id))
                    {
                        var st = dbContext.sampling_template
                            .Where(o => o.id == record.sampling_template_id)
                            .FirstOrDefault();
                        if(st != null)
                        {
                            var details = dbContext.sampling_template_detail
                                .Where(o => o.sampling_template_id == st.id)
                                .ToList();
                            if(details != null && details.Count > 0)
                            {
                                foreach (var d in details)
                                {
                                    dbContext.survey_analyte.Add(new survey_analyte()
                                    {
                                        id = Guid.NewGuid().ToString("N"),
                                        created_by = CurrentUserContext.AppUserId,
                                        created_on = DateTime.Now,
                                        owner_id = CurrentUserContext.AppUserId,
                                        organization_id = CurrentUserContext.OrganizationId,
                                        survey_id = record.id,
                                        analyte_id = d.analyte_id
                                    });
                                }

                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }

                    #endregion

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

        [HttpPut("UpdateData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateData([FromForm] string key, [FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.draft_survey
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (!string.IsNullOrEmpty(record.despatch_order_id))
                        {
                            var cekdata = dbContext.sales_invoice
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                    && o.despatch_order_id == record.despatch_order_id)
                                .FirstOrDefault();
                            if (cekdata != null)
                                return BadRequest("Can not be changed since the despatch order already been used in Sales Invoice.");
                        }

                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);

                            JsonConvert.PopulateObject(values, record);

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;
                            await dbContext.SaveChangesAsync();

                            if (!string.IsNullOrEmpty(record.sampling_template_id))
                            {
                                var details = dbContext.sampling_template_detail
                                    .FromSqlRaw(" SELECT * FROM sampling_template_detail "
                                        + $" WHERE sampling_template_id = '{record.sampling_template_id}' "
                                        + " AND analyte_id NOT IN ( "
                                        + " SELECT analyte_id FROM survey_analyte "
                                        + $" WHERE survey_id = '{record.id}' "
                                        + " ) ")
                                    .ToList();
                                if (details != null && details.Count > 0)
                                {
                                    foreach (var d in details)
                                    {
                                        var sa = await dbContext.survey_analyte.Where(o => o.analyte_id == d.analyte_id)
                                            .FirstOrDefaultAsync();
                                        if (sa == null)
                                        {
                                            dbContext.survey_analyte.Add(new survey_analyte()
                                            {
                                                id = Guid.NewGuid().ToString("N"),
                                                created_by = CurrentUserContext.AppUserId,
                                                created_on = DateTime.Now,
                                                owner_id = CurrentUserContext.AppUserId,
                                                organization_id = CurrentUserContext.OrganizationId,
                                                survey_id = record.id,
                                                analyte_id = d.analyte_id
                                            });
                                        }
                                    }

                                    await dbContext.SaveChangesAsync();
                                }
                            }

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
            draft_survey record;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.draft_survey
                        .Where(o => o.id == key)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (await mcsContext.CanDelete(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            if (record.approved_by != null)
                            {
                                return BadRequest("Record cannot be updated. Status is Closed.");
                            }

                            dbContext.draft_survey.Remove(record);
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
                    var _record = new DataAccess.Repository.draft_survey();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.DraftSurvey.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return Ok();
        }

        [HttpGet("StockLocationIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> StockLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                //var lookup = dbContext.stock_location
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                //    .Select(o => new { Value = o.id, Text = o.stock_location_name });

                var barges = dbContext.barge
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                .Select(o => new { Value = o.id, Text = o.vehicle_name });
                var vessels = dbContext.vessel
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                .Select(o => new { Value = o.id, Text = o.vehicle_name });
                var bv = barges.Union(vessels);

                return await DataSourceLoader.LoadAsync(bv, loadOptions);
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

        [HttpGet("SurveyorIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> SurveyorIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.business_partner
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.is_vendor == true)
                    .Select(o => new { Value = o.id, Text = o.business_partner_name });
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

        [HttpGet("DraftSurveyIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DraftSurveyIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.draft_survey
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.survey_number });
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
                var record = await dbContext.vw_draft_survey
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
        public async Task<IActionResult> SaveData([FromBody] draft_survey Record)
        {
            try
            {
                var record = dbContext.draft_survey
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
                    record = new draft_survey();
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

                    dbContext.draft_survey.Add(record);
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
                var record = dbContext.draft_survey
                    .Where(o => o.id == Id)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.draft_survey.Remove(record);
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

        [HttpGet("Approve")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Approve([FromQuery]string Id)
        {
            var result = new StandardResult();
            draft_survey record = null;

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    record = dbContext.draft_survey
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

            if (result.Success && record != null)
            {
                try
                {
                    var _record = new DataAccess.Repository.draft_survey();
                    _record.InjectFrom(record);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.DraftSurvey.UpdateStockState(connectionString, _record));
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
            }

            return new JsonResult(result);
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

                    var surveyor_id = "";
                    var surveyor = dbContext.business_partner
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code == PublicFunctions.IsNullCell(row.GetCell(2))).FirstOrDefault();
                    if (surveyor != null) surveyor_id = surveyor.id.ToString();

                    var stock_location_id = "";
                    var stock_location = dbContext.stock_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.stock_location_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(3)).ToLower()).FirstOrDefault();
                    if (stock_location != null) stock_location_id = stock_location.id.ToString();

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(6))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var sampling_template_id = "";
                    var sampling_template = dbContext.sampling_template
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.sampling_template_code == PublicFunctions.IsNullCell(row.GetCell(7))).FirstOrDefault();
                    if (sampling_template != null) sampling_template_id = sampling_template.id.ToString();

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.despatch_order_number == PublicFunctions.IsNullCell(row.GetCell(8))).FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var record = dbContext.draft_survey
                        .Where(o => o.survey_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).ToLower()
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
                        record.survey_date = Convert.ToDateTime(row.GetCell(1).ToString()); kol++;
                        record.surveyor_id = surveyor_id; kol++;
                        record.stock_location_id = stock_location_id; kol++;
                        record.product_id = product_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.sampling_template_id = sampling_template_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new draft_survey();
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

                        record.survey_number = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.survey_date = PublicFunctions.Tanggal(row.GetCell(1)); kol++;
                        record.surveyor_id = surveyor_id; kol++;
                        record.stock_location_id = stock_location_id; kol++;
                        record.product_id = product_id;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(5)); kol++;
                        record.uom_id = uom_id; kol++;
                        record.sampling_template_id = sampling_template_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;

                        dbContext.draft_survey.Add(record);
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

            sheet = wb.GetSheetAt(1); //*** detail sheet 1
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var survey_id = "";
                    var draft_survey = dbContext.draft_survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
                    if (draft_survey != null) survey_id = draft_survey.id.ToString();

                    var analyte_id = "";
                    var analyte = dbContext.analyte
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.analyte_name == PublicFunctions.IsNullCell(row.GetCell(1))).FirstOrDefault();
                    if (analyte != null) analyte_id = analyte.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.uom_symbol == PublicFunctions.IsNullCell(row.GetCell(2))).FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var record = dbContext.survey_analyte
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.analyte_id == analyte_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.uom_id = uom_id; kol++;
                        record.analyte_value = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new survey_analyte();
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

                        record.survey_id = survey_id; kol++;
                        record.analyte_id = analyte_id; kol++;
                        record.uom_id = uom_id; kol++;
                        record.analyte_value = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        dbContext.survey_analyte.Add(record);
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
            //***********

            sheet = wb.GetSheetAt(2); //*** detail sheet 2
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 1;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var survey_id = "";
                    var draft_survey = dbContext.draft_survey
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_number == PublicFunctions.IsNullCell(row.GetCell(0))).FirstOrDefault();
                    if (draft_survey != null) survey_id = draft_survey.id.ToString();

                    var record = dbContext.survey_detail
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.survey_id == survey_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;
                        kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(1)); kol++;
                        record.distance = PublicFunctions.Desimal(row.GetCell(2)); kol++;
                        record.elevation = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new survey_detail();
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

                        record.survey_id = survey_id; kol++;
                        record.quantity = PublicFunctions.Desimal(row.GetCell(1)); kol++;
                        record.distance = PublicFunctions.Desimal(row.GetCell(2)); kol++;
                        record.elevation = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        dbContext.survey_detail.Add(record);
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
                HttpContext.Session.SetString("filename", "DraftSurvey");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

        [HttpGet("select2")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Select2([FromQuery] string q)
        {
            var result = new Select2Response();

            try
            {
                var s2Request = new Select2Request()
                {
                    q = q
                };
                if (s2Request != null)
                {
                    var svc = new Survey(CurrentUserContext);
                    result = await svc.Select2(s2Request, "survey_number");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            return new JsonResult(result);
        }

        [HttpGet("ByDraftSurveyId/{Id}")]
        public async Task<object> ByDraftSurveyId(string Id, DataSourceLoadOptions loadOptions)
        {
            var record = dbContext.vw_draft_survey.Where(o => o.id == Id).FirstOrDefault();
            var quality_sampling_id = "";
            if (record != null) quality_sampling_id = record.quality_sampling_id;

            return await DataSourceLoader.LoadAsync(dbContext.vw_quality_sampling_analyte
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.quality_sampling_id == quality_sampling_id),
                loadOptions);
        }

        [HttpGet("LookupByDespatchOrderId/{despatchOrderId}")]
        public async Task<StandardResult> LookupByDespatchOrderId(string despatchOrderId)
        {
            var result = new StandardResult();
            result.Success = true;
            try
            {
                result.Data = await dbContext.vw_draft_survey.FirstOrDefaultAsync(o => o.despatch_order_id == despatchOrderId);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException.Message ?? ex.Message);
                result.Message = ex.InnerException.Message ?? ex.Message;
            }
            return result;
        }

    }
}
