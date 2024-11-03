using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{

    public class XFRProfile
    {
        public XFRProfileTokens xfr_create_profile_token { get; set; } = new XFRProfileTokens();
    }

    public class XFRProfileTokens
    {
        public List<XFRProfileToken> profile_tokens { get; set; } = new List<XFRProfileToken>();
    }

    public class XFRProfileToken
    {
        public string access_token { get; set; } = "";
    }
}