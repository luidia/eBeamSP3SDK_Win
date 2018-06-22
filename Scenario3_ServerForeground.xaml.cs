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
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SDKTemplate
{
    public struct IntPoint {
        public int X;
        public int Y;
    }

    public sealed partial class Scenario3_ServerForeground : Page
    {
        private MainPage rootPage = MainPage.Current;

        private eBeamSDKLib sdkLib;

        private int m_calStatus = 0;
        ///Actually User don't click the exact edge of paper. 
        ///the margin between the edge of pager and click point.
        /// 
        private const int CALIBRATION_MARGIN = 100; // about 8mm

        private IntPoint[] m_CalPoint = new IntPoint[4];

        #region UI Code
        public Scenario3_ServerForeground()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedTo Enter Calibration Page");


            sdkLib = rootPage.GetEBeam();
            if(sdkLib != null)
            {
                sdkLib.setCalirationMode(true);

                sdkLib.firePenDataEvent += OnPenDataHandlerAsync;

                
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedFrom Exit Calibration Page");
            if (sdkLib != null)
            {
                sdkLib.firePenDataEvent -= OnPenDataHandlerAsync;
                sdkLib.setCalirationMode(false);
            }

        }

        #endregion

        public async void OnPenDataHandlerAsync(object sender, PenDataEvent e)
        {
 

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                      
                      switch (e.pen_rec.PenStatus)
                      {
                          case eBeamSDKLib.PEN_DOWN:
                             
                              break;
                          case eBeamSDKLib.PEN_MOVE:
                               
                              break;
                          case eBeamSDKLib.PEN_UP:
                              m_calStatus++;
                              switch (m_calStatus)
                              {
                                  case 0: /// 
                                      break;
                                  case 1: ///
                                      calNumber1Do.Visibility = Visibility.Collapsed;
                                      calNumber1Check.Visibility = Visibility.Visible;
                                      calNumber1Text.Visibility = Visibility.Collapsed;


                                      calNumber2ColorPre.Visibility = Visibility.Collapsed;
                                      calNumber2ColorDo.Visibility = Visibility.Visible;
                                      calNumber2Do.Visibility = Visibility.Visible;
                                      calNumber2Check.Visibility = Visibility.Collapsed;
                                      calNumber2Text.Visibility = Visibility.Visible;

                                      m_CalPoint[0].X = e.pen_rec.X - CALIBRATION_MARGIN;
                                      m_CalPoint[0].Y = e.pen_rec.Y - CALIBRATION_MARGIN;

                                      break;
                                  case 2:
                                      calNumber2ColorPre.Visibility = Visibility.Collapsed;
                                      calNumber2ColorDo.Visibility = Visibility.Visible;
                                      calNumber2Do.Visibility = Visibility.Collapsed;
                                      calNumber2Check.Visibility = Visibility.Visible;
                                      calNumber2Text.Visibility = Visibility.Collapsed;

                                      calNumber3ColorPre.Visibility = Visibility.Collapsed;
                                      calNumber3ColorDo.Visibility = Visibility.Visible;
                                      calNumber3Do.Visibility = Visibility.Visible;
                                      calNumber3Check.Visibility = Visibility.Collapsed;
                                      calNumber3Text.Visibility = Visibility.Visible;

                                      m_CalPoint[1].X = e.pen_rec.X - CALIBRATION_MARGIN;
                                      m_CalPoint[1].Y = e.pen_rec.Y + CALIBRATION_MARGIN;
                                      break;
                                  case 3:
                                      calNumber3ColorPre.Visibility = Visibility.Collapsed;
                                      calNumber3ColorDo.Visibility = Visibility.Visible;
                                      calNumber3Do.Visibility = Visibility.Collapsed;
                                      calNumber3Check.Visibility = Visibility.Visible;
                                      //calNumber3Text.Visibility = Visibility.Collapsed;
                                      calNumber3Text.Text = "Calibration is Finished.";
                                      m_CalPoint[2].X = e.pen_rec.X + CALIBRATION_MARGIN;
                                      m_CalPoint[2].Y = e.pen_rec.Y + CALIBRATION_MARGIN;

                                      m_CalPoint[3].X = m_CalPoint[2].X;
                                      m_CalPoint[3].Y = m_CalPoint[0].Y;
                                      sdkLib.SaveCalibrationToDevice(m_CalPoint[0].X, m_CalPoint[0].Y,
                                          m_CalPoint[1].X, m_CalPoint[1].Y,
                                          m_CalPoint[2].X, m_CalPoint[2].Y,
                                          m_CalPoint[3].X, m_CalPoint[3].Y);
                                     
                                      break;
                              }

                              break;
                          case eBeamSDKLib.PEN_HOVER:
                              break;

                      }

                  });


        }
        public async void OnPenConditionHandlerAsync(object sender, PenConditionEvent e)
        {
            var newValue = string.Format("Sensor Position={0} \r\n", e.conditionData.StationPosition);
            newValue += string.Format("Battery Sensor={0}(%) Pen={1} (100:High Others:Low) \r\n", e.conditionData.battery_station, e.conditionData.battery_pen);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                       
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
 
                              break;
                          case eBeamSDKLib.PEN_DATA_READY:
                              
                              break;
                          case eBeamSDKLib.PEN_DISCONNECT:
                              
                              break;
                          case eBeamSDKLib.PEN_DATA_STOP:
                              
                              break;
                      }

                  });


        }
        public async void OnPenButtonHandlerAsync(object sender, PenButtonEvent e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  () =>
                  {
                       
                  });


        }
    }
}
