namespace esp32_s3.Interfaces
{
    public interface ILcdKeyShield
    {
        void DisplayText(string text, int column = 0, int row = 0, bool clearLine = true, bool center = false);
        void HandleButtons();
        void InitLCD();
        void RefreshLCDData();
        void UpdateLCD();
        void UpdateLCDTask();
        void initScrollText();
    }
}