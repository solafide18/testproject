using BusinessLogic;
using BusinessLogic.Entity;
using DataAccess.DTO;
using DataAccess.EFCore.Repository;
using DataAccess.Select2;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Omu.ValueInjecter;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;

namespace MCSWebApp.Controllers.API.StockpileManagement
{
    [Route("api/StockpileManagement/[controller]")]
    [ApiController]
    public class QualitySamplingController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly mcsContext dbContext;

        public QualitySamplingController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption,
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
            return await DataSourceLoader.LoadAsync(dbContext.vw_quality_sampling
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(dbContext.vw_quality_sampling
                .Where(o => o.id == Id
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
                    if (await mcsContext.CanCreate(dbContext, nameof(quality_sampling),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        var record = new quality_sampling();
                        JsonConvert.PopulateObject(values, record);

                        if (!string.IsNullOrEmpty(record.despatch_order_id))
                        {
                            var tempQualitySampling = dbContext.quality_sampling
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                    && o.despatch_order_id == record.despatch_order_id)
                                .FirstOrDefault();
                            if (tempQualitySampling != null)
                            {
                                return BadRequest($"Despatch Order has been use on other Quality Sampling. Cannot use this Despatch Order anymore.");
                            }
                        }

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

                        dbContext.quality_sampling.Add(record);
                        await dbContext.SaveChangesAsync();

                        #region Insert quality_sampling analytes

                        if (!string.IsNullOrEmpty(record.sampling_template_id))
                        {
                            var st = dbContext.sampling_template
                                .Where(o => o.id == record.sampling_template_id)
                                .FirstOrDefault();
                            if (st != null)
                            {
                                if ((st.is_despatch_order_required ?? false) && string.IsNullOrEmpty(record.despatch_order_id))
                                {
                                    return BadRequest($"Despatch Order is required on sampling template {st.sampling_template_name}. Please select Despatch Order.");
                                }

                                //if (!string.IsNullOrEmpty(record.despatch_order_id))
                                //{
                                //    var tempQualitySampling = dbContext.quality_sampling
                                //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                                //            && o.despatch_order_id == record.despatch_order_id)
                                //        .FirstOrDefault();
                                //    if (tempQualitySampling != null)
                                //    {
                                //        return BadRequest($"Despatch Order has been use on other Quality Sampling. Cannot use this Despatch Order anymore.");
                                //    }
                                //}

                                var details = dbContext.sampling_template_detail
                                    .Where(o => o.sampling_template_id == st.id)
                                    .ToList();
                                if (details != null && details.Count > 0)
                                {
                                    foreach (var d in details)
                                    {
                                        dbContext.quality_sampling_analyte.Add(new quality_sampling_analyte()
                                        {
                                            id = Guid.NewGuid().ToString("N"),
                                            created_by = CurrentUserContext.AppUserId,
                                            created_on = DateTime.Now,
                                            owner_id = CurrentUserContext.AppUserId,
                                            organization_id = CurrentUserContext.OrganizationId,
                                            quality_sampling_id = record.id,
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
                    var record = dbContext.quality_sampling
                        .Where(o => o.id == key
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        //if (!string.IsNullOrEmpty(record.despatch_order_id))
                        //{
                        //    var cekdata = dbContext.sales_invoice
                        //        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        //            && o.despatch_order_id == record.despatch_order_id)
                        //        .FirstOrDefault();
                        //    if (cekdata != null)
                        //        return BadRequest("Can not be changed since the despatch order already been used in Sales Invoice.");
                        //}

                        if (await mcsContext.CanUpdate(dbContext, record.id, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            var e = new entity();
                            e.InjectFrom(record);

                            JsonConvert.PopulateObject(values, record);

                            record.InjectFrom(e);
                            record.modified_by = CurrentUserContext.AppUserId;
                            record.modified_on = DateTime.Now;

                            var st = dbContext.sampling_template
                                .Where(o => o.id == record.sampling_template_id)
                                .FirstOrDefault();
                            if (st != null)
                            {
                                if ((st.is_despatch_order_required ?? false) && string.IsNullOrEmpty(record.despatch_order_id))
                                {
                                    return BadRequest($"Despatch Order is required on sampling template {st.sampling_template_name}. Please select Despatch Order.");
                                }
                            }

                            if (!string.IsNullOrEmpty(record.despatch_order_id))
                            {
                                //var tempQualitySampling = dbContext.quality_sampling
                                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId 
                                //        && o.despatch_order_id == record.despatch_order_id && o.id != record.despatch_order_id)
                                //    .FirstOrDefault();
                                var tempQualitySampling = dbContext.quality_sampling
                                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                        && o.id != record.id && o.despatch_order_id == record.despatch_order_id)
                                    .FirstOrDefault();
                                if (tempQualitySampling != null)
                                {
                                    return BadRequest($"Despatch Order has been use on other Quality Sampling. Cannot use this Despatch Order anymore.");
                                }
                            }

                            await dbContext.SaveChangesAsync();

                            //if (!string.IsNullOrEmpty(record.sampling_template_id))
                            //{
                            //    var details = dbContext.sampling_template_detail
                            //        .FromSqlRaw(" SELECT * FROM sampling_template_detail "
                            //            + $" WHERE sampling_template_id = {record.sampling_template_id} "
                            //            + " AND analyte_id NOT IN ( "
                            //            + " SELECT analyte_id FROM survey_analyte "
                            //            + $" WHERE quality_sampling_id = {record.id} "
                            //            + " ) ")
                            //        .ToList();
                            //    if (details != null && details.Count > 0)
                            //    {
                            //        foreach (var d in details)
                            //        {
                            //            var sa = await dbContext.quality_sampling_analyte
                            //                .Where(o => o.analyte_id == d.analyte_id)
                            //                .FirstOrDefaultAsync();
                            //            if (sa == null)
                            //            {
                            //                dbContext.quality_sampling_analyte.Add(
                            //                    new quality_sampling_analyte()
                            //                {
                            //                    id = Guid.NewGuid().ToString("N"),
                            //                    created_by = CurrentUserContext.AppUserId,
                            //                    created_on = DateTime.Now,
                            //                    owner_id = CurrentUserContext.AppUserId,
                            //                    organization_id = CurrentUserContext.OrganizationId,
                            //                    quality_sampling_id = record.id,
                            //                    analyte_id = d.analyte_id
                            //                });
                            //            }
                            //        }

                            //        await dbContext.SaveChangesAsync();
                            //    }
                            //}

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

            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.quality_sampling
                        .Where(o => o.id == key
                            && o.organization_id == CurrentUserContext.OrganizationId)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        if (!string.IsNullOrEmpty(record.despatch_order_id))
                        {
                            return BadRequest("Can not be deleted since Despatch Order is not empty.");
                        }

                        if (await mcsContext.CanDelete(dbContext, key, CurrentUserContext.AppUserId)
                            || CurrentUserContext.IsSysAdmin)
                        {
                            dbContext.quality_sampling.Remove(record);
                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
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

            return Ok();
        }

        [HttpGet("StockpileLocationIdLookup")]
        public async Task<object> StockpileLocationIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var stockpile = dbContext.vw_stockpile_location
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o =>
                        new
                        {
                            value = o.id,
                            text =  (o.business_area_name != null) ?  o.business_area_name + " > " + o.stock_location_name :  o.stock_location_name,
                            index = 1
                        });;
                var barges = dbContext.barge
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                .Select(o => new { value = o.id, text = "Barge > "  +  o.vehicle_name, index = 2 });
                var vessels = dbContext.vessel
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                                .Select(o => new { value = o.id, text = "Vessel > " +  o.vehicle_name, index = 3 });
                var lookup = stockpile.Union(barges).Union(vessels).OrderBy(o => o.index);
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

        [HttpGet("SamplingTemplateIdLookup")]
        public async Task<object> SamplingTemplateIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.sampling_template
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.sampling_template_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("SurveyorIdLookup")]
        public async Task<object> SurveyorIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                //var lookup = dbContext.business_partner
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                //        && o.is_vendor == true)
                //    .Select(o => new { Value = o.id, Text = o.business_partner_name });

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

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.vw_quality_sampling
                    .Where(o => o.id == Id
                        && o.organization_id == CurrentUserContext.OrganizationId)
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
        public async Task<IActionResult> SaveData([FromBody] survey Record)
        {
            try
            {
                var record = dbContext.quality_sampling
                    .Where(o => o.id == Record.id
                        && o.organization_id == CurrentUserContext.OrganizationId)
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
                    record = new quality_sampling();
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

                    dbContext.quality_sampling.Add(record);
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
                var record = dbContext.quality_sampling
                    .Where(o => o.id == Id
                        && o.organization_id == CurrentUserContext.OrganizationId)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.quality_sampling.Remove(record);
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

        [HttpPost("ApplyToTransactions")]
        public async Task<IActionResult> ApplyToTransactions([FromBody] dynamic Data)
        {
            var result = new StandardResult();

            try
            {
                if (Data != null && Data.id != null && Data.production_ids != null)
                {
                    var category = (string)Data.category;
                    var id = (string)Data.id;
                    logger.Debug($"Category = {category}");
                    logger.Debug($"Quality sampling id = {id}");
                    logger.Debug($"Production tx id = {(string)Data.production_ids}");

                    var production_ids = ((string)Data.production_ids)
                        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    var qs = new BusinessLogic.Entity.QualitySampling(CurrentUserContext);
                    result = await qs.ApplyToTransactions(category, id, production_ids);
                    logger.Debug($"{JsonConvert.SerializeObject(result)}");
                }
                else
                {
                    result.Message = "Invalid data.";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                result.Message = ex.Message;
            }

            if (result.Success)
            {
                try
                {
                    var _repo = new QualitySampling(CurrentUserContext);
                    var _record = await _repo.GetByIdAsync((string)Data.id);
                    var connectionString = CurrentUserContext.GetDataContext().Database.ConnectionString;
                    _backgroundJobClient.Enqueue(() => BusinessLogic.Entity.QualitySampling.UpdateStockState(connectionString, _record));
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
                    var surveyor = dbContext.contractor
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.business_partner_code.Trim() == PublicFunctions.IsNullCell(row.GetCell(2)).Trim())
                        .FirstOrDefault();
                    if (surveyor != null) surveyor_id = surveyor.id.ToString();

                    var stock_location_id = "";
                    //var stock_location = dbContext.stockpile_location
                    //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                    //        o.stockpile_location_code == PublicFunctions.IsNullCell(row.GetCell(3))).FirstOrDefault();

                    var stock_location = dbContext.stockpile_location
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.stockpile_location_code == PublicFunctions.IsNullCell(row.GetCell(3))).FirstOrDefault();
                    if (stock_location != null)
                        stock_location_id = stock_location.id.ToString();
                    else
                    {
                        var barges = dbContext.barge
                            .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                && o.vehicle_id.Trim() == PublicFunctions.IsNullCell(row.GetCell(3)).Trim())
                            .FirstOrDefault();
                        if (barges != null)
                            stock_location_id = barges.id.ToString();
                        else
                        {
                            var vessels = dbContext.vessel
                                .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                                    && o.vehicle_id.Trim() == PublicFunctions.IsNullCell(row.GetCell(3)).Trim())
                                .FirstOrDefault();
                            if (vessels != null)
                                stock_location_id = vessels.id.ToString();
                        }
                    }

                    var product_id = "";
                    var product = dbContext.product
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.product_code == PublicFunctions.IsNullCell(row.GetCell(4))).FirstOrDefault();
                    if (product != null) product_id = product.id.ToString();

                    var sampling_template_id = "";
                    var sampling_template = dbContext.sampling_template
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId && 
                            o.sampling_template_code == PublicFunctions.IsNullCell(row.GetCell(5))).FirstOrDefault();
                    if (sampling_template != null) sampling_template_id = sampling_template.id.ToString();

                    var despatch_order_id = "";
                    var despatch_order = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.despatch_order_number.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(6)).Trim().ToLower())
                        .FirstOrDefault();
                    if (despatch_order != null) despatch_order_id = despatch_order.id.ToString();

                    var record = dbContext.quality_sampling
                        .Where(o => o.sampling_number == PublicFunctions.IsNullCell(row.GetCell(0))
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
                        record.sampling_datetime = PublicFunctions.Waktu(row.GetCell(1)); kol++;
                        record.surveyor_id = surveyor_id; kol++;
                        record.stock_location_id = stock_location_id; kol++;
                        record.product_id = product_id; kol++;
                        record.sampling_template_id = sampling_template_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new quality_sampling();
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

                        record.sampling_number = PublicFunctions.IsNullCell(row.GetCell(0)); kol++;
                        record.sampling_datetime = PublicFunctions.Waktu(row.GetCell(1)); kol++;
                        record.surveyor_id = surveyor_id; kol++;
                        record.stock_location_id = stock_location_id; kol++;
                        record.product_id = product_id; kol++;
                        record.sampling_template_id = sampling_template_id; kol++;
                        record.despatch_order_id = despatch_order_id; kol++;

                        dbContext.quality_sampling.Add(record);
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


            sheet = wb.GetSheetAt(1); //*** detail sheet
            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
            {
                int kol = 2;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var quality_sampling_id = "";
                    var quality_sampling = dbContext.quality_sampling
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.sampling_number.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(0)).Trim().ToLower())
                        .FirstOrDefault();
                    if (quality_sampling != null) quality_sampling_id = quality_sampling.id.ToString();

                    var analyte_id = "";
                    var analyte = dbContext.analyte
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.analyte_symbol.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(1)).Trim().ToLower())
                        .FirstOrDefault();
                    if (analyte != null) analyte_id = analyte.id.ToString();

                    var uom_id = "";
                    var uom = dbContext.uom
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.uom_symbol.Trim().ToLower() == PublicFunctions.IsNullCell(row.GetCell(2)).Trim().ToLower())
                        .FirstOrDefault();
                    if (uom != null) uom_id = uom.id.ToString();

                    var record = dbContext.quality_sampling_analyte
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.quality_sampling_id == quality_sampling_id
                            && o.analyte_id == analyte_id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        var e = new entity();
                        e.InjectFrom(record);

                        record.InjectFrom(e);
                        record.modified_by = CurrentUserContext.AppUserId;
                        record.modified_on = DateTime.Now;

                        //record.analyte_id = analyte_id; kol++;
                        record.uom_id = uom_id; kol++;
                        record.analyte_value = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new quality_sampling_analyte();
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

                        record.quality_sampling_id = quality_sampling_id; kol++;
                        record.analyte_id = analyte_id; kol++;
                        record.uom_id = uom_id; kol++;
                        record.analyte_value = PublicFunctions.Desimal(row.GetCell(3)); kol++;

                        dbContext.quality_sampling_analyte.Add(record);
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
            wb.Close();
            if (gagal)
            {
                await transaction.RollbackAsync();
                HttpContext.Session.SetString("errormessage", teks);
                HttpContext.Session.SetString("filename", "QualitySampling");
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
                    var svc = new QualitySampling(CurrentUserContext);
                    result = await svc.Select2(s2Request, "sampling_number");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            return new JsonResult(result);
        }
    }
}
