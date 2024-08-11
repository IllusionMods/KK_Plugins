using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    public partial class MaterialEditorPluginBase : BaseUnityPlugin
    {
        /// <summary>
        /// Properties that are normal maps and will have their textures converted
        /// </summary>
        public static readonly List<string> NormalMapProperties = new List<string>();

        /// <summary>
        /// Dictionary of textures and textures converted to normal maps.
        /// </summary>
        WeakKeyDictionary<Texture, WeakReference> _convertedNormalMap = new WeakKeyDictionary<Texture, WeakReference>();

        void Update()
        {
            //Sweep dead values
            _convertedNormalMap.SweepDeadValuesLittle();
        }

        /// <summary>
        /// Convert a normal map texture from grey to red by setting the entire red color channel to white
        /// </summary>
        /// <param name="tex">Texture to convert</param>
        /// <param name="propertyName">Name of the property. Checks against the NormalMapProperties list and will not convert unless it matches.</param>
        /// <returns>True if the texture was converted</returns>
        public bool ConvertNormalMap(ref Texture tex, string propertyName)
        {
            try
            {
                if (!NormalMapProperties.Any(x => propertyName.Contains(x)))
                    return false;

                if (tex == null || IsBrokenTexture(tex))
                    return false;

                Texture normalTex;

                if (_convertedNormalMap.TryGetValue(tex, out var normalMapRef))
                {
                    //It was a texture that had been converted in the past.
                    if (normalMapRef != null)
                    {
                        normalTex = (Texture)normalMapRef.Target;

                        if (normalTex != null)
                        {
                            tex = normalTex;
                            return true;
                        }
                    }
                    else
                    {
                        // Unsupported textures
                        return false;
                    }
                }

                //Never converted or had been converted but was deleted.
                normalTex = ConvertNormalMap(tex);

                if (normalTex == null)
                {
                    // Unsupported textures
                    _convertedNormalMap[tex] = null;
                    return false;
                }

                var weakref = new WeakReference(normalTex);
                _convertedNormalMap[tex] = weakref;         //If the same conversion comes in, return this texture.
                _convertedNormalMap[normalTex] = weakref;   //If a converted texture comes in, return this texture.

                tex = normalTex;
                return true;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// Determine if the texture has been broken.
        /// 
        /// An object that is not a texture is set as a texture property.
        /// zipmod is broken.
        /// </summary>
        static bool IsBrokenTexture( Texture tex )
        {
            //This check does not work when dealing with corrupted Objects (a different object type is stored as Texture, which causes a crash on native side)
            //return !(tex is Texture);

            //If it is not a texture, return true.
            return !tex.GetType().IsSubclassOf(typeof(Texture));
        }

        /// <summary>
        /// Convert a normal map texture from grey to red by setting the entire red color channel to white
        /// </summary>
        /// </summary>
        /// <param name="src">Texture to convert</param>
        /// <returns>Converted Texture</returns>
        protected virtual Texture ConvertNormalMap( Texture src )
        {
            Texture2D readableTex = MakeTextureReadable(src);

            try
            {
                Color[] c = readableTex.GetPixels(0);
                //No conversion needed, but let it still cache the result
                if (IsUncompressedNormalMap(c[0])) return src;
                if (c[0].r != 1f) //Sample one pixel and don't covert normal maps that are already red
                {
                    //Set the entire red color channel to white
                    for (int k = 0; k < c.Length; k++)
                        c[k].r = 1;

                    readableTex.SetPixels(c, 0);
                    readableTex.Apply(true);

                    RenderTexture rt = new RenderTexture(readableTex.width, readableTex.height, 0);
                    rt.useMipMap = true;

                    var cur = RenderTexture.active;
                    RenderTexture.active = rt;
                    Graphics.Blit(readableTex, rt);
                    RenderTexture.active = cur;

                    return rt;
                }

                return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(readableTex);
            }
        }

        private static Texture2D MakeTextureReadable(Texture tex, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default)
        {
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, rtf, cs);
            var currentActiveRT = RenderTexture.active;

            try
            {
                RenderTexture.active = tmp;
                GL.Clear(false, true, new Color(0, 0, 0, 0));
                Graphics.Blit(tex, tmp);
                Texture2D tex2d = GetT2D(tmp);
                tex2d.Apply(true);
                return tex2d;
            }
            finally
            {
                RenderTexture.active = currentActiveRT;
                RenderTexture.ReleaseTemporary(tmp);
            }
        }

        internal static bool IsUncompressedNormalMap(Texture tex)
        {
            Texture2D readableTex = MakeTextureReadable(tex);
            try
            {
                return IsUncompressedNormalMap(readableTex.GetPixel(0, 0));
            }
            finally
            {
                UnityEngine.Object.Destroy(readableTex);
            }
        }

        internal static bool IsUncompressedNormalMap(Color color)
        {
            return Approximately(color.a, 1);
        }

        internal static bool Approximately(float a, float b)
        {
            const float e = 1e-05f * 8f;
            var x = a - b;
            return !(x >= 0 ? x > e : -x > e);

        }
    }


    /// <summary>
    /// Class that behaves like a Dictonary[TKey,TValue].
    /// Keys are held by weak reference; if a key is deleted by the GC, the Key-Value of that key is deleted.
    /// </summary>
    internal class WeakKeyDictionary<TKey, TValue> where TKey : class
    {
        // Practically the same as Dictionary<WeakKey<TKey>, TValue>()
        // Allows searches to both WeakKey/Key by making the key an object.
        private readonly Dictionary<object, TValue> _dict = new Dictionary<object, TValue>(new WeakKeyComperer<object>());
        private readonly List<WeakKey<TKey>> _weakKeys = new List<WeakKey<TKey>>();
        private int _sweepOffset = 0;

        /// <summary>
        /// Sweeps dead values deleted by the GC from the dictionary.
        /// 
        /// This is an assumption that is regularly called from Update() and other functions.
        /// Clean only to the extent that it does not overload the system.
        /// </summary>
        public void SweepDeadValuesLittle()
        {
            if (_sweepOffset >= _weakKeys.Count)
                _sweepOffset = 0;

            int begin = _sweepOffset;
            int end = Mathf.Min(_sweepOffset + 256, _weakKeys.Count);

            while (begin < end)
            {
                var weakKey = _weakKeys[begin];

                if (weakKey.IsAlive)
                {
                    ++begin;
                    continue;
                }

                //Delete from dictionaries and lists
                int lastIndex = _weakKeys.Count - 1;
                _dict.Remove(weakKey);
                _weakKeys[begin] = _weakKeys[lastIndex];
                _weakKeys.RemoveAt(lastIndex);
                --end;
            }

            _sweepOffset = end;
        }

        public void Add(TKey key, TValue value)
        {
            var weak = new WeakKey<TKey>(key);
            _dict.Add(weak, value);
            _weakKeys.Add(weak);
        }

        public bool Remove(TKey key)
        {
            return _dict.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                return _dict[key];
            }

            set
            {
                if( _dict.ContainsKey(key) )
                {
                    _dict[key] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for( int i = 0; i < _weakKeys.Count; )
            {
                var weakKey = _weakKeys[i];
                var key = weakKey.Target;

                if( key != null )
                {
                    yield return new KeyValuePair<TKey, TValue>(key, _dict[key]);
                    ++i;
                }
                else
                {
                    //Delete from dictionaries and lists
                    int lastIndex = _weakKeys.Count - 1;
                    _dict.Remove(weakKey);
                    _weakKeys[i] = _weakKeys[lastIndex];
                    _weakKeys.RemoveAt(lastIndex);
                }
            }
        }
    }

    /// <summary>
    /// A class that behaves as a dictionary key.
    /// Keys are held by weak reference.
    /// </summary>
    internal class WeakKey<TKey> where TKey : class
    {
        private System.WeakReference _ref;
        private int _hash;

        public WeakKey(TKey key)
        {
            if (key == null)
                throw new System.ArgumentNullException(nameof(key));

            _ref = new WeakReference(key);
            _hash = key.GetHashCode();
        }

        public bool IsAlive => _ref.IsAlive;

        public TKey Target => (TKey)_ref.Target;

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetHashCode() != _hash)
                return false;

            if (obj is TKey)
            {
                return _ref.Target == obj;
            }
            else if (obj is WeakKey<TKey>)
            {
                var other = (WeakKey<TKey>)obj;

                var selfKey = _ref.Target;
                var otherKey = other._ref.Target;

                var isAlive = selfKey != null;
                var isOtherAlive = otherKey != null;

                if (isAlive != isOtherAlive)
                    return false;          //Alive and dead values are not equal.

                if (isAlive)
                {
                    //Both are alive and are determined by Target.
                    return selfKey == otherKey;
                }

                //Both dead.
                return System.Object.ReferenceEquals(this, obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }

    internal class WeakKeyComperer<TKey> : IEqualityComparer<object> where TKey : class
    {
        /// <summary>
        /// Determine whether the objects are the same.
        /// Accepts TKey and WeakKey[TKey] as arguments.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public new bool Equals(object x, object y)
        {
            if (y is WeakKey<TKey>)
                return y.Equals(x);

            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
    }
}
