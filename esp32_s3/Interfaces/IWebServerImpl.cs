using nanoFramework.Networking;
using System;

namespace esp32_s3.Interfaces
{
    public interface IWebServerImpl: IDisposable
    {
        NetworkHelperStatus GetConnectState { get; }
        string GetIpAddres { get; }
        void Connect();
        void InitWebServer();
    }
}