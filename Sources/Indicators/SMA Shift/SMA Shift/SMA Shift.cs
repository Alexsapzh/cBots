// -------------------------------------------------------------------------------------------------
//
//    Simple Moving Average Shift
//    This code is a cAlgo API example.
//    
// -------------------------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true)]
    public class SMAShift : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 14)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 5, MinValue = -100, MaxValue = 500)]
        public int Shift { get; set; }

        [Output("Main", Color = Colors.Blue)]
        public IndicatorDataSeries Result { get; set; }

        private SimpleMovingAverage _simpleMovingAverage;

        protected override void Initialize()
        {
            _simpleMovingAverage = Indicators.SimpleMovingAverage(Source, Period);
        }

        public override void Calculate(int index)
        {
            if (Shift < 0 && index < Math.Abs(Shift))
                return;

            Result[index + Shift] = _simpleMovingAverage.Result[index];
        }
    }
}
