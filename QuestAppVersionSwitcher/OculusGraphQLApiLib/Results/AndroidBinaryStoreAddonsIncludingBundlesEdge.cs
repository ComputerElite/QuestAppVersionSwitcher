using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AppItemBundle : IAPItem
    {
        public Edges<Node<IAPItem>> bundle_items { get; set; } = new Edges<Node<IAPItem>>();
        public bool is_360_sensor_setup_required { get; set; } = false;
        public bool is_guardian_required { get; set; } = false;
        public bool is_roomscale_required { get; set; } = false;
        public bool is_touch_required { get; set; } = false;
    }
}
