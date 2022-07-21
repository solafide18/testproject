﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_advance_contract_item
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
        public string advance_contract_id { get; set; }
        public string contractor_code { get; set; }
        public string contractor_name { get; set; }
        public string advance_contract_number { get; set; }
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public decimal? quantity { get; set; }
        public string reference_number { get; set; }
        public string note { get; set; }
        public string quantity_uom_id { get; set; }
        public string quantity_uom_name { get; set; }
        public string quantity_uom_symbol { get; set; }
        public string item_name { get; set; }
        public string description { get; set; }
        public string master_list_id { get; set; }
        public string master_list_item_name { get; set; }
        public string master_list_item_group { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
