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
    public class HMAFastShift : Indicator
    {

        [Parameter(DefaultValue = 5)]
        public int FastPeriod { get; set; }

        [Parameter(DefaultValue = 0, MinValue = -100, MaxValue = 500)]
        public int FastShift { get; set; }

        [Output("HMA Fast Shift", Color = Colors.Blue)]
        public IndicatorDataSeries hmafast { get; set; }

        private HMAfast _hmafast;

        protected override void Initialize()
        {
            _hmafast = Indicators.GetIndicator<HMAfast>(FastPeriod);
        }

        public override void Calculate(int index)
        {
            if (FastShift < 0 && index < Math.Abs(FastShift))
                return;

            hmafast[index + FastShift] = _hmafast.hmafast[index];
        }
    }
}
