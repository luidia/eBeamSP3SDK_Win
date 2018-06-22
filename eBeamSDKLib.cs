using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SDKTemplate
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct _pen_rec
    {
        public int X;              // X
        public int Y;
        public short T;                // temperature
        public int P;              // pressure
        public float TX;               // transformed X
        public float TY;               // transformed Y
        public int FUNC;
        public int ModelCode;
        public int Sensor_dis;
        public int HWVer;
        public int MCU1;
        public int MCU2;
        public int PenStatus;
        public int IRGAP;
        public int PenTiming;
        public bool bRight;
        public int Station_Position;
        public int drawRectX;
        public int drawRectY;
    };
    public struct PENConditionData
    {
        public int modelCode;
        public int pen_alive;
        public int battery_station;
        public int battery_pen;
        public int StationPosition;
        public int usbConnect;
    };
    public struct PENCalibrationData
    {
        public int modelCode;
        public int position;
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public int x3;
        public int y3;
        public int x4;
        public int y4;
    };
    public delegate void PenDataEventHandler(object sender, PenDataEvent e);
    public class PenDataEvent : EventArgs
    {
        public _pen_rec pen_rec { get; set; }
    }
    public delegate void PenConditonEventHandler(object sender, PenConditionEvent e);
    public class PenConditionEvent : EventArgs
    {
        public PENConditionData conditionData { get; set; }
    }
    public delegate void PenConnectionEventHandler(object sender, PenConnectionEvent e);
    public class PenConnectionEvent : EventArgs
    {
        public int status { get; set; }
    }
    public delegate void PenButtonEventHandler(object sender, PenButtonEvent e);
    public class PenButtonEvent : EventArgs
    {
        public int status { get; set; }
    }

    public partial class eBeamSDKLib
    {
        public const int PEN_DOWN = 1;
        public const int PEN_MOVE = 2;
        public const int PEN_UP = 3;
        public const int PEN_HOVER = 4;
        const int PEN_HOVER_DOWN = 5;
        const int PEN_HOVER_MOVE = 6;

        public const int PEN_DISCONNECT = 0;
        public const int PEN_BLE_CONN = 1;
        public const int PEN_DATA_READY = 2;
        public const int PEN_DATA_STOP = 3;

        public const int NEWPAGE_BUTTON_PRESSED = 1;
        public const int NEWPAGE_BUTTON_LONG_PRESSED = 2;

        const int DIRECTION_TOP = 1;
        const int DIRECTION_LEFT = 2;
        const int DIRECTION_RIGHT = 3;
        const int DIRECTION_BOTTOM = 4;
        const int DIRECTION_BOTH = 5;

        public const int XY_RANGE = 65535;
        const int RMD_CONST = 600;

 
        private byte[] rxdata;

        public BluetoothLEDevice bluetoothLeDevice = null;
        private GattCharacteristic readCharacteristic = null;
        private GattCharacteristic writeCharacteristic = null;
        private int m_neBeamSMFirstData=0;
        private int m_nMCU1Ver = 0;
        private int m_nMCU2Ver = 0;
        private int m_nHWVer = 0;

        private int m_nSD; // distance between sensors
        private int m_nSD2; // square of distance between sensors
        private int m_nIRGap; // IR response delay time

        private int m_nConnectStatus = PEN_DISCONNECT;
        private bool m_bConnect = false;
        private int m_nModelCode;
        private int StationPosition = DIRECTION_TOP;
        /// <summary>
        ///  for calculation coordinates
        int xxx0 = 0, yyy0 = 0;
        uint Len_Lb, Len_Rb;
        int delta_move_b;
        bool bClickedSave;
        int rmdCnt;
        /// </summary>
        /// 

        private bool m_bCalivrationMode = false;
        _pen_rec pen_rec;


        private PENCalibrationData penCalData;
        private PENConditionData   penConditionData;


        public event PenDataEventHandler firePenDataEvent;
        public event PenConditonEventHandler firePenConditionDataEvent;
        public event PenConnectionEventHandler firePenConnectionEvent;
        public event PenButtonEventHandler firePenButtonEvent;


        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion
        public void setCalirationMode(bool mode)
        {
            m_bCalivrationMode = mode;
        }
        public int getCalibrationWidth() { return (penCalData.x3 - penCalData.x1); }
        public int getCalibrationHeight() { return (penCalData.y3 - penCalData.y1); }
        public async void SaveCalibrationToDevice(int x0,int y0, int x1,int y1,int x2,int y2,int x3,int y3)
        {
            
            await cmdCalStart();
            await cmdCalData1(x0, y0, x1, y1);
            await cmdCalData2(x2, y2, x3, y3);
            await cmdCalEnd();
            penCalData.x1 = x0;
            penCalData.y1 = y0;

            penCalData.x2 = x1;
            penCalData.y2 = y1;

            penCalData.x3 = x2;
            penCalData.y3 = y2;

            penCalData.x4 = x3;
            penCalData.y4 = y3;
            ConsoleWriteLine("Cal Save x1={0} y1={1} x2={2} y2={3} x3={4} y3={5} x4={6} y4={7}",
                penCalData.x1, penCalData.y1, penCalData.x2, penCalData.y2, penCalData.x3, penCalData.y3,
                penCalData.x4, penCalData.y4);
            SetCalibration_AllPoints(penCalData.x1, penCalData.y1, penCalData.x2, penCalData.y2,
                penCalData.x3, penCalData.y3, penCalData.x4, penCalData.y4);
            SetCalibration_End();
        }
        public int GetStatus() { return m_nConnectStatus; }
        public async Task DisConnectAsync()
        {
            await cmdAppClose();
            await ClearBluetoothLEDeviceAsync();
            m_bConnect = false;
            m_nConnectStatus = PEN_DISCONNECT;
            SendConnectionStatus();
        }
        private async Task<bool> ClearBluetoothLEDeviceAsync()
        {
            // Need to clear the CCCD from the remote device so we stop receiving notifications
            var result = await readCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            if (result != GattCommunicationStatus.Success)
            {
                return false;
            }
            else
            {
                readCharacteristic.ValueChanged -= Characteristic_ValueChanged;
            }
            bluetoothLeDevice?.Dispose();
            bluetoothLeDevice = null;
            return true;
        }
        public void Stop()
        {
            readCharacteristic.ValueChanged -= Characteristic_ValueChanged;
            m_nConnectStatus = PEN_DATA_STOP;
            SendConnectionStatus();
        }
        public void Resume()
        {
            readCharacteristic.ValueChanged += Characteristic_ValueChanged;
            m_nConnectStatus = PEN_DATA_READY;
            SendConnectionStatus();

        }
        public static uint MAKEWORD(byte low, byte high)
        {
            return ((uint)high << 8) | low;
        }
        private void SendButtonEvent(int btnStatus)
        {
            if(firePenButtonEvent != null)
            {
                PenButtonEvent ev = new PenButtonEvent();
                ev.status = btnStatus;
                firePenButtonEvent(this, ev);
                ev = null;

            }
        }
        private void SendConnectionStatus()
        {
            if (firePenConnectionEvent != null)
            {
                PenConnectionEvent ev = new PenConnectionEvent();
                ev.status = m_nConnectStatus;
                firePenConnectionEvent(this, ev);
                ev = null;

            }

        }
        public async void connectDeviceAsync(string selectedBleDeviceId)
        {
            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(selectedBleDeviceId);

                if (bluetoothLeDevice == null)
                {

                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {

            }

            if (bluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    
                    foreach (var service in services)
                    {

                        if (IsSigDefinedUuid(service.Uuid)) continue;

                        var accessStatus = await service.RequestAccessAsync();
                        IReadOnlyList<GattCharacteristic> characteristics = null;

                        if (accessStatus == DeviceAccessStatus.Allowed)
                        {
                            // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                            // and the new Async functions to get the characteristics of unpaired devices as well. 
                            var result2 = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                            if (result2.Status == GattCommunicationStatus.Success)
                            {
                                characteristics = result2.Characteristics;
                            }
                            else
                            {

                                // On error, act as if there are no characteristics.
                                characteristics = new List<GattCharacteristic>();
                            }


                            foreach (GattCharacteristic selectedCharacteristic in characteristics)
                            {
                                var result3 = await selectedCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                                if (result3.Status != GattCommunicationStatus.Success)
                                {

                                }
                                if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate) ||
                                    selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                                {
                                    GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                                    var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
                                    if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                                    if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                                    status = await selectedCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

                                    if (status == GattCommunicationStatus.Success)
                                    {
                                        selectedCharacteristic.ValueChanged += Characteristic_ValueChanged;
                                        readCharacteristic = selectedCharacteristic;
                                    }
                                    else
                                    {

                                    }
                                }
                                if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) ||
                                    selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                                {
                                    writeCharacteristic = selectedCharacteristic;

                                }

                            }
                            m_nConnectStatus = PEN_BLE_CONN;
                            SendConnectionStatus();

                            //var ret = await cmdAppStart();
                        }
                        else
                        {
                            // Not granted access


                            // On error, act as if there are no characteristics.
                            characteristics = new List<GattCharacteristic>();

                        }
                    }

                }
                else
                {

                }
            }

        }
        private static bool IsSigDefinedUuid(Guid uuid)
        {
            var bluetoothBaseUuid = new Guid("00000000-0000-1000-8000-00805F9B34FB");

            var bytes = uuid.ToByteArray();
            // Zero out the first and second bytes
            // Note how each byte gets flipped in a section - 1234 becomes 34 12
            // Example Guid: 35918bc9-1234-40ea-9779-889d79b753f0
            //                   ^^^^
            // bytes output = C9 8B 91 35 34 12 EA 40 97 79 88 9D 79 B7 53 F0
            //                ^^ ^^
            bytes[0] = 0;
            bytes[1] = 0;
            var baseUuid = new Guid(bytes);
            return baseUuid == bluetoothBaseUuid;
        }
        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // BT_Code: An Indicate or Notify reported that the value has changed.
            // Display the new value with a timestamp.
            // var newValue = FormatValueByPresentation(args.CharacteristicValue);
            // var message = $"Read Data {DateTime.Now:hh:mm:ss}: {newValue}";

            //System.Diagnostics.Debug.WriteLine(message);

            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out rxdata);
            var newValue = ByteArrayToString(rxdata);
