using CryptoBlade.Exchanges;
using CryptoBlade.Helpers;
using CryptoBlade.Models;
using CryptoBlade.Strategies.Common;
using CryptoBlade.Strategies.Wallet;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace CryptoBlade.Strategies
{
    public class LinearRegressionStrategyRyLoS : TradingStrategyBase
    {
        private readonly IOptions<LinearRegressionStrategyRyLoSOptions> m_options;

        public LinearRegressionStrategyRyLoS(IOptions<LinearRegressionStrategyRyLoSOptions> options, 
            string symbol,
            IWalletManager walletManager,
            ICbFuturesRestClient cbFuturesRestClient) 
            : base(options, symbol, GetRequiredTimeFrames(options.Value.ChannelLength), walletManager, cbFuturesRestClient)
        {
            m_options = options;
        }

        private static TimeFrameWindow[] GetRequiredTimeFrames(int channelLength)
        {
            return new[]
            {
                new TimeFrameWindow(TimeFrame.OneMinute, channelLength, true),
                new TimeFrameWindow(TimeFrame.FiveMinutes, channelLength, false),
            };
        }

        public override string Name => "LinearRegressionRyLoS";
        protected override decimal WalletExposureLong => m_options.Value.WalletExposureLong;
        protected override decimal WalletExposureShort => m_options.Value.WalletExposureShort;
        protected override int DcaOrdersCount => m_options.Value.DcaOrdersCount;
        protected override bool ForceMinQty => m_options.Value.ForceMinQty;

        protected override Task<SignalEvaluation> EvaluateSignalsInnerAsync(CancellationToken cancel)
        {
            var quotes1min = QuoteQueues[TimeFrame.OneMinute].GetQuotes();
            var quotes5min = QuoteQueues[TimeFrame.FiveMinutes].GetQuotes();
            List<StrategyIndicator> indicators = new();
            var lastQuote1min = quotes1min.LastOrDefault();
            var lastQuote5min = quotes5min.LastOrDefault();
            var ticker = Ticker;
            bool hasBuySignal = false;
            bool hasSellSignal = false;
            bool hasBuyExtraSignal = false;
            bool hasSellExtraSignal = false;

            if (lastQuote1min != null && lastQuote5min != null && ticker != null)
            {
                bool canBeTraded = (lastQuote1min.Date - SymbolInfo.LaunchTime).TotalDays > m_options.Value.InitialUntradableDays;
                var spread5Min = TradeSignalHelpers.Get5MinSpread(quotes1min); //giusto che lo faccia su 1min
                var volume = TradeSignalHelpers.VolumeInQuoteCurrency(lastQuote1min);
                bool hasMinSpread = spread5Min > m_options.Value.MinimumPriceDistance;
                bool hasMinVolume = volume >= m_options.Value.MinimumVolume;
                bool belowLinRegChannel1min = false;
                bool aboveLinRegChannel1min = false;
                bool belowLinRegChannel5min = false;
                bool aboveLinRegChannel5min = false;
                bool belowKChannel1min = false;
                bool aboveKChannel1min = false;
                bool belowKChannel5min = false;
                bool aboveKChannel5min = false;
                bool hasBasicConditions = canBeTraded && hasMinSpread && hasMinVolume;
                // hasBasicConditions = true; // DEBUG
                // double close_ha = 0;
                if (hasBasicConditions)
                {
                    var stdDevChn1min = quotes1min.Use(CandlePart.OC2).GetStdDevChannels(m_options.Value.ChannelLength, m_options.Value.StandardDeviation).LastOrDefault();
                    var stdDevChn5min = quotes5min.Use(CandlePart.OC2).GetStdDevChannels(m_options.Value.ChannelLength, m_options.Value.StandardDeviation).LastOrDefault();
                    if(stdDevChn1min!=null && stdDevChn5min!=null)
                    {
                        belowLinRegChannel1min = (double)ticker.LastPrice < stdDevChn1min.LowerChannel;
                        belowLinRegChannel5min = (double)ticker.LastPrice < stdDevChn5min.LowerChannel;
                        aboveLinRegChannel1min = (double)ticker.LastPrice > stdDevChn1min.UpperChannel;
                        aboveLinRegChannel5min = (double)ticker.LastPrice > stdDevChn5min.UpperChannel;
                    }
                    var kc1min = quotes1min.GetKeltner(m_options.Value.KCLength, m_options.Value.KCMultiplier).LastOrDefault();
                    var kc5min = quotes5min.GetKeltner(m_options.Value.KCLength, m_options.Value.KCMultiplier).LastOrDefault();
                    if (kc1min!=null && kc5min!=null)
                    {
                        belowKChannel1min = (double)ticker.LastPrice < kc1min.LowerBand;
                        aboveKChannel1min = (double)ticker.LastPrice > kc1min.UpperBand;
                        belowKChannel5min = (double)ticker.LastPrice < kc5min.LowerBand;
                        aboveKChannel5min = (double)ticker.LastPrice > kc5min.UpperBand;
                    }
                    
                    //HARSI ************************************************************
                    // var ha = quotes1min.GetHeikinAshi();
                    // double[] zRsi = quotes1min.GetRsi(14).Select(result => result.Rsi.HasValue ? result.Rsi.Value - 50 : 0).ToArray();
                    // double[] open = new double[zRsi.Length];
                    // double[] high = new double[zRsi.Length];
                    // double[] low = new double[zRsi.Length];
                    // double[] closeHa = new double[zRsi.Length];
                    // open[0] = zRsi[0];
                    // for (int i = 1; i < zRsi.Length; i++)
                    // {
                    //     open[i] = (open[i - 1] + zRsi[i]) / 2;
                    // }
                    // high[0] = zRsi[0];
                    // low[0] = zRsi[0];
                    // for (int i = 1; i < zRsi.Length; i++)
                    // {
                    //     high[i] = Math.Max(zRsi[i], Math.Max(open[i], closeHa[i - 1]));
                    //     low[i] = Math.Min(zRsi[i], Math.Min(open[i], closeHa[i - 1]));
                    // }
                    // closeHa[0] = (open[0] + high[0] + low[0] + zRsi[0]) / 4;
                    // for (int i = 1; i < zRsi.Length; i++)
                    // {
                    //     closeHa[i] = (open[i] + high[i] + low[i] + closeHa[i - 1]) / 4;
                    // }
                    // close_ha = closeHa[closeHa.Length - 1];
                    //END HARSI *********************************************************
                }

                Position? longPosition = LongPosition;
                Position? shortPosition = ShortPosition;
                
                hasBuySignal = hasMinVolume
                               && belowLinRegChannel1min
                               && belowLinRegChannel5min
                               && belowKChannel5min
                               && hasMinSpread
                               && canBeTraded;
                               // && (close_ha < -20);

                hasBuyExtraSignal = hasMinVolume
                               && longPosition != null
                               && ticker.BestBidPrice < longPosition.AveragePrice
                               && belowKChannel1min
                               && hasMinSpread
                               && canBeTraded
                               && (belowLinRegChannel1min || (belowKChannel1min && (ticker.BestBidPrice < longPosition.AveragePrice/1.07m)));
                               // && (close_ha < -20);
                               
                               

                hasSellSignal = hasMinVolume
                                && aboveLinRegChannel1min
                                && aboveLinRegChannel5min
                                && aboveKChannel5min
                                && hasMinSpread
                                && canBeTraded;
                                // && (close_ha > 20);

                hasSellExtraSignal = hasMinVolume
                                && shortPosition != null
                                && ticker.BestAskPrice > shortPosition.AveragePrice
                                && aboveKChannel1min
                                && hasMinSpread
                                && canBeTraded
                                && (aboveLinRegChannel1min || (aboveKChannel1min && (ticker.BestAskPrice > shortPosition.AveragePrice*1.07m)));
                                //&& (close_ha > 20);
                                
                                

                indicators.Add(new StrategyIndicator(nameof(IndicatorType.Volume1Min), volume));
                indicators.Add(new StrategyIndicator(nameof(IndicatorType.MainTimeFrameVolume), volume));
                indicators.Add(new StrategyIndicator(nameof(IndicatorType.Spread5Min), spread5Min));
            }

            return Task.FromResult(new SignalEvaluation(hasBuySignal, hasSellSignal, hasBuyExtraSignal, hasSellExtraSignal, indicators.ToArray()));
        }

        private static double StandardDeviation(double[] data)
        {
            double mean = data.Sum() / data.Length;
            double sumOfSquares = data.Select(x => (x - mean) * (x - mean)).Sum();
            return Math.Sqrt(sumOfSquares / data.Length);
        }
    }
}