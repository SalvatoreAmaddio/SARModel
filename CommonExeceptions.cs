using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SARModel
{
    public class ZeroOrNegativeIndexExeception : Exception 
    { 
        public ZeroOrNegativeIndexExeception() : base("The Index parameter cannot be 0 or negative.") { }   
    }

    public class NullRangeEx : Exception
    {
        public NullRangeEx() : base("The Range is null.") { }
    }
}
