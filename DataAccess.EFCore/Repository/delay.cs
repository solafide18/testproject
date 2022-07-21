﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class delay
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
        public DateTime delay_date { get; set; }
        public string business_area_id { get; set; }
        public string equipment_type_id { get; set; }
        public string equipment_id { get; set; }
        public string remark { get; set; }
        public string approved_by { get; set; }
        public DateTime? approved_on { get; set; }

        public virtual business_area business_area_ { get; set; }
        public virtual equipment equipment_ { get; set; }
        public virtual equipment_type equipment_type_ { get; set; }
        public virtual organization organization_ { get; set; }
    }
}
