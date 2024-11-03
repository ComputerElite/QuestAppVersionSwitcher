using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AppStoreAllAppsSection : GraphQLBase
    {
        public string section_name { get; set; } = "";
        public string id { get; set; } = "";
        public string style_theme { get; set; } = "DEFAULT";
        public Edges<Node<Application>> all_items { get; set; } = new Edges<Node<Application>>();
    }
}
