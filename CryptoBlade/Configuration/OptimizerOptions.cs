﻿namespace CryptoBlade.Configuration
{
    public class OptimizerOptions
    {
        public GeneticAlgorithmOptions GeneticAlgorithm { get; set; } = new GeneticAlgorithmOptions();
        public TartagliaOptimizerOptions Tartaglia { get; set; } = new TartagliaOptimizerOptions();
        public TradingBotOptimizerOptions TradingBot { get; set; } = new TradingBotOptimizerOptions();
        public string SessionId { get; set; } = "Session01";
        public bool EnableHistoricalDataCaching { get; set; } = true;
        public int ParallelTasks { get; set; } = 10;
    }
}