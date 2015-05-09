using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Reflection;
using System.Media;
using System.Threading.Tasks;
namespace MiWindows
{
    public class ControlService
    {
        public X360Device x360Bus;
        public MiDevice[] MiControllers = new MiDevice[4];
        public Mouse[] touchPad = new Mouse[4];
        private bool running = false;
        private MiState[] MappedState = new MiState[4];
        private MiState[] CurrentState = new MiState[4];
        private MiState[] PreviousState = new MiState[4];
        public MiStateExposed[] ExposedState = new MiStateExposed[4];
        public bool recordingMacro = false;
        public event EventHandler<DebugEventArgs> Debug = null;
        public bool eastertime = false;
        private int eCode = 0;
        bool[] buttonsdown = { false, false, false, false };
        List<MiControls> dcs = new List<MiControls>();
        bool[] held = new bool[4];
        int[] oldmouse = new int[4] { -1, -1, -1, -1 };
        SoundPlayer sp = new SoundPlayer();

        private class X360Data
        {
            public byte[] Report = new byte[28];
            public byte[] Rumble = new byte[8];
        }
        private X360Data[] processingData = new X360Data[4];

        public ControlService()
        {
            sp.Stream = Properties.Resources.EE;
            x360Bus = new X360Device();
            AddtoMiList();
            for (int i = 0; i < MiControllers.Length; i++)
            {
                processingData[i] = new X360Data();
                MappedState[i] = new MiState();
                CurrentState[i] = new MiState();
                PreviousState[i] = new MiState();
                ExposedState[i] = new MiStateExposed(CurrentState[i]);
            }
        }

        void AddtoMiList()
        {
            dcs.Add(MiControls.A);
            dcs.Add(MiControls.B);
            dcs.Add(MiControls.X);
            dcs.Add(MiControls.Y);
            dcs.Add(MiControls.Back);
            dcs.Add(MiControls.Menu);
            dcs.Add(MiControls.HomeSimulated);
            dcs.Add(MiControls.DpadUp);
            dcs.Add(MiControls.DpadDown);
            dcs.Add(MiControls.DpadLeft);
            dcs.Add(MiControls.DpadRight);
            dcs.Add(MiControls.L1);
            dcs.Add(MiControls.R1);
            dcs.Add(MiControls.LT);
            dcs.Add(MiControls.RT);
            dcs.Add(MiControls.LT);
            dcs.Add(MiControls.RT);
            dcs.Add(MiControls.LXPos);
            dcs.Add(MiControls.LXNeg);
            dcs.Add(MiControls.LYPos);
            dcs.Add(MiControls.LYNeg);
            dcs.Add(MiControls.RXPos);
            dcs.Add(MiControls.RXNeg);
            dcs.Add(MiControls.RYPos);
            dcs.Add(MiControls.RYNeg);
            dcs.Add(MiControls.LS);
            dcs.Add(MiControls.RS);
        }

        private async void WarnExclusiveModeFailure(MiDevice device)
        {
            if (MiDevices.isExclusiveMode && !device.IsExclusive)
            {
                await System.Threading.Tasks.Task.Delay(5);
                String message = Properties.Resources.CouldNotOpenMi.Replace("*Mac address*", device.MacAddress) + " " + Properties.Resources.QuitOtherPrograms;
                LogDebug(message, true);
                Log.LogToTray(message);
            }
        }        
        public bool Start(bool showlog = true)
        {
            if (x360Bus.Open() && x360Bus.Start())
            {
                if (showlog)
                LogDebug(Properties.Resources.Starting);
                MiDevices.isExclusiveMode = Global.UseExclusiveMode;
                if (showlog)
                {
                    LogDebug(Properties.Resources.SearchingController);
                    LogDebug(MiDevices.isExclusiveMode ?  Properties.Resources.UsingExclusive: Properties.Resources.UsingShared);
                }
                try
                {
                    MiDevices.findControllers();
                    IEnumerable<MiDevice> devices = MiDevices.getMiControllers();
                    int ind = 0;
                    //!MiLightBar.defualtLight = false;
                    foreach (MiDevice device in devices)
                    {
                        if (showlog)
                            LogDebug(Properties.Resources.FoundController + device.MacAddress + " (" + device.ConnectionType + ")");
                        WarnExclusiveModeFailure(device);
                        MiControllers[ind] = device;
                        device.Removal -= MiDevices.On_Removal;
                        device.Removal += this.On_MiRemoval;
                        device.Removal += MiDevices.On_Removal;
                        //!touchPad[ind] = new Mouse(ind, device);
                        //!device.LightBarColor = Global.MainColor[ind];
                        if (!Global.DinputOnly[ind])
                            x360Bus.Plugin(ind);
                        device.Report += this.On_Report;
                        //!TouchPadOn(ind, device);
                        //string filename = Global.ProfilePath[ind];
                        ind++;
                        if (showlog)
                            if (System.IO.File.Exists(Global.appdatapath + "\\Profiles\\" + Global.ProfilePath[ind-1] + ".xml"))
                            {
                                string prolog = Properties.Resources.UsingProfile.Replace("*number*", ind.ToString()).Replace("*Profile name*", Global.ProfilePath[ind-1]);
                                LogDebug(prolog);
                                Log.LogToTray(prolog);
                            }
                            else
                            {
                                string prolog = Properties.Resources.NotUsingProfile.Replace("*number*", (ind).ToString());
                                LogDebug(prolog);
                                Log.LogToTray(prolog);
                            }
                        if (ind >= 4) // out of Xinput devices!
                            break;
                    }
                }
                catch (Exception e)
                {
                    LogDebug(e.Message);
                    Log.LogToTray(e.Message);
                }
                running = true;

            }
            return true;
        }

