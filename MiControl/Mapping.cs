using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
namespace MiWindows
{
    public class Mapping
    {
        /*
         * Represent the synthetic keyboard and mouse events.  Maintain counts for each so we don't duplicate events.
         */
        public class SyntheticState
        {
            public struct MouseClick
            {
                public int leftCount, middleCount, rightCount, fourthCount, fifthCount, wUpCount, wDownCount, toggleCount;
                public bool toggle;
            }
            public MouseClick previousClicks, currentClicks;
            public struct KeyPress
            {
                public int vkCount, scanCodeCount, repeatCount, toggleCount; // repeat takes priority over non-, and scancode takes priority over non-
                public bool toggle;
            }
            public class KeyPresses
            {
                public KeyPress previous, current;
            }
            public Dictionary<UInt16, KeyPresses> keyPresses = new Dictionary<UInt16, KeyPresses>();

            public void SavePrevious(bool performClear)
            {
                previousClicks = currentClicks;
                if (performClear)
                    currentClicks.leftCount = currentClicks.middleCount = currentClicks.rightCount = currentClicks.fourthCount = currentClicks.fifthCount = currentClicks.wUpCount = currentClicks.wDownCount = currentClicks.toggleCount = 0;
                foreach (KeyPresses kp in keyPresses.Values)
                {
                    kp.previous = kp.current;
                    if (performClear)
                    {
                        kp.current.repeatCount = kp.current.scanCodeCount = kp.current.vkCount = kp.current.toggleCount = 0;
                        //kp.current.toggle = false;
                    }
                }
            }
        }
        public static SyntheticState globalState = new SyntheticState();
        public static SyntheticState[] deviceState = { new SyntheticState(), new SyntheticState(), new SyntheticState(), new SyntheticState() };

        // TODO When we disconnect, process a null/dead state to release any keys or buttons.
        public static DateTime oldnow = DateTime.UtcNow;
        private static bool pressagain = false;
        private static int wheel = 0, keyshelddown = 0;

        //mapcustom
        public static bool[] pressedonce = new bool[261], macrodone = new bool[34];
        static bool[] macroControl = new bool[25];

        //actions
        public static int[] fadetimer = { 0, 0, 0, 0 };
        public static int[] prevFadetimer = { 0, 0, 0, 0 };
        public static DS4Color[] lastColor = new DS4Color[4];
        public static bool[,] actionDone = new bool[4, 50];
        public static SpecialAction[] untriggeraction = new SpecialAction[4];
        public static DateTime[] nowAction = { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        public static DateTime[] oldnowAction = { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        public static int[] untriggerindex = { -1, -1, -1, -1 };
        public static DateTime[] oldnowKeyAct = { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };

        //special macros
        static bool altTabDone = true;
        static DateTime altTabNow = DateTime.UtcNow, oldAltTabNow = DateTime.UtcNow - TimeSpan.FromSeconds(1);

        //mouse
        public static int mcounter = 34;
        public static int mouseaccel = 0;
        public static int prevmouseaccel = 0;
        private static double horizontalRemainder = 0.0, verticalRemainder = 0.0;

        public static void Commit(int device)
        {
            SyntheticState state = deviceState[device];
            lock (globalState)
            {
                globalState.currentClicks.leftCount += state.currentClicks.leftCount - state.previousClicks.leftCount;
                globalState.currentClicks.middleCount += state.currentClicks.middleCount - state.previousClicks.middleCount;
                globalState.currentClicks.rightCount += state.currentClicks.rightCount - state.previousClicks.rightCount;
                globalState.currentClicks.fourthCount += state.currentClicks.fourthCount - state.previousClicks.fourthCount;
                globalState.currentClicks.fifthCount += state.currentClicks.fifthCount - state.previousClicks.fifthCount;
                globalState.currentClicks.wUpCount += state.currentClicks.wUpCount - state.previousClicks.wUpCount;
                globalState.currentClicks.wDownCount += state.currentClicks.wDownCount - state.previousClicks.wDownCount;
                globalState.currentClicks.toggleCount += state.currentClicks.toggleCount - state.previousClicks.toggleCount;
                globalState.currentClicks.toggle = state.currentClicks.toggle;

                if (globalState.currentClicks.toggleCount != 0 && globalState.previousClicks.toggleCount == 0 && globalState.currentClicks.toggle)
                {
                    if (globalState.currentClicks.leftCount != 0 && globalState.previousClicks.leftCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTDOWN);
                    if (globalState.currentClicks.rightCount != 0 && globalState.previousClicks.rightCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTDOWN);
                    if (globalState.currentClicks.middleCount != 0 && globalState.previousClicks.middleCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEDOWN);
                    if (globalState.currentClicks.fourthCount != 0 && globalState.previousClicks.fourthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 1);
                    if (globalState.currentClicks.fifthCount != 0 && globalState.previousClicks.fifthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 2);
                }
                else if (globalState.currentClicks.toggleCount != 0 && globalState.previousClicks.toggleCount == 0 && !globalState.currentClicks.toggle)
                {
                    if (globalState.currentClicks.leftCount != 0 && globalState.previousClicks.leftCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTUP);
                    if (globalState.currentClicks.rightCount != 0 && globalState.previousClicks.rightCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTUP);
                    if (globalState.currentClicks.middleCount != 0 && globalState.previousClicks.middleCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEUP);
                    if (globalState.currentClicks.fourthCount != 0 && globalState.previousClicks.fourthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 1);
                    if (globalState.currentClicks.fifthCount != 0 && globalState.previousClicks.fifthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 2);
                }

                if (globalState.currentClicks.toggleCount == 0 && globalState.previousClicks.toggleCount == 0)
                {
                    if (globalState.currentClicks.leftCount != 0 && globalState.previousClicks.leftCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTDOWN);
                    else if (globalState.currentClicks.leftCount == 0 && globalState.previousClicks.leftCount != 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTUP);

                    if (globalState.currentClicks.middleCount != 0 && globalState.previousClicks.middleCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEDOWN);
                    else if (globalState.currentClicks.middleCount == 0 && globalState.previousClicks.middleCount != 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEUP);

                    if (globalState.currentClicks.rightCount != 0 && globalState.previousClicks.rightCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTDOWN);
                    else if (globalState.currentClicks.rightCount == 0 && globalState.previousClicks.rightCount != 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTUP);

                    if (globalState.currentClicks.fourthCount != 0 && globalState.previousClicks.fourthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 1);
                    else if (globalState.currentClicks.fourthCount == 0 && globalState.previousClicks.fourthCount != 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 1);

                    if (globalState.currentClicks.fifthCount != 0 && globalState.previousClicks.fifthCount == 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 2);
                    else if (globalState.currentClicks.fifthCount == 0 && globalState.previousClicks.fifthCount != 0)
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 2);

                    if (globalState.currentClicks.wUpCount != 0 && globalState.previousClicks.wUpCount == 0)
                    {
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_WHEEL, 100);
                        oldnow = DateTime.UtcNow;
                        wheel = 100;
                    }
                    else if (globalState.currentClicks.wUpCount == 0 && globalState.previousClicks.wUpCount != 0)
                        wheel = 0;

                    if (globalState.currentClicks.wDownCount != 0 && globalState.previousClicks.wDownCount == 0)
                    {
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_WHEEL, -100);
                        oldnow = DateTime.UtcNow;
                        wheel = -100;
                    }
                    if (globalState.currentClicks.wDownCount == 0 && globalState.previousClicks.wDownCount != 0)
                        wheel = 0;
                }
            

                if (wheel != 0) //Continue mouse wheel movement
                {
                    DateTime now = DateTime.UtcNow;
                    if (now >= oldnow + TimeSpan.FromMilliseconds(100) && !pressagain)
                    {
                        oldnow = now;
                        InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_WHEEL, wheel);
                    }
                }