//            var message = $"Read Data {DateTime.Now:hh:mm:ss}: {newValue}";
//            ConsoleWriteLine(message);
            if (rxdata[2] == 0xC0) // env
            {
                if(m_bConnect == false) // 
                {
                    await cmdAppClose();
                    return;
                }
                penConditionData.StationPosition = StationPosition;
                float batS = (float)(MAKEWORD(rxdata[7], rxdata[8]) - 0x4A8);
                int batSt = (int)((batS / (float)(0x5A6 - 0x4A8)) * 100);
                if (batSt > 0)
                    penConditionData.battery_station = batSt > 100 ? 100 : batSt;

                batS = (float)(MAKEWORD(rxdata[9], rxdata[10]) - 0x408);
                batSt = (int)((batS / (float)(0x4EB - 0x408)) * 100);
                if (batSt > 0)
                    penConditionData.battery_pen = (batSt > 100) ? 100 : batSt;
                penConditionData.pen_alive = (int) MAKEWORD(rxdata[5],(byte) (rxdata[6] & 0x07));

                penConditionData.usbConnect = 0;

                if(firePenConditionDataEvent != null)
                {
                    PenConditionEvent ev = new PenConditionEvent();
                    ev.conditionData = penConditionData;
                    firePenConditionDataEvent(this, ev);
                    ev = null;

                }


            }
            else if(rxdata[2] == 0x7F)
            {
                if(rxdata[3] == 0xCF) // new page
                {
                    ConsoleWriteLine("new page!!!!");
                    SendButtonEvent(NEWPAGE_BUTTON_PRESSED);
 
                }
                else if(rxdata[3] == 0xDF)
                {
                    ConsoleWriteLine("duplicate page!!!!");
                    SendButtonEvent(NEWPAGE_BUTTON_LONG_PRESSED);
                 }
                else if (rxdata[3] == 0xFF)
                {
                    m_nConnectStatus = PEN_DISCONNECT;
                    SendConnectionStatus();

                    ConsoleWriteLine("Dis connected!!!!");
 
                }
                else
                {
                    m_nModelCode = rxdata[0];
                    if (rxdata[0] == 1)
                    {
                        if (m_bConnect == false)
                        {
                            switch (rxdata[1])
                            {
                                case 0:
                                    m_neBeamSMFirstData |= 1;
                                    m_nSD = (int)MAKEWORD(rxdata[5], rxdata[6]);
                                    m_nSD2 = m_nSD * m_nSD;
                                    m_nIRGap =(int) MAKEWORD(rxdata[7], rxdata[8]);
                                    m_nMCU1Ver = (int)rxdata[10];
                                    m_nMCU2Ver = (int)rxdata[11];
                                    m_nHWVer = (int)rxdata[12];
                                    ConsoleWriteLine("1 First Data ------------ MCU1: {0} MCU2:{1} HW={2} SD={3} IRG={4}", 
                                        m_nMCU1Ver, m_nMCU2Ver, m_nHWVer,m_nSD,m_nIRGap);
                                    break;
                                case 1:
                                    m_neBeamSMFirstData |= 2;
                                    penCalData.position = rxdata[5];
                                    StationPosition = rxdata[5];
                                    ConsoleWriteLine("2 First Data ------------ position={0} ",penCalData.position);
                                    break;
                                case 2:
                                    m_neBeamSMFirstData |= 4;
                                    penCalData.x1 = (int) MAKEWORD(rxdata[5], rxdata[6]);
                                    penCalData.y1 = (int) MAKEWORD(rxdata[7], rxdata[8]);

                                    penCalData.x2 = (int) MAKEWORD(rxdata[9], rxdata[10]);
                                    penCalData.y2 = (int) MAKEWORD(rxdata[11], rxdata[12]);
                                    ConsoleWriteLine("3 First Data------------ x1={0} y1={1} x2={2} y2={3} ",penCalData.x1,penCalData.y1,penCalData.x2,penCalData.y2);
                                    break;
                                case 3:
                                    m_neBeamSMFirstData |= 8;
                                    penCalData.x3 = (int) MAKEWORD(rxdata[5], rxdata[6]);
                                    penCalData.y3 = (int) MAKEWORD(rxdata[7], rxdata[8]);

                                    penCalData.x4 = (int) MAKEWORD(rxdata[9], rxdata[10]);
                                    penCalData.y4 = (int) MAKEWORD(rxdata[11], rxdata[12]);
                                    ConsoleWriteLine("4 First Data------------ x3={0} y3={1} x4={2} y4={3} ", penCalData.x3, penCalData.y3, penCalData.x4, penCalData.y4);
                                    break;
                                default:
                                    break;
                            }
                            if (m_bConnect == false && m_neBeamSMFirstData == 15)
                            {
                                m_bConnect = true;

                                bool bCalError = false;
                                if(penCalData.x1 >= penCalData.x3 || penCalData.x1 >= penCalData.x4)
                                {
                                    bCalError = true;
                                }
                                else if (penCalData.x2 >= penCalData.x3 || penCalData.x2 >= penCalData.x4)
                                {
                                    bCalError = true;

                                }
                                else if (penCalData.y1 >= penCalData.y2 || penCalData.y1 >= penCalData.y3)
                                {
                                    bCalError = true;

                                }
                                else if (penCalData.y4 >= penCalData.y2 || penCalData.y4 >= penCalData.y3)
                                {
                                    bCalError = true;

                                }
                                if (bCalError)
                                {
                                    ConsoleWriteLine("Calibration Data Error!!!!Set Default Data");
                                    SaveCalibrationToDevice(1768, 563, 1768, 5160, 5392, 5160, 5392, 563);
                                }
                                else
                                {
                                    SetCalibration_AllPoints(penCalData.x1, penCalData.y1, penCalData.x2, penCalData.y2,
                                        penCalData.x3, penCalData.y3, penCalData.x4, penCalData.y4);
                                    SetCalibration_End();

                                }





                                bool ret = await cmdAppStart();

                                m_nConnectStatus = PEN_DATA_READY;
                                SendConnectionStatus();

                            }


                        }
                    }

                }
            }
            else
            {
                 MakeVal();

            }


            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            ///() => CharacteristicLatestValue.Text = message);

        }
        private void MakeVal()
        {

            int xxx = 0, yyy = 0;

            uint Len_L, Len_R;
            int delta_move, rmd;

            int pressure;
            int SMPenFlag, SMPenState;
            int PenStatus;
            bool bClicked = false;
            bool bFuncClicked = false;
            for (int i = 0; i < 2; i++)
            {
                pressure = 100;
                if (i == 0)
                {
                    Len_R = MAKEWORD(rxdata[1], (byte)(0x3F & rxdata[2]));
                    Len_L = MAKEWORD(rxdata[3], (byte)(0x3F & rxdata[4]));
                    SMPenFlag = rxdata[5];
                    SMPenState = rxdata[6];
                    bFuncClicked = ((0x02 & rxdata[5]) == 0x02) ? true : false;

                }
                else
                {
                    Len_R = MAKEWORD(rxdata[7], (byte)(0x3F & rxdata[8]));
                    Len_L = MAKEWORD(rxdata[9], (byte)(0x3F & rxdata[10]));
                    SMPenFlag = rxdata[11];
                    SMPenState = rxdata[12];
                    bFuncClicked = ((0x02 & rxdata[11]) == 0x02) ? true : false;
                }

                switch (SMPenFlag)
                {
                    case 0x81:
                    case 0x82:
                        bClicked = true;
                        break;
                    case 0x80:
                        bClicked = false;
                        break;
                    default:
                        continue;
                }
 

                bool forceUp = false;

                pen_rec.Station_Position = StationPosition;


                MakeXYFromLengh(Len_R, Len_L, ref xxx, ref yyy);


                if (forceUp)
                {
                    xxx = xxx0;
                    yyy = yyy0;
                    delta_move = delta_move_b;
                }

                if (bClicked )
                {       // click
                    if (bClickedSave == true)
                    {
                        PenStatus = PEN_MOVE;
                    }
                    else
                    {       //first down
                        bClickedSave = true;
                        PenStatus = PEN_DOWN;
                        xxx0 = xxx;
                        yyy0 = yyy;
                        Len_Lb = Len_L;
                        Len_Rb = Len_R;
                        delta_move_b = 0;
                    }
                }
                else
                {   // pen up
                    if (bClickedSave == true)
                    {       // pen up
                        bClickedSave = false;
                        PenStatus = PEN_UP;
                    }
                    else
                    {       // hovering

                        PenStatus = PEN_HOVER;
                    }
                }
                if (Len_L == Len_Lb && Len_R == Len_Rb && bClicked == bClickedSave &&
                    PenStatus != PEN_DOWN ) continue;

#if true

                    delta_move = GetLengthBetweenPoints(xxx, yyy, xxx0, yyy0);

                ///////
                // ---------------------------------------
                //  error correction
                // ---------------------------------------
                rmd = Math.Abs(delta_move - delta_move_b);

                if (rmd > RMD_CONST)
                {
                    rmdCnt++;
                    xxx = xxx0;
                    yyy = yyy0;
                    delta_move = delta_move_b;
                    ConsoleWriteLine("RMD ERROR rmd = {0}", rmd);
                    continue;
                }
                else
                {
                    rmdCnt = 0;
                }

                delta_move_b = delta_move;
#endif
                xxx = (xxx + xxx + xxx0) / 3;
                yyy = (yyy + yyy + yyy0) / 3;

                //---------------------------------------
                // Data shift
                //---------------------------------------

                xxx0 = xxx;
                yyy0 = yyy;
                Len_Rb = Len_R;
                Len_Lb = Len_L;

                
                //TRACE("X = %d, Y = %d, P = %d \n",pen_rec.X,pen_rec.Y, pen_rec.P);

                /////               >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                double sx=0, sy=0;
                transPoint(xxx, yyy,ref sx,ref sy);
#if true
                switch (StationPosition)
                {
                    case DIRECTION_BOTTOM:
                        sy = XY_RANGE - sy;
                        sx = XY_RANGE - sx;
                        break;
                    case DIRECTION_LEFT:
                        {
                            double tempVV = sy;
                            sy = XY_RANGE - sx;
                            sx = tempVV;
                        }
                        break;
                    case DIRECTION_RIGHT:
                        {
                            double tempVV = sy;
                            sy = sx;
                            sx = XY_RANGE - tempVV;
                        }
                        break;
                }
#endif
//                ConsoleWriteLine("Trans Point xxx={0} yyy={1} sx={2} sy={3} status={4}", xxx,yyy,sx,sy,PenStatus);


                pen_rec.X = xxx;
                pen_rec.Y = yyy;
                pen_rec.FUNC = bFuncClicked ? 1 : 0;
                pen_rec.P = pressure;
                pen_rec.T = (short)rxdata[13];      // temperature
                pen_rec.ModelCode = m_nModelCode;
                pen_rec.PenStatus = PenStatus;
                pen_rec.PenTiming = SMPenState;
                pen_rec.TX = (float)sx;
                pen_rec.TY = (float)sy;
                if(firePenDataEvent != null)
                {
                    PenDataEvent ev = new PenDataEvent();
                    ev.pen_rec = pen_rec;
                    firePenDataEvent(this, ev);
                    ev = null;

                }

            }	//end for
        }
        private string FormatValueByPresentation(IBuffer buffer)
        {
            // BT_Code: For the purpose of this sample, this function converts only UInt32 and
            // UTF-8 buffers to readable text. It can be extended to support other formats if your app needs them.

            return "";

           // return ByteArrayToString(rxdata); // BitConverter.ToInt32(data, 0).ToString();
        }
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2} ", b);
            return hex.ToString();
        }
        private static byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }
        private async Task<bool> WriteValue(string cmdName,byte[] dataBuffer)
        {
            var dd = ByteArrayToString(dataBuffer);
            var message = $"Write {cmdName} {DateTime.Now:hh:mm:ss}: {dd}";
            System.Diagnostics.Debug.WriteLine(message);

            var writeBuffer = dataBuffer.AsBuffer();
            var writeSuccessful = await WriteBufferToSelectedCharacteristicAsync(writeBuffer);
            return writeSuccessful;
        }
        private async Task<bool> WriteBufferToSelectedCharacteristicAsync(IBuffer buffer)
        {
            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await writeCharacteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {

                    return true;
                }
                else
                {

                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                // This usually happens when a device reports that it support writing, but it actually doesn't.
                return false;
            }
        }
        private void ConsoleWriteLine(string format, params object[] arg)
        {
            System.Diagnostics.Debug.WriteLine(format, arg);
        }
        private int GetLengthBetweenPoints(int xxx, int yyy, int xxx0, int yyy0)
        {
            int x_gap, y_gap;
            int xsqr = 0, ysqr = 0;
            x_gap = Math.Abs(xxx - xxx0);
            y_gap = Math.Abs(yyy - yyy0);
            xsqr = (x_gap * x_gap);
            ysqr = (y_gap * y_gap);
            return (int)Math.Sqrt((double)(xsqr + ysqr));

        }
        int MakeXYFromLengh(uint Len_R, uint Len_L, ref int xxx, ref int yyy)
        {
            double xbuf, ybuf, dbuf, l_sqr;
            //---------------------------------------
            // X, Y Calc Operation
            //---------------------------------------
            dbuf = (double)(Len_R - m_nIRGap);
            l_sqr = (double)(dbuf * dbuf);
            //---------------------------------------
            dbuf = (double)(Len_L - m_nIRGap);
            dbuf = (double)(dbuf * dbuf);
            //---------------------------------------
            xbuf = (((double)(m_nSD2) + l_sqr) - dbuf) / (2*m_nSD);
            dbuf = xbuf * xbuf;
            //---------------------------------------
            ybuf = Math.Abs(l_sqr - dbuf);
            ybuf = Math.Sqrt(ybuf);
            //---------------------------------------
            // Cordination change Operation
            //---------------------------------------
            xxx = (int)(xbuf + 3000);
            yyy = (int)(ybuf + 200);

            return 0;
        }
    }

}