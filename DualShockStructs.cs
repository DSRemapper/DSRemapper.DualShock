using DSRemapper.Core.Types;
using System.Collections.Specialized;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DSRemapper.DualShock
{
    interface IDS4Report
    {
        public BasicInState Basic { get; }
        public ExtendedInState Extended { get; }
        public TouchStatus Touch { get; }
        public TouchStatus[] Touches { get; }
        public byte[] CRC { get; }
    }

    #region Input
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public struct USBStatus : IDS4Report
    {
        public byte reportId=0;
        public BasicInState basicState = new();
        public ExtendedInState extendedState = new();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public TouchStatus[] touchStatus=new TouchStatus[3];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] padding = new byte[3];

        public byte ReportId { get => reportId; set => reportId = value; }
        public BasicInState Basic { get => basicState; set => basicState = value; }
        public ExtendedInState Extended { get => extendedState; set => extendedState = value; }
        public TouchStatus Touch { get => touchStatus[0]; set => touchStatus[0] = value;}
        public TouchStatus[] Touches { get => touchStatus; set => touchStatus = value; }
        public byte[] CRC { get => []; }
        public USBStatus() { }
    }
    [StructLayout(LayoutKind.Sequential,Pack =1, Size = 78)]
    public struct BTStatus : IDS4Report
    {
        public byte reportId=0;
        private BitVector<byte> misc1 = new();
        private BitVector<byte> misc2 = new();
        public BasicInState basicState = new();
        public ExtendedInState extendedState = new();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public TouchStatus[] touchStatus = new TouchStatus[4];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] padding=new byte[2];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] crc=new byte[4];

        public byte ReportId { get => reportId; set => reportId = value; }
        public byte PollingRate { get => misc1[0x3F,0]; set => misc1[0x3F, 0] = value; }
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

    [StructLayout(LayoutKind.Sequential,Pack = 1, Size = 9)]
    public struct BasicInState
	{
		private byte lx=0;
        private byte ly=0;
        private byte rx = 0;
        private byte ry = 0;
        private BitVector<ushort> buttons = new();
        private BitVector<byte> misc = new();
        private byte lTrigger=0;
        private byte rTrigger = 0;

		public sbyte LX { get => lx.AxisConvertion(); set => lx = value.AxisConvertion(); }
        public sbyte LY { get => ly.AxisConvertion(); set => ly = value.AxisConvertion(); }
        public sbyte RX { get => rx.AxisConvertion(); set => rx = value.AxisConvertion(); }
        public sbyte RY { get => ry.AxisConvertion(); set => ry = value.AxisConvertion(); }
        public byte LTrigger { get => lTrigger; set => lTrigger = value; }
        public byte RTrigger { get => rTrigger; set => rTrigger = value; }

        public byte DPad { get => (byte)buttons[0x000F, 0]; set => buttons[0x000F, 0] = value; }
        public bool Square { get => buttons[0x0010]; set => buttons[0x0010]=value; }
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
        public byte Counter { get => misc[0x3F,2]; set => misc[0x3F, 2] = value; }
        public BasicInState() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 23)]
    public struct ExtendedInState
    {
        private ushort timestamp=0;
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

        public ushort Timestamp { get=> timestamp; set => timestamp = value; }
        public byte Temperature { get=> temperature; set => temperature = value; }
        public short AngularVelocityX { get=> angularVelocityX; set => angularVelocityX = value; }
        public short AngularVelocityY { get=> angularVelocityY; set => angularVelocityY = value; }
        public short AngularVelocityZ { get => angularVelocityZ; set => angularVelocityZ = value; }
        public short AccelerometerX { get => accelerometerX; set => accelerometerX = value; }
        public short AccelerometerY { get => accelerometerY; set => accelerometerY = value; }
        public short AccelerometerZ { get => accelerometerZ; set => accelerometerZ = value; }
        public byte[] ExtData { get => extData; set => extData=value; }
        public byte Battery { get => misc[0x0F, 0]; set => misc[0x0F, 0] = value; }
        public bool USB { get => misc[0x10]; set => misc[0x10] = value; }
        public bool Headphone { get => misc[0x20]; set => misc[0x20] = value; }
        public bool Mic { get => misc[0x40]; set => misc[0x40] = value; }
        public bool Ext { get => misc[0x80]; set => misc[0x80] = value; }
        public bool UnkExt1 { get => unk[0x0001]; set=> unk[0x0001] = value; }
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
        private byte timestamp=0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private FingerStatus[] fingers = new FingerStatus[2];

        public byte Timestamp { get => timestamp; set => timestamp = value; }
        public FingerStatus[] Fingers { get=>fingers; set => fingers = value; }
        public TouchStatus() { }
    }

    #endregion Input

    #region Output
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SetStateData
    {
        BitVector<byte> misc1;
        BitVector<byte> misc2;
        BitVector<byte> empty1;
        byte rumbleRight;
        byte rumbleLeft;
        byte red;
        byte green;
        byte blue;
        byte flashOn;
        byte flashOff;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 8)]
        byte[] extData = new byte[8];
        byte volumeLeft;
        byte volumeRight;
        byte volumeMic;
        byte volumeSpeaker;
        BitVector<byte> unkAduio;

        public SetStateData()
        {

        }
    }
    #endregion Output
}