        public bool Stop(bool showlog = true)
        {
            if (running)
            {
                running = false;
                if (showlog)
                    LogDebug(Properties.Resources.StoppingX360);
                bool anyUnplugged = false;                
                for (int i = 0; i < MiControllers.Length; i++)
                {
                    if (MiControllers[i] != null)
                    {
                        if (Global.DCBTatStop && !MiControllers[i].Charging && showlog)
                            MiControllers[i].DisconnectBT();
                        //!else
                        //{
                        //    MiLightBar.forcelight[i] = false;
                        //    MiLightBar.forcedFlash[i] = 0;
                        //    MiLightBar.defualtLight = true;
                        //    MiLightBar.updateLightBar(MiControllers[i], i, CurrentState[i], ExposedState[i], touchPad[i]);
                        //    System.Threading.Thread.Sleep(50);
                        //}
                        CurrentState[i].Battery = PreviousState[i].Battery = 0; // Reset for the next connection's initial status change.
                        x360Bus.Unplug(i);
                        anyUnplugged = true;
                        MiControllers[i] = null;
                        touchPad[i] = null;
                    }
                }
                if (anyUnplugged)
                    System.Threading.Thread.Sleep(XINPUT_UNPLUG_SETTLE_TIME);
                x360Bus.UnplugAll();
                x360Bus.Stop();
                if (showlog)
                    LogDebug(Properties.Resources.StoppingMi);
                MiDevices.stopControllers();
                if (showlog)
                    LogDebug(Properties.Resources.StoppedMiWindows);
                Global.ControllerStatusChanged(this);                
            }
            return true;
        }

        public bool HotPlug()
        {
            if (running)
            {
                MiDevices.findControllers();
                IEnumerable<MiDevice> devices = MiDevices.getMiControllers();
                foreach (MiDevice device in devices)
                {
                    if (device.IsDisconnecting)
                        continue;
                    if (((Func<bool>)delegate
                    {
                        for (Int32 Index = 0; Index < MiControllers.Length; Index++)
                            if (MiControllers[Index] != null && MiControllers[Index].MacAddress == device.MacAddress)
                                return true;
                        return false;
                    })())
                        continue;
                    for (Int32 Index = 0; Index < MiControllers.Length; Index++)
                        if (MiControllers[Index] == null)
                        {
                            LogDebug(Properties.Resources.FoundController + device.MacAddress + " (" + device.ConnectionType + ")");
                            WarnExclusiveModeFailure(device);
                            MiControllers[Index] = device;
                            device.Removal -= MiDevices.On_Removal;
                            device.Removal += this.On_MiRemoval;
                            device.Removal += MiDevices.On_Removal;
                            //!touchPad[Index] = new Mouse(Index, device);
                            //device.LightBarColor = Global.MainColor[Index];
                            device.Report += this.On_Report;
                            if (!Global.DinputOnly[Index])
                                x360Bus.Plugin(Index);
                            //!TouchPadOn(Index, device);
                            //string filename = Path.GetFileName(Global.ProfilePath[Index]);
                            if (System.IO.File.Exists(Global.appdatapath + "\\Profiles\\" + Global.ProfilePath[Index] + ".xml"))
                            {
                                string prolog = Properties.Resources.UsingProfile.Replace("*number*", (Index + 1).ToString()).Replace("*Profile name*", Global.ProfilePath[Index]);
                                LogDebug(prolog);
                                Log.LogToTray(prolog);
                            }
                            else
                            {
                                string prolog = Properties.Resources.NotUsingProfile.Replace("*number*", (Index + 1).ToString());
                                LogDebug(prolog);
                                Log.LogToTray(prolog);
                            }
                        
                            break;
                        }
                }
            }
            return true;
        }

