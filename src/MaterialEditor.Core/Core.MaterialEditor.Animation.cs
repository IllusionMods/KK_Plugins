using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

namespace KK_Plugins.MaterialEditor
{
    [Serializable]
    [MessagePackObject(false)]
    public class MEAnimationFrame
    {
        [Key(0)]
        public int texID;

        [Key(1)]
        public int beginFrame;

        [Key(2)]
        public int frames;
    }

    [Serializable]
    [MessagePackObject(true)]
    public class MEAnimationDefine
    {
        public int parentTexID;
        public int totalFrames;
        public int framePerSecond;
        public int iterations;
        public float totalTime;
        public MEAnimationFrame[] frames;
    }

    /// <summary>
    /// Material Editor animation controller. Used at runtime.
    /// This class is not saved to a file.
    /// </summary>
    public class MEAnimationController<Controller, Property>
    {
        static public System.Func<Property, int?> GetTexID = null;
        static public System.Action<Controller, GameObject, Property, int> UpdateTexture = null;

        public Controller parent;
        public MEAnimationDefine def;
        public GameObject go;
        public float playTime;
        public int curTexID = -1;

        public MEAnimationController( Controller parent, GameObject go, MEAnimationDefine def)
        {
            this.parent = parent;
            this.def = def;
            this.go = go;

            Reset(def);
        }

        public void Reset( MEAnimationDefine newAnime )
        {
            def = newAnime;
            playTime = 0f;
        }

        /// <summary>
        /// Let Time.deltaTime seconds elapse and set the texture.
        /// </summary>
        /// <param name="controllerMap"></param>
        public static void UpdateAnimations( Dictionary<Property,MEAnimationController<Controller, Property>> controllerMap )
        {
            List<Property> removed = new List<Property>();
            float dt = Time.deltaTime;

            foreach( var pair in controllerMap )
            {
                Property prop = pair.Key;
                MEAnimationController<Controller, Property> controller = pair.Value;

                if( controller.def.parentTexID == GetTexID(prop) )
                {
                    if( controller.go != null )
                       controller.UpdateAnimation(prop, dt);
                }
                else
                {
                    //A non-animated texture is set. Animation is finished.
                    removed.Add(prop);
                }
            }

            foreach (var key in removed)
                controllerMap.Remove(key);
        }

        public void UpdateAnimation( Property property, float dt = 0f )
        {
            float time = playTime;
            float totalTime = def.totalTime;
            time += dt;

            if (!float.IsInfinity(time) && !float.IsNaN(time))
            {
                while (time >= totalTime)
                    time -= totalTime;
            }
            else
            {
                time = 0f;
            }
            
            playTime = time;

            int frameCount = Mathf.FloorToInt(time * def.framePerSecond);

            var frames = def.frames;
            int left = 0, right = frames.Length - 1;

            while (left < right)
            {
                int mid = (left + right) >> 1;
                if (frames[mid].beginFrame + frames[mid].frames < frameCount)
                    left = mid + 1;
                else
                    right = mid;
            }

            var texID = frames[left].texID;

            if( texID != curTexID )
            {
                UpdateTexture(parent, go, property, texID);
                curTexID = texID;
            }
        }

        /// <summary>
        /// Get the set of TextureIDs used from animation and texture properties
        /// </summary>
        /// <returns></returns>
        public static HashSet<int> GetUsedTexIDSet(Dictionary<Property, MEAnimationController<Controller, Property>> controllerMap, IList<Property> usedProperties)
        {
            HashSet<int> used = new HashSet<int>();

            foreach( var prop in usedProperties )
            {
                var texID = GetTexID(prop);
                if (texID.HasValue)
                    used.Add(texID.Value);

                if( controllerMap.TryGetValue(prop, out var controller) )
                {
                    foreach (var frame in controller.def.frames)
                        used.Add(frame.texID);
                }
            }

            return used;
        }
    }

    public class MEAnimationUtil
    {
        /// <summary>
        /// Loads animation definition from a gif/apng byte array.
        /// If byte array is not gif/apng, null is returned.
        /// </summary>
        public static MEAnimationDefine LoadAnimationDefFromBytes( int texID, byte[] bytes, System.Func<byte[], int> setAndGetTextureID)
        {
            if (bytes == null)
                return null;

            return
                LoadAnimationDefFromApngBytes(texID, bytes, setAndGetTextureID) ??
                LoadAnimationDefFromGifBytes(texID, bytes, setAndGetTextureID);
        }

