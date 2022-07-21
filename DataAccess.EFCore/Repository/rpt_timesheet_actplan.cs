﻿using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class rpt_timesheet_actplan
    {
        public DateTime? timesheet_date { get; set; }
        public string timesheet_date_show { get; set; }
        public string timesheet_date_week { get; set; }
        public string shift_code { get; set; }
        public string product { get; set; }
        public string digger { get; set; }
        public string loader_name { get; set; }
        public decimal? volume { get; set; }
        public decimal? volume_plan { get; set; }
        public decimal? achievement { get; set; }
        public decimal? vol_distance { get; set; }
        public decimal? vol_distance_plan { get; set; }
        public decimal? vol_distance_achievement { get; set; }
    }
}
