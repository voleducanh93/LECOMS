using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Enum
{
    public enum ProductStatus : byte
    {
        Draft = 0,
        Published = 1,
        OutOfStock = 2,
        Archived = 3
    }
}