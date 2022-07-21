﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class shipping_cost
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
        public long shipping_cost_number { get; set; }
        public string approved_by { get; set; }
        public DateTime? approved_on { get; set; }
        public string despatch_order_id { get; set; }
        public decimal? freight_rate { get; set; }
        public decimal? insurance_cost { get; set; }
        public string remark { get; set; }
        public decimal? quantity { get; set; }
    }
}
