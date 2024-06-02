//https://github.com/octo-code/webp-unity3d
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

#pragma warning disable 1591

namespace WebP.Extern
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPIDecoder { }

    public enum WEBP_CSP_MODE
    {

        /// MODE_RGB -> 0
        MODE_RGB = 0,

        /// MODE_RGBA -> 1
        MODE_RGBA = 1,

        /// MODE_BGR -> 2
        MODE_BGR = 2,

        /// MODE_BGRA -> 3
        MODE_BGRA = 3,

        /// MODE_ARGB -> 4
        MODE_ARGB = 4,

        /// MODE_RGBA_4444 -> 5
        MODE_RGBA_4444 = 5,

        /// MODE_RGB_565 -> 6
        MODE_RGB_565 = 6,

        /// MODE_rgbA -> 7
        MODE_rgbA = 7,

        /// MODE_bgrA -> 8
        MODE_bgrA = 8,

        /// MODE_Argb -> 9
        MODE_Argb = 9,

        /// MODE_rgbA_4444 -> 10
        MODE_rgbA_4444 = 10,

        /// MODE_YUV -> 11
        MODE_YUV = 11,

        /// MODE_YUVA -> 12
        MODE_YUVA = 12,

        /// MODE_LAST -> 13
        MODE_LAST = 13,
    }

    //------------------------------------------------------------------------------
    // WebPDecBuffer: Generic structure for describing the output sample buffer.

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPRGBABuffer
    {

        /// uint8_t*
        public IntPtr rgba;

        /// int
        public int stride;

        /// size_t->unsigned int
        public UIntPtr size;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPYUVABuffer
    {

        /// uint8_t*
        public IntPtr y;

        /// uint8_t*
        public IntPtr u;

        /// uint8_t*
        public IntPtr v;

        /// uint8_t*
        public IntPtr a;

        /// int
        public int y_stride;

        /// int
        public int u_stride;

        /// int
        public int v_stride;

        /// int
        public int a_stride;

        /// size_t->unsigned int
        public UIntPtr y_size;

        /// size_t->unsigned int
        public UIntPtr u_size;

        /// size_t->unsigned int
        public UIntPtr v_size;

        /// size_t->unsigned int
        public UIntPtr a_size;
    }

    [StructLayoutAttribute(LayoutKind.Explicit)]
    public struct Anonymous_690ed5ec_4c3d_40c6_9bd0_0747b5a28b54
    {

        /// WebPRGBABuffer->Anonymous_47cdec86_3c1a_4b39_ab93_76bc7499076a
        [FieldOffsetAttribute(0)]
        public WebPRGBABuffer RGBA;

        /// WebPYUVABuffer->Anonymous_70de6e8e_c3ae_4506_bef0_c17f17a7e678
        [FieldOffsetAttribute(0)]
        public WebPYUVABuffer YUVA;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPDecBuffer
    {

        /// WEBP_CSP_MODE->Anonymous_cb136f5b_1d5d_49a0_aca4_656a79e9d159
        public WEBP_CSP_MODE colorspace;

        /// int
        public int width;

        /// int
        public int height;

        /// int
        public int is_external_memory;

        /// Anonymous_690ed5ec_4c3d_40c6_9bd0_0747b5a28b54
        public Anonymous_690ed5ec_4c3d_40c6_9bd0_0747b5a28b54 u;

        /// uint32_t[4]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;

        /// uint8_t*
        public IntPtr private_memory;
    }


    //------------------------------------------------------------------------------
    // Enumeration of the status codes

    public enum VP8StatusCode
    {

        /// VP8_STATUS_OK -> 0
        VP8_STATUS_OK = 0,

        VP8_STATUS_OUT_OF_MEMORY,

        VP8_STATUS_INVALID_PARAM,

        VP8_STATUS_BITSTREAM_ERROR,

        VP8_STATUS_UNSUPPORTED_FEATURE,

        VP8_STATUS_SUSPENDED,

        VP8_STATUS_USER_ABORT,

        VP8_STATUS_NOT_ENOUGH_DATA,
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPBitstreamFeatures
    {

        /// <summary>
        /// Width in pixels, as read from the bitstream
        /// </summary>
        public int width;

        /// <summary>
        /// Height in pixels, as read from the bitstream.
        /// </summary>
        public int height;

        /// <summary>
        /// // True if the bitstream contains an alpha channel.
        /// </summary>
        public int has_alpha;

        /// <summary>
        /// Unused for now - should be 0
        /// </summary>
        public int bitstream_version;

        /// <summary>
        /// If true, incremental decoding is not reccomended
        /// </summary>
        public int no_incremental_decoding;

        /// <summary>
        /// Unused, should be 0 for now
        /// </summary>
        public int rotate;

        /// <summary>
        /// Unused, should be 0 for now
        /// </summary>
        public int uv_sampling;

        /// <summary>
        /// Padding for later use
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;
    }

    // Decoding options
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPDecoderOptions
    {
        public int bypass_filtering;               // if true, skip the in-loop filtering
        public int no_fancy_upsampling;            // if true, use faster pointwise upsampler
        public int use_cropping;                   // if true, cropping is applied _first_
        public int crop_left, crop_top;            // top-left position for cropping.
        // Will be snapped to even values.
        public int crop_width, crop_height;        // dimension of the cropping area
        public int use_scaling;                    // if true, scaling is applied _afterward_
        public int scaled_width, scaled_height;    // final resolution
        public int use_threads;                    // if true, use multi-threaded decoding
        public int dithering_strength;             // dithering strength (0=Off, 100=full)

        // Unused for now:
        public int force_rotation;                 // forced rotation (to be applied _last_)
        public int no_enhancement;                 // if true, discard enhancement layer
        /// uint32_t[5]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPDecoderConfig
    {

        /// WebPBitstreamFeatures->Anonymous_c6b01f0b_3e38_4731_b2d6_9c0e3bdb71aa
        public WebPBitstreamFeatures input;

        /// WebPDecBuffer->Anonymous_5c438b36_7de6_498e_934a_d3613b37f5fc
        public WebPDecBuffer output;

        /// WebPDecoderOptions->Anonymous_78066979_3e1e_4d74_aee5_f316b20e3385
        public WebPDecoderOptions options;
    }


    public enum WebPImageHint
    {

        /// WEBP_HINT_DEFAULT -> 0
        WEBP_HINT_DEFAULT = 0,

        WEBP_HINT_PICTURE,

        WEBP_HINT_PHOTO,

        WEBP_HINT_GRAPH,

        WEBP_HINT_LAST,
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPConfig
    {

        /// int
        public int lossless;

        /// float
        public float quality;

        /// int
        public int method;

        /// WebPImageHint->Anonymous_838f22f5_6f57_48a0_9ecb_8eec917009f9
        public WebPImageHint image_hint;

        /// int
        public int target_size;

        /// float
        public float target_PSNR;

        /// int
        public int segments;

        /// int
        public int sns_strength;

        /// int
        public int filter_strength;

        /// int
        public int filter_sharpness;

        /// int
        public int filter_type;

        /// int
        public int autofilter;

        /// int
        public int alpha_compression;

        /// int
        public int alpha_filtering;

        /// int
        public int alpha_quality;

        /// int
        public int pass;

        /// int
        public int show_compressed;

        /// int
        public int preprocessing;

        /// int
        public int partitions;

        /// int
        public int partition_limit;

        public int emulate_jpeg_size;

        public int thread_level;

        public int low_memory;

        /// uint32_t[5]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;
    }

    public enum WebPPreset
    {

        /// WEBP_PRESET_DEFAULT -> 0
        WEBP_PRESET_DEFAULT = 0,

        WEBP_PRESET_PICTURE,

        WEBP_PRESET_PHOTO,

        WEBP_PRESET_DRAWING,

        WEBP_PRESET_ICON,

        WEBP_PRESET_TEXT,
    }






    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPAuxStats
    {

        /// int
        public int coded_size;

        /// float[5]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.R4)]
        public float[] PSNR;

        /// int[3]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
        public int[] block_count;

        /// int[2]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I4)]
        public int[] header_bytes;

        /// int[12]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12, ArraySubType = UnmanagedType.I4)]
        public int[] residual_bytes;

        /// int[4]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.I4)]
        public int[] segment_size;

        /// int[4]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.I4)]
        public int[] segment_quant;

        /// int[4]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.I4)]
        public int[] segment_level;

        /// int
        public int alpha_data_size;

        /// int
        public int layer_data_size;

        /// uint32_t->unsigned int
        public uint lossless_features;

        /// int
        public int histogram_bits;

        /// int
        public int transform_bits;

        /// int
        public int cache_bits;

        /// int
        public int palette_size;

        /// int
        public int lossless_size;

        /// uint32_t[4]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;
    }


    /// Return Type: int
    ///data: uint8_t*
    ///data_size: size_t->unsigned int
    ///picture: WebPPicture*
    public delegate int WebPWriterFunction([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture);

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPMemoryWriter
    {

        /// uint8_t*
        public IntPtr mem;

        /// size_t->unsigned int
        public UIntPtr size;

        /// size_t->unsigned int
        public UIntPtr max_size;

        /// uint32_t[1]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.U4)]
        public uint[] pad;
    }



    /// Return Type: int
    ///percent: int
    ///picture: WebPPicture*
    public delegate int WebPProgressHook(int percent, ref WebPPicture picture);

    public enum WebPEncCSP
    {

        /// WEBP_YUV420 -> 0
        WEBP_YUV420 = 0,

        /// WEBP_YUV422 -> 1
        WEBP_YUV422 = 1,

        /// WEBP_YUV444 -> 2
        WEBP_YUV444 = 2,

        /// WEBP_YUV400 -> 3
        WEBP_YUV400 = 3,

        /// WEBP_CSP_UV_MASK -> 3
        WEBP_CSP_UV_MASK = 3,

        /// WEBP_YUV420A -> 4
        WEBP_YUV420A = 4,

        /// WEBP_YUV422A -> 5
        WEBP_YUV422A = 5,

        /// WEBP_YUV444A -> 6
        WEBP_YUV444A = 6,

        /// WEBP_YUV400A -> 7
        WEBP_YUV400A = 7,

        /// WEBP_CSP_ALPHA_BIT -> 4
        WEBP_CSP_ALPHA_BIT = 4,
    }


    public enum WebPEncodingError
    {

        /// VP8_ENC_OK -> 0
        VP8_ENC_OK = 0,

        VP8_ENC_ERROR_OUT_OF_MEMORY,

        VP8_ENC_ERROR_BITSTREAM_OUT_OF_MEMORY,

        VP8_ENC_ERROR_NULL_PARAMETER,

        VP8_ENC_ERROR_INVALID_CONFIGURATION,

        VP8_ENC_ERROR_BAD_DIMENSION,

        VP8_ENC_ERROR_PARTITION0_OVERFLOW,

        VP8_ENC_ERROR_PARTITION_OVERFLOW,

        VP8_ENC_ERROR_BAD_WRITE,

        VP8_ENC_ERROR_FILE_TOO_BIG,

        VP8_ENC_ERROR_USER_ABORT,

        VP8_ENC_ERROR_LAST,
    }



    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPPicture
    {

        /// int
        public int use_argb;

        /// WebPEncCSP->Anonymous_84ce7065_fe91_48b4_93d8_1f0e84319dba
        public WebPEncCSP colorspace;

        /// int
        public int width;

        /// int
        public int height;

        /// uint8_t*
        public IntPtr y;

        /// uint8_t*
        public IntPtr u;

        /// uint8_t*
        public IntPtr v;

        /// int
        public int y_stride;

        /// int
        public int uv_stride;

        /// uint8_t*
        public IntPtr a;

        /// int
        public int a_stride;

        /// uint32_t[2]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
        public uint[] pad1;

        /// uint32_t*
        public IntPtr argb;

        /// int
        public int argb_stride;

        /// uint32_t[3]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
        public uint[] pad2;

        /// WebPWriterFunction
        public WebPWriterFunction writer;

        /// void*
        public IntPtr custom_ptr;

        /// int
        public int extra_info_type;

        /// uint8_t*
        public IntPtr extra_info;

        /// WebPAuxStats*
        public IntPtr stats;

        /// WebPEncodingError->Anonymous_8b714d63_f91b_46af_b0d0_667c703ed356
        public WebPEncodingError error_code;

        /// WebPProgressHook
        public WebPProgressHook progress_hook;

        /// void*
        public IntPtr user_data;

        /// uint32_t[3]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
        public uint[] pad3;

        /// uint8_t*
        public IntPtr u0;

        /// uint8_t*
        public IntPtr v0;

        /// int
        public int uv0_stride;

        /// uint32_t[7]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 7, ArraySubType = UnmanagedType.U4)]
        public uint[] pad4;

        /// void*
        public IntPtr memory_;

        /// void*
        public IntPtr memory_argb_;

        /// void*[2]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.SysUInt)]
        public IntPtr[] pad5;
    }

    public class NativeBindings
    {

        /// WEBP_DECODER_ABI_VERSION 0x0203    // MAJOR(8b) + MINOR(8b)
        public const int WEBP_DECODER_ABI_VERSION = 515;

        /// WEBP_ENCODER_ABI_VERSION 0x0202    // MAJOR(8b) + MINOR(8b)
        public const int WEBP_ENCODER_ABI_VERSION = 514;

        /// <summary>
        /// The maximum length of any dimension of a WebP image is 16383
        /// </summary>
        public const int WEBP_MAX_DIMENSION = 16383;

        #region NATIVE_WRAPPERS


        /// Return Type: int
        public static int WebPGetDecoderVersion()
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPGetDecoderVersion() :
                External.WebPGetDecoderVersion();
        }

        public static void WebPSafeFree(IntPtr toDeallocate)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPSafeFree(toDeallocate);
            }
            else
            {
                External.WebPSafeFree(toDeallocate);
            }
        }


        /// <summary>
        /// Retrieve basic header information: width, height.
        /// This function will also validate the header and return 0 in
        /// case of formatting error.
        /// Pointers 'width' and 'height' can be passed NULL if deemed irrelevant.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="data_size"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int WebPGetInfo([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPGetInfo(data, data_size, ref width, ref height) :
                External.WebPGetInfo(data, data_size, ref width, ref height);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        public static IntPtr WebPDecodeRGBA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeRGBA(data, data_size, ref width, ref height) :
                External.WebPDecodeRGBA(data, data_size, ref width, ref height);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        public static IntPtr WebPDecodeARGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeARGB(data, data_size, ref width, ref height) :
                External.WebPDecodeARGB(data, data_size, ref width, ref height);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        public static IntPtr WebPDecodeBGRA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeBGRA(data, data_size, ref width, ref height) :
                External.WebPDecodeBGRA(data, data_size, ref width, ref height);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        public static IntPtr WebPDecodeRGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeRGB(data, data_size, ref width, ref height) :
                External.WebPDecodeRGB(data, data_size, ref width, ref height);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        public static IntPtr WebPDecodeBGR([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeBGR(data, data_size, ref width, ref height) :
                External.WebPDecodeBGR(data, data_size, ref width, ref height);
        }

        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///width: int*
        ///height: int*
        ///u: uint8_t**
        ///v: uint8_t**
        ///stride: int*
        ///uv_stride: int*
        public static IntPtr WebPDecodeYUV([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height, ref IntPtr u, ref IntPtr v, ref int stride, ref int uv_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeYUV(data, data_size, ref width, ref height, ref u, ref v, ref stride, ref uv_stride) :
                External.WebPDecodeYUV(data, data_size, ref width, ref height, ref u, ref v, ref stride, ref uv_stride);
        }

        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPDecodeRGBAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeRGBAInto(data, data_size, output_buffer, output_buffer_size, output_stride) :
                External.WebPDecodeRGBAInto(data, data_size, output_buffer, output_buffer_size, output_stride);
        }

        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPDecodeARGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeARGBInto(data, data_size, output_buffer, output_buffer_size, output_stride) :
                External.WebPDecodeARGBInto(data, data_size, output_buffer, output_buffer_size, output_stride);
        }

        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPDecodeBGRAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeBGRAInto(data, data_size, output_buffer, output_buffer_size, output_stride) :
                External.WebPDecodeBGRAInto(data, data_size, output_buffer, output_buffer_size, output_stride);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPDecodeRGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeRGBInto(data, data_size, output_buffer, output_buffer_size, output_stride) :
                External.WebPDecodeRGBInto(data, data_size, output_buffer, output_buffer_size, output_stride);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPDecodeBGRInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeBGRInto(data, data_size, output_buffer, output_buffer_size, output_stride) :
                External.WebPDecodeBGRInto(data, data_size, output_buffer, output_buffer_size, output_stride);
        }


        /// Return Type: uint8_t*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///luma: uint8_t*
        ///luma_size: size_t->unsigned int
        ///luma_stride: int
        ///u: uint8_t*
        ///u_size: size_t->unsigned int
        ///u_stride: int
        ///v: uint8_t*
        ///v_size: size_t->unsigned int
        ///v_stride: int
        public static IntPtr WebPDecodeYUVInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecodeYUVInto(data, data_size, luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride) :
                External.WebPDecodeYUVInto(data, data_size, luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride);
        }


        /// Return Type: int
        ///param0: WebPDecBuffer*
        ///param1: int
        public static int WebPInitDecBufferInternal(ref WebPDecBuffer param0, int param1)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPInitDecBufferInternal(ref param0, param1) :
                External.WebPInitDecBufferInternal(ref param0, param1);
        }


        /// Return Type: void
        ///buffer: WebPDecBuffer*
        public static void WebPFreeDecBuffer(ref WebPDecBuffer buffer)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPFreeDecBuffer(ref buffer);
            }
            else
            {
                External.WebPFreeDecBuffer(ref buffer);
            }
        }


        /// Return Type: WebPIDecoder*
        ///output_buffer: WebPDecBuffer*
        public static IntPtr WebPINewDecoder(ref WebPDecBuffer output_buffer)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPINewDecoder(ref output_buffer) :
                External.WebPINewDecoder(ref output_buffer);
        }


        /// Return Type: WebPIDecoder*
        ///csp: WEBP_CSP_MODE->Anonymous_cb136f5b_1d5d_49a0_aca4_656a79e9d159
        ///output_buffer: uint8_t*
        ///output_buffer_size: size_t->unsigned int
        ///output_stride: int
        public static IntPtr WebPINewRGB(WEBP_CSP_MODE csp, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPINewRGB(csp, output_buffer, output_buffer_size, output_stride) :
                External.WebPINewRGB(csp, output_buffer, output_buffer_size, output_stride);
        }

        /// Return Type: WebPIDecoder*
        ///luma: uint8_t*
        ///luma_size: size_t->unsigned int
        ///luma_stride: int
        ///u: uint8_t*
        ///u_size: size_t->unsigned int
        ///u_stride: int
        ///v: uint8_t*
        ///v_size: size_t->unsigned int
        ///v_stride: int
        ///a: uint8_t*
        ///a_size: size_t->unsigned int
        ///a_stride: int
        public static IntPtr WebPINewYUVA(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride, IntPtr a, UIntPtr a_size, int a_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPINewYUVA(luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride, a, a_size, a_stride) :
                External.WebPINewYUVA(luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride, a, a_size, a_stride);
        }


        /// Return Type: WebPIDecoder*
        ///luma: uint8_t*
        ///luma_size: size_t->unsigned int
        ///luma_stride: int
        ///u: uint8_t*
        ///u_size: size_t->unsigned int
        ///u_stride: int
        ///v: uint8_t*
        ///v_size: size_t->unsigned int
        ///v_stride: int
        public static IntPtr WebPINewYUV(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPINewYUV(luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride) :
                External.WebPINewYUV(luma, luma_size, luma_stride, u, u_size, u_stride, v, v_size, v_stride);
        }

        /// Return Type: void
        ///idec: WebPIDecoder*
        public static void WebPIDelete(ref WebPIDecoder idec)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPIDelete(ref idec);
            }
            else
            {
                External.WebPIDelete(ref idec);
            }
        }

        /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
        ///idec: WebPIDecoder*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        public static VP8StatusCode WebPIAppend(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIAppend(ref idec, data, data_size) :
                External.WebPIAppend(ref idec, data, data_size);
        }


        /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
        ///idec: WebPIDecoder*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        public static VP8StatusCode WebPIUpdate(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIUpdate(ref idec, data, data_size) :
                External.WebPIUpdate(ref idec, data, data_size);
        }


        /// Return Type: uint8_t*
        ///idec: WebPIDecoder*
        ///last_y: int*
        ///width: int*
        ///height: int*
        ///stride: int*
        public static IntPtr WebPIDecGetRGB(ref WebPIDecoder idec, ref int last_y, ref int width, ref int height, ref int stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIDecGetRGB(ref idec, ref last_y, ref width, ref height, ref stride) :
                External.WebPIDecGetRGB(ref idec, ref last_y, ref width, ref height, ref stride);
        }


        /// Return Type: uint8_t*
        ///idec: WebPIDecoder*
        ///last_y: int*
        ///u: uint8_t**
        ///v: uint8_t**
        ///a: uint8_t**
        ///width: int*
        ///height: int*
        ///stride: int*
        ///uv_stride: int*
        ///a_stride: int*
        public static IntPtr WebPIDecGetYUVA(ref WebPIDecoder idec, ref int last_y, ref IntPtr u, ref IntPtr v, ref IntPtr a, ref int width, ref int height, ref int stride, ref int uv_stride, ref int a_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIDecGetYUVA(ref idec, ref last_y, ref u, ref v, ref a, ref width, ref height, ref stride, ref uv_stride, ref a_stride) :
                External.WebPIDecGetYUVA(ref idec, ref last_y, ref u, ref v, ref a, ref width, ref height, ref stride, ref uv_stride, ref a_stride);
        }


        /// Return Type: WebPDecBuffer*
        ///idec: WebPIDecoder*
        ///left: int*
        ///top: int*
        ///width: int*
        ///height: int*
        public static IntPtr WebPIDecodedArea(ref WebPIDecoder idec, ref int left, ref int top, ref int width, ref int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIDecodedArea(ref idec, ref left, ref top, ref width, ref height) :
                External.WebPIDecodedArea(ref idec, ref left, ref top, ref width, ref height);
        }


        /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
        ///param0: uint8_t*
        ///param1: size_t->unsigned int
        ///param2: WebPBitstreamFeatures*
        ///param3: int
        public static VP8StatusCode WebPGetFeaturesInternal([InAttribute()] IntPtr param0, UIntPtr param1, ref WebPBitstreamFeatures param2, int param3)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPGetFeaturesInternal(param0, param1, ref param2, param3) :
                External.WebPGetFeaturesInternal(param0, param1, ref param2, param3);
        }


        /// Return Type: int
        ///param0: WebPDecoderConfig*
        ///param1: int
        public static int WebPInitDecoderConfigInternal(ref WebPDecoderConfig param0, int param1)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPInitDecoderConfigInternal(ref param0, param1) :
                External.WebPInitDecoderConfigInternal(ref param0, param1);
        }


        /// Return Type: WebPIDecoder*
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///config: WebPDecoderConfig*
        public static IntPtr WebPIDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPIDecode(data, data_size, ref config) :
                External.WebPIDecode(data, data_size, ref config);
        }


        /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///config: WebPDecoderConfig*
        public static VP8StatusCode WebPDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPDecode(data, data_size, ref config) :
                External.WebPDecode(data, data_size, ref config);
        }


        /// Return Type: int
        public static int WebPGetEncoderVersion()
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPGetEncoderVersion() :
                External.WebPGetEncoderVersion();
        }


        /// Return Type: size_t->unsigned int
        ///rgb: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///quality_factor: float
        ///output: uint8_t**
        public static UIntPtr WebPEncodeRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, float quality_factor, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeRGB(rgb, width, height, stride, quality_factor, ref output) :
                External.WebPEncodeRGB(rgb, width, height, stride, quality_factor, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///bgr: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///quality_factor: float
        ///output: uint8_t**
        public static UIntPtr WebPEncodeBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, float quality_factor, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeBGR(bgr, width, height, stride, quality_factor, ref output) :
                External.WebPEncodeBGR(bgr, width, height, stride, quality_factor, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///rgba: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///quality_factor: float
        ///output: uint8_t**
        public static UIntPtr WebPEncodeRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, float quality_factor, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeRGBA(rgba, width, height, stride, quality_factor, ref output) :
                External.WebPEncodeRGBA(rgba, width, height, stride, quality_factor, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///bgra: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///quality_factor: float
        ///output: uint8_t**
        public static IntPtr WebPEncodeBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, float quality_factor, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeBGRA(bgra, width, height, stride, quality_factor, ref output) :
                External.WebPEncodeBGRA(bgra, width, height, stride, quality_factor, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///rgb: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///output: uint8_t**
        public static UIntPtr WebPEncodeLosslessRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeLosslessRGB(rgb, width, height, stride, ref output) :
                External.WebPEncodeLosslessRGB(rgb, width, height, stride, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///bgr: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///output: uint8_t**
        public static UIntPtr WebPEncodeLosslessBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeLosslessBGR(bgr, width, height, stride, ref output) :
                External.WebPEncodeLosslessBGR(bgr, width, height, stride, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///rgba: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///output: uint8_t**
        public static UIntPtr WebPEncodeLosslessRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeLosslessRGBA(rgba, width, height, stride, ref output) :
                External.WebPEncodeLosslessRGBA(rgba, width, height, stride, ref output);
        }


        /// Return Type: size_t->unsigned int
        ///bgra: uint8_t*
        ///width: int
        ///height: int
        ///stride: int
        ///output: uint8_t**
        public static UIntPtr WebPEncodeLosslessBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, ref IntPtr output)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncodeLosslessBGRA(bgra, width, height, stride, ref output) :
                External.WebPEncodeLosslessBGRA(bgra, width, height, stride, ref output);
        }


        /// Return Type: int
        ///param0: WebPConfig*
        ///param1: WebPPreset->Anonymous_017d4167_f53e_4b3d_b029_592ff5c3f80b
        ///param2: float
        ///param3: int
        public static int WebPConfigInitInternal(ref WebPConfig param0, WebPPreset param1, float param2, int param3)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPConfigInitInternal(ref param0, param1, param2, param3) :
                External.WebPConfigInitInternal(ref param0, param1, param2, param3);
        }


        /// Return Type: int
        ///config: WebPConfig*
        public static int WebPValidateConfig(ref WebPConfig config)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPValidateConfig(ref config) :
                External.WebPValidateConfig(ref config);
        }


        /// Return Type: void
        ///writer: WebPMemoryWriter*
        public static void WebPMemoryWriterInit(ref WebPMemoryWriter writer)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPMemoryWriterInit(ref writer);
            }
            else
            {
                External.WebPMemoryWriterInit(ref writer);
            }
        }


        /// Return Type: int
        ///data: uint8_t*
        ///data_size: size_t->unsigned int
        ///picture: WebPPicture*
        public static int WebPMemoryWrite([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPMemoryWrite(data, data_size, ref picture) :
                External.WebPMemoryWrite(data, data_size, ref picture);
        }


        /// Return Type: int
        ///param0: WebPPicture*
        ///param1: int
        public static int WebPPictureInitInternal(ref WebPPicture param0, int param1)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureInitInternal(ref param0, param1) :
                External.WebPPictureInitInternal(ref param0, param1);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        public static int WebPPictureAlloc(ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureAlloc(ref picture) :
                External.WebPPictureAlloc(ref picture);
        }


        /// Return Type: void
        ///picture: WebPPicture*
        public static void WebPPictureFree(ref WebPPicture picture)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPPictureFree(ref picture);
            }
            else
            {
                External.WebPPictureFree(ref picture);
            }
        }


        /// Return Type: int
        ///src: WebPPicture*
        ///dst: WebPPicture*
        public static int WebPPictureCopy(ref WebPPicture src, ref WebPPicture dst)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureCopy(ref src, ref dst) :
                External.WebPPictureCopy(ref src, ref dst);
        }

        /// Return Type: int
        ///pic1: WebPPicture*
        ///pic2: WebPPicture*
        ///metric_type: int
        ///result: float* result[5]
        ///

        /// <summary>
        /// Compute PSNR, SSIM or LSIM distortion metric between two pictures.
        /// Result is in dB, stores in result[] in the Y/U/V/Alpha/All order.
        /// Returns false in case of error (src and ref don't have same dimension, ...)
        /// Warning: this function is rather CPU-intensive.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="reference"></param>
        /// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static int WebPPictureDistortion(ref WebPPicture src, ref WebPPicture reference, int metric_type, ref float result)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureDistortion(ref src, ref reference, metric_type, ref result) :
                External.WebPPictureDistortion(ref src, ref reference, metric_type, ref result);
        }

        /// Return Type: int
        ///picture: WebPPicture*
        ///left: int
        ///top: int
        ///width: int
        ///height: int
        public static int WebPPictureCrop(ref WebPPicture picture, int left, int top, int width, int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureCrop(ref picture, left, top, width, height) :
                External.WebPPictureCrop(ref picture, left, top, width, height);
        }

        /// Return Type: int
        ///src: WebPPicture*
        ///left: int
        ///top: int
        ///width: int
        ///height: int
        ///dst: WebPPicture*
        public static int WebPPictureView(ref WebPPicture src, int left, int top, int width, int height, ref WebPPicture dst)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureView(ref src, left, top, width, height, ref dst) :
                External.WebPPictureView(ref src, left, top, width, height, ref dst);
        }

        /// Return Type: int
        ///picture: WebPPicture*
        public static int WebPPictureIsView(ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureIsView(ref picture) :
                External.WebPPictureIsView(ref picture);
        }


        /// Return Type: int
        ///pic: WebPPicture*
        ///width: int
        ///height: int
        public static int WebPPictureRescale(ref WebPPicture pic, int width, int height)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureRescale(ref pic, width, height) :
                External.WebPPictureRescale(ref pic, width, height);
        }

        /// Return Type: int
        ///picture: WebPPicture*
        ///rgb: uint8_t*
        ///rgb_stride: int
        public static int WebPPictureImportRGB(ref WebPPicture picture, [InAttribute()] IntPtr rgb, int rgb_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportRGB(ref picture, rgb, rgb_stride) :
                External.WebPPictureImportRGB(ref picture, rgb, rgb_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///rgba: uint8_t*
        ///rgba_stride: int
        public static int WebPPictureImportRGBA(ref WebPPicture picture, [InAttribute()] IntPtr rgba, int rgba_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportRGBA(ref picture, rgba, rgba_stride) :
                External.WebPPictureImportRGBA(ref picture, rgba, rgba_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///rgbx: uint8_t*
        ///rgbx_stride: int
        public static int WebPPictureImportRGBX(ref WebPPicture picture, [InAttribute()] IntPtr rgbx, int rgbx_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportRGBX(ref picture, rgbx, rgbx_stride) :
                External.WebPPictureImportRGBX(ref picture, rgbx, rgbx_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///bgr: uint8_t*
        ///bgr_stride: int
        public static int WebPPictureImportBGR(ref WebPPicture picture, [InAttribute()] IntPtr bgr, int bgr_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportBGR(ref picture, bgr, bgr_stride) :
                External.WebPPictureImportBGR(ref picture, bgr, bgr_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///bgra: uint8_t*
        ///bgra_stride: int
        public static int WebPPictureImportBGRA(ref WebPPicture picture, [InAttribute()] IntPtr bgra, int bgra_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportBGRA(ref picture, bgra, bgra_stride) :
                External.WebPPictureImportBGRA(ref picture, bgra, bgra_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///bgrx: uint8_t*
        ///bgrx_stride: int
        public static int WebPPictureImportBGRX(ref WebPPicture picture, [InAttribute()] IntPtr bgrx, int bgrx_stride)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureImportBGRX(ref picture, bgrx, bgrx_stride) :
                External.WebPPictureImportBGRX(ref picture, bgrx, bgrx_stride);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        ///colorspace: WebPEncCSP->Anonymous_84ce7065_fe91_48b4_93d8_1f0e84319dba
        public static int WebPPictureARGBToYUVA(ref WebPPicture picture, WebPEncCSP colorspace)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureARGBToYUVA(ref picture, colorspace) :
                External.WebPPictureARGBToYUVA(ref picture, colorspace);
        }


        /// Return Type: int
        ///picture: WebPPicture*
        public static int WebPPictureYUVAToARGB(ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureYUVAToARGB(ref picture) :
                External.WebPPictureYUVAToARGB(ref picture);
        }


        /// Return Type: void
        ///picture: WebPPicture*
        public static void WebPCleanupTransparentArea(ref WebPPicture picture)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Internal.WebPCleanupTransparentArea(ref picture);
            }
            else
            {
                External.WebPCleanupTransparentArea(ref picture);
            }
        }


        /// Return Type: int
        ///picture: WebPPicture*
        public static int WebPPictureHasTransparency(ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPPictureHasTransparency(ref picture) :
                External.WebPPictureHasTransparency(ref picture);
        }

        /// Return Type: int
        ///config: WebPConfig*
        ///picture: WebPPicture*
        public static int WebPEncode(ref WebPConfig config, ref WebPPicture picture)
        {
            return (Application.platform == RuntimePlatform.IPhonePlayer) ?
                Internal.WebPEncode(ref config, ref picture) :
                External.WebPEncode(ref config, ref picture);
        }

        #endregion

        #region INTERNAL_DLL_IMPORT

        class Internal
        {
            /// Return Type: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPGetDecoderVersion")]
            public static extern int WebPGetDecoderVersion();

            [DllImportAttribute("__Internal", EntryPoint = "WebPSafeFree")]
            public static extern void WebPSafeFree(IntPtr toDeallocate);


            /// <summary>
            /// Retrieve basic header information: width, height.
            /// This function will also validate the header and return 0 in
            /// case of formatting error.
            /// Pointers 'width' and 'height' can be passed NULL if deemed irrelevant.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="data_size"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns></returns>
            [DllImportAttribute("__Internal", EntryPoint = "WebPGetInfo")]
            public static extern int WebPGetInfo([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeRGBA")]
            public static extern IntPtr WebPDecodeRGBA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeARGB")]
            public static extern IntPtr WebPDecodeARGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeBGRA")]
            public static extern IntPtr WebPDecodeBGRA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeRGB")]
            public static extern IntPtr WebPDecodeRGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeBGR")]
            public static extern IntPtr WebPDecodeBGR([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            ///u: uint8_t**
            ///v: uint8_t**
            ///stride: int*
            ///uv_stride: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeYUV")]
            public static extern IntPtr WebPDecodeYUV([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height, ref IntPtr u, ref IntPtr v, ref int stride, ref int uv_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeRGBAInto")]
            public static extern IntPtr WebPDecodeRGBAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeARGBInto")]
            public static extern IntPtr WebPDecodeARGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeBGRAInto")]
            public static extern IntPtr WebPDecodeBGRAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeRGBInto")]
            public static extern IntPtr WebPDecodeRGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeBGRInto")]
            public static extern IntPtr WebPDecodeBGRInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecodeYUVInto")]
            public static extern IntPtr WebPDecodeYUVInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride);


            /// Return Type: int
            ///param0: WebPDecBuffer*
            ///param1: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPInitDecBufferInternal")]
            public static extern int WebPInitDecBufferInternal(ref WebPDecBuffer param0, int param1);


            /// Return Type: void
            ///buffer: WebPDecBuffer*
            [DllImportAttribute("__Internal", EntryPoint = "WebPFreeDecBuffer")]
            public static extern void WebPFreeDecBuffer(ref WebPDecBuffer buffer);


            /// Return Type: WebPIDecoder*
            ///output_buffer: WebPDecBuffer*
            [DllImportAttribute("__Internal", EntryPoint = "WebPINewDecoder")]
            public static extern IntPtr WebPINewDecoder(ref WebPDecBuffer output_buffer);


            /// Return Type: WebPIDecoder*
            ///csp: WEBP_CSP_MODE->Anonymous_cb136f5b_1d5d_49a0_aca4_656a79e9d159
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPINewRGB")]
            public static extern IntPtr WebPINewRGB(WEBP_CSP_MODE csp, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: WebPIDecoder*
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            ///a: uint8_t*
            ///a_size: size_t->unsigned int
            ///a_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPINewYUVA")]
            public static extern IntPtr WebPINewYUVA(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride, IntPtr a, UIntPtr a_size, int a_stride);


            /// Return Type: WebPIDecoder*
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPINewYUV")]
            public static extern IntPtr WebPINewYUV(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride);


            /// Return Type: void
            ///idec: WebPIDecoder*
            [DllImportAttribute("__Internal", EntryPoint = "WebPIDelete")]
            public static extern void WebPIDelete(ref WebPIDecoder idec);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///idec: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            [DllImportAttribute("__Internal", EntryPoint = "WebPIAppend")]
            public static extern VP8StatusCode WebPIAppend(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///idec: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            [DllImportAttribute("__Internal", EntryPoint = "WebPIUpdate")]
            public static extern VP8StatusCode WebPIUpdate(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size);


            /// Return Type: uint8_t*
            ///idec: WebPIDecoder*
            ///last_y: int*
            ///width: int*
            ///height: int*
            ///stride: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPIDecGetRGB")]
            public static extern IntPtr WebPIDecGetRGB(ref WebPIDecoder idec, ref int last_y, ref int width, ref int height, ref int stride);


            /// Return Type: uint8_t*
            ///idec: WebPIDecoder*
            ///last_y: int*
            ///u: uint8_t**
            ///v: uint8_t**
            ///a: uint8_t**
            ///width: int*
            ///height: int*
            ///stride: int*
            ///uv_stride: int*
            ///a_stride: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPIDecGetYUVA")]
            public static extern IntPtr WebPIDecGetYUVA(ref WebPIDecoder idec, ref int last_y, ref IntPtr u, ref IntPtr v, ref IntPtr a, ref int width, ref int height, ref int stride, ref int uv_stride, ref int a_stride);


            /// Return Type: WebPDecBuffer*
            ///idec: WebPIDecoder*
            ///left: int*
            ///top: int*
            ///width: int*
            ///height: int*
            [DllImportAttribute("__Internal", EntryPoint = "WebPIDecodedArea")]
            public static extern IntPtr WebPIDecodedArea(ref WebPIDecoder idec, ref int left, ref int top, ref int width, ref int height);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///param0: uint8_t*
            ///param1: size_t->unsigned int
            ///param2: WebPBitstreamFeatures*
            ///param3: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPGetFeaturesInternal")]
            public static extern VP8StatusCode WebPGetFeaturesInternal([InAttribute()] IntPtr param0, UIntPtr param1, ref WebPBitstreamFeatures param2, int param3);


            /// Return Type: int
            ///param0: WebPDecoderConfig*
            ///param1: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPInitDecoderConfigInternal")]
            public static extern int WebPInitDecoderConfigInternal(ref WebPDecoderConfig param0, int param1);


            /// Return Type: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///config: WebPDecoderConfig*
            [DllImportAttribute("__Internal", EntryPoint = "WebPIDecode")]
            public static extern IntPtr WebPIDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///config: WebPDecoderConfig*
            [DllImportAttribute("__Internal", EntryPoint = "WebPDecode")]
            public static extern VP8StatusCode WebPDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);


            /// Return Type: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPGetEncoderVersion")]
            public static extern int WebPGetEncoderVersion();


            /// Return Type: size_t->unsigned int
            ///rgb: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeRGB")]
            public static extern UIntPtr WebPEncodeRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgr: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeBGR")]
            public static extern UIntPtr WebPEncodeBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgba: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeRGBA")]
            public static extern UIntPtr WebPEncodeRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgra: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeBGRA")]
            public static extern IntPtr WebPEncodeBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgb: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeLosslessRGB")]
            public static extern UIntPtr WebPEncodeLosslessRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgr: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeLosslessBGR")]
            public static extern UIntPtr WebPEncodeLosslessBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgba: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeLosslessRGBA")]
            public static extern UIntPtr WebPEncodeLosslessRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgra: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncodeLosslessBGRA")]
            public static extern UIntPtr WebPEncodeLosslessBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, ref IntPtr output);


            /// Return Type: int
            ///param0: WebPConfig*
            ///param1: WebPPreset->Anonymous_017d4167_f53e_4b3d_b029_592ff5c3f80b
            ///param2: float
            ///param3: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPConfigInitInternal")]
            public static extern int WebPConfigInitInternal(ref WebPConfig param0, WebPPreset param1, float param2, int param3);


            /// Return Type: int
            ///config: WebPConfig*
            [DllImportAttribute("__Internal", EntryPoint = "WebPValidateConfig")]
            public static extern int WebPValidateConfig(ref WebPConfig config);


            /// Return Type: void
            ///writer: WebPMemoryWriter*
            [DllImportAttribute("__Internal", EntryPoint = "WebPMemoryWriterInit")]
            public static extern void WebPMemoryWriterInit(ref WebPMemoryWriter writer);


            /// Return Type: int
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPMemoryWrite")]
            public static extern int WebPMemoryWrite([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture);


            /// Return Type: int
            ///param0: WebPPicture*
            ///param1: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureInitInternal")]
            public static extern int WebPPictureInitInternal(ref WebPPicture param0, int param1);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureAlloc")]
            public static extern int WebPPictureAlloc(ref WebPPicture picture);


            /// Return Type: void
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureFree")]
            public static extern void WebPPictureFree(ref WebPPicture picture);


            /// Return Type: int
            ///src: WebPPicture*
            ///dst: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureCopy")]
            public static extern int WebPPictureCopy(ref WebPPicture src, ref WebPPicture dst);


            /// Return Type: int
            ///pic1: WebPPicture*
            ///pic2: WebPPicture*
            ///metric_type: int
            ///result: float* result[5]
            ///

            /// <summary>
            /// Compute PSNR, SSIM or LSIM distortion metric between two pictures.
            /// Result is in dB, stores in result[] in the Y/U/V/Alpha/All order.
            /// Returns false in case of error (src and ref don't have same dimension, ...)
            /// Warning: this function is rather CPU-intensive.
            /// </summary>
            /// <param name="src"></param>
            /// <param name="reference"></param>
            /// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
            /// <param name="result"></param>
            /// <returns></returns>
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureDistortion")]
            public static extern int WebPPictureDistortion(ref WebPPicture src, ref WebPPicture reference, int metric_type, ref float result);




            /// Return Type: int
            ///picture: WebPPicture*
            ///left: int
            ///top: int
            ///width: int
            ///height: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureCrop")]
            public static extern int WebPPictureCrop(ref WebPPicture picture, int left, int top, int width, int height);


            /// Return Type: int
            ///src: WebPPicture*
            ///left: int
            ///top: int
            ///width: int
            ///height: int
            ///dst: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureView")]
            public static extern int WebPPictureView(ref WebPPicture src, int left, int top, int width, int height, ref WebPPicture dst);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureIsView")]
            public static extern int WebPPictureIsView(ref WebPPicture picture);


            /// Return Type: int
            ///pic: WebPPicture*
            ///width: int
            ///height: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureRescale")]
            public static extern int WebPPictureRescale(ref WebPPicture pic, int width, int height);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgb: uint8_t*
            ///rgb_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportRGB")]
            public static extern int WebPPictureImportRGB(ref WebPPicture picture, [InAttribute()] IntPtr rgb, int rgb_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgba: uint8_t*
            ///rgba_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportRGBA")]
            public static extern int WebPPictureImportRGBA(ref WebPPicture picture, [InAttribute()] IntPtr rgba, int rgba_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgbx: uint8_t*
            ///rgbx_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportRGBX")]
            public static extern int WebPPictureImportRGBX(ref WebPPicture picture, [InAttribute()] IntPtr rgbx, int rgbx_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgr: uint8_t*
            ///bgr_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportBGR")]
            public static extern int WebPPictureImportBGR(ref WebPPicture picture, [InAttribute()] IntPtr bgr, int bgr_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgra: uint8_t*
            ///bgra_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportBGRA")]
            public static extern int WebPPictureImportBGRA(ref WebPPicture picture, [InAttribute()] IntPtr bgra, int bgra_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgrx: uint8_t*
            ///bgrx_stride: int
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureImportBGRX")]
            public static extern int WebPPictureImportBGRX(ref WebPPicture picture, [InAttribute()] IntPtr bgrx, int bgrx_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///colorspace: WebPEncCSP->Anonymous_84ce7065_fe91_48b4_93d8_1f0e84319dba
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureARGBToYUVA")]
            public static extern int WebPPictureARGBToYUVA(ref WebPPicture picture, WebPEncCSP colorspace);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureYUVAToARGB")]
            public static extern int WebPPictureYUVAToARGB(ref WebPPicture picture);


            /// Return Type: void
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPCleanupTransparentArea")]
            public static extern void WebPCleanupTransparentArea(ref WebPPicture picture);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPPictureHasTransparency")]
            public static extern int WebPPictureHasTransparency(ref WebPPicture picture);


            /// Return Type: int
            ///config: WebPConfig*
            ///picture: WebPPicture*
            [DllImportAttribute("__Internal", EntryPoint = "WebPEncode")]
            public static extern int WebPEncode(ref WebPConfig config, ref WebPPicture picture);
        }

        #endregion

        #region EXTERNAL_DLL_IMPORT

        class External
        {
            /// Return Type: int
            [DllImportAttribute("webp", EntryPoint = "WebPGetDecoderVersion")]
            public static extern int WebPGetDecoderVersion();

            [DllImportAttribute("webp", EntryPoint = "WebPSafeFree")]
            public static extern void WebPSafeFree(IntPtr toDeallocate);


            /// <summary>
            /// Retrieve basic header information: width, height.
            /// This function will also validate the header and return 0 in
            /// case of formatting error.
            /// Pointers 'width' and 'height' can be passed NULL if deemed irrelevant.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="data_size"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns></returns>
            [DllImportAttribute("webp", EntryPoint = "WebPGetInfo")]
            public static extern int WebPGetInfo([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeRGBA")]
            public static extern IntPtr WebPDecodeRGBA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeARGB")]
            public static extern IntPtr WebPDecodeARGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeBGRA")]
            public static extern IntPtr WebPDecodeBGRA([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeRGB")]
            public static extern IntPtr WebPDecodeRGB([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeBGR")]
            public static extern IntPtr WebPDecodeBGR([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///width: int*
            ///height: int*
            ///u: uint8_t**
            ///v: uint8_t**
            ///stride: int*
            ///uv_stride: int*
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeYUV")]
            public static extern IntPtr WebPDecodeYUV([InAttribute()] IntPtr data, UIntPtr data_size, ref int width, ref int height, ref IntPtr u, ref IntPtr v, ref int stride, ref int uv_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeRGBAInto")]
            public static extern IntPtr WebPDecodeRGBAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeARGBInto")]
            public static extern IntPtr WebPDecodeARGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeBGRAInto")]
            public static extern IntPtr WebPDecodeBGRAInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeRGBInto")]
            public static extern IntPtr WebPDecodeRGBInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeBGRInto")]
            public static extern IntPtr WebPDecodeBGRInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: uint8_t*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPDecodeYUVInto")]
            public static extern IntPtr WebPDecodeYUVInto([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride);


            /// Return Type: int
            ///param0: WebPDecBuffer*
            ///param1: int
            [DllImportAttribute("webp", EntryPoint = "WebPInitDecBufferInternal")]
            public static extern int WebPInitDecBufferInternal(ref WebPDecBuffer param0, int param1);


            /// Return Type: void
            ///buffer: WebPDecBuffer*
            [DllImportAttribute("webp", EntryPoint = "WebPFreeDecBuffer")]
            public static extern void WebPFreeDecBuffer(ref WebPDecBuffer buffer);


            /// Return Type: WebPIDecoder*
            ///output_buffer: WebPDecBuffer*
            [DllImportAttribute("webp", EntryPoint = "WebPINewDecoder")]
            public static extern IntPtr WebPINewDecoder(ref WebPDecBuffer output_buffer);


            /// Return Type: WebPIDecoder*
            ///csp: WEBP_CSP_MODE->Anonymous_cb136f5b_1d5d_49a0_aca4_656a79e9d159
            ///output_buffer: uint8_t*
            ///output_buffer_size: size_t->unsigned int
            ///output_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPINewRGB")]
            public static extern IntPtr WebPINewRGB(WEBP_CSP_MODE csp, IntPtr output_buffer, UIntPtr output_buffer_size, int output_stride);


            /// Return Type: WebPIDecoder*
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            ///a: uint8_t*
            ///a_size: size_t->unsigned int
            ///a_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPINewYUVA")]
            public static extern IntPtr WebPINewYUVA(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride, IntPtr a, UIntPtr a_size, int a_stride);


            /// Return Type: WebPIDecoder*
            ///luma: uint8_t*
            ///luma_size: size_t->unsigned int
            ///luma_stride: int
            ///u: uint8_t*
            ///u_size: size_t->unsigned int
            ///u_stride: int
            ///v: uint8_t*
            ///v_size: size_t->unsigned int
            ///v_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPINewYUV")]
            public static extern IntPtr WebPINewYUV(IntPtr luma, UIntPtr luma_size, int luma_stride, IntPtr u, UIntPtr u_size, int u_stride, IntPtr v, UIntPtr v_size, int v_stride);


            /// Return Type: void
            ///idec: WebPIDecoder*
            [DllImportAttribute("webp", EntryPoint = "WebPIDelete")]
            public static extern void WebPIDelete(ref WebPIDecoder idec);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///idec: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            [DllImportAttribute("webp", EntryPoint = "WebPIAppend")]
            public static extern VP8StatusCode WebPIAppend(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///idec: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            [DllImportAttribute("webp", EntryPoint = "WebPIUpdate")]
            public static extern VP8StatusCode WebPIUpdate(ref WebPIDecoder idec, [InAttribute()] IntPtr data, UIntPtr data_size);


            /// Return Type: uint8_t*
            ///idec: WebPIDecoder*
            ///last_y: int*
            ///width: int*
            ///height: int*
            ///stride: int*
            [DllImportAttribute("webp", EntryPoint = "WebPIDecGetRGB")]
            public static extern IntPtr WebPIDecGetRGB(ref WebPIDecoder idec, ref int last_y, ref int width, ref int height, ref int stride);


            /// Return Type: uint8_t*
            ///idec: WebPIDecoder*
            ///last_y: int*
            ///u: uint8_t**
            ///v: uint8_t**
            ///a: uint8_t**
            ///width: int*
            ///height: int*
            ///stride: int*
            ///uv_stride: int*
            ///a_stride: int*
            [DllImportAttribute("webp", EntryPoint = "WebPIDecGetYUVA")]
            public static extern IntPtr WebPIDecGetYUVA(ref WebPIDecoder idec, ref int last_y, ref IntPtr u, ref IntPtr v, ref IntPtr a, ref int width, ref int height, ref int stride, ref int uv_stride, ref int a_stride);


            /// Return Type: WebPDecBuffer*
            ///idec: WebPIDecoder*
            ///left: int*
            ///top: int*
            ///width: int*
            ///height: int*
            [DllImportAttribute("webp", EntryPoint = "WebPIDecodedArea")]
            public static extern IntPtr WebPIDecodedArea(ref WebPIDecoder idec, ref int left, ref int top, ref int width, ref int height);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///param0: uint8_t*
            ///param1: size_t->unsigned int
            ///param2: WebPBitstreamFeatures*
            ///param3: int
            [DllImportAttribute("webp", EntryPoint = "WebPGetFeaturesInternal")]
            public static extern VP8StatusCode WebPGetFeaturesInternal([InAttribute()] IntPtr param0, UIntPtr param1, ref WebPBitstreamFeatures param2, int param3);


            /// Return Type: int
            ///param0: WebPDecoderConfig*
            ///param1: int
            [DllImportAttribute("webp", EntryPoint = "WebPInitDecoderConfigInternal")]
            public static extern int WebPInitDecoderConfigInternal(ref WebPDecoderConfig param0, int param1);


            /// Return Type: WebPIDecoder*
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///config: WebPDecoderConfig*
            [DllImportAttribute("webp", EntryPoint = "WebPIDecode")]
            public static extern IntPtr WebPIDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);


            /// Return Type: VP8StatusCode->Anonymous_b244cc15_fbc7_4c41_8884_71fe4f515cd6
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///config: WebPDecoderConfig*
            [DllImportAttribute("webp", EntryPoint = "WebPDecode")]
            public static extern VP8StatusCode WebPDecode([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);


            /// Return Type: int
            [DllImportAttribute("webp", EntryPoint = "WebPGetEncoderVersion")]
            public static extern int WebPGetEncoderVersion();


            /// Return Type: size_t->unsigned int
            ///rgb: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeRGB")]
            public static extern UIntPtr WebPEncodeRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgr: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeBGR")]
            public static extern UIntPtr WebPEncodeBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgba: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeRGBA")]
            public static extern UIntPtr WebPEncodeRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgra: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///quality_factor: float
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeBGRA")]
            public static extern IntPtr WebPEncodeBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, float quality_factor, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgb: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeLosslessRGB")]
            public static extern UIntPtr WebPEncodeLosslessRGB([InAttribute()] IntPtr rgb, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgr: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeLosslessBGR")]
            public static extern UIntPtr WebPEncodeLosslessBGR([InAttribute()] IntPtr bgr, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///rgba: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeLosslessRGBA")]
            public static extern UIntPtr WebPEncodeLosslessRGBA([InAttribute()] IntPtr rgba, int width, int height, int stride, ref IntPtr output);


            /// Return Type: size_t->unsigned int
            ///bgra: uint8_t*
            ///width: int
            ///height: int
            ///stride: int
            ///output: uint8_t**
            [DllImportAttribute("webp", EntryPoint = "WebPEncodeLosslessBGRA")]
            public static extern UIntPtr WebPEncodeLosslessBGRA([InAttribute()] IntPtr bgra, int width, int height, int stride, ref IntPtr output);


            /// Return Type: int
            ///param0: WebPConfig*
            ///param1: WebPPreset->Anonymous_017d4167_f53e_4b3d_b029_592ff5c3f80b
            ///param2: float
            ///param3: int
            [DllImportAttribute("webp", EntryPoint = "WebPConfigInitInternal")]
            public static extern int WebPConfigInitInternal(ref WebPConfig param0, WebPPreset param1, float param2, int param3);


            /// Return Type: int
            ///config: WebPConfig*
            [DllImportAttribute("webp", EntryPoint = "WebPValidateConfig")]
            public static extern int WebPValidateConfig(ref WebPConfig config);


            /// Return Type: void
            ///writer: WebPMemoryWriter*
            [DllImportAttribute("webp", EntryPoint = "WebPMemoryWriterInit")]
            public static extern void WebPMemoryWriterInit(ref WebPMemoryWriter writer);


            /// Return Type: int
            ///data: uint8_t*
            ///data_size: size_t->unsigned int
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPMemoryWrite")]
            public static extern int WebPMemoryWrite([InAttribute()] IntPtr data, UIntPtr data_size, ref WebPPicture picture);


            /// Return Type: int
            ///param0: WebPPicture*
            ///param1: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureInitInternal")]
            public static extern int WebPPictureInitInternal(ref WebPPicture param0, int param1);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureAlloc")]
            public static extern int WebPPictureAlloc(ref WebPPicture picture);


            /// Return Type: void
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureFree")]
            public static extern void WebPPictureFree(ref WebPPicture picture);


            /// Return Type: int
            ///src: WebPPicture*
            ///dst: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureCopy")]
            public static extern int WebPPictureCopy(ref WebPPicture src, ref WebPPicture dst);


            /// Return Type: int
            ///pic1: WebPPicture*
            ///pic2: WebPPicture*
            ///metric_type: int
            ///result: float* result[5]
            ///

            /// <summary>
            /// Compute PSNR, SSIM or LSIM distortion metric between two pictures.
            /// Result is in dB, stores in result[] in the Y/U/V/Alpha/All order.
            /// Returns false in case of error (src and ref don't have same dimension, ...)
            /// Warning: this function is rather CPU-intensive.
            /// </summary>
            /// <param name="src"></param>
            /// <param name="reference"></param>
            /// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
            /// <param name="result"></param>
            /// <returns></returns>
            [DllImportAttribute("webp", EntryPoint = "WebPPictureDistortion")]
            public static extern int WebPPictureDistortion(ref WebPPicture src, ref WebPPicture reference, int metric_type, ref float result);




            /// Return Type: int
            ///picture: WebPPicture*
            ///left: int
            ///top: int
            ///width: int
            ///height: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureCrop")]
            public static extern int WebPPictureCrop(ref WebPPicture picture, int left, int top, int width, int height);


            /// Return Type: int
            ///src: WebPPicture*
            ///left: int
            ///top: int
            ///width: int
            ///height: int
            ///dst: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureView")]
            public static extern int WebPPictureView(ref WebPPicture src, int left, int top, int width, int height, ref WebPPicture dst);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureIsView")]
            public static extern int WebPPictureIsView(ref WebPPicture picture);


            /// Return Type: int
            ///pic: WebPPicture*
            ///width: int
            ///height: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureRescale")]
            public static extern int WebPPictureRescale(ref WebPPicture pic, int width, int height);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgb: uint8_t*
            ///rgb_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportRGB")]
            public static extern int WebPPictureImportRGB(ref WebPPicture picture, [InAttribute()] IntPtr rgb, int rgb_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgba: uint8_t*
            ///rgba_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportRGBA")]
            public static extern int WebPPictureImportRGBA(ref WebPPicture picture, [InAttribute()] IntPtr rgba, int rgba_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///rgbx: uint8_t*
            ///rgbx_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportRGBX")]
            public static extern int WebPPictureImportRGBX(ref WebPPicture picture, [InAttribute()] IntPtr rgbx, int rgbx_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgr: uint8_t*
            ///bgr_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportBGR")]
            public static extern int WebPPictureImportBGR(ref WebPPicture picture, [InAttribute()] IntPtr bgr, int bgr_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgra: uint8_t*
            ///bgra_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportBGRA")]
            public static extern int WebPPictureImportBGRA(ref WebPPicture picture, [InAttribute()] IntPtr bgra, int bgra_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///bgrx: uint8_t*
            ///bgrx_stride: int
            [DllImportAttribute("webp", EntryPoint = "WebPPictureImportBGRX")]
            public static extern int WebPPictureImportBGRX(ref WebPPicture picture, [InAttribute()] IntPtr bgrx, int bgrx_stride);


            /// Return Type: int
            ///picture: WebPPicture*
            ///colorspace: WebPEncCSP->Anonymous_84ce7065_fe91_48b4_93d8_1f0e84319dba
            [DllImportAttribute("webp", EntryPoint = "WebPPictureARGBToYUVA")]
            public static extern int WebPPictureARGBToYUVA(ref WebPPicture picture, WebPEncCSP colorspace);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureYUVAToARGB")]
            public static extern int WebPPictureYUVAToARGB(ref WebPPicture picture);


            /// Return Type: void
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPCleanupTransparentArea")]
            public static extern void WebPCleanupTransparentArea(ref WebPPicture picture);


            /// Return Type: int
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPPictureHasTransparency")]
            public static extern int WebPPictureHasTransparency(ref WebPPicture picture);


            /// Return Type: int
            ///config: WebPConfig*
            ///picture: WebPPicture*
            [DllImportAttribute("webp", EntryPoint = "WebPEncode")]
            public static extern int WebPEncode(ref WebPConfig config, ref WebPPicture picture);
        }

        #endregion

        // Some useful macros:

        /// <summary>
        /// Returns true if the specified mode uses a premultiplied alpha
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool WebPIsPremultipliedMode(WEBP_CSP_MODE mode)
        {

            return (mode == WEBP_CSP_MODE.MODE_rgbA || mode == WEBP_CSP_MODE.MODE_bgrA || mode == WEBP_CSP_MODE.MODE_Argb ||
                mode == WEBP_CSP_MODE.MODE_rgbA_4444);

        }

        /// <summary>
        /// Returns true if the given mode is RGB(A)
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool WebPIsRGBMode(WEBP_CSP_MODE mode)
        {

            return (mode < WEBP_CSP_MODE.MODE_YUV);

        }


        /// <summary>
        /// Returns true if the given mode has an alpha channel
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool WebPIsAlphaMode(WEBP_CSP_MODE mode)
        {

            return (mode == WEBP_CSP_MODE.MODE_RGBA || mode == WEBP_CSP_MODE.MODE_BGRA || mode == WEBP_CSP_MODE.MODE_ARGB ||
                    mode == WEBP_CSP_MODE.MODE_RGBA_4444 || mode == WEBP_CSP_MODE.MODE_YUVA ||
                    WebPIsPremultipliedMode(mode));

        }



        // 

        /// <summary>
        /// Retrieve features from the bitstream. The *features structure is filled
        /// with information gathered from the bitstream.
        /// Returns false in case of error or version mismatch.
        /// In case of error, features->bitstream_status will reflect the error code.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="data_size"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static VP8StatusCode WebPGetFeatures(IntPtr data, UIntPtr data_size, ref WebPBitstreamFeatures features)
        {
            return NativeBindings.WebPGetFeaturesInternal(data, data_size, ref features, WEBP_DECODER_ABI_VERSION);

        }
        /// <summary>
        /// Initialize the configuration as empty. This function must always be
        /// called first, unless WebPGetFeatures() is to be called.
        /// Returns false in case of mismatched version.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static int WebPInitDecoderConfig(ref WebPDecoderConfig config)
        {

            return WebPInitDecoderConfigInternal(ref config, WEBP_DECODER_ABI_VERSION);

        }


        /// <summary>
        /// Initialize the structure as empty. Must be called before any other use. Returns false in case of version mismatch
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int WebPInitDecBuffer(ref WebPDecBuffer buffer)
        {
            return WebPInitDecBufferInternal(ref buffer, WEBP_DECODER_ABI_VERSION);
        }



        //    // Deprecated alpha-less version of WebPIDecGetYUVA(): it will ignore the

        //// alpha information (if present). Kept for backward compatibility.

        //public IntPtr WebPIDecGetYUV(IntPtr decoder, int* last_y, uint8_t** u, uint8_t** v,

        //    int* width, int* height, int* stride, int* uv_stride) {

        //  return WebPIDecGetYUVA(idec, last_y, u, v, NULL, width, height,

        //                         stride, uv_stride, NULL);

        /// <summary>
        /// Should always be called, to initialize a fresh WebPConfig structure before
        /// modification. Returns false in case of version mismatch. WebPConfigInit()
        /// must have succeeded before using the 'config' object.
        /// Note that the default values are lossless=0 and quality=75.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static int WebPConfigInit(ref WebPConfig config)
        {
            return NativeBindings.WebPConfigInitInternal(ref config, WebPPreset.WEBP_PRESET_DEFAULT, 75.0f, WEBP_ENCODER_ABI_VERSION);
        }

        /// <summary>
        /// This function will initialize the configuration according to a predefined
        /// set of parameters (referred to by 'preset') and a given quality factor.
        /// This function can be called as a replacement to WebPConfigInit(). Will return false in case of error.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="preset"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static int WebPConfigPreset(ref WebPConfig config, WebPPreset preset, float quality)
        {
            return NativeBindings.WebPConfigInitInternal(ref config, preset, quality, WEBP_ENCODER_ABI_VERSION);
        }

        /// <summary>
        /// Should always be called, to initialize the structure. Returns false in case
        /// of version mismatch. WebPPictureInit() must have succeeded before using the
        /// 'picture' object.
        /// Note that, by default, use_argb is false and colorspace is WEBP_YUV420.
        /// </summary>
        /// <param name="picture"></param>
        /// <returns></returns>
        public static int WebPPictureInit(ref WebPPicture picture)
        {

            return NativeBindings.WebPPictureInitInternal(ref picture, WEBP_ENCODER_ABI_VERSION);

        }
    }
}

#pragma warning restore 1591
