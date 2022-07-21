﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_sales_contract_detail
    {
        public string id { get; set; }
        public string created_by { get; set; }
        public DateTime? created_on { get; set; }
        public string modified_by { get; set; }
        public DateTime? modified_on { get; set; }
        public bool? is_active { get; set; }
        public bool? is_locked { get; set; }
        public bool? is_default { get; set; }
        public string owner_id { get; set; }
        public string organization_id { get; set; }
        public string entity_id { get; set; }
        public string sales_contract_id { get; set; }
        public string contract_number { get; set; }
        public string contract_name { get; set; }
        public string product_id { get; set; }
        public string product_name { get; set; }
        public decimal? quantity { get; set; }
        public string sales_price_id { get; set; }
        public decimal? sp_price { get; set; }
        public string sales_charge_id { get; set; }
        public string sales_charge_name { get; set; }
        public string tax_id { get; set; }
        public string tax_name { get; set; }
        public decimal? rate { get; set; }
        public string notes { get; set; }
        public decimal? price { get; set; }
        public string uom_id { get; set; }
        public string uom_name { get; set; }
        public string uom_symbol { get; set; }
        public string charge_formula { get; set; }
        public decimal? tax_rate { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
