namespace esp32_s3.Interfaces
{
    public interface IFilesManager
    {
        void LoadAll();
        void LoadClients();
        void LoadGpio();
        void LoadWifiCredentials();
        void SaveClient();
        void SaveGpio();
        void SaveWifiCredentials();
        void LoadServerSetting();
        void SaveServerSetting();
    }
}