using DataAccess.DTO;
using DataAccess.EFCore.Repository;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using Omu.ValueInjecter;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using DataAccess.EFCore;
using Microsoft.EntityFrameworkCore;
using HiSystems.Interpreter;
using BusinessLogic;
using System.IO;
using Common;
using System.Dynamic;
using DataAccess.Select2;
using BusinessLogic.Entity;
using Microsoft.AspNetCore.Http.Extensions;

namespace MCSWebApp.Controllers.API.Sales
{
    [Route("api/Sales/[controller]")]
    [ApiController]
    public class SalesInvoiceController : ApiBaseController
    {
        private const string stringQuotationPeriod_4LIBL = "4LIBL";
        private const string stringQuotationPeriod_4LI1LD = "4LI1LD";
        private const string stringQuotationPeriod_QBQBL = "QBQBL";
        private const string stringQuotationPeriod_QBQLD = "QBQLD";
        private const string stringQuotationPeriod_MILD = "MILD";
        private const string stringQuotationPeriod_MIBLD = "MIBLD";

        private const string stringPricingMethod_Calculated = "Calculated";
        private const string stringPricingMethod_Fixed = "Fixed";
        private const string stringsalesChargeType_discount= "discount";
        private const string stringsalesChargeType_premium = "premium";
        private const string stringsalesChargeType_adjustment = "adjustment";
        private const string stringQuotationPrice = "currentinvoiceunitprice()";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        private double globalRemainedCreditLimit = 0.0;
        private string globalCustomerId;

