using esp32_s3.Models;
using System;

namespace esp32_s3.Interfaces
{
    public interface IBoardManager
    {
        GpioInfo[] GetGpio { get; }        
        string GetWorkTimeString { get; }
        int BoardTemperature { get; set; }
        void InitGpios(GpioInfo[] gpios);
        bool UpdateGpio(GpioInfo gpio);
        void UpdateGpios(GpioInfo[] gpios);
        void SetWifiCredentials(WifiCredentials wifiCredentials);
        void SetWifiCredentialsSSID(string ssid);
        void SetWifiCredentialsPassword(string password);
        WifiCredentials GetWifiCredentials { get; }
        void UpdateServerWorkTime();
        void SetServerWorkTime(long workSecond);

        long GetServerWorkTime { get; }
    }
}