using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cAlgo
{
    [Indicator("RenkoChart", IsOverlay = true, AutoRescale = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RenkoChart : Indicator
    {
        [Parameter("Renko (Pips)", DefaultValue = 10, MinValue = 0.1, Step = 1)]
        public double RenkoPips { get; set; }

        [Parameter("Bricks To Show", DefaultValue = 100, MinValue = 1)]
        public int BricksToShow { get; set; }

        [Parameter("Zoom Level", DefaultValue = 3, MinValue = 0, MaxValue = 5, Step = 1)]
        public double ZoomLevel { get; set; }

        [Parameter("Bullish Color", DefaultValue = "SeaGreen")]
        public string ColorBull { get; set; }

        [Parameter("Bearish Color", DefaultValue = "Tomato")]
        public string ColorBear { get; set; }

        [Output("Open", Color = Colors.DimGray, Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries Open { get; set; }

        [Output("Close", Color = Colors.DimGray, Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries Close { get; set; }

        public class Brick
        {
            public double Open { get; set; }
            public double Close { get; set; }
        }

        private List<Brick> renkos = new List<Brick>();
        private double thickness, renkoPips, renkoLastValue;
        private Colors colorBull, colorBear;
        private bool colorError;

        protected override void Initialize()
        {
            if (!Enum.TryParse<Colors>(ColorBull, out colorBull) || !Enum.TryParse<Colors>(ColorBear, out colorBear))
                colorError = true;

            renkoPips = RenkoPips * Symbol.PipSize;
            thickness = Math.Pow(2, ZoomLevel) - (ZoomLevel > 0 ? 1 : 0);
            renkoLastValue = 0;
        }

        public override void Calculate(int index)
        {
            if (colorError)
            {
                ChartObjects.DrawText("Error0", "{o,o}\n/)_)\n \" \"\nOops! Incorrect colors.", StaticPosition.TopCenter, Colors.Gray);
                return;
            }

            if (renkoLastValue == 0)
            {
                var open = MarketSeries.Open.LastValue;

                renkoLastValue = open - (open % renkoPips) + renkoPips / 2;
            }

            var closeLastValue = MarketSeries.Close.LastValue;

            while (closeLastValue >= renkoLastValue + renkoPips * 1.5)
            {
                renkoLastValue += renkoPips;
                renkos.Insert(0, new Brick 
                {
                    Open = renkoLastValue - renkoPips / 2,
                    Close = renkoLastValue + renkoPips / 2
                });
                if (renkos.Count() > BricksToShow)
                    renkos.RemoveRange(BricksToShow, renkos.Count() - BricksToShow);
            }
            while (closeLastValue <= renkoLastValue - renkoPips * 1.5)
            {
                renkoLastValue -= renkoPips;
                renkos.Insert(0, new Brick 
                {
                    Open = renkoLastValue + renkoPips / 2,
                    Close = renkoLastValue - renkoPips / 2
                });
                if (renkos.Count() > BricksToShow)
                    renkos.RemoveRange(BricksToShow, renkos.Count() - BricksToShow);
            }

            if (IsLastBar)
                for (int i = 0; i < BricksToShow - 1; i++)
                {
                    var color = renkos[i].Open < renkos[i].Close ? colorBull : colorBear;

                    ChartObjects.DrawLine(string.Format("renko.Last({0})", i + 1), index - i - 1, renkos[i].Open, index - i - 1, renkos[i].Close, color, thickness, LineStyle.Solid);

                    Open[index - i - 1] = renkos[i].Open;
                    Close[index - i - 1] = renkos[i].Close;
                }

            double y1, y2;
            var top = Math.Max(renkos[0].Open, renkos[0].Close);
            var bottom = Math.Min(renkos[0].Open, renkos[0].Close);

            if (closeLastValue > top)
                y1 = top;
            else if (closeLastValue < bottom)
                y1 = bottom;
            else
                y1 = closeLastValue;

            y2 = closeLastValue;

            var colorLive = y1 < y2 ? colorBull : colorBear;

            ChartObjects.DrawLine("renko.Last(0)", index, y1, index, y2, colorLive, thickness, LineStyle.Solid);

            Open[index] = y1;
            Close[index] = y2;
        }
    }
}