        //public void TouchPadOn(int ind, MiDevice device)
        //{
        //    ITouchpadBehaviour tPad = touchPad[ind];
        //    device.Touchpad.TouchButtonDown += tPad.touchButtonDown;
        //    device.Touchpad.TouchButtonUp += tPad.touchButtonUp;
        //    device.Touchpad.TouchesBegan += tPad.touchesBegan;
        //    device.Touchpad.TouchesMoved += tPad.touchesMoved;
        //    device.Touchpad.TouchesEnded += tPad.touchesEnded;
        //    device.Touchpad.TouchUnchanged += tPad.touchUnchanged;
        //    //LogDebug("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
        //    //Log.LogToTray("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
        //    Global.ControllerStatusChanged(this);
        //}

        public void TimeoutConnection(MiDevice d)
        {
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                while (!d.IsAlive())
                {
                    if (sw.ElapsedMilliseconds < 1000)
                        System.Threading.Thread.SpinWait(500); 
                        //If weve been waiting less than 1 second let the thread keep its processing chunk
                    else
                        System.Threading.Thread.Sleep(500); 
                    //If weve been waiting more than 1 second give up some resources

                    if (sw.ElapsedMilliseconds > 5000) throw new TimeoutException(); //Weve waited long enough
                }
                sw.Reset();
            }
            catch (TimeoutException)
            {
                Stop(false);
                Start(false);
            }
        }

        public string getMiControllerInfo(int index)
        {
            if (MiControllers[index] != null)
            {
                MiDevice d = MiControllers[index];
                if (!d.IsAlive())
                    //return "Connecting..."; // awaiting the first battery charge indication
                {
                    var TimeoutThread = new System.Threading.Thread(() => TimeoutConnection(d));
                    TimeoutThread.IsBackground = true;
                    TimeoutThread.Name = "TimeoutFor" + d.MacAddress.ToString();
                    TimeoutThread.Start();
                    return Properties.Resources.Connecting;
                }
                String battery;
                //if (d.Charging)
                //{
                    //if (d.Battery >= 100)
                        battery = Properties.Resources.Charged;
                    //else
                    //    battery = Properties.Resources.Charging.Replace("*number*", d.Battery.ToString());
                //}
                //else
                //{
                //    battery = Properties.Resources.Battery.Replace("*number*", d.Battery.ToString());
                //}
                return d.MacAddress + " (" + d.ConnectionType + "), " + battery;
                //return d.MacAddress + " (" + d.ConnectionType + "), Battery is " + battery + ", Touchpad in " + modeSwitcher[index].ToString();
            }
            else
                return String.Empty;
        }

        public string getMiMacAddress(int index)
        {
            if (MiControllers[index] != null)
            {
                MiDevice d = MiControllers[index];
                if (!d.IsAlive())
                //return "Connecting..."; // awaiting the first battery charge indication
                {
                    var TimeoutThread = new System.Threading.Thread(() => TimeoutConnection(d));
                    TimeoutThread.IsBackground = true;
                    TimeoutThread.Name = "TimeoutFor" + d.MacAddress.ToString();
                    TimeoutThread.Start();
                    return Properties.Resources.Connecting;
                }
                return d.MacAddress;
            }
            else
                return String.Empty;
        }

        public string getShortMiControllerInfo(int index)
        {
            if (MiControllers[index] != null)
            {
                MiDevice d = MiControllers[index];
                String battery;
                if (!d.IsAlive())
                    battery = "...";
                if (d.Charging)
                {
                    if (d.Battery >= 100)
                        battery = Properties.Resources.Full;
                    else
                        battery = d.Battery + "%+";
                }
                else
                {
                    battery = d.Battery + "%";
                }
                return (d.ConnectionType + " " + battery);
            }
            else
                return Properties.Resources.NoneText;
        }

