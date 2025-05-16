using esp32_s3.Interfaces;
using esp32_s3.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace esp32_s3.Services
{
    public class MainLogicWorker : SchedulerService
    {
        readonly ILcdKeyShield _lcdKeyShield;
        readonly IFilesManager _filesManager;
        readonly IBoardManager _boardManager;
        readonly IDevicesManager _devicesManager;
        DateTime LastGpioControlTime = DateTime.UtcNow;
        DateTime LastDataSaveStat = DateTime.UtcNow;
        Hashtable _activeGpio = new();
        bool IsExpiredIntervalControlGpio => (DateTime.UtcNow - LastGpioControlTime).TotalMilliseconds > GlobalConstant.CONTROL_DELAY;
        bool IsExpiredIntervalSaveStat => (DateTime.UtcNow - LastDataSaveStat).TotalMilliseconds > GlobalConstant.SAVE_HEATING_STAT_DELAY;

        public MainLogicWorker(ILcdKeyShield lcdKeyShield, IFilesManager filesManager, IBoardManager boardManager, IDevicesManager devicesManager) : base(TimeSpan.FromMilliseconds(200))
        {
            _lcdKeyShield = lcdKeyShield;
            _filesManager = filesManager;
            _boardManager = boardManager;
            _devicesManager = devicesManager;
        }

        // Инициализация GPIO
        private void InitializeGpio()
        {
            GpioController gpio = new();
            foreach (var pin in _boardManager.GetGpio)
            {
                var g = gpio.OpenPin(pin.Pin, PinMode.Output);
                g.Write(PinValue.Low);
                _activeGpio.Add(pin.Pin, g);
            }
        }
        // Управление GPIO
        private void ControlGPIO()
        {
            try
            {
                Debug.WriteLine("Проверка необходимости включения GPIO");
                ArrayList gpiosToTurnOn = new();
                var currentTime = DateTime.UtcNow;

                // Собираем GPIO для включения
                foreach (DeviceData device in _devicesManager.GetDevices)
                {
                    if (device.IsDataValid())
                    {
                        if (device.HeatingActive)
                        {
                            // Вычисляем время, прошедшее с момента последнего обновления
                            var elapsedTime = currentTime - device.HeatingStartTime;
                            // Обновляем общее время работы
                            device.TotalHeatingTime += elapsedTime;
                            // Обновляем время начала для следующего расчета
                            device.HeatingStartTime = currentTime;
                        }

                        Debug.WriteLine($"Устройство {device.Name}: обогрев включен - {(device.HeatingActive ? "да" : "нет")}, необходим обогрев - {((device.CurrentTemperature + 2) < device.TargetTemperature ? "да" : "нет")}");

                        if (!device.HeatingActive && device.Enabled && device.IsOnline && (device.CurrentTemperature + 2) < device.TargetTemperature)
                        {
                            device.HeatingActive = true;
                            device.HeatingStartTime = currentTime; // Запоминаем время включения

                            // Добавляем GPIO пины устройства в список для включения
                            foreach (int pin in device.GpioPins)
                            {
                                gpiosToTurnOn.Add(pin);
                            }

                            Debug.WriteLine($"Устройство {device.Name}: включаем обогрев (температура {device.CurrentTemperature:F1}°C, целевая {device.TargetTemperature:F1}°C)");
                        }
                        else if (device.HeatingActive && device.CurrentTemperature >= device.TargetTemperature)
                        {
                            // Температура достигла целевой - выключаем обогрев
                            device.HeatingActive = false;
                            Debug.WriteLine($"Устройство {device.Name}: выключаем обогрев (температура {device.CurrentTemperature:F1}°C, целевая {device.TargetTemperature:F1}°C)");
                        }
                        else if (!device.Enabled && device.HeatingActive)
                        {
                            // Если обогрев был активен, обновляем общее время работы перед выключением
                            var elapsedTime = currentTime - device.HeatingStartTime;
                            device.TotalHeatingTime += elapsedTime;
                            device.HeatingActive = false;
                        }
                    }
                    else if (device.IsOnline)
                    {
                        device.IsOnline = false;
                        device.HeatingActive = false;
                        Debug.WriteLine($"Устройство {device.Name}: нет данных");
                    }
                }

                // Удаляем дубликаты GPIO
                ArrayList uniqueGpios = new();
                foreach (int pin in gpiosToTurnOn)
                {
                    if (!uniqueGpios.Contains(pin))
                    {
                        uniqueGpios.Add(pin);
                    }
                }
                // Управляем GPIO
                foreach (byte pin in _activeGpio.Keys)
                {
                    bool shouldTurnOn = uniqueGpios.Contains(pin);

                    ((GpioPin)_activeGpio[pin]).Write(shouldTurnOn ? PinValue.High : PinValue.Low);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка во время управления gpio: {ex.Message}");
            }
        }
        public override void StartAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"Service '{nameof(MainLogicWorker)}' is now running in the background.");
            _filesManager.LoadAll();
            _lcdKeyShield.InitLCD();
            _lcdKeyShield.initScrollText();
            _lcdKeyShield.UpdateLCD();
            InitializeGpio();
            base.StartAsync(cancellationToken);
        }
        protected override void ExecuteAsync(CancellationToken stoppingToken)
        {
            _lcdKeyShield.HandleButtons();
            _lcdKeyShield.UpdateLCDTask();

            if (IsExpiredIntervalControlGpio)
            {
                LastGpioControlTime = DateTime.UtcNow;
                ControlGPIO();
                _lcdKeyShield.RefreshLCDData();
            }

            if (IsExpiredIntervalSaveStat)
            {
                Debug.WriteLine("Сохранение статистики согласно таймаута, сохраняем результаты");
                LastDataSaveStat = DateTime.UtcNow;
                _filesManager.SaveClient();
                _boardManager.UpdateServerWorkTime();
                _filesManager.SaveServerSetting();
            }
        }

        public override void StopAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"Service '{nameof(MainLogicWorker)}' is stopping.");
            _filesManager.LoadClients();
            _filesManager.SaveGpio();
            base.StopAsync(cancellationToken);
        }
    }
}
