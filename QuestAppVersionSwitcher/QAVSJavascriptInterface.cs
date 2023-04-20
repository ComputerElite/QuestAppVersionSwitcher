using System;
using System.Text.RegularExpressions;
using Android.Runtime;
using Android.Webkit;
using ComputerUtils.Android.Logging;
using Java.Lang;
using QuestAppVersionSwitcher.Core;
using QuestAppVersionSwitcher.Mods;
using Object = Java.Lang.Object;

namespace QuestAppVersionSwitcher
{
    public class QAVSJavascriptInterface : Object
    {

        public static string fileMimeType;

        public static string getBase64StringFromBlobUrl(string blobUrl, string mimeType)
        {
            if (blobUrl.StartsWith("blob"))
            {
                fileMimeType = mimeType;
                return "javascript: var xhr = new XMLHttpRequest();" +
                       "xhr.open('GET', '" + blobUrl + "', true);" +
                       "xhr.setRequestHeader('Content-type','" + mimeType + ";charset=UTF-8');" +
                       "xhr.responseType = 'blob';" +
                       "xhr.onload = function(e) {" +
                       "    if (this.status == 200) {" +
                       "        var blobFile = this.response;" +
                       "        var reader = new FileReader();" +
                       "        reader.readAsDataURL(blobFile);" +
                       "        reader.onloadend = function() {" +
                       "            base64data = reader.result;" +
                       "            fetch('http://127.0.0.1:" + CoreService.coreVars.serverPort + "/api/base64?mime=" + mimeType + "', {method: 'POST', body: base64data})" +
                       "        }" +
                       "    }" +
                       "};" +
                       "xhr.send();";
            }

            return "javascript: console.log('It is not a Blob URL');";
        }
    }
}