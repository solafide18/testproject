﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_truck_cost_rate
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
        public string code { get; set; }
        public string name { get; set; }
        public string accounting_period_id { get; set; }
        public string accounting_period_name { get; set; }
        public decimal? hourly_rate { get; set; }
        public decimal? trip_rate { get; set; }
        public string currency_id { get; set; }
        public string currency_name { get; set; }
        public string truck_id { get; set; }
        public string vehicle_name { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
