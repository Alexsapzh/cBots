using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class GuppyRibbon : Indicator
    {

        [Parameter("Data Source")]
        public DataSeries Price { get; set; }
        [Output("RibEMA13", Color = Colors.Red)]
        public IndicatorDataSeries EMA13 { get; set; }
        [Output("RibEMA21", Color = Colors.Red)]
        public IndicatorDataSeries EMA12 { get; set; }
        [Output("RibEMA11", Color = Colors.Orange)]
        public IndicatorDataSeries EMA11 { get; set; }
        [Output("RibEMA10", Color = Colors.Orange)]
        public IndicatorDataSeries EMA10 { get; set; }
        [Output("RibEMA9", Color = Colors.Yellow)]
        public IndicatorDataSeries EMA9 { get; set; }
        [Output("RibEMA8", Color = Colors.LightYellow)]
        public IndicatorDataSeries EMA8 { get; set; }
        [Output("RibEMA7", Color = Colors.Yellow)]
        public IndicatorDataSeries EMA7 { get; set; }
        [Output("RibEMA6", Color = Colors.Gold)]
        public IndicatorDataSeries EMA6 { get; set; }
        [Output("RibEMA5", Color = Colors.Gold)]
        public IndicatorDataSeries EMA5 { get; set; }


        [Parameter("Exp Fast Periods 13", DefaultValue = 13)]
        public int FastPeriods13 { get; set; }
        [Parameter("Exp Fast Periods 12", DefaultValue = 12)]
        public int FastPeriods12 { get; set; }
        [Parameter("Exp Fast Periods 11", DefaultValue = 11)]
        public int FastPeriods11 { get; set; }
        [Parameter("Exp Fast Periods 10", DefaultValue = 10)]
        public int FastPeriods10 { get; set; }
        [Parameter("Exp Fast Periods 9", DefaultValue = 9)]
        public int FastPeriods9 { get; set; }
        [Parameter("Exp Fast Periods 8", DefaultValue = 8)]
        public int FastPeriods8 { get; set; }
        [Parameter("Exp Fast Periods 7", DefaultValue = 7)]
        public int FastPeriods7 { get; set; }
        [Parameter("Exp Fast Periods 6", DefaultValue = 6)]
        public int FastPeriods6 { get; set; }
        [Parameter("Exp Fast Periods 5", DefaultValue = 5)]
        public int FastPeriods5 { get; set; }

        private ExponentialMovingAverage _emaFast13;
        private ExponentialMovingAverage _emaFast12;
        private ExponentialMovingAverage _emaFast11;
        private ExponentialMovingAverage _emaFast10;
        private ExponentialMovingAverage _emaFast9;
        private ExponentialMovingAverage _emaFast8;
        private ExponentialMovingAverage _emaFast7;
        private ExponentialMovingAverage _emaFast6;
        private ExponentialMovingAverage _emaFast5;


        protected override void Initialize()
        {
            _emaFast13 = Indicators.ExponentialMovingAverage(Price, 13);
            _emaFast12 = Indicators.ExponentialMovingAverage(Price, 12);
            _emaFast11 = Indicators.ExponentialMovingAverage(Price, 11);
            _emaFast10 = Indicators.ExponentialMovingAverage(Price, 10);
            _emaFast9 = Indicators.ExponentialMovingAverage(Price, 9);
            _emaFast8 = Indicators.ExponentialMovingAverage(Price, 8);
            _emaFast7 = Indicators.ExponentialMovingAverage(Price, 7);
            _emaFast6 = Indicators.ExponentialMovingAverage(Price, 6);
            _emaFast5 = Indicators.ExponentialMovingAverage(Price, 5);

        }

        public override void Calculate(int index)
        {

            EMA13[index] = _emaFast13.Result[index];
            EMA12[index] = _emaFast12.Result[index];
            EMA11[index] = _emaFast11.Result[index];
            EMA10[index] = _emaFast10.Result[index];
            EMA9[index] = _emaFast9.Result[index];
            EMA8[index] = _emaFast8.Result[index];
            EMA7[index] = _emaFast7.Result[index];
            EMA6[index] = _emaFast7.Result[index];
            EMA5[index] = _emaFast5.Result[index];




        }
    }
}
