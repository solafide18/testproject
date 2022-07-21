using System;
using System.Collections.Generic;

namespace DataAccess.EFCore.Repository
{
    public partial class application_entity
    {
        public application_entity()
        {
            role_access = new HashSet<role_access>();
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
        public string entity_name { get; set; }
        public string display_name { get; set; }

        public virtual ICollection<role_access> role_access { get; set; }
    }
}