                // Merge and synthesize all key presses/releases that are present in this device's mapping.
                // TODO what about the rest?  e.g. repeat keys really ought to be on some set schedule
                foreach (KeyValuePair<UInt16, SyntheticState.KeyPresses> kvp in state.keyPresses)
                {
                    SyntheticState.KeyPresses gkp;
                    if (globalState.keyPresses.TryGetValue(kvp.Key, out gkp))
                    {
                        gkp.current.vkCount += kvp.Value.current.vkCount - kvp.Value.previous.vkCount;
                        gkp.current.scanCodeCount += kvp.Value.current.scanCodeCount - kvp.Value.previous.scanCodeCount;
                        gkp.current.repeatCount += kvp.Value.current.repeatCount - kvp.Value.previous.repeatCount;
                        gkp.current.toggle = kvp.Value.current.toggle;
                        gkp.current.toggleCount += kvp.Value.current.toggleCount - kvp.Value.previous.toggleCount;
                    }
                    else
                    {
                        gkp = new SyntheticState.KeyPresses();
                        gkp.current = kvp.Value.current;
                        globalState.keyPresses[kvp.Key] = gkp;
                    }
                    if (gkp.current.toggleCount != 0 && gkp.previous.toggleCount == 0 && gkp.current.toggle)
                    {
                        if (gkp.current.scanCodeCount != 0)
                            InputMethods.performSCKeyPress(kvp.Key);
                        else
                            InputMethods.performKeyPress(kvp.Key);
                    }
                    else if (gkp.current.toggleCount != 0 && gkp.previous.toggleCount == 0 && !gkp.current.toggle)
                    {
                        if (gkp.previous.scanCodeCount != 0) // use the last type of VK/SC
                            InputMethods.performSCKeyRelease(kvp.Key);
                        else
                         InputMethods.performKeyRelease(kvp.Key);
                    }
                    else if (gkp.current.vkCount + gkp.current.scanCodeCount != 0 && gkp.previous.vkCount + gkp.previous.scanCodeCount == 0)
                    {
                        if (gkp.current.scanCodeCount != 0)
                        {
                            oldnow = DateTime.UtcNow;
                            InputMethods.performSCKeyPress(kvp.Key);
                            pressagain = false;
                            keyshelddown = kvp.Key;
                        }
                        else
                        {
                            oldnow = DateTime.UtcNow;
                            InputMethods.performKeyPress(kvp.Key);
                            pressagain = false;
                            keyshelddown = kvp.Key;
                        }
                    }
                    else if (gkp.current.toggleCount != 0 || gkp.previous.toggleCount != 0 || gkp.current.repeatCount != 0 || // repeat or SC/VK transition
                        ((gkp.previous.scanCodeCount == 0) != (gkp.current.scanCodeCount == 0))) //repeat keystroke after 500ms
                    {
                        if (keyshelddown == kvp.Key)
                        {
                            DateTime now = DateTime.UtcNow;
                            if (now >= oldnow + TimeSpan.FromMilliseconds(500) && !pressagain)
                            {
                                oldnow = now;
                                pressagain = true;
                            }
                            if (pressagain && gkp.current.scanCodeCount != 0)
                            {
                                now = DateTime.UtcNow;
                                if (now >= oldnow + TimeSpan.FromMilliseconds(25) && pressagain)
                                {
                                    oldnow = now;
                                    InputMethods.performSCKeyPress(kvp.Key);
                                }
                            }
                            else if (pressagain)
                            {
                                now = DateTime.UtcNow;
                                if (now >= oldnow + TimeSpan.FromMilliseconds(25) && pressagain)
                                {
                                    oldnow = now;
                                    InputMethods.performKeyPress(kvp.Key);
                                }
                            }
                        }
                    }
                    if ((gkp.current.toggleCount == 0 && gkp.previous.toggleCount == 0) && gkp.current.vkCount + gkp.current.scanCodeCount == 0 && gkp.previous.vkCount + gkp.previous.scanCodeCount != 0)
                    {
                        if (gkp.previous.scanCodeCount != 0) // use the last type of VK/SC
                        {
                            InputMethods.performSCKeyRelease(kvp.Key);
                            pressagain = false;
                        }
                        else
                        {
                            InputMethods.performKeyRelease(kvp.Key);
                            pressagain = false;
                        }
                    }
                }
                globalState.SavePrevious(false);
            }
            state.SavePrevious(true);
        }
        public enum Click { None, Left, Middle, Right, Fourth, Fifth, WUP, WDOWN };
        public static void MapClick(int device, Click mouseClick)
        {
            switch (mouseClick)
            {
                case Click.Left:
                    deviceState[device].currentClicks.leftCount++;
                    break;
                case Click.Middle:
                    deviceState[device].currentClicks.middleCount++;
                    break;
                case Click.Right:
                    deviceState[device].currentClicks.rightCount++;
                    break;
                case Click.Fourth:
                    deviceState[device].currentClicks.fourthCount++;
                    break;
                case Click.Fifth:
                    deviceState[device].currentClicks.fifthCount++;
                    break;
                case Click.WUP:
                    deviceState[device].currentClicks.wUpCount++;
                    break;
                case Click.WDOWN:
                    deviceState[device].currentClicks.wDownCount++;
                    break;
            }
        }

        public static int DS4ControltoInt(MiControls ctrl)
        {
            switch (ctrl)
            {
                case MiControls.Back: return 1;
                case MiControls.Menu: return 2;
                case MiControls.L1: return 3;
                case MiControls.R1: return 4;
                case MiControls.LS: return 5;
                case MiControls.RS: return 6;
                case MiControls.DpadUp: return 7;
                case MiControls.DpadDown: return 8;
                case MiControls.DpadLeft: return 9;
                case MiControls.DpadRight: return 10;
                case MiControls.HomeSimulated: return 11;
                case MiControls.A: return 12;
                case MiControls.X: return 13;
                case MiControls.Y: return 14;
                case MiControls.B: return 15;
                case MiControls.LXNeg: return 16;
                case MiControls.LYNeg: return 17;
                case MiControls.RXNeg: return 18;
                case MiControls.RYNeg: return 19;
                case MiControls.LXPos: return 20;
                case MiControls.LYPos: return 21;
                case MiControls.RXPos: return 22;
                case MiControls.RYPos: return 23;
                case MiControls.LT: return 24;
                case MiControls.RT: return 25;
                //case MiControls.TouchMulti: return 26;
                //case MiControls.TouchLeft: return 27;
                //case MiControls.TouchRight: return 28;
                //case MiControls.TouchUpper: return 29;
                //case MiControls.GyroXNeg: return 30;
                //case MiControls.GyroXPos: return 31;
                //case MiControls.GyroZNeg: return 32;
                //case MiControls.GyroZPos: return 33;
            }
            return 0; 
        }

        static double TValue(double value1, double value2, double percent)
        {
            percent /= 100f;
            return value1 * percent + value2 * (1 - percent);
        }

        public static MiState SetCurveAndDeadzone(int device, MiState cState)
        {
            MiState dState = new MiState(cState);
            int x;
            int y;
            int curve;
            if (Global.LSCurve[device] > 0)
            {
                x = cState.LX;
                y = cState.LY;
                float max = x + y;
                double curvex;
                double curvey;
                curve = Global.LSCurve[device];
                double multimax = TValue(382.5, max, curve);
                double multimin = TValue(127.5, max, curve);
                if ((x > 127.5f && y > 127.5f) || (x < 127.5f && y < 127.5f))
                {
                    curvex = (x > 127.5f ? Math.Min(x, (x / max) * multimax) : Math.Max(x, (x / max) * multimin));
                    curvey = (y > 127.5f ? Math.Min(y, (y / max) * multimax) : Math.Max(y, (y / max) * multimin));
                    //btnLSTrack.Location = new Point((int)(dpix * curvex / 2.09 + lbLSTrack.Location.X), (int)(dpiy * curvey / 2.09 + lbLSTrack.Location.Y));
                }
                else
                {
                    if (x < 127.5f)
                    {
                        curvex = Math.Min(x, (x / max) * multimax);
                        curvey = Math.Min(y, (-(y / max) * multimax + 510));
                    }
                    else
                    {
                        curvex = Math.Min(x, (-(x / max) * multimax + 510));
                        curvey = Math.Min(y, (y / max) * multimax);
                    }
                }
                dState.LX = (byte)Math.Round(curvex, 0);
                dState.LY = (byte)Math.Round(curvey, 0);
            }
            if (Global.RSCurve[device] > 0)
            {
                x = cState.RX;
                y = cState.RY;
                float max = x + y;
                double curvex;
                double curvey;
                curve = Global.RSCurve[device];
                double multimax = TValue(382.5, max, curve);
                double multimin = TValue(127.5, max, curve);
                if ((x > 127.5f && y > 127.5f) || (x < 127.5f && y < 127.5f))
                {
                    curvex = (x > 127.5f ? Math.Min(x, (x / max) * multimax) : Math.Max(x, (x / max) * multimin));
                    curvey = (y > 127.5f ? Math.Min(y, (y / max) * multimax) : Math.Max(y, (y / max) * multimin));
                }
                else
                {
                    if (x < 127.5f)
                    {
                        curvex = Math.Min(x, (x / max) * multimax);
                        curvey = Math.Min(y, (-(y / max) * multimax + 510));
                    }
                    else
                    {
                        curvex = Math.Min(x, (-(x / max) * multimax + 510));
                        curvey = Math.Min(y, (y / max) * multimax);
                    }
                }
                dState.RX = (byte)Math.Round(curvex, 0);
                dState.RY = (byte)Math.Round(curvey, 0);
            }
            double ls = Math.Sqrt(Math.Pow(cState.LX - 127.5f, 2) + Math.Pow(cState.LY - 127.5f, 2));
            //deadzones
            if (Global.LSDeadzone[device] > 0 && ls < Global.LSDeadzone[device])
            {
                dState.LX = 127;
                dState.LY = 127;
            }
            else if (Global.LSDeadzone[device] < 0 && ls > 127.5f + Global.LSDeadzone[device])
            {
                double r = Math.Atan2((dState.LY - 127.5f), (dState.LX - 127.5f));
                dState.LX = (byte)(Math.Cos(r) * (127.5f + Global.LSDeadzone[device]) + 127.5f);
                dState.LY = (byte)(Math.Sin(r) * (127.5f + Global.LSDeadzone[device]) + 127.5f);
            }
            //Console.WriteLine
            double rs = Math.Sqrt(Math.Pow(cState.RX - 127.5f, 2) + Math.Pow(cState.RY - 127.5f, 2));
            if (Global.RSDeadzone[device] > 0 && rs < Global.LSDeadzone[device])
            {
                dState.RX = 127;
                dState.RY = 127;
            }
            else if (Global.RSDeadzone[device] < 0 && rs > 127.5f + Global.RSDeadzone[device])
            {
                double r = Math.Atan2((dState.RY - 127.5f), (dState.RX - 127.5f));
                dState.RX = (byte)(Math.Cos(r) * (127.5f + Global.RSDeadzone[device]) + 127.5f);
                dState.RY = (byte)(Math.Sin(r) * (127.5f + Global.RSDeadzone[device]) + 127.5f);
            }
            if (Global.L2Deadzone[device] > 0 && cState.LT < Global.L2Deadzone[device])
                dState.LT = 0;
            if (Global.R2Deadzone[device] > 0 && cState.RT < Global.R2Deadzone[device])
                dState.RT = 0;
            return dState;
        }

        /// <summary>
        /// Map DS4 Buttons/Axes to other DS4 Buttons/Axes (largely the same as Xinput ones) and to keyboard and mouse buttons.
        /// </summary>
        public static void MapCustom(int device, MiState cState, MiState MappedState, MiStateExposed eState, Mouse tp, ControlService ctrl)
        {
            bool shift;
            
            MappedState.LX = 127;
            MappedState.LY = 127;
            MappedState.RX = 127;
            MappedState.RY = 127;
            int MouseDeltaX = 0;
            int MouseDeltaY = 0;
            
            SyntheticState deviceState = Mapping.deviceState[device];
            if (Global.GetActions().Count > 0 && (Global.ProfileActions[device].Count > 0 ||
                !string.IsNullOrEmpty(Global.tempprofilename[device])))
                MapCustomAction(device, cState, MappedState, eState, tp, ctrl);
            if (ctrl.MiControllers[device] == null) return;
            switch (Global.ShiftModifier[device])
            {
                case 1: shift = getBoolMapping(MiControls.A, cState, eState, tp); break;
                case 2: shift = getBoolMapping(MiControls.B, cState, eState, tp); break;
                case 3: shift = getBoolMapping(MiControls.X, cState, eState, tp); break;
                case 4: shift = getBoolMapping(MiControls.Y, cState, eState, tp); break;
                case 5: shift = getBoolMapping(MiControls.Menu, cState, eState, tp); break;
                case 6: shift = getBoolMapping(MiControls.Back, cState, eState, tp); break;
                case 7: shift = getBoolMapping(MiControls.DpadUp, cState, eState, tp); break;
                case 8: shift = getBoolMapping(MiControls.DpadDown, cState, eState, tp); break;
                case 9: shift = getBoolMapping(MiControls.DpadLeft, cState, eState, tp); break;
                case 10: shift = getBoolMapping(MiControls.DpadRight, cState, eState, tp); break;
                case 11: shift = getBoolMapping(MiControls.HomeSimulated, cState, eState, tp); break;
                case 12: shift = getBoolMapping(MiControls.L1, cState, eState, tp); break;
                case 13: shift = getBoolMapping(MiControls.R1, cState, eState, tp); break;
                case 14: shift = getBoolMapping(MiControls.LT, cState, eState, tp); break;
                case 15: shift = getBoolMapping(MiControls.RT, cState, eState, tp); break;
                case 16: shift = getBoolMapping(MiControls.LS, cState, eState, tp); break;
                case 17: shift = getBoolMapping(MiControls.RS, cState, eState, tp); break;
                //case 18: shift = getBoolMapping(MiControls.TouchLeft, cState, eState, tp); break;
                //case 19: shift = getBoolMapping(MiControls.TouchUpper, cState, eState, tp); break;
                //case 20: shift = getBoolMapping(MiControls.TouchMulti, cState, eState, tp); break;
                //case 21: shift = getBoolMapping(MiControls.TouchRight, cState, eState, tp); break;
                //case 22: shift = getBoolMapping(MiControls.GyroZNeg, cState, eState, tp); break;
                //case 23: shift = getBoolMapping(MiControls.GyroZPos, cState, eState, tp); break;
                //case 24: shift = getBoolMapping(MiControls.GyroXPos, cState, eState, tp); break;
                //case 25: shift = getBoolMapping(MiControls.GyroXNeg, cState, eState, tp); break;
                //case 26: shift = cState.Touch1; break;
                default: shift = false; break;
            }
            cState.CopyTo(MappedState);
            if (shift)
                MapShiftCustom(device, cState, MappedState, eState, tp);
            foreach (KeyValuePair<MiControls, string> customKey in Global.getCustomMacros(device))
            {
                if (shift == false ||
                    (Global.getShiftCustomMacro(device, customKey.Key) == "0" &&
                    Global.getShiftCustomKey(device, customKey.Key) == 0 &&
                    Global.getShiftCustomButton(device, customKey.Key) == X360Controls.None))
                {
                    DS4KeyType keyType = Global.getCustomKeyType(device, customKey.Key);
                    if (getBoolMapping(customKey.Key, cState, eState, tp))
                    {
                        resetToDefaultValue(customKey.Key, MappedState);
                        PlayMacro(device, macroControl, customKey.Value, customKey.Key, keyType);
                    }
                    else if (!getBoolMapping(customKey.Key, cState, eState, tp))
                    {
                        EndMacro(device, macroControl, customKey.Value, customKey.Key);
                    }
                }
            }
            foreach (KeyValuePair<MiControls, ushort> customKey in Global.getCustomKeys(device))
            {
                if (shift == false ||
                    (Global.getShiftCustomMacro(device, customKey.Key) == "0" &&
                    Global.getShiftCustomKey(device, customKey.Key) == 0 &&
                    Global.getShiftCustomButton(device, customKey.Key) == X360Controls.None))
                {
                    DS4KeyType keyType = Global.getCustomKeyType(device, customKey.Key);
                    if (getBoolMapping(customKey.Key, cState, eState, tp))
                    {
                        resetToDefaultValue(customKey.Key, MappedState);
                        SyntheticState.KeyPresses kp;
                        if (!deviceState.keyPresses.TryGetValue(customKey.Value, out kp))
                            deviceState.keyPresses[customKey.Value] = kp = new SyntheticState.KeyPresses();
                        if (keyType.HasFlag(DS4KeyType.ScanCode))
                            kp.current.scanCodeCount++;
                        else
                            kp.current.vkCount++;
                        if (keyType.HasFlag(DS4KeyType.Toggle))
                        {
                            if (!pressedonce[customKey.Value])
                            {
                                kp.current.toggle = !kp.current.toggle;
                                pressedonce[customKey.Value] = true;
                            }
                            kp.current.toggleCount++;
                        }
                        kp.current.repeatCount++;
                    }
                    else
                        pressedonce[customKey.Value] = false;
                }
            }

            //Dictionary<DS4Controls, X360Controls> customButtons = Global.getCustomButtons(device);
            //foreach (KeyValuePair<DS4Controls, X360Controls> customButton in customButtons)
            List<MiControls> A = new List<MiControls>();
            List<MiControls> B = new List<MiControls>();
            List<MiControls> X = new List<MiControls>();
            List<MiControls> Y = new List<MiControls>();
            List<MiControls> Menu = new List<MiControls>();
            List<MiControls> Back = new List<MiControls>();
            List<MiControls> DpadUp = new List<MiControls>();
            List<MiControls> DpadDown = new List<MiControls>();
            List<MiControls> DpadLeft = new List<MiControls>();
            List<MiControls> DpadRight = new List<MiControls>();
            List<MiControls> HomeSimulated = new List<MiControls>();
            List<MiControls> L1 = new List<MiControls>();
            List<MiControls> R1 = new List<MiControls>();
            List<MiControls> LT = new List<MiControls>();
            List<MiControls> RT = new List<MiControls>();
            List<MiControls> LS = new List<MiControls>();
            List<MiControls> RS = new List<MiControls>();
            List<MiControls> LXN = new List<MiControls>();
            List<MiControls> LXP = new List<MiControls>();
            List<MiControls> LYN = new List<MiControls>();
            List<MiControls> LYP = new List<MiControls>();
            List<MiControls> RXN = new List<MiControls>();
            List<MiControls> RXP = new List<MiControls>();
            List<MiControls> RYN = new List<MiControls>();
            List<MiControls> RYP = new List<MiControls>();
            foreach (KeyValuePair<MiControls, X360Controls> customButton in Global.getCustomButtons(device))
            {
                if (shift == false ||
                    (Global.getShiftCustomMacro(device, customButton.Key) == "0" &&
                    Global.getShiftCustomKey(device, customButton.Key) == 0 &&
                    Global.getShiftCustomButton(device, customButton.Key) == X360Controls.None))
                {
                    DS4KeyType keyType = Global.getCustomKeyType(device, customButton.Key);
                    int keyvalue = 0;
                    switch (customButton.Value)
                    {
                        case X360Controls.LeftMouse: keyvalue = 256; break;
                        case X360Controls.RightMouse: keyvalue = 257; break;
                        case X360Controls.MiddleMouse: keyvalue = 258; break;
                        case X360Controls.FourthMouse: keyvalue = 259; break;
                        case X360Controls.FifthMouse: keyvalue = 260; break;
                    }
                    if (keyType.HasFlag(DS4KeyType.Toggle))
                    {
                        if (getBoolMapping(customButton.Key, cState, eState, tp))
                        {
                            resetToDefaultValue(customButton.Key, MappedState);
                            if (!pressedonce[keyvalue])
                            {
                                deviceState.currentClicks.toggle = !deviceState.currentClicks.toggle;
                                pressedonce[keyvalue] = true;
                            }
                            deviceState.currentClicks.toggleCount++;
                        }
                        else
                        {
                            pressedonce[keyvalue] = false;
                        }
                    }
                    resetToDefaultValue(customButton.Key, MappedState); // erase default mappings for things that are remapped
                    bool isAnalog = customButton.Key.ToString().Contains("LX") ||
                                    customButton.Key.ToString().Contains("RX") ||
                                    customButton.Key.ToString().Contains("LY") ||
                                    customButton.Key.ToString().Contains("LY") ||
                                    customButton.Key.ToString().Contains("R2") ||
                                    customButton.Key.ToString().Contains("L2") ||
                                    customButton.Key.ToString().Contains("Gyro");
                    switch (customButton.Value)
                    {
                        case X360Controls.A: A.Add(customButton.Key); break;
                        case X360Controls.B: B.Add(customButton.Key); break;
                        case X360Controls.X: X.Add(customButton.Key); break;
                        case X360Controls.Y: Y.Add(customButton.Key); break;
                        case X360Controls.LB: L1.Add(customButton.Key); break;
                        case X360Controls.LS: LS.Add(customButton.Key); break;
                        case X360Controls.RB: R1.Add(customButton.Key); break;
                        case X360Controls.RS: RS.Add(customButton.Key); break;
                        case X360Controls.DpadUp: DpadUp.Add(customButton.Key); break;
                        case X360Controls.DpadDown: DpadDown.Add(customButton.Key); break;
                        case X360Controls.DpadLeft: DpadLeft.Add(customButton.Key); break;
                        case X360Controls.DpadRight: DpadRight.Add(customButton.Key); break;
                        case X360Controls.Start: Menu.Add(customButton.Key); break;
                        case X360Controls.Guide: HomeSimulated.Add(customButton.Key); break;
                        case X360Controls.Back: Back.Add(customButton.Key); break;
                        case X360Controls.LXNeg: LXN.Add(customButton.Key); break;
                        case X360Controls.LYNeg: LYN.Add(customButton.Key); break;
                        case X360Controls.RXNeg: RXN.Add(customButton.Key); break;
                        case X360Controls.RYNeg: RYN.Add(customButton.Key); break;
                        case X360Controls.LXPos: LXP.Add(customButton.Key); break;
                        case X360Controls.LYPos: LYP.Add(customButton.Key); break;
                        case X360Controls.RXPos: RXP.Add(customButton.Key); break;
                        case X360Controls.RYPos: RYP.Add(customButton.Key); break;
                        case X360Controls.LT: LT.Add(customButton.Key); break;
                        case X360Controls.RT: RT.Add(customButton.Key); break;
                        case X360Controls.LeftMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.leftCount++;
                            break;
                        case X360Controls.RightMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.rightCount++;
                            break;
                        case X360Controls.MiddleMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.middleCount++;
                            break;
                        case X360Controls.FourthMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.fourthCount++;
                            break;
                        case X360Controls.FifthMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.fifthCount++;
                            break;
                        case X360Controls.WUP:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                if (isAnalog)
                                    getMouseWheelMapping(device, customButton.Key, cState, eState, tp, false);
                                else
                                    deviceState.currentClicks.wUpCount++;
                            break;
                        case X360Controls.WDOWN:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                if (isAnalog)
                                    getMouseWheelMapping(device, customButton.Key, cState, eState, tp, true);
                                else
                                    deviceState.currentClicks.wDownCount++;
                            break;
                        case X360Controls.MouseUp:
                            if (MouseDeltaY == 0)
                            {
                                MouseDeltaY = getMouseMapping(device, customButton.Key, cState, eState, 0);
                                MouseDeltaY = -Math.Abs((MouseDeltaY == -2147483648 ? 0 : MouseDeltaY));
                            }
                            break;
                        case X360Controls.MouseDown:
                            if (MouseDeltaY == 0)
                            {
                                MouseDeltaY = getMouseMapping(device, customButton.Key, cState, eState, 1);
                                MouseDeltaY = Math.Abs((MouseDeltaY == -2147483648 ? 0 : MouseDeltaY));
                            }
                            break;
                        case X360Controls.MouseLeft:
                            if (MouseDeltaX == 0)
                            {
                                MouseDeltaX = getMouseMapping(device, customButton.Key, cState, eState, 2);
                                MouseDeltaX = -Math.Abs((MouseDeltaX == -2147483648 ? 0 : MouseDeltaX));
                            }
                            break;
                        case X360Controls.MouseRight:
                            if (MouseDeltaX == 0)
                            {
                                MouseDeltaX = getMouseMapping(device, customButton.Key, cState, eState, 3);
                                MouseDeltaX = Math.Abs((MouseDeltaX == -2147483648 ? 0 : MouseDeltaX));
                            }
                            break;
                    }
                }
            }
            if (macroControl[00]) MappedState.A = true;
            if (macroControl[01]) MappedState.B = true;
            if (macroControl[02]) MappedState.X = true;
            if (macroControl[03]) MappedState.Y = true;
            if (macroControl[04]) MappedState.Menu = true;
            if (macroControl[05]) MappedState.Back = true;
            if (macroControl[06]) MappedState.DpadUp = true;
            if (macroControl[07]) MappedState.DpadDown = true;
            if (macroControl[08]) MappedState.DpadLeft = true;
            if (macroControl[09]) MappedState.DpadRight = true;
            if (macroControl[10]) MappedState.HomeSimulated = true;
            if (macroControl[11]) MappedState.L1 = true;
            if (macroControl[12]) MappedState.R1 = true;
            if (macroControl[13]) MappedState.LT = 255;
            if (macroControl[14]) MappedState.RT = 255;
            if (macroControl[15]) MappedState.LS = true;
            if (macroControl[16]) MappedState.RS = true;
            if (macroControl[17]) MappedState.LX = 255;
            if (macroControl[18]) MappedState.LX = 0;
            if (macroControl[19]) MappedState.LY = 255;
            if (macroControl[20]) MappedState.LY = 0;
            if (macroControl[21]) MappedState.RX = 255;
            if (macroControl[22]) MappedState.RX = 0;
            if (macroControl[23]) MappedState.RY = 255;
            if (macroControl[24]) MappedState.RY = 0;
            foreach (MiControls dc in A)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.A = true;
            foreach (MiControls dc in B)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.B = true;
            foreach (MiControls dc in X)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.X = true;
            foreach (MiControls dc in Y)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Y = true;
            foreach (MiControls dc in L1)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.L1 = true;
            foreach (MiControls dc in LT)
                if (getByteMapping(device, dc, cState, eState, tp) > 5)
                    MappedState.LT = getByteMapping(device, dc, cState, eState, tp);
            foreach (MiControls dc in LS)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.LS = true;
            foreach (MiControls dc in R1)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.R1 = true;
            foreach (MiControls dc in RT)
                if (getByteMapping(device, dc, cState, eState, tp) > 5)
                    MappedState.RT = getByteMapping(device, dc, cState, eState, tp);
            foreach (MiControls dc in RS)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.RS = true;
            foreach (MiControls dc in DpadUp)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadUp = true;
            foreach (MiControls dc in DpadRight)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadRight = true;
            foreach (MiControls dc in DpadLeft)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadLeft = true;
            foreach (MiControls dc in DpadDown)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadDown = true;
            foreach (MiControls dc in Menu)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Menu = true;
            foreach (MiControls dc in Back)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Back = true;
            foreach (MiControls dc in HomeSimulated)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.HomeSimulated = true;

            if (Global.getCustomButton(device, MiControls.LXNeg) == X360Controls.None)
                LXN.Add(MiControls.LXNeg);
            if (Global.getCustomButton(device, MiControls.LXPos) == X360Controls.None)
                LXP.Add(MiControls.LXPos);
            if (Global.getCustomButton(device, MiControls.LYNeg) == X360Controls.None)
                LYN.Add(MiControls.LYNeg);
            if (Global.getCustomButton(device, MiControls.LYPos) == X360Controls.None)
                LYP.Add(MiControls.LYPos);
            if (Global.getCustomButton(device, MiControls.RXNeg) == X360Controls.None)
                RXN.Add(MiControls.RXNeg);
            if (Global.getCustomButton(device, MiControls.RXPos) == X360Controls.None)
                RXP.Add(MiControls.RXPos);
            if (Global.getCustomButton(device, MiControls.RYNeg) == X360Controls.None)
                RYN.Add(MiControls.RYNeg);
            if (Global.getCustomButton(device, MiControls.RYPos) == X360Controls.None)
                RYP.Add(MiControls.RYPos);
            if ((shift && MappedState.LX == 127) || !shift)
                if (LXN.Count > 0 || LXP.Count > 0)
                {
                    foreach (MiControls dc in LXP)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                            MappedState.LX = getXYAxisMapping(device, dc, cState, eState, tp, true);
                    foreach (MiControls dc in LXN)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                            MappedState.LX = getXYAxisMapping(device, dc, cState, eState, tp);
                }
                else
                    MappedState.LX = cState.LX;
            if ((shift && MappedState.LY == 127) || !shift)
                if (LYN.Count > 0 || LYP.Count > 0)
                {
                    foreach (MiControls dc in LYN)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                            MappedState.LY = getXYAxisMapping(device, dc, cState, eState, tp);
                    foreach (MiControls dc in LYP)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                            MappedState.LY = getXYAxisMapping(device, dc, cState, eState, tp, true);
                }
                else
                    MappedState.LY = cState.LY;
            if ((shift && MappedState.RX == 127) || !shift)
                if (RXN.Count > 0 || RXP.Count > 0)
                {
                    foreach (MiControls dc in RXN)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                            MappedState.RX = getXYAxisMapping(device, dc, cState, eState, tp);
                    foreach (MiControls dc in RXP)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                            MappedState.RX = getXYAxisMapping(device, dc, cState, eState, tp, true);
                }
                else
                    MappedState.RX = cState.RX;
            if ((shift && MappedState.RY == 127) || !shift)
                if (RYN.Count > 0 || RYP.Count > 0)
                {
                    foreach (MiControls dc in RYN)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                            MappedState.RY = getXYAxisMapping(device, dc, cState, eState, tp);
                    foreach (MiControls dc in RYP)
                        if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                            MappedState.RY = getXYAxisMapping(device, dc, cState, eState, tp, true);
                }
                else
                    MappedState.RY = cState.RY;
            InputMethods.MoveCursorBy(MouseDeltaX, MouseDeltaY);
        }

        public static void MapShiftCustom(int device, MiState cState, MiState MappedState, MiStateExposed eState, Mouse tp)
        {
            //cState.CopyTo(MappedState);
            SyntheticState deviceState = Mapping.deviceState[device];
            foreach (KeyValuePair<MiControls, string> customKey in Global.getShiftCustomMacros(device)) //with delays
            {
                DS4KeyType keyType = Global.getShiftCustomKeyType(device, customKey.Key);
                if (getBoolMapping(customKey.Key, cState, eState, tp))
                {
                    resetToDefaultValue(customKey.Key, MappedState);
                    PlayMacro(device, macroControl, customKey.Value, customKey.Key, keyType);
                }
                else if (!getBoolMapping(customKey.Key, cState, eState, tp))
                {
                    EndMacro(device, macroControl, customKey.Value, customKey.Key);
                }
            }
            foreach (KeyValuePair<MiControls, ushort> customKey in Global.getShiftCustomKeys(device))
            {
                DS4KeyType keyType = Global.getShiftCustomKeyType(device, customKey.Key);
                if (getBoolMapping(customKey.Key, cState, eState, tp))
                {
                    resetToDefaultValue(customKey.Key, MappedState);
                    SyntheticState.KeyPresses kp;
                    if (!deviceState.keyPresses.TryGetValue(customKey.Value, out kp))
                        deviceState.keyPresses[customKey.Value] = kp = new SyntheticState.KeyPresses();
                    if (keyType.HasFlag(DS4KeyType.ScanCode))
                        kp.current.scanCodeCount++;
                    else
                        kp.current.vkCount++;
                    if (keyType.HasFlag(DS4KeyType.Toggle))
                    {
                        if (!pressedonce[customKey.Value])
                        {
                            kp.current.toggle = !kp.current.toggle;
                            pressedonce[customKey.Value] = true;
                        }
                        kp.current.toggleCount++;
                    }
                    kp.current.repeatCount++;
                }
                else
                    pressedonce[customKey.Value] = false;
            }

            MappedState.LX = 127;
            MappedState.LY = 127;
            MappedState.RX = 127;
            MappedState.RY = 127;
            int MouseDeltaX = 0;
            int MouseDeltaY = 0;

            Dictionary<MiControls, X360Controls> customButtons = Global.getShiftCustomButtons(device);
            //foreach (KeyValuePair<DS4Controls, X360Controls> customButton in customButtons)
            // resetToDefaultValue(customButton.Key, MappedState); // erase default mappings for things that are remapped

            List<MiControls> A = new List<MiControls>();
            List<MiControls> B = new List<MiControls>();
            List<MiControls> X = new List<MiControls>();
            List<MiControls> Y = new List<MiControls>();
            List<MiControls> Menu = new List<MiControls>();
            List<MiControls> Back = new List<MiControls>();
            List<MiControls> DpadUp = new List<MiControls>();
            List<MiControls> DpadDown = new List<MiControls>();
            List<MiControls> DpadLeft = new List<MiControls>();
            List<MiControls> DpadRight = new List<MiControls>();
            List<MiControls> HomeSimulated = new List<MiControls>();
            List<MiControls> L1 = new List<MiControls>();
            List<MiControls> R1 = new List<MiControls>();
            List<MiControls> LT = new List<MiControls>();
            List<MiControls> RT = new List<MiControls>();
            List<MiControls> LS = new List<MiControls>();
            List<MiControls> RS = new List<MiControls>();
            List<MiControls> LXN = new List<MiControls>();
            List<MiControls> LXP = new List<MiControls>();
            List<MiControls> LYN = new List<MiControls>();
            List<MiControls> LYP = new List<MiControls>();
            List<MiControls> RXN = new List<MiControls>();
            List<MiControls> RXP = new List<MiControls>();
            List<MiControls> RYN = new List<MiControls>();
            List<MiControls> RYP = new List<MiControls>();
            foreach (KeyValuePair<MiControls, X360Controls> customButton in customButtons)
            {
                resetToDefaultValue(customButton.Key, MappedState); // erase default mappings for things that are remapped
                DS4KeyType keyType = Global.getShiftCustomKeyType(device, customButton.Key);
                int keyvalue = 0;
                switch (customButton.Value)
                {
                    case X360Controls.LeftMouse: keyvalue = 256; break;
                    case X360Controls.RightMouse: keyvalue = 257; break;
                    case X360Controls.MiddleMouse: keyvalue = 258; break;
                    case X360Controls.FourthMouse: keyvalue = 259; break;
                    case X360Controls.FifthMouse: keyvalue = 260; break;
                }
                if (keyType.HasFlag(DS4KeyType.Toggle))
                {
                    if (getBoolMapping(customButton.Key, cState, eState, tp))
                    {
                        if (!pressedonce[keyvalue])
                        {
                            deviceState.currentClicks.toggle = !deviceState.currentClicks.toggle;
                            pressedonce[keyvalue] = true;
                        }
                        deviceState.currentClicks.toggleCount++;
                    }
                    else
                    {
                        pressedonce[keyvalue] = false;
                    }
                }
               switch (customButton.Value)
                    {
                        case X360Controls.A: A.Add(customButton.Key); break;
                        case X360Controls.B: B.Add(customButton.Key); break;
                        case X360Controls.X: X.Add(customButton.Key); break;
                        case X360Controls.Y: Y.Add(customButton.Key); break;
                        case X360Controls.LB: L1.Add(customButton.Key); break;
                        case X360Controls.LS: LS.Add(customButton.Key); break;
                        case X360Controls.RB: R1.Add(customButton.Key); break;
                        case X360Controls.RS: RS.Add(customButton.Key); break;
                        case X360Controls.DpadUp: DpadUp.Add(customButton.Key); break;
                        case X360Controls.DpadDown: DpadDown.Add(customButton.Key); break;
                        case X360Controls.DpadLeft: DpadLeft.Add(customButton.Key); break;
                        case X360Controls.DpadRight: DpadRight.Add(customButton.Key); break;
                        case X360Controls.Start: Menu.Add(customButton.Key); break;
                        case X360Controls.Guide: HomeSimulated.Add(customButton.Key); break;
                        case X360Controls.Back: Back.Add(customButton.Key); break;
                        case X360Controls.LXNeg: LXN.Add(customButton.Key); break;
                        case X360Controls.LYNeg: LYN.Add(customButton.Key); break;
                        case X360Controls.RXNeg: RXN.Add(customButton.Key); break;
                        case X360Controls.RYNeg: RYN.Add(customButton.Key); break;
                        case X360Controls.LXPos: LXP.Add(customButton.Key); break;
                        case X360Controls.LYPos: LYP.Add(customButton.Key); break;
                        case X360Controls.RXPos: RXP.Add(customButton.Key); break;
                        case X360Controls.RYPos: RYP.Add(customButton.Key); break;
                        case X360Controls.LT: LT.Add(customButton.Key); break;
                        case X360Controls.RT: RT.Add(customButton.Key); break;
                        case X360Controls.LeftMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.leftCount++;
                            break;
                        case X360Controls.RightMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.rightCount++;
                            break;
                        case X360Controls.MiddleMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.middleCount++;
                            break;
                        case X360Controls.FourthMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.fourthCount++;
                            break;
                        case X360Controls.FifthMouse:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.fifthCount++;
                            break;
                        case X360Controls.WUP:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.wUpCount++;
                            break;
                        case X360Controls.WDOWN:
                            if (getBoolMapping(customButton.Key, cState, eState, tp))
                                deviceState.currentClicks.wDownCount++;
                            break;
                        case X360Controls.MouseUp:
                            if (MouseDeltaY == 0)
                            {
                                MouseDeltaY = getMouseMapping(device, customButton.Key, cState, eState, 0);
                                MouseDeltaY = -Math.Abs((MouseDeltaY == -2147483648 ? 0 : MouseDeltaY));
                            }
                            break;
                        case X360Controls.MouseDown:
                            if (MouseDeltaY == 0)
                            {
                                MouseDeltaY = getMouseMapping(device, customButton.Key, cState, eState, 1);
                                MouseDeltaY = Math.Abs((MouseDeltaY == -2147483648 ? 0 : MouseDeltaY));
                            }
                            break;
                        case X360Controls.MouseLeft:
                            if (MouseDeltaX == 0)
                            {
                                MouseDeltaX = getMouseMapping(device, customButton.Key, cState, eState, 2);
                                MouseDeltaX = -Math.Abs((MouseDeltaX == -2147483648 ? 0 : MouseDeltaX));
                            }
                            break;
                        case X360Controls.MouseRight:
                            if (MouseDeltaX == 0)
                            {
                                MouseDeltaX = getMouseMapping(device, customButton.Key, cState, eState, 3);
                                MouseDeltaX = Math.Abs((MouseDeltaX == -2147483648 ? 0 : MouseDeltaX));
                            }
                            break;
                    }
                }
            if (macroControl[0]) MappedState.A = true;
            if (macroControl[1]) MappedState.B = true;
            if (macroControl[2]) MappedState.X = true;
            if (macroControl[3]) MappedState.Y = true;
            if (macroControl[4]) MappedState.Menu = true;
            if (macroControl[5]) MappedState.Back = true;
            if (macroControl[6]) MappedState.DpadUp = true;
            if (macroControl[7]) MappedState.DpadDown = true;
            if (macroControl[8]) MappedState.DpadLeft = true;
            if (macroControl[9]) MappedState.DpadRight = true;
            if (macroControl[10]) MappedState.HomeSimulated = true;
            if (macroControl[11]) MappedState.L1 = true;
            if (macroControl[12]) MappedState.R1 = true;
            if (macroControl[13]) MappedState.LT = 255;
            if (macroControl[14]) MappedState.RT = 255;
            if (macroControl[15]) MappedState.LS = true;
            if (macroControl[16]) MappedState.RS = true;
            if (macroControl[17]) MappedState.LX = 255;
            if (macroControl[18]) MappedState.LX = 0;
            if (macroControl[19]) MappedState.LY = 255;
            if (macroControl[20]) MappedState.LY = 0;
            if (macroControl[21]) MappedState.RX = 255;
            if (macroControl[22]) MappedState.RX = 0;
            if (macroControl[23]) MappedState.RY = 255;
            if (macroControl[24]) MappedState.RY = 0;
            foreach (MiControls dc in A)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.A = true;
            foreach (MiControls dc in B)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.B = true;
            foreach (MiControls dc in X)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.X = true;
            foreach (MiControls dc in Y)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Y = true;
            foreach (MiControls dc in L1)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.L1 = true;
            foreach (MiControls dc in LT)
                if (getByteMapping(device, dc, cState, eState, tp) != 0)
                    MappedState.LT = getByteMapping(device, dc, cState, eState, tp);
            foreach (MiControls dc in LS)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.LS = true;
            foreach (MiControls dc in R1)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.R1 = true;
            foreach (MiControls dc in RT)
                  if (getByteMapping(device, dc, cState, eState, tp) != 0)
                    MappedState.RT = getByteMapping(device, dc, cState, eState, tp);
            foreach (MiControls dc in RS)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.RS = true;
            foreach (MiControls dc in DpadUp)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadUp = true;
            foreach (MiControls dc in DpadRight)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadRight = true;
            foreach (MiControls dc in DpadLeft)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadLeft = true;
            foreach (MiControls dc in DpadDown)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.DpadDown = true;
            foreach (MiControls dc in Menu)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Menu = true;
            foreach (MiControls dc in Back)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.Back = true;
            foreach (MiControls dc in HomeSimulated)
                if (getBoolMapping(dc, cState, eState, tp))
                    MappedState.HomeSimulated = true;

            if (Global.getShiftCustomButton(device, MiControls.LXNeg) == X360Controls.None)
                LXN.Add(MiControls.LXNeg);
            if (Global.getShiftCustomButton(device, MiControls.LXPos) == X360Controls.None)
                LXP.Add(MiControls.LXPos);
            if (Global.getShiftCustomButton(device, MiControls.LYNeg) == X360Controls.None)
                LYN.Add(MiControls.LYNeg);
            if (Global.getShiftCustomButton(device, MiControls.LYPos) == X360Controls.None)
                LYP.Add(MiControls.LYPos);
            if (Global.getShiftCustomButton(device, MiControls.RXNeg) == X360Controls.None)
                RXN.Add(MiControls.RXNeg);
            if (Global.getShiftCustomButton(device, MiControls.RXPos) == X360Controls.None)
                RXP.Add(MiControls.RXPos);
            if (Global.getShiftCustomButton(device, MiControls.RYNeg) == X360Controls.None)
                RYN.Add(MiControls.RYNeg);
            if (Global.getShiftCustomButton(device, MiControls.RYPos) == X360Controls.None)
                RYP.Add(MiControls.RYPos);
            if (LXN.Count > 0 || LXP.Count > 0)
            {
                foreach (MiControls dc in LXP)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                        MappedState.LX = getXYAxisMapping(device, dc, cState, eState, tp, true);
                foreach (MiControls dc in LXN)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                        MappedState.LX = getXYAxisMapping(device, dc, cState, eState, tp);
            }
            else
                MappedState.LX = cState.LX;
            if (LYN.Count > 0 || LYP.Count > 0)
            {
                foreach (MiControls dc in LYN)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                        MappedState.LY = getXYAxisMapping(device, dc, cState, eState, tp);
                foreach (MiControls dc in LYP)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                        MappedState.LY = getXYAxisMapping(device, dc, cState, eState, tp, true);
            }
            else
                MappedState.LY = cState.LY;
            if (RXN.Count > 0 || RXP.Count > 0)
            {
                foreach (MiControls dc in RXN)
                {
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                    {
                        MappedState.RX = getXYAxisMapping(device, dc, cState, eState, tp);
                    }
                }
                foreach (MiControls dc in RXP)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                        MappedState.RX = getXYAxisMapping(device, dc, cState, eState, tp, true);
            }
            else
                MappedState.RX = cState.RX;
            if (RYN.Count > 0 || RYP.Count > 0)
            {
                foreach (MiControls dc in RYN)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp)) > 5)
                        MappedState.RY = getXYAxisMapping(device, dc, cState, eState, tp);
                foreach (MiControls dc in RYP)
                    if (Math.Abs(127 - getXYAxisMapping(device, dc, cState, eState, tp, true)) > 5)
                        MappedState.RY = getXYAxisMapping(device, dc, cState, eState, tp, true);
            }
            else
                MappedState.RY = cState.RY;
            InputMethods.MoveCursorBy(MouseDeltaX, MouseDeltaY);
        }

        public static async void MapCustomAction(int device, MiState cState, MiState MappedState, MiStateExposed eState, Mouse tp, ControlService ctrl)
        {
            foreach (string actionname in Global.ProfileActions[device])
            {
                //DS4KeyType keyType = Global.getShiftCustomKeyType(device, customKey.Key);
                SpecialAction action = Global.GetAction(actionname);
                int index = Global.GetActionIndexOf(actionname);
                double time;
                //If a key or button is assigned to the trigger, a key special action is used like
                //a quick tap to use and hold to use the regular custom button/key
                bool triggerToBeTapped = action.type == "Key" && action.trigger.Count == 1 &&
                        (Global.getCustomMacro(device,  action.trigger[0]) != "0" ||
                    Global.getCustomKey(device, action.trigger[0]) != 0 ||
                    Global.getCustomButton(device, action.trigger[0]) != X360Controls.None);
                if (!(action.name == "null" || index < 0))
                {
                    bool triggeractivated = true;
                    if (action.delayTime > 0)
                    {
                        triggeractivated = false;
                        bool subtriggeractivated = true;
                        foreach (MiControls dc in action.trigger)
                        {
                            if (!getBoolMapping(dc, cState, eState, tp))
                            {
                                subtriggeractivated = false;
                                break;
                            }
                        }
                        if (subtriggeractivated)
                        {
                            time = action.delayTime;
                            nowAction[device] = DateTime.UtcNow;
                            if (nowAction[device] >= oldnowAction[device] + TimeSpan.FromSeconds(time))
                                triggeractivated = true;
                        }
                        else if (nowAction[device] < DateTime.UtcNow - TimeSpan.FromMilliseconds(100))
                            oldnowAction[device] = DateTime.UtcNow;
                    }
                    else if (triggerToBeTapped && oldnowKeyAct[device] == DateTime.MinValue)
                    {
                        triggeractivated = false;
                        bool subtriggeractivated = true;
                        foreach (MiControls dc in action.trigger)
                        {
                            if (!getBoolMapping(dc, cState, eState, tp))
                            {
                                subtriggeractivated = false;
                                break;
                            }
                        }
                        if (subtriggeractivated)
                        {
                            oldnowKeyAct[device] = DateTime.UtcNow;
                        }
                    }
                    else if (triggerToBeTapped && oldnowKeyAct[device] != DateTime.MinValue)
                    {
                        triggeractivated = false;
                        bool subtriggeractivated = true;
                        foreach (MiControls dc in action.trigger)
                        {
                            if (!getBoolMapping(dc, cState, eState, tp))
                            {
                                subtriggeractivated = false;
                                break;
                            }
                        }
                        DateTime now = DateTime.UtcNow;
                        if (!subtriggeractivated && now <= oldnowKeyAct[device] + TimeSpan.FromMilliseconds(250))
                        {
                            await Task.Delay(3); //if the button is assigned to the same key use a delay so the keydown is the last action, not key up
                            triggeractivated = true;
                            oldnowKeyAct[device] = DateTime.MinValue;
                        }
                        else if (!subtriggeractivated)
                            oldnowKeyAct[device] = DateTime.MinValue;
                    }
                    else
                        foreach (MiControls dc in action.trigger)
                        {
                            if (!getBoolMapping(dc, cState, eState, tp))
                            {
                                triggeractivated = false;
                                break;
                            }
                        }

                    bool utriggeractivated = true;
                    if (action.type == "Key" && action.uTrigger.Count > 0)
                    {
                        foreach (MiControls dc in action.uTrigger)
                        {
                            if (!getBoolMapping(dc, cState, eState, tp))
                            {
                                utriggeractivated = false;
                                break;
                            }
                        }
                        if (action.pressRelease) utriggeractivated = !utriggeractivated;
                    }

                    if (triggeractivated && action.type == "Program")
                    {
                        if (!actionDone[device, index])
                        {
                            actionDone[device, index] = true;
                            if (!string.IsNullOrEmpty(action.extra))
                                Process.Start(action.details, action.extra);
                            else
                                Process.Start(action.details);
                        }
                    }
                    else if (triggeractivated && action.type == "Profile")
                    {
                        if (!actionDone[device, index] && string.IsNullOrEmpty(Global.tempprofilename[device]))
                        {
                            actionDone[device, index] = true;
                            untriggeraction[device] = action;
                            untriggerindex[device] = index;
                            foreach (MiControls dc in action.trigger)
                            {
                                InputMethods.performKeyRelease(Global.getCustomKey(0, dc));
                                string[] skeys = Global.getCustomMacro(0, dc).Split('/');
                                ushort[] keys = new ushort[skeys.Length];
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    keys[i] = ushort.Parse(skeys[i]);
                                    InputMethods.performKeyRelease(keys[i]);
                                }
                            }
                            Global.LoadTempProfile(device, action.details, true, ctrl);
                            return;
                        }
                    }
                    else if (triggeractivated && action.type == "Macro")
                    {
                        if (!actionDone[device, index])
                        {
                            DS4KeyType keyType = action.keyType;
                            actionDone[device, index] = true;
                            foreach (MiControls dc in action.trigger)
                                resetToDefaultValue(dc, MappedState);
                            PlayMacro(device, macroControl, String.Join("/", action.macro), MiControls.None, keyType);
                        }
                        else
                            EndMacro(device, macroControl, String.Join("/", action.macro), MiControls.None);
                    }
                    else if (triggeractivated && action.type == "Key")
                    {
                        if (action.uTrigger.Count == 0 || (action.uTrigger.Count > 0 && untriggerindex[device] == -1 && !actionDone[device, index]))
                        {
                            actionDone[device, index] = true;
                            untriggerindex[device] = index;
                            ushort key;
                            ushort.TryParse(action.details, out key);
                            if (action.uTrigger.Count == 0)
                            {
                                SyntheticState.KeyPresses kp;
                                if (!deviceState[device].keyPresses.TryGetValue(key, out kp))
                                    deviceState[device].keyPresses[key] = kp = new SyntheticState.KeyPresses();
                                if (action.keyType.HasFlag(DS4KeyType.ScanCode))
                                    kp.current.scanCodeCount++;
                                else
                                    kp.current.vkCount++;
                                kp.current.repeatCount++;
                            }
                            else if (action.keyType.HasFlag(DS4KeyType.ScanCode))
                                InputMethods.performSCKeyPress(key);
                            else
                                InputMethods.performKeyPress(key);
                        }
                    }
                    else if (action.uTrigger.Count > 0 && utriggeractivated && action.type == "Key")
                    {
                        if (untriggerindex[device] > -1 && !actionDone[device, index])
                        {
                            actionDone[device, index] = true;
                            untriggerindex[device] = -1;
                            ushort key;
                            ushort.TryParse(action.details, out key);
                            if (action.keyType.HasFlag(DS4KeyType.ScanCode))
                                InputMethods.performSCKeyRelease(key);
                            else
                                InputMethods.performKeyRelease(key);
                        }
                    }
                    else if (triggeractivated && action.type == "DisconnectBT")
                    {
                        MiDevice d = ctrl.MiControllers[device];
                        if (!d.Charging)
                        {
                            d.DisconnectBT();
                            foreach (MiControls dc in action.trigger)
                            {
                                InputMethods.performKeyRelease(Global.getCustomKey(0, dc));
                                string[] skeys = Global.getCustomMacro(0, dc).Split('/');
                                ushort[] keys = new ushort[skeys.Length];
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    keys[i] = ushort.Parse(skeys[i]);
                                    InputMethods.performKeyRelease(keys[i]);
                                }
                            }
                            return;
                        }
                    }
                    //else if (triggeractivated && action.type == "BatteryCheck")
                    //{
                    //    string[] dets = action.details.Split(',');
                    //    if (bool.Parse(dets[1]) && !actionDone[device, index])
                    //    {
                    //       Log.LogToTray("Controller " + (device + 1) + ": " +
                    //           ctrl.getDS4Battery(device), true);
                    //    }
                    //    if (bool.Parse(dets[2]))
                    //    {
                    //        MiDevice d = ctrl.MiControllers[device];
                    //        if (!actionDone[device, index])
                    //        {
                    //            lastColor[device] = d.LightBarColor;
                    //            //DS4LightBar.forcelight[device] = true;
                    //        }
                    //        DS4Color empty = new DS4Color(byte.Parse(dets[3]), byte.Parse(dets[4]), byte.Parse(dets[5]));
                    //        DS4Color full = new DS4Color(byte.Parse(dets[6]), byte.Parse(dets[7]), byte.Parse(dets[8]));
                    //        DS4Color trans = Global.getTransitionedColor(empty, full, d.Battery);
                    //        //if (fadetimer[device] < 100)
                    //        //DS4LightBar.forcedColor[device] = Global.getTransitionedColor(lastColor[device], trans, fadetimer[device] += 2);
                    //    }
                    //    actionDone[device, index] = true;
                    //}
                    //else if (!triggeractivated && action.type == "BatteryCheck")
                    //{
                    //    if (actionDone[device, index])
                    //    {
                    //        fadetimer[device] = 0;
                    //        /*if (prevFadetimer[device] == fadetimer[device])
                    //        {
                    //            prevFadetimer[device] = 0;
                    //            fadetimer[device] = 0;
                    //        }
                    //        else
                    //            prevFadetimer[device] = fadetimer[device];*/
                    //        //DS4LightBar.forcelight[device] = false;
                    //        actionDone[device, index] = false;
                    //    }
                    //}
                    else
                        actionDone[device, index] = false;
                }
            }

            if (untriggeraction[device] != null)
            {
                SpecialAction action = untriggeraction[device];
                int index = untriggerindex[device];
                bool utriggeractivated = true;
                foreach (MiControls dc in action.uTrigger)
                {
                    if (!getBoolMapping(dc, cState, eState, tp))
                    {
                        utriggeractivated = false;
                        break;
                    }
                }

                if (utriggeractivated && action.type == "Profile")
                {
                    if ((action.controls == action.ucontrols && !actionDone[device, index]) || //if trigger and end trigger are the same
                    action.controls != action.ucontrols)
                        if (!string.IsNullOrEmpty(Global.tempprofilename[device]))
                        {
                            foreach (MiControls dc in action.uTrigger)
                            {
                                actionDone[device, index] = true;
                                InputMethods.performKeyRelease(Global.getCustomKey(0, dc));
                                string[] skeys = Global.getCustomMacro(0, dc).Split('/');
                                ushort[] keys = new ushort[skeys.Length];
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    keys[i] = ushort.Parse(skeys[i]);
                                    InputMethods.performKeyRelease(keys[i]);
                                }
                            }
                            untriggeraction[device] = null;
                            Global.LoadProfile(device, false, ctrl);
                        }
                }
                else
                    actionDone[device, index] = false;
            }
        }

        private static async void PlayMacro(int device, bool[] macrocontrol, string macro, MiControls control, DS4KeyType keyType)
        {
            if (macro.StartsWith("164/9/9/164") || macro.StartsWith("18/9/9/18"))
            {
                string[] skeys;
                int wait = 1000;
                if (!string.IsNullOrEmpty(macro))
                {
                    skeys = macro.Split('/');
                    ushort delay;
                    if (ushort.TryParse(skeys[skeys.Length - 1], out delay) && delay > 300)
                        wait = delay - 300;
                }
                AltTabSwapping(wait, device);
                if (control != MiControls.None)
                    macrodone[DS4ControltoInt(control)] = true;
            }
            else
            {
                string[] skeys;
                int[] keys;
                if (!string.IsNullOrEmpty(macro))
                {
                    skeys = macro.Split('/');
                    keys = new int[skeys.Length];
                }
                else
                {
                    skeys = new string[0];
                    keys = new int[0];
                }
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = ushort.Parse(skeys[i]);
                bool[] keydown = new bool[286];
                if (control == MiControls.None || !macrodone[DS4ControltoInt(control)])
                {
                    if (control != MiControls.None)
                        macrodone[DS4ControltoInt(control)] = true;
                    foreach (int i in keys)
                    {
                        if (i >= 300) //ints over 300 used to delay
                            await Task.Delay(i - 300);
                        else if (!keydown[i])
                        {
                            if (i == 256) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTDOWN); //anything above 255 is not a keyvalue
                            else if (i == 257) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTDOWN);
                            else if (i == 258) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEDOWN);
                            else if (i == 259) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 1);
                            else if (i == 260) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONDOWN, 2);
                            else if (i == 261) macroControl[0] = true;
                            else if (i == 262) macroControl[1] = true;
                            else if (i == 263) macroControl[2] = true;
                            else if (i == 264) macroControl[3] = true;
                            else if (i == 265) macroControl[4] = true;
                            else if (i == 266) macroControl[5] = true;
                            else if (i == 267) macroControl[6] = true;
                            else if (i == 268) macroControl[7] = true;
                            else if (i == 269) macroControl[8] = true;
                            else if (i == 270) macroControl[9] = true;
                            else if (i == 271) macroControl[10] = true;
                            else if (i == 272) macroControl[11] = true;
                            else if (i == 273) macroControl[12] = true;
                            else if (i == 274) macroControl[13] = true;
                            else if (i == 275) macroControl[14] = true;
                            else if (i == 276) macroControl[15] = true;
                            else if (i == 277) macroControl[16] = true;
                            else if (i == 278) macroControl[17] = true;
                            else if (i == 279) macroControl[18] = true;
                            else if (i == 280) macroControl[19] = true;
                            else if (i == 281) macroControl[20] = true;
                            else if (i == 282) macroControl[21] = true;
                            else if (i == 283) macroControl[22] = true;
                            else if (i == 284) macroControl[23] = true;
                            else if (i == 285) macroControl[24] = true;
                            else if (keyType.HasFlag(DS4KeyType.ScanCode))
                                InputMethods.performSCKeyPress((ushort)i);
                            else
                                InputMethods.performKeyPress((ushort)i);
                            keydown[i] = true;
                        }
                        else
                        {
                            if (i == 256) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTUP); //anything above 255 is not a keyvalue
                            else if (i == 257) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTUP);
                            else if (i == 258) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEUP);
                            else if (i == 259) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 1);
                            else if (i == 260) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 2);
                            else if (i == 261) macroControl[0] = false;
                            else if (i == 262) macroControl[1] = false;
                            else if (i == 263) macroControl[2] = false;
                            else if (i == 264) macroControl[3] = false;
                            else if (i == 265) macroControl[4] = false;
                            else if (i == 266) macroControl[5] = false;
                            else if (i == 267) macroControl[6] = false;
                            else if (i == 268) macroControl[7] = false;
                            else if (i == 269) macroControl[8] = false;
                            else if (i == 270) macroControl[9] = false;
                            else if (i == 271) macroControl[10] = false;
                            else if (i == 272) macroControl[11] = false;
                            else if (i == 273) macroControl[12] = false;
                            else if (i == 274) macroControl[13] = false;
                            else if (i == 275) macroControl[14] = false;
                            else if (i == 276) macroControl[15] = false;
                            else if (i == 277) macroControl[16] = false;
                            else if (i == 278) macroControl[17] = false;
                            else if (i == 279) macroControl[18] = false;
                            else if (i == 280) macroControl[19] = false;
                            else if (i == 281) macroControl[20] = false;
                            else if (i == 282) macroControl[21] = false;
                            else if (i == 283) macroControl[22] = false;
                            else if (i == 284) macroControl[23] = false;
                            else if (i == 285) macroControl[24] = false;
                            else if (keyType.HasFlag(DS4KeyType.ScanCode))
                                InputMethods.performSCKeyRelease((ushort)i);
                            else
                                InputMethods.performKeyRelease((ushort)i);
                            keydown[i] = false;
                        }
                    }
                    for (ushort i = 0; i < keydown.Length; i++)
                    {
                        if (keydown[i])
                            if (i == 256) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_LEFTUP); //anything above 255 is not a keyvalue
                            else if (i == 257) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_RIGHTUP);
                            else if (i == 258) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_MIDDLEUP);
                            else if (i == 259) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 1);
                            else if (i == 260) InputMethods.MouseEvent(InputMethods.MOUSEEVENTF_XBUTTONUP, 2);
                            else if (i == 261) macroControl[0] = false;
                            else if (i == 262) macroControl[1] = false;
                            else if (i == 263) macroControl[2] = false;
                            else if (i == 264) macroControl[3] = false;
                            else if (i == 265) macroControl[4] = false;
                            else if (i == 266) macroControl[5] = false;
                            else if (i == 267) macroControl[6] = false;
                            else if (i == 268) macroControl[7] = false;
                            else if (i == 269) macroControl[8] = false;
                            else if (i == 270) macroControl[9] = false;
                            else if (i == 271) macroControl[10] = false;
                            else if (i == 272) macroControl[11] = false;
                            else if (i == 273) macroControl[12] = false;
                            else if (i == 274) macroControl[13] = false;
                            else if (i == 275) macroControl[14] = false;
                            else if (i == 276) macroControl[15] = false;
                            else if (i == 277) macroControl[16] = false;
                            else if (i == 278) macroControl[17] = false;
                            else if (i == 279) macroControl[18] = false;
                            else if (i == 280) macroControl[19] = false;
                            else if (i == 281) macroControl[20] = false;
                            else if (i == 282) macroControl[21] = false;
                            else if (i == 283) macroControl[22] = false;
                            else if (i == 284) macroControl[23] = false;
                            else if (i == 285) macroControl[24] = false;
                            else if (keyType.HasFlag(DS4KeyType.ScanCode))
                                InputMethods.performSCKeyRelease(i);
                            else
                                InputMethods.performKeyRelease(i);
                    }
                    if (keyType.HasFlag(DS4KeyType.HoldMacro))
                    {
                        await Task.Delay(50);
                        if (control != MiControls.None)
                            macrodone[DS4ControltoInt(control)] = false;
                    }
                }
            }
        }

        private static void EndMacro(int device, bool[] macrocontrol, string macro, MiControls control)
        {
            if ((macro.StartsWith("164/9/9/164") || macro.StartsWith("18/9/9/18")) && !altTabDone)
                AltTabSwappingRelease();
            if (control != MiControls.None)
                macrodone[DS4ControltoInt(control)] = false;
        }
        private static void AltTabSwapping(int wait, int device)
        {
            if (altTabDone)
            {
                altTabDone = false;
                InputMethods.performKeyPress(18);
            }
            else
            {
                altTabNow = DateTime.UtcNow;
                if (altTabNow >= oldAltTabNow + TimeSpan.FromMilliseconds(wait))
                {
                    oldAltTabNow = altTabNow;
                    InputMethods.performKeyPress(9);
                    InputMethods.performKeyRelease(9);
                }
            }
        }

        private static void AltTabSwappingRelease()
        {
            if (altTabNow < DateTime.UtcNow - TimeSpan.FromMilliseconds(10)) //in case multiple controls are mapped to alt+tab
            {
                altTabDone = true;
                InputMethods.performKeyRelease(9);
                InputMethods.performKeyRelease(18);
                altTabNow = DateTime.UtcNow;
                oldAltTabNow = DateTime.UtcNow - TimeSpan.FromDays(1);
            }
        }

        private static void getMouseWheelMapping(int device, MiControls control, MiState cState, MiStateExposed eState, Mouse tp, bool down)
        {
            DateTime now = DateTime.UtcNow;
            if (now >= oldnow + TimeSpan.FromMilliseconds(10) && !pressagain)
            {
                oldnow = now;
                InputMethods.MouseWheel((int)(getByteMapping(device, control, cState, eState, tp) / 51f * (down ? -1 : 1)), 0);
            }
        }

        private static int getMouseMapping(int device, MiControls control, MiState cState, MiStateExposed eState, int mnum)
        {
            int controlnum = DS4ControltoInt(control);
            double SXD = Global.SXDeadzone[device];
            double SZD = Global.SZDeadzone[device];
            int deadzoneL = 3;
            int deadzoneR = 3;
            if (Global.LSDeadzone[device] >= 3)
                deadzoneL = 0;
            if (Global.RSDeadzone[device] >= 3)
                deadzoneR = 0;
            double value = 0;
            int speed = Global.ButtonMouseSensitivity[device] + 15;
            double root = 1.002;
            double divide = 10000d;
            //DateTime now = mousenow[mnum];
            switch (control)
            {
                case MiControls.LXNeg:
                    if (cState.LX - 127.5f < -deadzoneL)
                        value = -(cState.LX - 127.5f) / 2550d * speed;
                    break;
                case MiControls.LXPos:
                    if (cState.LX - 127.5f > deadzoneL)
                        value = (cState.LX - 127.5f) / 2550d * speed;
                    break;
                case MiControls.RXNeg:
                    if (cState.RX - 127.5f < -deadzoneR)
                        value = -(cState.RX - 127.5f) / 2550d * speed;
                    break;
                case MiControls.RXPos:
                    if (cState.RX - 127.5f > deadzoneR)
                        value = (cState.RX - 127.5f) / 2550d * speed;
                    break;
                case MiControls.LYNeg:
                    if (cState.LY - 127.5f < -deadzoneL)
                        value = -(cState.LY - 127.5f) / 2550d * speed;
                    break;
                case MiControls.LYPos:
                    if (cState.LY - 127.5f > deadzoneL)
                        value = (cState.LY - 127.5f) / 2550d * speed;
                    break;
                case MiControls.RYNeg:
                    if (cState.RY - 127.5f < -deadzoneR)
                        value = -(cState.RY - 127.5f) / 2550d * speed;
                    break;
                case MiControls.RYPos:
                    if (cState.RY - 127.5f > deadzoneR)
                        value = (cState.RY - 127.5f) / 2550d * speed;
                    break;
                case MiControls.Back: value = (cState.Back ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.Menu: value = (cState.Menu ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.L1: value = (cState.L1 ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.R1: value = (cState.R1 ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.LS: value = (cState.LS ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.RS: value = (cState.RS ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.DpadUp: value = (cState.DpadUp ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.DpadDown: value = (cState.DpadDown ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.DpadLeft: value = (cState.DpadLeft ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.DpadRight: value = (cState.DpadRight ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.HomeSimulated: value = (cState.HomeSimulated ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.A: value = (cState.A ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.X: value = (cState.X ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.Y: value = (cState.Y ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.B: value = (cState.B ? Math.Pow(root + speed / divide, 100) - 1 : 0); break;
                case MiControls.LT: value = Math.Pow(root + speed / divide, cState.LT / 2d) - 1; break;
                case MiControls.RT: value = Math.Pow(root + speed / divide, cState.RT / 2d) - 1; break;
                //case MiControls.GyroXPos: return (byte)(eState.GyroX > SXD * 7500 ?
                //    Math.Pow(root + speed / divide, eState.GyroX / 62) : 0);
                //case MiControls.GyroXNeg: return (byte)(eState.GyroX < -SXD * 7500 ?
                //    Math.Pow(root + speed / divide, -eState.GyroX / 48) : 0);
                //case MiControls.GyroZPos: return (byte)(eState.GyroZ > SZD * 7500 ?
                //    Math.Pow(root + speed / divide, eState.GyroZ / 62) : 0);
                //case MiControls.GyroZNeg: return (byte)(eState.GyroZ < -SZD * 7500 ?
                //    Math.Pow(root + speed / divide, -eState.GyroZ / 62) : 0);
            }
            bool LXChanged = (Math.Abs(127 - cState.LX) < deadzoneL);
            bool LYChanged = (Math.Abs(127 - cState.LY) < deadzoneL);
            bool RXChanged = (Math.Abs(127 - cState.RX) < deadzoneR);
            bool RYChanged = (Math.Abs(127 - cState.RY) < deadzoneR);
            bool contains = (control.ToString().Contains("LX") ||
                control.ToString().Contains("LY") ||
                control.ToString().Contains("RX") ||
                    control.ToString().Contains("RY"));
            if (Global.MouseAccel[device])
            {
                if (value > 0)
                {
                    mcounter = 34;
                    mouseaccel++;
                }
                if (mouseaccel == prevmouseaccel)
                {
                    mcounter--;
                }
                if (mcounter <= 0)
                {
                    mouseaccel = 0;
                    mcounter = 34;
                }
                value *= 1 + (double)Math.Min(20000, (mouseaccel)) / 10000d;
                prevmouseaccel = mouseaccel;
            }
            int intValue;
            if (mnum > 1)
            {
                if ((value > 0.0 && horizontalRemainder > 0.0) || (value < 0.0 && horizontalRemainder < 0.0))
                    value += horizontalRemainder;
                intValue = (int)value;
                horizontalRemainder = value - intValue;
            }
            else
            {
                if ((value > 0.0 && verticalRemainder > 0.0) || (value < 0.0 && verticalRemainder < 0.0))
                    value += verticalRemainder;
                intValue = (int)value;
                verticalRemainder = value - intValue;
            }
            return intValue;
        }

        public static bool compare(byte b1, byte b2)
        {
            if (Math.Abs(b1 - b2) > 10)
            {
                return false;
            }
            return true;
        }

        public static byte getByteMapping(int device, MiControls control, MiState cState, MiStateExposed eState, Mouse tp)
        {
            double SXD = Global.SXDeadzone[device];
            double SZD = Global.SZDeadzone[device];
            switch (control)
            {
                case MiControls.Back: return (byte)(cState.Back ? 255 : 0);
                case MiControls.Menu: return (byte)(cState.Menu ? 255 : 0);
                case MiControls.L1: return (byte)(cState.L1 ? 255 : 0);
                case MiControls.R1: return (byte)(cState.R1 ? 255 : 0);
                case MiControls.LS: return (byte)(cState.LS ? 255 : 0);
                case MiControls.RS: return (byte)(cState.RS ? 255 : 0);
                case MiControls.DpadUp: return (byte)(cState.DpadUp ? 255 : 0);
                case MiControls.DpadDown: return (byte)(cState.DpadDown ? 255 : 0);
                case MiControls.DpadLeft: return (byte)(cState.DpadLeft ? 255 : 0);
                case MiControls.DpadRight: return (byte)(cState.DpadRight ? 255 : 0);
                case MiControls.HomeSimulated: return (byte)(cState.HomeSimulated ? 255 : 0);
                case MiControls.A: return (byte)(cState.A ? 255 : 0);
                case MiControls.X: return (byte)(cState.X ? 255 : 0);
                case MiControls.Y: return (byte)(cState.Y ? 255 : 0);
                case MiControls.B: return (byte)(cState.B ? 255 : 0);
                //case MiControls.TouchLeft: return (byte)(tp != null && tp.leftDown ? 255 : 0);
                //case MiControls.TouchRight: return (byte)(tp != null && tp.rightDown ? 255 : 0);
                //case MiControls.TouchMulti: return (byte)(tp != null && tp.multiDown ? 255 : 0);
                //case MiControls.TouchUpper: return (byte)(tp != null && tp.upperDown ? 255 : 0);
                case MiControls.LXNeg: return (byte)(cState.LX - 127.5f > 0 ? 0 : -(cState.LX - 127.5f) * 2);
                case MiControls.LYNeg: return (byte)(cState.LY - 127.5f > 0 ? 0 : -(cState.LY - 127.5f) * 2);
                case MiControls.RXNeg: return (byte)(cState.RX - 127.5f > 0 ? 0 : -(cState.RX - 127.5f) * 2);
                case MiControls.RYNeg: return (byte)(cState.RY - 127.5f > 0 ? 0 : -(cState.RY - 127.5f) * 2);
                case MiControls.LXPos: return (byte)(cState.LX - 127.5f < 0 ? 0 : (cState.LX - 127.5f) * 2);
                case MiControls.LYPos: return (byte)(cState.LY - 127.5f < 0 ? 0 : (cState.LY - 127.5f) * 2);
                case MiControls.RXPos: return (byte)(cState.RX - 127.5f < 0 ? 0 : (cState.RX - 127.5f) * 2);
                case MiControls.RYPos: return (byte)(cState.RY - 127.5f < 0 ? 0 : (cState.RY - 127.5f) * 2);
                case MiControls.LT: return cState.LT;
                case MiControls.RT: return cState.RT;
                //case MiControls.GyroXPos: return (byte)(eState.GyroX > SXD * 7500 ? Math.Min(255, eState.GyroX / 31) : 0);
                //case MiControls.GyroXNeg: return (byte)(eState.GyroX < -SXD * 7500 ? Math.Min(255, -eState.GyroX / 31) : 0);
                //case MiControls.GyroZPos: return (byte)(eState.GyroZ > SZD * 7500 ? Math.Min(255, eState.GyroZ / 31) : 0);
                //case MiControls.GyroZNeg: return (byte)(eState.GyroZ < -SZD * 7500 ? Math.Min(255, -eState.GyroZ / 31) : 0);
                //case MiControls.SwipeUp: return (byte)(tp != null ? tp.swipeUpB : 0);
                //case MiControls.SwipeDown: return (byte)(tp != null ? tp.swipeDownB: 0);
                //case MiControls.SwipeLeft: return (byte)(tp != null ?  tp.swipeLeftB: 0);
                //case MiControls.SwipeRight: return (byte)(tp != null ? tp.swipeRightB : 0);
            }
            return 0;
        }

        public static bool getBoolMapping(MiControls control, MiState cState, MiStateExposed eState, Mouse tp)
        {
            switch (control)
            {
                case MiControls.Back: return cState.Back;
                case MiControls.Menu: return cState.Menu;
                case MiControls.L1: return cState.L1;
                case MiControls.R1: return cState.R1;
                case MiControls.LS: return cState.LS;
                case MiControls.RS: return cState.RS;
                case MiControls.DpadUp: return cState.DpadUp;
                case MiControls.DpadDown: return cState.DpadDown;
                case MiControls.DpadLeft: return cState.DpadLeft;
                case MiControls.DpadRight: return cState.DpadRight;
                case MiControls.HomeSimulated: return cState.HomeSimulated;
                case MiControls.A: return cState.A;
                case MiControls.X: return cState.X;
                case MiControls.Y: return cState.Y;
                case MiControls.B: return cState.B;
                //case MiControls.TouchLeft: return (tp != null ? tp.leftDown : false);
                //case MiControls.TouchRight: return (tp != null ? tp.rightDown : false);
                //case MiControls.TouchMulti: return (tp != null ? tp.multiDown : false);
                //case MiControls.TouchUpper: return (tp != null ? tp.upperDown : false);
                case MiControls.LXNeg: return cState.LX < 127 - 55;
                case MiControls.LYNeg: return cState.LY < 127 - 55;
                case MiControls.RXNeg: return cState.RX < 127 - 55;
                case MiControls.RYNeg: return cState.RY < 127 - 55;
                case MiControls.LXPos: return cState.LX > 127 + 55;
                case MiControls.LYPos: return cState.LY > 127 + 55;
                case MiControls.RXPos: return cState.RX > 127 + 55;
                case MiControls.RYPos: return cState.RY > 127 + 55;
                case MiControls.LT: return cState.LT > 100;
                case MiControls.RT: return cState.RT > 100;
            //    case MiControls.GyroXPos: return eState.GyroX > 5000;
            //    case MiControls.GyroXNeg: return eState.GyroX < -5000;
            //    case MiControls.GyroZPos: return eState.GyroZ > 5000;
            //    case MiControls.GyroZNeg: return eState.GyroZ < -5000;
            //    case MiControls.SwipeUp: return (tp != null && tp.swipeUp);
            //    case MiControls.SwipeDown: return (tp != null && tp.swipeDown);
            //    case MiControls.SwipeLeft: return (tp != null && tp.swipeLeft);
            //    case MiControls.SwipeRight: return (tp != null && tp.swipeRight);
            }
            return false;
        }

        public static byte getXYAxisMapping(int device, MiControls control, MiState cState, MiStateExposed eState, Mouse tp, bool alt = false)
        {
            byte trueVal = 0;
            byte falseVal = 127;
            double SXD = Global.SXDeadzone[device];
            double SZD = Global.SZDeadzone[device];
            if (alt)
                trueVal = 255;
            switch (control)
            {
                case MiControls.Back: return (byte)(cState.Back ? trueVal : falseVal);
                case MiControls.Menu: return (byte)(cState.Menu ? trueVal : falseVal);
                case MiControls.L1: return (byte)(cState.L1 ? trueVal : falseVal);
                case MiControls.R1: return (byte)(cState.R1 ? trueVal : falseVal);
                case MiControls.LS: return (byte)(cState.LS ? trueVal : falseVal);
                case MiControls.RS: return (byte)(cState.RS ? trueVal : falseVal);
                case MiControls.DpadUp: return (byte)(cState.DpadUp ? trueVal : falseVal);
                case MiControls.DpadDown: return (byte)(cState.DpadDown ? trueVal : falseVal);
                case MiControls.DpadLeft: return (byte)(cState.DpadLeft ? trueVal : falseVal);
                case MiControls.DpadRight: return (byte)(cState.DpadRight ? trueVal : falseVal);
                case MiControls.HomeSimulated: return (byte)(cState.HomeSimulated ? trueVal : falseVal);
                case MiControls.A: return (byte)(cState.A ? trueVal : falseVal);
                case MiControls.X: return (byte)(cState.X ? trueVal : falseVal);
                case MiControls.Y: return (byte)(cState.Y ? trueVal : falseVal);
                case MiControls.B: return (byte)(cState.B ? trueVal : falseVal);
                //case MiControls.TouchLeft: return (byte)(tp != null && tp.leftDown ? trueVal : falseVal);
                //case MiControls.TouchRight: return (byte)(tp != null && tp.rightDown ? trueVal : falseVal);
                //case MiControls.TouchMulti: return (byte)(tp != null && tp.multiDown ? trueVal : falseVal);
                //case MiControls.TouchUpper: return (byte)(tp != null && tp.upperDown ? trueVal : falseVal);
                case MiControls.LT: if (alt) return (byte)(127.5f + cState.LT / 2f); else return (byte)(127.5f - cState.LT / 2f);
                case MiControls.RT: if (alt) return (byte)(127.5f + cState.RT / 2f); else return (byte)(127.5f - cState.RT / 2f);
                //case MiControls.SwipeUp: if (alt) return (byte)(tp != null ? 127.5f + tp.swipeUpB / 2f : 0); else return (byte)(tp != null ? 127.5f - tp.swipeUpB / 2f : 0);
                //case MiControls.SwipeDown: if (alt) return (byte)(tp != null ? 127.5f + tp.swipeDownB / 2f : 0); else return (byte)(tp != null ? 127.5f - tp.swipeDownB / 2f : 0);
                //case MiControls.SwipeLeft: if (alt) return (byte)(tp != null ? 127.5f + tp.swipeLeftB / 2f : 0); else return (byte)(tp != null ? 127.5f - tp.swipeLeftB / 2f : 0);
                //case MiControls.SwipeRight: if (alt) return (byte)(tp != null ? 127.5f + tp.swipeRightB / 2f : 0); else return (byte)(tp != null ? 127.5f - tp.swipeRightB / 2f : 0);
                //case MiControls.GyroXPos: if (eState.GyroX > SXD * 7500)
                //        if (alt) return (byte)Math.Min(255, 127 + eState.GyroX / 62); else return (byte)Math.Max(0, 127 - eState.GyroX / 62);
                //    else return falseVal;
                //case MiControls.GyroXNeg: if (eState.GyroX < -SXD * 7500)
                //        if (alt) return (byte)Math.Min(255, 127 + -eState.GyroX / 62); else return (byte)Math.Max(0, 127 - -eState.GyroX / 62);
                //    else return falseVal;
                //case MiControls.GyroZPos: if (eState.GyroZ > SZD * 7500)
                //        if (alt) return (byte)Math.Min(255, 127 + eState.GyroZ / 62); else return (byte)Math.Max(0, 127 - eState.GyroZ / 62);
                //    else return falseVal;
                //case MiControls.GyroZNeg: if (eState.GyroZ < -SZD * 7500)
                //        if (alt) return (byte)Math.Min(255, 127 + -eState.GyroZ / 62); else return (byte)Math.Max(0, 127 - -eState.GyroZ / 62);
                //    else return falseVal;
            }            
            if (!alt)
            {
                switch (control)
                {
                    case MiControls.LXNeg: return cState.LX;
                    case MiControls.LYNeg: return cState.LY;
                    case MiControls.RXNeg: return cState.RX;
                    case MiControls.RYNeg: return cState.RY;
                    case MiControls.LXPos: return (byte)(255 - cState.LX);
                    case MiControls.LYPos: return (byte)(255 - cState.LY);
                    case MiControls.RXPos: return (byte)(255 - cState.RX);
                    case MiControls.RYPos: return (byte)(255 - cState.RY);
                }
            }
            else
            {
                switch (control)
                {
                    case MiControls.LXNeg: return (byte)(255 - cState.LX);
                    case MiControls.LYNeg: return (byte)(255 - cState.LY);
                    case MiControls.RXNeg: return (byte)(255 - cState.RX);
                    case MiControls.RYNeg: return (byte)(255 - cState.RY);
                    case MiControls.LXPos: return cState.LX;
                    case MiControls.LYPos: return cState.LY;
                    case MiControls.RXPos: return cState.RX;
                    case MiControls.RYPos: return cState.RY;
                }
            }
            return 0;
        }

        //Returns false for any bool, 
        //if control is one of the xy axis returns 127
        //if its a trigger returns 0
        public static void resetToDefaultValue(MiControls control, MiState cState)
        {
            switch (control)
            {
                case MiControls.Menu: cState.Menu = false; break;
                case MiControls.Back: cState.Back = false; break;
                case MiControls.L1: cState.L1 = false; break;
                case MiControls.R1: cState.R1 = false; break;
                case MiControls.LS: cState.LS = false; break;
                case MiControls.RS: cState.RS = false; break;
                case MiControls.DpadUp: cState.DpadUp = false; break;
                case MiControls.DpadDown: cState.DpadDown = false; break;
                case MiControls.DpadLeft: cState.DpadLeft = false; break;
                case MiControls.DpadRight: cState.DpadRight = false; break;
                case MiControls.HomeSimulated: cState.HomeSimulated = false; break;
                case MiControls.A: cState.A = false; break;
                case MiControls.X: cState.X = false; break;
                case MiControls.Y: cState.Y = false; break;
                case MiControls.B: cState.B = false; break;
                case MiControls.LXNeg: cState.LX = 127; break;
                case MiControls.LYNeg: cState.LY = 127; break;
                case MiControls.RXNeg: cState.RX = 127; break;
                case MiControls.RYNeg: cState.RY = 127; break;
                case MiControls.LXPos: cState.LX = 127; break;
                case MiControls.LYPos: cState.LY = 127; break;
                case MiControls.RXPos: cState.RX = 127; break;
                case MiControls.RYPos: cState.RY = 127; break;
                case MiControls.LT: cState.LT = 0; break;
                case MiControls.RT: cState.RT = 0; break;
            }
        }
    }
}
