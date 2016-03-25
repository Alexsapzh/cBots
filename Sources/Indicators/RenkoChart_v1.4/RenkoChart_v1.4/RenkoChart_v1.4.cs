using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AutoRescale = true, ScalePrecision = 5, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RenkoChart_v14 : Indicator
    {
        [Parameter("Renko (Pips)", DefaultValue = 10, MinValue = 0.1, Step = 1)]
        public double RenkoPips { get; set; }

        [Parameter("Bricks To Show", DefaultValue = 100, MinValue = 0)]
        public int BricksToShow { get; set; }

        [Parameter("Bullish Color", DefaultValue = "SeaGreen")]
        public string ColorBull { get; set; }

        [Parameter("Bearish Color", DefaultValue = "Tomato")]
        public string ColorBear { get; set; }

        [Parameter("Zoom Level", DefaultValue = 3, MinValue = 0, MaxValue = 5, Step = 1)]
        public double ZoomLevel { get; set; }

        [Parameter("Reference Mode", DefaultValue = false)]
        public bool ReferenceMode { get; set; }

        [Output("Result", Color = Colors.DimGray, Thickness = 1, PlotType = PlotType.Line)]
        public IndicatorDataSeries Result { get; set; }

        public class Renko
        {
            public int Index { get; set; }
            public double Value { get; set; }
            public double Movement { get; set; }
        }

        private List<Renko> renkos = new List<Renko>();
        private int i = 0;
        private double renkoLastValue = 0;
        private double thickness, renkoPips;
        private Colors colorBull, colorBear;
        private bool errorColors;

        protected override void Initialize()
        {
            renkoPips = RenkoPips * Symbol.PipSize;
            thickness = Math.Pow(2, ZoomLevel) - (ZoomLevel > 0 ? 1 : 0);

            if (!Enum.TryParse<Colors>(ColorBull, out colorBull) || !Enum.TryParse<Colors>(ColorBear, out colorBear))
                errorColors = true;
        }

        public override void Calculate(int index)
        {
            if (errorColors)
            {
                ChartObjects.DrawText("Error", "Incorrect colors", StaticPosition.Center, Colors.Red);
                return;
            }

            if (renkoLastValue == 0)
                renkoLastValue = MarketSeries.Open.LastValue - (MarketSeries.Open.LastValue % renkoPips);

            var closeLastValue = MarketSeries.Close.LastValue;

            while (closeLastValue >= renkoLastValue + renkoPips)
            {
                renkoLastValue += renkoPips;
                AddRenko(i, renkoLastValue, +renkoPips);
                i++;
            }
            while (closeLastValue <= renkoLastValue - renkoPips)
            {
                renkoLastValue -= renkoPips;
                AddRenko(i, renkoLastValue, -renkoPips);
                i++;
            }

            if (IsLastBar)
            {
                if (!ReferenceMode)
                    Result[index] = closeLastValue;
                RefreshRenkoChart(index);
            }
        }

        private void AddRenko(int i, double lastValue, double movement)
        {
            renkos.Add(new Renko 
            {
                Index = i,
                Value = lastValue,
                Movement = movement
            });
        }

        private void RefreshRenkoChart(int index)
        {
            foreach (var renko in renkos)
            {
                var i = renkos.Count() - renko.Index;

                if (ReferenceMode)
                    Result[index - i] = renko.Movement > 0 ? 1 : -1;
                else
                {
                    Result[index - i] = renko.Value;

                    var y1 = renkoLastValue + (Result[index] > renkoLastValue ? +renkoPips / 2 : -renkoPips / 2);
                    var y2 = Result[index] + (Result[index] > renkoLastValue ? +renkoPips / 2 : -renkoPips / 2);
                    var color = Result[index] > Result[index - 1] ? colorBull : colorBear;

                    ChartObjects.DrawLine("renko0", index, y1, index, y2, color, thickness, LineStyle.Solid);

                    if (i < BricksToShow)
                        ChartObjects.DrawLine("renko" + i, index - i, Result[index - i] - renkoPips / 2, index - i, Result[index - i] + renkoPips / 2, renko.Movement > 0 ? colorBull : colorBear, thickness, LineStyle.Solid);
                }
            }
        }
    }
}
