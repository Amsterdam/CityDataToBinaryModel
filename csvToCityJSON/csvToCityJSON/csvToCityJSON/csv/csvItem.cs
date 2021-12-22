using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csvToCityJSON.csv
{
    class csvItem
    {
        public Dictionary<string, string> properties;
        public csvItem()
        {
            properties = new Dictionary<string, string>();
        }
    }
}
