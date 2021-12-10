﻿using ATL.AudioData;
using ATL.Logging;
using Commons;
using HashDepot;
using System;
using System.IO;
using static ATL.AudioData.MetaDataIOFactory;

namespace ATL
{
    /// <summary>
    /// Information about an embedded picture
    /// </summary>
    public class PictureInfo
    {
        /// <summary>
        /// Type of the embedded picture
        /// </summary>
        public enum PIC_TYPE
        {
            /// <summary>
            /// Unsupported (i.e. none of the supported values in the enum)
            /// </summary>
            Unsupported = 99,
            /// <summary>
            /// Generic
            /// </summary>
            Generic = 1,
            /// <summary>
            /// Front cover
            /// </summary>
            Front = 2,
            /// <summary>
            /// Back cover
            /// </summary>
            Back = 3,
            /// <summary>
            /// Media (e.g. label side of CD)
            /// </summary>
            CD = 4,
            /// <summary>
            /// File icon
            /// </summary>
            Icon = 5,
            /// <summary>
            /// Leaflet
            /// </summary>
            Leaflet = 6,
            /// <summary>
            /// Lead artist/lead performer/soloist
            /// </summary>
            LeadArtist = 7,
            /// <summary>
            /// Artist/performer
            /// </summary>
            Artist = 8,
            /// <summary>
            /// Conductor
            /// </summary>
            Conductor = 9,
            /// <summary>
            /// Band/Orchestra
            /// </summary>
            Band = 10,
            /// <summary>
            /// Composer
            /// </summary>
            Composer = 11,
            /// <summary>
            /// Lyricist/text writer
            /// </summary>
            Lyricist = 12,
            /// <summary>
            /// Recording location
            /// </summary>
            RecordingLocation = 13,
            /// <summary>
            /// During recording
            /// </summary>
            DuringRecording = 14,
            /// <summary>
            /// During performance
            /// </summary>
            DuringPerformance = 15,
            /// <summary>
            /// Movie/video screen capture
            /// </summary>
            MovieCapture = 16,
            /// <summary>
            /// A bright, coloured fish
            /// </summary>
            Fishie = 17,
            /// <summary>
            /// Illustration
            /// </summary>
            Illustration = 18,
            /// <summary>
            /// Band/artist logotype
            /// </summary>
            BandLogo = 19,
            /// <summary>
            /// Publisher/Studio logotype
            /// </summary>
            PublisherLogo = 20
        };

        /// <summary>
        /// Normalized picture type (see enum)
        /// </summary>
        public PIC_TYPE PicType;
        /// <summary>
        /// Native image format
        /// </summary>
        public ImageFormat NativeFormat;
        /// <summary>
        /// Position of the picture among pictures of the same generic type / native code (default 1 if the picture is one of its kind)
        /// </summary>
        public int Position;

        /// <summary>
        /// Tag type where the picture originates from
        /// </summary>
        public TagType TagType;
        /// <summary>
        /// Native picture code according to TagType convention (numeric : e.g. ID3v2)
        /// </summary>
        public int NativePicCode;
        /// <summary>
        /// Native picture code according to TagType convention (string : e.g. APEtag)
        /// </summary>
        public string NativePicCodeStr;

        /// <summary>
        /// Picture description
        /// </summary>
        public string Description = "";

        /// <summary>
        /// Binary picture data
        /// </summary>
        public byte[] PictureData { get; private set; }

        /// <summary>
        /// Hash of binary picture data
        /// </summary>
        public uint PictureHash;

        /// <summary>
        /// True if the field has to be deleted in the next IMetaDataIO.Write operation
        /// </summary>
        public bool MarkedForDeletion = false;
        /// <summary>
        /// Freeform transient value to be used by other parts of the library
        /// </summary>
        public int TransientFlag;

        /// <summary>
        /// Get the MIME-type associated with the picture
        /// </summary>
        public string MimeType
        {
            get { return ImageUtils.GetMimeTypeFromImageFormat(NativeFormat); }
        }


        // ---------------- STATIC CONSTRUCTORS