        public SalesInvoiceController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
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
                    dbContext.vw_sales_invoice.Where(o => o.organization_id == CurrentUserContext.OrganizationId),
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
                dbContext.vw_sales_invoice.Where(o => o.id == Id),
                loadOptions);
        }

        //[HttpPost("InsertApproval")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<IActionResult> InsertApproval([FromForm] string key, [FromForm] string values)
        //{
        //    try
        //    {
        //        var record = dbContext.sales_invoice_approval
        //            .Where(o => o.sales_invoice_id == key)
        //            .FirstOrDefault();
        //        if (record != null)
        //        {
        //            var e = new entity();
        //            e.InjectFrom(record);

        //            JsonConvert.PopulateObject(values, record);

        //            record.InjectFrom(e);
                    
        //            record.modified_by = CurrentUserContext.AppUserId;
        //            record.modified_on = System.DateTime.Now;

        //            if (record.approve_status == "APPROVED")
        //            {
        //                record.approve_status = "UNAPPROVED";
        //                record.disapprove_by_id = CurrentUserContext.AppUserId;

        //                //var sales_invoice_ell = dbContext.sales_invoice_ell
        //                //    .Where(o => o.id == key && o.sync_type == "INSERT" && o.sync_status == "SUCCESS")
        //                //    .FirstOrDefault();
        //                //if (sales_invoice_ell != null)
        //                //{
        //                //    sales_invoice_ell.sync_status = "PENDING";
        //                //    sales_invoice_ell.error_msg = null;
        //                //    sales_invoice_ell.response_code = null;
        //                //    sales_invoice_ell.canceled = true;
        //                //}
        //            }
        //            else
        //            {
        //                record.approve_status = "APPROVED";
        //                record.approve_by_id = CurrentUserContext.AppUserId;

        //                //var sales_invoice_ell = dbContext.sales_invoice_ell
        //                //    .Where(o => o.id == key && o.sync_status == "PENDING")
        //                //    .FirstOrDefault();
        //                //if (sales_invoice_ell != null)
        //                //{
        //                //    sales_invoice_ell.sync_status = null;
        //                //    sales_invoice_ell.error_msg = null;
        //                //    sales_invoice_ell.response_text = null;
        //                //}
        //            }

        //            await dbContext.SaveChangesAsync();

        //            return Ok(record);
        //        }
        //        else
        //        //if (await mcsContext.CanCreate(dbContext, nameof(sales_invoice_approval),
        //        //    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
        //        {
        //            var newRec = new sales_invoice_approval();
        //            JsonConvert.PopulateObject(values, newRec);

        //            newRec.id = Guid.NewGuid().ToString("N");
        //            newRec.created_by = CurrentUserContext.AppUserId;
        //            newRec.created_on = System.DateTime.Now;
        //            newRec.modified_by = null;
        //            newRec.modified_on = null;
        //            newRec.is_active = true;
        //            newRec.is_default = null;
        //            newRec.is_locked = null;
        //            newRec.entity_id = null;
        //            newRec.owner_id = CurrentUserContext.AppUserId;
        //            newRec.organization_id = CurrentUserContext.OrganizationId;

        //            newRec.approve_status = "APPROVED";
        //            newRec.approve_by_id = CurrentUserContext.AppUserId;

        //            dbContext.sales_invoice_approval.Add(newRec);
        //            await dbContext.SaveChangesAsync();

        //            return Ok(newRec);
        //        }
        //        //else
        //        //{
        //        //    return BadRequest("User is not authorized.");
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex.InnerException ?? ex);
        //        return BadRequest(ex.InnerException?.Message ?? ex.Message);
        //    }
        //}

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertData([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
				if (await mcsContext.CanCreate(dbContext, nameof(sales_invoice),
					CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
				{
                    var record = new sales_invoice();
                    JsonConvert.PopulateObject(values, record);

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = System.DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
                    record.is_active = true;
                    record.is_default = null;
                    record.is_locked = null;
                    record.entity_id = null;
                    record.owner_id = CurrentUserContext.AppUserId;
                    record.organization_id = CurrentUserContext.OrganizationId;

                    // update table sales_invoice_charges
                    vw_sales_contract_charges vwSalesContractCharges = new vw_sales_contract_charges();
                    JsonConvert.PopulateObject(values, vwSalesContractCharges);
                    var records_vw_sales_contract_charge = dbContext.vw_sales_contract_charges
                        .Where(o => o.sales_contract_term_id == vwSalesContractCharges.sales_contract_term_id && o.sales_charge_type_name == "Adjustment")
                        ;
                    if (records_vw_sales_contract_charge.Count() > 0)
                    {
                        var array_vw_contract_charges = await records_vw_sales_contract_charge.ToArrayAsync();
                        foreach (vw_sales_contract_charges item_contract_charge in array_vw_contract_charges)
                        {
                            var recordSalesCharge = new sales_invoice_charges();
                            JsonConvert.PopulateObject(values, recordSalesCharge);

                            recordSalesCharge.id = Guid.NewGuid().ToString("N");
                            recordSalesCharge.created_by = CurrentUserContext.AppUserId;
                            recordSalesCharge.created_on = System.DateTime.Now;
                            recordSalesCharge.modified_by = null;
                            recordSalesCharge.modified_on = null;
                            recordSalesCharge.is_active = true;
                            recordSalesCharge.is_default = null;
                            recordSalesCharge.is_locked = null;
                            recordSalesCharge.entity_id = null;
                            recordSalesCharge.owner_id = CurrentUserContext.AppUserId;
                            recordSalesCharge.organization_id = CurrentUserContext.OrganizationId;
                            recordSalesCharge.sales_invoice_id = record.id;
                            recordSalesCharge.sales_charge_id = item_contract_charge.sales_charge_id;
                            recordSalesCharge.sales_charge_code = item_contract_charge.sales_charge_code;
                            recordSalesCharge.sales_charge_name = item_contract_charge.sales_charge_name;

                            var valuesArray = values.Split("\"" + item_contract_charge.sales_charge_code + "\"");
                            if (valuesArray.Length>1)
                            {
                                var valuesArray2 = valuesArray[1].Split(',');
                                var isPrice = valuesArray2[0].Substring(1).Replace(".", ",");

                                CultureInfo culture_curr = CultureInfo.CurrentCulture;
                                CultureInfo.CurrentCulture = new CultureInfo("en-ID", false);
                                recordSalesCharge.price = Convert.ToDecimal(isPrice, CultureInfo.CurrentCulture);
                                CultureInfo.CurrentCulture = culture_curr;

                                dbContext.sales_invoice_charges.Add(recordSalesCharge);
                            }

                        }

                    }

                    // look up currency_id from view vw_sales_contract_term
                    if ((record.currency_id == null) || (record.currency_id == ""))
                    {
                        var record_vw_sales_contract_term = await dbContext.vw_sales_contract_term
                        .Where(o => o.id == vwSalesContractCharges.sales_contract_term_id).FirstOrDefaultAsync()
                        ;
                        record.currency_id = record_vw_sales_contract_term.currency_id;
                    }

                    // update table customer
                    var recordCustId = dbContext.customer
                        .Where(o => o.id == globalCustomerId)
                        .FirstOrDefault();
                    if (recordCustId != null)
                    {
                        var e = new entity();
                        e.InjectFrom(recordCustId);

                        JsonConvert.PopulateObject(values, recordCustId);

                        recordCustId.InjectFrom(e);
                        recordCustId.modified_by = CurrentUserContext.AppUserId;
                        recordCustId.modified_on = System.DateTime.Now;
                        recordCustId.remained_credit_limit = (decimal)globalRemainedCreditLimit;
                        dbContext.customer.Update(recordCustId);
                        
                    }
                    
                    dbContext.sales_invoice.Add(record);
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
                var record = dbContext.sales_invoice
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    var e = new entity();
                    e.InjectFrom(record);

                    JsonConvert.PopulateObject(values, record);

                    record.InjectFrom(e);
                    record.modified_by = CurrentUserContext.AppUserId;
                    record.modified_on = System.DateTime.Now;

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

        [HttpPost("SaveData")]
        public async Task<IActionResult> SaveData([FromBody] sales_invoice Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.sales_invoice
                        .Where(o => o.id == Record.id)
                        .FirstOrDefaultAsync();
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
                            record.modified_on = System.DateTime.Now;

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok(record);
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else if (await mcsContext.CanCreate(dbContext, nameof(sales_invoice),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        record = new sales_invoice();
                        record.InjectFrom(Record);

                        record.id = Guid.NewGuid().ToString("N");
                        record.created_by = CurrentUserContext.AppUserId;
                        record.created_on = System.DateTime.Now;
                        record.modified_by = null;
                        record.modified_on = null;
                        record.is_active = true;
                        record.is_default = null;
                        record.is_locked = null;
                        record.entity_id = null;
                        record.owner_id = CurrentUserContext.AppUserId;
                        record.organization_id = CurrentUserContext.OrganizationId;

                        dbContext.sales_invoice.Add(record);
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
					logger.Error(ex.InnerException ?? ex);
					return BadRequest(ex.InnerException?.Message ?? ex.Message);
				}
            }
        }

        [HttpPost("SaveDataPayment")]
        public async Task<IActionResult> SaveDataPayment([FromBody] sales_invoice_payment Record)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = await dbContext.sales_invoice_payment
                        .Where(o => o.id == Record.id)
                        .FirstOrDefaultAsync();
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
                            record.modified_on = System.DateTime.Now;

                            await dbContext.SaveChangesAsync();
                            await tx.CommitAsync();
                            return Ok(record);
                        }
                        else
                        {
                            return BadRequest("User is not authorized.");
                        }
                    }
                    else if (await mcsContext.CanCreate(dbContext, nameof(sales_invoice_payment),
                        CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                    {
                        record = new sales_invoice_payment();
                        record.InjectFrom(Record);

                        record.id = Guid.NewGuid().ToString("N");
                        record.created_by = CurrentUserContext.AppUserId;
                        record.created_on = System.DateTime.Now;
                        record.modified_by = null;
                        record.modified_on = null;
                        record.is_active = true;
                        record.is_default = null;
                        record.is_locked = null;
                        record.entity_id = null;
                        record.owner_id = CurrentUserContext.AppUserId;
                        record.organization_id = CurrentUserContext.OrganizationId;

                        dbContext.sales_invoice_payment.Add(record);
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
                    logger.Error(ex.InnerException ?? ex);
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            }
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            try
            {
                var record = dbContext.sales_invoice
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {
                    var recordX = dbContext.sales_invoice_charges
                        .Where(o => o.sales_invoice_id == key)
                        ;
                    if (recordX != null)
                    {
                        foreach (sales_invoice_charges recX in recordX)
                        {
                            dbContext.sales_invoice_charges.Remove(recX);
                        }
                    }

                    dbContext.sales_invoice.Remove(record);
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


        [HttpGet("Finalisasi/{Id}")]
        public async Task<IActionResult> Finalisasi(string Id)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.sales_invoice
                        .Where(o => o.id == Id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        record.approve_status = "APPROVED";
                        record.approve_by_id = CurrentUserContext.AppUserId;

                        await dbContext.SaveChangesAsync();
                    }

                    var sales_invoice_ell = dbContext.sales_invoice_ell
                        .Where(o => o.id == Id && o.sync_status == "PENDING")
                        .FirstOrDefault();
                    if (sales_invoice_ell != null)
                    {
                        sales_invoice_ell.sync_status = null;
                        await dbContext.SaveChangesAsync();
                    }

                    await tx.CommitAsync();
                    return Ok(record);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.Error(ex.InnerException ?? ex);
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            }
        }

        [HttpGet("Unapprove/{Id}")]
        public async Task<IActionResult> Unapprove(string Id)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.sales_invoice
                        .Where(o => o.id == Id)
                        .FirstOrDefault();
                    if (record != null)
                    {
                        record.approve_status = "UNAPPROVED";
                        record.disapprove_by_id = CurrentUserContext.AppUserId;

                        await dbContext.SaveChangesAsync();
                    }

                    var sales_invoice_ell = dbContext.sales_invoice_ell
                        .Where(o => o.id == Id && o.sync_status == "PENDING")
                        .FirstOrDefault();
                    if (sales_invoice_ell != null)
                    {
                        sales_invoice_ell.sync_status = null;

                        await dbContext.SaveChangesAsync();
                    }

                    await tx.CommitAsync();
                    return Ok(record);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.Error(ex.InnerException ?? ex);
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            }
        }

        [HttpGet("RequestApproval/{Id}")]
        public async Task<IActionResult> RequestApproval(string Id)
        {
            using (var tx = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var record = dbContext.sales_invoice_approval
                        .Where(o => o.sales_invoice_id == Id)
                        .FirstOrDefault();
                    if (record == null)
                    {
                        var recordSalesInvoiceApproval = new sales_invoice_approval();

                        recordSalesInvoiceApproval.id = Guid.NewGuid().ToString("N");
                        recordSalesInvoiceApproval.created_by = CurrentUserContext.AppUserId;
                        recordSalesInvoiceApproval.created_on = System.DateTime.Now;
                        recordSalesInvoiceApproval.modified_by = null;
                        recordSalesInvoiceApproval.modified_on = null;
                        recordSalesInvoiceApproval.is_active = true;
                        recordSalesInvoiceApproval.is_default = null;
                        recordSalesInvoiceApproval.is_locked = null;
                        recordSalesInvoiceApproval.entity_id = null;
                        recordSalesInvoiceApproval.owner_id = CurrentUserContext.AppUserId;
                        recordSalesInvoiceApproval.organization_id = CurrentUserContext.OrganizationId;

                        recordSalesInvoiceApproval.sales_invoice_id = Id;
                        recordSalesInvoiceApproval.approve_status = "UNAPPROVED";
                        recordSalesInvoiceApproval.approve_by_id = CurrentUserContext.AppUserId;

                        dbContext.sales_invoice_approval.Add(recordSalesInvoiceApproval);
                        await dbContext.SaveChangesAsync();

                        #region Send Email
                        var recEmail = new email_notification();

                        var recSalesInvoice = dbContext.vw_sales_invoice
                            .Where(o => o.id == Id)
                            .FirstOrDefault();
                        if (recSalesInvoice != null)
                        {
                            recEmail.id = Guid.NewGuid().ToString("N");
                            recEmail.created_by = CurrentUserContext.AppUserId;
                            recEmail.created_on = System.DateTime.Now;
                            recEmail.modified_by = null;
                            recEmail.modified_on = null;
                            recEmail.is_active = true;
                            recEmail.is_default = null;
                            recEmail.is_locked = null;
                            recEmail.entity_id = null;
                            recEmail.owner_id = CurrentUserContext.AppUserId;
                            recEmail.organization_id = CurrentUserContext.OrganizationId;

                            recEmail.email_subject = "SALES INVOICE #" + recSalesInvoice.invoice_number;
                            var url = HttpContext.Request.GetEncodedUrl();
                            url = url.Substring(0, url.IndexOf("/api"));

                            string teks = string.Concat("<p><strong style='style=width: 100%; font-size: 14pt; font-family: Tahoma; text-align: center'>",
                                "SALES INVOICE #", recSalesInvoice.invoice_number, "</strong>",
                                "<p>Dear Pak Brilianto,</p>",
                                "<p> </p>",
                                "<p>Mohon bantuannya untuk review dan konfirmasi shadow invoice terlampir.</p>",
                                "<p>Thank you.</p>",
                                "<p> </p>",
                                "<p>Please find the attachment and sales invoice detail in this link below:</p>",
                                "<div> <a href=", url, "/Sales/SalesInvoiceApproval/Index?Id=", Id, "&openEditingForm=false>",
                                "See Sales Invoice Approval</a> </div>",
                                "<p> </p>",
                                "<p>Thank You,</p>",
                                "<p>Best Regards.</p>"
                                );

                            recEmail.email_content = teks;

                            recEmail.delivery_schedule = System.DateTime.Now;
                            recEmail.table_name = "vw_sales_invoice";
                            recEmail.fields = "sales_contract_name, sales_contract_term_name";
                            recEmail.criteria = string.Format("id='{0}'", Id);
                            recEmail.email_code = "SalesInvoice-" + recSalesInvoice.invoice_number;

                            dbContext.email_notification.Add(recEmail);
                            await dbContext.SaveChangesAsync();

                        }
                        #endregion
                        
                        await tx.CommitAsync();

                        return Ok(recordSalesInvoiceApproval);
                    }
                    else
                    {
                        return null;
                    }

                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.Error(ex.InnerException ?? ex);
                    return BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
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

        [HttpGet("DespatchOrderDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DespatchOrderDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_despatch_order.Where(o => o.id == Id &&
                    (CustomFunctions.CanRead(o.id, CurrentUserContext.AppUserId)
                        || CurrentUserContext.IsSysAdmin)),
                loadOptions);
        }

        [HttpGet("ShippingCostDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> ShippingCostDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.vw_shipping_cost.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.despatch_order_id == Id),
                loadOptions);
        }

        [HttpGet("DespatchOrderIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DespatchOrderIdLookup([FromQuery] string SalesOrderId, DataSourceLoadOptions loadOptions)
        {
            try
            {
                if (string.IsNullOrEmpty(SalesOrderId))
                {
                    logger.Debug($"SalesOrderId in DespatchOrderIdLookup is null or empty");
                    var lookup = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                        .Select(o => new { Value = o.id, Text = o.despatch_order_number, o.sales_order_id });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
                else
                {
                    logger.Debug($"SalesOrderId in DespatchOrderIdLookup is {SalesOrderId}");
                    var lookup = dbContext.despatch_order
                        .Where(o => o.organization_id == CurrentUserContext.OrganizationId
                            && o.sales_order_id == SalesOrderId)
                        .Select(o => new { Value = o.id, Text = o.despatch_order_number, o.sales_order_id });
                    return await DataSourceLoader.LoadAsync(lookup, loadOptions);
                }
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

        [HttpGet("SalesInvoiceIdLookup")]
        public async Task<object> SalesInvoiceIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = dbContext.sales_invoice
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId)
                    .Select(o => new { Value = o.id, Text = o.invoice_number });
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

        [HttpGet("InvoiceCurrencyExchangeIdLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> InvoiceCurrencyExchangeIdLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                logger.Debug($"func InvoiceCurrencyExchangeIdLookup()");
                var lookup = dbContext.vw_currency_exchange
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId ) 
                    .OrderBy(o => o.end_date)
                    .Select(o => new { Value = o.id, Text = o.source_currency_code + "-" + o.target_currency_code, o.source_currency_id, o.start_date , o.end_date, Xchange = o.exchange_rate });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("InvoiceCurrencyExchangeIdLookupByDo")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> InvoiceCurrencyExchangeIdLookupByDo(DataSourceLoadOptions loadOptions, string despatch_order_id, string invoice_date)
        {
            try
            {
                var exchDate = dbContext.vw_do_inv_currency_exchange
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.id == despatch_order_id)
                    .Select(o => new { Value = o.exchange_date})
                    .FirstOrDefault();
                
                if (exchDate != null)
                {
                    //var _exchDate = Convert.ToString(exchDate.Value);
                    //if (_exchDate != "{ Value =  }")
                    if (exchDate.Value != null)
                    {
                        invoice_date = exchDate.Value.ToString().Replace(" 00:00:00", "");
                    }
                }

                logger.Debug($"func InvoiceCurrencyExchangeIdLookup()");
                //var lookup = dbContext.vw_currency_exchange
                //    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.start_date == Convert.ToDateTime(invoice_date))// && o.end_date >= Convert.ToDateTime(invoice_date))
                //    .OrderBy(o => o.end_date)
                //    .Select(o => new { Value = o.id, Text = o.source_currency_code + "-" + o.target_currency_code, o.source_currency_id, o.start_date, o.end_date, Xchange = o.exchange_rate });

                var tgl = Convert.ToDateTime(invoice_date);
                if (invoice_date == null) tgl = System.DateTime.Now;
                else tgl = Convert.ToDateTime(invoice_date);

                var daritgl = Convert.ToDateTime(tgl.AddDays(-1));
                var hinggatgl = Convert.ToDateTime(tgl.AddDays(1));

                var lookup = dbContext.vw_currency_exchange
                    .Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.end_date >= daritgl
                        && o.end_date <= hinggatgl)
                    .OrderBy(o => o.end_date)
                    .Select(o => new {
                        Value = o.id,
                        Text = o.source_currency_code + " - " + o.target_currency_code + " - " + o.end_date,
                        o.source_currency_id,
                        o.start_date,
                        o.end_date,
                        Xchange = o.exchange_rate
                    });
                return await DataSourceLoader.LoadAsync(lookup, loadOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("SalesInvoiceDetail/{Id}")]
        public async Task<object> SalesInvoiceDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.sales_invoice_detail.Where(o => o.organization_id == CurrentUserContext.AppUserId
                    && o.sales_invoice_id == Id),
                loadOptions);
        }

        //public double RoundI(double number, double roundingInterval)
        //{
        //    return (double)((decimal)roundingInterval * Math.Round((decimal)number / (decimal)roundingInterval, MidpointRounding.AwayFromZero));
        //}

        double patternAdjustment(string the_text, List<AdjustmentCalculation> listAdjCalc)
        {
            double retVal = 0.0;
            var tText = the_text.Replace(" ", String.Empty).Replace("\n", String.Empty);
            string _value = ".value()";
            string _target = ".target()";
            string _min = ".min()";
            string _max = ".max()";
            string current_invoice_unit_price = stringQuotationPrice;

            var strUnitPrice = "0.0";
            if (listAdjCalc.Count > 1)
            {
                try
                {
                    strUnitPrice = listAdjCalc[1].analyte_value.ToString().Replace(',', '.');
                } 
                catch (Exception e)
                {
                    logger.Debug($"got exception inside patternAdjustment = {e.Message}");
                }
            }
            
            tText = tText.Replace(listAdjCalc[0].analyte_symbol + _value, listAdjCalc[0].analyte_value.ToString().Replace(',', '.'));
            tText = tText.Replace(listAdjCalc[0].analyte_symbol + _target, listAdjCalc[0].analyte_target.ToString().Replace(',', '.'));

            //tText = tText.Replace(listAdjCalc[0].analyte_symbol + _min, listAdjCalc[0].analyte_value.ToString().Replace(',', '.'));
            tText = tText.Replace(listAdjCalc[0].analyte_symbol + _min, listAdjCalc[0].analyte_minimum.ToString().Replace(',', '.'));

            //tText = tText.Replace(listAdjCalc[0].analyte_symbol + _max, listAdjCalc[0].analyte_target.ToString().Replace(',', '.'));
            tText = tText.Replace(listAdjCalc[0].analyte_symbol + _max, listAdjCalc[0].analyte_maximum.ToString().Replace(',', '.'));

            tText = tText.Replace(current_invoice_unit_price, strUnitPrice.Replace(',', '.'));
            CultureInfo culture_curr = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo ("en-ID",false);
            var engine1 = new Engine();
            var expression1 = engine1.Parse(tText);
            var resultText = expression1.Execute().ToString();
            //bobby 20220419 menghitung formula
            retVal = Convert.ToDouble(resultText, CultureInfo.CurrentCulture);
            
            logger.Debug($"resultText is {resultText} and retVal is {retVal}");
            CultureInfo.CurrentCulture = culture_curr;
            return retVal;
        }

        bool patternPrerequisitePremium(string the_text, string p_vessel_id)
        {
            bool isOK = false;
            bool lanjut = false;
            //string sqlPattern = @"([\+\*-/])([0-9]*)([.,]?)([0-9]*)";
            string defaultPattern = @"([_\-a-zA-z]+)[.]([_\-a-zA-z]+)\s*=\s*(.*)";
            //string numeric1Pattern = @"([\+\*-/])([0-9]*)([.,]?)([0-9]*)";

            //double returnValue = quotation_price;

            string replacement1 = "$1";
            string replacement2 = "$2";
            string replacement3 = "$3";
            string inputed_hex1, inputed_hex2, inputed_hex3;
            //double sales_contract_charge_premium_value = 0.0;
            if (the_text.Equals("true"))
            {
                lanjut = true;
                isOK = true;
            } 
            else if (the_text.Equals("false"))
            {
                lanjut = false;
            }
            else
            {
                if (Regex.IsMatch(the_text, defaultPattern))
                {
                    lanjut = true;
                }
            }

            if (lanjut)
            {
                inputed_hex1 = Regex.Replace(the_text, defaultPattern, replacement1);
                inputed_hex2 = Regex.Replace(the_text, defaultPattern, replacement2);
                inputed_hex3 = Regex.Replace(the_text, defaultPattern, replacement3);

                if (inputed_hex1 == "vessel" && inputed_hex2 == "is_geared")
                {
                    var vessel_lookup = dbContext.vessel.Where(o => o.id == p_vessel_id)
                        .FirstOrDefault();

                    if (inputed_hex3=="false")
                    {
                        isOK = !(bool)vessel_lookup.is_geared;
                    }
                    else if (inputed_hex3 == "true")
                    {
                        isOK = (bool)vessel_lookup.is_geared;
                    }
                    else
                    {

                    }

                }

            }
            else
            {
                Console.WriteLine("not match:");
            }
            return isOK;
        }

        private double patternFormulaPremium(string the_text, double quotation_price)
        {
            double returnValue = 0.0;
            returnValue = quotation_price;
            string pattern = @"([\+\*-/])([0-9]*)([.,]?)([0-9]*)";
            string replacement1 = "$1";
            string replacement2 = "$2$3$4";
            string replacement_separator = "$3";
            string inputed_hex1, inputed_hex2, separator;
            double sales_contract_charge_premium_value = 0.0;

            if (Regex.IsMatch(the_text, pattern))
            {
                inputed_hex1 = Regex.Replace(the_text, pattern, replacement1);
                inputed_hex2 = Regex.Replace(the_text, pattern, replacement2);
                separator = Regex.Replace(the_text, pattern, replacement_separator);
                Console.WriteLine("value1:" + inputed_hex1);
                Console.WriteLine("value2:" + inputed_hex2);

                NumberFormatInfo nfi = new NumberFormatInfo();
                if (separator.Length > 0)
                {
                    nfi.NumberDecimalSeparator = separator;
                    nfi.CurrencyDecimalSeparator = separator;
                }
                sales_contract_charge_premium_value = double.Parse(inputed_hex2, nfi);

                switch (inputed_hex1)
                {
                    case "+": returnValue += sales_contract_charge_premium_value; break;
                    case "-": returnValue -= sales_contract_charge_premium_value; break;
                    case "*": returnValue *= sales_contract_charge_premium_value; break;
                    case "/": returnValue /= sales_contract_charge_premium_value; break;

                    default: break;
                }
            }
            else
            {
                Console.WriteLine("not match:");
            }
            return returnValue;
        }

        [HttpGet("LookupQuotationTypeOnDespatchOrder")]
        public async Task<object> LookupQuotationTypeOnDespatchOrder(string despatch_order_id, DataSourceLoadOptions loadOptions)
        {
            if (despatch_order_id is null)
            {
                return BadRequest("null input parameter");
                //return 9.9999;
            }

            var quotationTypeArray = dbContext.vw_lookup_despatch_order_for_quotation
                .Where(o => o.despatch_order_id == despatch_order_id)
                .Select(o => new { Value = o.quotation_master_id, Text = o.quotation_name}); ;

            return await DataSourceLoader.LoadAsync(quotationTypeArray, loadOptions);

        }

        [HttpGet("LookupInvoiceTypeOnDespatchOrder")]
        public async Task<object> LookupInvoiceTypeOnDespatchOrder(string despatch_order_id, DataSourceLoadOptions loadOptions)
        {
            if (despatch_order_id is null)
            {
                return BadRequest("null input parameter");
                //return 9.9999;
            }

            var invoiceTypeArray = dbContext.vw_lookup_despatch_order_for_invoice
                .Where(o => o.despatch_order_id == despatch_order_id)
                .Select(o => new { Value = o.invoice_master_id, Text = o.invoice_name});

            return await DataSourceLoader.LoadAsync(invoiceTypeArray, loadOptions);

        }

        [HttpGet("LookupBothOnDespatchOrder")]
        public async Task<object> LookupBothOnDespatchOrder(string despatch_order_id, DataSourceLoadOptions loadOptions)
        {
            if (despatch_order_id is null )
            {
                return BadRequest("null input parameter");
                //return 9.9999;
            }

            var invoiceTypeList = dbContext.vw_lookup_despatch_order_for_invoice.Where(o => o.despatch_order_id == despatch_order_id)
                .Select(o => new { Value = o.invoice_master_id, Text = o.invoice_name });
            var invoiceTypeArray =  await invoiceTypeList.ToArrayAsync();
            var quotationTypeList = dbContext.vw_lookup_despatch_order_for_quotation.Where(o => o.despatch_order_id == despatch_order_id)
                .Select(o => new { Value = o.quotation_master_id, Text = o.quotation_name});
            var quotationTypeArray = await quotationTypeList.ToArrayAsync();

            var retVal2 = new
            {
                quotationType = quotationTypeArray,
                invoiceType = invoiceTypeArray
            };

            return retVal2;
            
        }
         
        [HttpGet("CountPrice")]
        public async Task<object> CountPrice(string despatch_order_id, string sales_contract_id, string sales_contract_term_id, string invoice_type_id, string quotation_type_id, DataSourceLoadOptions loadOptions)
        {
            if (despatch_order_id is null && invoice_type_id is null && sales_contract_term_id is null)
            {
                return BadRequest("required input parameter is null");
                //return 9.9999;
            }

            logger.Debug($"string key = {despatch_order_id} dan  {invoice_type_id}");

            var splitInvoiceTypeId = invoice_type_id.Split('-');
            var sales_contract_payment_term_id = splitInvoiceTypeId[0];
            var real_invoice_type_id = splitInvoiceTypeId[1];

            // get how many payment term is listed in contract 
            var vw_payment_term_data = dbContext.vw_sales_contract_payment_term
                .Where(o => o.sales_contract_term_id == sales_contract_term_id)
                ;
            var array_vw_payment_term_data = await vw_payment_term_data.ToArrayAsync();
            if (array_vw_payment_term_data.Length == 0)
            {
                return BadRequest("no payment term is listed in " + array_vw_payment_term_data[0].contract_term_name);
            }

            // get how many invoice already generated by despatch_order_id
            var vw_sales_invoice_data = dbContext.vw_sales_invoice
                .Where(o => o.despatch_order_id == despatch_order_id)
                ;
            var array_vw_sales_invoice_data = await vw_sales_invoice_data.ToArrayAsync();

            var numberInvoicesCanBeIssued = array_vw_payment_term_data.Length - array_vw_sales_invoice_data.Length;
            if (numberInvoicesCanBeIssued == 0)
            {
                return BadRequest("All invoice had already issued from despatch order " + array_vw_sales_invoice_data[0].despatch_order_number );
            }

            // Get Invoice Type Data
            var invoice_type_data = await dbContext.master_list
            .Where(o => o.id == real_invoice_type_id).FirstOrDefaultAsync()
            ;

            //var quotation_type_data = await dbContext.master_list
            //    .Where(o => o.id == quotation_type_id).FirstOrDefaultAsync()
            //    ;

            List<SalesPrice> arrayRetVal = new List<SalesPrice>();
           
            var bill_of_lading = new System.DateTime(2020, 3, 30);
            var laycan_date = new System.DateTime(2020, 3, 30);
            var invoice_date = new System.DateTime(2020, 3, 30);
            var t_quotation_price = 0.0;

            var record_despatch_order = await dbContext.despatch_order
                .Where(o => o.id == despatch_order_id)
                .FirstOrDefaultAsync();
            if (record_despatch_order == null)
            {
                return BadRequest("Despatch Order Laycan Date is not available.");
            }
            // get how many loading term is listed in contract 
            //var invoice_type = dbContext.master_list
            //.Where(o => o.id == real_invoice_type_id).FirstOrDefault();
            if (invoice_type_data.item_in_coding.Substring(0,2) != "dp" )
            { 
                var delivery_term = dbContext.master_list
                .Where(o => o.id == record_despatch_order.delivery_term_id).FirstOrDefault();
                if (delivery_term.item_in_coding== "FOBBG")
                {
                    if (record_despatch_order.multiple_barge == true)
                    {
                        var record_shipping_transaction = dbContext.vw_shipping_transaction
                           .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == true).FirstOrDefault();
                        if (record_shipping_transaction == null)
                        {
                            return BadRequest("There is no Shipping Loading (FOB Barge (Multiple)) Data in this Despatch Order");
                            
                        }
                    }
                    else
                    {
                        var record_barging_transaction = dbContext.vw_barging_transaction
                       .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == true).FirstOrDefault();
                        if (record_barging_transaction == null)
                        {
                            return BadRequest("There is no Barging Loading (FOB Barge) Data in this Despatch Order");
                        }
                    }
                }
                else if (delivery_term.item_in_coding == "CIFBG")
                {
                    var record_barging_transaction = dbContext.vw_barging_transaction
                   .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == false).FirstOrDefault();
                    if (record_barging_transaction == null)
                    {
                        return BadRequest("There is no Barging Unloading (CIF Barge) Data in this Despatch Order");
                    }
                }
                else if (delivery_term.item_in_coding == "FOBMV")
                {
                    var record_shipping_transaction = dbContext.vw_shipping_transaction
                   .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == true).FirstOrDefault();
                    if (record_shipping_transaction == null)
                    {
                        return BadRequest("There is no Shipping Loading (FOB Vessel) Data in this Despatch Order");
                    }
                }
                
                else if (delivery_term.item_in_coding == "CIFMV")
                {
                    var record_shipping_transaction = dbContext.vw_shipping_transaction
                   .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == false).FirstOrDefault();
                    if (record_shipping_transaction == null)
                    {
                        return BadRequest("There is no Shipping Unloading (CIF Vessel) Data in this Despatch Order");
                    }
                }
                //else 
                //{
                //    var record_shipping_transaction = dbContext.vw_shipping_transaction
                //   .Where(o => o.despatch_order_id == despatch_order_id && o.is_loading == true).FirstOrDefault();
                //    if (record_shipping_transaction == null)
                //    {
                //        return BadRequest("There is no Shipping Data (FAS) in this Despatch Order");
                //    }
                //}
            }
            // get how many invoice already generated by despatch_order_id

            laycan_date = (System.DateTime)(record_despatch_order.laycan_start ?? Convert.ToDateTime("1900-01-01"));
            //Bobby 20220121 change quantity source
            decimal doQuantity = 0;
            //Bobby 20220405 change quantity source option
            if (invoice_type_data.item_name.ToLower().Contains("dp") || invoice_type_data.item_name.ToLower().Contains("proforma"))
            {
                doQuantity = record_despatch_order.required_quantity ?? 0;
                //Bobby 20220405 change bill_of_lading source
                bill_of_lading = (System.DateTime)(record_despatch_order.order_reference_date ?? Convert.ToDateTime("1900-01-01"));
            }
            else
            {
                var cow = await dbContext.draft_survey.Where(o => o.organization_id == CurrentUserContext.OrganizationId
                    && o.despatch_order_id == despatch_order_id).FirstOrDefaultAsync();
                if (cow != null)
                {
                    doQuantity = cow.quantity ?? 0;
                    //Bobby 20220218 change bill_of_lading source
                    bill_of_lading = (System.DateTime)cow.bill_lading_date;
                }
            }
            //var doQuantity = record_despatch_order.quantity;
            string the_contract_term_id = record_despatch_order.contract_term_id;
            string the_contract_product_id = record_despatch_order.contract_product_id;
            var v_vessel_id = record_despatch_order.vessel_id;


            // Find Quotation Type using Invoice Type Name
            //var masterlist_id_for_quotation_type = await dbContext.master_list
            //    .Where(o => o.item_name == quotation_type_data.item_name && o.item_group == "quotation-type").FirstOrDefaultAsync()
            //    ;

            var records_sales_contract_quotation_price = dbContext.vw_sales_contract_quotation_price
                .Where(o => o.sales_contract_term_id == the_contract_term_id
                && o.quotation_type_id == quotation_type_id)
                ;
            var arraySalesContractQuotationPrice = await records_sales_contract_quotation_price.ToArrayAsync();

            try
            {
                // menghitung data quotation price
                t_quotation_price = 0.0;
                var total_weightening_value = 0;
                foreach (vw_sales_contract_quotation_price item1 in arraySalesContractQuotationPrice)
                {
                    total_weightening_value += (int)item1.weightening_value;
                    
                    if (item1.pricing_method_in_coding == stringPricingMethod_Calculated)
                    {

                        if (item1.price_index_id is null)
                        {
                            t_quotation_price = 0.0;
                        }
                        else
                        {
                            // check quotation_period,  if its 4LIBL , 4LI1LD , QBQBL , QBQLD
                            var avg_price_index = 0.0;
                            var total_price_index = 0.0;
                            var count_price_include = 0;
                            int[] arrayMonth = new int[1];
                            List<int> listMonth = new List<int>();
                            var theDate = new System.DateTime(2020, 3, 30);
                            var errMsg = "";
                            switch (item1.quotation_period)
                            {
                                case stringQuotationPeriod_4LIBL:
                                case stringQuotationPeriod_4LI1LD:

                                    if (item1.quotation_period == stringQuotationPeriod_4LIBL)
                                    {
                                        theDate = bill_of_lading;
                                    } else {
                                        theDate = laycan_date;
                                    }
                                    var records_price_index_history = dbContext.vw_price_index_history
                                        .Where(o => o.price_index_id == item1.price_index_id 
                                            && o.index_date.Value.AddDays(28) >= theDate
                                            && o.index_date.Value.AddDays(28) <= theDate.AddDays(28))
                                        .OrderByDescending(o => o.index_date);
                                    if (records_price_index_history.Count() < 4 || records_price_index_history.Count() > 5)
                                    {
                                        errMsg = "Price is not available. Number of index price from "+ records_price_index_history.FirstOrDefault().price_index_name + " in 28-days-range from " + theDate.ToString() + " is not 4";
                                        avg_price_index = 0;
                                        t_quotation_price = 0;
                                        break;
                                    }
                                    var array_price_index_history = await records_price_index_history.ToArrayAsync();
                                    foreach (vw_price_index_history item2 in array_price_index_history)
                                    {
                                        if ((theDate <= item2.index_date.Value.AddDays(28)) && (count_price_include<4))
                                        {
                                            total_price_index += (float)item2.index_value;
                                            count_price_include++;
                                        }
                                    }
                                    if (count_price_include != 4)
                                    {
                                        errMsg = "Price is not available. Number of index price in 28-days-range from " + theDate.ToString() + " is not 4";
                                        avg_price_index = 0;
                                        t_quotation_price = 0;
                                        break;
                                    }
                                    avg_price_index = total_price_index / count_price_include;
                                    t_quotation_price += avg_price_index * (double)item1.weightening_value / 100;
                                    break;

                                case stringQuotationPeriod_QBQBL:
                                case stringQuotationPeriod_QBQLD:
                                    if (item1.quotation_period == stringQuotationPeriod_QBQBL)
                                    {
                                        theDate = bill_of_lading;
                                    } else {
                                        theDate = laycan_date;
                                    }
                                    var records_price_index_history_quarter = dbContext.price_index_history
                                        .Where(o => o.price_index_id == item1.price_index_id ).OrderByDescending(o => o.index_date)
                                        ;
                                    var array_price_index_history_quarter = await records_price_index_history_quarter.ToArrayAsync();
                                    switch (theDate.Month)
                                    {
                                        case 1:
                                        case 2:
                                        case 3:
                                            listMonth.Add(10);
                                            listMonth.Add(11);
                                            listMonth.Add(12);
                                            break;
                                        case 4:
                                        case 5:
                                        case 6:
                                            listMonth.Add(1);
                                            listMonth.Add(2);
                                            listMonth.Add(3);
                                            break;
                                        case 7:
                                        case 8:
                                        case 9:
                                            listMonth.Add(4);
                                            listMonth.Add(5);
                                            listMonth.Add(6);
                                            break;
                                        case 10:
                                        case 11:
                                        case 12:
                                            listMonth.Add(7);
                                            listMonth.Add(8);
                                            listMonth.Add(9);
                                            break;
                                        default:
                                            listMonth.Add(0);
                                            break;
                                    }
                                    arrayMonth = listMonth.ToArray();
                                    foreach (price_index_history item2 in array_price_index_history_quarter)
                                    {
                                        if  ((item2.index_date.Year == theDate.Year) && (arrayMonth.Contains(item2.index_date.Month)))
                                        {
                                            total_price_index += (float)item2.index_value;
                                            count_price_include++;
                                        }
                                    }
                                    avg_price_index = total_price_index / count_price_include;
                                    t_quotation_price += avg_price_index * (double)item1.weightening_value / 100;
                                    break;

                                //Bobby Price HPB 20220520

                                case stringQuotationPeriod_MILD:
                                case stringQuotationPeriod_MIBLD:

                                    if (item1.quotation_period == stringQuotationPeriod_MIBLD)
                                    {
                                        theDate = bill_of_lading;
                                    }
                                    else
                                    {
                                        theDate = laycan_date;
                                    }
                                    var records_monthly_index_history = dbContext.vw_price_index_history
                                        .Where(o => o.price_index_id == item1.price_index_id
                                            && o.index_date.Value.Month == theDate.Month)
                                        .OrderByDescending(o => o.index_date);
                                    if (records_monthly_index_history.Count() > 1 )
                                    {
                                        errMsg = "There is multiple index";
                                        avg_price_index = 0;
                                        t_quotation_price = 0;
                                        break;
                                    }
                                    var array_monthly_index_history = await records_monthly_index_history.ToArrayAsync();
                                    foreach (vw_price_index_history item2 in array_monthly_index_history)
                                    {
                                       // if ((theDate <= item2.index_date.Value.AddDays(28)) && (count_price_include < 4))
                                       // {
                                            total_price_index += (float)item2.index_value;
                                            count_price_include++;
                                       // }
                                    }
                                    if (count_price_include != 1)
                                    {
                                        errMsg = "Monthly Index is not available";
                                        avg_price_index = 0;
                                        t_quotation_price = 0;
                                        break;
                                    }
                                    avg_price_index = total_price_index / count_price_include;
                                    t_quotation_price += avg_price_index * (double)item1.weightening_value / 100;
                                    break;
                                //Bobby Price HPB 20220520
                                default: break;
                            } // end of switch:
                            if (errMsg.Length > 0)
                            {
                                arrayRetVal.Add(new SalesPrice("error", errMsg, -1));
                                //return arrayRetVal;
                                return BadRequest(errMsg);
                            }
                            t_quotation_price = Math.Round(t_quotation_price, (int)item1.decimal_places);
                        } // end of else:  price_index_id is not null
                    
                    }
                    else if (item1.pricing_method_in_coding == stringPricingMethod_Fixed)
                    {
                        t_quotation_price += (double)(double)item1.price_value * (double)item1.weightening_value / 100;
                    }
                    else
                    {
                        // none 
                    }

                }
            }
            catch (Exception e)
            {
                logger.Debug($"got exception when count quotationprice = {e.Message}");
                return BadRequest("Error when counting quotationprice = " + e.Message);
            }

            try
            {
                // menghitung sales charge jika Premium atau adjustment 
                var records_vw_sales_contract_charge = dbContext.vw_sales_contract_charges
                    .Where(o => o.sales_contract_term_id == the_contract_term_id)
                    .OrderBy(o => o.order);

                var array_vw_contract_charges = await records_vw_sales_contract_charge.ToArrayAsync();
                foreach (vw_sales_contract_charges item_contract_charge in array_vw_contract_charges)
                {
                    //bobby arvian 20220407 penambahan feature discount
                    if (item_contract_charge.sales_charge_type_name.ToLower() == stringsalesChargeType_discount)
                    {
                        string the_text = item_contract_charge.charge_formula;
                        //string the_prerequisite = item_contract_charge.prerequisite;
                        //bool isPrerequisite = patternPrerequisitePremium(the_prerequisite, v_vessel_id);
                        //if (isPrerequisite)
                        //{
                            t_quotation_price = patternFormulaPremium(the_text, t_quotation_price);
                        //}
                    }
                    if (  item_contract_charge.sales_charge_type_name.ToLower() == stringsalesChargeType_premium)
                    {
                        string the_text = item_contract_charge.charge_formula;
                        string the_prerequisite = item_contract_charge.prerequisite;
                        bool isPrerequisite = patternPrerequisitePremium(the_prerequisite, v_vessel_id);
                        if (isPrerequisite)
                        {
                            t_quotation_price = patternFormulaPremium(the_text, t_quotation_price);
                        }
                    }

                    if (item_contract_charge.sales_charge_type_name.ToLower() == stringsalesChargeType_adjustment)
                    {
                        //var sementara = 0.0;

                        var tText = item_contract_charge.charge_formula.ToLower().Replace(" ", String.Empty);
                        var tName = item_contract_charge.sales_charge_name;
                        var tCode = item_contract_charge.sales_charge_code;
                        List<AdjustmentCalculation> adjCalc = new List<AdjustmentCalculation>();

                        AdjustmentCalculation ac1;
                        //var valuenya = "";

                        var records_sales_contract_product_specification = dbContext.vw_quality_sampling_analyte
                            .Where(o => o.despatch_order_id == despatch_order_id);
                        var array_sales_contract_product_specification = await records_sales_contract_product_specification.ToArrayAsync();

                        //var records_sales_contract_product_specification = dbContext.vw_sales_contract_product_specifications
                        //    .Where(o => o.sales_contract_product_id == the_contract_product_id);
                        //var array_sales_contract_product_specification = await records_sales_contract_product_specification.ToArrayAsync();
                        //foreach (vw_sales_contract_product_specifications item1 in array_sales_contract_product_specification)

                        foreach (vw_quality_sampling_analyte item1 in array_sales_contract_product_specification)
                        {

                            var analytesym = item1.analyte_symbol.Replace(" ", String.Empty).ToLower();
                            if (tText.Contains(analytesym))
                            {
                                //Bobby 20220121 change quality source
                                //despatch_order_id
                                ac1 = new AdjustmentCalculation();
                                ac1.analyte_maximum = (float)(item1.maximum ?? 0);
                                ac1.analyte_minimum = (float)(item1.minimum ?? 0);
                                ac1.analyte_target = (float)(item1.target ?? 0);
                                //ac1.analyte_value = (float)item1.value;
                                //20220425 bobby take out float
                                ac1.analyte_value = (double)item1.analyte_value;
                                //ac1.analyte_value = (float)item1.analyte_value;
                                 ac1.analyte_name = item1.analyte_name;
                                ac1.analyte_symbol = analytesym;
                                adjCalc.Add(ac1);

                                ac1 = new AdjustmentCalculation();
                                //20220425 bobby take out float
                                ac1.analyte_value = t_quotation_price;
                                ac1.analyte_name = stringQuotationPrice;
                                ac1.analyte_symbol = stringQuotationPrice;
                                adjCalc.Add(ac1);

                                double hasilParsing = patternAdjustment(tText, adjCalc);
                                if (item_contract_charge.decimal_places == null)
                                {
                                    item_contract_charge.decimal_places = 2;
                                }
                                //Bobby 20220425 Rounding
                                if (item_contract_charge.rounding_type_name == "Round Up")
                                {
                                    hasilParsing = Math.Round(hasilParsing, (int)item_contract_charge.decimal_places, MidpointRounding.ToEven);
                                }
                                else if (item_contract_charge.rounding_type_name == "Floor")
                                {
                                    hasilParsing = Math.Floor(hasilParsing);
                                }
                                else if (item_contract_charge.rounding_type_name == "Truncate")
                                {
                                    hasilParsing = Math.Truncate(hasilParsing);
                                }
                                else
                                {
                                    hasilParsing = Math.Round(hasilParsing, (int)item_contract_charge.decimal_places);
                                }
                                arrayRetVal.Add(new SalesPrice(tName, tCode, hasilParsing));
                            }
                        }


                    }

                }

            }
            catch (Exception e)
            {
                logger.Debug($"got exception when parsing prerequisite = {e.Message}");
                return BadRequest("Error when parsing prerequisite = " + e.Message);
            }

            var shippingCost = dbContext.vw_shipping_cost
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                    o.despatch_order_id == despatch_order_id).FirstOrDefault();

           

            arrayRetVal.Add(new SalesPrice("Quotation Price", "quotation_price", t_quotation_price ));
            double totalInvoice = 0.0;
            try {
                // menghitung down payment
                double dpValue = 0.0;
                totalInvoice = getTotalInvoice(arrayRetVal, (double)doQuantity);
                //bobby 20020422 Freight Cost
                arrayRetVal.Add(new SalesPrice("subtotal", "subtotal", totalInvoice));

                if(shippingCost!=null)
                {
                    totalInvoice += ((double)shippingCost.freight_rate * (double)shippingCost.quantity);//(double)doQuantity);
                    totalInvoice += (double)shippingCost.insurance_cost;
                }

                //bobby 20220425 rounding total
                var lookupSalesContractTerm = await dbContext.vw_sales_contract_term.Where(o => o.id == the_contract_term_id).FirstOrDefaultAsync();
                if (lookupSalesContractTerm != null)
                {
                    if (lookupSalesContractTerm.decimal_places == null)
                    {
                        lookupSalesContractTerm.decimal_places = 2;
                    }
                    if (lookupSalesContractTerm.rounding_type_name == "Round Up")
                    {
                        totalInvoice = Math.Round(totalInvoice, (int)lookupSalesContractTerm.decimal_places, MidpointRounding.ToEven);
                    }
                    else if (lookupSalesContractTerm.rounding_type_name == "Floor")
                    {
                        totalInvoice = Math.Floor(totalInvoice);
                    }
                    else if (lookupSalesContractTerm.rounding_type_name == "Truncate")
                    {
                        totalInvoice = Math.Truncate(totalInvoice);
                    }
                    else
                    {
                        totalInvoice = Math.Round(totalInvoice, (int)lookupSalesContractTerm.decimal_places);
                    }
                }
                //bobby 20220425 rounding total

                arrayRetVal.Add(new SalesPrice("Total Price", "total_price", totalInvoice));
                if (numberInvoicesCanBeIssued == 1)
                {
                    // last invoice 

                    double totalDPfromAllInvoice = 0.0;
                    foreach (vw_sales_invoice item1 in array_vw_sales_invoice_data)
                    {
                        totalDPfromAllInvoice += (double)item1.downpayment;
                    }

                    dpValue = totalInvoice - totalDPfromAllInvoice ;

                } else
                {
                    // 
                    var outLoop = false;

                    foreach (vw_sales_contract_payment_term item1 in array_vw_payment_term_data)
                    {
                        if (item1.id == sales_contract_payment_term_id)
                        {
                            switch (item1.invoice_in_coding)
                            {
                                case "general":
                                    dpValue = 0.0;
                                    break;
                                case "dp_fixed_amount":
                                    if (item1.downpayment_value != null)
                                    {
                                        dpValue = (double) item1.downpayment_value;
                                    }
                                    outLoop = true;
                                    break;
                                case "dp_percentage":
                                    if (item1.downpayment_value != null)
                                    {
                                        // total = (based price + adjustment ) * quantity  
                                        // dpValue = total * percentage 
                                        dpValue = totalInvoice * (double)(item1.downpayment_value/100);
                                    }
                                    outLoop = true;
                                    break;
                                default: break;
                            }
                        }
                        if (outLoop)
                        {
                            break;
                        }
                    }
                    
                }
                arrayRetVal.Add(new SalesPrice("Down Payment", "down_payment", dpValue));

            }
            catch (Exception e)
            {
                logger.Debug($"got exception when processing Down Payment = {e.Message}");
                return BadRequest("Error when processing Down Payment = " + e.Message);
            }

            // get credit limit data
            var vw_sales_contract_data = dbContext.vw_sales_contract
                .Where(o => o.id == sales_contract_id);

            var array_vw_sales_contract_data = await vw_sales_contract_data.ToArrayAsync();
            if (array_vw_sales_contract_data.Length == 0)
            {
                return BadRequest("No defined customer in " + array_vw_sales_contract_data[0].sales_contract_name);
            }

            // find all sales contract within the same customer
            globalCustomerId = array_vw_sales_contract_data[0].customer_id;
            var vw_sales_invoice_custid = dbContext.vw_sales_invoice
                .Where(o => o.customer_id == globalCustomerId);

            var array_vw_sales_invoice_custid = await vw_sales_invoice_custid.ToArrayAsync();
            double totalPaymentfromAllInvoice = 0.0;
            double totalPricefromAllInvoice = 0.0;
            foreach (vw_sales_invoice item1 in array_vw_sales_invoice_custid)
            {
                totalPricefromAllInvoice += (double)(item1.total_price ?? 0);

                var vw_sales_invoice_payment_data = dbContext.vw_sales_invoice_payment.Where(o => o.sales_invoice_number == item1.invoice_number);
                var array_vw_sales_invoice_payment_data = await vw_sales_invoice_payment_data.ToArrayAsync();
                foreach (vw_sales_invoice_payment itemX in array_vw_sales_invoice_payment_data)
                {
                    totalPaymentfromAllInvoice += (double)(itemX.payment_value ?? 0);
                }
            }

            double remainedCreditLimit = 0.0;
            double customerCreditLimit = 0.0;

            List<CreditLimitData> varCreditLimitData = (List<CreditLimitData>)CountCreditLimit(array_vw_sales_contract_data[0].customer_id).Result;
            remainedCreditLimit = varCreditLimitData[0].RemainedCreditLimit;
            customerCreditLimit = varCreditLimitData[0].InitialCreditLimit;

            remainedCreditLimit = customerCreditLimit - totalInvoice - totalPricefromAllInvoice + totalPaymentfromAllInvoice;
            arrayRetVal.Add(new SalesPrice("Credit Limit", "credit_limit", remainedCreditLimit));
            globalRemainedCreditLimit = remainedCreditLimit;

            return arrayRetVal;
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
                    customerCreditLimit += (double) v;
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
                            totalPricefromAllInvoice += (double) item1.total_price;
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

        double getTotalInvoice (List<SalesPrice> arrayRetVal, double doQuantity)
        {
            double totalPrice = 0.0;
            foreach (SalesPrice eachSP in arrayRetVal)
            {
                totalPrice += eachSP.price;
            }
            double totalInvoice = totalPrice * doQuantity;
            return totalInvoice;
        }

        [HttpGet("SalesInvoiceOutline")]
        public async Task<object> SalesInvoiceOutline(string sales_invoice_id, DataSourceLoadOptions loadOptions)
        {
            //var notestext = "Note :\nPlease pay to (In Full Amount):  \n\nPT. Indexim Coalindo (USD)\t\nPT. Bank Ganesha TBK\t   BANK CORRESPONDENT:\nA/C No. 0910.2.00098.4\t   Bank Negara Indonesia, New York Agency\nWisma Hayam Wuruk Building\nJl. Hayam Wuruk No. 8\t   Swift Code: BNINUS33  \nJakarta 10120 Indonesia\t\t\t\nSwift Code : GNESIDJA\ninstruction to applicant’s bank as per our Central Bank Regulation\nPlease, insert below on the payment message:\n*Transaction Code : 1011\n*Invoice No.\t: XXX/IC-INV/XI/2021\n*Amount\t    \t: USD. ……………\n";
            //var notestext = @"Please pay to (In Full Amount): 
            //                PT. Indexim Coalindo (USD)  
            //                PT. Bank Ganesha TBK A/C No. 0910.2.00098.4   
            //                Swift Code : GNESIDJA, Wisma Hayam Wuruk Building, Jl. Hayam Wuruk No. 8 Jakarta 10120 Indonesia 
            //                BANK CORRESPONDENT: Bank Negara Indonesia, New York Agency Swift Code: BNINUS33
                                                      
            //                instruction to applicant’s bank as per our Central Bank Regulation
            //                Please, insert below on the payment message:
            //                *Transaction Code : 1011
            //                *Invoice No.    : XXX/IC-INV/XI/2021
            //                *Amount     : USD. ……………";
            var lookupSalesInvoice = await dbContext.vw_sales_invoice.Where(o => o.id == sales_invoice_id).FirstOrDefaultAsync();
            var lookupSalesInvoiceDP = await dbContext.vw_sales_invoice.Where(o => o.id != sales_invoice_id && o.despatch_order_id == lookupSalesInvoice.despatch_order_id && o.invoice_date < lookupSalesInvoice.invoice_date).FirstOrDefaultAsync();
            var lookupDespatchOrder = await dbContext.vw_despatch_order.Where(o => o.id == lookupSalesInvoice.despatch_order_id).FirstOrDefaultAsync();
            

            var notestext = string.Format(@"Please pay to (In Full Amount): 
                            {0}  
                            {1} A/C No. {2}   
                            Swift Code : {3}, {4}
                            BANK CORRESPONDENT: {5}, {6} Swift Code: {7}
                                                      
                            instruction to applicant’s bank as per our Central Bank Regulation
                            Please, insert below on the payment message:
                            *Transaction Code : {8}
                            *Invoice No.    : {9}
                            *Amount     : {10}. ……………", lookupSalesInvoice.account_holder, lookupSalesInvoice.bank_name, lookupSalesInvoice.account_number,
                            lookupSalesInvoice.swift_code, lookupSalesInvoice.branch_information, lookupSalesInvoice.correspondent_bank_name,
                            lookupSalesInvoice.correspondent_branch_information, lookupSalesInvoice.correspondent_swift_code, 
                            lookupSalesInvoice.transaction_code, lookupSalesInvoice.invoice_number, lookupSalesInvoice.currency_code );

            double exchange_rate = 1;
            var varFreightCost = lookupSalesInvoice.freight_cost;
            var varInvoiceCurrencySymbol = lookupSalesInvoice.currency_symbol;
            var varInvoiceCurrencyCode = lookupSalesInvoice.currency_code;
            var varInvoiceCurrencyName = lookupSalesInvoice.currency_name;
            if (lookupSalesInvoice.currency_exchange_id != null)
            {
                exchange_rate = (double)lookupSalesInvoice.exchange_rate;
                varInvoiceCurrencySymbol = lookupSalesInvoice.target_currency_symbol;
                varInvoiceCurrencyCode = lookupSalesInvoice.target_currency_code;
                varInvoiceCurrencyName = lookupSalesInvoice.target_currency_name;
            }
            var hargaDasar = new InvoiceOutline()
            {
                invoice_type = "export",
                invoice_item = "Harga Dasar",
                invoice_item_type = "Initial Price",
                quantity = 80000,
                adjustment_quantity = 80000,
                price = 55,
                adjustment_price = 56
            };

            hargaDasar.invoice_type = lookupSalesInvoice.sales_type_name ?? "";
            hargaDasar.quantity = (double)lookupSalesInvoice.quantity;
            hargaDasar.adjustment_quantity = (double)lookupSalesInvoice.quantity;
            hargaDasar.price = (double)lookupSalesInvoice.unit_price * exchange_rate;
            hargaDasar.adjustment_price = (double)lookupSalesInvoice.unit_price * exchange_rate;
            hargaDasar.value = hargaDasar.adjustment_price * hargaDasar.adjustment_quantity;
            hargaDasar.total_invoice = hargaDasar.value;

            List<InvoiceOutline> listTaxes = new List<InvoiceOutline>() { };
            List<InvoiceOutline> retVal = new List<InvoiceOutline>()
            {
                hargaDasar
            };


            double xTotalInvoice = hargaDasar.total_invoice;

            var array1 = await dbContext.sales_invoice_charges
                .Where(o => o.sales_invoice_id == sales_invoice_id).ToArrayAsync();
            var tempQualitySamplingAnalyte = dbContext.vw_quality_sampling_analyte.Where(o => o.despatch_order_id == lookupSalesInvoice.despatch_order_id);


            foreach (sales_invoice_charges arrayX in array1)
            {
                var vAdjustment = new InvoiceOutline()
                {
                    //invoice_type = "exportB",
                    invoice_type = lookupSalesInvoice.sales_type_name ?? "",
                    invoice_item = arrayX.sales_charge_name, //"CV Price",
                    invoice_item_type = arrayX.sales_charge_name,  //"CV Price Adjustment",
                    quantity = (double)lookupSalesInvoice.quantity,
                    adjustment_quantity = (double)lookupSalesInvoice.quantity,
                    price = (double)lookupSalesInvoice.unit_price * exchange_rate,
                    adjustment_price = (double)arrayX.price * exchange_rate,
                    actualValue = 100
                };

                dynamic lookup;
                switch(arrayX.sales_charge_code)
                {
                    //case "ash" : vAdjustment.actualValue = (double)tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "Ash (adb)").FirstOrDefault().analyte_value;
                    case "ash":
                        lookup = tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "Ash (adb)").FirstOrDefault();
                        if (lookup != null && lookup.analyte_value != null)
                            vAdjustment.actualValue = (double)lookup.analyte_value;
                        else
                            return BadRequest("'Ash (adb)' not found!");
                        break;

                    //case "sulphur_adjustment": vAdjustment.actualValue = (double)tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "TS (adb)").FirstOrDefault().analyte_value;
                    case "sulphur_adjustment":
                        lookup = tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "TS (adb)").FirstOrDefault();
                        if (lookup != null && lookup.analyte_value != null)
                            vAdjustment.actualValue = (double)lookup.analyte_value;
                        else
                            return BadRequest("'TS (adb)' not found!");
                        break;

                    //case "cv_adjustment": vAdjustment.actualValue = (double)tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "GCV (ar)").FirstOrDefault().analyte_value;
                    case "cv_adjustment":
                        lookup = tempQualitySamplingAnalyte.Where(o => o.analyte_symbol == "GCV (ar)").FirstOrDefault();
                        if (lookup != null && lookup.analyte_value != null)
                            vAdjustment.actualValue = (double)lookup.analyte_value;
                        else
                            return BadRequest("'GCV (ar)' not found!");
                        break;
                    default: vAdjustment.actualValue = 0;
                        break;

                }
                vAdjustment.value = vAdjustment.adjustment_price * vAdjustment.adjustment_quantity;
                xTotalInvoice += vAdjustment.value;
                vAdjustment.total_invoice = xTotalInvoice;

                retVal.Add(vAdjustment);
            }

            var shippingCost = dbContext.vw_shipping_cost
                .Where(o => o.organization_id == CurrentUserContext.OrganizationId &&
                    o.despatch_order_id == lookupSalesInvoice.despatch_order_id).FirstOrDefault();

            //bobby 20020420 freight cost
            if (shippingCost != null)
            {
                
                var cost1_Outline = new InvoiceOutline()
                {
                    invoice_type = "shipping cost",
                    invoice_item = "shipping cost",
                    invoice_item_type = "freight cost",
                    quantity = (double)lookupSalesInvoice.quantity,
                    adjustment_quantity = (double)shippingCost.quantity, //lookupSalesInvoice.quantity,
                    price = (Double)(shippingCost.freight_rate), // * exchange_rate,
                    //adjustment_price = (Double)(shippingCost.freight_rate * (int)lookupSalesInvoice.quantity)
                };
                cost1_Outline.value = (Double)(shippingCost.freight_rate * shippingCost.quantity); //(int)lookupSalesInvoice.quantity) * exchange_rate ;
                cost1_Outline.total_invoice = xTotalInvoice + ((Double)(shippingCost.freight_rate * shippingCost.quantity)) ;//(int)lookupSalesInvoice.quantity) * exchange_rate);
                retVal.Add(cost1_Outline);
                xTotalInvoice += (Double)(shippingCost.freight_rate * shippingCost.quantity);//(int)lookupSalesInvoice.quantity) * exchange_rate;

                var cost2_Outline = new InvoiceOutline()
                {
                    invoice_type = "shipping cost",
                    invoice_item = "shipping cost",
                    invoice_item_type = "insurance",
                    quantity = (double)lookupSalesInvoice.quantity,
                    adjustment_quantity = (double)lookupSalesInvoice.quantity,
                    price = (Double)shippingCost.insurance_cost,// * exchange_rate,
                    adjustment_price = (Double)shippingCost.insurance_cost,// * exchange_rate
                };
                cost2_Outline.value = (Double)shippingCost.insurance_cost;
                cost2_Outline.total_invoice = xTotalInvoice + ((Double)shippingCost.insurance_cost);// * exchange_rate);
                retVal.Add(cost2_Outline);
                xTotalInvoice += (Double)shippingCost.insurance_cost;// * exchange_rate;
            }
            var xTotalInvoice_beforTax = xTotalInvoice;

            var taxes1 = await dbContext.vw_sales_contract_taxes
                .Where(o => o.sales_contract_term_id == lookupSalesInvoice.sales_contract_term_id).ToArrayAsync();

            double tempTaxInvoice = 0.0;
            foreach (vw_sales_contract_taxes tax1 in taxes1)
            {
                var taxOutline = new InvoiceOutline()
                {
                    invoice_type = "tax",
                    invoice_item = tax1.sales_contract_tax_name, 
                    invoice_item_type = tax1.tax_name,  
                    quantity = (int)tax1.calculation_sign,
                    adjustment_quantity = (int)tax1.calculation_sign,
                    price = (double)(tax1.tax_rate),
                    adjustment_price = (double)(tax1.tax_rate)
                };
                taxOutline.value = taxOutline.adjustment_price * taxOutline.adjustment_quantity;
                taxOutline.total_invoice = xTotalInvoice * taxOutline.value/100;
                tempTaxInvoice += taxOutline.total_invoice;
                retVal.Add(taxOutline);
                listTaxes.Add(taxOutline);
            }
            xTotalInvoice += tempTaxInvoice;

            //bobby 20220519 show total
            var totalOutline = new InvoiceOutline()
            {
                invoice_type = "total",
                invoice_item = "Total",
                invoice_item_type = "Total",
                quantity = 0,
                adjustment_quantity = 0,
                price = 0,
                adjustment_price = 0
            };
            //totalOutline.value = xTotalInvoice;
            totalOutline.total_invoice = xTotalInvoice;
            retVal.Add(totalOutline);
            listTaxes.Add(totalOutline);
            //


            var lookupCustomer = await dbContext.vw_customer.Where(o => o.id == lookupDespatchOrder.customer_id).FirstOrDefaultAsync();
            var lookupSalesContract = await dbContext.vw_sales_contract.Where(o => o.id == lookupDespatchOrder.sales_contract_id).FirstOrDefaultAsync();
            //var lookupSalesContractTerm = await dbContext.vw_sales_contract_term.Where(o => o.id == lookupSalesInvoice.sales_contract_term_id).FirstOrDefaultAsync();

            var respDownPayment = 0.0;
            if (lookupSalesInvoice.downpayment != null)
            {
                respDownPayment = (double)lookupSalesInvoice.downpayment * exchange_rate;

                //bobby 20220523 downpayment
                if (lookupSalesInvoice.downpayment != lookupSalesInvoice.total_price)
                {
                    //bobby 20220523 downpayment pelunasan
                    double dp_value = 0;
                    double dp_exchange_rate = 0;
                    if (lookupSalesInvoiceDP != null)
                    {
                        //var total_price = (double)lookupSalesInvoice.total_price;
                        dp_value = Convert.ToDouble(lookupSalesInvoiceDP.downpayment);
                        dp_exchange_rate = Convert.ToDouble(lookupSalesInvoiceDP.exchange_rate);
                        // var invoice_value = xTotalInvoice / exchange_rate;
                        // invoice_value -= dp_value;
                        var totaldownpayment2 = new InvoiceOutline()
                        {
                            invoice_type = "total",
                            invoice_item = "Down Payment",
                            invoice_item_type = "Down Payment",
                            quantity = 0,
                            adjustment_quantity = 0,
                            price = 0,
                            adjustment_price = 0
                        };
                        //totalOutline.value = xTotalInvoice;
                        totaldownpayment2.total_invoice = dp_value * dp_exchange_rate;
                        retVal.Add(totaldownpayment2);
                        listTaxes.Add(totaldownpayment2);

                    }
                    // downpayment pelunasan

                    var totaldownpayment = new InvoiceOutline()
                    {
                        invoice_type = "total",
                        invoice_item = "Total Invoice",
                        invoice_item_type = "Total Invoice",
                        quantity = 0,
                        adjustment_quantity = 0,
                        price = 0,
                        adjustment_price = 0
                    };
                    //totalOutline.value = xTotalInvoice;
                    if (lookupSalesInvoiceDP != null)
                    {
                        totaldownpayment.total_invoice = xTotalInvoice_beforTax - (dp_value * dp_exchange_rate) ;
                    }
                    else
                    {
                        totaldownpayment.total_invoice = respDownPayment;
                    }
                    retVal.Add(totaldownpayment);
                    listTaxes.Add(totaldownpayment);
                }
                //

                
            }

            if (lookupSalesInvoice.sales_type_name != null && lookupSalesInvoice.sales_type_name.ToLower().Trim() == "domestic pln")
                xTotalInvoice = (double)lookupSalesInvoice.total_price;

            var retVal2 = new
            {
                retVal,
                invoiceTotal = xTotalInvoice,
                invoiceFreightCost = varFreightCost,
                invoiceDownPayment = respDownPayment,
                invoiceDueDate = lookupSalesInvoice.invoice_date,
                invoiceName = lookupSalesInvoice.invoice_number,
                invoiceCurrencySymbol = varInvoiceCurrencySymbol,
                invoiceCurrencyCode = varInvoiceCurrencyCode,
                invoiceCurrencyName = varInvoiceCurrencyName,
                invoiceBuyerName = lookupDespatchOrder.customer_name,
                invoiceContractName = lookupDespatchOrder.sales_contract_name,
                invoiceVesselName = lookupDespatchOrder.vessel_name,
                invoiceLaycanDate = lookupDespatchOrder.laycan_start.ToString() + " - " + lookupDespatchOrder.laycan_end.ToString(),
                invoiceBuyerAddress = lookupCustomer.primary_address,
                invoiceBillOfLadingDate = lookupDespatchOrder.bill_of_lading_date,
                invoiceBillOfLadingNumber = "Bill 973 (TBD)",
                vesselFrom = "Indonesia (TBD)",
                vesselTo = lookupDespatchOrder.ship_to,
                invoicePayment = "Payment Term 1 (TBD)",
                letterOfCreditNumber = lookupDespatchOrder.letter_of_credit,
                dateOfIssue = "2022/01/23 (TBD)",
                issuedByUser = "User Issuer (TBD)",
                //invoiceNotes = lookupSalesInvoice.notes,
                invoiceNotes = notestext,
                salesContractNumber = lookupSalesContract.sales_contract_name,
                salesContractId = lookupSalesContract.id,
                salesContractDate = lookupSalesContract.start_date,
                lcStatus = lookupSalesInvoice.lc_status,
                lcDateIssue = lookupSalesInvoice.lc_date_issue,
                lcIssuingBank = lookupSalesInvoice.lc_issuing_bank
                //listTaxes,
            };

            return retVal2;
        }

        [HttpGet("PrintStatusLookup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public object PrintStatusLookup()
        {
            var result = new List<dynamic>();
            try
            {
                foreach (var item in Common.Constants.PrintStatus)
                {
                    dynamic obj = new ExpandoObject();
                    obj.value = item;
                    obj.text = item;
                    result.Add(obj);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }

            return result;
        }

        [HttpGet("GetSalesInvoiceAdjusmentBySalesInvoiceId")]
        public async Task<object> GetSalesInvoiceAdjusmentBySalesInvoiceId(string salesInvoiceId, DataSourceLoadOptions loadOptions)
        {
            var lookup = dbContext.sales_invoice_charges
                .Where(o => o.sales_invoice_id == salesInvoiceId);
            return await DataSourceLoader.LoadAsync(lookup, loadOptions);
        }

        [HttpGet("CheckSyntaxFormula")]
        public async Task<object> CheckSyntaxFormula(string tsyntax, DataSourceLoadOptions loadOptions)
        {
            string tstatus = "";
            string tmessage = "";
            string xsyntax = tsyntax.ToLower().Replace(" ", "");
            var lookupMasterList = await dbContext.master_list.Where(o => o.item_group == "reserved-words").ToArrayAsync() ;
            foreach (master_list lookup in lookupMasterList)
            {
                xsyntax = xsyntax.Replace(lookup.item_name.ToLower(), "5");
            }
            var lookupAnalyteSymbol = dbContext.analyte.
                Where(o => o.analyte_symbol.Length > 0);
            foreach (analyte lookup in lookupAnalyteSymbol)
            {
                xsyntax = xsyntax.Replace((lookup.analyte_symbol.Replace(" ","")+".value()").ToLower(), "4");
                xsyntax = xsyntax.Replace((lookup.analyte_symbol.Replace(" ","")+".target()").ToLower(), "3");

                xsyntax = xsyntax.Replace((lookup.analyte_symbol.Replace(" ", "") + ".min()").ToLower(), "4");
                xsyntax = xsyntax.Replace((lookup.analyte_symbol.Replace(" ", "") + ".max()").ToLower(), "3");
            }
            try
            {
                var engine1 = new Engine();
                var expression1 = engine1.Parse(xsyntax);
                var resultText = expression1.Execute().ToString();
                tstatus = "OK";
                tmessage = "OK";
            }
            catch (Exception ex)
            {
                tstatus = "Error in syntax";
                tmessage = ex.Message;
            }
            var retVal = new
            {
                status = tstatus,
                message = tmessage
            };
            return retVal;
        }

        [HttpGet("GetReservedWords")]
        public async Task<object> GetReservedWords(DataSourceLoadOptions loadOptions)
        {
            ReservedWords rw;
            List<ReservedWords> listRW = new List<ReservedWords>();
            var lookupMasterList = await dbContext.master_list.
                Where(o => o.item_group == "reserved-words").ToArrayAsync();
            foreach (master_list lookup in lookupMasterList)
            {
                rw = new ReservedWords(lookup.item_name, lookup.notes);
                listRW.Add(rw);
            }
            var lookupAnalyteSymbol = await dbContext.analyte.
                Where(o => o.analyte_symbol.Length > 0).ToArrayAsync();
            foreach (analyte lookup in lookupAnalyteSymbol)
            {
                rw = new ReservedWords(lookup.analyte_symbol+".Value()", "actual value of " + lookup.analyte_name);
                listRW.Add(rw);
                rw = new ReservedWords(lookup.analyte_symbol + ".Target()", "target value of " + lookup.analyte_name);
                listRW.Add(rw);

                rw = new ReservedWords(lookup.analyte_symbol + ".Min()", "minimum value of " + lookup.analyte_name);
                listRW.Add(rw);

                rw = new ReservedWords(lookup.analyte_symbol + ".Max()", "maximum value of " + lookup.analyte_name);
                listRW.Add(rw);
            }
            return listRW;
        }

        [HttpGet("GetReservedFunctions")]
        public async Task<object> GetReservedFunctions(DataSourceLoadOptions loadOptions)
        {
            ReservedWords rw;
            List<ReservedWords> listRW = new List<ReservedWords>();
            var lookupMasterList = await dbContext.master_list.
                Where(o => o.item_group == "reserved-functions").ToArrayAsync();
            foreach (master_list lookup in lookupMasterList)
            {
                rw = new ReservedWords(lookup.item_name, lookup.notes);
                listRW.Add(rw);
            }
            return listRW;
        }


        [HttpPost("InsertPayment")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> InsertPayment([FromForm] string values)
        {
            logger.Trace($"string values = {values}");

            try
            {
                if (await mcsContext.CanCreate(dbContext, nameof(sales_invoice_payment),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    var record = new sales_invoice_payment();
                    JsonConvert.PopulateObject(values, record);

                    record.id = Guid.NewGuid().ToString("N");
                    record.created_by = CurrentUserContext.AppUserId;
                    record.created_on = System.DateTime.Now;
                    record.modified_by = null;
                    record.modified_on = null;
                    record.is_active = true;
                    record.is_default = null;
                    record.is_locked = null;
                    record.entity_id = null;
                    record.owner_id = CurrentUserContext.AppUserId;
                    record.organization_id = CurrentUserContext.OrganizationId;

                    var lookupCurrency = await dbContext.vw_currency
                    .Where(o => o.currency_code == record.currency_code)
                    .FirstOrDefaultAsync();
                    record.currency_id = lookupCurrency.id;

                    dbContext.sales_invoice_payment.Add(record);
                    await dbContext.SaveChangesAsync();

                    return Ok(record.payment_value);
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

        //[HttpGet("RemainedCreditLimit")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<object> RemainedCreditLimit(string customer_id, DataSourceLoadOptions loadOptions)
        //{
        //    double remainedCreditLimit = 0.0;
        //    try
        //    {
        //        // find all sales contract within the same customer
        //        var vw_sales_invoice_custid = dbContext.vw_sales_invoice
        //            .Where(o => o.customer_id == customer_id)
        //            ;
        //        var array_vw_sales_invoice_custid = await vw_sales_invoice_custid.ToArrayAsync();
        //        double totalPaymentfromAllInvoice = 0.0;
        //        double totalPricefromAllInvoice = 0.0;
        //        double customerCreditLimit = 0.0;


        //        if (array_vw_sales_invoice_custid.Length > 0)
        //        {
        //            if (array_vw_sales_invoice_custid[0].credit_limit != null)
        //            {
        //                customerCreditLimit = (double)array_vw_sales_invoice_custid[0].credit_limit;
        //            }

        //            foreach (vw_sales_invoice item1 in array_vw_sales_invoice_custid)
        //            {
        //                if (item1.total_price != null)
        //                {
        //                    totalPricefromAllInvoice += (double)item1.total_price;
        //                }
        //                var vw_sales_invoice_payment_data = dbContext.vw_sales_invoice_payment.Where(o => o.sales_invoice_number == item1.invoice_number);
        //                var array_vw_sales_invoice_payment_data = await vw_sales_invoice_payment_data.ToArrayAsync();
        //                if (array_vw_sales_invoice_payment_data.Length > 0)
        //                {
        //                    foreach (vw_sales_invoice_payment itemX in array_vw_sales_invoice_payment_data)
        //                    {
        //                        if (itemX.payment_value != null)
        //                        {
        //                            totalPaymentfromAllInvoice += (double)itemX.payment_value;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        remainedCreditLimit = customerCreditLimit - totalPricefromAllInvoice + totalPaymentfromAllInvoice;

        //        return Ok(remainedCreditLimit);

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

       
    }

    public class AdjustmentCalculation
    {
        public string analyte_symbol { get; set; }
        public string analyte_name { get; set; }
        public double analyte_value { get; set; }
        public double analyte_target { get; set; }
        public double analyte_minimum { get; set; }
        public double analyte_maximum { get; set; }

        public AdjustmentCalculation()
        {
            analyte_symbol = "";
            analyte_name = "";
            analyte_value = 0.0;
            analyte_target = 0.0;
            analyte_minimum = 0.0;
            analyte_maximum = 0.0;
        }
    }

    public class SalesPrice
    {
        public string chargeCode { get; set; }
        public string chargeName { get; set; }
        public double price { get; set; }
        public SalesPrice()
        {
            chargeName = "name";
            price = 0.0;
            chargeCode = "code";
        }
        public SalesPrice(string chargeN, string chargeC, double chargeP)
        {
            chargeName = chargeN;
            price = chargeP;
            chargeCode = chargeC;
        }

    }

    public class ReservedWords
    {
        public string reservedW { get; set; }
        public string description { get; set; }
        public ReservedWords()
        {
            reservedW = "";
            description = "";
        }
        public ReservedWords(string rw, string desc)
        {
            reservedW = rw;
            description = desc;
        }
    }

    public class InvoiceOutline
    {
        public string invoice_type { get; set; }
        public string invoice_item { get; set; }
        public string invoice_item_type { get; set; }
        public double quantity { get; set; }
        public double adjustment_quantity { get; set; }
        public double price { get; set; }
        public double adjustment_price { get; set; }
        public double actualValue { get; set; }

        public double value { get; set; }
        public double total_invoice { get; set; }

    }

    public class CreditLimitData
    {
        public double InitialCreditLimit { get; set; }
        public double RemainedCreditLimit { get; set; }
        public CreditLimitData()
        {
            InitialCreditLimit = 0.0;
            RemainedCreditLimit = 0.0;
        }
        public CreditLimitData(double initCL, double remainedCL)
        {
            InitialCreditLimit = initCL;
            RemainedCreditLimit = remainedCL;
        }

    }
}