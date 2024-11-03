using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class IAPEntitlement
    {
        public string _typename { get; set; } = "IAPEntitlement";
        public IAPItem item { get; set; } = new IAPItem();
    }
}
