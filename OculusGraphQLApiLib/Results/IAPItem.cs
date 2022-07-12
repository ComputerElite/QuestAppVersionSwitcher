using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class IAPItem : GraphQLBase
    {
        public AppStoreOffer current_offer { get; set; } = new AppStoreOffer();
        public string display_name { get; set; } = "";
        public string display_short_description { get; set; } = "";
        public string id { get; set; } = "";
        public ParentApplication parentApplication { get; set; } = new ParentApplication();
        public ParentApplication parent_application
        {
            get
            {
                return parentApplication;
            }
        }
        public AssetFile latest_supported_asset_file { get; set; } = new AssetFile();
    }
}
