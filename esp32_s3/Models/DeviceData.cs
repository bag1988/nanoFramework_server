using System;

namespace esp32_s3.Models
{
    // Класс для хранения данных устройства
    public class DeviceData
    {
        public string Name { get; set; }// Имя устройства
        public string MacAddress { get; set; }// MAC-адрес датчика
        public float TargetTemperature { get; set; }// Текущая температура
        public float CurrentTemperature { get; set; }//Целевая температура        
        public float Humidity { get; set; }// Влажность
        public byte Battery { get; set; }// Уровень заряда батареи
        public bool Enabled { get; set; }// Включено ли устройство
        public bool IsOnline { get; set; }// Находится ли устройство в сети
        public DateTime LastUpdate { get; set; }// Время последнего обновления данных
        public byte[] GpioPins { get; set; }// Пины GPIO для управления    
        public bool HeatingActive { get; set; }// Поле для отслеживания текущего состояния обогрева
        public DateTime HeatingStartTime { get; set; } // Время последнего включения обогрева
        public TimeSpan TotalHeatingTime { get; set; }// Общее время работы обогрева
        public ushort BatteryVoltage { get; set; }// Уровень заряда батареи

        public DeviceData()
        {
            Name = "";
            MacAddress = "";
            CurrentTemperature = 25;
            TargetTemperature = 25;
            GpioPins = new byte[0];
            HeatingStartTime = new DateTime();
            TotalHeatingTime = new TimeSpan();
        }

        public DeviceData(string name, string macAddress)
        {
            Name = name;
            MacAddress = macAddress;
            CurrentTemperature = 25;
            TargetTemperature = 25;
            IsOnline = true;
            LastUpdate = DateTime.UtcNow;
            GpioPins = new byte[0];
            HeatingStartTime = new DateTime();
            TotalHeatingTime = new TimeSpan();
        }
        // Метод для обновления данных датчика
        public void updateSensorData(float temp, float hum, byte bat, ushort batteryVoltage)
        {
            CurrentTemperature = temp;
            Humidity = hum;
            Battery = bat;
            LastUpdate = DateTime.UtcNow;
            BatteryVoltage = batteryVoltage;
            IsOnline = true;
        }

        public void updateFromUi(string name, float targetTemp, bool enabled, byte[] gpioPins)
        {
            TargetTemperature = targetTemp;
            Name = name;
            Enabled = enabled;
            GpioPins = gpioPins;
        }

        public void RemoveOrAddGpioPins(byte gpioPins)
        {
            var arr = new byte[GpioPins.Length + 1];
            int index = 0;
            bool isFind = false;
            for (int i = 0; i < GpioPins.Length; i++)
            {
                if (GpioPins[i] == gpioPins)
                {
                    isFind = true;
                    continue;
                }
                arr[index++] = GpioPins[i];
            }

            int newSize = GpioPins.Length - 1;

            if (!isFind)
            {
                newSize = GpioPins.Length + 1;
                arr[index] = gpioPins;
            }

            GpioPins = new byte[newSize];

            for (int i = 0; i < newSize; i++)
            {
                GpioPins[i] = arr[i];
            }
        }

        // Метод для проверки актуальности данных
      public  bool IsDataValid(long timeout = GlobalConstant.XIAOMI_OFFLINE_TIMEOUT)
        {
            return IsOnline && ((DateTime.UtcNow - LastUpdate).TotalMilliseconds < timeout);
        }
    }
}
