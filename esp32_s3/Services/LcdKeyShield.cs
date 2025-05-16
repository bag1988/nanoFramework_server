using esp32_s3.Interfaces;
using esp32_s3.Models;
using Iot.Device.CharacterLcd;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace esp32_s3.Services
{
    /// <summary>
    /// Класс для работы с LCD дисплеем
    /// </summary>
    public class LcdKeyShield : ILcdKeyShield
    {
        // Переменные для прокрутки текста
        public string ScrollText = "";
        public int ScrollPosition = 0;

        // Текущее состояние меню
        public MenuState CurrentMenu = MenuState.MAIN_SCREEN;

        // Индексы для навигации
        public int DeviceListIndex = 0;    // Индекс в списке устройств
        public int DeviceMenuIndex = 0;    // Индекс в меню устройства
        public int GpioSelectionIndex = 0; // Индекс для выбора GPIO

        // Опции меню устройства
        public readonly string[] DeviceMenuOptions = { "Temperature", "GPIO", "On/Off", "Back" };
        public readonly int DeviceMenuOptionsCount = 5;

        // Флаг для обновления экрана
        public bool NeedLcdUpdate = true;

        // LCD объект
        //private  LiquidCrystal _lcd;
        private Lcd1602 _lcd;

        // Последнее время нажатия кнопки
        private long _lastButtonTime = 0;
        private int _lastButton = GlobalConstant.BUTTON_NONE;
        private long _buttonPressStartTime = 0;
        private bool _longPressHandled = false;
        private readonly GpioController _gpio;
        private readonly GpioPin _analogPin;
        readonly IDevicesManager _devicesManager;
        readonly IBoardManager _boardManager;
        readonly IWebServerImpl _webServerManager;
        readonly IFilesManager _filesManager;

        bool IsInit = false;

        public LcdKeyShield(IDevicesManager devicesManager, IBoardManager boardManager, IWebServerImpl webServerManager, IFilesManager filesManager)
        {
            _gpio = new GpioController();
            _analogPin = _gpio.OpenPin(GlobalConstant.KEYPAD_PIN, PinMode.Input);
            _devicesManager = devicesManager;
            _boardManager = boardManager;
            _webServerManager = webServerManager;
            _filesManager = filesManager;
        }

        /// <summary>
        /// Инициализация LCD дисплея
        /// </summary>
        public void InitLCD()
        {
            try
            {
                // Инициализация LCD
                // Для ESP32-S3 UNO с LCD Keypad Shield
                // RS, E, D4, D5, D6, D7
                //_lcd = new LiquidCrystal(21, 46, 19, 20, 3, 14);

                _lcd = new Lcd1602(21, 46, new int[] { 19, 20, 3, 14 });

                //_lcd.Begin(16, 2);
                DisplayText("Initialization...");

                Thread.Sleep(1000);

                // Информация о навигации
                DisplayText("Navigation info:");
                DisplayText("LONG RIGHT=SELECT", 0, 1);
                IsInit = true;
                // Настройка пина для считывания кнопок
                // Configuration.SetPinFunction(GlobalConstant.KEYPAD_PIN, DeviceFunction.ADC1_CH2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации LCD: {ex.Message}");
            }
        }

        /// <summary>
        /// Чтение состояния кнопок
        /// </summary>
        private int ReadKeypad()
        {
            // Чтение аналогового значения с пина кнопок

            int adcValue = AnalogRead();

            if (adcValue == 0)
            {
                return GlobalConstant.BUTTON_NONE;
            }
            else if (adcValue < GlobalConstant.KEY_RIGHT_VAL + GlobalConstant.KEY_THRESHOLD)
            {
                return GlobalConstant.BUTTON_RIGHT;
            }
            else if (adcValue < GlobalConstant.KEY_UP_VAL + GlobalConstant.KEY_THRESHOLD)
            {
                return GlobalConstant.BUTTON_UP;
            }
            else if (adcValue < GlobalConstant.KEY_DOWN_VAL + GlobalConstant.KEY_THRESHOLD)
            {
                return GlobalConstant.BUTTON_DOWN;
            }
            else if (adcValue < GlobalConstant.KEY_LEFT_VAL + GlobalConstant.KEY_THRESHOLD)
            {
                return GlobalConstant.BUTTON_LEFT;
            }

            return GlobalConstant.BUTTON_NONE; // Ни одна кнопка не нажата
        }

        /// <summary>
        /// Чтение аналогового значения с пина
        /// </summary>
        private int AnalogRead()
        {
            // Пример реализации:
            return (int)_analogPin.Read();
        }

        /// <summary>
        /// Отображение текста на LCD
        /// </summary>
        public void DisplayText(string text, int column = 0, int row = 0, bool clearLine = true, bool center = false)
        {
            if (!IsInit) return;

            // Проверка корректности параметров
            if (row < 0 || row > 1)
            {
                Debug.WriteLine("Ошибка: Некорректный номер строки. Допустимые значения: 0 или 1");
                return;
            }

            // Если нужно очистить всю строку перед выводом
            if (clearLine)
            {
                _lcd.SetCursorPosition(0, row);
                _lcd.Write("                "); // 16 пробелов для очистки строки
            }

            // Преобразуем текст в кодировку A02 для корректного отображения кириллицы
            string a02Text = Utf8ToA02(text);

            // Если нужно центрировать текст
            if (center)
            {
                int textLength = text.Length;
                if (textLength < 16)
                {
                    column = (16 - textLength) / 2;
                }
                else
                {
                    column = 0;
                }
            }

            // Ограничиваем столбец допустимыми значениями
            column = column > 15 ? 15 : column;

            // Устанавливаем курсор и выводим текст
            _lcd.SetCursorPosition(column, row);
            _lcd.Write(a02Text);
        }

        /// <summary>
        /// Преобразование UTF-8 в кодировку A02 для LCD
        /// </summary>
        private static string Utf8ToA02(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var result = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                // Преобразование символов кириллицы в кодировку A02
                switch (c)
                {
                    // Русские буквы (верхний регистр)
                    case 'А': result.Append((char)0x41); break; // A
                    case 'Б': result.Append((char)0xA0); break; // Б
                    case 'В': result.Append((char)0x42); break; // B
                    case 'Г': result.Append((char)0xA1); break; // Г
                    case 'Д': result.Append((char)0xE0); break; // Д
                    case 'Е': result.Append((char)0x45); break; // E
                    case 'Ё': result.Append((char)0xA2); break; // Ё
                    case 'Ж': result.Append((char)0xA3); break; // Ж
                    case 'З': result.Append((char)0xA4); break; // З
                    case 'И': result.Append((char)0xA5); break; // И
                    case 'Й': result.Append((char)0xA6); break; // Й
                    case 'К': result.Append((char)0x4B); break; // K
                    case 'Л': result.Append((char)0xA7); break; // Л
                    case 'М': result.Append((char)0x4D); break; // M
                    case 'Н': result.Append((char)0x48); break; // H
                    case 'О': result.Append((char)0x4F); break; // O
                    case 'П': result.Append((char)0xA8); break; // П
                    case 'Р': result.Append((char)0x50); break; // P
                    case 'С': result.Append((char)0x43); break; // C
                    case 'Т': result.Append((char)0x54); break; // T
                    case 'У': result.Append((char)0xA9); break; // У
                    case 'Ф': result.Append((char)0xAA); break; // Ф
                    case 'Х': result.Append((char)0x58); break; // X
                    case 'Ц': result.Append((char)0xE1); break; // Ц
                    case 'Ч': result.Append((char)0xAB); break; // Ч
                    case 'Ш': result.Append((char)0xAC); break; // Ш
                    case 'Щ': result.Append((char)0xE2); break; // Щ
                    case 'Ъ': result.Append((char)0xAD); break; // Ъ
                    case 'Ы': result.Append((char)0xAE); break; // Ы
                    case 'Ь': result.Append((char)0x62); break; // Ь (как b)
                    case 'Э': result.Append((char)0xAF); break; // Э
                    case 'Ю': result.Append((char)0xB0); break; // Ю
                    case 'Я': result.Append((char)0xB1); break; // Я

                    // Русские буквы (нижний регистр)
                    case 'а': result.Append((char)0x61); break; // a
                    case 'б': result.Append((char)0xB2); break; // б
                    case 'в': result.Append((char)0xB3); break; // в
                    case 'г': result.Append((char)0xB4); break; // г
                    case 'д': result.Append((char)0xE3); break; // д
                    case 'е': result.Append((char)0x65); break; // e
                    case 'ё': result.Append((char)0xB5); break; // ё
                    case 'ж': result.Append((char)0xB6); break; // ж
                    case 'з': result.Append((char)0xB7); break; // з
                    case 'и': result.Append((char)0xB8); break; // и
                    case 'й': result.Append((char)0xB9); break; // й
                    case 'к': result.Append((char)0xBA); break; // к
                    case 'л': result.Append((char)0xBB); break; // л
                    case 'м': result.Append((char)0xBC); break; // м
                    case 'н': result.Append((char)0xBD); break; // н
                    case 'о': result.Append((char)0x6F); break; // o
                    case 'п': result.Append((char)0xBE); break; // п
                    case 'р': result.Append((char)0x70); break; // p
                    case 'с': result.Append((char)0x63); break; // c
                    case 'т': result.Append((char)0xBF); break; // т
                    case 'у': result.Append((char)0x79); break; // y
                    case 'ф': result.Append((char)0xE4); break; // ф
                    case 'х': result.Append((char)0x78); break; // x
                    case 'ц': result.Append((char)0xE5); break; // ц
                    case 'ч': result.Append((char)0xC0); break; // ч
                    case 'ш': result.Append((char)0xC1); break; // ш
                    case 'щ': result.Append((char)0xE6); break; // щ
                    case 'ъ': result.Append((char)0xC2); break; // ъ
                    case 'ы': result.Append((char)0xC3); break; // ы
                    case 'ь': result.Append((char)0xC4); break; // ь
                    case 'э': result.Append((char)0xC5); break; // э
                    case 'ю': result.Append((char)0xC6); break; // ю
                    case 'я': result.Append((char)0xC7); break; // я

                    // Специальные символы
                    case '°': result.Append((char)0xDF); break; // Градус
                    case 'µ': result.Append((char)0xE4); break; // Микро

                    // Для остальных символов (латиница, цифры, знаки пунктуации) используем исходный код
                    default:
                        result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Обновление текста для прокрутки
        /// </summary>
        public void initScrollText()
        {
            if (!IsInit) return;
            ScrollText = "";

            // Добавляем информацию о датчиках и устройствах
            foreach (DeviceData device in _devicesManager.GetDevices)
            {
                if (device.IsDataValid())
                {
                    ScrollText += device.Name + ": " + device.CurrentTemperature.ToString("F1") + "C";

                    if (device.Enabled)
                    {
                        ScrollText += "/" + device.TargetTemperature.ToString("F1") + "C";

                        // Добавляем статус обогрева
                        if (device.HeatingActive)
                        {
                            ScrollText += "-(heating)";
                        }
                        else
                        {
                            ScrollText += "-(OK)";
                        }
                    }

                    ScrollText += " Hum: " + device.Humidity.ToString("F1") + "% Battery: " + device.Battery + "% | ";
                }
                else if (device.Enabled)
                {
                    // Показываем только включенные устройства, которые не в сети
                    ScrollText += device.Name + ": Not data | ";
                }
            }

            // Если текст пустой, добавляем информационное сообщение
            if (string.IsNullOrEmpty(ScrollText))
            {
                ScrollText = "No active devices | Add devices via the web interface | ";
            }

            // Сбрасываем позицию прокрутки
            ScrollPosition = 0;
        }

        /// <summary>
        /// Обработка нажатий кнопок
        /// </summary>
        public void HandleButtons()
        {
            try
            {
                if (!IsInit) return;
                // Чтение состояния кнопок
                int pressedButton = ReadKeypad();

                // Если ни одна кнопка не нажата, выходим
                if (pressedButton == GlobalConstant.BUTTON_NONE)
                {
                    return;
                }

                long currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                // Если нажата новая кнопка, сбрасываем таймер длительного нажатия
                if (pressedButton != _lastButton)
                {
                    _buttonPressStartTime = currentTime;
                    _longPressHandled = false;
                }

                // Проверяем, не та же ли кнопка нажата и прошло ли достаточно времени (защита от дребезга)
                if (pressedButton == _lastButton && currentTime - _lastButtonTime < GlobalConstant.BUTTON_DEBOUNCE_DELAY)
                {
                    return;
                }

                // Проверка длительного нажатия RIGHT (замена SELECT)
                if (pressedButton == GlobalConstant.BUTTON_RIGHT && !_longPressHandled &&
                    currentTime - _buttonPressStartTime > 1000)
                {                                // 1000 мс для длительного нажатия
                    pressedButton = GlobalConstant.BUTTON_SELECT; // Заменяем на SELECT
                    _longPressHandled = true;
                }

                // Обновляем время последнего нажатия и последнюю нажатую кнопку
                _lastButtonTime = currentTime;
                _lastButton = pressedButton;

                // Флаг для обновления экрана
                NeedLcdUpdate = true;

                // Обработка нажатий в зависимости от текущего состояния меню
                switch (CurrentMenu)
                {
                    case MenuState.MAIN_SCREEN:
                        HandleMainScreenButtons(pressedButton);
                        break;
                    case MenuState.DEVICE_LIST:
                        HandleDeviceListButtons(pressedButton);
                        break;
                    case MenuState.DEVICE_MENU:
                        HandleDeviceMenuButtons(pressedButton);
                        break;
                    case MenuState.EDIT_TEMPERATURE:
                        HandleTemperatureEditButtons(pressedButton);
                        break;
                    case MenuState.EDIT_GPIO:
                        HandleGpioEditButtons(pressedButton);
                        break;
                    case MenuState.EDIT_ENABLED:
                        HandleEnabledEditButtons(pressedButton);
                        break;
                }

                // Обновляем дисплей
                UpdateLCD();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обработки нажатия кнопок: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка кнопок на главном экране
        /// </summary>
        private void HandleMainScreenButtons(int pressedButton)
        {
            // На главном экране
            if (pressedButton == GlobalConstant.BUTTON_SELECT)
            {
                // Переход к списку устройств
                CurrentMenu = MenuState.DEVICE_LIST;
            }
            else if (pressedButton == GlobalConstant.BUTTON_LEFT || pressedButton == GlobalConstant.BUTTON_RIGHT)
            {
                // Прокрутка текста влево/вправо
                if (pressedButton == GlobalConstant.BUTTON_LEFT)
                {
                    ScrollPosition = (ScrollPosition + 1) % ScrollText.Length;
                }
                else if (pressedButton == GlobalConstant.BUTTON_RIGHT && !_longPressHandled) // Проверяем, что это не длительное нажатие
                {
                    ScrollPosition = (ScrollPosition + ScrollText.Length - 1) % ScrollText.Length;
                }
            }
        }

        /// <summary>
        /// Обработка кнопок в списке устройств
        /// </summary>
        private void HandleDeviceListButtons(int pressedButton)
        {
            // В списке устройств
            if (_devicesManager.GetDevices.Length > 0)
            {
                if (pressedButton == GlobalConstant.BUTTON_UP)
                {
                    // Предыдущее устройство
                    DeviceListIndex = (DeviceListIndex + _devicesManager.GetDevices.Length - 1) % _devicesManager.GetDevices.Length;
                }
                else if (pressedButton == GlobalConstant.BUTTON_DOWN)
                {
                    // Следующее устройство
                    DeviceListIndex = (DeviceListIndex + 1) % _devicesManager.GetDevices.Length;
                }
                else if (pressedButton == GlobalConstant.BUTTON_SELECT)
                {
                    // Выбор устройства - переход в меню устройства
                    CurrentMenu = MenuState.DEVICE_MENU;
                    DeviceMenuIndex = 0;
                }
                else if (pressedButton == GlobalConstant.BUTTON_LEFT)
                {
                    // Возврат на главный экран
                    CurrentMenu = MenuState.MAIN_SCREEN;
                }
            }
            else if (pressedButton == GlobalConstant.BUTTON_LEFT)
            {
                // Возврат на главный экран
                CurrentMenu = MenuState.MAIN_SCREEN;
            }
        }

        /// <summary>
        /// Обработка кнопок в меню устройства
        /// </summary>
        private void HandleDeviceMenuButtons(int pressedButton)
        {
            // В меню устройства
            if (pressedButton == GlobalConstant.BUTTON_UP)
            {
                // Предыдущий пункт меню
                DeviceMenuIndex = (DeviceMenuIndex + DeviceMenuOptionsCount - 1) % DeviceMenuOptionsCount;
            }
            else if (pressedButton == GlobalConstant.BUTTON_DOWN)
            {
                // Следующий пункт меню
                DeviceMenuIndex = (DeviceMenuIndex + 1) % DeviceMenuOptionsCount;
            }
            else if (pressedButton == GlobalConstant.BUTTON_SELECT)
            {
                // Выбор пункта меню
                switch (DeviceMenuIndex)
                {
                    case 0: // Температура
                        CurrentMenu = MenuState.EDIT_TEMPERATURE;
                        break;
                    case 1: // GPIO пины
                        CurrentMenu = MenuState.EDIT_GPIO;
                        GpioSelectionIndex = 0;
                        break;
                    case 2: // Вкл/Выкл
                        CurrentMenu = MenuState.EDIT_ENABLED;
                        break;
                    case 3: // Назад
                        CurrentMenu = MenuState.DEVICE_LIST;
                        break;
                }
            }
            else if (pressedButton == GlobalConstant.BUTTON_LEFT)
            {
                // Возврат к списку устройств
                CurrentMenu = MenuState.DEVICE_LIST;
            }
        }

        /// <summary>
        /// Обработка кнопок при редактировании температуры
        /// </summary>
        private void HandleTemperatureEditButtons(int pressedButton)
        {
            // Редактирование температуры
            DeviceData device = (DeviceData)_devicesManager.GetDevices[DeviceListIndex];

            if (pressedButton == GlobalConstant.BUTTON_UP)
            {
                // Увеличение температуры
                device.TargetTemperature += 0.5f;
            }
            else if (pressedButton == GlobalConstant.BUTTON_DOWN)
            {
                // Уменьшение температуры
                device.TargetTemperature -= 0.5f;
                if (device.TargetTemperature < 0)
                {
                    device.TargetTemperature = 0;
                }
            }
            else if (pressedButton == GlobalConstant.BUTTON_SELECT)
            {
                Debug.WriteLine("Нажата кнопка SELECT, сохраняем результаты");
                // Сохранение и возврат в меню устройства
                _filesManager.SaveClient();
                CurrentMenu = MenuState.DEVICE_MENU;
            }
            else if (pressedButton == GlobalConstant.BUTTON_LEFT)
            {
                // Отмена и возврат в меню устройства без сохранения
                CurrentMenu = MenuState.DEVICE_MENU;
            }
        }

        /// <summary>
        /// Обработка кнопок при редактировании GPIO
        /// </summary>
        private void HandleGpioEditButtons(int pressedButton)
        {
            // Редактирование GPIO
            if (_boardManager.GetGpio.Length > 0)
            {
                if (pressedButton == GlobalConstant.BUTTON_UP)
                {
                    // Предыдущий GPIO
                    GpioSelectionIndex = (GpioSelectionIndex + _boardManager.GetGpio.Length - 1) % _boardManager.GetGpio.Length;
                }
                else if (pressedButton == GlobalConstant.BUTTON_DOWN)
                {
                    // Следующий GPIO
                    GpioSelectionIndex = (GpioSelectionIndex + 1) % _boardManager.GetGpio.Length;
                }
                else if (pressedButton == GlobalConstant.BUTTON_SELECT)
                {
                    // Выбор/отмена выбора текущего GPIO
                    var device = _devicesManager.GetDevices[DeviceListIndex];
                    byte selectedGpio = _boardManager.GetGpio[GpioSelectionIndex].Pin;

                    device.RemoveOrAddGpioPins(selectedGpio);

                    Debug.WriteLine("Нажата кнопка SELECT при редактировании GPIO, сохраняем результаты");
                    // Сохраняем изменения
                    _filesManager.SaveClient();
                }
                else if (pressedButton == GlobalConstant.BUTTON_LEFT || pressedButton == GlobalConstant.BUTTON_RIGHT)
                {
                    // Возврат в меню устройства
                    CurrentMenu = MenuState.DEVICE_MENU;
                }
            }
            else if (pressedButton == GlobalConstant.BUTTON_LEFT || pressedButton == GlobalConstant.BUTTON_SELECT)
            {
                // Если нет доступных GPIO, возвращаемся в меню
                CurrentMenu = MenuState.DEVICE_MENU;
            }
        }

        /// <summary>
        /// Обработка кнопок при редактировании статуса устройства
        /// </summary>
        private void HandleEnabledEditButtons(int pressedButton)
        {
            // Включение/выключение устройства
            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];

            if (pressedButton == GlobalConstant.BUTTON_UP || pressedButton == GlobalConstant.BUTTON_DOWN)
            {
                // Переключение состояния
                device.Enabled = !device.Enabled;

                // Если устройство выключено, деактивируем обогрев
                if (!device.Enabled)
                {
                    device.HeatingActive = false;
                }
                Debug.WriteLine("Нажата кнопка BUTTON_UP при изменении доступности устройства, сохраняем результаты");
                // Сохраняем изменения
                _filesManager.SaveClient();
            }
            else if (pressedButton == GlobalConstant.BUTTON_SELECT || pressedButton == GlobalConstant.BUTTON_LEFT)
            {
                // Возврат в меню устройства
                CurrentMenu = MenuState.DEVICE_MENU;
            }
        }

        /// <summary>
        /// Обновление LCD дисплея
        /// </summary>
        public void UpdateLCD()
        {
            if (!IsInit) return;
            if (!NeedLcdUpdate)
            {
                return;
            }

            switch (CurrentMenu)
            {
                case MenuState.MAIN_SCREEN:
                    ShowMainScreen();
                    break;
                case MenuState.DEVICE_LIST:
                    ShowDeviceList();
                    break;
                case MenuState.DEVICE_MENU:
                    ShowDeviceMenu();
                    break;
                case MenuState.EDIT_TEMPERATURE:
                    ShowTemperatureEdit();
                    break;
                case MenuState.EDIT_GPIO:
                    ShowGpioEdit();
                    break;
                case MenuState.EDIT_ENABLED:
                    ShowEnabledEdit();
                    break;
            }

            NeedLcdUpdate = false;
        }

        /// <summary>
        /// Отображение главного экрана
        /// </summary>
        private void ShowMainScreen()
        {
            // Верхняя строка - статус системы
            // Отображаем статус WiFi
            if (_webServerManager.GetConnectState == nanoFramework.Networking.NetworkHelperStatus.NetworkIsReady)
            {
                DisplayText(_webServerManager.GetIpAddres);
            }
            else
            {
                DisplayText("WiFi: Disabled");
            }

            // Нижняя строка - информация о датчиках или прокручиваемый текст
            if (!string.IsNullOrEmpty(ScrollText))
            {
                // Вычисляем, какую часть текста показать
                int endPos = ScrollPosition + 16;
                if (endPos > ScrollText.Length)
                {
                    // Если достигли конца текста, показываем начало
                    string textPart = ScrollText.Substring(ScrollPosition);
                    textPart += ScrollText.Substring(0, endPos - ScrollText.Length);
                    DisplayText(textPart, 0, 1);
                }
                else
                {
                    DisplayText(ScrollText.Substring(ScrollPosition, 16), 0, 1);
                }
            }
            else
            {
                DisplayText("LONG RIGHT=SELECT", 0, 1);
            }
        }

        /// <summary>
        /// Отображение списка устройств
        /// </summary>
        private void ShowDeviceList()
        {
            DisplayText("Devices:");

            if (_devicesManager.GetDevices.Length > 0)
            {
                // Показываем текущее устройство с индикатором выбора
                DisplayText("> ", 0, 1);

                // Проверяем, не выходит ли индекс за пределы
                if (DeviceListIndex >= _devicesManager.GetDevices.Length)
                {
                    DeviceListIndex = 0;
                }

                // Отображаем имя устройства
                DeviceData device = _devicesManager.GetDevices[DeviceListIndex];
                string deviceName = device.Name;
                // Ограничиваем длину имени, чтобы оно поместилось на экране
                if (deviceName.Length > 14)
                {
                    deviceName = deviceName.Substring(0, 14);
                }
                DisplayText(deviceName, 2, 1, false);
            }
            else
            {
                DisplayText("There are no devices", 0, 1);
            }
        }

        /// <summary>
        /// Отображение меню устройства
        /// </summary>
        private void ShowDeviceMenu()
        {
            // Показываем имя устройства
            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];
            string deviceName = device.Name;
            if (deviceName.Length > 16)
            {
                deviceName = deviceName.Substring(0, 16);
            }
            DisplayText(deviceName);

            // Показываем текущий пункт меню
            DisplayText("> " + DeviceMenuOptions[DeviceMenuIndex], 0, 1);
        }

        /// <summary>
        /// Отображение редактирования температуры
        /// </summary>
        private void ShowTemperatureEdit()
        {
            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];
            DisplayText("Temp:");
            DisplayText(device.TargetTemperature.ToString("F1") + " C  [+/-]", 0, 1);
        }

        /// <summary>
        /// Отображение редактирования GPIO
        /// </summary>
        private void ShowGpioEdit()
        {
            // Проверяем, есть ли доступные GPIO
            if (_boardManager.GetGpio.Length == 0)
            {
                DisplayText("There are no available");
                DisplayText("GPIO pins", 0, 1);
                return;
            }

            // Проверяем индекс
            if (GpioSelectionIndex >= _boardManager.GetGpio.Length)
            {
                GpioSelectionIndex = 0;
            }

            DisplayText("GPIO pins:");

            DisplayText("PIN " + _boardManager.GetGpio[GpioSelectionIndex].Pin, 0, 1);

            // Проверяем, выбран ли этот GPIO для устройства
            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];
            bool isSelected = false;
            foreach (int gpio in device.GpioPins)
            {
                if (gpio == _boardManager.GetGpio[GpioSelectionIndex].Pin)
                {
                    isSelected = true;
                    break;
                }
            }
            DisplayText(isSelected ? "[X]" : "[ ]", 10, 1, false);
        }

        /// <summary>
        /// Отображение редактирования статуса устройства
        /// </summary>
        private void ShowEnabledEdit()
        {
            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];
            DisplayText("Device:");
            DisplayText((device.Enabled ? "Enabled" : "Disabled") + "[+/-]", 0, 1);
        }

        /// <summary>
        /// Прокрутка текста на главном экране
        /// </summary>
        private void ScrollMainScreenText()
        {
            long lastScrollTime = 0;
            long currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            // Прокручиваем текст каждые 500 мс
            if (CurrentMenu == MenuState.MAIN_SCREEN && currentTime - lastScrollTime > GlobalConstant.SCROLL_DELAY)
            {
                ScrollPosition = (ScrollPosition + 1) % ScrollText.Length;
                lastScrollTime = currentTime;

                // Обновляем экран только если мы на главном экране
                if (CurrentMenu == MenuState.MAIN_SCREEN)
                {
                    NeedLcdUpdate = true;
                }
            }
        }

        /// <summary>
        /// Периодическое обновление информации на экране
        /// </summary>
        public void UpdateLCDTask()
        {
            try
            {
                if (!IsInit) return;
                // Прокрутка текста на главном экране
                ScrollMainScreenText();

                // Обновление экрана при необходимости
                if (NeedLcdUpdate)
                {
                    UpdateLCD();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка периодического обновления экрана: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление данных на экране при изменении устройств
        /// </summary>
        public void RefreshLCDData()
        {
            if (!IsInit) return;
            if (_devicesManager.CheckOnlineDevice())
            {
                // Обновляем текст для прокрутки
                initScrollText();

                // Устанавливаем флаг для обновления экрана
                NeedLcdUpdate = true;
            }
        }

        /// <summary>
        /// Форматирование времени работы обогрева
        /// </summary>
        private string FormatHeatingTime(TimeSpan timeInMillis)
        {
            return timeInMillis.TotalDays.ToString("D2") + " day " + timeInMillis.TotalHours.ToString("D2") + ":" + timeInMillis.TotalMinutes.ToString("D2") + ":" + timeInMillis.TotalSeconds.ToString("D2");
        }

        /// <summary>
        /// Отображение статистики обогрева
        /// </summary>
        private void ShowHeatingStats()
        {
            if (_devicesManager.GetDevices.Length == 0 || DeviceListIndex >= _devicesManager.GetDevices.Length)
            {
                DisplayText("There are no devices");
                return;
            }

            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];

            // Показываем имя устройства
            string deviceName = device.Name;
            if (deviceName.Length > 10)
            {
                deviceName = deviceName.Substring(0, 10);
            }
            DisplayText(deviceName);

            // Показываем статус обогрева
            DisplayText(device.HeatingActive ? "ON" : "OFF", 11, 0, false);

            // Показываем общее время работы
            DisplayText("Time: " + FormatHeatingTime(device.TotalHeatingTime), 0, 1);
        }

        /// <summary>
        /// Отображение информации о GPIO
        /// </summary>
        private void ShowGpioInfo()
        {
            if (_devicesManager.GetDevices.Length == 0 || DeviceListIndex >= _devicesManager.GetDevices.Length)
            {
                DisplayText("There are no devices");
                return;
            }

            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];

            // Показываем имя устройства
            string deviceName = device.Name;
            if (deviceName.Length > 16)
            {
                deviceName = deviceName.Substring(0, 16);
            }
            DisplayText(deviceName);

            // Показываем GPIO пины
            if (device.GpioPins.Length == 0)
            {
                DisplayText("GPIO: Not selected", 0, 1);
            }
            else
            {
                StringBuilder gpioList = new StringBuilder();
                for (int i = 0; i < device.GpioPins.Length && i < 4; i++)
                {
                    if (i > 0)
                    {
                        gpioList.Append(",");
                    }
                    gpioList.Append(device.GpioPins[i]);
                }

                if (device.GpioPins.Length > 4)
                {
                    gpioList.Append("...");
                }
                DisplayText("GPIO: " + gpioList.ToString(), 0, 1);
            }
        }

        /// <summary>
        /// Отображение информации о температуре
        /// </summary>
        private void ShowTemperatureInfo()
        {
            _lcd.Clear();

            if (_devicesManager.GetDevices.Length == 0 || DeviceListIndex >= _devicesManager.GetDevices.Length)
            {
                DisplayText("There are no devices");
                return;
            }

            DeviceData device = _devicesManager.GetDevices[DeviceListIndex];

            // Показываем имя устройства
            string deviceName = device.Name;
            if (deviceName.Length > 10)
            {
                deviceName = deviceName.Substring(0, 10);
            }
            DisplayText(deviceName);

            // Показываем текущую температуру
            DisplayText(device.CurrentTemperature.ToString("F1") + "C", 11, 0, false);

            // Показываем целевую температуру и статус
            DisplayText("Target: " + device.TargetTemperature.ToString("F1") + "C ", 0, 1);

            // Статус обогрева  
            if (device.Enabled)
            {
                DisplayText(device.HeatingActive ? "Heat" : "ОК", 12, 1, false);
            }
            else
            {
                DisplayText("OFF", 12, 1, false);
            }
        }

        /// <summary>
        /// Циклическое переключение информационных экранов
        /// </summary>
        private void CycleInfoScreens()
        {
            int infoScreenIndex = 0;
            long lastScreenChangeTime = 0;
            long currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            // Меняем экран каждые 5 секунд, только если мы на главном экране
            if (CurrentMenu == MenuState.MAIN_SCREEN && currentTime - lastScreenChangeTime > 5000)
            {
                infoScreenIndex = (infoScreenIndex + 1) % 3;
                lastScreenChangeTime = currentTime;

                switch (infoScreenIndex)
                {
                    case 0:
                        ShowMainScreen();
                        break;
                    case 1:
                        ShowTemperatureInfo();
                        break;
                    case 2:
                        ShowHeatingStats();
                        break;
                }
            }
        }
    }
}
