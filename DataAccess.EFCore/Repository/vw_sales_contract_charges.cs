﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_sales_contract_charges
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
        public string sales_contract_term_id { get; set; }
        public string contract_term_name { get; set; }
        public string sales_charge_id { get; set; }
        public string sales_charge_name { get; set; }
        public string sales_charge_code { get; set; }
        public string sales_charge_type_name { get; set; }
        public string charge_formula { get; set; }
        public decimal? decimal_places { get; set; }
        public string rounding_type_id { get; set; }
        public string rounding_type_name { get; set; }
        public string description { get; set; }
        public string prerequisite { get; set; }
        public int? order { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