        public string getMiBattery(int index)
        {
            if (MiControllers[index] != null)
            {
                MiDevice d = MiControllers[index];
                String battery;
                if (!d.IsAlive())
                    battery = "...";
                if (d.Charging)
                {
                    if (d.Battery >= 100)
                        battery = Properties.Resources.Full;
                    else
                        battery = d.Battery + "%+";
                }
                else
                {
                    battery = d.Battery + "%";
                }
                return battery;
            }
            else
                return Properties.Resources.NA;
        }

        public string getMiStatus(int index)
        {
            if (MiControllers[index] != null)
            {
                MiDevice d = MiControllers[index];
                return d.ConnectionType+"";
            }
            else
                return Properties.Resources.NoneText;
        }


        private int XINPUT_UNPLUG_SETTLE_TIME = 250; // Inhibit races that occur with the asynchronous teardown of ScpVBus -> X360 driver instance.
        //Called when Mi is disconnected or timed out
        protected virtual void On_MiRemoval(object sender, EventArgs e)
        {
            MiDevice device = (MiDevice)sender;
            int ind = -1;
            for (int i = 0; i < MiControllers.Length; i++)
                if (MiControllers[i] != null && device.MacAddress == MiControllers[i].MacAddress)
                    ind = i;
            if (ind != -1)
            {
                CurrentState[ind].Battery = PreviousState[ind].Battery = 0; // Reset for the next connection's initial status change.
                x360Bus.Unplug(ind);
                LogDebug(Properties.Resources.ControllerWasRemoved.Replace("*Mac address*", device.MacAddress));
                Log.LogToTray(Properties.Resources.ControllerWasRemoved.Replace("*Mac address*", device.MacAddress));
                System.Threading.Thread.Sleep(XINPUT_UNPLUG_SETTLE_TIME);
                MiControllers[ind] = null;
                touchPad[ind] = null;
                Global.ControllerStatusChanged(this);
            }
        }
        public bool[] lag = { false, false, false, false };
        //Called every time the new input report has arrived
        protected virtual void On_Report(object sender, EventArgs e)
        {

            MiDevice device = (MiDevice)sender;

            int ind = -1;
            for (int i = 0; i < MiControllers.Length; i++)
                if (device == MiControllers[i])
                    ind = i;

            if (ind != -1)
            {
                if (Global.FlushHIDQueue[ind])
                    device.FlushHID();
                if (!string.IsNullOrEmpty(device.error))
                {
                    LogDebug(device.error);
                }
                if (DateTime.UtcNow - device.firstActive > TimeSpan.FromSeconds(5))
                {
                    if (device.Latency >= 10 && !lag[ind])
                        LagFlashWarning(ind, true);
                    else if (device.Latency < 10 && lag[ind])
                        LagFlashWarning(ind, false);
                }
                device.getExposedState(ExposedState[ind], CurrentState[ind]);
                MiState cState = CurrentState[ind];
                device.getPreviousState(PreviousState[ind]);
                MiState pState = PreviousState[ind];
                if (pState.Battery != cState.Battery)
                    Global.ControllerStatusChanged(this);
                //CheckForHotkeys(ind, cState, pState);
                if (eastertime)
                    EasterTime(ind);
                GetInputkeys(ind);
                if (Global.LSCurve[ind] != 0 || Global.RSCurve[ind] != 0 || Global.LSDeadzone[ind] != 0 || Global.RSDeadzone[ind] != 0 ||
                    Global.L2Deadzone[ind] != 0 || Global.R2Deadzone[ind] != 0) //if a curve or deadzone is in place
                    cState = Mapping.SetCurveAndDeadzone(ind, cState);
                if (!recordingMacro && (!string.IsNullOrEmpty(Global.tempprofilename[ind]) ||
                    Global.getHasCustomKeysorButtons(ind) || Global.getHasShiftCustomKeysorButtons(ind) || Global.ProfileActions[ind].Count > 0))
                {
                    Mapping.MapCustom(ind, cState, MappedState[ind], ExposedState[ind], touchPad[ind], this);
                    cState = MappedState[ind];
                }
                if (Global.getHasCustomExtras(ind))
                    DoExtras(ind);

                // Update the GUI/whatever.
                //MiLightBar.updateLightBar(device, ind, cState, ExposedState[ind], touchPad[ind]);

                x360Bus.Parse(cState, processingData[ind].Report, ind);
                // We push the translated Xinput state, and simultaneously we
                // pull back any possible rumble data coming from Xinput consumers.
                if (x360Bus.Report(processingData[ind].Report, processingData[ind].Rumble))
                {
                    Byte Big = (Byte)(processingData[ind].Rumble[3]);
                    Byte Small = (Byte)(processingData[ind].Rumble[4]);

                    if (processingData[ind].Rumble[1] == 0x08)
                    {
                        setRumble(Big, Small, ind);
                    }
                }

                // Output any synthetic events.
                Mapping.Commit(ind);
                // Pull settings updates.
                device.IdleTimeout = Global.IdleDisconnectTimeout[ind];
            }
        }

