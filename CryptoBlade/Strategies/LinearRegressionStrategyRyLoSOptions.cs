namespace CryptoBlade.Strategies
{
    public class LinearRegressionStrategyRyLoSOptions : TradingStrategyBaseOptions
    {
        public decimal MinimumVolume { get; set; }

        public decimal MinimumPriceDistance { get; set; }

        public int ChannelLength { get; set; } = 100;

        public double StandardDeviation { get; set; } = 2.3;

        public int KCLength { get; set; } = 20;

        public double KCMultiplier { get; set; } = 2.3;

    }
}