#if !HS
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KK_Plugins
{
    using LogLevel = BepInEx.Logging.LogLevel;

    public static class TextAssetPatcher
    {
        // Harmony can't patch these getters (empty bodies) so using MonoMod for now
        // if this changes this class could be replaced with postfix patches
        private static bool patched = false;
        private static Func<TextAsset, string> textGetterOrig;
        private static Func<TextAsset, byte[]> bytesGetterOrig;

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        public static void PatchTextAsset()
        {
            if (!patched)
            {
                textGetterOrig = PatchGetter<TextAsset, string>("text", TextGetterNew);
                bytesGetterOrig = PatchGetter<TextAsset, byte[]>("bytes", BytesGetterNew);
                patched = true;
            }
        }

        internal static Func<T, TResult> PatchGetter<T, TResult>(string propertyToPatch, Func<T, TResult> replacementGetter)
        {
            BindingFlags baseFlags = BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo origGetter = typeof(TextAsset).GetProperty(propertyToPatch, baseFlags | BindingFlags.Instance)?.GetGetMethod();
            if (origGetter is null)
            {
                throw new ArgumentException($"Unable to patch { propertyToPatch }. Property not found.");
            }

            NativeDetour detour = new NativeDetour(origGetter, replacementGetter.Method);

            detour.Apply();
            Logger.Log(LogLevel.Debug, $"patched: {origGetter.ReturnType} {nameof(TextAsset)}.{origGetter.Name}()");
            return detour.GenerateTrampoline<Func<T, TResult>>();
        }

        internal static string TextGetterOrig(this TextAsset obj)
        {
            if (patched)
            {
                return textGetterOrig(obj);
            }
            else
            {
                return obj.text;
            }
        }

        internal static string TextGetterNew(this TextAsset obj)
        {
            string result = obj.TextGetterOrig();
            if (patched && TextAssetResourceRedirector.TryLoadReplacement(result, out string replacement))
            {
                return replacement;
            }
            return result;
        }

        internal static byte[] BytesGetterOrig(this TextAsset obj)
        {
            if (patched)
            {
                return bytesGetterOrig(obj);
            }
            else
            {
                return obj.bytes;
            }
        }

        internal static byte[] BytesGetterNew(this TextAsset obj)
        {
            if (patched && TextAssetResourceRedirector.TryLoadReplacement(obj.TextGetterOrig(), out string replacement))
            {
                return Encoding.ASCII.GetBytes(replacement);
            }
            return obj.BytesGetterOrig();
        }
    }
}
#endif