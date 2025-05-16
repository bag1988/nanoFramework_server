
namespace esp32_s3.Models
{
    public enum MenuState
    {
        MAIN_SCREEN,      // Главный экран с информацией
        DEVICE_LIST,      // Список устройств
        DEVICE_MENU,      // Меню настроек устройства
        EDIT_TEMPERATURE, // Редактирование целевой температуры
        EDIT_GPIO,        // Редактирование GPIO пинов
        EDIT_ENABLED      // Включение/выключение устройства
    }
}
