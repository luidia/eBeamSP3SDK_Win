//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace SDKTemplate
{

    public sealed partial class Scenario2_Client : Page
    {
        [DllImport("user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_ABSOLUTE = 0x00008000;
        private static eBeamSDKLib sdkLib = null;
        private MainPage rootPage = MainPage.Current;

       private Polyline polyline = null;

        private const double DRAW_CANVAS_HEIGHT = 500.0;

        private bool m_bMouseMode = false;
        #region UI Code
        public Scenario2_Client()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(rootPage.SelectedBleDeviceId))
            {
                ConnectButton.IsEnabled = false;
            }
            if (sdkLib != null)
            {
#if true
                sdkLib.firePenDataEvent += OnPenDataHandlerAsync;
                sdkLib.firePenConditionDataEvent += OnPenConditionHandlerAsync;
                sdkLib.firePenConnectionEvent += OnPenConnectionHandlerAsync;
                sdkLib.firePenButtonEvent += OnPenButtonHandlerAsync;
#endif
                int w = sdkLib.getCalibrationWidth();
                int h = sdkLib.getCalibrationHeight();
                drawCanvas.Height = DRAW_CANVAS_HEIGHT;
                drawCanvas.Width = DRAW_CANVAS_HEIGHT * ((float)w / (float)h);
                System.Diagnostics.Debug.WriteLine("canvas w={0} h={1}", 
                    drawCanvas.Width, drawCanvas.Height);
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedFrom");
            if (sdkLib != null)
            {
#if true 
                sdkLib.firePenDataEvent -= OnPenDataHandlerAsync;
                sdkLib.firePenConditionDataEvent -= OnPenConditionHandlerAsync;
                sdkLib.firePenConnectionEvent -= OnPenConnectionHandlerAsync;
                sdkLib.firePenButtonEvent -= OnPenButtonHandlerAsync;
#endif
            }

        }
        #endregion

        #region Enumerating Services

        private void MouseCheck_Click()
        {
            m_bMouseMode = (bool)MouseCheck.IsChecked;
        }
        private void StopButton_Click()
        {
            if(sdkLib == null)
            {

            }
            else
            {
                if (sdkLib.GetStatus() == eBeamSDKLib.PEN_DATA_STOP)
                    sdkLib.Resume();
                else
                    sdkLib.Stop();
            }
        }
        private  void ConnectButton_Click()
        {
            if(sdkLib == null)
            {
                sdkLib = new eBeamSDKLib();
                rootPage.SetEBeamSDK(ref sdkLib);
                sdkLib.connectDeviceAsync(rootPage.SelectedBleDeviceId);
#if true
                PenDataEventHandler handler = new PenDataEventHandler(OnPenDataHandlerAsync);
                sdkLib.firePenDataEvent += handler;
                PenConditonEventHandler handler2 = new PenConditonEventHandler(OnPenConditionHandlerAsync);
                sdkLib.firePenConditionDataEvent += handler2;
                PenConnectionEventHandler handler3 = new PenConnectionEventHandler(OnPenConnectionHandlerAsync);
                sdkLib.firePenConnectionEvent += handler3;
                PenButtonEventHandler handler4 = new PenButtonEventHandler(OnPenButtonHandlerAsync);
                sdkLib.firePenButtonEvent += handler4;
#endif 
            }
            else
            {

                sdkLib.DisConnectAsync();
            }
        }
#endregion

        
        public async void OnPenDataHandlerAsync(object sender, PenDataEvent e)
        {
            var newValue = string.Format("Orignal (Virtual coordinates used by device.) : X={0} Y={1}  \r\n", e.pen_rec.X, e.pen_rec.Y);
               newValue += string.Format("Calibrated (Calibrated coordinates 0~{2}) : X={0} Y={1} \r\n", e.pen_rec.TX, e.pen_rec.TY,(int)eBeamSDKLib.XY_RANGE);
               newValue += string.Format("Pentip (1:Down 2:Move 3:Up 4:Hover) : {0} ", e.pen_rec.PenStatus);
            newValue += string.Format("Func Button(1:Clicked 2:not Clicked) : {0} \r\n", e.pen_rec.FUNC);
            
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      if (m_bMouseMode)
                      {

                          double rx = 1;// Windows.Graphics.Display.DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels / (double)eBeamSDKLib.XY_RANGE;
                          double ry = 1;// Windows.Graphics.Display.DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels / (double)eBeamSDKLib.XY_RANGE;
                          double localX = e.pen_rec.TX * rx;
                          double localY = e.pen_rec.TY * ry;
                          newValue += string.Format("mouse X={0} Y={1} (Coordinates fitting screen)\r\n", localX, localY);
                          CharacteristicLatestValue.Text = newValue;
                          switch (e.pen_rec.PenStatus)
                          {
                              case eBeamSDKLib.PEN_DOWN:
                                  mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)localX, (uint)localY, 0, 0);
                                  break;
                              case eBeamSDKLib.PEN_MOVE:
                                  mouse_event( MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)localX, (uint)localY, 0, 0);
                                  break;
                              case eBeamSDKLib.PEN_UP:
                                  mouse_event( MOUSEEVENTF_LEFTUP | MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)localX, (uint)localY, 0, 0);
                                  break;
                              case eBeamSDKLib.PEN_HOVER:
                                  mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)localX, (uint)localY, 0, 0);
                                  break;

                          }

                      }
                      else
                      {
                          double rx = drawCanvas.Width / (double)eBeamSDKLib.XY_RANGE;
                          double ry = drawCanvas.Height / (double)eBeamSDKLib.XY_RANGE;
                          double localX = e.pen_rec.TX * rx;
                          double localY = e.pen_rec.TY * ry;
                          newValue += string.Format("draw X={0} Y={1} (Coordinates fitting canvas)\r\n", localX, localY);
                          CharacteristicLatestValue.Text = newValue;
#if true  // line
                          switch (e.pen_rec.PenStatus)
                          {
                              case eBeamSDKLib.PEN_DOWN:
                                  polyline = new Polyline();
                                  polyline.StrokeLineJoin = PenLineJoin.Round;
                                  polyline.StrokeStartLineCap = PenLineCap.Round;
                                  polyline.StrokeEndLineCap = PenLineCap.Round;
                                  polyline.Stroke = new SolidColorBrush(Colors.Blue);
                                  polyline.StrokeThickness = 2;
                                  polyline.Points.Add(new Windows.Foundation.Point(localX, localY));
                                  drawCanvas.Children.Add(polyline);

                                  break;
                              case eBeamSDKLib.PEN_MOVE:
                                  polyline.Points.Add(new Windows.Foundation.Point(localX, localY));
                                  break;
                              case eBeamSDKLib.PEN_UP:
                                  polyline = null;
                                  break;
                              case eBeamSDKLib.PEN_HOVER:
                                  break;

                          }
#else // dot
                     
                     Ellipse els = new Ellipse();
                      els.Width = 2;
                      els.Height = 2;
                      
                      switch (e.pen_rec.PenStatus)
                      {
                          case eBeamSDKLib.PEN_DOWN:
                              els.Fill = new SolidColorBrush( Color.FromArgb(255, 255, 0, 0));

                              break;
                          case eBeamSDKLib.PEN_MOVE:
                              els.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                              break;
                          case eBeamSDKLib.PEN_UP:
                              els.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                              break;
                          case eBeamSDKLib.PEN_HOVER:
                              els.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                              break;

                      }
                      Canvas.SetLeft(els, localX);
                      Canvas.SetTop(els, localY);

                      drawCanvas.Children.Add(els);
                      
#endif

                      }

                  });


        }
        public async void OnPenConditionHandlerAsync(object sender, PenConditionEvent e)
        {
            var newValue = string.Format("Sensor Position={0} \r\n", e.conditionData.StationPosition);
            newValue += string.Format("Battery Sensor={0}(%) Pen={1} (100:High Others:Low) \r\n\r\n\r\n", 
                e.conditionData.battery_station,e.conditionData.battery_pen);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      CharacteristicLatestValue.Text = newValue;
                  });


        }
        public async void OnPenConnectionHandlerAsync(object sender, PenConnectionEvent e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      switch (e.status)
                      {
                          case eBeamSDKLib.PEN_BLE_CONN:
                              StopButton.Content = "Stop";
                              ConnectButton.Content = "DisConnect";
                              ConnectionStatus.Text = "BLE CONNECTED";
                              break;
                          case eBeamSDKLib.PEN_DATA_READY:
                              StopButton.Content = "Stop";
                              ConnectButton.Content = "DisConnect";
                              ConnectionStatus.Text = "DATA READY";
                              int w = sdkLib.getCalibrationWidth();
                              int h = sdkLib.getCalibrationHeight();
                              drawCanvas.Width = DRAW_CANVAS_HEIGHT * ((float)w/ (float)h);
                              drawCanvas.Height = DRAW_CANVAS_HEIGHT;
                              break;
                          case eBeamSDKLib.PEN_DISCONNECT:
                              StopButton.Content = "Stop";
                              ConnectButton.Content = "Connect";
                              ConnectionStatus.Text = "DISCONNECTED";
                              sdkLib = null;
                              break;
                          case eBeamSDKLib.PEN_DATA_STOP:
                              StopButton.Content = "Resume";
                              ConnectButton.Content = "DisConnect";
                              ConnectionStatus.Text = "STOPPED";
                              break;
                      }

                  });


        }
        public async void OnPenButtonHandlerAsync(object sender, PenButtonEvent e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      switch (e.status)
                      {
                          case eBeamSDKLib.NEWPAGE_BUTTON_PRESSED:
                              CharacteristicLatestValue.Text = "New Page Button Pressed\r\n\r\n\r\n\r\n";
                              drawCanvas.Children.Clear();
                              break;
                          case eBeamSDKLib.NEWPAGE_BUTTON_LONG_PRESSED:
                              CharacteristicLatestValue.Text = "New Page Button Long Pressed\r\n\r\n\r\n\r\n";
                              break;
                      }

                  });


        }
    }
}
