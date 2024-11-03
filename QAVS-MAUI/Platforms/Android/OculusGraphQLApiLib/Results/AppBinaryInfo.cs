using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AppBinaryInfoContainer
    {
        public AppBinaryInfo app_binary_info { get; set; } = new AppBinaryInfo();
    }

    public class AppBinaryInfo
    {
        public List<Binary> info { get; set; } = new List<Binary>();
    }

    public class Binary
    {
        public AndroidBinary binary { get; set; } = new AndroidBinary();
    }
}
