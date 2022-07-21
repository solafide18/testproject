﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class timesheet_detail
    {
        public timesheet_detail()
        {
            timesheet_detail_event = new HashSet<timesheet_detail_event>();
            timesheet_detail_productivity_problem = new HashSet<timesheet_detail_productivity_problem>();
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
        public string event_category_id { get; set; }
        public string mine_location_id { get; set; }
        public string destination_id { get; set; }
        public decimal? refuelling_quantity { get; set; }
        public decimal? quantity { get; set; }
        public decimal? distance { get; set; }
        public decimal? ritase { get; set; }
        public string uom_id { get; set; }
        public TimeSpan? timesheet_time { get; set; }
        public decimal? duration { get; set; }
        public string periode { get; set; }
        public string timesheet_id { get; set; }
        public TimeSpan? classification { get; set; }
        public string pit_id { get; set; }
        public string loader_id { get; set; }
        public string source_id { get; set; }
        public string material_id { get; set; }
        public decimal? volume { get; set; }
        public decimal? rit_rehandling { get; set; }
        public decimal? vol_rehandling { get; set; }
        public decimal? productivity { get; set; }
        public decimal? vol_distance { get; set; }
        public decimal? vol_density { get; set; }
        public string accounting_periode_id { get; set; }

        public virtual organization organization_ { get; set; }
        public virtual timesheet timesheet_ { get; set; }
        public virtual ICollection<timesheet_detail_event> timesheet_detail_event { get; set; }
        public virtual ICollection<timesheet_detail_productivity_problem> timesheet_detail_productivity_problem { get; set; }
    }
}
