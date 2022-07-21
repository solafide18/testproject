﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class blending_plan
    {
        public blending_plan()
        {
            blending_plan_quality = new HashSet<blending_plan_quality>();
            blending_plan_source = new HashSet<blending_plan_source>();
        }

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
        public string transaction_number { get; set; }
        public string reference_number { get; set; }
        public string accounting_period_id { get; set; }
        public string process_flow_id { get; set; }
        public string survey_id { get; set; }
        public string product_id { get; set; }
        public string uom_id { get; set; }
        public string destination_location_id { get; set; }
        public DateTime? unloading_datetime { get; set; }
        public decimal? unloading_quantity { get; set; }
        public string transport_id { get; set; }
        public int? trip_count { get; set; }
        public string equipment_id { get; set; }
        public decimal? hour_usage { get; set; }
        public string despatch_order_id { get; set; }
        public string planning_category { get; set; }
        public string source_shift_id { get; set; }

        public virtual organization organization_ { get; set; }
        public virtual ICollection<blending_plan_quality> blending_plan_quality { get; set; }
        public virtual ICollection<blending_plan_source> blending_plan_source { get; set; }
    }
}
