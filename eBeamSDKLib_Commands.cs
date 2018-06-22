using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SDKTemplate
{

    public partial class eBeamSDKLib
    {
        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, String lpWindowName);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);


        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TestDll(int aa);
        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCalibration_Top(double x, double y);
        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCalibration_Bottom(double x, double y);

        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCalibration_AllPoints(double x0, double y0, double x1, double y1,
            double x2, double y2, double x3, double y3);
        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCalibration_End();
        [DllImport("CalibrationLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int transPoint(double orgX, double orgY, ref double desX, ref double desY);
        private async Task<bool> cmdAppStart()
        {
            byte[] data = new byte[] { 0x71, 0x70, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };

            var ret =  await WriteValue("cmdAppStart",data);
            return ret;
        }
        private async Task<bool> cmdAppClose()
        {
            byte[] data = new byte[] { 0x70, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };

            var ret = await WriteValue("cmdAppClose", data);
            return ret;
        }
        private async Task<bool> cmdCalStart()
        {
            byte[] data = new byte[] { 0x00, 0x90, 0x8A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
            for (int i = 1; i < 9; i++) data[0] += data[i];
            var ret = await WriteValue("cmdCalStart", data);
            return ret;

        }
        private async Task<bool> cmdCalEnd()
        {
            byte[] data = new byte[] { 0x00, 0x90, 0x8B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
            for (int i = 1; i < 9; i++) data[0] += data[i];
            var ret = await WriteValue("cmdCalEnd", data);
            return ret;

        }
        private async Task<bool> cmdCalData1(int x1,int y1, int x2, int y2)
        {
            byte[] data = new byte[] { 0x00, 0x90, 0x83, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };

            byte low  = (byte) x1;
            byte high = (byte) ((x1 >> 8) & 0x00FF);

            data[3] = low;
            data[4] = high;

            low = (byte)y1;
            high = (byte)((y1 >> 8) & 0x00FF);
            data[5] = low;
            data[6] = high;

            low = (byte)x2;
            high = (byte)((x2 >> 8) & 0x00FF);
            data[7] = low;
            data[8] = high;

            low = (byte)y2;
            high = (byte)((y2 >> 8) & 0x00FF);
            data[9] = low;
            data[10] = high;

            for (int i = 1; i < 11 ; i++) data[0] += data[i];
            var ret = await WriteValue("cmdCalData1", data);
            return ret;
        }
        private async Task<bool> cmdCalData2(int x1, int y1, int x2, int y2)
        {
            byte[] data = new byte[] { 0x00, 0x90, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,0x00,0xFF, 0xFF };

            byte low = (byte)x1;
            byte high = (byte)((x1 >> 8) & 0x00FF);

            data[3] = low;
            data[4] = high;

            low = (byte)y1;
            high = (byte)((y1 >> 8) & 0x00FF);
            data[5] = low;
            data[6] = high;

            low = (byte)x2;
            high = (byte)((x2 >> 8) & 0x00FF);
            data[7] = low;
            data[8] = high;

            low = (byte)y2;
            high = (byte)((y2 >> 8) & 0x00FF);
            data[9] = low;
            data[10] = high;

            for (int i = 1; i < 11; i++) data[0] += data[i];
            var ret = await WriteValue("cmdCalData2", data);
            return ret;
        }
        private async Task<bool> cmdSetDate()
        {
            byte[] data = new byte[] { 0x00, 0x90, 0x07, 0x17, 0xA2, 0x04, 0x17, 0x09, 0x00, 0xFF, 0xFF };
            data[3] = (byte)(DateTime.Now.Year - 100);//(unsigned char)timeinfo->tm_year - 100;
            data[4] = (byte)DateTime.Now.Month;//(unsigned char)timeinfo->tm_mon;
            data[5] = (byte)DateTime.Now.Day;//(unsigned char)timeinfo->tm_mday;
            data[6] = (byte)DateTime.Now.Hour;//(unsigned char)timeinfo->tm_hour;
            data[7] = (byte)DateTime.Now.Minute;//(unsigned char)timeinfo->tm_min;
            data[8] = (byte)DateTime.Now.Second;//(unsigned char)timeinfo->tm_sec;
            for (int i = 1; i < 9; i++) data[0] += data[i];
            var ret = await WriteValue("cmdSetDate",data);
            return ret;
        }
    }
}
