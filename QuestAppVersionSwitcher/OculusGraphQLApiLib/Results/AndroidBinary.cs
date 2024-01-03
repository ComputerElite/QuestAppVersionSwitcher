using System;
using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class AndroidBinary : GraphQLBase
    {
        public Application binary_application { get; set; } = null;
        public string id { get; set; } = "";
        public string version { get; set; } = "";
        public string platform { get; set; } = "";
        public string package_name { get; set; } = null;
        public string file_name { get; set; } = "";
        public string uri { get; set; } = "";
        public string change_log { get { return changeLog; } set { changeLog = value; } }
        public string changeLog { get; set; } = null;
        public string richChangeLog { get; set; } = "";
        public bool firewall_exceptions_required { get; set; } = false;
        public bool is_2d_mode_supported { get; set; } = false;
        public string launch_file { get; set; } = "";
        public string launch_file_2d { get; set; } = null;
        public string launch_parameters { get; set; } = "";
        public string launch_parameters_2d { get; set; } = null;
        public string Platform { get; set; } = "PC";
        public string release_notes_plain_text { get; set; } = "";
        public string required_space { get; set; } = "0";
        public long required_space_numerical { get
            {
                return long.Parse(required_space);
            } }
        public string size { get; set; } = "0";
        public long size_numerical
        {
            get
            {
                return long.Parse(size);
            }
        }
        public string status { get; set; } = "DRAFT";
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// WARNING!!! IN CASE SOMETHING DOESN'T WORK SOMEWHERE ADD binary_application BACK IN AND FIX RECURSION ///
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //public EdgesPrimaryBinaryApplication binary_application { get; set; } = new EdgesPrimaryBinaryApplication();
        public string __isAppBinary { get; set; } = "";
        //public Edges<AssetFile> asset_files { get; set; } = ;
        //public Edges<DebugSymbol> debug_symbols { get; set; } = ;
        public string __isAppBinaryWithFileAsset { get; set; } = "";
        public long version_code { get { return versionCode; } set { versionCode = value; } }
        public long versionCode { get; set; } = 0;
        public long created_date { get; set; } = 0;
        public Nodes<ReleaseChannel> binary_release_channels { get; set; } = null;
        public Edges<Node<AppItemBundle>> lastIapItems { get; set; } = new Edges<Node<AppItemBundle>>();
        public Edges<Node<AppItemBundle>> firstIapItems { get; set; } = new Edges<Node<AppItemBundle>>();
        public Nodes<AssetFile> asset_files { get; set; } = new Nodes<AssetFile>();
        public AssetFile obb_binary { get; set; } = null;

        public override string ToString()
        {
            return "Version: " + version + " (" + id + ")\nChangelog: " + change_log;
        }
    }
}