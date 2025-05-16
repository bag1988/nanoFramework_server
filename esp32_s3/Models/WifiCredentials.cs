
namespace esp32_s3.Models
{
    // Класс для хранения учетных данных WiFi
    public class WifiCredentials
    {
        public string SSID { get; set; }
        public string Password { get; set; }

        public WifiCredentials()
        {
            SSID = "";
            Password = "";
        }
    }
}
