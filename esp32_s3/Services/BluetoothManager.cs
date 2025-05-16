using nanoFramework.Device.Bluetooth.Advertisement;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using nanoFramework.Device.Bluetooth;
using System;
using System.Diagnostics;
using System.Threading;
using esp32_s3.Models;
using esp32_s3.Interfaces;

namespace esp32_s3.Services
{
    /// <summary>
    /// Класс для сканирования и обработки данных с датчиков Xiaomi
    /// </summary>
    public class BluetoothManager : IBluetoothManager
    {
        // Глобальные переменные
        private BluetoothLEAdvertisementWatcher _bleWatcher;
        private bool _scanningActive = false;
        private BluetoothLEServer _bleServer;
        private Timer _scanTimer;
        private static readonly object _lockObject = new object();
        readonly IBoardManager _boardManager;
        readonly IFilesManager _filesManager;
        public BluetoothManager(IBoardManager boardManager, IFilesManager filesManager)
        {
            _boardManager = boardManager;
            _filesManager = filesManager;
        }

        /// <summary>
        /// Инициализация сканера BLE
        /// </summary>
        public void SetupXiaomiScanner()
        {
            try
            {
                // Инициализация BLE               
                Debug.WriteLine("Инициализация Bluetooth...");

                // Создание сканера BLE
                _bleWatcher = new BluetoothLEAdvertisementWatcher();
                _bleWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                _bleWatcher.Received += BleWatcher_Received;

                // Настройка параметров сканирования
                _bleWatcher.SignalStrengthFilter.InRangeThresholdInDBm = -90; // Минимальный уровень сигнала
                _bleWatcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -100; // Уровень сигнала для "вне зоны"
                _bleWatcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromSeconds(10); // Таймаут для "вне зоны"

                // Создание BLE сервера
                SetupBleServer();

                StartXiaomiScan();

                Debug.WriteLine("Сканер датчиков Xiaomi инициализирован");
                Debug.WriteLine("Запускаем сервисы редактирования SSID и пароля");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при инициализации BLE: {ex.Message}");
            }
        }


