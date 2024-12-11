using DSRemapper.Core;
using DSRemapper.DSRMath;
using DSRemapper.SixAxis;
using DSRemapper.Types;
using FireLibs.IO.HID;
using FireLibs.Logging;
using System.Collections.Specialized;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;

/* 
 * Some things of this class are copied from some github repositories.
 * I didn't keep track of the consulted repositories and also use information from multiple wikis and web pages.
 * Disclaimer: It wasn't my intention to use other people's code without mentioning them, if this happened.
 */

namespace DSRemapper.DualShock
{
    /*
     * Dualshock vendor id: 054C
     * Dualshock product id: 09CC (of my dualshock at least)
     */
    /// <summary>
    /// DualShock info class
    /// </summary>
    /// <param name="path">Hid Device path</param>
    /// <param name="name">Device name</param>
    /// <param name="id">Device unique id</param>
    /// <param name="vendorId">Device vendor id</param>
    /// <param name="productId">Device product id</param>
    public class DualShockInfo(string path, string name, string id, int vendorId, int productId) : IDSRInputDeviceInfo
    {
        /// <summary>
        /// Hid Device path of the DualShock controller
        /// </summary>
        public string Path { get; private set; } = path;
        /// <inheritdoc/>
        public string Id { get; private set; } = id;
        /// <inheritdoc/>
        public string Name { get; private set; } = name;
        /// <summary>
        /// Hid Device vendor id of the DualShock controller
        /// </summary>
        public int VendorId { get; private set; } = vendorId;
        /// <summary>
        /// Hid Device product id of the DualShock controller
        /// </summary>
        public int ProductId { get; private set; } = productId;

        /// <inheritdoc/>
        public IDSRInputController CreateController()
        {
            return new DualShock(this);
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Device {Name} [{Id}] [{VendorId:X4}][{ProductId:X4}]";
        }

        /// <summary>
        /// Conversion from HidInfo to DualShockInfo
        /// </summary>
        /// <param name="info">HidInfo</param>
        public static explicit operator DualShockInfo(HidInfo info) => new(info.Path, info.Name, info.Id, info.VendorId, info.ProductId);
        /// <summary>
        /// Conversion from DualShockInfo to HidInfo
        /// </summary>
        /// <param name="info">HidInfo</param>
        public static explicit operator HidInfo(DualShockInfo info) => new(info.Path, info.Name, info.Id, info.VendorId, info.ProductId);
    }

    /// <summary>
    /// DualShock device scanner class
    /// </summary>
    public class DualShockScanner : IDSRDeviceScanner
    {
        /// <summary>
        /// DualShockScanner class constructor
        /// </summary>
        public DualShockScanner() { }
        /// <inheritdoc/>
        public IDSRInputDeviceInfo[] ScanDevices() => HidEnumerator
            .WmiEnumerateDevices(0x054C).Select(i => (DualShockInfo)i).ToArray();
    }

    internal enum DualShockConnection : byte
    {
        USB,
        Bluetooth,
        Dongle,
    }

    /// <summary>
    /// DualShock controller class
    /// </summary>
    public class DualShock : IDSRInputController
    {
        private static readonly DSRLogger logger = DSRLogger.GetLogger("DSRemapper.Dualshock");
        private readonly HidDevice hidDevice;

        private DSRVector3 lastGyro = new();
        private readonly SixAxisProcess motPro = new();
        private readonly ExpMovingAverageVector3 gyroAvg = new();

        //private int offset = 0;
        //private DualShockInReport strRawReport;
        private SetStateData outState = new();
        private IDS4OutReport? outReport = null;
        private Crc32 inCRC = new();
        private Crc32 outCRC = new();
        private byte[] rawReport = [], crc = [];
        private readonly DualShockInputReport report = new();
        private List<byte> sendReport = [];
        private readonly DualShockConnection conType;

        ///<summary>
        /// DualShock controller class constructor
        /// </summary>
        /// <param name="info">A DualShockInfo class with the physical controller info</param>
        public DualShock(DualShockInfo info)
        {
            hidDevice = new((HidInfo)info);

            if (hidDevice.Capabilities.InputReportByteLength == 64)
                if (hidDevice.Capabilities.NumberFeatureDataIndices == 22)
                    conType = DualShockConnection.Dongle;
                else
                    conType = DualShockConnection.USB;
            else
                conType = DualShockConnection.Bluetooth;

            logger.LogInformation($"{Name} [{Id}]: Connected using {conType}");
        }
        /// <inheritdoc/>
        public string Id => hidDevice.Information.Id;

        /// <inheritdoc/>
        public string Name => "DualShock 4";

        /// <inheritdoc/>
        public string Type => "DS";

        /// <inheritdoc/>
        public string Info { get; private set; } = "Test";

        /// <inheritdoc/>
        public bool IsConnected => hidDevice.IsOpen;

        /// <inheritdoc/>
        public string ImgPath => "DS4.png";

