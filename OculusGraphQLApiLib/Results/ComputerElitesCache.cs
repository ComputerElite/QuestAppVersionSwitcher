using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
    public class IndexEntry
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string img { get; set; } = "";
        public string headset { get; set; } = "";
    }

    public class ComputersCacheApplication
    {
        public string appName { get; set; } = "";
        private string internalHeadset { get; set; } = "RIFT";
        public string headset { get { return internalHeadset; } }
        public Headset GetHeadset()
        {
            return (Headset)Enum.Parse(typeof(Headset), headset);
        }
        public void SetHeadset(Headset h)
        {
            internalHeadset = Enum.GetName(typeof(Headset), h);
        }
        public string squareImage { get; set; } = "";
        public string id { get; set; } = "";
        public Edges<Node<AndroidBinary>> binaries { get; set; } = new Edges<Node<AndroidBinary>>();
    }
}
