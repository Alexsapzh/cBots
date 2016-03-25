using System;
using cAlgo.API;


namespace cAlgo.Indicators
{
    [Indicator(TimeZone = TimeZones.EEuropeStandardTime, IsOverlay = true, AutoRescale = false, AccessRights = AccessRights.None)]
    public class WeekOpen : Indicator
    {
        [Output("Open", Color = Colors.DarkOrange, PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries Open { get; set; }

        [Parameter("Show 100PipsLevels", DefaultValue = 1)]
        public bool Set100Levels { get; set; }


        [Parameter("100 Thicknes", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int t1 { get; set; }

        [Parameter("Show 500PipsLevels", DefaultValue = 1)]
        public bool Set500Levels { get; set; }

        [Parameter("500 Thicknes", DefaultValue = 3, MinValue = 1, MaxValue = 5)]
        public int t2 { get; set; }

        [Parameter("MinLevel", DefaultValue = 0, MinValue = 0)]
        public int MinLevel { get; set; }

        [Parameter("MaxLevel", DefaultValue = 300, MinValue = 2)]
        public int MaxLevel { get; set; }

        [Parameter("Twitter", DefaultValue = "@fxstarforex")]
        public string Twitter { get; set; }

        public double openprice = 0;

        public override void Calculate(int index)
        {

            if (index < 1)
            {
                // If first bar is first bar of the day set open
                if (MarketSeries.OpenTime[index].TimeOfDay == TimeSpan.Zero)
                    Open[index] = MarketSeries.Open[index];
                return;
            }

            DateTime openTime = MarketSeries.OpenTime[index];
            DateTime lastOpenTime = MarketSeries.OpenTime[index - 1];
            const string objectName = "messageNA";

            if (!ApplicableTimeFrame(openTime, lastOpenTime))
            {
                // Display message that timeframe is N/A
                const string text = "TimeFrame Not Applicable. Choose a lower Timeframe";
                ChartObjects.DrawText(objectName, text, StaticPosition.TopLeft, Colors.Red);
                return;
            }

            // If TimeFrame chosen is applicable remove N/A message
            ChartObjects.RemoveObject(objectName);

            // Plot Daily Open and Close
            PlotDailyOpenClose(openTime, lastOpenTime, index);

            string Side = "BUY";

            double PipsWeek = 0;
            PipsWeek = (Symbol.Ask - openprice) / Symbol.PipSize;

            double Pips = 0;
            if (Symbol.Ask > openprice)
                Pips = (Symbol.Ask - openprice) / Symbol.PipSize;

            if (Symbol.Ask < openprice)
            {
                Pips = (openprice - Symbol.Ask) / Symbol.PipSize;
                Side = "SELL";
            }

            double Profit = (Pips / 100) * 1000;
            double PL = Math.Round(Account.Equity - Account.Balance);

            var name1 = "Week";
            var text1 = "Week Open : " + openprice.ToString() + "\n Week open pips:  " + (int)PipsWeek + " Side: " + Side;
            var staticPos = StaticPosition.TopRight;
            var color = Colors.Red;
            ChartObjects.DrawText(name1, text1, staticPos, color);

            var name12 = "Pips";
            var text12 = "Position P&L: " + PL + " \n Week open pips : " + (int)PipsWeek + " Side: " + Side + " \n Each 0.01 Lot earn => " + (int)Profit / 100 + " USD (Initial Deposit 100$)";
            var staticPos12 = StaticPosition.BottomLeft;
            var color12 = Colors.DodgerBlue;
            ChartObjects.DrawText(name12, text12, staticPos12, color12);


            if (Set100Levels && MinLevel < MaxLevel)
            {
                for (int i = MinLevel; i < MaxLevel; i++)
                {
                    ChartObjects.DrawHorizontalLine("Level100" + i, i * 100 * Symbol.PipSize, Colors.DodgerBlue, t1, LineStyle.Solid);
                }
            }

            if (Set500Levels && MinLevel < MaxLevel)
            {
                for (int i = MinLevel; i < MaxLevel; i++)
                {
                    ChartObjects.DrawHorizontalLine("Level500" + i, i * 500 * Symbol.PipSize, Colors.YellowGreen, t2, LineStyle.Solid);
                }
            }

        }

        private bool ApplicableTimeFrame(DateTime openTime, DateTime lastOpenTime)
        {
            // minutes difference between bars
            var timeFrameMinutes = (int)(openTime - lastOpenTime).TotalMinutes;

            bool daily = timeFrameMinutes == 1440;
            bool weeklyOrGreater = timeFrameMinutes >= 7200;

            bool timeFrameNotApplicable = daily || weeklyOrGreater;

            if (timeFrameNotApplicable)
                return false;

            return true;
        }

        private void PlotDailyOpenClose(DateTime openTime, DateTime lastOpenTime, int index)
        {

            DateTime currentTime = MarketSeries.OpenTime[MarketSeries.OpenTime.Count - 1];
            DateTime previousTime = MarketSeries.OpenTime[MarketSeries.OpenTime.Count - 2];

            int index1 = MarketSeries.OpenTime.Count - 1;

            // Day change
            //if (openTime.Day != lastOpenTime.Day)
            if (currentTime.DayOfWeek == DayOfWeek.Monday && previousTime.DayOfWeek != DayOfWeek.Monday)
            {
                // Plot Open
                Open[index] = MarketSeries.Open[index];
                openprice = MarketSeries.Open[index];
            }
            // Same Day
            else
            {
                // Plot Open
                Open[index] = Open[index - 1];
                //openprice = MarketSeries.Open[index];
            }

            // Plot todays close
            DateTime today = DateTime.Now.Date;
            if (openTime.Date != today)
                return;
        }
    }
}
