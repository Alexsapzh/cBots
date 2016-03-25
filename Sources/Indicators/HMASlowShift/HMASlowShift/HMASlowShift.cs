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
    public class HMASlowShift : Indicator
    {

        [Output("HMA Slow Shift", Color = Colors.Orange)]
        public IndicatorDataSeries hmaslow { get; set; }

        [Parameter(DefaultValue = 31)]
        public int SlowPeriod { get; set; }

        [Parameter(DefaultValue = -2, MinValue = -100, MaxValue = 500)]
        public int SlowShift { get; set; }

        private HMAslow _hmaslow;

        protected override void Initialize()
        {
            _hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriod);
        }

        public override void Calculate(int index)
        {
            if (SlowShift < 0 && index < Math.Abs(SlowShift))
                return;

            hmaslow[index + SlowShift] = _hmaslow.hmaslow[index];
        }
    }
}
