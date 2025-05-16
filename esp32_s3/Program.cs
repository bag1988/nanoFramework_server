using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using esp32_s3.Services;
using esp32_s3.Interfaces;

// Browse our samples repository: https://github.com/nanoframework/samples
// Check our documentation online: https://docs.nanoframework.net/
// Join our lively Discord community: https://discord.gg/gCyBu8T
namespace esp32_s3
{
    public class Program
    {
        // Основная точка входа
        public static void Main()
        {
            Debug.WriteLine("Запуск системы...");

            // Инициализация NeoPixel
            //pixels = new NeoPixel(PIN_NEOPIXEL, NUM_LEDS);
            //pixels.SetBrightness(50);
            //pixels.Show();

            // registering services 
            //var serviceProvider = new ServiceCollection()
            //    .AddSingleton(typeof(IDevicesManager), typeof(DevicesManager))
            //    .AddHostedService(typeof(NetworkWorker))
            //    .BuildServiceProvider();

            // create a service provider to get access to the RootObject
            //var service = (RootObject)serviceProvider.GetService(typeof(RootObject));

            IHostBuilder hostBuilder = CreateHostBuilder();
            IHost host = hostBuilder.Build();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder() =>
           Host.CreateDefaultBuilder()
               .ConfigureServices(services =>
               {
                   services.AddSingleton(typeof(IDevicesManager), typeof(DevicesManager));
                   services.AddSingleton(typeof(IBoardManager), typeof(BoardManager));
                   services.AddSingleton(typeof(IBluetoothManager), typeof(BluetoothManager));
                   services.AddSingleton(typeof(IFilesManager), typeof(IFilesManager));
                   services.AddSingleton(typeof(ILcdKeyShield), typeof(LcdKeyShield));
                   services.AddSingleton(typeof(IWebServerImpl), typeof(WebServerImpl));                   
                   services.AddHostedService(typeof(MainLogicWorker));
                   services.AddHostedService(typeof(NetworkWorker));
               });
    }
}

