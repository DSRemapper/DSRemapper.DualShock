using DSRemapper.Core;
using DSRemapper.DSRMath;
using DSRemapper.SixAxis;
using DSRemapper.Types;
using FireLibs.IO.HID;
using System.Collections.Specialized;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using DSRemapper.DualSense;

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

        internal static List<int> DS4ProdId = [0x05C4, 0x09CC, 0x0BA0, 0x0BA1];
        internal static List<int> DS5ProdId = [0x0CE6, 0x0DF2];

        /// <inheritdoc/>
        public IDSRInputController CreateController()
        {
            //Console.WriteLine(Path);
            if (DS4ProdId.Contains(ProductId))
                return new DualShock(this);
            else
                return new DualSense(this);
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
            .WmiEnumerateDevices(0x054C)
            .Where(i=> DualShockInfo.DS4ProdId.Contains(i.ProductId) || DualShockInfo.DS5ProdId.Contains(i.ProductId))
            .Select(i => (DualShockInfo)i).ToArray();
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

        private SetStateData outState = new();
        private IDS4OutReport? outReport = null;
        private Crc32 outCRC = new();
        private byte[] rawReport = [];
        private readonly DualShockInputReport report = new();
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
            IDS4InReport strRawReport;
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
            }
            else
            {
                outReport ??= new USBOutReport();
                outReport.State = outState;
            }
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
    public class DualSense : IDSRInputController
    {
        private static readonly DSRLogger logger = DSRLogger.GetLogger("DSRemapper.Dualshock");
        private readonly HidDevice hidDevice;

        private DSRVector3 lastGyro = new();
        private readonly SixAxisProcess motPro = new();
        private readonly ExpMovingAverageVector3 gyroAvg = new();

        private bool outReportInitialized = false;
        private DualSenseOutputReport outReport = new();
        private Crc32 outCRC = new();
        private byte[] rawReport = [];
        private readonly DualSenseInputReport report = new();
        private readonly DualShockConnection conType;
        public DualSense(DualShockInfo info)
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
        /// /// <inheritdoc/>
        public string Id => Convert.ToHexString(Crc64.Hash(Encoding.ASCII.GetBytes(hidDevice.Information.Path)));

        /// <inheritdoc/>
        public string Name => "DualSense";

        /// <inheritdoc/>
        public string Type => "DS";

        /// <inheritdoc/>
        public string Info { get; private set; } = "Test";

        /// <inheritdoc/>
        public bool IsConnected => hidDevice.IsOpen;

        /// <inheritdoc/>
        public string ImgPath => "DS5.png";
        /// <inheritdoc/>
        public void Connect()
        {
            logger.LogInformation($"{Name} [{Id}]: Trying to connect using {conType}");
            hidDevice.OpenDevice(false);
            if (IsConnected)
            {
                hidDevice.SetInputBufferCount(3);

                // Log the current report buffer count to ensure that no errors has been occurred.
                // If report buffer count is bigger than 3 can cause input lag
                hidDevice.GetInputBufferCount(out int bufCount);
                logger.LogInformation($"{Name} [{Id}]: buffer count set to {bufCount}");

                rawReport = new byte[hidDevice.Capabilities.InputReportByteLength];
                GetFeatureReport();
                InitOutReport();

                if (conType == DualShockConnection.Bluetooth)
                {
                    outReport.Raw.ResetLights = true;
                    outReport.UpdateRaw();
                    hidDevice.WriteFile(GetBluetoothOutReport());
                    outReport.Raw.ResetLights = false;
                }

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
        /// /// <summary>
        /// Gets the 0x05 feature report of DualShock4 controller, which enables the input report with IMU information
        /// </summary>
        private void GetFeatureReport()
        {
            byte[] fetRep = new byte[41];
            fetRep[0] = 0x05;
            hidDevice.GetFeature(fetRep);
            //logger.LogDebug($"Output Report Length: {hidDevice.Capabilities.OutputReportByteLength}");
        }

        /// /// <inheritdoc/>
        public IDSRInputReport GetInputReport()
        {
            hidDevice.ReadFile(rawReport);
            GCHandle ptr = GCHandle.Alloc(rawReport, GCHandleType.Pinned);
            InState strRawReport;
            if (conType == DualShockConnection.Bluetooth)
            {
                strRawReport = Marshal.PtrToStructure<BTReport>(ptr.AddrOfPinnedObject()).State;
            }
            else
            {
                strRawReport = Marshal.PtrToStructure<USBReport>(ptr.AddrOfPinnedObject()).State;
            }
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

            Info = $"{conType} - Battery: {report.Battery * 100,3:###}%{(report.Charging ? " Charging" : "")}";

            return report;
        }
        private void InitOutReport()
        {
            outReport.Raw.AllowMuteLight = true;
            outReport.Raw.AllowLedColor = true;
            outReport.Raw.AllowLightBritnessChange = true;
            outReport.Raw.AllowColorLightFadeAnimation = true;

            outReport.Raw.AllowPlayerLights = true;
            outReport.Raw.PlayerLightFade = false;
            outReport.Raw.ResetLights = false;

            outReport.Raw.EnableRumbleEmulation = false;
            outReport.Raw.EnableImprovedRumbleEmulation = true;
            outReport.Raw.UseRumbleNotHaptics = true;

            outReport.Raw.AllowRightTriggerFeedback = true;
            outReport.Raw.AllowLeftTriggerFeedback = true;

            outReportInitialized = true;
        }
        private byte[] GetBluetoothOutReport()
        {
            DSRemapper.DualSense.BTOutReport rawReport = new()
            {
                ReportId = 0x31,
                State = outReport.Raw
            };
            rawReport.misc.Data = 0x02;

            byte[] rawOutput = rawReport.ToArray();
            outCRC.Append([0xA2, .. rawOutput]);
            return [.. rawOutput, .. outCRC.GetHashAndReset()];
        }
        /// <inheritdoc/>
        public void SendOutputReport(IDSROutputReport report)
        {
            if (report is DualSenseOutputReport rawOutReport)
            {
                //Console.WriteLine("DualSense Raw Report");
                outReport = rawOutReport;
                outReportInitialized = false;
            }
            else
            {
                if (!outReportInitialized)
                    InitOutReport();

                outReport.Feedbacks = report.Feedbacks;
                outReport.Rumble = report.Rumble;
                outReport.Led = report.Led;
                outReport.ExtLeds = report.ExtLeds;
            }
            outReport.UpdateRaw();

            byte[] rawOutput;
            if (conType == DualShockConnection.Bluetooth)
            {
                rawOutput = GetBluetoothOutReport();
            }
            else
            {
                DSRemapper.DualSense.USBOutReport rawReport = new()
                {
                    ReportId = 0x02,
                    State = outReport.Raw,
                };
                rawOutput = rawReport.ToArray();
            }
            //Console.WriteLine(Convert.ToHexString(rawOutput));
            hidDevice.WriteFile(rawOutput);
        }
    }
}