using nanoFramework.Hardware.Esp32;
namespace esp32_s3.Models
{
    public class GlobalConstant
    {
        public const int XIAOMI_OFFLINE_TIMEOUT = 30_000;// 5 минут до перехода в оффлайн        
        public const int CONTROL_DELAY = 30_000;       // Интервал проверки и управления GPIO (мс)
        public const int WIFI_RECONNECT_DELAY = 60_000; // Интервал попыток переподключения к WiFi (мс)
        public const int SAVE_HEATING_STAT_DELAY = 300_000; // каждые 5 минут
        public const int XIAOMI_SCAN_INTERVAL = 60_000; // Интервал сканирования датчиков Xiaomi (мс)
        public const int XIAOMI_SCAN_DURATION = 30; // Продолжительность сканирования BLE (секунд)

        // Определение пинов
        public const int NUM_LEDS = 1;
        public const int PIN_NEOPIXEL = 48; // пин для NeoPixel

        #region for BLE
        public const string SERVER_NAME = "ESP32_BLE_CENTRAL_SERVER";
        public const string SERVICE_UUID = "33b6ebbe-538f-4d4a-ba39-2ee04516ff39";
        public const string TEMPERATURE_UUID = "ccfe71ea-e98b-4927-98e2-6c1b77d1f756";
        public const string HUMIDITY_UUID = "6ed76625-573e-4caa-addf-3ddc5a283095";
        public const string WIFI_SERVICE_UUID = "e1de7d6e-3104-4065-a187-2de5e5727b26";
        public const string SSID_CHARACTERISTIC_UUID = "93d971b2-4bb8-45d0-9ab3-74d7f881d828";
        public const string PASSWORD_CHARACTERISTIC_UUID = "c5481513-22cb-4aae-9fe3-e9db5d06bf6f";

        public const string UUID_ATC_1 = "0000181A-0000-1000-8000-00805F9B34FB";
        public const string UUID_ATC_2 = "0000FE95-0000-1000-8000-00805F9B34FB";

        #endregion

        #region for LCD
        // Определение кодов кнопок для LCD Keypad Shield
        public const int BUTTON_RIGHT = 0;
        public const int BUTTON_UP = 1;
        public const int BUTTON_DOWN = 2;
        public const int BUTTON_LEFT = 3;
        public const int BUTTON_SELECT = 4;
        public const int BUTTON_NONE = 5;

        // Задержка для устранения дребезга контактов (мс)
        public const int BUTTON_DEBOUNCE_DELAY = 200;

        // Определение кнопок LCD Keypad Shield
        // Аналоговый пин для кнопок на ESP32-S3 UNO
        public const int KEYPAD_PIN = Gpio.IO02; // GPIO2 соответствует A0 на arduino UNO

        // Значения для ESP32-S3 (12-битный ADC, 0-4095)
        // Эти значения требуют калибровки для конкретного устройства
        public const int KEY_RIGHT_VAL = 0;     // Значение около 0
        public const int KEY_UP_VAL = 700;      // Примерные значения
        public const int KEY_DOWN_VAL = 1700;   // Требуют калибровки
        public const int KEY_LEFT_VAL = 2800;   // 
        public const int KEY_SELECT_VAL = 3000; //
        public const int KEY_NONE_VAL = 4095;   // Ни одна кнопка не нажата

        // Допустимое отклонение для значений кнопок
        public const int KEY_THRESHOLD = 200;
        // Задержка для прокрутки текста
        public const int SCROLL_DELAY = 500;
        #endregion
    }
}
