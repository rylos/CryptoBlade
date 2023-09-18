namespace CryptoBlade.Configuration
{
    public class LinearRegressionRyLoS
    {
        public int ChannelLength { get; set; } = 100;

        public double StandardDeviation { get; set; } = 2.3;

        public int KCLength { get; set; } = 20;

        public double KCMultiplier { get; set; } = 2.3;
    }
}