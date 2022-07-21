﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_price_index_map_detail
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
        public string price_index_map_id { get; set; }
        public decimal? start_range { get; set; }
        public decimal? end_range { get; set; }
        public decimal? value { get; set; }
        public string price_index_name { get; set; }
        public string price_index_code { get; set; }
        public bool? is_base_index { get; set; }
        public string price_index_map_name { get; set; }
        public string organization_name { get; set; }
        public string record_created_by { get; set; }
        public string record_modified_by { get; set; }
        public string record_owning_user { get; set; }
        public string record_owning_team { get; set; }
    }
}
