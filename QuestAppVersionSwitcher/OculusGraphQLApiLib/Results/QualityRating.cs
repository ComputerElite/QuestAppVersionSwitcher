using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class QualityRating
    {
        public long star_rating { get; set; } = 0;
        public long count { get; set; } = 0;
        public long starRating { get { return star_rating; } set { star_rating = value; } }
    }
}
