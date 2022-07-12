using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib
{
    public enum Headset
    {
        INVALID = -1,
        RIFT = 0,
        MONTEREY = 1, // aka quest 1
        HOLLYWOOD = 2, // aka quest 2
        GEARVR = 3,
        PACIFIC = 4, // aka Go
        LAGUNA = 5, // aka Rift S
        SEACLIFF = 6
    }

    public class HeadsetTools
    {
        public static Headset GetHeadsetFromCodeName(string codename)
        {
            switch(codename)
            {
                case "MONTEREY":
                    return Headset.MONTEREY;
                case "RIFT":
                    return Headset.RIFT;
                case "HOLLYWOOD":
                    return Headset.HOLLYWOOD;
                case "GEARVR":
                    return Headset.GEARVR;
                case "PACIFIC":
                    return Headset.PACIFIC;
                case "LAGUNA":
                    return Headset.LAGUNA;
                case "SEACLIFF":
                    return Headset.SEACLIFF;
            }
            return Headset.INVALID;
        }

        public static string GetHeadsetCodeName(Headset headset)
        {
            return Enum.GetName(typeof(Headset), headset);
        }
        public static string GetHeadsetDisplayName(Headset headset)
        {
            switch (headset)
            {
                case Headset.RIFT:
                    return "Rift";
                case Headset.LAGUNA:
                    return "Rift S";
                case Headset.MONTEREY:
                    return "Quest 1";
                case Headset.HOLLYWOOD:
                    return "Quest 2";
                case Headset.GEARVR:
                    return "GearVR";
                case Headset.PACIFIC:
                    return "Go";
                case Headset.SEACLIFF:
                    return "Seacliff";
                default:
                    return "unknown";
            }
        }

        public static string GetHeadsetDisplayNameGeneral(Headset headset)
        {
            switch (headset)
            {
                case Headset.RIFT:
                    return "Rift";
                case Headset.LAGUNA:
                    return "Rift";
                case Headset.MONTEREY:
                    return "Quest";
                case Headset.HOLLYWOOD:
                    return "Quest";
                case Headset.GEARVR:
                    return "GearVR";
                case Headset.PACIFIC:
                    return "Go";
                case Headset.SEACLIFF:
                    return "Seacliff";
                default:
                    return "unknown";
            }
        }

        public static string GetHeadsetInstallActionName(Headset headset)
        {
            switch (headset)
            {
                case Headset.RIFT:
                    return "Launch";
                case Headset.LAGUNA:
                    return "Launch";
                case Headset.MONTEREY:
                    return "Install";
                case Headset.HOLLYWOOD:
                    return "Install";
                case Headset.GEARVR:
                    return "Install";
                case Headset.PACIFIC:
                    return "Install";
                case Headset.SEACLIFF:
                    return "Install";
            }
            return "unknown";
        }
    }
}
