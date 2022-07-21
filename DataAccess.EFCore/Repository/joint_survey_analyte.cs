﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class joint_survey_analyte
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
        public string joint_survey_id { get; set; }
        public string analyte_id { get; set; }
        public string uom_id { get; set; }
        public decimal? analyte_value { get; set; }

        public virtual analyte analyte_ { get; set; }
        public virtual organization organization_ { get; set; }
    }
}
