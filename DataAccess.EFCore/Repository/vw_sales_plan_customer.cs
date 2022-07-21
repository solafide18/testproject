using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class vw_sales_plan_customer
    {
        public string organization_name { get; set; }
        public string owner_name { get; set; }
        public string business_partner_name { get; set; }
        public string primary_contact_name { get; set; }
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
        public string customer_id { get; set; }
        public string sales_plan_detail_id { get; set; }
        public decimal? quantity { get; set; }
        public string sales_contract_id { get; set; }
        public string dmo { get; set; }
        public string electricity { get; set; }
    }
}
