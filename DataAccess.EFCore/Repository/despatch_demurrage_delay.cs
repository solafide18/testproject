﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class despatch_demurrage_delay
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
        public string despatch_demurrage_id { get; set; }
        public string event_category_id { get; set; }
        public decimal? demurrage_percent { get; set; }
        public decimal? despatch_percent { get; set; }
        public string despatch_order_id { get; set; }

        public virtual despatch_demurrage despatch_demurrage_ { get; set; }
        public virtual despatch_order despatch_order_ { get; set; }
        public virtual organization organization_ { get; set; }
    }
}
