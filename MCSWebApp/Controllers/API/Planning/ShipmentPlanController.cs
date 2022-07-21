using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using DataAccess.EFCore;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DataAccess.DTO;
using Omu.ValueInjecter;
using Microsoft.EntityFrameworkCore;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Hosting;
using BusinessLogic;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;

namespace MCSWebApp.Controllers.API.Planning
{
    [Route("api/Planning/[controller]")]
    [ApiController]
    public class ShipmentPlanController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public ShipmentPlanController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions, string latestUpdate)
        {
            try
            {
                //return await DataSourceLoader.LoadAsync(dbContext.vw_shipment_plan_report, loadOptions);
                return await DataSourceLoader.LoadAsync(dbContext.vw_shipment_plan, loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("SalesContractIdLookup")]
        public async Task<object> SalesContractLookup(DataSourceLoadOptions loadOptions, string CustomerId)
        {
            try
            {
                if (CustomerId != null)
                {
                    var lookup = dbContext.sales_contract
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                            o.customer_id == CustomerId)
                        .Select(o => new { Value = o.id, Text = o.sales_contract_name });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    var lookup = dbContext.sales_contract
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { Value = o.id, Text = o.sales_contract_name });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
        [HttpGet("ShipmentPlanByID")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ShipmentPlanByID(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_shipment_plan.Where(o => o.id == Id),
                loadOptions);
        }
        [HttpGet("ShipmentPlanIdLookup")]
        public async Task<object> ShipmentPlanIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.vw_shipment_plan
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.shipment_code + " - " + o.business_partner_name + " - " + o.sales_contract_name });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("MonthIndexLookup")]
        public object MonthIndexLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var months = new Dictionary<int, string>();
                for (var i = 1; i <= 12; i++)
                {
                    months.Add(i, i.ToString("00") + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i).ToUpper());
                }
                var lookup = months
                    .Select(o => new { Value = o.Key, Text = o.Value });
                return DataSourceLoader.Load(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("YearIdLookup")]
        public object YearIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var yearlist = new Dictionary<int, int>();
                for(var i = 2022; i <= 2040; i++)
                {
                    yearlist.Add(i, i);
                }
                var lookup = yearlist
                    .Select(o => new { Value = o.Key, Text = o.Value });
                return DataSourceLoader.Load(lookup, loadOptions);
            }
			catch (Exception ex)
			{
				logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
        }

        [HttpGet("GenerateReport")]
        public object GenerateReport()
        {
            //try
            //{
            //    var temp = dbContext.vw_shipment_plan.Where(x => x.year == DateTime.Now.Year);
            //    List<vw_shipment_plan> result = new List<vw_shipment_plan>();
            //    int ctr = 1;
            //    double monthTemp = 1;
            //    foreach(var item in temp)
            //    {
            //        if(item.month != monthTemp)
            //        {
            //            monthTemp = (double)item.month;
            //            ctr = 1;
            //            item.no = ctr.ToString();
            //        }
            //        else
            //        {
            //            item.no = ctr.ToString();
            //            ctr++;
            //        }
            //        result.Add(item);
            //    }
            //    var tempForecast = dbContext.shipment_forecast.Where(x => x.year == DateTime.Now.Year);
            //    if(tempForecast != null && tempForecast.Count() > 0)
            //    {
            //        foreach (var item in tempForecast)
            //        {
            //            vw_shipment_plan vwTemp = new vw_shipment_plan
            //            {
            //                id = item.id
            //                , year = (double)item.year
            //                , month = (double)item.month
            //                , no = item.no
            //                , customer_id = item.customer_id
            //                , business_partner_name = item.business_partner_name
            //                , country_name = item.country_name
            //                , shipment_no = item.shipment_no
            //                , total_shipment = (long)item.total_shipment
            //                , delivery_term = item.delivery_term
            //                , vessel_id = item.vessel_id
            //                , vessel = item.vessel
            //                , laycan_start = item.laycan_start
            //                , laycan_end = item.laycan_end
            //                , order_reference_date = item.order_reference_date
            //                , quantity_plan = item.quantity_plan
            //                , quantity_actual = item.quantity_actual
            //                , eta = item.eta
            //                , comm_date = item.comm_date
            //                , bl_date = item.bl_date
            //                , remark = item.remark
            //                , traffic_officer = item.traffic_officer
            //                , payment_method = item.payment_method
            //                , destination_bank = item.destination_bank
            //                , invoice_ref = item.invoice_ref
            //                , invoice_amount = item.invoice_amount
            //                , invoice_date = item.invoice_date
            //                , invoice_price = item.invoice_price
            //                , exchange_rate = item.exchange_rate
            //                , si_currency_id = item.si_currency_id
            //                , scpt_currency_id = item.scpt_currency_id
            //                , invoice_due_date = item.invoice_due_date
            //                , tax_invoice_ref_no = item.tax_invoice_ref_no
            //                , payment_receiving_date = item.payment_receiving_date
            //                , amount_received = item.amount_received
            //            };
            //            result.Add(vwTemp);
            //        }

            //    }
            //    //temp = result.AsQueryable();
            //    return result.OrderBy(x => x.month);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(ex.Message);
            //}
            return null;
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                if (await mcsContext.CanCreate(dbContext, nameof(shipment_plan),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    var record = new shipment_plan();
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

                    dbContext.shipment_plan.Add(record);
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
                var record = dbContext.shipment_plan
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
                var despatch_order = dbContext.despatch_order
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                        && o.despatch_plan_id == key).FirstOrDefault();
                if (despatch_order != null) return BadRequest("Can not be deleted since it is already have one or more transactions.");

                var record = dbContext.shipment_plan
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    dbContext.shipment_plan.Remove(record);
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

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] shipment_plan Record)
        {
            try
            {
                var record = dbContext.shipment_plan
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
                        return BadRequest("User is not authorized.");
                    }
                }
                else if (await mcsContext.CanCreate(dbContext, nameof(shipment_plan),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    record = new shipment_plan();
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

                    dbContext.shipment_plan.Add(record);
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

        [HttpGet("Detail/{Id}")]
        public async Task<IActionResult> Detail(string Id)
        {
            try
            {
                var record = await dbContext.vw_shipment_plan
                    .Where(o => o.id == Id)
                    .FirstOrDefaultAsync();
                if (record != null)
                {
                    if (await mcsContext.CanRead(dbContext, Id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)
                    {
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

        [HttpDelete("Delete/{Id}")]
        public async Task<IActionResult> DeleteById(string Id)
        {
            logger.Trace($"string Id = {Id}");

            if (await mcsContext.CanDelete(dbContext, Id, CurrentUserContext.AppUserId)
                || CurrentUserContext.IsSysAdmin)
            {
                try
                {
                    var record = dbContext.shipment_plan
                        .Where(o => o.id == Id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        dbContext.shipment_plan.Remove(record);
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
            if (!Directory.Exists(FilePath)) Directory.CreateDirectory(FilePath);

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
                int kol = 2;
                try
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    string sales_contract_id = "";
                    var sales_contract = dbContext.sales_contract
                        .Where(o => o.sales_contract_name == PublicFunctions.IsNullCell(row.GetCell(0))
                            && o.organization_id == CurrentUserContext.OrganizationId).FirstOrDefault();
                    if (sales_contract != null) sales_contract_id = sales_contract.id.ToString();

                    string customer_id = "";
                    var customer = dbContext.customer
                        .Where(o => o.business_partner_code == PublicFunctions.IsNullCell(row.GetCell(4))
                            && o.organization_id == CurrentUserContext.OrganizationId).FirstOrDefault();
                    if (customer != null) customer_id = customer.id.ToString();

                    string transport_id = "";
                    var vessel = dbContext.vessel
                        .Where(o => o.vehicle_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(7)).ToLower()
                            && o.organization_id == CurrentUserContext.OrganizationId).FirstOrDefault();
                    if (vessel != null) 
                        transport_id = vessel.id.ToString();
                    else
                    {
                        var barge = dbContext.barge
                            .Where(o => o.vehicle_name.ToLower() == PublicFunctions.IsNullCell(row.GetCell(7)).ToLower()
                                && o.organization_id == CurrentUserContext.OrganizationId)
                            .FirstOrDefault();
                        if (barge != null) transport_id = barge.id.ToString();
                    }

                    string employee_id = "";
                    var employee = dbContext.employee
                        .Where(o => o.employee_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(12)).ToLower()
                            && o.organization_id == CurrentUserContext.OrganizationId).FirstOrDefault();
                    if (employee != null) employee_id = employee.id.ToString();

                    var record = dbContext.shipment_plan
                        .Where(o => o.shipment_number.ToLower() == PublicFunctions.IsNullCell(row.GetCell(5)).ToLower()
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
                        record.sales_contract_id = sales_contract_id; kol++;
                        record.shipment_year = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.month_id = PublicFunctions.Bulat(row.GetCell(2)); kol++;
                        record.destination = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.customer_id = customer_id; kol++;
                        record.incoterm = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.transport_id = transport_id; kol++;
                        record.laycan = PublicFunctions.IsNullCell(row.GetCell(8)); kol++;
                        record.eta = Convert.ToDateTime(PublicFunctions.Tanggal(row.GetCell(9))); kol++;
                        record.qty_sp = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.remarks = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.traffic_officer_id = employee_id; kol++;

                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        record = new shipment_plan();
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

                        record.sales_contract_id = sales_contract_id; kol++;
                        record.shipment_year = PublicFunctions.IsNullCell(row.GetCell(1)); kol++;
                        record.month_id = PublicFunctions.Bulat(row.GetCell(2)); kol++;
                        record.destination = PublicFunctions.IsNullCell(row.GetCell(3)); kol++;
                        record.customer_id = customer_id; kol++;
                        record.shipment_number = PublicFunctions.IsNullCell(row.GetCell(5)); kol++;
                        record.incoterm = PublicFunctions.IsNullCell(row.GetCell(6)); kol++;
                        record.transport_id = transport_id; kol++;
                        record.laycan = PublicFunctions.IsNullCell(row.GetCell(8)); kol++;
                        record.eta = Convert.ToDateTime(PublicFunctions.Tanggal(row.GetCell(9))); kol++;
                        record.qty_sp = PublicFunctions.Desimal(row.GetCell(10)); kol++;
                        record.remarks = PublicFunctions.IsNullCell(row.GetCell(11)); kol++;
                        record.traffic_officer_id = employee_id; kol++;

                        dbContext.shipment_plan.Add(record);
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
            wb.Close();
            if (gagal)
            {
                await transaction.RollbackAsync();
                HttpContext.Session.SetString("errormessage", teks);
                HttpContext.Session.SetString("filename", "ShipmentPlan");
                return BadRequest("File gagal di-upload");
            }
            else
            {
                await transaction.CommitAsync();
                return "File berhasil di-upload!";
            }
        }

    }
}