        /// <summary>
        /// Запуск сканирования BLE устройств
        /// </summary>
        public void StartXiaomiScan(int duration = GlobalConstant.XIAOMI_SCAN_DURATION)
        {
            try
            {
                Debug.WriteLine("Начало сканирования датчиков Xiaomi...");
                if (_scanningActive)
                {
                    Debug.WriteLine("Сканирование датчиков Xiaomi уже запущено, выход");
                    return;
                }

                lock (_lockObject)
                {
                    _scanningActive = true;

                    try
                    {
                        // Запуск сканирования
                        _bleWatcher.Start();

                        // Установка таймера для остановки сканирования
                        _scanTimer = new Timer(StopScanCallback, null, duration, Timeout.Infinite);

                        Debug.WriteLine($"Сканирование запущено на {duration} мс");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка при запуске сканирования: {ex.Message}");
                        _scanningActive = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ошибка запуска сканирования: {ex.Message}");
            }
        }

        /// <summary>
        /// Callback для остановки сканирования по таймеру
        /// </summary>
        private void StopScanCallback(object state)
        {
            lock (_lockObject)
            {
                if (_scanningActive)
                {
                    try
                    {
                        _bleWatcher.Stop();
                        Debug.WriteLine("Сканирование остановлено");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка при остановке сканирования: {ex.Message}");
                    }
                    finally
                    {
                        _scanningActive = false;
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик события обнаружения BLE устройства
        /// </summary>
        private void BleWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                var macAddressBytes = BitConverter.GetBytes(args.BluetoothAddress);

                string deviceAddress = string.Empty;

                for (var i = 0; i < macAddressBytes.Length; i++)
                {
                    deviceAddress += macAddressBytes[i].ToString("X2");
                    if ((i + 1) < macAddressBytes.Length)
                    {
                        deviceAddress += ":";
                    }
                }

                Debug.WriteLine("//////////////////////////////////");
                Debug.WriteLine($"Обнаружено устройство: {deviceAddress}");

                var devName = args.Advertisement.LocalName;

                Debug.WriteLine($"Имя: {devName}");


                var uuidsAtc1 = new Guid(GlobalConstant.UUID_ATC_1);
                var uuidsAtc2 = new Guid(GlobalConstant.UUID_ATC_2);

                if (args.Advertisement.ServiceUuids.Length > 0 && (Array.IndexOf(args.Advertisement.ServiceUuids, uuidsAtc1) >= 0 || Array.IndexOf(args.Advertisement.ServiceUuids, uuidsAtc2) >= 0))
                {
                    Debug.WriteLine($"Проверка на ATC прошивку пройдена успешно");


                    // Обработка данных производителя
                    if (args.Advertisement.ManufacturerData.Count > 0)
                    {
                        Debug.WriteLine("Данные производителя: ");
                        foreach (BluetoothLEManufacturerData manufacturerData in args.Advertisement.ManufacturerData)
                        {
                            Debug.WriteLine($"-- Company:{manufacturerData.CompanyId}, длина:{manufacturerData.Data.Length}");
                            Debug.WriteLine("Данные: ");
                            DataReader dr = DataReader.FromBuffer(manufacturerData.Data);
                            byte[] bytes = new byte[manufacturerData.Data.Length];
                            dr.ReadBytes(bytes);
                            foreach (byte b in bytes)
                            {
                               Debug.Write(b.ToString("X"));
                            }
                            Debug.WriteLine();
                            // Обработка рекламного пакета Xiaomi
                            if (bytes.Length >= 19)
                            {
                                ProcessXiaomiAdvertisement(bytes, deviceAddress, devName);
                            }
                        }
                    }

                    // Обработка сервисных данных
                    if (args.Advertisement.DataSections.Count > 0)
                    {
                        Debug.WriteLine("Секционные данные: ");
                        foreach (BluetoothLEAdvertisementDataSection serviceData in args.Advertisement.DataSections)
                        {
                            Debug.WriteLine($"-- Тип данных: {serviceData.DataType}, длина:{serviceData.Data.Length}");
                            Debug.WriteLine("Данные: ");
                            DataReader dr = DataReader.FromBuffer(serviceData.Data);
                            byte[] bytes = new byte[serviceData.Data.Length];
                            dr.ReadBytes(bytes);
                            foreach (byte b in bytes)
                            {
                                Debug.Write(b.ToString("X"));
                            }
                            Debug.WriteLine();
                            // Обработка рекламного пакета Xiaomi
                            if (bytes.Length >= 19)
                            {
                                ProcessXiaomiAdvertisement(bytes, deviceAddress, devName);
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Неизвестный UUID!!!!!!!!!!!!!");
                }


                Debug.WriteLine("//////////////////////////////////");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке BLE устройства: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка рекламного пакета Xiaomi
        /// </summary>
        private void ProcessXiaomiAdvertisement(byte[] data, string deviceAddress, string devName)
        {
            try
            {
                bool isXiaomiDevice = false;
                bool isCustomFirmware = false;

                // Проверка MAC-адреса в данных (для ATC)
                var packetAddr = string.Empty;
                for (int j = 0; j < 6; j++)
                {
                    packetAddr += data[9 - j].ToString("X2");
                    if ((j + 1) < 6)
                    {
                        packetAddr += ":";
                    }
                }

                bool equalsMac = packetAddr.ToLower() == deviceAddress.ToLower();
                Debug.WriteLine($"Сравниваем MAC {deviceAddress} в пакете с MAC устройства: {packetAddr}, равны: {(equalsMac ? "да" : "нет")}");
                if (equalsMac)
                {
                    isXiaomiDevice = true;
                    isCustomFirmware = true;
                    Debug.WriteLine("Обнаружено устройство с кастомной прошивкой ATC");
                }

                // Если это устройство Xiaomi, обрабатываем его
                if (isXiaomiDevice)
                {
                    // Парсим данные из рекламного пакета
                    float temperature = 0.0f;
                    float humidity = 0.0f;
                    byte battery = 0;
                    ushort batteryV = 0;
                    bool dataFound = false;

                    // Обработка данных для устройств с кастомной прошивкой ATC
                    // Обработка данных для устройств с кастомной прошивкой ATC
                    if (isCustomFirmware)
                    {
                        Debug.WriteLine($"Размер данных {data.Length}");
                        short temperatureRaw = 0;
                        short humidityRaw = 0;

                        // Формат ATC: байты 6-7 - температура, байты 8-9 - влажность, байт 12 - батарея
                        temperatureRaw = (short)((data[11] << 8) | data[10]);
                        temperature = temperatureRaw / 100.0f;

                        humidityRaw = (short)((data[13] << 8) | data[12]);
                        humidity = humidityRaw / 100.0f;

                        batteryV = (ushort)((data[15] << 8) | data[14]);

                        battery = data[16];

                        dataFound = true;
                        Debug.WriteLine($"Парсинг данных ATC: Температура: {temperature:F1}°C, Влажность: {humidity:F1}%, Батарея: {battery}%, Напряжение: {batteryV}");
                    }

                    // Если данные найдены, обновляем информацию об устройстве                    
                    if (dataFound)
                    {
                        var dev = new DeviceData(devName, deviceAddress);
                        dev.updateSensorData(temperature, humidity, battery, batteryV);
                        Debug.WriteLine($"Обновлены данные устройства: {devName} ({deviceAddress}) - Температура: {temperature}°C, Влажность: {humidity}%, Батарея: {battery}%");
                        // Обновляем данные на LCD
                        //LcdSetting.RefreshLCDData();
                    }
                    else
                    {
                        Debug.WriteLine($"Устройство Xiaomi обнаружено, но данные не найдены: {deviceAddress}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке рекламного пакета Xiaomi: {ex.Message}");
            }
        }

        /// <summary>
        /// Настройка BLE сервера для редактирования WiFi настроек
        /// </summary>
        private void SetupBleServer()
        {
            try
            {
                // Создание BLE сервера
                _bleServer = BluetoothLEServer.Instance;
                _bleServer.DeviceName = GlobalConstant.SERVER_NAME;

                // Создание сервиса WiFi               
                GattServiceProviderResult result = GattServiceProvider.Create(new Guid(GlobalConstant.WIFI_SERVICE_UUID));
                if (result.Error != BluetoothError.Success)
                {
                    return;
                }

                GattServiceProvider serviceProvider = result.ServiceProvider;

                // Get created Primary service from provider
                GattLocalService service = serviceProvider.Service;


                GattLocalCharacteristicResult characteristicSsid = service.CreateCharacteristic(new Guid(GlobalConstant.SSID_CHARACTERISTIC_UUID),
                new GattLocalCharacteristicParameters()
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wifi SSID"
                });


                if (characteristicSsid.Error != BluetoothError.Success)
                {
                    // An error occurred.
                    return;
                }
                var writerSsid = characteristicSsid.Characteristic;

                writerSsid.WriteRequested += _readCharacteristic_SSIDRequested;

                GattLocalCharacteristicResult characteristicPassword = service.CreateCharacteristic(new Guid(GlobalConstant.PASSWORD_CHARACTERISTIC_UUID),
                new GattLocalCharacteristicParameters()
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write,
                    UserDescription = "Wifi SSID"
                });


                if (characteristicPassword.Error != BluetoothError.Success)
                {
                    // An error occurred.
                    return;
                }
                var writerPassword = characteristicPassword.Characteristic;

                writerPassword.WriteRequested += _readCharacteristic_PasswordRequested;

                serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters()
                {
                    IsConnectable = true,
                    IsDiscoverable = true
                });

                Debug.WriteLine("Сервисы редактирования SSID и пароля запущены");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при настройке BLE сервера: {ex.Message}");
            }
        }

        private void _readCharacteristic_SSIDRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();

            // Check expected data length, we are expecting 3 bytes
            if (request.Value.Length != 3)
            {
                request.RespondWithProtocolError((byte)BluetoothError.NotSupported);
                return;
            }
            Buffer bb = request.Value;

            // Unpack data from buffer
            DataReader rdr = DataReader.FromBuffer(request.Value);
            var ssid = rdr.ReadString(rdr.UnconsumedBufferLength);

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }
            _boardManager.SetWifiCredentialsSSID(ssid);
            _filesManager.SaveWifiCredentials();
            Debug.WriteLine($"Новый ssid {ssid}");
        }

        private void _readCharacteristic_PasswordRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();

            // Check expected data length, we are expecting 3 bytes
            if (request.Value.Length != 3)
            {
                request.RespondWithProtocolError((byte)BluetoothError.NotSupported);
                return;
            }
            Buffer bb = request.Value;

            // Unpack data from buffer
            DataReader rdr = DataReader.FromBuffer(request.Value);
            var password = rdr.ReadString(rdr.UnconsumedBufferLength);

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }
            _boardManager.SetWifiCredentialsPassword(password);
            _filesManager.SaveWifiCredentials();
            Debug.WriteLine($"Новый пароль {password}");
        }

        public void Dispose()
        {
            try
            {
                _scanTimer.Dispose();
                _bleServer.Stop();
                _bleServer.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при удалении BLE сервера: {ex.Message}");
            }
        }
    }
}


