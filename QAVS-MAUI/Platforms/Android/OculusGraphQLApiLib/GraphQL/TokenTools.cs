using ComputerUtils.Logging;
using System;
using System.Text.RegularExpressions;

namespace OculusGraphQLApiLib
{
    public class TokenTools
    {
        public static bool IsUserTokenValid(string token)
        {
            //yes this is basic
            Logger.Log("Checking if token matches requirements");
            if (token.Contains("%"))
            {
                Logger.Log("Token contains %. Token most likely comes from an uri and won't work");
                Console.WriteLine("You got your token from the wrong place. Go to the payload tab. Don't get it from the url.");
                return false;
            }
            if (!token.StartsWith("FRL"))
            {
                Logger.Log("Token doesn't start with FRL");
                Console.WriteLine("Tokens must start with 'FRL'. Please get a new one");
                return false;
            }
            if (token.Contains("|"))
            {
                Logger.Log("Token contains | which usually indicates an application token which is not valid for user tokens");
                Console.WriteLine("You seem to have entered a token of an application. Please get YOUR token. Usually this can be done by using another request in the network tab.");
                return false;
            }
            return true;
        }
        public static string GetUserTokenErrorMessage(string token)
        {
            //yes this is basic
            Logger.Log("Checking if token matches requirements");
            if (token.Contains("%"))
            {
                Logger.Log("Token contains %. Token most likely comes from an uri and won't work");
                return "You got your token from the wrong place. Go to the payload tab. Don't get it from the url.";
            }
            if (!token.StartsWith("FRL"))
            {
                Logger.Log("Token doesn't start with FRL");
                return "Tokens must start with 'FRL'. Please get a new one";
            }
            if (token.Contains("|"))
            {
                Logger.Log("Token contains | which usually indicates an application token which is not valid for user tokens");
                return "You seem to have entered a token of an application. Please get YOUR token. Usually this can be done by using another request in the network tab.";
            }
            if (Regex.IsMatch(token, "OC[0-9]{15}"))
            {
                Logger.Log("Token matches /OC[0-9}{15}/ which usually indicates a changed oculus store token");
                return "Don't change your token. This will only cause issues. Check another request for the right token.";
            }
            return "";
        }
    }
}