        public void LagFlashWarning(int ind, bool on)
        {
            if (on)
            {
                lag[ind] = true;
                LogDebug(Properties.Resources.LatencyOverTen.Replace("*number*", (ind + 1).ToString()), true);
                if (Global.FlashWhenLate)
                {
                    MiColor color = new MiColor { red = 50, green = 0, blue = 0 };
                    //MiLightBar.forcedColor[ind] = color;
                    //MiLightBar.forcedFlash[ind] = 2;
                    //MiLightBar.forcelight[ind] = true;
                }
            }
            else
            {
                lag[ind] = false;
                LogDebug(Properties.Resources.LatencyNotOverTen.Replace("*number*", (ind + 1).ToString()));
                //MiLightBar.forcelight[ind] = false;
                //MiLightBar.forcedFlash[ind] = 0;
            }
        }
        
        private void DoExtras(int ind)
        {
            MiState cState = CurrentState[ind];
            MiStateExposed eState = ExposedState[ind];
            Mouse tp = touchPad[ind];
            MiControls helddown = MiControls.None;
            foreach (KeyValuePair<MiControls, string> p in Global.getCustomExtras(ind))
            {
                if (Mapping.getBoolMapping(p.Key, cState, eState, tp))
                {
                    helddown = p.Key;
                    break;
                }
            }
            if (helddown != MiControls.None)
            {
                string p = Global.getCustomExtras(ind)[helddown];
                string[] extraS = p.Split(',');
                int[] extras = new int[extraS.Length];
                for (int i = 0; i < extraS.Length; i++)
                {
                    int b;
                    if (int.TryParse(extraS[i], out b))
                        extras[i] = b;
                }
                held[ind] = true;
                try
                {
                    if (!(extras[0] == extras[1] && extras[1] == 0))
                        setRumble((byte)extras[0], (byte)extras[1], ind);
                    if (extras[2] == 1)
                    {
                        MiColor color = new MiColor { red = (byte)extras[3], green = (byte)extras[4], blue = (byte)extras[5] };
                        //MiLightBar.forcedColor[ind] = color;
                        //MiLightBar.forcedFlash[ind] = (byte)extras[6];
                        //MiLightBar.forcelight[ind] = true;
                    }
                    if (extras[7] == 1)
                    {
                        if (oldmouse[ind] == -1)
                            oldmouse[ind] = Global.ButtonMouseSensitivity[ind];
                        Global.ButtonMouseSensitivity[ind] = extras[8];
                    }
                }
                catch { }
            }
            else if (held[ind])
            {
                //MiLightBar.forcelight[ind] = false;
                //MiLightBar.forcedFlash[ind] = 0;                
                Global.ButtonMouseSensitivity[ind] = oldmouse[ind];
                oldmouse[ind] = -1;
                setRumble(0, 0, ind);
                held[ind] = false;
            }
        }



