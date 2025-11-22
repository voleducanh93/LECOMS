using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Gamification
{
    namespace LECOMS.Data.DTOs.Gamification
    {
        public class RedeemRuleCreateDTO
        {
            public string Reward { get; set; } = null!;
            public int CostPoints { get; set; }
            public bool Active { get; set; } = true;
        }

        public class RedeemRuleUpdateDTO
        {
            public int? CostPoints { get; set; }
            public bool? Active { get; set; }
        }

        public class RedeemRuleDTO
        {
            public string Id { get; set; } = null!;
            public string Reward { get; set; } = null!;
            public int CostPoints { get; set; }
            public bool Active { get; set; }
        }
    }

}
