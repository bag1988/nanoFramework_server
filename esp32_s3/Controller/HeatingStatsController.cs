using nanoFramework.WebServer;
using System.Net;
using System;
using esp32_s3.Interfaces;
using nanoFramework.Json;
using System.Collections;
using esp32_s3.Models;


namespace esp32_s3.Controller
{
    public class HeatingStatsController
    {
        readonly IDevicesManager _devicesManager;

        public HeatingStatsController(IDevicesManager devicesManager)
        {
            _devicesManager = devicesManager;
        }

        [Route("heating_stats")]
        [Method("GET")]
        public void GetHeatingStats(WebServerEventArgs e)
        {
            try
            {
                var devList = _devicesManager.GetDevices;

                ArrayList infoModel = new();

                foreach (var device in devList)
                {
                    infoModel.Add(new HeatingStatsInfo
                    (
                        device.Name,
                        device.MacAddress,
                        device.CurrentTemperature,
                        device.TargetTemperature,
                        device.HeatingActive,
                        device.TotalHeatingTime,
                        device.TotalHeatingTime.Days + " дней " + device.TotalHeatingTime.Hours + " часов " + device.TotalHeatingTime.Minutes + " минут " + device.TotalHeatingTime.Seconds + " секунд"
                    ));
                }

                var response = JsonConvert.SerializeObject(infoModel);
                e.Context.Response.ContentType = "application/json";
                e.Context.Response.ContentLength64 = response.Length;
                WebServer.OutPutStream(e.Context.Response, response);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("heating_stats")]
        [Method("POST")]
        public void ResetHeatingStats(WebServerEventArgs e)
        {
            try
            {
                _devicesManager.ResetHeatingStat();
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }
    }
}