        public void EasterTime(int ind)
        {
            MiState cState = CurrentState[ind];
            MiStateExposed eState = ExposedState[ind];
            Mouse tp = touchPad[ind];

            bool pb = false;
            foreach (MiControls dc in dcs)
            {
                if (Mapping.getBoolMapping(dc, cState, eState, tp))
                {
                    pb = true;
                    break;
                }
            }
            int temp = eCode;
            //Looks like you found the easter egg code, since you're already cheating,
            //I scrambled the code for you :)
            if (pb && !buttonsdown[ind])
            {
                if (cState.X && eCode == 9)
                    eCode++;
                else if (!cState.X && eCode == 9)
                    eCode = 0;
                else if (cState.DpadLeft && eCode == 6)
                    eCode++;
                else if (!cState.DpadLeft && eCode == 6)
                    eCode = 0;
                else if (cState.DpadRight && eCode == 7)
                    eCode++;
                else if (!cState.DpadRight && eCode == 7)
                    eCode = 0;
                else if (cState.DpadLeft && eCode == 4)
                    eCode++;
                else if (!cState.DpadLeft && eCode == 4)
                    eCode = 0;
                else if (cState.DpadDown && eCode == 2)
                    eCode++;
                else if (!cState.DpadDown && eCode == 2)
                    eCode = 0;
                else if (cState.DpadRight && eCode == 5)
                    eCode++;
                else if (!cState.DpadRight && eCode == 5)
                    eCode = 0;
                else if (cState.DpadUp && eCode == 1)
                    eCode++;
                else if (!cState.DpadUp && eCode == 1)
                    eCode = 0;
                else if (cState.DpadDown && eCode == 3)
                    eCode++;
                else if (!cState.DpadDown && eCode == 3)
                    eCode = 0;
                else if (cState.B && eCode == 8)
                    eCode++;
                else if (!cState.B && eCode == 8)
                    eCode = 0;

                if (cState.DpadUp && eCode == 0)
                    eCode++;

                if (eCode == 10)
                {
                    string message = "(!)";
                    sp.Play();
                    LogDebug(message, true);
                    eCode = 0;
                }

                if (temp != eCode)
                    Console.WriteLine(eCode);
                buttonsdown[ind] = true;
            }
            else if (!pb)
                buttonsdown[ind] = false;
        }

        public string GetInputkeys(int ind)
        {
            MiState cState = CurrentState[ind];
            MiStateExposed eState = ExposedState[ind];
            Mouse tp = touchPad[ind];
            if (MiControllers[ind] != null)
                if (Mapping.getBoolMapping(MiControls.A, cState, eState, tp)) return "Cross";
                else if (Mapping.getBoolMapping(MiControls.B, cState, eState, tp)) return "Circle";
                else if (Mapping.getBoolMapping(MiControls.Y, cState, eState, tp)) return "Triangle";
                else if (Mapping.getBoolMapping(MiControls.X, cState, eState, tp)) return "Square";
                else if (Mapping.getBoolMapping(MiControls.L1, cState, eState, tp)) return "L1";
                else if (Mapping.getBoolMapping(MiControls.R1, cState, eState, tp)) return "R1";
                else if (Mapping.getBoolMapping(MiControls.LT, cState, eState, tp)) return "L2";
                else if (Mapping.getBoolMapping(MiControls.RT, cState, eState, tp)) return "R2";
                else if (Mapping.getBoolMapping(MiControls.LS, cState, eState, tp)) return "L3";
                else if (Mapping.getBoolMapping(MiControls.RS, cState, eState, tp)) return "R3";
                else if (Mapping.getBoolMapping(MiControls.DpadUp, cState, eState, tp)) return "Up";
                else if (Mapping.getBoolMapping(MiControls.DpadDown, cState, eState, tp)) return "Down";
                else if (Mapping.getBoolMapping(MiControls.DpadLeft, cState, eState, tp)) return "Left";
                else if (Mapping.getBoolMapping(MiControls.DpadRight, cState, eState, tp)) return "Right";
                else if (Mapping.getBoolMapping(MiControls.Menu, cState, eState, tp)) return "Share";
                else if (Mapping.getBoolMapping(MiControls.Back, cState, eState, tp)) return "Options";
                else if (Mapping.getBoolMapping(MiControls.HomeSimulated, cState, eState, tp)) return "PS";
                else if (Mapping.getBoolMapping(MiControls.LXPos, cState, eState, tp)) return "LS Right";
                else if (Mapping.getBoolMapping(MiControls.LXNeg, cState, eState, tp)) return "LS Left";
                else if (Mapping.getBoolMapping(MiControls.LYPos, cState, eState, tp)) return "LS Down";
                else if (Mapping.getBoolMapping(MiControls.LYNeg, cState, eState, tp)) return "LS Up";
                else if (Mapping.getBoolMapping(MiControls.RXPos, cState, eState, tp)) return "RS Right";
                else if (Mapping.getBoolMapping(MiControls.RXNeg, cState, eState, tp)) return "RS Left";
                else if (Mapping.getBoolMapping(MiControls.RYPos, cState, eState, tp)) return "RS Down";
                else if (Mapping.getBoolMapping(MiControls.RYNeg, cState, eState, tp)) return "RS Up";
                //else if (Mapping.getBoolMapping(MiControls.TouchLeft, cState, eState, tp)) return "Touch Left";
                //else if (Mapping.getBoolMapping(MiControls.TouchRight, cState, eState, tp)) return "Touch Right";
                //else if (Mapping.getBoolMapping(MiControls.TouchMulti, cState, eState, tp)) return "Touch Multi";
                //else if (Mapping.getBoolMapping(MiControls.TouchUpper, cState, eState, tp)) return "Touch Upper";
            return "nothing";
        }

