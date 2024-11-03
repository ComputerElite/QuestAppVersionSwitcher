using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class Review
    {
        public OculusUser author { get; set; } = new OculusUser();
        public long date { get; set; } = 0;
        public string id { get; set; } = "";
        public string reviewDescription { get; set; } = "";
        public string reviewTitle { get; set; } = "";
        public long review_helpful_count { get; set; } = 0;
        public long score { get; set; } = 0;
    }
}
