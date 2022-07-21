using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class draft_survey
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
        public string survey_number { get; set; }
        public DateTime? survey_date { get; set; }
        public string stock_location_id { get; set; }
        public string product_id { get; set; }
        public string sampling_template_id { get; set; }
        public decimal? quantity { get; set; }
        public string uom_id { get; set; }
        public string surveyor_id { get; set; }
        public string approved_by { get; set; }
        public DateTime? approved_on { get; set; }
        public string quality_sampling_id { get; set; }
        public string despatch_order_id { get; set; }
        public DateTime? bill_lading_date { get; set; }
        public string bill_lading_number { get; set; }

        public virtual organization organization_ { get; set; }
    }
}
