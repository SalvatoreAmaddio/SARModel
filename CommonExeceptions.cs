using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SARModel
{

    public class DBNotFoundException : Exception 
    { 
        public DBNotFoundException(string dbName, IEnumerable dbs) : base(dbName)
        {
            dbName = $"{dbName} NOT FOUND";
        }
    }

    public class ZeroOrNegativeIndexExeception : Exception 
    { 
        public ZeroOrNegativeIndexExeception() : base("The Index parameter cannot be 0 or negative.") { }   
    }

    public class NullRangeEx : Exception
    {
        public NullRangeEx() : base("The Range is null.") { }
    }
}
