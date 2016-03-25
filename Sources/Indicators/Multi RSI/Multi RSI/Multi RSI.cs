using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Requests;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class MultiSymbolRsiRobot : Robot
    {

        private const string MyLabel = "MultiSymbolRsiRobot";

        [Parameter("Periods", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter("Volume", DefaultValue = 10000, MinValue = 1000)]
        public int Volume { get; set; }

        private RelativeStrengthIndex eurJpyRSI;
        private RelativeStrengthIndex eurUsdRSI;

        private Symbol eurUsd;

        protected override void OnStart()
        {
            var eurJpySeries = MarketData.GetSeries("EURJPY", TimeFrame);
            var eurUsdSeries = MarketData.GetSeries("EURUSD", TimeFrame);

            eurJpyRSI = Indicators.RelativeStrengthIndex(eurJpySeries.Close, Periods);
            eurUsdRSI = Indicators.RelativeStrengthIndex(eurUsdSeries.Close, Periods);

            eurUsd = MarketData.GetSymbol("EURUSD");
        }

        protected override void OnTick()
        {
            if (Trade.IsExecuting)
                return;

            if (eurUsdRSI.Result.LastValue > 70 && eurJpyRSI.Result.LastValue > 70)
            {
                ClosePosition(eurUsd, TradeType.Buy);
                OpenPosition(eurUsd, TradeType.Sell);
            }
            if (eurUsdRSI.Result.LastValue < 30 && eurJpyRSI.Result.LastValue < 30)
            {
                ClosePosition(eurUsd, TradeType.Sell);
                OpenPosition(eurUsd, TradeType.Buy);
            }
        }


        private void ClosePosition(Symbol symbol, TradeType tradeType)
        {
            foreach (Position position in Positions)
            {
                if (position.Label == MyLabel && position.SymbolCode == symbol.Code && position.TradeType == tradeType)
                    ClosePosition(position);
            }
        }

        private void OpenPosition(Symbol symbol, TradeType tradeType)
        {
            if (HasPosition(symbol, tradeType))
                return;

            ExecuteMarketOrder(tradeType, Volume);
            {
                Label = MyLabel
                Symbol = symbol
            }
            

            Trade.Send(request);
        }

        private bool HasPosition(Symbol symbol, TradeType tradeType)
        {
            foreach (Position position in Positions)
            {
                if (position.SymbolCode == symbol.Code && position.Label == MyLabel && position.TradeType == tradeType)
                    return true;
            }
            return false;
        }

    }
}
