﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class quality_sampling
    {
        public quality_sampling()
        {
            quality_sampling_analyte = new HashSet<quality_sampling_analyte>();
            quality_sampling_document = new HashSet<quality_sampling_document>();
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
        public string sampling_number { get; set; }
        public DateTime sampling_datetime { get; set; }
        public string stock_location_id { get; set; }
        public string product_id { get; set; }
        public string sampling_template_id { get; set; }
        public string surveyor_id { get; set; }
        public string approved_by { get; set; }
        public DateTime? approved_on { get; set; }
        public string despatch_order_id { get; set; }
        public string loading_type_id { get; set; }

        public virtual organization organization_ { get; set; }
        public virtual ICollection<quality_sampling_analyte> quality_sampling_analyte { get; set; }
        public virtual ICollection<quality_sampling_document> quality_sampling_document { get; set; }
    }
}
