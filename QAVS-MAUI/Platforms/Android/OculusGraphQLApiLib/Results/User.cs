using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class OculusUserWrapper
    {
        public OculusUser user { get; set; } = new OculusUser();
    }
    public class OculusUser
    {
        public string alias { get { return display_name; } set { display_name = value; } }
        public string display_name { get; set; } = "";
        public OculusUri profile_photo { get;set; } = new OculusUri();
        public string id { get; set; } = "";
        public Nodes<Entitlement> active_entitlements { get; set; } = new Nodes<Entitlement>();
    }
}