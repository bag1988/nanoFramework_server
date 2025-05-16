
using nanoFramework.WebServer;
using System.Net;
using System;
using esp32_s3.Interfaces;
using nanoFramework.Json;
using esp32_s3.Models;
using System.Diagnostics;

namespace esp32_s3.Controller
{
    public class BoardController
    {
        readonly IBoardManager _boardManager;

        public BoardController(IBoardManager boardManager)
        {
            _boardManager = boardManager;
        }

        [Route("serverinfo")]
        [Method("GET")]
        public void GetServerInfo(WebServerEventArgs e)
        {
            try
            {               
                nanoFramework.Hardware.Esp32.NativeMemory.GetMemoryInfo(nanoFramework.Hardware.Esp32.NativeMemory.MemoryType.SpiRam, out var totalSpi, out var freeSpi, out var largestFreeBlockSpi);
                nanoFramework.Hardware.Esp32.NativeMemory.GetMemoryInfo(nanoFramework.Hardware.Esp32.NativeMemory.MemoryType.Internal, out var totalInternal, out var freeInternal, out var largestFreeBlockInternal);

                var responseModel = new
                {
                    totalSpi,
                    freeSpi,
                    largestFreeBlockSpi,
                    totalInternal,
                    freeInternal,
                    largestFreeBlockInternal,
                    workTime = _boardManager.GetWorkTimeString,
                    boardTemperature = _boardManager.BoardTemperature
                };

                var response = JsonSerializer.SerializeObject(responseModel);
                e.Context.Response.ContentType = "application/json";
                e.Context.Response.ContentLength64 = response.Length;
                WebServer.OutPutStream(e.Context.Response, response);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("availablegpio")]
        [Method("GET")]
        public void GetGpio(WebServerEventArgs e)
        {
            try
            {
                var response = JsonSerializer.SerializeObject(_boardManager.GetGpio);
                e.Context.Response.ContentType = "application/json";
                e.Context.Response.ContentLength64 = response.Length;
                WebServer.OutPutStream(e.Context.Response, response);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("availablegpio")]
        [Method("POST")]
        public void SaveGpio(WebServerEventArgs e)
        {
            try
            {
                var gpios = (GpioInfo[])JsonConvert.DeserializeObject(e.Context.Request.InputStream, typeof(GpioInfo[]));
                _boardManager.UpdateGpios(gpios);
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.Accepted);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления gpio: {ex.Message}");
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

        [Route("scan")]
        [Method("POST")]
        public void StartScan(WebServerEventArgs e)
        {
            try
            {
                
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления gpio: {ex.Message}");
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.BadRequest);
            }
        }

    }
}
