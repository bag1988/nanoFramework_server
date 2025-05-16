using esp32_s3.Controller;
using esp32_s3.Interfaces;
using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Device.Wifi;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace esp32_s3.Services
{
    public class WebServerImpl : IWebServerImpl
    {        
        CancellationTokenSource tokenSource = new();
        private WebServer _server;
        readonly IBoardManager _boardManager;

        public WebServerImpl(IBoardManager boardManager)
        {
            _boardManager = boardManager;
        }
              
        public void InitWebServer()
        {
            try
            {
                //_server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(DevicesController), typeof(BoardController), typeof(HeatingStatsController) });
                _server.CommandReceived += ServerCommandReceived;
                _server.WebServerStatusChanged += WebServerStatusChanged;
                _server.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка запуска веб-сервера: " + ex.Message);
            }
        }

        private static void ServerCommandReceived(object source, WebServerEventArgs e)
        {
            try
            {
                var urlSegment = e.Context.Request.RawUrl.ToLower();

                if (string.IsNullOrEmpty(urlSegment) || urlSegment == "/")
                {
                    urlSegment = "index.html";
                }
                else
                {
                    urlSegment = urlSegment.TrimStart('/').TrimEnd('/');
                }

                if (urlSegment.IndexOf('/') < 0 && Path.HasExtension(urlSegment))
                {
                    var filePath = Path.Combine("I:\\Data", urlSegment);
                    if (File.Exists(filePath))
                    {
                        var content = File.ReadAllBytes(filePath);
                        WebServer.SendFileOverHTTP(e.Context.Response, Path.GetFileName(filePath), content);
                    }
                }
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
        }
        private void WebServerStatusChanged(object obj, WebServerStatusEventArgs e)
        {
            Debug.WriteLine("Изменен статус сервера: " + e.Status);
        }

        public void Connect()
        {
            try
            {
                var wifiSetting = _boardManager.GetWifiCredentials;
                if(!string.IsNullOrEmpty(wifiSetting.SSID))
                {
                    Debug.WriteLine($"Подключаемся к WiFi {wifiSetting.SSID}, пароль {wifiSetting.Password}");
                    var b = WifiNetworkHelper.ConnectDhcp(wifiSetting.SSID, wifiSetting.Password, WifiReconnectionKind.Automatic, token: tokenSource.Token);
                }
               else
                {
                    Debug.WriteLine($"Параметры wifi не заданы!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подключения к Wifi: {ex}");
            }
        }

        public string GetIpAddres
        {
            get
            {
                return System.Net.NetworkInformation.IPGlobalProperties.GetIPAddress().MapToIPv4().ToString();
            }
        }

        public NetworkHelperStatus GetConnectState
        {
            get
            {
                return WifiNetworkHelper.Status;
            }
        }

        public void Dispose()
        {
            try
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                _server.Stop();
                _server.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка остановки сервера: {ex.Message}");
            }            
        }
    }
}
