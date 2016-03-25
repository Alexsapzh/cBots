using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC)]
    public class MultiSymbolMA : Indicator
    {
        private MovingAverage ma1, ma2, ma3, ma4, ma5, ma6, ma7, ma8, ma9, ma10;
        private MarketSeries series2, series3, series4, series5, series6, series7, series8, series9, series10;
        private Symbol symbol2, symbol3, symbol4, symbol5, symbol6, symbol7, symbol8, symbol9, symbol10;

        [Parameter(DefaultValue = "GBPAUD")]
        public string Symbol2 { get; set; }

        [Parameter(DefaultValue = "GBPCAD")]
        public string Symbol3 { get; set; }

        [Parameter(DefaultValue = "GBPCHF")]
        public string Symbol4 { get; set; }

        [Parameter(DefaultValue = "GBPDKK")]
        public string Symbol5 { get; set; }

        [Parameter(DefaultValue = "GBPJPY")]
        public string Symbol6 { get; set; }

        [Parameter(DefaultValue = "GBPNOK")]
        public string Symbol7 { get; set; }

        [Parameter(DefaultValue = "GBPNZD")]
        public string Symbol8 { get; set; }

        [Parameter(DefaultValue = "GBPSEK")]
        public string Symbol9 { get; set; }

        [Parameter(DefaultValue = "GBPSGD")]
        public string Symbol10 { get; set; }

        [Parameter(DefaultValue = 14)]
        public int Period { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MaType { get; set; }

        [Output("MA Symbol 1", Color = Colors.Magenta)]
        public IndicatorDataSeries Result1 { get; set; }

        [Output("MA Symbol 2", Color = Colors.Magenta)]
        public IndicatorDataSeries Result2 { get; set; }

        [Output("MA Symbol 3", Color = Colors.Magenta)]
        public IndicatorDataSeries Result3 { get; set; }

        [Output("MA Symbol 4", Color = Colors.Magenta)]
        public IndicatorDataSeries Result4 { get; set; }

        [Output("MA Symbol 5", Color = Colors.Magenta)]
        public IndicatorDataSeries Result5 { get; set; }

        [Output("MA Symbol 6", Color = Colors.Magenta)]
        public IndicatorDataSeries Result6 { get; set; }

        [Output("MA Symbol 7", Color = Colors.Magenta)]
        public IndicatorDataSeries Result7 { get; set; }

        [Output("MA Symbol 8", Color = Colors.Magenta)]
        public IndicatorDataSeries Result8 { get; set; }

        [Output("MA Symbol 9", Color = Colors.Magenta)]
        public IndicatorDataSeries Result9 { get; set; }

        [Output("MA Symbol 10", Color = Colors.Magenta)]
        public IndicatorDataSeries Result10 { get; set; }

        protected override void Initialize()
        {
            symbol2 = MarketData.GetSymbol(Symbol2);
            symbol3 = MarketData.GetSymbol(Symbol3);
            symbol4 = MarketData.GetSymbol(Symbol4);
            symbol5 = MarketData.GetSymbol(Symbol5);
            symbol6 = MarketData.GetSymbol(Symbol6);
            symbol7 = MarketData.GetSymbol(Symbol7);
            symbol8 = MarketData.GetSymbol(Symbol8);
            symbol9 = MarketData.GetSymbol(Symbol9);
            symbol10 = MarketData.GetSymbol(Symbol10);


            series2 = MarketData.GetSeries(symbol2, TimeFrame);
            series3 = MarketData.GetSeries(symbol3, TimeFrame);
            series4 = MarketData.GetSeries(symbol4, TimeFrame);
            series5 = MarketData.GetSeries(symbol5, TimeFrame);
            series6 = MarketData.GetSeries(symbol6, TimeFrame);
            series7 = MarketData.GetSeries(symbol7, TimeFrame);
            series8 = MarketData.GetSeries(symbol8, TimeFrame);
            series9 = MarketData.GetSeries(symbol9, TimeFrame);
            series10 = MarketData.GetSeries(symbol10, TimeFrame);

            ma1 = Indicators.MovingAverage(MarketSeries.Close, Period, MaType);
            ma2 = Indicators.MovingAverage(series2.Close, Period, MaType);
            ma3 = Indicators.MovingAverage(series3.Close, Period, MaType);
            ma4 = Indicators.MovingAverage(series4.Close, Period, MaType);
            ma5 = Indicators.MovingAverage(series5.Close, Period, MaType);
            ma6 = Indicators.MovingAverage(series6.Close, Period, MaType);
            ma7 = Indicators.MovingAverage(series7.Close, Period, MaType);
            ma8 = Indicators.MovingAverage(series8.Close, Period, MaType);
            ma9 = Indicators.MovingAverage(series9.Close, Period, MaType);
            ma10 = Indicators.MovingAverage(series10.Close, Period, MaType);

        }

        public override void Calculate(int index)
        {
            ShowOutput(Symbol, Result1, ma1, MarketSeries, index);
            ShowOutput(symbol2, Result2, ma2, series2, index);
            ShowOutput(symbol3, Result3, ma3, series3, index);
            ShowOutput(symbol4, Result4, ma4, series4, index);
            ShowOutput(symbol5, Result5, ma5, series5, index);
            ShowOutput(symbol6, Result6, ma6, series6, index);
            ShowOutput(symbol7, Result7, ma7, series7, index);
            ShowOutput(symbol8, Result8, ma8, series8, index);
            ShowOutput(symbol9, Result9, ma9, series9, index);
            ShowOutput(symbol10, Result10, ma10, series10, index);

        }

        private void ShowOutput(Symbol symbol, IndicatorDataSeries result, MovingAverage movingAverage, MarketSeries series, int index)
        {
            var index2 = series.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            result[index] = movingAverage.Result[index2];

            string text = string.Format("{0} {1}", symbol.Code, Math.Round(result[index], symbol.Digits));
            ChartObjects.DrawText(symbol.Code, text, index, result[index], VerticalAlignment.Top, HorizontalAlignment.Right, Colors.Yellow);
        }
    }
}
