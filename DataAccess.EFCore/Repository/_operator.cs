﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class _operator
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
        public string operator_name { get; set; }
        public string nik { get; set; }
        public string versatility { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public bool? is_supervisor { get; set; }
    }
}
