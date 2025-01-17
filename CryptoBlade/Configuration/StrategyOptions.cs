﻿using CryptoBlade.Strategies;

namespace CryptoBlade.Configuration
{
    public class StrategyOptions
    {
        public AutoHedge AutoHedge { get; set; } = new AutoHedge();
        public LinearRegression LinearRegression { get; set; } = new LinearRegression();
        public LinearRegressionRyLoS LinearRegressionRyLoS { get; set; } = new LinearRegressionRyLoS();
        public Tartaglia Tartaglia { get; set; } = new Tartaglia();
        public Mona Mona { get; set; } = new Mona();
        public MfiRsiEriTrend MfiRsiEriTrend { get; set; } = new MfiRsiEriTrend();
        public RecursiveStrategyOptions Recursive { get; set; } = new RecursiveStrategyOptions();
        public Qiqi Qiqi { get; set; } = new Qiqi();
    }
}