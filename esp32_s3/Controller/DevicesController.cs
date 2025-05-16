using nanoFramework.WebServer;
using System.Net;
using System;
using esp32_s3.Interfaces;
using nanoFramework.Json;
using esp32_s3.Models;
using System.Diagnostics;

namespace esp32_s3.Controller
{
    public class DevicesController
    {
        readonly IDevicesManager _devicesManager;

        public DevicesController(IDevicesManager devicesManager)
        {
            _devicesManager = devicesManager;
        }

        [Route("clients")]
        [Method("GET")]
        public void GetClients(WebServerEventArgs e)
        {
            try
            {
                var response = JsonConvert.SerializeObject(_devicesManager.GetDevices);
                e.Context.Response.ContentType = "application/json";
                e.Context.Response.ContentLength64 = response.Length;
                WebServer.OutPutStream(e.Context.Response, response);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("clients")]
        [Method("POST")]
        public void SaveClient(WebServerEventArgs e)
        {
            try
            {
                var device = (DeviceData)JsonConvert.DeserializeObject(e.Context.Request.InputStream, typeof(DeviceData));

                _devicesManager.UpdateOrAddDevice(device);
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.Accepted);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления данных устройств: {ex.Message}");
            }
            WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
        }

    }
}
