using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Web;
using ComputerUtils.Android.Logging;
using OculusGraphQLApiLib.Results;

namespace OculusGraphQLApiLib
{
    public class GraphQLClient
    {
        public string uri { get; set; } = "";
        public GraphQLOptions options { get; set; } = new GraphQLOptions();
        public const string oculusUri = "https://graph.oculus.com/graphql";
        public static string oculusStoreToken = "OC|752908224809889|";
        public static string forcedLocale = "";
        public static bool throwException = true;
        public static bool log = true;
        public static int retryTimes = 3;
        public static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public GraphQLClient(string uri, GraphQLOptions options)
        {
            this.uri = uri;
            this.options = options;
        }

        public GraphQLClient(string uri)
        {
            this.uri = uri;
        }

        public GraphQLClient() { }

        public string GetForcedLocale()
        {
            return forcedLocale != "" ? "?forced_locale=" + forcedLocale : "";
        }

        public string Request(GraphQLOptions options)
        {
            WebClient c = new WebClient();
            //c.Headers.Add("x-requested-with", "RiftDowngrader");
            if (log) Logger.Log("Doing POST Request to " + uri + " with args " + options.ToLoggingString());
            try
            {
                string returning = c.UploadString(uri + GetForcedLocale(), "POST", options.ToStringEncoded());
                return returning;
            }
            catch (WebException e)
            {
                if (log) Logger.Log("Request failed (" + e.Status + "): \n" + new StreamReader(e.Response.GetResponseStream()).ReadToEnd(), LoggingType.Error);
                Console.ForegroundColor = ConsoleColor.Red;
                if(log) Console.WriteLine("Request to Oculus failed. Please try again later and/or contact ComputerElite.");
                if(throwException) throw new Exception(e.Status.ToString().StartsWith("4") ? "I fuqed up" : "Some Request to Oculus failed so yeah idk how to handle it.");
            }
            return "{}";
        }

