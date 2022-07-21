﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_shipping_instruction
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
        public string shipping_instruction_number { get; set; }
        public DateTime? shipping_instruction_date { get; set; }
        public string despatch_order_id { get; set; }
        public string to_other { get; set; }
        public string notify_party { get; set; }
        public string cargo_description { get; set; }
        public string cc { get; set; }
        public string sampling_template_id { get; set; }
        public string approved_by_id { get; set; }
        public string marked { get; set; }
        public DateTime? issued_date { get; set; }
        public string placed { get; set; }
        public string shipping_instruction_created_by { get; set; }
        public string lampiran1 { get; set; }
        public string lampiran2 { get; set; }
        public string lampiran3 { get; set; }
        public string lampiran4 { get; set; }
        public string lampiran5 { get; set; }
        public DateTime? released_date { get; set; }
        public string barge_id { get; set; }
        public string tug_id { get; set; }
        public string hs_code { get; set; }
        public string analyte_standard { get; set; }
        public string notify_party_address { get; set; }
        public string vessel_id { get; set; }
        public string vessel_name { get; set; }
        public string barge_name { get; set; }
        public bool? is_geared { get; set; }
        public string sales_contract_name { get; set; }
        public string contract_term_name { get; set; }
        public string sales_contract_id { get; set; }
        public string despatch_plan_name { get; set; }
        public DateTime? scdp_despatch_date { get; set; }
        public string scdp_notes { get; set; }
        public string scdp_fulfilment_type_id { get; set; }
        public string scdp_delivery_term_id { get; set; }
        public string delivery_term_id { get; set; }
        public string seller_id { get; set; }
        public string seller_name { get; set; }
        public DateTime? eta_plan { get; set; }
        public string customer_id { get; set; }
        public string customer_name { get; set; }
        public string ship_to_name { get; set; }
        public string ship_to { get; set; }
        public string shipping_agent { get; set; }
        public DateTime? laycan_start { get; set; }
        public DateTime? laycan_end { get; set; }
        public string loading_port { get; set; }
        public string discharge_port { get; set; }
        public decimal? required_quantity { get; set; }
        public string sampling_template_name { get; set; }
        public string sampling_template_code { get; set; }
        public string organization_name { get; set; }
        public string si_number { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string approved_by { get; set; }
        public string record_owning_team { get; set; }
    }
}
