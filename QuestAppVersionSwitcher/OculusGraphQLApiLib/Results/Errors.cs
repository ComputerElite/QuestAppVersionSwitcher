using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class Error
    {
        public string message { get; set; } = "";
        public string serverity { get; set; } = "";
        public List<object> path { get; set; } = new List<object>();
    }
}