using System;
using System.Runtime.InteropServices;

//From https://stackoverflow.com/questions/16082330/communicating-with-windows7-display-api via https://stackoverflow.com/questions/22258906/how-to-detect-duplicated-monitors-as-separate-screens

namespace PauseToScreen
{
    /// <summary>
    /// This class takes care of wrapping "Connecting and Configuring Displays(CCD) Win32 API"
    /// Author Erti-Chris Eelmaa || easter199 at hotmail dot com
    /// </summary>
    public class CCDWrapper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public uint HighPart;
        }

        [Flags]
        public enum DisplayConfigVideoOutputTechnology : uint
        {
            Other = 4294967295, // -1
            Hd15 = 0,
            Svideo = 1,
            CompositeVideo = 2,
            ComponentVideo = 3,
            Dvi = 4,
            Hdmi = 5,
            Lvds = 6,
            DJpn = 8,
            Sdi = 9,
            DisplayportExternal = 10,
            DisplayportEmbedded = 11,
            UdiExternal = 12,
            UdiEmbedded = 13,
            Sdtvdongle = 14,
            Internal = 0x80000000,
            ForceUint32 = 0xFFFFFFFF
        }

        #region SdcFlags enum

        [Flags]
        public enum SdcFlags : uint
        {
            Zero = 0,

            TopologyInternal = 0x00000001,
            TopologyClone = 0x00000002,
            TopologyExtend = 0x00000004,
            TopologyExternal = 0x00000008,
            TopologySupplied = 0x00000010,

            UseSuppliedDisplayConfig = 0x00000020,
            Validate = 0x00000040,
            Apply = 0x00000080,
            NoOptimization = 0x00000100,
            SaveToDatabase = 0x00000200,
            AllowChanges = 0x00000400,
            PathPersistIfRequired = 0x00000800,
            ForceModeEnumeration = 0x00001000,
            AllowPathOrderChanges = 0x00002000,

            UseDatabaseCurrent = TopologyInternal | TopologyClone | TopologyExtend | TopologyExternal
        }

        [Flags]
        public enum DisplayConfigFlags : uint
        {
            Zero = 0x0,
            PathActive = 0x00000001
        }

        [Flags]
        public enum DisplayConfigSourceStatus
        {
            Zero = 0x0,
            InUse = 0x00000001
        }

        [Flags]
        public enum DisplayConfigTargetStatus : uint
        {
            Zero = 0x0,

            InUse                         = 0x00000001,
            FORCIBLE                       = 0x00000002,
            ForcedAvailabilityBoot       = 0x00000004,
            ForcedAvailabilityPath       = 0x00000008,
            ForcedAvailabilitySystem     = 0x00000010,
        }

        [Flags]
        public enum DisplayConfigRotation : uint
        {
            Zero = 0x0,

            Identity = 1,
            Rotate90 = 2,
            Rotate180 = 3,
            Rotate270 = 4,
            ForceUint32 = 0xFFFFFFFF
        }

        [Flags]
        public enum DisplayConfigPixelFormat : uint
        {
            Zero = 0x0,

            Pixelformat8Bpp = 1,
            Pixelformat16Bpp = 2,
            Pixelformat24Bpp = 3,
            Pixelformat32Bpp = 4,
            PixelformatNongdi = 5,
            PixelformatForceUint32 = 0xffffffff
        }

        [Flags]
        public enum DisplayConfigScaling : uint
        {
            Zero = 0x0, 

            Identity = 1,
            Centered = 2,
            Stretched = 3,
            Aspectratiocenteredmax = 4,
            Custom = 5,
            Preferred = 128,
            ForceUint32 = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigRational
        {
            public uint numerator;
            public uint denominator;
        }

        [Flags]
        public enum DisplayConfigScanLineOrdering : uint
        {
            Unspecified = 0,
            Progressive = 1,
            Interlaced = 2,
            InterlacedUpperfieldfirst = Interlaced,
            InterlacedLowerfieldfirst = 3,
            ForceUint32 = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathInfo
        {
            public DisplayConfigPathSourceInfo sourceInfo;
            public DisplayConfigPathTargetInfo targetInfo;
            public uint flags;
        }

        [Flags]
        public enum DisplayConfigModeInfoType : uint
        {
            Zero = 0,

            Source = 1,
            Target = 2,
            ForceUint32 = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DisplayConfigModeInfo
        {
            [FieldOffset((0))]
            public DisplayConfigModeInfoType infoType;

            [FieldOffset(4)]
            public uint id;

            [FieldOffset(8)]
            public LUID adapterId;

            [FieldOffset(16)]
            public DisplayConfigTargetMode targetMode;

            [FieldOffset(16)]
            public DisplayConfigSourceMode sourceMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfig2DRegion
        {
            public uint cx;
            public uint cy;
        }

        [Flags]
        public enum D3DmdtVideoSignalStandard : uint
        {
            Uninitialized = 0,
            VesaDmt = 1,
            VesaGtf = 2,
            VesaCvt = 3,
            Ibm = 4,
            Apple = 5,
            NtscM = 6,
            NtscJ = 7,
            Ntsc443 = 8,
            PalB = 9,
            PalB1 = 10,
            PalG = 11,
            PalH = 12,
            PalI = 13,
            PalD = 14,
            PalN = 15,
            PalNc = 16,
            SecamB = 17,
            SecamD = 18,
            SecamG = 19,
            SecamH = 20,
            SecamK = 21,
            SecamK1 = 22,
            SecamL = 23,
            SecamL1 = 24,
            Eia861 = 25,
            Eia861A = 26,
            Eia861B = 27,
            PalK = 28,
            PalK1 = 29,
            PalL = 30,
            PalM = 31,
            Other = 255
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigVideoSignalInfo
        {
            public long pixelRate;
            public DisplayConfigRational hSyncFreq;
            public DisplayConfigRational vSyncFreq;
            public DisplayConfig2DRegion activeSize;
            public DisplayConfig2DRegion totalSize;

            public D3DmdtVideoSignalStandard videoStandard;
            public DisplayConfigScanLineOrdering ScanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigTargetMode
        {
            public DisplayConfigVideoSignalInfo targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PointL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigSourceMode
        {
            public uint width;
            public uint height;
            public DisplayConfigPixelFormat pixelFormat;
            public PointL position;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathSourceInfo
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;

            public DisplayConfigSourceStatus statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathTargetInfo
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public DisplayConfigVideoOutputTechnology outputTechnology; 
            public DisplayConfigRotation rotation;
            public DisplayConfigScaling scaling;
            public DisplayConfigRational refreshRate;
            public DisplayConfigScanLineOrdering scanLineOrdering;

            public bool targetAvailable;
            public DisplayConfigTargetStatus statusFlags;
        }

        [Flags]
        public enum QueryDisplayFlags : uint
        {
            Zero = 0x0,

            AllPaths = 0x00000001,
            OnlyActivePaths = 0x00000002,
            DatabaseCurrent = 0x00000004
        }

        [Flags]
        public enum DisplayConfigTopologyId : uint
        {
            Zero = 0x0,

            Internal = 0x00000001,
            Clone = 0x00000002,
            Extend = 0x00000004,
            External = 0x00000008,
            ForceUint32 = 0xFFFFFFFF
        }


        #endregion

        [DllImport("User32.dll")]
        public static extern int SetDisplayConfig(
            uint numPathArrayElements, 
            [In] DisplayConfigPathInfo[] pathArray,
            uint numModeInfoArrayElements, 
            [In] DisplayConfigModeInfo[] modeInfoArray,
            SdcFlags flags
        );

        [DllImport("User32.dll")]
        public static extern int QueryDisplayConfig(
            QueryDisplayFlags flags, 
            ref int numPathArrayElements,
            [Out] DisplayConfigPathInfo[] pathInfoArray, 
            ref int modeInfoArrayElements,
            [Out] DisplayConfigModeInfo[] modeInfoArray,
            ref DisplayConfigTopologyId z
            //IntPtr z
        );
        [DllImport("User32.dll")]
        public static extern int GetDisplayConfigBufferSizes(QueryDisplayFlags flags, out int numPathArrayElements, out int numModeInfoArrayElements);
    }
}