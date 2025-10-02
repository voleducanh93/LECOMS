using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Enum
{
    public enum PaymentStatus { 
        Pending = 1, Succeeded, Failed, Refunded, PartiallyRefunded 
    }

}