        public string Request(bool asBody = false, Dictionary<string, string> customHeaders = null, int retry = 0, string status = "200")
        {
            if (retry == retryTimes)
            {
                if (log) Logger.Log("Retry limit exceeded. Stopping requests");
                Console.ForegroundColor = ConsoleColor.Red;
                if (log) Console.WriteLine("Request to Oculus failed. Please try again later and/or contact ComputerElite.");
                if (throwException) throw new Exception(status.StartsWith("4") ? "I fuqed up" : "Some Request to Oculus failed so yeah idk how to handle it.");
                return "{}";
            }
            if(log && retry != 0) Logger.Log("Starting retry number " + retry);
            WebClient c = new WebClient();
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    c.Headers.Add(header.Key, header.Value);
                }
            }
            if (log) Logger.Log("Doing POST Request to " + uri + " with args " + options.ToLoggingString());
            try
            {
                string res = "";
                if (asBody) res = c.UploadString(uri + GetForcedLocale(), "POST", options.ToStringEncoded());
                else res = c.UploadString(uri + "?" + this.options.ToString() + GetForcedLocale().Replace("?", "&"), "POST", "");
                if(log) Logger.Log(res);
                return res;
            }
            catch (WebException e)
            {

                if (log) Logger.Log("Request failed, retrying (" + e.Status.ToString() + ", " + (int)e.Status + "): \n" + new StreamReader(e.Response.GetResponseStream()).ReadToEnd(), LoggingType.Error);
                return Request(asBody, customHeaders, retry + 1, e.Status.ToString());
            }
            return "{}";
        }

        public static Data<Application> VersionHistory(string appid)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "1586217024733717";
            c.options.variables = "{\"id\":\"" + appid + "\"}";
            return JsonSerializer.Deserialize<Data<Application>>(c.Request(), jsonOptions);
        }

        public static ViewerData<OculusUserWrapper> GetCurrentUser()
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "4149322231793299";
            c.options.variables = "{}";
            return JsonSerializer.Deserialize<ViewerData<OculusUserWrapper>>(c.Request(), jsonOptions);
        }

        public static Data<AppStoreAllAppsSection> AllApps(Headset headset, string cursor = null, int maxApps = 500)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "3821696797949516";
            string id = "";
            switch(headset)
            {
                case Headset.MONTEREY:
                    id = "1888816384764129";
                    break;
                case Headset.HOLLYWOOD:
                    id = "1888816384764129";
                    break;
                case Headset.RIFT:
                    id = "1736210353282450";
                    break;
                case Headset.LAGUNA:
                    id = "1736210353282450";
                    break;
                case Headset.GEARVR:
                    id = "174868819587665";
                    break;
                case Headset.PACIFIC:
                    id = "174868819587665";
                    break;
            }
            c.options.variables = "{\"sectionId\":\"" + id + "\",\"sortOrder\":null,\"sectionItemCount\":" + maxApps + ",\"sectionCursor\":" + (cursor == null ? "null" : "\"" + cursor + "\"") + ",\"hmdType\":\"" + HeadsetTools.GetHeadsetCodeName(headset) + "\"}";
            return JsonSerializer.Deserialize<Data<AppStoreAllAppsSection>>(c.Request());
        }

        public static Data<NodesPrimaryBinaryApplication> AllVersionsOfApp(string appid) // DONE
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "2885322071572384";
            c.options.variables = "{\"applicationID\":\"" + appid + "\"}";
            return JsonSerializer.Deserialize<Data<NodesPrimaryBinaryApplication>>(c.Request(), jsonOptions);
        }

        public static ViewerData<OculusUserWrapper> GetActiveEntitelments() // DONE
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "4850747515044496";
            c.options.variables = "{}";
            return JsonSerializer.Deserialize<ViewerData<OculusUserWrapper>>(c.Request(), jsonOptions);
        }

        public static Data<EdgesPrimaryBinaryApplication> ReleaseChannelsOfApp(string appid) // DONE
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "3828663700542720";
            c.options.variables = "{\"applicationID\":\"" + appid + "\"}";
            return JsonSerializer.Deserialize<Data<EdgesPrimaryBinaryApplication>>(c.Request(), jsonOptions);
        }

        public static Data<ReleaseChannel> ReleaseChannelReleases(string channelId) // DONE
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "3973666182694273";
            c.options.variables = "{\"releaseChannelID\":\"" + channelId + "\"}";
            return JsonSerializer.Deserialize<Data<ReleaseChannel>>(c.Request(), jsonOptions);
        }

        public static ViewerData<ContextualSearch> StoreSearch(string query, Headset headset) // DONE
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "3928907833885295";
            c.options.variables = "{\"query\":\"" + query + "\",\"hmdType\":\"" + HeadsetTools.GetHeadsetCodeName(headset) + "\",\"firstSearchResultItems\":100}";
            return JsonSerializer.Deserialize<ViewerData<ContextualSearch>>(c.Request(), jsonOptions);
        }

        public static GraphQLClient CurrentVersionOfApp(string appid)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "1586217024733717";
            c.options.variables = "{\"id\":\"" + appid + "\"}";
            return c;
        }

        public static Data<Application> GetAppDetail(string id, Headset headset)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "4282918028433524";
            c.options.variables = "{\"itemId\":\"" + id + "\",\"first\":20,\"last\":null,\"after\":null,\"before\":null,\"forward\":true,\"ordering\":null,\"ratingScores\":null,\"hmdType\":\"" + HeadsetTools.GetHeadsetCodeName(headset) + "\"}";
            return JsonSerializer.Deserialize<Data<Application>>(c.Request(), jsonOptions);
        }

        public static Data<Application> GetDLCs(string appId)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "3853229151363174";
            c.options.variables = "{\"id\":\"" + appId + "\",\"first\":200,\"last\":null,\"after\":null,\"before\":null,\"forward\":true}";
            return JsonSerializer.Deserialize<Data<Application>>(c.Request(), jsonOptions);
        }

        public static Data<AndroidBinary> GetBinaryDetails(string binaryId)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc_id = "4734929166632773";
            c.options.variables = "{\"binaryID\":\"" + binaryId + "\"}";
            return JsonSerializer.Deserialize<Data<AndroidBinary>>(c.Request(), jsonOptions);
        }

        public static PlainData<AppBinaryInfoContainer> GetAssetFiles(string appId, long versionCode)
        {
            GraphQLClient c = OculusTemplate();
            c.options.doc = "query ($params: AppBinaryInfoArgs!) { app_binary_info(args: $params) { info { binary { ... on AndroidBinary { id package_name version_code asset_files { edges { node { ... on AssetFile {  file_name uri size  } } } } } } } }}";
            c.options.variables = "{\"params\":{\"app_params\":[{\"app_id\":\"" + appId + "\",\"version_code\":\"" + versionCode + "\"}]}}";
            return JsonSerializer.Deserialize<PlainData<AppBinaryInfoContainer>>(c.Request(), jsonOptions);
        }

        public static GraphQLClient OculusTemplate()
        {
            GraphQLClient c = new GraphQLClient(oculusUri);
            GraphQLOptions o = new GraphQLOptions();
            o.access_token = oculusStoreToken;
            c.options = o;
            return c;
        }
    }

    public class GraphQLOptions
    {
        public string access_token { get; set; } = "";
        public string variables { get; set; } = "";
        public string doc_id { get; set; } = "";
        public string doc { get; set; } = "";

        public override string ToString()
        {
            return "access_token=" + access_token + "&variables=" + variables + "&doc_id=" + doc_id + "&doc=" + doc;
        }

        public string ToStringEncoded()
        {
            return "access_token=" + Uri.EscapeUriString(access_token) + "&variables=" + Uri.EscapeUriString(variables) + "&doc_id=" + doc_id + "&doc=" + Uri.EscapeUriString(doc);
        }

        public string ToLoggingString()
        {
            return "access_token=aSecret:)&variables=" + variables + "&doc_id=" + doc_id + "&doc=" + doc;
        }
    }}