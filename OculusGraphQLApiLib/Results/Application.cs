using System;
using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class Application
    {
        public string appName { get; set; } = "";
        public AppStoreOffer baseline_offer { get; set; } = new AppStoreOffer();
        public string canonicalName { get; set; } = "";
        public OculusUri cover_landscape_image { get; set; } = new OculusUri();
        public OculusUri cover_portrait_image { get; set; } = new OculusUri();
        public OculusUri cover_square_image { get; set; } = new OculusUri();
        public AppStoreOffer current_gift_offer { get; set; } = new AppStoreOffer();
        public AppStoreOffer current_offer { get; set; } = new AppStoreOffer();
        public string displayName { get { return display_name; } set { display_name = value; } }
        public string display_long_description { get; set; } = "";
        public string display_name { get; set; } = "";
        public Edges<Node<Review>> firstQualityRatings { get; set; } = new Edges<Node<Review>>();
        public List<string> genre_names { get; set; } = new List<string>();
        public bool has_in_app_ads { get; set; } = false;
        public string id { get; set; } = "";
        public bool is_approved { get; set; } = false;
        public bool is_concept { get; set; } = false; // aka AppLab
        public bool is_enterprise_enabled { get; set; } = false;
        public Organization organization { get; set; } = new Organization();
        public string platform { get; set; } = "";
        public string publisher_name { get; set; } = "";
        public double quality_rating_aggregate { get; set; } = 0.0;
        public List<QualityRating> quality_rating_history_aggregate_all { get; set; } = new List<QualityRating>();
        public Nodes<ReleaseChannel> release_channels { get; set; } = new Nodes<ReleaseChannel>();
        public long? release_date { get; set; } = 0;
        public Nodes<Revision> revisions { get; set; } = new Nodes<Revision>();
        public List<OculusUri> screenshots { get; set; } = new List<OculusUri>();
        public Edges<Node<AndroidBinary>> supportedBinaries { get; set; } = new Edges<Node<AndroidBinary>>();
        public List<string> supported_hmd_platforms { get; set; } = new List<string>();
        public List<Headset> supported_hmd_platforms_enum
        {
            get
            {
                List<Headset> headsets = new List<Headset>();
                foreach (string s in supported_hmd_platforms)
                {
                    headsets.Add((Headset)Enum.Parse(typeof(Headset), s));
                }
                return headsets;
            }
        }
        public bool viewer_has_preorder { get; set; } = false;
        public string website_url { get; set; } = "";
        public AndroidBinary latest_supported_binary { get; set; } = new AndroidBinary();

        public List<IAPEntitlement> active_dlc_entitlements { get; set; } = new List<IAPEntitlement>();
    }
    public class EdgesPrimaryBinaryApplication : Application
    {
        public Edges<Node<AndroidBinary>> primary_binaries { get; set; } = new Edges<Node<AndroidBinary>>();
    }

    public class NodesPrimaryBinaryApplication : Application
    {
        public Nodes<AndroidBinary> primary_binaries { get; set; } = new Nodes<AndroidBinary>();
    }

    public class OculusUri
    {
        public string uri { get; set; } = "";
    }
}