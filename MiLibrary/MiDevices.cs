using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiWindows
{
    public class MiDevices
    {
        private static Dictionary<string, MiDevice> Devices = new Dictionary<string, MiDevice>();
        private static HashSet<String> DevicePaths = new HashSet<String>();
        public static bool isExclusiveMode = false;

        //enumerates ds4 controllers in the system
        public static void findControllers()
        {
            lock (Devices)
            {
                IEnumerable<HidDevice> hDevices = HidDevices.Enumerate(0x2717, 0x3144);
                // Sort Bluetooth first in case USB is also connected on the same controller.
                //!hDevices = hDevices.OrderBy<HidDevice, ConnectionType>((HidDevice d) => { return DS4Device.HidConnectionType(d); });

                foreach (HidDevice hDevice in hDevices)
                {
                    if (DevicePaths.Contains(hDevice.DevicePath))
                        continue; // BT/USB endpoint already open once
                    if (!hDevice.IsOpen)
                    {
                        hDevice.OpenDevice(isExclusiveMode);
                        // TODO in exclusive mode, try to hold both open when both are connected
                        if (isExclusiveMode && !hDevice.IsOpen)
                            hDevice.OpenDevice(false);
                    }
                    if (hDevice.IsOpen)
                    {
                        if (Devices.ContainsKey(hDevice.readSerial()))
                            continue; // happens when the BT endpoint already is open and the USB is plugged into the same host
                        else
                        {
                            MiDevice miDevice = new MiDevice(hDevice);
                            miDevice.Removal += On_Removal;
                            Devices.Add(miDevice.MacAddress, miDevice);
                            DevicePaths.Add(hDevice.DevicePath);
                            miDevice.StartUpdate();
                        }
                    }
                }
                
            }
        }

        //allows to get DS4Device by specifying unique MAC address
        //format for MAC address is XX:XX:XX:XX:XX:XX
        //!public static MiDevice getDS4Controller(string mac)
        //{
        //    lock (Devices)
        //    {
        //        MiDevice device = null;
        //        try
        //        {
        //            Devices.TryGetValue(mac, out device);
        //        }
        //        catch (ArgumentNullException) { }
        //        return device;
        //    }
        //}
        
        //returns DS4 controllers that were found and are running
        public static IEnumerable<MiDevice> getMiControllers()
        {
            lock (Devices)
            {
                MiDevice[] controllers = new MiDevice[Devices.Count];
                Devices.Values.CopyTo(controllers, 0);
                return controllers;
            }
        }

        public static void stopControllers()
        {
            lock (Devices)
            {
                IEnumerable<MiDevice> devices = getMiControllers();
                foreach (MiDevice device in devices)
                {
                    device.StopUpdate();
                    device.HidDevice.CloseDevice();
                }
                Devices.Clear();
                DevicePaths.Clear();
            }
        }

        //called when devices is diconnected, timed out or has input reading failure
        public static void On_Removal(object sender, EventArgs e)
        {
            lock (Devices)
            {
                MiDevice device = (MiDevice)sender;
                device.HidDevice.CloseDevice();
                Devices.Remove(device.MacAddress);
                DevicePaths.Remove(device.HidDevice.DevicePath);
            }
        }
    }
}
