﻿
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Android.Graphics;
using System.Threading.Tasks;



namespace Domotica
{
    [Activity(Label = "@string/application_name", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class MainActivity : Activity
    {
        // Variables (components/controls)
        // Controls on GUI
        Button buttonConnect;
        Button buttonChangePinState;
        TextView textViewServerConnect, textViewTimerStateValue;
        public TextView temptxt,suntxt, airqualitytxt,humiditytxt,windtxt,raintxt, textViewChangePinStateValue, textViewSensorValue, textViewDebugValue;
        EditText editTextIPAddress, editTextIPPort;
        RelativeLayout connectlayout;
        LinearLayout plantlayout,controllayout;
        ToggleButton sunbtn, raintbtn, windbtn;
        ProgressBar temp,humidity, Airquality;
        Timer timerClock, timerSockets;            // Timers   
        Socket socket = null;                       // Socket           
        List<Tuple<string, TextView>> commandList = new List<Tuple<string, TextView>>();  // List for commands and response places on UI
        int listIndex = 0;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);


            // Set our view from the "main" layout resource (strings are loaded from Recources -> values -> Strings.xml)
            SetContentView(Resource.Layout.Main);

            // find and set the controls, so it can be used in the code
            buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
            buttonChangePinState = FindViewById<Button>(Resource.Id.buttonChangePinState);
            textViewTimerStateValue = FindViewById<TextView>(Resource.Id.textViewTimerStateValue);
            textViewServerConnect = FindViewById<TextView>(Resource.Id.textViewServerConnect);
            textViewChangePinStateValue = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue);
            textViewSensorValue = FindViewById<TextView>(Resource.Id.textViewSensorValue);
            textViewDebugValue = FindViewById<TextView>(Resource.Id.textViewDebugValue);
            editTextIPAddress = FindViewById<EditText>(Resource.Id.editTextIPAddress);
            editTextIPPort = FindViewById<EditText>(Resource.Id.editTextIPPort);
            connectlayout = FindViewById<RelativeLayout>(Resource.Id.Connectlayout);
            plantlayout = FindViewById<LinearLayout>(Resource.Id.PlantLayout);
            controllayout = FindViewById<LinearLayout>(Resource.Id.Controllayout);
            raintxt = FindViewById<TextView>(Resource.Id.rainTXT);
            temptxt = FindViewById<TextView>(Resource.Id.Temptxt);
            windtxt = FindViewById<TextView>(Resource.Id.windTXT);
            suntxt = FindViewById<TextView>(Resource.Id.sunTXT);
            sunbtn = FindViewById<ToggleButton>(Resource.Id.sunBTN);
            raintbtn = FindViewById<ToggleButton>(Resource.Id.raintBTN);
            windbtn = FindViewById<ToggleButton>(Resource.Id.windBTN);
            airqualitytxt = FindViewById<TextView>(Resource.Id.aqualitytxt);
            humiditytxt = FindViewById<TextView>(Resource.Id.humidity);
            humidity = FindViewById<ProgressBar>(Resource.Id.humidityBar);
            Airquality = FindViewById<ProgressBar>(Resource.Id.aqbar);
            temp = FindViewById<ProgressBar>(Resource.Id.tempbar);
            UpdateConnectionState(4, "Disconnected");

            plantlayout.Visibility = ViewStates.Gone;
            controllayout.Visibility = ViewStates.Gone;
            
            // Init commandlist, scheduled by socket timer
            commandList.Add(new Tuple<string, TextView>("s", textViewChangePinStateValue));
            //commandList.Add(new Tuple<string, TextView>("R", textViewSensorValue));// rain on command
            //commandList.Add(new Tuple<string, TextView>("r", textViewSensorValue));// rain off command
            //commandList.Add(new Tuple<string, TextView>("W", textViewSensorValue));// wind on command
            //commandList.Add(new Tuple<string, TextView>("w", textViewSensorValue));// wind off command
            //commandList.Add(new Tuple<string, TextView>("Z", textViewSensorValue));// sun on command
            //commandList.Add(new Tuple<string, TextView>("z", textViewSensorValue));// sun off command

            //commandList.Add(new Tuple<string, TextView>("h", textViewSensorValue));// request humidity
            //commandList.Add(new Tuple<string, TextView>("a", textViewSensorValue));// request air quality
            //commandList.Add(new Tuple<string, TextView>("t", textViewSensorValue));// request temp



            this.Title = "Connect To Terrarium";
            

            // timer object, running clock
            timerClock = new System.Timers.Timer() { Interval = 2000, Enabled = true }; // Interval >= 1000
            timerClock.Elapsed += (obj, args) =>
            {
                RunOnUiThread(() => { textViewTimerStateValue.Text = DateTime.Now.ToString("h:mm:ss"); }); 
            };

            // timer object, check Arduino state
            // Only one command can be serviced in an timer tick, schedule from list
            timerSockets = new System.Timers.Timer() { Interval = 1000, Enabled = false }; // Interval >= 750
            timerSockets.Elapsed += (obj, args) =>
            {
                //RunOnUiThread(() =>
                
                    if (socket != null) // only if socket exists
                    {
                        // Send a command to the Arduino server on every tick (loop though list)
                        UpdateGUI(executeCommand(commandList[listIndex].Item1), commandList[listIndex].Item2);  //e.g. UpdateGUI(executeCommand("s"), textViewChangePinStateValue);
                    
                    if (++listIndex >= commandList.Count) listIndex = 0;                  
                    }
                    else timerSockets.Enabled = false;  // If socket broken -> disable timer
                //});
            };

            //Add the "Connect" button handler.
            if (buttonConnect != null)  // if button exists
            {
                buttonConnect.Click += (sender, e) =>
                {
                    //Validate the user input (IP address and port)
                    if (CheckValidIpAddress(editTextIPAddress.Text) && CheckValidPort(editTextIPPort.Text))
                    {
                        ConnectSocket(editTextIPAddress.Text, editTextIPPort.Text);
                        connectlayout.Visibility = ViewStates.Gone;// when connection is made, the connectlayout is hidden
                        plantlayout.Visibility = ViewStates.Visible;// when connection is made, the plantlayout is visible
                        controllayout.Visibility = ViewStates.Visible;// when connection is made, the plantlayout is visible
                        this.Title = "Smart Terrarium";
                    }
                    else UpdateConnectionState(3, "Please check IP");
                };
            }

            //Add the "Change pin state" button handler.
            if (buttonChangePinState != null)
            {
                buttonChangePinState.Click += (sender, e) =>
                {
                    socket.Send(Encoding.ASCII.GetBytes("t"));                 // Send toggle-command to the Arduino
                };
            }
            //rain,wind,sun buttonevents  when toggled they send a command to the arduino
            raintbtn.Click += delegate
            {              
                if (raintbtn.Checked && CheckCon(socket) == true)
                {
                    socket.Send(Encoding.ASCII.GetBytes("R"));                 // Send toggle-command to the Arduino
                 
                }
                else
                {
                    if (CheckCon(socket) == true)
                    {
                    socket.Send(Encoding.ASCII.GetBytes("r"));                 // Send toggle-command to the Arduino 
                    }
                }
            };
            windbtn.Click += delegate
            {              
                if (windbtn.Checked && CheckCon(socket) == true)
                {
                    socket.Send(Encoding.ASCII.GetBytes("W"));                 // Send toggle-command to the Arduino
                    
                }
                else
                {
                    if(CheckCon(socket) == true)
                    {
                    socket.Send(Encoding.ASCII.GetBytes("w"));                 // Send toggle-command to the Arduino
                    }
                }
            };
            sunbtn.Click += delegate
            {
                if (sunbtn.Checked && CheckCon(socket) == true)
                {
                    socket.Send(Encoding.ASCII.GetBytes("Z"));                 // Send toggle-command to the Arduino
                }
                else
                {
                    if (CheckCon(socket) == true)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("z"));                 // Send toggle-command to the Arduino
                    }
                    }
            };
        }

        //happens when the gui is updated, it updates the progressbars
        public void UpdateBar()
        {
            string h = executeCommand("h");
            string a = executeCommand("a");
            string t = executeCommand("t");
            humidity.Progress = Convert.ToInt32("h");
            Airquality.Progress = Convert.ToInt32("a");
            temp.Progress = Convert.ToInt32("t");
            humiditytxt.Text = "humidity: " + h;
            airqualitytxt.Text = "Air Quality:" + a;
            temptxt.Text = "Tempature: " + t;
        }

        public bool CheckCon(Socket socket)
        {
            if (socket == null)
            {
                connectlayout.Visibility = ViewStates.Visible;
                plantlayout.Visibility = ViewStates.Gone;
                controllayout.Visibility = ViewStates.Gone;
                return false;
            }
            else
            {
                connectlayout.Visibility = ViewStates.Gone;
                plantlayout.Visibility = ViewStates.Visible;
                controllayout.Visibility = ViewStates.Visible;
                return true;
            }
        }

        //Send command to server and wait for response (blocking)
        //Method should only be called when socket existst
        public string executeCommand(string cmd)
        {
            byte[] buffer = new byte[4]; // response is always 4 bytes
            int bytesRead = 0;
            string result = "---";

            if (socket != null)
            {
                //Send command to server
                socket.Send(Encoding.ASCII.GetBytes(cmd));

                try //Get response from server
                {
                    //Store received bytes (always 4 bytes, ends with \n)
                    bytesRead = socket.Receive(buffer);  // If no data is available for reading, the Receive method will block until data is available,
                    //Read available bytes.              // socket.Available gets the amount of data that has been received from the network and is available to be read
                    while (socket.Available > 0) bytesRead = socket.Receive(buffer);
                    if (bytesRead == 4)
                        result = Encoding.ASCII.GetString(buffer, 0, bytesRead - 1); // skip \n
                    else result = "err";
                }
                catch (Exception exception) {
                    result = exception.ToString();
                    if (socket != null) {
                        socket.Close();
                        socket = null;
                    }
                    UpdateConnectionState(3, result);
                }
            }
            return result;
        }

        //Update connection state label (GUI).
        public void UpdateConnectionState(int state, string text)
        {
            // connectButton
            string butConText = "Connect";  // default text
            bool butConEnabled = true;      // default state
            Color color = Color.Red;        // default color
            // pinButton
            bool butPinEnabled = false;     // default state 

            //Set "Connect" button label according to connection state.
            if (state == 1)
            {
                butConText = "Please wait";
                color = Color.Orange;
                butConEnabled = false;
            } else
            if (state == 2)
            {
                butConText = "Disconnect";
                color = Color.Green;
                butPinEnabled = true;
            }
            //Edit the control's properties on the UI thread
            RunOnUiThread(() =>
            {
                textViewServerConnect.Text = text;
                if (butConText != null)  // text existst
                {
                    buttonConnect.Text = butConText;
                    textViewServerConnect.SetTextColor(color);
                    buttonConnect.Enabled = butConEnabled;
                }
                buttonChangePinState.Enabled = butPinEnabled;
            });
        }

        //Update GUI based on Arduino response
        public void UpdateGUI(string result, TextView textview)
        {

            UpdateBar();
            RunOnUiThread(() =>
            {
                if (result == "OFF") textview.SetTextColor(Color.Red);
                else if (result == " ON") textview.SetTextColor(Color.Green);
                else textview.SetTextColor(Color.White);  
                textview.Text = result;
               
            });
        }

        // Connect to socket ip/prt (simple sockets)
        public void ConnectSocket(string ip, string prt)
        {
            RunOnUiThread(() =>
            {
                if (socket == null)                                       // create new socket
                {
                    UpdateConnectionState(1, "Connecting...");
                    try  // to connect to the server (Arduino).
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        //socket.SendTimeout = 1000;
                        //socket.ReceiveTimeout = 1000;
                        socket.Connect(new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(prt)));
                        if (socket.Connected)
                        {
                            UpdateConnectionState(2, "Connected");
                            timerSockets.Enabled = true;                //Activate timer for communication with Arduino     
                        }
                    } catch (Exception exception) {
                        timerSockets.Enabled = false;
                        if (socket != null)
                        {
                            socket.Close();
                            socket = null;
                        }
                        UpdateConnectionState(4, exception.Message);
                    }
	            }
                else // disconnect socket
                {
                    socket.Close(); socket = null;
                    timerSockets.Enabled = false;
                    UpdateConnectionState(4, "Disconnected");
                }
            });
        }

        //Close the connection (stop the threads) if the application stops.
        protected override void OnStop()
        {
            base.OnStop();
        }

        //Close the connection (stop the threads) if the application is destroyed.
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        //Prepare the Screen's standard options menu to be displayed.
        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            //Prevent menu items from being duplicated.
            menu.Clear();

            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        //Executes an action when a menu button is pressed.
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.exit:
                    //Force quit the application.
                    System.Environment.Exit(0);
                    return true;
                case Resource.Id.abort:
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        //Check if the entered IP address is valid.
        private bool CheckValidIpAddress(string ip)
        {
            if (ip != "") {
                //Check user input against regex (check if IP address is not empty).
                Regex regex = new Regex("\\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\\.|$)){4}\\b");
                Match match = regex.Match(ip);
                return match.Success;
            } else return false;
        }

        //Check if the entered port is valid.
        private bool CheckValidPort(string port)
        {
            //Check if a value is entered.
            if (port != "")
            {
                Regex regex = new Regex("[0-9]+");
                Match match = regex.Match(port);

                if (match.Success)
                {
                    int portAsInteger = Int32.Parse(port);
                    //Check if port is in range.
                    return ((portAsInteger >= 0) && (portAsInteger <= 65535));
                }
                else return false;
            } else return false;
        }
    }
}
