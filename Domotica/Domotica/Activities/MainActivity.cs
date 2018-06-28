
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Threading;
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
        TextView textViewServerConnect, textViewTimerStateValue;
        public TextView lighttxt,temptxt,suntxt, airqualitytxt, humiditytxt, windtxt, raintxt;
        EditText editTextIPAddress, editTextIPPort, rainDelay, windDelay, sunDelay;
        RelativeLayout connectlayout;
        LinearLayout plantlayout,controllayout;
        ToggleButton sunbtn, raintbtn, windbtn;
        ProgressBar light,temp,humidity, Airquality;
        System.Timers.Timer timerClock, timerUpdateBar;            // Timers   
        Socket socket = null;                                      // Socket           
        //List<Tuple<string, TextView>> commandList = new List<Tuple<string, TextView>>();  // List for commands and response places on UI
        //int listIndex = 0;

        //string h = "0", a = "0", t = "0", l = "0";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);


            // Set our view from the "main" layout resource (strings are loaded from Recources -> values -> Strings.xml)
            SetContentView(Resource.Layout.Main);

            // find and set the controls, so it can be used in the code
            buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
            textViewTimerStateValue = FindViewById<TextView>(Resource.Id.textViewTimerStateValue);
            textViewServerConnect = FindViewById<TextView>(Resource.Id.textViewServerConnect);
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
            light = FindViewById<ProgressBar>(Resource.Id.Lightbar);
            lighttxt = FindViewById<TextView>(Resource.Id.Lighttxt);
            rainDelay = FindViewById<EditText>(Resource.Id.rainDelay);
            windDelay = FindViewById<EditText>(Resource.Id.windDelay);
            sunDelay = FindViewById<EditText>(Resource.Id.sunDelay);

            UpdateConnectionState(4, "Disconnected");


            plantlayout.Visibility = ViewStates.Gone;
            controllayout.Visibility = ViewStates.Gone;
            
            // Init commandlist, scheduled by socket timer
            //commandList.Add(new Tuple<string, TextView>("s", textViewChangePinStateValue));

            this.Title = "Connect To Terrarium";
            

            // timer object, running clock
            timerClock = new System.Timers.Timer() { Interval = 2000, Enabled = true }; // Interval >= 1000
            timerClock.Elapsed += (obj, args) =>
            {
                RunOnUiThread(() => { textViewTimerStateValue.Text = DateTime.Now.ToString("h:mm:ss"); }); 
            };

            timerUpdateBar = new System.Timers.Timer() { Interval = 2000, Enabled = true }; // Interval >= 1000
            timerUpdateBar.Elapsed += (obj, args) =>
            {
                if (socket.Connected) // only if socket exists
                {
                    UpdateBar();
                }
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
                        this.Title = "Smart Terrarium";
                    }
                    else UpdateConnectionState(3, "Please check IP");
                };
            }

            //rain,wind,sun buttonevents  when toggled they send a command to the arduino
            raintbtn.Click += delegate
            {
                if (CheckCon(socket))
                {
                    
                    string delay = rainDelay.Text;
                    int checkDelay;
                    if (int.TryParse(delay, out checkDelay)) {
                        if (checkDelay < 2)
                        {
                            checkDelay = 2;
                        }
                    } else
                    {
                        checkDelay = 2;
                        rainDelay.Text = "2";
                    }

                    int absDelay = checkDelay * 1000;

                    if (raintbtn.Checked)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("R"));                 // Send toggle-command to the Arduino
                        resetbtn(raintbtn, absDelay, "r");

                    }
                    else
                    {
                        socket.Send(Encoding.ASCII.GetBytes("r"));                 // Send toggle-command to the Arduino 
                    }
                }
            };
            windbtn.Click += delegate
            {
                if (CheckCon(socket))
                {
                    string delay = windDelay.Text;
                    int checkDelay;
                    if (int.TryParse(delay, out checkDelay))
                    {
                        if (checkDelay < 2)
                        {
                            checkDelay = 2;
                        }
                    }
                    else
                    {
                        checkDelay = 2;
                        windDelay.Text = "2";
                    }

                    int absDelay = checkDelay * 1000;
                    if (windbtn.Checked)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("W"));                 // Send toggle-command to the Arduino
                        resetbtn(windbtn, 25000, "w");
                    }
                    else
                    {
                        socket.Send(Encoding.ASCII.GetBytes("w"));                 // Send toggle-command to the Arduino
                    }
                }
            };
            sunbtn.Click += delegate
            {
                if (CheckCon(socket))
                {
                    string delay = sunDelay.Text;
                    int checkDelay;
                    if (int.TryParse(delay, out checkDelay))
                    {
                        if (checkDelay < 2)
                        {
                            checkDelay = 2;
                        }
                    }
                    else
                    {
                        checkDelay = 2;
                        sunDelay.Text = "2";
                    }

                    int absDelay = checkDelay * 1000;
                    if (sunbtn.Checked)
                    {
                        socket.Send(Encoding.ASCII.GetBytes("Z"));                 // Send toggle-command to the Arduino
                        resetbtn(sunbtn, 25000, "z");
                    }
                    else
                    {
                        socket.Send(Encoding.ASCII.GetBytes("z"));                 // Send toggle-command to the Arduino
                    }
                }
            };
        }

        public async void resetbtn(ToggleButton togglebutten, int waittime, string cmd)
        {
            await Task.Delay(waittime);
            togglebutten.Checked = false;
            socket.Send(Encoding.ASCII.GetBytes(cmd));                
        }

        /// <summary>
        /// Calls method that updates GUI multiple times
        /// </summary>
        public void UpdateBar()
        {
            VisualUpdateBar("humidity: ", executeCommand("h"), humiditytxt, humidity);
            VisualUpdateBar("Air: ", executeCommand("a"), airqualitytxt, Airquality);
            VisualUpdateBar("Temprature: ", executeCommand("t"), temptxt, temp);
            VisualUpdateBar("Light: ", executeCommand("l"), lighttxt, light);
        }

        /// <summary>
        /// Updates a textview & progressbar with values given
        /// </summary>
        public void VisualUpdateBar(string b4result, string result, TextView textview, ProgressBar progressBar)
        {
            RunOnUiThread(() =>
            {
                textview.Text = b4result + result;
                progressBar.Progress = Convert.ToInt32(result);
            });
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
                            //timerSockets.Enabled = true;                //Activate timer for communication with Arduino
                            timerUpdateBar.Enabled = true;

                            connectlayout.Visibility = ViewStates.Gone;// when connection is made, the connectlayout is hidden
                            plantlayout.Visibility = ViewStates.Visible;// when connection is made, the plantlayout is visible
                            controllayout.Visibility = ViewStates.Visible;// when connection is made, the plantlayout is visible
                        }
                    } catch (Exception exception) {
                        //timerSockets.Enabled = false;
                        timerUpdateBar.Enabled = false;
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
                    timerUpdateBar.Enabled = false;
                    //timerSockets.Enabled = false;
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
                case Resource.Id.Disconnect:
                    socket = null;
                    buttonConnect.Enabled = true;
                    buttonConnect.Text = "Connect";
                    CheckCon(socket);
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
