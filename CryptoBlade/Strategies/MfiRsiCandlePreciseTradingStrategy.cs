﻿using Bybit.Net.Interfaces.Clients;
using CryptoBlade.Helpers;
using CryptoBlade.Models;
using CryptoBlade.Strategies.Common;
using CryptoBlade.Strategies.Wallet;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;
using System.Threading;

namespace CryptoBlade.Strategies
{
    public class MfiRsiCandlePreciseTradingStrategy : TradingStrategyBase
    {
        private readonly IOptions<MfiRsiCandlePreciseTradingStrategyOptions> m_options;
        private const int c_candlePeriod = 50;

        public MfiRsiCandlePreciseTradingStrategy(IOptions<MfiRsiCandlePreciseTradingStrategyOptions> options,
            string symbol, IWalletManager walletManager, IBybitRestClient restClient)
            : base(options, symbol, GetRequiredTimeFrames(), walletManager, restClient)
        {
            m_options = options;
        }

        private static TimeFrameWindow[] GetRequiredTimeFrames()
        {
            return new[]
            {
                new TimeFrameWindow(TimeFrame.OneMinute, c_candlePeriod, true),
                new TimeFrameWindow(TimeFrame.FiveMinutes, c_candlePeriod, false),
            };
        }

        public override string Name
        {
            get { return "MfiRsiCandlePrecise"; }
        }

        protected override decimal WalletExposureLong
        {
            get { return m_options.Value.WalletExposureLong; }
        }

        protected override decimal WalletExposureShort
        {
            get { return m_options.Value.WalletExposureShort; }
        }

        protected override int DcaOrdersCount
        {
            get { return m_options.Value.DcaOrdersCount; }
        }

        protected override bool ForceMinQty
        {
            get { return m_options.Value.ForceMinQty; }
        }

        protected override Task<SignalEvaluation> EvaluateSignalsInnerAsync(CancellationToken cancel)
        {
            var quotes = QuoteQueues[TimeFrame.OneMinute].GetQuotes();
            List<StrategyIndicator> indicators = new();
            var lastQuote = quotes.LastOrDefault();
            bool hasBuySignal = false;
            bool hasSellSignal = false;
            if (lastQuote != null)
            {
                var spread5Min = TradeSignalHelpers.Get5MinSpread(quotes);
                var mfi = quotes.GetMfi();
                var kc = quotes.GetKeltner();
                var lastkc = kc.LastOrDefault();
                var lastMfi = mfi.LastOrDefault();
                var rsi = quotes.GetRsi();
                var lastRsi = rsi.LastOrDefault();
                var mfiRsiBuy = TradeSignalHelpers.IsMfiRsiBuy(lastMfi, lastRsi, lastQuote);
                var mfiRsiSell = TradeSignalHelpers.IsMfiRsiSell(lastMfi, lastRsi, lastQuote);
                var KcBuy = TradeSignalHelpers.IsKcBuy(lastkc, lastQuote);
                var KcSell = TradeSignalHelpers.IsKcSell(lastkc, lastQuote);
                bool hasMinSpread = spread5Min >= m_options.Value.MinimumPriceDistance;
                var volume = TradeSignalHelpers.VolumeInQuoteCurrency(lastQuote);
                bool hasMinVolume = volume > m_options.Value.MinimumVolume;
                hasBuySignal = mfiRsiBuy && KcBuy && hasMinSpread && hasMinVolume;
                hasSellSignal = mfiRsiSell && KcSell && hasMinSpread && hasMinVolume;

                indicators.Add(new StrategyIndicator(nameof(IndicatorType.Volume1Min), volume));
                indicators.Add(new StrategyIndicator(nameof(IndicatorType.MainTimeFrameVolume), volume));
                indicators.Add(new StrategyIndicator(nameof(IndicatorType.Spread5Min), spread5Min));
                if (lastMfi?.Mfi != null)
                    indicators.Add(new StrategyIndicator(nameof(IndicatorType.Mfi1Min), lastMfi.Mfi.Value));
                if (lastRsi?.Rsi != null)
                    indicators.Add(new StrategyIndicator(nameof(IndicatorType.Rsi1Min), lastRsi.Rsi.Value));
                if (lastkc?.LowerBand != null)
                    indicators.Add(new StrategyIndicator(nameof(IndicatorType.KCLower), lastkc.LowerBand.Value));
                if (lastkc?.UpperBand != null)
                    indicators.Add(new StrategyIndicator(nameof(IndicatorType.KCUpper), lastkc.UpperBand.Value));
            }

            return Task.FromResult(new SignalEvaluation(hasBuySignal, hasSellSignal, hasBuySignal, hasSellSignal, indicators.ToArray()));
        }
    }
}