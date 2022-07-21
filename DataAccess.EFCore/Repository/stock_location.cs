﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class stock_location
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
        public string stock_location_name { get; set; }
        public float? latitude { get; set; }
        public float? longitude { get; set; }
        public string product_id { get; set; }
        public string uom_id { get; set; }
        public decimal? minimum_capacity { get; set; }
        public decimal? maximum_capacity { get; set; }
        public decimal? target_capacity { get; set; }
        public DateTime? opening_date { get; set; }
        public DateTime? closing_date { get; set; }
        public string business_area_id { get; set; }
        public string parent_stock_location_id { get; set; }
        public decimal? current_stock { get; set; }

        public virtual organization organization_ { get; set; }
        public virtual product product_ { get; set; }
        public virtual uom uom_ { get; set; }
    }
}
