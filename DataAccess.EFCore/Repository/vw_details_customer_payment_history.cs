﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_details_customer_payment_history
    {
        public string customer_id { get; set; }
        public string business_partner_name { get; set; }
        public DateTime? tdate { get; set; }
        public string invoice_number { get; set; }
        public int? billing { get; set; }
        public decimal? receipt { get; set; }
        public int? outstanding { get; set; }
        public string currency { get; set; }
        public string despatch_order_number { get; set; }
        public string sales_contract_name { get; set; }
        public string contract_term_name { get; set; }
        public string despatch_plan_name { get; set; }
        public decimal? credit_limit { get; set; }
    }
}