        /// <inheritdoc/>
        public void Connect()
        {
            hidDevice.OpenDevice(false);
            if (IsConnected)
            {
                hidDevice.SetInputBufferCount(3);

                // Log the current report buffer count to ensure that no errors has been occurred.
                // If report buffer count is bigger than 3 can cause input lag
                hidDevice.GetInputBufferCount(out int bufCount);
                logger.LogInformation($"{Name} [{Id}]: buffer count set to {bufCount}");
                //logger.LogDebug($"{Name} [{Id}]: Input Report Length {hidDevice.Capabilities.InputReportByteLength}");

                rawReport = new byte[hidDevice.Capabilities.InputReportByteLength];
                GetFeatureReport();
            }
        }
        /// <inheritdoc/>
        public void Disconnect()
        {
            hidDevice.CancelIO();
            hidDevice.CloseDevice();
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Disconnect();
            hidDevice.Dispose();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Gets the 0x05 feature report of DualShock4 controller, which enables the input report with IMU information
        /// </summary>
        private void GetFeatureReport()
        {
            byte[] fetRep = new byte[64];
            fetRep[0] = 0x05;
            hidDevice.GetFeature(fetRep);
            //logger.LogDebug($"Output Report Length: {hidDevice.Capabilities.OutputReportByteLength}");
        }
        /// <inheritdoc/>
        public IDSRInputReport GetInputReport()
        {
            hidDevice.ReadFile(rawReport);
            GCHandle ptr = GCHandle.Alloc(rawReport, GCHandleType.Pinned);
            IDS4InReport strRawReport; //new IntPtr(ptr.AddrOfPinnedObject().ToInt64() + offset)
            if (conType == DualShockConnection.USB)
                strRawReport = Marshal.PtrToStructure<USBStatus>(ptr.AddrOfPinnedObject());
            else
                strRawReport = Marshal.PtrToStructure<BTStatus>(ptr.AddrOfPinnedObject());
            ptr.Free();
            /*if(strRawReport.ReportId != 19)
                return report;*/

            report.Set(strRawReport);

            DSRVector3 temp = (report.Gyro - lastGyro);
            if (temp.Length < 1f)
                gyroAvg.Update(report.Gyro, 200);
            lastGyro = report.Gyro;

            report.Gyro -= gyroAvg.Average;

            motPro.Update(report.RawAccel, report.Gyro);

            report.Grav = motPro.Grav;
            report.Accel = motPro.Accel;
            report.Rotation = motPro.rotation;
            report.DeltaRotation = motPro.deltaRotation;
            report.DeltaTime = motPro.DeltaTime;

            /*report.LX = AxisToFloat(strRawReport.Basic.LX);
            report.LY = AxisToFloat(strRawReport.Basic.LY);
            report.RX = AxisToFloat(strRawReport.Basic.RX);
            report.RY = AxisToFloat(strRawReport.Basic.RY);

            report.Povs[0].SetDSPov(strRawReport.Basic.DPad);

            report.Square = strRawReport.Basic.Square;
            report.Cross = strRawReport.Basic.Cross;
            report.Circle = strRawReport.Basic.Circle;
            report.Triangle = strRawReport.Basic.Triangle;

            report.L1 = strRawReport.Basic.L1;
            report.R1 = strRawReport.Basic.R1;
            report.L2 = strRawReport.Basic.L2;
            report.R2 = strRawReport.Basic.R2;
            report.Share = strRawReport.Basic.Share;
            report.Options = strRawReport.Basic.Options;
            report.L3 = strRawReport.Basic.L3;
            report.R3 = strRawReport.Basic.R3;

            report.PS = strRawReport.Basic.PS;
            report.TouchPad = strRawReport.Basic.TPad;

            report.LTrigger = AxisToFloat(strRawReport.Basic.LTrigger);
            report.RTrigger = AxisToFloat(strRawReport.Basic.RTrigger);
            report.Battery = Math.Clamp(strRawReport.Extended.Battery / 10f, 0f, 1f);
            report.Charging = strRawReport.Extended.USB;

            report.SixAxes[1].X = -strRawReport.Extended.AngularVelocityX * (2000f / 32767f);
            report.SixAxes[1].Y = -strRawReport.Extended.AngularVelocityY * (2000f / 32767f);
            report.SixAxes[1].Z = strRawReport.Extended.AngularVelocityZ * (2000f / 32767f);
            report.SixAxes[0].X = -strRawReport.Extended.AccelerometerX / 8192f;
            report.SixAxes[0].Y = -strRawReport.Extended.AccelerometerY / 8192f;
            report.SixAxes[0].Z = strRawReport.Extended.AccelerometerZ / 8192f;

            DSRVector3 temp = (report.Gyro - lastGyro);
            if (temp.Length < 1f)
                gyroAvg.Update(report.Gyro, 200);
            lastGyro = report.Gyro;

            report.Gyro -= gyroAvg.Average;

            motPro.Update(report.RawAccel, report.Gyro);

            report.Grav = motPro.Grav;
            report.Accel = motPro.Accel;
            report.Rotation = motPro.rotation;
            report.DeltaRotation = motPro.deltaRotation;
            report.DeltaTime = motPro.DeltaTime;

            report.TouchPadSize = new(1920, 943);

            report.Touch[0].Pressed = strRawReport.Touch.Fingers[0].FingerTouch;
            report.Touch[0].Id = strRawReport.Touch.Fingers[0].FingerId;
            report.Touch[0].Pos.X = strRawReport.Touch.Fingers[0].FingerX;
            report.Touch[0].Pos.Y = strRawReport.Touch.Fingers[0].FingerY;
            report.Touch[0].Pos /= report.TouchPadSize;

            report.Touch[1].Pressed = strRawReport.Touch.Fingers[1].FingerTouch;
            report.Touch[1].Id = strRawReport.Touch.Fingers[1].FingerId;
            report.Touch[1].Pos.X = strRawReport.Touch.Fingers[1].FingerX;
            report.Touch[1].Pos.Y = strRawReport.Touch.Fingers[1].FingerY;
            report.Touch[1].Pos /= report.TouchPadSize;*/

            Info = $"{conType} - Battery: {report.Battery * 100,3:###}%{(report.Charging ? " Charging" : "")}";

            return report;
        }
        /// <inheritdoc/>
        public void SendOutputReport(IDSROutputReport report)
        {
            if (report is DualShockOutputReport rawOutReport)
            {
                rawOutReport.UpdateRaw();
                outState = rawOutReport.Raw.State;
            }
            else
            {
                outState.EnableLedBlink = true;
                outState.EnableLedUpdate = true;
                outState.EnableRumbleUpdate = true;
                outState.RumbleRight = (byte)(report.Weak * 255);
                outState.RumbleLeft = (byte)(report.Strong * 255);
                outState.Red = (byte)(report.Red * 255);
                outState.Green = (byte)(report.Green * 255);
                outState.Blue = (byte)(report.Blue * 255);
                outState.FlashOn = (byte)(report.OnTime * 255);
                outState.FlashOff = (byte)(report.OffTime * 255);
            }

            if (conType == DualShockConnection.Bluetooth)
            {
                outReport ??= new BTOutReport()
                {
                    PollingRate = 0,
                    EnableCRC = true,
                    EnableHID = true,
                    EnableMic = 0,
                    EnableAudio = false,
                };
                outReport.State = outState;

                outCRC.Append([0xA2]);
                outCRC.Append(outReport.ToArray()[0..74]);


                outReport.CRC = outCRC.GetHashAndReset();
                /*// rumble
                sendReport[7] = (byte)(report.Weak * 255);
                sendReport[8] = (byte)(report.Strong * 255);
                // colour
                sendReport[9] = (byte)(report.Red * 255);
                sendReport[10] = (byte)(report.Green * 255);
                sendReport[11] = (byte)(report.Blue * 255);
                // flash time
                sendReport[12] = (byte)(report.OnTime * 255);
                sendReport[13] = (byte)(report.OffTime * 255);

                crc = Crc32.Hash([..sendReport[0..75]]);//sendReport.GetRange(0, 75).ToArray()
                sendReport[75] = crc[0];
                sendReport[76] = crc[1];
                sendReport[77] = crc[2];
                sendReport[78] = crc[3];

                hidDevice.WriteFile([.. sendReport[1..]]);//sendReport.GetRange(1, 78)*/
            }
            else
            {
                outReport ??= new USBOutReport();
                outReport.State = outState;

                /*// rumble
                sendReport[4] = (byte)(report.Weak * 255);
                sendReport[5] = (byte)(report.Strong * 255);
                // colour
                sendReport[6] = (byte)(report.Red * 255);
                sendReport[7] = (byte)(report.Green * 255);
                sendReport[8] = (byte)(report.Blue * 255);
                // flash time
                sendReport[9] = (byte)(report.OnTime * 255);
                sendReport[10] = (byte)(report.OffTime * 255);

                hidDevice.WriteFile([.. sendReport]);*/
                //hidDevice.WriteFile(outReport.ToArray());
            }
            /*byte[] byteReport = new byte[hidDevice.Capabilities.OutputReportByteLength];
            byte[] byteRep = outReport.ToArray();
            Array.Copy(byteRep, byteReport, byteRep.Length);*/

            //logger.LogDebug($"RawState: {byteReport.Length}");
            hidDevice.WriteFile(outReport.ToArray());
        }
        private static string FormatByteArray(byte[] data)
        {
            // Calculate the number of rows needed
            int rows = (data.Length + 7) / 8;

            // Create a string builder to store the formatted data
            StringBuilder sb = new StringBuilder();

            // Loop through each row
            for (int i = 0; i < rows; i++)
            {
                // Loop through each column in the row
                for (int j = 0; j < 8 && i * 8 + j < data.Length; j++)
                {
                    // Format the byte as a hex string
                    sb.Append($"{data[i * 8 + j]:X2} ");
                }

                // Add a new line after each row
                sb.AppendLine();
            }

            // Return the formatted string
            return sb.ToString();
        }
        private static float AxisToFloat(sbyte axis) => axis / (axis < 0 ? 128f : 127f);
        private static float AxisToFloat(byte axis) => axis / 255f;
    }
}