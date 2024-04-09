using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DSRemapper.DualShock
{
	internal interface IDualShockInputReport
	{
		public StateData StateData { get; }
	}
	[StructLayout(LayoutKind.Explicit, Size = 64)]
    internal struct StateData
	{
		[FieldOffset(0)]
		public byte id = 0;
		[FieldOffset(1)]
		public byte LX = 0;
		[FieldOffset(2)]
		public byte LY = 0;
		[FieldOffset(3)]
		public byte RX = 0;
		[FieldOffset(4)]
		public byte RY = 0;

		[FieldOffset(5)]
		private BitVector32 buttons = new();
		private static readonly BitVector32.Section[] masks = new BitVector32.Section[15];

		[FieldOffset(8)]
		public byte LT = 0;
		[FieldOffset(9)]
		public byte RT = 0;

		[FieldOffset(13)]
		public short GyroX = 0;
		[FieldOffset(15)]
		public short GyroY = 0;
		[FieldOffset(17)]
		public short GyroZ = 0;

		[FieldOffset(19)]
		public short AccelX = 0;
		[FieldOffset(21)]
		public short AccelY = 0;
		[FieldOffset(23)]
		public short AccelZ = 0;

		[FieldOffset(30)]
		private byte misc = 0;

		[FieldOffset(35)]
		private BitVector32 touchf1 = new();
		[FieldOffset(39)]
		private BitVector32 touchf2 = new();

		private static readonly BitVector32.Section touchId = BitVector32.CreateSection(0x7F);
		private static readonly BitVector32.Section touchPress = BitVector32.CreateSection(0x01, touchId);
		private static readonly BitVector32.Section touchPosX = BitVector32.CreateSection(0xFFF, touchPress);
		private static readonly BitVector32.Section touchPosY = BitVector32.CreateSection(0xFFF, touchPosX);

		public byte DPad => (byte)buttons[masks[0]];
		public bool Square => buttons[masks[1]] != 0;
		public bool Cross => buttons[masks[2]] != 0;
		public bool Circle => buttons[masks[3]] != 0;
		public bool Triangle => buttons[masks[4]] != 0;
		public bool L1 => buttons[masks[5]] != 0;
		public bool R1 => buttons[masks[6]] != 0;
		public bool L2 => buttons[masks[7]] != 0;
		public bool R2 => buttons[masks[8]] != 0;
		public bool Options => buttons[masks[9]] != 0;
		public bool Share => buttons[masks[10]] != 0;
		public bool L3 => buttons[masks[11]] != 0;
		public bool R3 => buttons[masks[12]] != 0;
		public bool PS => buttons[masks[13]] != 0;
		public bool TPad => buttons[masks[14]] != 0;

		public byte Baterry => (byte)(misc & 0x0F);
		public bool USB => (misc & (1 << 4)) != 0;

		public byte TF1Id => (byte)touchf1[touchId];
		public bool TF1Press => touchf1[touchPress] == 0;
		public short TF1PosX => (short)touchf1[touchPosX];
		public short TF1PosY => (short)touchf1[touchPosY];
		public byte TF2Id => (byte)touchf2[touchId];
		public bool TF2Press => touchf2[touchPress] == 0;
		public short TF2PosX => (short)touchf2[touchPosX];
		public short TF2PosY => (short)touchf2[touchPosY];

		static StateData()
		{
			masks[0] = BitVector32.CreateSection(0x0f);
			for (int i = 1; i < masks.Length; i++)
			{
				masks[i] = BitVector32.CreateSection(0x01, masks[i - 1]);
			}
		}

		public StateData() { }
    }
    [StructLayout(LayoutKind.Explicit,Size = 64)]
    internal struct DSUSBInputReport : IDualShockInputReport
    {
        [FieldOffset(0)]
        StateData stateData;

        public StateData StateData => stateData;
    }
	[StructLayout(LayoutKind.Explicit, Size = 64)]
    internal struct DSBTInputReport : IDualShockInputReport
	{
		[FieldOffset(2)]
		StateData stateData;

		public StateData StateData => stateData;
	}

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    internal struct SimpleInStateData
	{
		[FieldOffset(0)]
		private byte lx;
        [FieldOffset(1)]
        private byte ly;
        [FieldOffset(2)]
        private byte rx;
        [FieldOffset(3)]
        private byte ry;
        [FieldOffset(4)]
        private BitVector32 buttons;
        [FieldOffset(7)]
        private byte lTrigger = 0;
        [FieldOffset(8)]
        private byte tTrigger = 0;

        private static readonly BitVector32.Section[] buttonMasks = new BitVector32.Section[16];

		public sbyte LX { get => (sbyte)(lx - 128); set => lx = (byte)(value + 128); }

        public byte DPad { get => (byte)buttons[buttonMasks[0]]; set => buttons[buttonMasks[0]]=value; }
        public bool Square { get => buttons[buttonMasks[1]] != 0; set => buttons[buttonMasks[1]]=value?1:0; }
        public bool Cross { get => buttons[buttonMasks[2]] != 0; set => buttons[buttonMasks[2]] = value ? 1 : 0; }
        public bool Circle { get => buttons[buttonMasks[3]] != 0; set => buttons[buttonMasks[3]] = value ? 1 : 0; }
        public bool Triangle { get => buttons[buttonMasks[4]] != 0; set => buttons[buttonMasks[4]] = value ? 1 : 0; }
        public bool L1 { get => buttons[buttonMasks[5]] != 0; set => buttons[buttonMasks[5]] = value ? 1 : 0; }
        public bool R1 { get => buttons[buttonMasks[6]] != 0; set => buttons[buttonMasks[6]] = value ? 1 : 0; }
        public bool L2 { get => buttons[buttonMasks[7]] != 0; set => buttons[buttonMasks[7]] = value ? 1 : 0; }
        public bool R2 { get => buttons[buttonMasks[8]] != 0; set => buttons[buttonMasks[8]] = value ? 1 : 0; }
        public bool Options { get => buttons[buttonMasks[9]] != 0; set => buttons[buttonMasks[9]] = value ? 1 : 0; }
        public bool Share { get => buttons[buttonMasks[10]] != 0; set => buttons[buttonMasks[10]] = value ? 1 : 0; }
        public bool L3 { get => buttons[buttonMasks[11]] != 0; set => buttons[buttonMasks[11]] = value ? 1 : 0; }
        public bool R3 { get => buttons[buttonMasks[12]] != 0; set => buttons[buttonMasks[12]] = value ? 1 : 0; }
        public bool PS { get => buttons[buttonMasks[13]] != 0; set => buttons[buttonMasks[13]] = value ? 1 : 0; }
        public bool TPad { get => buttons[buttonMasks[14]] != 0; set => buttons[buttonMasks[14]] = value ? 1 : 0; }
        public byte Counter { get => (byte)buttons[buttonMasks[15]]; set => buttons[buttonMasks[15]] = value; }

        static SimpleInStateData()
        {
            buttonMasks[0] = BitVector32.CreateSection(0x0f);
            for (int i = 1; i < buttonMasks.Length-1; i++)
            {
                buttonMasks[i] = BitVector32.CreateSection(0x01, buttonMasks[i - 1]);
            }
            buttonMasks[15] = BitVector32.CreateSection(0x3F, buttonMasks[14]);
        }
		public SimpleInStateData() { }
    }

	internal struct InStateData
	{

	}

    internal struct FeedbackData
	{

	}
}
