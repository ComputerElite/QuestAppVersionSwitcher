using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class ParentApplication
    {
        public string id { get; set; } = "";
        public string canonicalName { get; set; } = "";
        public string displayName { get; set; } = "";
        public Headset hmd { get; set; } = Headset.RIFT;
    }
}
