using OculusGraphQLApiLib.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class Entitlement
    {
        public string active_state { get; set; } = "";
        public ActiveState activeState {  get
            {
                return (ActiveState)Enum.Parse(typeof(ActiveState), active_state);
            } }
        public string grant_reason { get; set; } = "";
        public GrantReason grantReason
        {
            get
            {
                return (GrantReason)Enum.Parse(typeof(GrantReason), grant_reason);
            }
        }
        public long grant_time { get; set; } = 0;
        public long expiration_time { get; set; } = 0;
        public bool is_refundable { get; set; } = false;
        public string id { get; set; } = "";
        public Application item { get; set; } = new Application();
    }
}
