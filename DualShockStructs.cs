using DSRemapper.Core.Types;
using System.Collections.Specialized;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DSRemapper.DualShock
{
    interface IDS4InReport
    {
        public BasicInState Basic { get; set; }
        public ExtendedInState Extended { get; set; }
        public TouchStatus Touch { get; set; }
        public TouchStatus[] Touches { get; set; }
        public byte[] CRC { get; set; }
    }
    interface IDS4OutReport
    {
        public SetStateData State { get; set; }
        public byte[] CRC { get; set; }
    }

    #region Input
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public struct USBStatus : IDS4InReport
    {
        private byte reportId = 0;
        private BasicInState basicState = new();
        private ExtendedInState extendedState = new();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private TouchStatus[] touchStatus = new TouchStatus[3];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private byte[] padding = new byte[3];

        public byte ReportId { get => reportId; set => reportId = value; }
        public BasicInState Basic { get => basicState; set => basicState = value; }
        public ExtendedInState Extended { get => extendedState; set => extendedState = value; }
        public TouchStatus Touch { get => touchStatus[0]; set => touchStatus[0] = value; }
        public TouchStatus[] Touches { get => touchStatus; set => touchStatus = value; }
        public byte[] CRC { get => []; set => _ = value; }
        public USBStatus() { }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 78)]
    public struct BTStatus : IDS4InReport
    {
        private byte reportId = 0;
        private BitVector<byte> misc1 = new();
        private BitVector<byte> misc2 = new();
        private BasicInState basicState = new();
        private ExtendedInState extendedState = new();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private TouchStatus[] touchStatus = new TouchStatus[4];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private byte[] padding = new byte[2];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] crc = new byte[4];

        public byte ReportId { get => reportId; set => reportId = value; }
        public byte PollingRate { get => misc1[0x3F, 0]; set => misc1[0x3F, 0] = value; }
        public bool EnableCRC { get => misc1[0x40]; set => misc1[0x40] = value; }
        public bool EnableHID { get => misc1[0x80]; set => misc1[0x80] = value; }
        public byte EnableMic { get => misc2[0x07, 0]; set => misc2[0x07, 0] = value; }
        public byte Unk { get => misc2[0x0F, 3]; set => misc2[0x0F, 3] = value; }
        public bool EnableAudio { get => misc2[0x80]; set => misc2[0x80] = value; }
        public BasicInState Basic { get => basicState; set => basicState = value; }
        public ExtendedInState Extended { get => extendedState; set => extendedState = value; }
        public TouchStatus Touch { get => touchStatus[0]; set => touchStatus[0] = value; }
        public TouchStatus[] Touches { get => touchStatus; set => touchStatus = value; }
        public byte[] CRC { get => crc; set => crc = value; }

        public BTStatus() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 9)]
    public struct BasicInState
    {
        private byte lx = 0;
        private byte ly = 0;
        private byte rx = 0;
        private byte ry = 0;
        private BitVector<ushort> buttons = new();
        private BitVector<byte> misc = new();
        private byte lTrigger = 0;
        private byte rTrigger = 0;

        public sbyte LX { get => lx.AxisConvertion(); set => lx = value.AxisConvertion(); }
        public sbyte LY { get => ly.AxisConvertion(); set => ly = value.AxisConvertion(); }
        public sbyte RX { get => rx.AxisConvertion(); set => rx = value.AxisConvertion(); }
        public sbyte RY { get => ry.AxisConvertion(); set => ry = value.AxisConvertion(); }
        public byte LTrigger { get => lTrigger; set => lTrigger = value; }
        public byte RTrigger { get => rTrigger; set => rTrigger = value; }

        public byte DPad { get => (byte)buttons[0x000F, 0]; set => buttons[0x000F, 0] = value; }
        public bool Square { get => buttons[0x0010]; set => buttons[0x0010] = value; }
        public bool Cross { get => buttons[0x0020]; set => buttons[0x0020] = value; }
        public bool Circle { get => buttons[0x0040]; set => buttons[0x0040] = value; }
        public bool Triangle { get => buttons[0x0080]; set => buttons[0x0080] = value; }
        public bool L1 { get => buttons[0x0100]; set => buttons[0x0100] = value; }
        public bool R1 { get => buttons[0x0200]; set => buttons[0x0200] = value; }
        public bool L2 { get => buttons[0x0400]; set => buttons[0x0400] = value; }
        public bool R2 { get => buttons[0x0800]; set => buttons[0x0800] = value; }
        public bool Share { get => buttons[0x1000]; set => buttons[0x1000] = value; }
        public bool Options { get => buttons[0x2000]; set => buttons[0x2000] = value; }
        public bool L3 { get => buttons[0x4000]; set => buttons[0x4000] = value; }
        public bool R3 { get => buttons[0x8000]; set => buttons[0x8000] = value; }
        public bool PS { get => misc[0x01]; set => misc[0x01] = value; }
        public bool TPad { get => misc[0x02]; set => misc[0x02] = value; }
        public byte Counter { get => misc[0x3F, 2]; set => misc[0x3F, 2] = value; }
        public BasicInState() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 23)]
    public struct ExtendedInState
    {
        private ushort timestamp = 0;
        private byte temperature = 0;
        private short angularVelocityX = 0;
        private short angularVelocityY = 0;
        private short angularVelocityZ = 0;
        private short accelerometerX = 0;
        private short accelerometerY = 0;
        private short accelerometerZ = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        private byte[] extData = new byte[5];
        private BitVector<byte> misc = new();
        private BitVector<ushort> unk = new();
        private byte touchCount = 0;

        public ushort Timestamp { get => timestamp; set => timestamp = value; }
        public byte Temperature { get => temperature; set => temperature = value; }
        public short AngularVelocityX { get => angularVelocityX; set => angularVelocityX = value; }
        public short AngularVelocityY { get => angularVelocityY; set => angularVelocityY = value; }
        public short AngularVelocityZ { get => angularVelocityZ; set => angularVelocityZ = value; }
        public short AccelerometerX { get => accelerometerX; set => accelerometerX = value; }
        public short AccelerometerY { get => accelerometerY; set => accelerometerY = value; }
        public short AccelerometerZ { get => accelerometerZ; set => accelerometerZ = value; }
        public byte[] ExtData { get => extData; set => extData = value; }
        public byte Battery { get => misc[0x0F, 0]; set => misc[0x0F, 0] = value; }
        public bool USB { get => misc[0x10]; set => misc[0x10] = value; }
        public bool Headphone { get => misc[0x20]; set => misc[0x20] = value; }
        public bool Mic { get => misc[0x40]; set => misc[0x40] = value; }
        public bool Ext { get => misc[0x80]; set => misc[0x80] = value; }
        public bool UnkExt1 { get => unk[0x0001]; set => unk[0x0001] = value; }
        public bool UnkExt2 { get => unk[0x0002]; set => unk[0x0002] = value; }
        public bool NotConnected { get => unk[0x0004]; set => unk[0x0004] = value; }
        public ushort Unk { get => unk[0x1FFF, 3]; set => unk[0x1FFF, 3] = value; }
        public byte TouchPackCount { get => touchCount; set => touchCount = value; }

        public ExtendedInState() { }

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FingerStatus
    {
        private BitVector<uint> fingerData = new();

        private static readonly BitVector<uint>.Section fingerId = BitVector<uint>.CreateSection(0x7F);
        private static readonly BitVector<uint>.Section fingerX = BitVector<uint>.CreateSection(0xFFF, 8);
        private static readonly BitVector<uint>.Section fingerY = BitVector<uint>.CreateSection(0xFFF, fingerX);

        public byte FingerId { get => (byte)fingerData[fingerId]; set => fingerData[fingerId] = value; }
        public bool FingerTouch { get => !fingerData[0x80]; set => fingerData[0x80] = !value; }
        public short FingerX { get => (short)fingerData[fingerX]; set => fingerData[fingerX] = (uint)value; }
        public short FingerY { get => (short)fingerData[fingerY]; set => fingerData[fingerY] = (uint)value; }
        public FingerStatus() { }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TouchStatus
    {
        private byte timestamp = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private FingerStatus[] fingers = new FingerStatus[2];

        public byte Timestamp { get => timestamp; set => timestamp = value; }
        public FingerStatus[] Fingers { get => fingers; set => fingers = value; }
        public TouchStatus() { }
    }

    #endregion Input

    #region Output
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
    public struct USBOutReport : IDS4OutReport
    {
        private byte reportId = 0x05;
        SetStateData state = new();

        public SetStateData State { get => state; set => state = value; }
        public byte[] CRC { get => []; set => _ = value; }

        public USBOutReport() { }
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 78)]
    public struct BTOutReport : IDS4OutReport
    {
        [FieldOffset(0)]
        private byte reportId = 0x11;
        [FieldOffset(1)]
        private BitVector<byte> misc1;
        [FieldOffset(2)]
        private BitVector<byte> misc2;
        [FieldOffset(3)]
        SetStateData state = new();
        [FieldOffset(74)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] crc = new byte[4];

        public byte PollingRate { get => misc1[0x3F, 0]; set => misc1[0x3F, 0] = value; }
        public bool EnableCRC { get => misc1[0x40]; set => misc1[0x40] = value; }
        public bool EnableHID { get => misc1[0x80]; set => misc1[0x80] = value; }
        public byte EnableMic { get => misc2[0x07, 0]; set => misc2[0x07, 0] = value; }
        public bool UnkA4 { get => misc2[0x08]; set => misc2[0x08] = value; }
        public bool UnkB1 { get => misc2[0x10]; set => misc2[0x10] = value; }
        public bool UnkB2 { get => misc2[0x20]; set => misc2[0x20] = value; }
        public bool UnkB3 { get => misc2[0x40]; set => misc2[0x40] = value; }
        public bool EnableAudio { get => misc2[0x80]; set => misc2[0x80] = value; }
        public SetStateData State { get => state; set => state = value; }
        public byte[] CRC { get => crc; set => crc = value; }

        public BTOutReport() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SetStateData
    {
        private BitVector<byte> options;
        private BitVector<byte> unk1;
        private byte empty1;
        private byte rumbleRight;
        private byte rumbleLeft;
        private byte red;
        private byte green;
        private byte blue;
        private byte flashOn;
        private byte flashOff;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private byte[] extData = new byte[8];
        private byte volumeLeft;
        private byte volumeRight;
        private byte volumeMic;
        private byte volumeSpeaker;
        private BitVector<byte> unkAudio;

        public bool EnableRumbleUpdate { get => options[0x01]; set => options[0x01] = value; }
        public bool EnableLedUpdate { get => options[0x02]; set => options[0x02] = value; }
        public bool EnableLedBlink { get => options[0x04]; set => options[0x04] = value; }
        public bool EnableExtWrite { get => options[0x08]; set => options[0x08] = value; }
        public bool EnableVolumeLeftUpdate { get => options[0x10]; set => options[0x10] = value; }
        public bool EnableVolumeRightUpdate { get => options[0x20]; set => options[0x20] = value; }
        public bool EnableVolumeMicUpdate { get => options[0x40]; set => options[0x40] = value; }
        public bool EnableVolumeSpeakerUpdate { get => options[0x80]; set => options[0x80] = value; }

        public bool UnkReset1 { get => unk1[0x01]; set => unk1[0x01] = value; }
        public bool UnkReset2 { get => unk1[0x02]; set => unk1[0x02] = value; }
        public bool Unk1 { get => unk1[0x04]; set => unk1[0x04] = value; }
        public bool Unk2 { get => unk1[0x08]; set => unk1[0x08] = value; }
        public bool Unk3 { get => unk1[0x10]; set => unk1[0x10] = value; }
        public byte UnkPad1 { get => unk1[0x07, 5]; set => unk1[0x07, 5] = value; }
        
        public byte Empty1 { get => empty1; set => empty1 = value; }

        public byte RumbleRight { get => rumbleRight; set => rumbleRight = value; }
        public byte RumbleLeft { get => rumbleLeft; set => rumbleLeft = value; }
        public byte Red { get=> red; set => red = value; }
        public byte Green { get=> green; set => green = value; }
        public byte Blue { get => blue; set => blue = value; }
        public byte FlashOn { get=> flashOn; set => flashOn = value; }
        public byte FlashOff { get=> flashOff; set => flashOff = value; }

        public byte[] ExtData { get=> extData; set => extData = value; }
        public byte VolumeLeft { get=> volumeLeft; set => volumeLeft = value; }
        public byte VolumeRight { get=> volumeRight; set => volumeRight = value; }
        public byte VolumeMic { get=> volumeMic; set => volumeMic = value; }
        public byte VolumeSpeaker { get=> volumeSpeaker; set => volumeSpeaker = value; }

        public byte UnkAudio1 { get => unkAudio[0x7F, 0]; set => unkAudio[0x7F, 0] = value; }
        public bool UnkAudio2 { get => unkAudio[0x80]; set => unkAudio[0x80] = value; }

        public SetStateData() { }
    }
    #endregion Output
}
