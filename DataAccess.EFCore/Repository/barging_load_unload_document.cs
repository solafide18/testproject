﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class barging_load_unload_document
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
        public string barging_transaction_id { get; set; }
        public string activity_id { get; set; }
        public string document_type_id { get; set; }
        public string remark { get; set; }
        public bool? quantity { get; set; }
        public bool? quality { get; set; }
        public string filename { get; set; }
        public string type { get; set; }
        public bool? return_cargo { get; set; }

        public virtual barging_transaction barging_transaction_ { get; set; }
        public virtual organization organization_ { get; set; }
    }
}
