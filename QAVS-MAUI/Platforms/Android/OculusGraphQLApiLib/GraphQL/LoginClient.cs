using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AndroidX.Emoji2.Text.FlatBuffer;
using ComputerUtils.Logging;
using Microsoft.Win32;
using OculusGraphQLApiLib.Results;

namespace OculusGraphQLApiLib.GraphQL
{

    public class LoginClient
    {
        public string etoken { get; set; } = "";
        public string token { get; set; } = "";
        public string blob { get; set; } = "";
        public const string webviewTokensQuery = "https://meta.graph.meta.com/webview_tokens_query";
        public const string webviewTokensDecrypt = "https://meta.graph.meta.com/webview_blobs_decrypt";
        public const string frlToken = "https://meta.graph.meta.com/graphql";

        public const string authenticate =
            "https://graph.oculus.com/authenticate_application?app_id=1481000308606657&access_token=";

      


        /// <summary>
        /// Starts the login process
        /// </summary>
        /// <returns></returns>
        public LoginResponse StartLogin()
        {
            //RegisterUri();
            LoginPayload payload = new LoginPayload(GraphQLClient.oculusClientToken);
            // post payload to webviewTokensQuery url with httpclient
            string response = DoPostRequest(webviewTokensQuery, JsonSerializer.Serialize(payload));
            // parse response
            LoginResponse loginResponse = JsonSerializer.Deserialize<LoginResponse>(response);
            // set etoken
            etoken = loginResponse.native_sso_etoken;
            token = loginResponse.native_sso_token;
            Logger.Log("Got tokens for login", "Login");
            // return url
            return loginResponse;
        }

        public string GetToken()
        {
            string response = DoPostRequest(webviewTokensDecrypt, JsonSerializer.Serialize(new LoginDecryptPayload
            {
                access_token = GraphQLClient.oculusClientToken,
                blob = blob,
                request_token = token
            }));
            string firstToken = JsonSerializer.Deserialize<LoginDecryptResponse>(response).access_token;
            GraphQLClient c = GraphQLClient.OculusTemplate();
            c.options.access_token = firstToken;
            c.options.doc_id = "5787825127910775";
            c.options.variables = "{\"app_id\":\"1582076955407037\"}";
            Logger.Log("Got user token, requesting correct token", "Login");
            response = DoPostRequest(frlToken, JsonSerializer.Serialize(c.options));
            PlainData<XFRProfile> p = JsonSerializer.Deserialize<PlainData<XFRProfile>>(response);
            Logger.Log("Got user scoped token, getting quest scoped token next, credit to kaitlyn", "Login");
            string secondToken = p.data.xfr_create_profile_token.profile_tokens[0].access_token;
            string authenticateResponse = DoPostRequest(authenticate + secondToken, "");
            LoginDecryptResponse loginDecryptResponse = JsonSerializer.Deserialize<LoginDecryptResponse>(authenticateResponse);
            Logger.Log("Got scoped quest token, returning", "Login");
            return loginDecryptResponse.access_token;
        }

        public string DoPostRequest(string uri, string requestBody)
        {
            string responseString = "";
            //Logger.Log("Request to " + uri);
            //Logger.Log("body: " + requestBody);
            using (HttpClient client = new HttpClient())
            {
                // Create StringContent with the request body and specify the media type
                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                try
                {
                    // Send the POST request
                    HttpResponseMessage response = client.PostAsync(uri, content).Result;

                    // Check if the request was successful (status code 200-299)
                    //Logger.Log(response.Content.Headers.ContentType.CharSet);
                    if(response.Content.Headers.ContentType.CharSet != "\"utf-8\"") responseString = response.Content.ReadAsStringAsync().Result;
                    else responseString = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
                    //Logger.Log(response.StatusCode.ToString());
                    //Logger.Log("response: " + responseString);
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and display the response content as a string
                    }
                    else
                    {
                        // If the request was not successful, display the status code
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions, such as network issues
                    Logger.Log("Exception while sending request to " + uri + ": " + ex);
                }
            }

            return responseString;
        }

        public string UriCallback(string uri)
        {
            Logger.Log("Received uri callback, extracting info", "Login");
            string[] parameters = uri.Replace("oculus://", "").Split('?')[1].Split('&');
            string token = parameters[0].Split('=')[1];
            string blob = parameters[1].Split('=')[1];
            //Console.WriteLine("token: " + token);
            //Console.WriteLine("blob: " + blob);
            this.blob = blob;
            //this.token = token;
            return GetToken();
        }
    }

    public class LoginApproveRequest
    {
        public string native_app_id { get; set; } = "";
        public string native_sso_etoken { get; set; } = "";
    }

    public class LoginApproveResponse
    {
        public LoginApproveResponsePayload payload { get; set; } = new LoginApproveResponsePayload();
    }

    public class LoginApproveResponsePayload
    {
        public string uri { get; set; } = "";
    }

    public class LoginPayload
    {
        public string access_token { get; set; } = "";

        public LoginPayload(string clientToken)
        {
            access_token = clientToken;
        }
    }

    public class LoginDecryptPayload
    {
        public string access_token { get; set; } = "";
        public string blob { get; set; } = "";
        public string request_token { get; set; } = "";
    }

    public class LoginDecryptResponse
    {
        public string access_token { get; set; } = "";
    }

    public class LoginResponse
    {
        public string native_sso_etoken { get; set; } = "";
        public string native_sso_token { get; set; } = "";

        public string url
        {
            get
            {
                return "https://auth.meta.com/native_sso/confirm?native_app_id=512466987071624&native_sso_etoken=" +
                       native_sso_etoken +
                       "&utm_source=skyline_splash";
            }
        }
    }
}