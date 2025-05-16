using System;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace esp32_s3.Models
{
    /// <summary>
    /// Класс для работы с LCD дисплеем через GPIO
    /// </summary>
    public class LiquidCrystal
    {
        // Пины для подключения LCD
        private readonly GpioPin _rsPin;
        private readonly GpioPin _enablePin;
        private readonly GpioPin _d4Pin;
        private readonly GpioPin _d5Pin;
        private readonly GpioPin _d6Pin;
        private readonly GpioPin _d7Pin;

        // Размеры дисплея
        private int _displayRows;
        private int _displayCols;

        // Контроллер GPIO
        private readonly GpioController _gpio;

        // Константы для команд LCD
        private const byte LCD_CLEARDISPLAY = 0x01;
        private const byte LCD_RETURNHOME = 0x02;
        private const byte LCD_ENTRYMODESET = 0x04;
        private const byte LCD_DISPLAYCONTROL = 0x08;
        private const byte LCD_CURSORSHIFT = 0x10;
        private const byte LCD_FUNCTIONSET = 0x20;
        private const byte LCD_SETCGRAMADDR = 0x40;
        private const byte LCD_SETDDRAMADDR = 0x80;

        // Флаги для дисплея
        private const byte LCD_DISPLAYON = 0x04;
        private const byte LCD_DISPLAYOFF = 0x00;
        private const byte LCD_CURSORON = 0x02;
        private const byte LCD_CURSOROFF = 0x00;
        private const byte LCD_BLINKON = 0x01;
        private const byte LCD_BLINKOFF = 0x00;

        // Флаги для режима ввода
        private const byte LCD_ENTRYRIGHT = 0x00;
        private const byte LCD_ENTRYLEFT = 0x02;
        private const byte LCD_ENTRYSHIFTINCREMENT = 0x01;
        private const byte LCD_ENTRYSHIFTDECREMENT = 0x00;

        // Флаги для функций
        private const byte LCD_8BITMODE = 0x10;
        private const byte LCD_4BITMODE = 0x00;
        private const byte LCD_2LINE = 0x08;
        private const byte LCD_1LINE = 0x00;
        private const byte LCD_5x10DOTS = 0x04;
        private const byte LCD_5x8DOTS = 0x00;

        // Текущие настройки
        private byte _displayControl;
        private byte _displayMode;

        /// <summary>
        /// Конструктор для 4-битного режима
        /// </summary>
        public LiquidCrystal(int rs, int enable, int d4, int d5, int d6, int d7)
        {
            _gpio = new GpioController();

            // Инициализация пинов
            _rsPin = _gpio.OpenPin(rs, PinMode.Output);
            _enablePin = _gpio.OpenPin(enable, PinMode.Output);
            _d4Pin = _gpio.OpenPin(d4, PinMode.Output);
            _d5Pin = _gpio.OpenPin(d5, PinMode.Output);
            _d6Pin = _gpio.OpenPin(d6, PinMode.Output);
            _d7Pin = _gpio.OpenPin(d7, PinMode.Output);
        }

        /// <summary>
        /// Инициализация дисплея
        /// </summary>
        public void Begin(int cols, int rows)
        {
            _displayRows = rows;
            _displayCols = cols;

            // Инициализация в 4-битном режиме
            Thread.Sleep(50); // Ждем >40ms после включения питания

            // Устанавливаем RS и E в LOW
            _rsPin.Write(PinValue.Low);
            _enablePin.Write(PinValue.Low);

            // Последовательность инициализации для 4-битного режима
            Write4Bits(0x03);
            Thread.Sleep(5); // Ждем >4.1ms

            Write4Bits(0x03);
            Thread.Sleep(5); // Ждем >100us

            Write4Bits(0x03);
            Thread.Sleep(1);

            Write4Bits(0x02); // Устанавливаем 4-битный режим

            // Настройка количества строк и размера шрифта
            Command(LCD_FUNCTIONSET | LCD_4BITMODE | LCD_2LINE | LCD_5x8DOTS);

            // Включаем дисплей с выключенным курсором и миганием
            _displayControl = LCD_DISPLAYON | LCD_CURSOROFF | LCD_BLINKOFF;
            Display();

            // Очищаем дисплей
            Clear();

            // Устанавливаем направление ввода слева направо
            _displayMode = LCD_ENTRYLEFT | LCD_ENTRYSHIFTDECREMENT;
            Command((byte)(LCD_ENTRYMODESET | _displayMode));
        }

        /// <summary>
        /// Очистка дисплея
        /// </summary>
        public void Clear()
        {
            Command(LCD_CLEARDISPLAY);
            Thread.Sleep(2); // Ждем выполнения команды
        }

        /// <summary>
        /// Возврат курсора в начальное положение
        /// </summary>
        public void Home()
        {
            Command(LCD_RETURNHOME);
            Thread.Sleep(2); // Ждем выполнения команды
        }

        /// <summary>
        /// Установка курсора в указанную позицию
        /// </summary>
        public void SetCursor(int col, int row)
        {
            int[] rowOffsets = { 0x00, 0x40, 0x14, 0x54 };
            if (row >= _displayRows)
            {
                row = _displayRows - 1;
            }
            Command((byte)(LCD_SETDDRAMADDR | (col + rowOffsets[row])));
        }

        /// <summary>
        /// Включение дисплея без изменения содержимого
        /// </summary>
        public void Display()
        {
            _displayControl |= LCD_DISPLAYON;
            Command((byte)(LCD_DISPLAYCONTROL | _displayControl));
        }

        /// <summary>
        /// Выключение дисплея без изменения содержимого
        /// </summary>
        public void NoDisplay()
        {
            var r = (int)_displayControl;
            r &= ~LCD_DISPLAYON;
            Command((byte)(LCD_DISPLAYCONTROL | (byte)r));
        }

        /// <summary>
        /// Вывод текста на дисплей
        /// </summary>
        public void Print(string text)
        {
            foreach (char c in text)
            {
                Write(c);
            }
        }

        /// <summary>
        /// Отправка команды на дисплей
        /// </summary>
        private void Command(byte value)
        {
            Send(value, PinValue.Low);
        }

        /// <summary>
        /// Запись символа на дисплей
        /// </summary>
        private void Write(char value)
        {
            Send((byte)value, PinValue.High);
        }

        /// <summary>
        /// Отправка данных на дисплей
        /// </summary>
        private void Send(byte value, PinValue mode)
        {
            _rsPin.Write(mode);

            // 4-битный режим
            Write4Bits((byte)(value >> 4));
            Write4Bits((byte)(value & 0x0F));
        }

        /// <summary>
        /// Запись 4 бит данных
        /// </summary>
        private void Write4Bits(byte value)
        {
            _d4Pin.Write((PinValue)((value >> 0) & 0x01));
            _d5Pin.Write((PinValue)((value >> 1) & 0x01));
            _d6Pin.Write((PinValue)((value >> 2) & 0x01));
            _d7Pin.Write((PinValue)((value >> 3) & 0x01));

            PulseEnable();
        }

        /// <summary>
        /// Импульс на пине Enable для фиксации данных
        /// </summary>
        private void PulseEnable()
        {
            _enablePin.Write(PinValue.Low);
            Thread.Sleep(1);
            _enablePin.Write(PinValue.High);
            Thread.Sleep(1);
            _enablePin.Write(PinValue.Low);
            Thread.Sleep(1);
        }
    }

}
