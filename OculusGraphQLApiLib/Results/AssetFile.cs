using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AssetFile
    {
        public string file_name { get; set; } = "";
        public string uri { get; set; } = "";
        public string size { get; set; } = "0";
        public string id { get; set; } = "";
        public long sizeNumerical
        {
            get
            {
                return long.Parse(size);
            }
        }
    }
}
