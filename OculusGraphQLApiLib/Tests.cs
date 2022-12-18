using OculusGraphQLApiLib.Results;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace OculusGraphQLApiLib
{
    public class Tests
    {
        public static string appid = "2448060205267927";
        public static string releasechannelid = "703449633385683";
        public static void Start()
        {
            StackTrace stackTrace = new StackTrace();
            Console.WriteLine("Starting " + (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name);
        }

        public static void Stop()
        {
            Console.WriteLine("Test End. Press Enter to continue");
            Console.ReadLine();
        }

        public static void ReleaseChannelReleases()
        {
            Start();
            Console.WriteLine(Serialize<Data<ReleaseChannel>>(GraphQLClient.ReleaseChannelReleases(releasechannelid)));
            Stop();
        }

        public static void DLCs()
        {
            Start();
            Console.WriteLine(Serialize<Data<Application>>(GraphQLClient.GetDLCs(appid)));
            Stop();
        }
        public static void ReleaseChannelsOfApp()
        {
            Start();
            Console.WriteLine(Serialize<Data<EdgesPrimaryBinaryApplication>>(GraphQLClient.ReleaseChannelsOfApp(appid)));
            Stop();
        }
        public static void CurrentVersionOfApp()
        {
            Start();
            Console.WriteLine(GraphQLClient.CurrentVersionOfApp(appid));
            Stop();
        }

        public static void StoreSearch()
        {
            Start();
            Console.WriteLine(Serialize(GraphQLClient.StoreSearch("beat saber", Headset.MONTEREY)));
            Stop();
        }

        public static void VersionHistory()
        {
            Start();
            Console.WriteLine(Serialize( GraphQLClient.VersionHistory(appid)));
            Stop();
        }

        public static string Serialize<T>(T o)
        {
            return JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}