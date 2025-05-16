using System;

namespace esp32_s3.Interfaces
{
    public interface IBluetoothManager: IDisposable
    {
        void SetupXiaomiScanner();
        void StartXiaomiScan(int duration = 30);
    }
}