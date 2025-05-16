
namespace esp32_s3.Models
{
    public class ServerInfo
    {
        public ServerInfo(uint totalSpi, uint freeSpi, uint largestFreeBlockSpi, uint totalInternal, uint freeInternal, uint largestFreeBlockInternal, string workTime, int boardTemperature)
        {
            TotalSpi = totalSpi;
            FreeSpi = freeSpi;
            LargestFreeBlockSpi = largestFreeBlockSpi;
            TotalInternal = totalInternal;
            FreeInternal = freeInternal;
            LargestFreeBlockInternal = largestFreeBlockInternal;
            WorkTime = workTime;
            BoardTemperature = boardTemperature;
        }

        public uint TotalSpi { get; set; }
        public uint FreeSpi { get; set; }
        public uint LargestFreeBlockSpi { get; set; }
        public uint TotalInternal { get; set; }
        public uint FreeInternal { get; set; }
        public uint LargestFreeBlockInternal { get; set; }
        public string WorkTime { get; set; }
        public int BoardTemperature { get; set; }
    }
}
