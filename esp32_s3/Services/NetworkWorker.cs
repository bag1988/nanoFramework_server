using esp32_s3.Interfaces;
using esp32_s3.Models;
using Iot.Device.Ws28xx.Esp32;
using Microsoft.Extensions.Hosting;
using nanoFramework.Hardware.Esp32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing;

namespace esp32_s3.Services
{
    public class NetworkWorker : SchedulerService
    {
        readonly IBluetoothManager _bluetoothManager;
        readonly IWebServerImpl _webServerManager;

        DateTime LastDataScanBle = DateTime.UtcNow;
        DateTime LastDataWifiConnect = DateTime.UtcNow;

        bool IsExpiredIntervalScanBle => (DateTime.UtcNow - LastDataScanBle).TotalMilliseconds > GlobalConstant.XIAOMI_SCAN_INTERVAL;
        bool IsExpiredIntervalReconnect => (DateTime.UtcNow - LastDataWifiConnect).TotalMilliseconds > GlobalConstant.WIFI_RECONNECT_DELAY;
               
        // Use Ws2812 or SK6812 instead if needed
        Ws2808 neo = new Ws2808(GlobalConstant.NUM_LEDS, GlobalConstant.PIN_NEOPIXEL);
        bool IsRed = false;
        public NetworkWorker(IBluetoothManager bluetoothManager, IWebServerImpl webServerManager) : base(TimeSpan.FromSeconds(1))
        {
            _bluetoothManager = bluetoothManager;
            _webServerManager = webServerManager;
        }

        public override void StartAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"Service '{nameof(NetworkWorker)}' is now running in the background.");
            _webServerManager.Connect();
            _bluetoothManager.SetupXiaomiScanner();
            _webServerManager.InitWebServer();

            Configuration.SetPinFunction(23, DeviceFunction.SPI2_MOSI);

            base.StartAsync(cancellationToken);
        }
        protected override void ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine($"Service '{nameof(NetworkWorker)}' is work.");

            if (_webServerManager.GetConnectState == nanoFramework.Networking.NetworkHelperStatus.NetworkIsReady)
            {
                Rainbow(Color.Green);
                if (IsExpiredIntervalReconnect)
                {
                    LastDataWifiConnect = DateTime.UtcNow;
                    _webServerManager.Connect();
                }
            }
            else
            {
                if(IsRed)
                {
                    neo.Image.Clear();
                    neo.Update();
                }
                else
                {
                    Rainbow(Color.Red);
                }
                IsRed = !IsRed;
            }

            if (IsExpiredIntervalScanBle)
            {
                LastDataScanBle = DateTime.UtcNow;
                _bluetoothManager.StartXiaomiScan();
            }
        }

        void Rainbow(Color color)
        {
            neo.Image.SetPixel(0, 0, color);
            neo.Update();
        }

        public override void StopAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"Service '{nameof(NetworkWorker)}' is stopping.");
            _bluetoothManager.Dispose();
            _webServerManager.Dispose();
            base.StopAsync(cancellationToken);
        }
    }
}
