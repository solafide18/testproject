﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class business_unit
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
        public string business_unit_name { get; set; }
        public string parent_business_unit_id { get; set; }
        public string default_team_id { get; set; }
        public string business_unit_code { get; set; }

        public virtual team default_team_ { get; set; }
        public virtual organization organization_ { get; set; }
    }
}
