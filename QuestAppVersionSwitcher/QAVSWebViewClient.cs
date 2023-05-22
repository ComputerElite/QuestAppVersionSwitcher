using System.Collections.Generic;
using System.IO;
using Android.Webkit;
using ComputerUtils.Android;
using QuestAppVersionSwitcher.Core;

namespace QuestAppVersionSwitcher
{
    public class QAVSWebViewClient : WebViewClient
    {
        public string injectJsJs = "var tag = document.createElement('script');tag.src = 'http://localhost:" +
                                   CoreService.coreVars.serverPort + "/inject.js';document.head.appendChild(tag)";

        public string injectedJs = "";
        
        // Grab token
        public override void OnPageFinished(WebView view, string url)
        {
            CookieManager.Instance.Flush();
            if(!url.ToLower().Contains("localhost") && !url.ToLower().StartsWith("https://auth.meta.com") && !url.ToLower().Contains("http://127.0.0.1"))
            {
                if (injectedJs == "")
                {
                    // Load qavs_inject.js
                    injectedJs = new StreamReader(AndroidCore.assetManager.Open("html/qavs_inject.js")).ReadToEnd();
                }
                view.EvaluateJavascript(injectedJs.Replace("{0}", CoreService.coreVars.serverPort.ToString()), null);
            }

            if (url.ToLower().StartsWith("https://auth.meta.com/settings"))
            {
                // redirect to oculus page
                view.LoadUrl("https://oculus.com/experiences/quest");
            }
        }

        public static Dictionary<string, string> headers = new Dictionary<string, string>
        {
            ["sec-fetch-mode"] = "navigate",
            ["sec-fetch-site"] = "same-origin",
            ["sec-fetch-dest"] = "document",
            ["sec-ch-ua-platform"] = "\"Linux\"",
            ["sec-ch-ua"] = "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"104\"",
            ["sec-ch-ua-mobile"] = "?0",
            ["sec-fetch-user"] = "?1",
            ["cross-origin-opener-policy"] = "unsafe-none"
        };
        
        

        public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
        {
            return base.ShouldInterceptRequest(view, request);

            foreach (KeyValuePair<string, string> p in headers)
            {
                if(!request.RequestHeaders.ContainsKey(p.Key)) request.RequestHeaders.Add(p.Key, p.Value);
                else request.RequestHeaders[p.Key] = p.Value;
            }
            if (request.RequestHeaders.ContainsKey("document-policy")) request.RequestHeaders.Remove("document-policy");
            if (request.RequestHeaders.ContainsKey("document-domain")) request.RequestHeaders.Remove("document-domain");
            
            string cookie = CookieManager.Instance.GetCookie(request.Url.ToString());
            if (cookie != null) request.RequestHeaders["cookie"] = cookie;
            if (request.Method == "POST")
            {
                request.RequestHeaders["sec-fetch-mode"] = "cors";
                request.RequestHeaders["sec-fetch-dest"] = "empty";
            }
            return base.ShouldInterceptRequest(view, request);
        }

        public Dictionary<string, string> GetHeaders(IDictionary<string, IList<string>> h)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (KeyValuePair<string, IList<string>> p in h)
            {
                if (p.Value.Count > 0)
                {
                    if(p.Key != null) headers.Add(p.Key.ToLower(), p.Value[0]);
                }
            }
            return headers;
        }
        
        /*
        public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
        {
            bool changed = false;
            Logger.Log(request.Url.ToString());
            if (request.RequestHeaders != null)
            {
                foreach (KeyValuePair<string, string> p in request.RequestHeaders)
                {
                    Logger.Log("Header: " + p.Key + " - " + p.Value);
                    request.RequestHeaders[p.Key] = p.Value.Replace("require-trusted-types-for 'script';", "");
                }
            }
            if (request.RequestHeaders != null)
            {
                foreach (KeyValuePair<string, string> p in request.RequestHeaders)
                {
                    if (p.Value.Contains(toRemove))
                    {
                        changed = true;
                        request.RequestHeaders[p.Key] = p.Value.Replace(toRemove, "");
                    }
                }

                if (changed)
                    if (request.Url != null)
                        view.LoadUrl(request.Url.ToString(), request.RequestHeaders);
            }

            return changed;
        }
        */
	}
}