        public MiControls GetInputkeysMi(int ind)
        {
            MiState cState = CurrentState[ind];
            MiStateExposed eState = ExposedState[ind];
            Mouse tp = touchPad[ind];
            if (MiControllers[ind] != null)
                if (Mapping.getBoolMapping(MiControls.A, cState, eState, tp)) return MiControls.A;
                else if (Mapping.getBoolMapping(MiControls.B, cState, eState, tp)) return MiControls.B;
                else if (Mapping.getBoolMapping(MiControls.Y, cState, eState, tp)) return MiControls.Y;
                else if (Mapping.getBoolMapping(MiControls.X, cState, eState, tp)) return MiControls.X;
                else if (Mapping.getBoolMapping(MiControls.L1, cState, eState, tp)) return MiControls.L1;
                else if (Mapping.getBoolMapping(MiControls.R1, cState, eState, tp)) return MiControls.R1;
                else if (Mapping.getBoolMapping(MiControls.LT, cState, eState, tp)) return MiControls.LT;
                else if (Mapping.getBoolMapping(MiControls.RT, cState, eState, tp)) return MiControls.RT;
                else if (Mapping.getBoolMapping(MiControls.LS, cState, eState, tp)) return MiControls.LS;
                else if (Mapping.getBoolMapping(MiControls.RS, cState, eState, tp)) return MiControls.RS;
                else if (Mapping.getBoolMapping(MiControls.DpadUp, cState, eState, tp)) return MiControls.DpadUp;
                else if (Mapping.getBoolMapping(MiControls.DpadDown, cState, eState, tp)) return MiControls.DpadDown;
                else if (Mapping.getBoolMapping(MiControls.DpadLeft, cState, eState, tp)) return MiControls.DpadLeft;
                else if (Mapping.getBoolMapping(MiControls.DpadRight, cState, eState, tp)) return MiControls.DpadRight;
                else if (Mapping.getBoolMapping(MiControls.Menu, cState, eState, tp)) return MiControls.Menu;
                else if (Mapping.getBoolMapping(MiControls.Back, cState, eState, tp)) return MiControls.Back;
                else if (Mapping.getBoolMapping(MiControls.HomeSimulated, cState, eState, tp)) return MiControls.HomeSimulated;
                else if (Mapping.getBoolMapping(MiControls.LXPos, cState, eState, tp)) return MiControls.LXPos;
                else if (Mapping.getBoolMapping(MiControls.LXNeg, cState, eState, tp)) return MiControls.LXNeg;
                else if (Mapping.getBoolMapping(MiControls.LYPos, cState, eState, tp)) return MiControls.LYPos;
                else if (Mapping.getBoolMapping(MiControls.LYNeg, cState, eState, tp)) return MiControls.LYNeg;
                else if (Mapping.getBoolMapping(MiControls.RXPos, cState, eState, tp)) return MiControls.RXPos;
                else if (Mapping.getBoolMapping(MiControls.RXNeg, cState, eState, tp)) return MiControls.RXNeg;
                else if (Mapping.getBoolMapping(MiControls.RYPos, cState, eState, tp)) return MiControls.RYPos;
                else if (Mapping.getBoolMapping(MiControls.RYNeg, cState, eState, tp)) return MiControls.RYNeg;
                //else if (Mapping.getBoolMapping(MiControls.TouchLeft, cState, eState, tp)) return MiControls.TouchLeft;
                //else if (Mapping.getBoolMapping(MiControls.TouchRight, cState, eState, tp)) return MiControls.TouchRight;
                //else if (Mapping.getBoolMapping(MiControls.TouchMulti, cState, eState, tp)) return MiControls.TouchMulti;
                //else if (Mapping.getBoolMapping(MiControls.TouchUpper, cState, eState, tp)) return MiControls.TouchUpper;
            return MiControls.None;
        }

