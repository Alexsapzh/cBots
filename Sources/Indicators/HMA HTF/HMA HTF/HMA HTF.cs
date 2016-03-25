using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class HMAHTF : Indicator
    {
        [Output("HMA", Color = Colors.Orange)]
        public IndicatorDataSeries hma { get; set; }

        [Parameter(DefaultValue = 21)]
        public int HTF_Period { get; set; }

        [Parameter("Display Line", DefaultValue = true)]
        public bool DisplayHullLine { get; set; }

        [Parameter("Display Trend Signal", DefaultValue = true)]
        public bool DisplayTrendSignal { get; set; }

        [Parameter("Display Position, 1-8", DefaultValue = 5, MinValue = 1, MaxValue = 8)]
        public int DisplayPostion { get; set; }

        [Parameter("Display Arrow Signal", DefaultValue = true)]
        public bool DisplayArrowSignal { get; set; }

        [Parameter("Arrow Spacing from Line", DefaultValue = 50)]
        public int ArrowSpacing { get; set; }

        private IndicatorDataSeries diff;
        private WeightedMovingAverage wma1;
        private WeightedMovingAverage wma2;
        private WeightedMovingAverage wma3;

        private string trend = string.Empty;
        private Colors trendColor = Colors.Red;
        private StaticPosition position;

        private bool bullishArrowDrawn = false;
        private bool bearishArrowDrawn = false;

        public bool IsBullish = false;
        public bool IsBearish = false;

        double var1 = 0;
        double var2 = 0;

        protected override void Initialize()
        {
            var index = MarketSeries.Close.Count - 1;

            diff = CreateDataSeries();
            wma1 = Indicators.WeightedMovingAverage(MarketSeries.Close, (int)HTF_Period / 2);
            wma2 = Indicators.WeightedMovingAverage(MarketSeries.Close, HTF_Period);
            wma3 = Indicators.WeightedMovingAverage(diff, (int)Math.Sqrt(HTF_Period));

            var1 = 2 * wma1.Result[index];
            var2 = wma2.Result[index];

            diff[index] = var1 - var2;
        }

        public override void Calculate(int index)
        {
            var1 = 2 * wma1.Result[index];
            var2 = wma2.Result[index];

            diff[index] = var1 - var2;

            if (DisplayHullLine)
                hma[index] = wma3.Result[index];

            // BEARISH
            if (wma3.Result.IsFalling())
            {
                IsBearish = true;
                IsBullish = false;
                trend = "BEARISH (hma)";
                trendColor = Colors.Red;
            }

            // BULLISH
            if (wma3.Result.IsRising())
            {
                IsBearish = false;
                IsBullish = true;
                trend = "BULLISH (hma)";
                trendColor = Colors.Green;
            }

            switch (DisplayPostion)
            {
                case 1:
                    position = StaticPosition.TopLeft;
                    break;
                case 2:
                    position = StaticPosition.TopCenter;
                    break;
                case 3:
                    position = StaticPosition.TopRight;
                    break;
                case 4:
                    position = StaticPosition.Right;
                    break;
                case 5:
                    position = StaticPosition.BottomRight;
                    break;
                case 6:
                    position = StaticPosition.BottomCenter;
                    break;
                case 7:
                    position = StaticPosition.BottomLeft;
                    break;
                case 8:
                    position = StaticPosition.Left;
                    break;
                default:
                    position = StaticPosition.TopLeft;
                    break;
            }

            if (DisplayTrendSignal)
                ChartObjects.DrawText("trendText", trend, position, trendColor);

            if (DisplayArrowSignal)
            {
                if (IsBullish && !bullishArrowDrawn)
                {
                    ChartObjects.DrawText(String.Format("Arrow{0}", index), "▲", index, wma3.Result.LastValue - Symbol.PipSize * ArrowSpacing, VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Green);
                    bullishArrowDrawn = true;
                    bearishArrowDrawn = false;
                }

                if (IsBearish && !bearishArrowDrawn)
                {
                    ChartObjects.DrawText(String.Format("Arrow{0}", index), "▼", index, wma3.Result.LastValue + Symbol.PipSize * ArrowSpacing, VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Red);
                    bearishArrowDrawn = true;
                    bullishArrowDrawn = false;
                }
            }
        }
    }
}
