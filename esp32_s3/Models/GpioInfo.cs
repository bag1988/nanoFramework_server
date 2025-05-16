namespace esp32_s3.Models
{
    // Класс для хранения информации о GPIO
    public class GpioInfo
    {
        public byte Pin { get; set; }
        public string Name { get; set; }

        public GpioInfo(byte pin, string name)
        {
            Pin = pin;
            Name = name;
        }
    }
}