        public bool[] touchreleased = { true, true, true, true }, touchslid = { false, false, false, false };
        public byte[] oldtouchvalue = { 0, 0, 0, 0 };
        public int[] oldscrollvalue = { 0, 0, 0, 0 };
        //protected virtual void CheckForHotkeys(int deviceID, MiState cState, MiState pState)
        //{
        //    if (!Global.UseTPforControls[deviceID] && cState.Touch1 && pState.PS)
        //    {
        //        if (Global.TouchSensitivity[deviceID] > 0 && touchreleased[deviceID])
        //        {
        //            oldtouchvalue[deviceID] = Global.TouchSensitivity[deviceID];
        //            oldscrollvalue[deviceID] = Global.ScrollSensitivity[deviceID];
        //            Global.TouchSensitivity[deviceID] = 0;
        //            Global.ScrollSensitivity[deviceID] = 0;
        //            LogDebug(Global.TouchSensitivity[deviceID] > 0 ? Properties.Resources.TouchpadMovementOn : Properties.Resources.TouchpadMovementOff);
        //            Log.LogToTray(Global.TouchSensitivity[deviceID] > 0 ? Properties.Resources.TouchpadMovementOn : Properties.Resources.TouchpadMovementOff);
        //            touchreleased[deviceID] = false;
        //        }
        //        else if (touchreleased[deviceID])
        //        {
        //            Global.TouchSensitivity[deviceID] = oldtouchvalue[deviceID];
        //            Global.ScrollSensitivity[deviceID] = oldscrollvalue[deviceID];
        //            LogDebug(Global.TouchSensitivity[deviceID] > 0 ? Properties.Resources.TouchpadMovementOn : Properties.Resources.TouchpadMovementOff);
        //            Log.LogToTray(Global.TouchSensitivity[deviceID] > 0 ? Properties.Resources.TouchpadMovementOn : Properties.Resources.TouchpadMovementOff);
        //            touchreleased[deviceID] = false;
        //        }
        //    }
        //    else
        //        touchreleased[deviceID] = true;            
        //}

        public virtual void StartTPOff(int deviceID)
        {
            if (deviceID < 4)
            {
                oldtouchvalue[deviceID] = Global.TouchSensitivity[deviceID];
                oldscrollvalue[deviceID] = Global.ScrollSensitivity[deviceID];
                Global.TouchSensitivity[deviceID] = 0;
                Global.ScrollSensitivity[deviceID] = 0;
            }
        }
            
        //public virtual string TouchpadSlide(int ind)
        //{
        //    MiState cState = CurrentState[ind];
        //    string slidedir = "none";
        //    if (MiControllers[ind] != null)
        //        if (cState.Touch2)
        //            if (MiControllers[ind] != null)
        //                if (touchPad[ind].slideright && !touchslid[ind])
        //                {
        //                    slidedir = "right";
        //                    touchslid[ind] = true;
        //                }
        //                else if (touchPad[ind].slideleft && !touchslid[ind])
        //                {
        //                    slidedir = "left";
        //                    touchslid[ind] = true;
        //                }
        //                else if (!touchPad[ind].slideleft && !touchPad[ind].slideright)
        //                {
        //                    slidedir = "";
        //                    touchslid[ind] = false;
        //                }
        //    return slidedir;
        //}
        public virtual void LogDebug(String Data, bool warning = false)
        {
            Console.WriteLine(System.DateTime.Now.ToString("G") + "> " + Data);
            if (Debug != null)
            {
                DebugEventArgs args = new DebugEventArgs(Data, warning);
                OnDebug(this, args);
            }
        }

        public virtual void OnDebug(object sender, DebugEventArgs args)
        {
            if (Debug != null)
                Debug(this, args);
        }

        //sets the rumble adjusted with rumble boost
        public virtual void setRumble(byte heavyMotor, byte lightMotor, int deviceNum)
        {
            byte boost = Global.RumbleBoost[deviceNum];
            uint lightBoosted = ((uint)lightMotor * (uint)boost) / 100;
            if (lightBoosted > 255)
                lightBoosted = 255;
            uint heavyBoosted = ((uint)heavyMotor * (uint)boost) / 100;
            if (heavyBoosted > 255)
                heavyBoosted = 255;
            if (deviceNum < 4)
                if (MiControllers[deviceNum] != null)
                    MiControllers[deviceNum].setRumble((byte)lightBoosted, (byte)heavyBoosted);
        }

        public MiState getMiState(int ind)
        {
            return CurrentState[ind];
        }
        public MiState getMiStateMapped(int ind)
        {
            return MappedState[ind];
        }        
    }
}
