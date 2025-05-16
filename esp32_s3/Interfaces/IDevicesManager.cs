using esp32_s3.Models;

namespace esp32_s3.Interfaces
{
    public interface IDevicesManager
    {
        void InitDevices(DeviceData[] devices);
        bool AddDevice(DeviceData device);
        void UpdateOrAddDevice(DeviceData device);
        void ResetHeatingStat();
        bool CheckOnlineDevice();
        DeviceData[] GetDevices { get; }
    }
}