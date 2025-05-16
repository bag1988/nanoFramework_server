using System;
using System.Collections;
using System.Diagnostics;
using esp32_s3.Interfaces;
using esp32_s3.Models;

namespace esp32_s3.Services
{
    public class DevicesManager : IDevicesManager
    {
        Hashtable _deviceHash = new();
        private static object _lock = new object();

        public void InitDevices(DeviceData[] devices)
        {
            lock (_lock)
            {
                foreach (var dev in devices)
                {
                    if (!_deviceHash.Contains(dev.MacAddress))
                    {
                        dev.IsOnline = false;
                        dev.HeatingActive = false;
                        dev.CurrentTemperature = dev.TargetTemperature;
                        dev.HeatingStartTime = DateTime.UtcNow;
                        _deviceHash.Add(dev.MacAddress, dev);
                    }
                }
            }
        }

        public bool AddDevice(DeviceData device)
        {
            lock (_lock)
            {
                if (!_deviceHash.Contains(device.MacAddress))
                {
                    _deviceHash.Add(device.MacAddress, device);
                    return true;
                }
            }
            return false;
        }

        public void UpdateOrAddDevice(DeviceData device)
        {
            lock (_lock)
            {
                if (_deviceHash.Contains(device.MacAddress))
                {
                    var item = (DeviceData)_deviceHash[device.MacAddress];
                    item.updateFromUi(device.Name, device.TargetTemperature, device.Enabled, device.GpioPins);
                }
                else
                {
                    _deviceHash.Add(device.MacAddress, device);
                }
            }
        }

        public void UpdateSensorData(string macAddress, string name, float temp, float hum, byte bat, ushort batteryVoltage)
        {
            lock (_lock)
            {
                if (_deviceHash.Contains(macAddress))
                {
                    var item = (DeviceData)_deviceHash[macAddress];
                    item.updateSensorData(temp, hum, bat, batteryVoltage);
                }
                else
                {
                    var dev = new DeviceData(name, macAddress);
                    dev.updateSensorData(temp, hum, bat, batteryVoltage);
                    _deviceHash.Add(macAddress, dev);
                }
            }
        }

        public void ResetHeatingStat()
        {
            lock (_lock)
            {
                foreach (var dev in _deviceHash.Values)
                {
                    var d = (DeviceData)dev;
                    d.TotalHeatingTime = new();
                    if (d.HeatingActive)
                    {
                        d.HeatingStartTime = DateTime.UtcNow;
                    }
                }
            }
        }

        public bool CheckOnlineDevice()
        {
            lock (_lock)
            {
                bool statusChanged = false;
                foreach (var dev in _deviceHash.Values)
                {
                    var device = (DeviceData)dev;
                    if (!device.IsDataValid())
                    {
                        device.IsOnline = false;
                        statusChanged = true;
                        Debug.WriteLine($"Устройство {device.Name} перешло в оффлайн (нет данных более 5 минут)");
                    }
                }
                return statusChanged;
            }
        }

        public DeviceData[] GetDevices
        {
            get
            {
                return (DeviceData[])_deviceHash.Values;
            }
        }
    }
}