        private static MEAnimationDefine LoadAnimationDefFromGifBytes( int texID, byte[] bytes, System.Func<byte[], int> setAndGetTextureID)
        {
            int totalFrames = 0;
            List<MEAnimationFrame> animationFrames = new List<MEAnimationFrame>();
            List<byte[]> framePngs = new List<byte[]>();

            try
            {
                using (var gif = new MG.GIF.Decoder(bytes))
                {
                    var image = gif.NextImage();

                    while ( image != null )
                    {
                        MEAnimationFrame meaf = new MEAnimationFrame();
                        meaf.frames = image.Delay;
                        meaf.beginFrame = totalFrames;
                        totalFrames += image.Delay;

                        Texture2D tex = null;

                        try
                        {
                            tex = image.CreateTexture();
                            framePngs.Add(tex.EncodeToPNG());
                        }
                        finally
                        {
                            if(tex != null)
                                Texture2D.Destroy(tex);
                        }
                        
                        animationFrames.Add(meaf);
                        image = gif.NextImage();
                    }

                    //MaterialEditorPlugin.Logger.LogMessage($"Load {animationFrames.Count} frames of animation.");
                }
            }
            catch
            {
                return null;
            }

            if (animationFrames.Count == 0)
                return null;

            for( int i = 0; i < animationFrames.Count; ++i )
                animationFrames[i].texID = setAndGetTextureID(framePngs[i]);

            const int ticks = 1000;   //constant. mgGif spec.

            MEAnimationDefine animation = new MEAnimationDefine();
            animation.frames = animationFrames.ToArray();
            animation.framePerSecond = ticks;
            animation.totalFrames = totalFrames;
            animation.totalTime = (float)totalFrames / ticks;
            animation.parentTexID = texID;

            return animation;
        }

