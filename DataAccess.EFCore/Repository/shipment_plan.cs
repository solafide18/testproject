using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class shipment_plan
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
        public string sales_contract_id { get; set; }
        public int? month_id { get; set; }
        public string destination { get; set; }
        public string customer_id { get; set; }
        public string shipment_number { get; set; }
        public string incoterm { get; set; }
        public string transport_id { get; set; }
        public string laycan { get; set; }
        public DateTime? eta { get; set; }
        public decimal? qty_sp { get; set; }
        public string remarks { get; set; }
        public string traffic_officer_id { get; set; }
        public string sales_plan_customer_id { get; set; }
        public string shipment_year { get; set; }

        public virtual organization organization_ { get; set; }
    }
}
