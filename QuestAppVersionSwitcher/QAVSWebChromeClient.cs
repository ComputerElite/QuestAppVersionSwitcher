using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Webkit;
using ComputerUtils.Android.Logging;
using Object = Java.Lang.Object;

namespace QuestAppVersionSwitcher
{

    // Inspired by maui
    // https://github.com/dotnet/maui/blob/7be95d4e895af0f225f63c8a65fb04f2528e5fa3/src/Core/src/Platform/Android/MauiWebChromeClient.cs
    public class QAVSWebChromeClient : WebChromeClient
    {
        Activity activity;
        public QAVSWebChromeClient(Activity activity)
        {
            this.activity = activity;
        }
        
        
        
        public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
        {
            Logger.Log("WConsole: " + consoleMessage.Message() + " at " + consoleMessage.SourceId().Split('?')[0] + ":" + consoleMessage.LineNumber());
            return true;
        }
        
        WeakReference<Activity> _activityRef;
        List<int> _requestCodes;
        
        public override bool OnShowFileChooser(WebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
        {
            base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
            Logger.Log("Show File chooser called");
            return ChooseFile(filePathCallback, fileChooserParams.CreateIntent(), fileChooserParams.Title);
        }
        
        public void UnregisterCallbacks()
        {
            if (_requestCodes == null || _requestCodes.Count == 0 || !_activityRef.TryGetTarget(out Activity _))
                return;

            foreach (int requestCode in _requestCodes)
            {
                ActivityResultCallbackRegistry.UnregisterActivityResultCallback(requestCode);
            }

            _requestCodes = null;
        }

        protected bool ChooseFile(IValueCallback filePathCallback, Intent intent, string title)
        {

            Action<Result, Intent> callback = (resultCode, intentData) =>
            {
                if (filePathCallback == null)
                    return;

                Object result = ParseResult(resultCode, intentData);
                filePathCallback.OnReceiveValue(result);
            };

            _requestCodes ??= new List<int>();

            int newRequestCode = ActivityResultCallbackRegistry.RegisterActivityResultCallback(callback);

            _requestCodes.Add(newRequestCode);
            Intent i = Intent.CreateChooser(intent, title);
            Logger.Log("Starting activity for result");
            activity.StartActivityForResult(i, newRequestCode);

            return true;
        }
        protected virtual Object ParseResult(Result resultCode, Intent data)
        {

            return FileChooserParams.ParseResult((int)resultCode, data);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UnregisterCallbacks();
            
            base.Dispose(disposing);
        }
        
        internal void Disconnect()
        {
            UnregisterCallbacks();
            _activityRef = null;
        }
    }
}