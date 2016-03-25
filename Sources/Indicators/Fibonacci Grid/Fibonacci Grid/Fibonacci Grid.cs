using System;
using System.Linq;
using System.Reflection;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;


namespace cAlgo
{
    [Indicator(IsOverlay = true, ScalePrecision = 5, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FibonacciGrid : Indicator
    {

        [Parameter("EMA HFT Period", DefaultValue = 20, MinValue = 1, MaxValue = 250, Step = 1)]
        public int EMAPeriod { get; set; }

        [Output("EMA 1m", Color = Colors.Orange)]
        public IndicatorDataSeries EMA1m { get; set; }

        [Output("EMA 5m", Color = Colors.Red)]
        public IndicatorDataSeries EMA5m { get; set; }

        [Output("EMA 15m", Color = Colors.Blue)]
        public IndicatorDataSeries EMA15m { get; set; }

        [Output("EMA 60m", Color = Colors.Purple)]
        public IndicatorDataSeries EMA60m { get; set; }

        //[Parameter(DefaultValue = 55)]
        //public int PeriodEma { get; set; }

        [Parameter(DefaultValue = 21)]
        public int PeriodAtr { get; set; }


        [Output("1m Upper Band 1", PlotType = PlotType.Line, Color = Colors.Yellow)]
        public IndicatorDataSeries UpperBand1 { get; set; }

        [Output("1m Upper Band 2", PlotType = PlotType.Line, Color = Colors.Yellow)]
        public IndicatorDataSeries UpperBand2 { get; set; }

        [Output("1m Upper Band 3", PlotType = PlotType.Line, Color = Colors.Yellow)]
        public IndicatorDataSeries UpperBand3 { get; set; }

        [Output("1m Upper Band 4", PlotType = PlotType.Line, Color = Colors.Yellow)]
        public IndicatorDataSeries UpperBand4 { get; set; }

        [Output("1m Lower Band 1", PlotType = PlotType.Line, Color = Colors.White)]
        public IndicatorDataSeries LowerBand1 { get; set; }

        [Output("1m Lower Band 2", PlotType = PlotType.Line, Color = Colors.White)]
        public IndicatorDataSeries LowerBand2 { get; set; }

        [Output("1m Lower Band 3", PlotType = PlotType.Line, Color = Colors.White)]
        public IndicatorDataSeries LowerBand3 { get; set; }

        [Output("1m Lower Band 4", PlotType = PlotType.Line, Color = Colors.White)]
        public IndicatorDataSeries LowerBand4 { get; set; }

        [Output("5m Upper Band 1", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries UpperBand5 { get; set; }

        [Output("5m Upper Band 2", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries UpperBand6 { get; set; }

        [Output("5m Upper Band 3", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries UpperBand7 { get; set; }

        [Output("5m Upper Band 4", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries UpperBand8 { get; set; }

        [Output("5m Lower Band 1", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries LowerBand5 { get; set; }

        [Output("5m Lower Band 2", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries LowerBand6 { get; set; }

        [Output("5m Lower Band 3", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries LowerBand7 { get; set; }

        [Output("5m Lower Band 4", PlotType = PlotType.Line, Color = Colors.DarkGreen)]
        public IndicatorDataSeries LowerBand8 { get; set; }


        [Output("15m Upper Band 1", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries UpperBand9 { get; set; }

        [Output("15m Upper Band 2", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries UpperBand10 { get; set; }

        [Output("15m Upper Band 3", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries UpperBand11 { get; set; }

        [Output("15m Upper Band 4", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries UpperBand12 { get; set; }

        [Output("15m Lower Band 1", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries LowerBand9 { get; set; }

        [Output("15m Lower Band 2", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries LowerBand10 { get; set; }

        [Output("15m Lower Band 3", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries LowerBand11 { get; set; }

        [Output("15m Lower Band 4", PlotType = PlotType.Line, Color = Colors.DarkCyan)]
        public IndicatorDataSeries LowerBand12 { get; set; }


        [Output("60m Upper Band 1", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries UpperBand13 { get; set; }

        [Output("60m Upper Band 2", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries UpperBand14 { get; set; }

        [Output("60m Upper Band 3", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries UpperBand15 { get; set; }

        [Output("60m Upper Band 4", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries UpperBand16 { get; set; }

        [Output("60m Lower Band 1", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries LowerBand13 { get; set; }

        [Output("60m Lower Band 2", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries LowerBand14 { get; set; }

        [Output("60m Lower Band 3", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries LowerBand15 { get; set; }

        [Output("60m Lower Band 4", PlotType = PlotType.Line, Color = Colors.DarkMagenta)]
        public IndicatorDataSeries LowerBand16 { get; set; }

        private SignalFibGrid _emasignal;
        private ATRSignals _atr;

        //private ExponentialMovingAverage _emaFast;
        //private ExponentialMovingAverage _exponentialMovingAverage;

        protected override void Initialize()
        {

            _emasignal = Indicators.GetIndicator<SignalFibGrid>(20);
            _atr = Indicators.GetIndicator<ATRSignals>(21, MovingAverageType.Exponential);
        }

        public override void Calculate(int index)
        {
            double ema1m = _emasignal.EMA1m[index];
            double ema5m = _emasignal.EMA5m[index];
            double ema15m = _emasignal.EMA15m[index];
            double ema60m = _emasignal.EMA60m[index];
            double atr1m = _atr.ATR1m[index];
            double atr5m = _atr.ATR5m[index];
            double atr15m = _atr.ATR15m[index];
            double atr60m = _atr.ATR60m[index];


            UpperBand1[index] = ema1m + 1.62 * atr1m;
            UpperBand2[index] = ema1m + 2.62 * atr1m;
            UpperBand3[index] = ema1m + 4.23 * atr1m;
            UpperBand4[index] = ema1m + 1 * atr1m;
            LowerBand1[index] = ema1m - 1.62 * atr1m;
            LowerBand2[index] = ema1m - 2.62 * atr1m;
            LowerBand3[index] = ema1m - 4.23 * atr1m;
            LowerBand4[index] = ema1m - 1 * atr1m;

            UpperBand5[index] = ema5m + 1.62 * atr5m;
            UpperBand6[index] = ema5m + 2.62 * atr5m;
            UpperBand7[index] = ema5m + 4.23 * atr5m;
            UpperBand8[index] = ema5m + 1 * atr5m;
            LowerBand5[index] = ema5m - 1.62 * atr5m;
            LowerBand6[index] = ema5m - 2.62 * atr5m;
            LowerBand7[index] = ema5m - 4.23 * atr5m;
            LowerBand8[index] = ema5m - 1 * atr5m;

            UpperBand9[index] = ema15m + 1.62 * atr15m;
            UpperBand10[index] = ema15m + 2.62 * atr15m;
            UpperBand11[index] = ema15m + 4.23 * atr15m;
            UpperBand12[index] = ema15m + 1 * atr15m;
            LowerBand9[index] = ema15m - 1.62 * atr15m;
            LowerBand10[index] = ema15m - 2.62 * atr15m;
            LowerBand11[index] = ema15m - 4.23 * atr15m;
            LowerBand12[index] = ema15m - 1 * atr15m;

            UpperBand13[index] = ema60m + 1.62 * atr60m;
            UpperBand14[index] = ema60m + 2.62 * atr60m;
            UpperBand15[index] = ema60m + 4.23 * atr60m;
            UpperBand16[index] = ema60m + 1 * atr60m;
            LowerBand13[index] = ema60m - 1.62 * atr60m;
            LowerBand14[index] = ema60m - 2.62 * atr60m;
            LowerBand15[index] = ema60m - 4.23 * atr60m;
            LowerBand16[index] = ema60m - 1 * atr60m;

            //EMA1m[index] = ema1m;
            //EMA5m[index] = ema5m;
            //EMA15m[index] = ema15m;
            //EMA60m[index] = ema60m;

            //ATR1m[index] = atr1m;
            //ATR5m[index] = atr5m;
            //ATR15m[index] = atr15m;
            //ATR60m[index] = atr60m;


        }
    }
}
