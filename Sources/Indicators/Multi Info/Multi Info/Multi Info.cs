using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class MultiSymbolMarketInfo : Indicator
    {
        private Symbol symbol1;
        private Symbol symbol2;
        private Symbol symbol3;

        [Parameter(DefaultValue = "EURGBP")]
        public string Symbol1 { get; set; }

        [Parameter(DefaultValue = "GBPUSD")]
        public string Symbol2 { get; set; }

        [Parameter(DefaultValue = "EURUSD")]
        public string Symbol3 { get; set; }

        protected override void Initialize()
        {
            symbol1 = MarketData.GetSymbol(Symbol1);
            symbol2 = MarketData.GetSymbol(Symbol2);
            symbol3 = MarketData.GetSymbol(Symbol3);
        }

        public override void Calculate(int index)
        {
            if (!IsLastBar)
                return;

            var text = FormatSymbol(symbol1) + "\n" + FormatSymbol(symbol2) + "\n" + FormatSymbol(symbol3);

            ChartObjects.DrawText("symbol1", text, StaticPosition.TopLeft, Colors.Lime);
        }

        private string FormatSymbol(Symbol symbol)
        {
            var spread = Math.Round(symbol.Spread / symbol.PipSize, 1);
            return string.Format("{0}\t Ask: {1}\t Bid: {2}\t Spread: {3}", symbol.Code, symbol.Ask, symbol.Bid, spread);

        }
    }
}