        /// <summary>
        /// Construct picture information from its raw, binary data
        /// </summary>
        /// <param name="data">Raw picture data</param>
        /// <param name="picType">Type of the picture (default : Generic)</param>
        /// <param name="tagType">Type of the containing tag (default : TAG_ANY)</param>
        /// <param name="nativePicCode">Native code of the picture, as stated in its containing format's specs (default : not set)</param>
        /// <param name="position">Position of the picture among the other pictures of the same file (default : 1)</param>
        /// <returns></returns>
        public static PictureInfo fromBinaryData(byte[] data, PIC_TYPE picType = PIC_TYPE.Generic, TagType tagType = TagType.ANY, object nativePicCode = null, int position = 1)
        {
            if (null == data || data.Length < 3) throw new ArgumentException("Data should not be null and be at least 3 bytes long");
            if (null == nativePicCode) nativePicCode = 0; // Can't default with 0 in params declaration

            return new PictureInfo(picType, tagType, nativePicCode, position, data);
        }

        /// <summary>
        /// Construct picture information from its raw, binary data
        /// </summary>
        /// <param name="stream">Stream containing raw picture data, positioned at the beginning of picture data</param>
        /// <param name="length">Length of the picture data to read inside the given stream</param>
        /// <param name="picType">Type of the picture (default : Generic)</param>
        /// <param name="tagType">Type of the containing tag (default : TAG_ANY)</param>
        /// <param name="nativePicCode">Native code of the picture, as stated in its containing format's specs (default : not set)</param>
        /// <param name="position">Position of the picture among the other pictures of the same file (default : 1)</param>
        /// <returns></returns>
        public static PictureInfo fromBinaryData(Stream stream, int length, PIC_TYPE picType, TagType tagType, object nativePicCode, int position = 1)
        {
            if (null == stream || length < 3) throw new ArgumentException("Stream should not be null and be at least 3 bytes long");

            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            return new PictureInfo(picType, tagType, nativePicCode, position, data);
        }

        // ---------------- CONSTRUCTORS

        /// <summary>
        /// Construct picture information by copying data from another PictureInfo object
        /// </summary>
        /// <param name="picInfo">PictureInfo object to copy data from</param>
        /// <param name="copyPictureData">If true, copy raw picture data; if false only take its reference from the given PictureInfo</param>
        public PictureInfo(PictureInfo picInfo, bool copyPictureData = true)
        {
            this.PicType = picInfo.PicType;
            this.NativeFormat = picInfo.NativeFormat;
            this.Position = picInfo.Position;
            this.TagType = picInfo.TagType;
            this.NativePicCode = picInfo.NativePicCode;
            this.NativePicCodeStr = picInfo.NativePicCodeStr;
            this.Description = picInfo.Description;
            if (copyPictureData && picInfo.PictureData != null)
            {
                PictureData = new byte[picInfo.PictureData.Length];
                picInfo.PictureData.CopyTo(PictureData, 0);
            }
            else
            {
                this.PictureData = picInfo.PictureData;
            }
            this.PictureHash = picInfo.PictureHash;
            this.MarkedForDeletion = picInfo.MarkedForDeletion;
            this.TransientFlag = picInfo.TransientFlag;
        }

        /// <summary>
        /// Construct picture information from its parts
        /// </summary>
        /// <param name="picType">Type of the picture</param>
        /// <param name="tagType">Type of the containing tag</param>
        /// <param name="nativePicCode">Native code of the picture, as stated in its containing format's specs</param>
        /// <param name="position">Position of the picture among the other pictures of the same file</param>
        /// <param name="binaryData">Raw binary data of the picture</param>
        private PictureInfo(PIC_TYPE picType, TagType tagType, object nativePicCode, int position, byte[] binaryData)
        {
            PicType = picType;
            TagType = tagType;
            Position = position;

            string picCodeStr = nativePicCode as string;
            if (picCodeStr != null)
            {
                NativePicCodeStr = picCodeStr;
                NativePicCode = -1;
            }
            else if (nativePicCode is byte)
            {
                NativePicCode = (byte)nativePicCode;
            }
            else if (nativePicCode is int)
            {
                NativePicCode = (int)nativePicCode;
            }
            else
            {
                LogDelegator.GetLogDelegate()(Log.LV_WARNING, "nativePicCode type is not supported; expected byte, int or string; found " + nativePicCode.GetType().Name);
            }
            PictureData = binaryData;
            NativeFormat = ImageUtils.GetImageFormatFromPictureHeader(PictureData);
        }