        private static MEAnimationDefine LoadAnimationDefFromApngBytes( int texID, byte[] bytes, System.Func<byte[], int> setAndGetTextureID)
        {
            int ticks = 1;
            int totalFrames = 0;
            List<MEAnimationFrame> animationFrames = new List<MEAnimationFrame>();

            Texture2D tex = null;
            Texture2D add = null;

            try
            {
                var apng = new LibAPNG.APNG(bytes);

                if (apng.Frames.Length <= 1)
                    return null;

                //MaterialEditorPlugin.Logger.LogMessage($"Load {apng.Frames.Length} frames of animation.");

                tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                add = new Texture2D(1, 1, TextureFormat.ARGB32, false);

                int width = apng.IHDRChunk.Width;
                int height = apng.IHDRChunk.Height;
                Color32[] pixels = new Color32[width * height];
                tex.Resize(width, height, TextureFormat.ARGB32, false);

                ticks = LCM(apng.Frames.Select(frame => frame.fcTLChunk.DelayDen == 0 ? 100 : frame.fcTLChunk.DelayDen));
                
                foreach ( var frame in apng.Frames )
                {
                    var fctl = frame.fcTLChunk;

                    ushort delayNum = fctl.DelayNum;
                    ushort delayDen = fctl.DelayDen;
                    if (delayDen == 0) delayDen = 100;
                    int delay = ticks * delayNum / delayDen;

                    if(delay == 0)
                    {
                        /* https://wiki.mozilla.org/APNG_Specification
                         * The `delay_num` and `delay_den` parameters together specify a fraction indicating the time to display the current frame, in seconds. 
                         * If the denominator is 0, it is to be treated as if it were 100 (that is, `delay_num` then specifies 1/100ths of a second). If the the value of the numerator is 0 the decoder should render the next frame as quickly as possible, though viewers may impose a reasonable lower bound.
                         */
                        delay = Mathf.Max(ticks / 100, 1);
                    }

                    MEAnimationFrame meaf = new MEAnimationFrame();
                    meaf.frames = delay;
                    meaf.beginFrame = totalFrames;
                    totalFrames += delay;

                    add.LoadImage(frame.GetStream().ToArray());
                    add.Apply();
                    var addPixels = add.GetPixels32();

                    Color32[] previous = null;

                    uint frameWidth = fctl.Width;
                    uint frameHeight = fctl.Height;
                    uint offsetX = fctl.XOffset;
                    uint offsetY = (uint)height - (fctl.YOffset + frameHeight);

                    if (fctl.DisposeOp == LibAPNG.DisposeOps.APNGDisposeOpPrevious)
                    {
                        previous = new Color32[frameWidth * frameHeight];
                    }

                    switch (fctl.BlendOp)
                    {
                        case LibAPNG.BlendOps.APNGBlendOpSource:
                            {
                                for (uint y = 0, offset = 0; y < frameHeight; ++y)
                                {
                                    for (uint x = 0; x < frameWidth; ++x)
                                    {
                                        if (previous != null)
                                            previous[offset] = pixels[(y + offsetY) * width + x + offsetX];
                                        pixels[(y + offsetY) * width + x + offsetX] = addPixels[offset++];
                                    }
                                }
                                break;
                            }

                        case LibAPNG.BlendOps.APNGBlendOpOver:
                            {
                                for (uint y = 0, offset = 0; y < frameHeight; ++y)
                                {
                                    for (uint x = 0; x < frameWidth; ++x)
                                    {
                                        long offset0 = (y + offsetY) * width + x + offsetX;
                                        if(previous != null)
                                            previous[offset] = pixels[offset0];
                                        Color32 p = pixels[offset0];
                                        Color32 q = addPixels[offset++];

                                        int s = 255 - q.a;
                                        int t = q.a;

                                        int r = (p.r * s + q.r * t) / 255;
                                        int g = (p.g * s + q.g * t) / 255;
                                        int b = (p.b * s + q.b * t) / 255;
                                        int a = (p.a * s + q.a * t) / 255;

                                        pixels[offset0] = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
                                    }
                                }

                                break;
                            }
                    }

                    tex.SetPixels32(pixels);
                    tex.Apply();

                    meaf.texID = setAndGetTextureID(tex.EncodeToPNG());
                    animationFrames.Add(meaf);
                    
                    switch (fctl.DisposeOp)
                    {
                        case LibAPNG.DisposeOps.APNGDisposeOpNone:
                            //Nothing
                            break;

                        case LibAPNG.DisposeOps.APNGDisposeOpBackground:
                            //Clear black
                            Color32 clear = new Color32(0, 0, 0, 0);
                            for (uint y = 0; y < frameHeight; ++y)
                            {
                                for (uint x = 0; x < frameWidth; ++x)
                                {
                                    long offset0 = (y + offsetY) * width + x + offsetX;
                                    pixels[offset0] = clear;
                                }
                            }
                            break;

                        case LibAPNG.DisposeOps.APNGDisposeOpPrevious:
                            //Prev frame
                            for (uint y = 0, offset = 0; y < frameHeight; ++y)
                            {
                                for (uint x = 0; x < frameWidth; ++x)
                                {
                                    long offset0 = (y + offsetY) * width + x + offsetX;
                                    pixels[offset0] = previous[offset++];
                                }
                            }
                            break;
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if( tex != null )
                    Texture2D.Destroy(tex);

                if( add != null )
                    Texture2D.Destroy(add);
            }

            if (totalFrames == 0)
                return null;

            MEAnimationDefine animation = new MEAnimationDefine();
            animation.parentTexID = texID;
            animation.frames = animationFrames.ToArray();
            animation.framePerSecond = ticks;
            animation.totalFrames = totalFrames;
            animation.totalTime = (float)totalFrames / ticks;

            return animation;
        }

        /// <summary>
        /// Remaps TextureID contained in the MEAnimationDefine
        /// </summary>
        public static void RemapTexID(MEAnimationDefine def, Dictionary<int,int> texIDRemapping )
        {
            if (def == null)
                return;

            int newTexID;
            if (texIDRemapping.TryGetValue(def.parentTexID, out newTexID))
                def.parentTexID = newTexID;

            foreach( var frame in def.frames )
            {
                if (texIDRemapping.TryGetValue(frame.texID, out newTexID))
                    frame.texID = newTexID;
            }
        }

        /// <summary>
        /// Purge unused texture properties from animation dictionary
        /// </summary>
        public static void PurgeUnusedAnimation<Controller, Property>(Dictionary<Property, MEAnimationController<Controller, Property>> controllerMap, IList<Property> usedProperties )
        {
            if (controllerMap == null || usedProperties == null || controllerMap.Count <= 0)
                return;

            HashSet<Property> unuseds = new HashSet<Property>(controllerMap.Keys);
            foreach (var used in usedProperties)
                unuseds.Remove(used);

            foreach( var prop in unuseds )
                controllerMap.Remove(prop);
        }

        /// <summary>
        /// Least Common Multiple
        /// </summary>
        private static int LCM( IEnumerable<int> values )
        {
            long x = 1;
            foreach (var y in values)
                x = x * y / GCD(x, y);
            return (int)x;
        }

        /// <summary>
        /// Greatest Common Divisor
        /// </summary>
        private static long GCD(long a, long b)
        {
            while (b != 0)
            {
                long t = a % b;
                a = b;
                b = t;
            }
            return a;
        }
    }
}
