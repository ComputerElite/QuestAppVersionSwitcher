using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Webkit;
using ComputerUtils.Logging;
using Object = Java.Lang.Object;
using System.Collections.Concurrent;

namespace QuestAppVersionSwitcher
{
    public static class ActivityResultCallbackRegistry
    {
        static readonly ConcurrentDictionary<int, Action<Result, Intent>> ActivityResultCallbacks =
            new ConcurrentDictionary<int, Action<Result, Intent>>();

        static int s_nextActivityResultCallbackKey;

        public static void InvokeCallback(int requestCode, Result resultCode, Intent data)
        {
            Action<Result, Intent> callback;

            if (ActivityResultCallbacks.TryGetValue(requestCode, out callback))
            {
                callback(resultCode, data);
            }
        }

        internal static int RegisterActivityResultCallback(Action<Result, Intent> callback)
        {
            int requestCode = s_nextActivityResultCallbackKey;

            while (!ActivityResultCallbacks.TryAdd(requestCode, callback))
            {
                s_nextActivityResultCallbackKey += 1;
                requestCode = s_nextActivityResultCallbackKey;
            }

            s_nextActivityResultCallbackKey += 1;

            return requestCode;
        }

        internal static void UnregisterActivityResultCallback(int requestCode)
        {
            Action<Result, Intent> callback;
            ActivityResultCallbacks.TryRemove(requestCode, out callback);
        }
    }
}