        /// <summary>
        /// Construct picture information from its parts
        /// </summary>
        /// <param name="picType">Type of the picture</param>
        /// <param name="position">Position of the picture among the other pictures of the same file (default : 1)</param>
        public PictureInfo(PIC_TYPE picType, int position = 1)
        {
            PicType = picType;
            NativeFormat = ImageFormat.Undefined;
            Position = position;
        }

        /// <summary>
        /// Construct picture information from its parts
        /// </summary>
        /// <param name="tagType">Type of the containing tag</param>
        /// <param name="nativePicCode">Native code of the picture, as stated in its containing format's specs</param>
        /// <param name="position">Position of the picture among the other pictures of the same file (default : 1)</param>
        public PictureInfo(TagType tagType, object nativePicCode, int position = 1)
        {
            PicType = PIC_TYPE.Unsupported;
            NativeFormat = ImageFormat.Undefined;
            TagType = tagType;
            Position = position;

            string picCodeStr = nativePicCode as string;
            if (picCodeStr != null)
            {
                NativePicCodeStr = picCodeStr;
                NativePicCode = -1;
            }
            else if (nativePicCode is byte)
            {
                NativePicCode = (byte)nativePicCode;
            }
            else if (nativePicCode is int)
            {
                NativePicCode = (int)nativePicCode;
            }
            else
            {
                LogDelegator.GetLogDelegate()(Log.LV_WARNING, "nativePicCode type is not supported; expected byte, int or string; found " + nativePicCode.GetType().Name);
            }
        }

        /// <summary>
        /// Calculate the hash of the raw, binary data of this picture, using FNV-1a
        /// </summary>
        /// <returns>FNV-1a hash of the raw binary data</returns>
        public uint ComputePicHash()
        {
            uint result = 0;
            if (PictureData != null) result = FNV1a.Hash32(PictureData);
            PictureHash = result;
            return PictureHash;
        }

        // TODO doc
        public bool EqualsProper(PictureInfo picInfo)
        {
            return Position == picInfo.Position && (equalsNative(picInfo) || equalsGeneric(picInfo));
        }

        private bool equalsNative(PictureInfo picInfo)
        {
            if (0 == TagType || TagType != picInfo.TagType) return false;
            if (NativePicCode > 0 && NativePicCode == picInfo.NativePicCode) return true;
            if (NativePicCodeStr != null && NativePicCodeStr.Length > 0 && NativePicCodeStr == picInfo.NativePicCodeStr) return true;
            return false;
        }

        private bool equalsGeneric(PictureInfo picInfo)
        {
            return (PIC_TYPE.Unsupported != PicType && PicType == picInfo.PicType);
        }

        // ---------------- OVERRIDES FOR DICTIONARY STORING & UTILS

        /// <summary>
        /// Return the string representation of the object
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            return Utils.BuildStrictLengthString(Position.ToString(), 2, '0', false) + valueToString();
        }

        private string valueToString()
        {
            if (NativePicCode > 0 && TagType > 0)
                return ((10000000 * ((int)TagType)) + "N" + NativePicCode).ToString();
            else if (NativePicCodeStr != null && NativePicCodeStr.Length > 0 && TagType > 0)
                return (10000000 * ((int)TagType)).ToString() + "N" + NativePicCodeStr;
            else if (PicType != PIC_TYPE.Unsupported)
                return "T" + Utils.BuildStrictLengthString(((int)PicType).ToString(), 2, '0', false); // TagType doesn't matter if we're working with generic picture codes
            else
                LogDelegator.GetLogDelegate()(Log.LV_WARNING, "Non-supported picture detected, but no native picture code found");

            return "";
        }

        /// <summary>
        /// Return the hash of the object
        /// </summary>
        /// <returns>Hash of the object</returns>
        public override int GetHashCode()
        {
            return (int)FNV1a.Hash32(Utils.Latin1Encoding.GetBytes(ToString()));
        }

        /// <summary>
        /// Equals override
        /// </summary>
        /// <param name="obj">Object to test comparison with</param>
        /// <returns>Result of the comparison</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            // Actually check the type, should not throw exception from Equals override
            if (obj.GetType() != this.GetType()) return false;

            // Call the implementation from IEquatable
            return this.ToString().Equals(obj.ToString());
        }
    }
}
