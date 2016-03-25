using cAlgo.API;


namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class BarIDs : Indicator
    {
        [Parameter(DefaultValue = 0.0012, MinValue = 0.0001)]
        public double dblSpacing { get; set; }


        public override void Calculate(int index)
        {
            ChartObjects.DrawText(string.Concat("id-", index), (index).ToString(), index, MarketSeries.High.Last(0) + dblSpacing);
        }
    }
}
