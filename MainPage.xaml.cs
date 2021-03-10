/*
 * Company: Holt Environments
 * Project: HESwitch
 * Author: Anthony Mesa (amesa@holtenvironments.com)
 * Date: 04 March 2021
 * Platform: Android, iOS
 * 
 * HESwitch is an application that allows users to control LED panels over wifi
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

// So we can get x:Names
using System.Reflection;
using System.Threading;
using Xamarin.Essentials;

namespace HESwitch
{
    public partial class MainPage : ContentPage
    {
        HoltEnv.LEDController active_controller;
        Boolean is_connected = false;

        string ip_address = "192.168.0.0";
        int port = 5200;

        public MainPage()
        {
            InitializeComponent();
        }

        /*
         * This function gets the x:Name string set for an element in the view.
         */
        private string GetViewElementName(object obj)
        {
            List<FieldInfo> localFields = new List<FieldInfo>(this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            return localFields.Find(info => obj == info.GetValue(this)).Name;
        }

        /*
         * Sets IP address based off of IP address Entry input on MainPage.xml.
         * If box is empty, IP reverts to base value.
         */
        void SetIpAddress(object sender, System.EventArgs e)
        {
            string server_value = ((Entry)sender).Text;
            bool input_empty = (server_value.Length == 0) ? true : false;

            if (input_empty)
            {
                ip_address = "192.168.0.0";
            }
            else
            {
                ip_address = server_value;
            }
        }

        /*
         * Sets server port based off of port Entry input on MainPage.xml.
         * If box is empty, port resverts to base value.
         * 
         * Exception should catch any non-integer inputs.
         */
        void SetPort(object sender, System.EventArgs e)
        {
            string input_port_value = ((Entry)sender).Text;
            bool input_empty = (input_port_value.Length == 0) ? true : false;

            if (input_empty)
            {
                port = 5200;
            }
            else
            {
                try
                {
                    port = Int32.Parse(input_port_value);
                }
                catch (Exception f)
                {
                    App.Current.MainPage.DisplayAlert("Oops!", "Port value must be a number.", "OK");
                }
            }
        }

        /*
         * Should be used for all times that a button is clicked
         * 
         * Need to figure out why status not being 0 causes the
         * child thread for checking connection to crash
         */
        public void OnButtonClicked(object sender, System.EventArgs e)
        {
            string command = GetViewElementName(sender);
            int status = 0;

            Debug.WriteLine("MainPage::OnButtonClicked - Command from button click is " + command);
            Debug.WriteLine("MainPage::OnButtonClicked - Command is 'ConnectToDevice': " + (command == "ConnectToDevice"));

            if (command == "ConnectToDevice")
            {
                if (!is_connected)
                    Connect(sender);
                else
                    SetDisconnected(sender);
            }
            else if (is_connected)
            {
                switch (command)
                {
                    case "InputToDVI":
                        status = active_controller.SetInputToDVI();
                        break;
                    case "InputToHDMI":
                        status = active_controller.SetInputToHDMI();
                        break;
                    case "InputToVGA1":
                        status = active_controller.SetInputToVGA1();
                        break;
                    case "InputToVGA2":
                        status = active_controller.SetInputToVGA2();
                        break;
                    case "InputToSDI":
                        if((status = active_controller.SetInputToSDI()) != 0)
                        {
                            MessageAndroid temp = new MessageAndroid();
                            temp.LongAlert("Failed. SDI not connected to device");
                            status = 0;
                        }
                        break;
                    case "InputToDP":
                        status = active_controller.SetInputToDP();
                        break;
                }
            }
            else
            {
                App.Current.MainPage.DisplayAlert("Oops!", "Connect to device.", "OK");
            }

            if (status != 0)
            {
                Debug.WriteLine(status);
                App.Current.MainPage.DisplayAlert("Oops!", "An error occured in the HoltEnv LEDController library. LEDController was forced to disconnect.", "OK");
            }
        }

        public int Connect(object sender)
        {
            active_controller = new HoltEnv.LEDController(
                "controller1",
                ip_address,
                port
            );

            TestConnection(sender);

            if (is_connected)
            {
                MessageAndroid temp = new MessageAndroid();
                temp.ShortAlert("Success");
                ThreadPool.QueueUserWorkItem(state => StartConnectionCheck(sender));
                return 0;
            }
            else
            {
                App.Current.MainPage.DisplayAlert("Oops!", "Connection failed. Check that connection info is correct and that device is on and connected to the same network.", "OK");
                return -1;
            }
        }
        
        public void SetConnected(object sender)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ((Button)sender).Text = "Connected";
                ((Button)sender).BackgroundColor = Color.Green;
            });
            is_connected = true;
        }

        public void SetDisconnected(object sender)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ((Button)sender).Text = "Click to Connect";
                ((Button)sender).BackgroundColor = Color.Red;
            });
            is_connected = false;
        }

        public void TestConnection(object sender)
        {
            if (active_controller.GetConnectionStatus() == 0)
            {
                SetConnected(sender);
            }
            else
            {
                SetDisconnected(sender);
            }
        }

        public void StartConnectionCheck(object sender)
        {
            while (is_connected)
            {
                TestConnection(sender);
                Debug.WriteLine("MainPage::StartConnectionCheck - Checked connection...");
                Thread.Sleep(5000);
            }
            Debug.WriteLine("MainPage::StartConnectionCheck - Name of connect_button is " + GetViewElementName(sender));
            Debug.WriteLine("MainPage::StartConnectionCheck - Exiting...");
        }

        /*
         * Should be used for all times that a switch is toggled
         */
        void OnToggled(object sender, ToggledEventArgs e)
        {
            string command = GetViewElementName(sender) + e.Value;
            int status = 0;

            Debug.WriteLine("MainPage::OnToggle - Command is " + command);

            switch (command)
            {
                case "AutoScaleSwitchTrue":
                    status = active_controller.AutoScaleOn();
                    break;
                case "AutoScaleSwitchFalse":
                    status = active_controller.AutoScaleOff();
                    break;
                case "BlackoutSwitchTrue":
                    status = active_controller.BlackoutOn();
                    break;
                case "BlackoutSwitchFalse":
                    status = active_controller.BlackoutOff();
                    break;
            }

            if (status != 0)
            {
                App.Current.MainPage.DisplayAlert("Oops!", "An error occured in the HoltEnv LEDController library. LEDController was forced to disconnect.", "OK");
            }
        }

        /*
         * Should be used for all times that a slider is used
         */
        void OnSliderChanged(object sender, ValueChangedEventArgs args)
        {
            string slider = GetViewElementName(sender);
            int status = 0;

            Debug.WriteLine("MainPage::OnSliderChanged - Slider value is " + (int)args.NewValue);

            switch (slider)
            {
                case "BrightnessAdjust":
                    status = active_controller.SetBrightness((int)args.NewValue);
                    break;
            }

            if (status != 0)
            {
                App.Current.MainPage.DisplayAlert("Oops!", "An error occured in the HoltEnv LEDController library.", "OK");
            }
        }
    }
}
