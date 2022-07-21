﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_shipping_instruction_stevedoring
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
        public string shipping_instruction_id { get; set; }
        public string barge_id { get; set; }
        public string vehicle_name { get; set; }
        public string vehicle_id { get; set; }
        public string port_location_id { get; set; }
        public string business_area_id { get; set; }
        public string port_location_code { get; set; }
        public string stock_location_name { get; set; }
        public decimal? cargo { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
