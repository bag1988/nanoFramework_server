using System;
using System.Collections;
using esp32_s3.Interfaces;
using esp32_s3.Models;

namespace esp32_s3.Services
{
    public class BoardManager : IBoardManager
    {
        Hashtable _gpioInfo = new();
        private static object _lock = new object();

        private int _BoardTemperature = 0;

        WifiCredentials _wifiCredentials = new();

        DateTime StartTime = DateTime.UtcNow;

        long _ServerWorkTime = 0;

        GpioInfo[] availableGpio = new GpioInfo[10] {
            new(15, "GPIO 15"),
            new (16, "GPIO 16"),
            new (17, "GPIO 17"),
            new (18, "GPIO 18"),
            new (38, "GPIO 38"),
            new (39, "GPIO 39"),
            new (40, "GPIO 40"),
            new (42, "GPIO 41"),
            new (45, "GPIO 45"),
            new (47, "GPIO 47")
        };

        public int BoardTemperature
        {
            get
            {
                return _BoardTemperature;
            }
            set
            {
                _BoardTemperature = value;
            }
        }

        public void InitGpios(GpioInfo[] gpios)
        {
            lock (_lock)
            {
                if (gpios.Length == 0)
                {
                    gpios = availableGpio;
                }
                foreach (var gpio in gpios)
                {
                    if (!_gpioInfo.Contains(gpio.Pin))
                    {
                        _gpioInfo.Add(gpio.Pin, gpio);
                    }
                }
            }
        }

        public bool UpdateGpio(GpioInfo gpio)
        {
            lock (_lock)
            {
                if (_gpioInfo.Contains(gpio.Pin))
                {
                    _gpioInfo[gpio.Pin] = gpio;
                    return true;
                }
            }
            return false;
        }

        public TimeSpan GetWorkTime
        {
            get
            {
                return TimeSpan.FromSeconds(_ServerWorkTime);
            }
        }

        public string GetWorkTimeString
        {
            get
            {
                var workTime = GetWorkTime;
                return workTime.Days + " дней " + workTime.Hours + " часов " + workTime.Minutes + " минут " + workTime.Seconds + " секунд";
            }
        }

        public void UpdateGpios(GpioInfo[] gpios)
        {
            lock (_lock)
            {
                foreach (var gpio in gpios)
                {
                    if (_gpioInfo.Contains(gpio.Pin))
                    {
                        _gpioInfo[gpio.Pin] = gpio;
                    }
                }
            }
        }

        public void SetWifiCredentials(WifiCredentials wifiCredentials)
        {
            _wifiCredentials = wifiCredentials;
        }

        public void SetWifiCredentialsSSID(string ssid)
        {
            _wifiCredentials.SSID = ssid;
        }

        public void SetWifiCredentialsPassword(string password)
        {
            _wifiCredentials.Password = password;
        }

        public WifiCredentials GetWifiCredentials => _wifiCredentials;

        public GpioInfo[] GetGpio
        {
            get
            {
                return (GpioInfo[])_gpioInfo.Values;
            }
        }

        public void SetServerWorkTime(long workSecond)
        {
            _ServerWorkTime = workSecond;
        }

        public void UpdateServerWorkTime()
        {
            var currentDate = DateTime.UtcNow;
            _ServerWorkTime += (long)(currentDate - StartTime).TotalSeconds;
            StartTime = currentDate;
        }

        public long GetServerWorkTime => _ServerWorkTime;

    }
}
