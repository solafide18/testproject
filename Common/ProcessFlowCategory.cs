using System;
using System.Collections.Generic;

namespace Common
{
    public class ProcessFlowCategory
    {
        public const string PRODUCTION = "Production";
        public const string PROCESSING = "Processing";
        public const string BLENDING = "Blending";
        public const string HAULING = "Hauling";
        public const string RAILING = "Railing";
        public const string BARGING = "Barging";
        public const string SHIPPING = "Shipping";
        public const string WASTE_REMOVAL = "Waste Removal";
        public const string REHANDLING = "Rehandling";

        public static List<string> ProcessFlowCategories = new List<string>
        {
            PRODUCTION,
            PROCESSING,
            BLENDING,
            HAULING,
            RAILING,
            BARGING,
            SHIPPING,
            WASTE_REMOVAL,
            REHANDLING
        };
    }
}
