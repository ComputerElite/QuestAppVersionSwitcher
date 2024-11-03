using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class AppStoreOffer
    {
        public long end_time { get; set; } = 0;
        public string id { get; set; } = "";
        public bool show_timer { get; set; } = false;
        public AppStoreOfferPrice price { get; set; } = new AppStoreOfferPrice();
        public string promo_benefit { get; set; } = null;
        public AppStoreOfferPrice strikethrough_price { get; set; } = new AppStoreOfferPrice();
    }

    public class AppStoreOfferPrice
    {
        public string offset_amount { get; set; } = "0";
        public string currency { get; set; } = "USD";
        public string formatted { get; set; } = "$0.00";
    }
}
