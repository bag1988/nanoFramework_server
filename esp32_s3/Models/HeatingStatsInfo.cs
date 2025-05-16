using System;

namespace esp32_s3.Models
{
    public class HeatingStatsInfo
    {
        public HeatingStatsInfo(string name, string macAddress, double currentTemperature, double targetTemperature, bool heatingActive, TimeSpan totalHeatingTime, string totalHeatingTimeFormatted)
        {
            Name = name;
            MacAddress = macAddress;
            CurrentTemperature = currentTemperature;
            TargetTemperature = targetTemperature;
            HeatingActive = heatingActive;
            TotalHeatingTime = totalHeatingTime;
            TotalHeatingTimeFormatted = totalHeatingTimeFormatted;
        }

        public string Name { get; set; }
        public string MacAddress { get; set; }
        public double CurrentTemperature { get; set; }
        public double TargetTemperature { get; set; }
        public bool HeatingActive { get; set; }
        public TimeSpan TotalHeatingTime { get; set; }
        public string TotalHeatingTimeFormatted { get; set; }
    }
}
