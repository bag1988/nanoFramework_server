using System;
using esp32_s3.Interfaces;
using nanoFramework.Json;
using esp32_s3.Models;
using System.Diagnostics;
using System.IO;

namespace esp32_s3.Controller
{
    public class FilesManager : IFilesManager
    {
        readonly IDevicesManager _devicesManager;
        readonly IBoardManager _boardManager;
        const string DEVICE_FILE_NAME = "I:\\devices.json";
        const string GPIO_FILE_NAME = "I:\\aviable_gpio.json";
        const string WIFI_FILE_NAME = "I:\\wifi_setting.json";
        const string SERVER_WORK_TIME_FILE_NAME = "I:\\server_setting.json";
        public FilesManager(IDevicesManager devicesManager, IBoardManager boardManager)
        {
            _devicesManager = devicesManager;
            _boardManager = boardManager;
        }

        public void LoadAll()
        {
            LoadServerSetting();
            LoadClients();
            LoadGpio();
            LoadWifiCredentials();
        }

        public void LoadClients()
        {
            try
            {
                Debug.WriteLine($"Загружаем сохраненные данные устройств");
                if (File.Exists(DEVICE_FILE_NAME))
                {
                    var data = (DeviceData[])JsonConvert.DeserializeObject(File.ReadAllText(DEVICE_FILE_NAME), typeof(DeviceData[]));
                    _devicesManager.InitDevices(data);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных устройств: {ex.Message}");
            }
        }

        public void SaveClient()
        {
            try
            {
                Debug.WriteLine($"Сохраняем данные устройств");
                File.WriteAllText(DEVICE_FILE_NAME, JsonConvert.SerializeObject(_devicesManager.GetDevices));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения данных устройств: {ex.Message}");
            }
        }

        public void LoadGpio()
        {
            try
            {
                Debug.WriteLine($"Загружаем сохраненные gpio");
                if (File.Exists(GPIO_FILE_NAME))
                {
                    var data = (GpioInfo[])JsonConvert.DeserializeObject(File.ReadAllText(GPIO_FILE_NAME), typeof(GpioInfo[]));
                    _boardManager.InitGpios(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки gpio: {ex.Message}");
            }
        }

        public void SaveGpio()
        {
            try
            {
                Debug.WriteLine($"Сохраняем gpio");
                File.WriteAllText(GPIO_FILE_NAME, JsonConvert.SerializeObject(_boardManager.GetGpio));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения gpio: {ex.Message}");
            }
        }

        public void LoadWifiCredentials()
        {
            try
            {
                Debug.WriteLine($"Загружаем сохраненные Wifi");
                if (File.Exists(WIFI_FILE_NAME))
                {
                    var data = (WifiCredentials)JsonConvert.DeserializeObject(File.ReadAllText(WIFI_FILE_NAME), typeof(WifiCredentials));
                    _boardManager.SetWifiCredentials(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки Wifi: {ex.Message}");
            }
        }

        public void SaveWifiCredentials()
        {
            try
            {
                Debug.WriteLine($"Сохраняем Wifi");           
                File.WriteAllText(WIFI_FILE_NAME, JsonConvert.SerializeObject(_boardManager.GetWifiCredentials));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения Wifi: {ex.Message}");
            }
        }

        public void LoadServerSetting()
        {
            try
            {
                Debug.WriteLine($"Загружаем сохраненные данные сервера");
                if (File.Exists(SERVER_WORK_TIME_FILE_NAME))
                {
                    var data = (long)JsonConvert.DeserializeObject(File.ReadAllText(SERVER_WORK_TIME_FILE_NAME), typeof(long));
                    _boardManager.SetServerWorkTime(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных сервера: {ex.Message}");
            }
        }

        public void SaveServerSetting()
        {
            try
            {
                Debug.WriteLine($"Сохраняем данные сервера");
                File.WriteAllText(SERVER_WORK_TIME_FILE_NAME, JsonConvert.SerializeObject(_boardManager.GetServerWorkTime));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения данных сервера: {ex.Message}");
            }
        }

    }
}
