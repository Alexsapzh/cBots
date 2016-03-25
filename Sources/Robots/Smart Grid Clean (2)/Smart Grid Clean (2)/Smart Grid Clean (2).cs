using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SmartGrid : Robot
    {
        private bool _accountIsOutOfMoney;
        private int _openTradeResult;

        private readonly string Label = "SmartGrid2";
        private DateTime _lastBuyTradeTime;
        private DateTime _lastSellTradeTime;

        [Parameter("Buy", DefaultValue = true)]
        public bool Buy { get; set; }

        [Parameter("Sell", DefaultValue = true)]
        public bool Sell { get; set; }

        [Parameter("Pip Step", DefaultValue = 10, MinValue = 1)]
        public int PipStep { get; set; }

        [Parameter("First Volume", DefaultValue = 1000, MinValue = 1000, Step = 1000)]
        public int FirstVolume { get; set; }

        [Parameter("Volume Exponent", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 15.0)]
        public double VolumeExponent { get; set; }

        [Parameter("Max Spread", DefaultValue = 3.0)]
        public double MaxSpread { get; set; }

        [Parameter("Average TP", DefaultValue = 3, MinValue = 1)]
        public int AverageTakeProfit { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Stop Loss", DefaultValue = 10)]
        public int StopLoss { get; set; }

        [Parameter("Period", DefaultValue = 1)]
        public int HeikenPeriod { get; set; }

        [Parameter("KAMA Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("KAMA FastPeriod", DefaultValue = 2)]
        public int Fast { get; set; }

        [Parameter("KAMA SlowPeriod", DefaultValue = 30)]
        public int Slow { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Parameter("ADX Trend Strength", DefaultValue = 20, MinValue = 1, MaxValue = 30, Step = 1)]
        public int trend { get; set; }

        [Parameter("Fisher Length", DefaultValue = 13, MinValue = 2)]
        public int Len { get; set; }



        private HeikenAshi2 _heiken;
        private FisherSignal Fisher;
        private KAMASignal _kama;
        private ADXRSignal _adx;
        private int index;

        private double CurrentSpread
        {
            get { return (Symbol.Ask - Symbol.Bid) / Symbol.PipSize; }
        }


        protected override void OnStart()
        {

            _heiken = Indicators.GetIndicator<HeikenAshi2>(1);
            _kama = Indicators.GetIndicator<KAMASignal>(Source, Fast, Slow, Period);
            _adx = Indicators.GetIndicator<ADXRSignal>(Source, interval);
            Fisher = Indicators.GetIndicator<FisherSignal>(Len);
        }

        protected override void OnTick()
        {
            if (CountOfTradesOfType(TradeType.Buy) > 0)
                AdjustBuyPositionTakeProfits(CalculateAveragePositionPrice(TradeType.Buy), AverageTakeProfit);
            if (CountOfTradesOfType(TradeType.Sell) > 0)
                AdjustSellPositionTakeProfits(CalculateAveragePositionPrice(TradeType.Sell), AverageTakeProfit);
            if (CurrentSpread <= MaxSpread && !_accountIsOutOfMoney)
                ProcessTrades();

            if (!this.IsBacktesting)
                DisplayStatusOnChart();


        }

        protected override void OnError(Error error)
        {
            if (error.Code == ErrorCode.NoMoney)
            {
                _accountIsOutOfMoney = true;
                Print("opening stopped because: not enough money");
            }
        }

        protected override void OnBar()
        {
            RefreshData();
        }

        protected override void OnStop()
        {
            ChartObjects.RemoveAllObjects();
        }

        private void ProcessTrades()
        {
            index = MarketSeries.Close.Count - 1;

            //Heiken EMA
            var emalong = _heiken.xClose[index] > _heiken.xOpen[index];
            var emashort = _heiken.xClose[index] < _heiken.xOpen[index];

            // Heiken Ashi EMA HTF Signal
            var emalong1 = _kama.KAMA5.IsRising();
            var emashort1 = _kama.KAMA5.IsFalling();
            var emalong2 = _kama.KAMA15.IsRising();
            var emashort2 = _kama.KAMA15.IsFalling();
            var emalong3 = _heiken.xClose[index] > _kama.KAMA15[index];
            var emashort3 = _heiken.xClose[index] < _kama.KAMA15[index];
            var emalong4 = _heiken.xClose[index] > _kama.KAMA5[index];
            var emashort4 = _heiken.xClose[index] < _kama.KAMA5[index];

            var kamalong = _heiken.xClose[index] > _kama.KAMAHOUR[index] && _kama.KAMAHOUR.IsRising();
            var kamashort = _heiken.xClose[index] < _kama.KAMAHOUR[index] && _kama.KAMAHOUR.IsFalling();

            var adxrtrend = _adx.ADXRHOUR[index] >= trend && _adx.ADXRHOUR.IsRising();

            var adxrlong = _adx.DIPLUS[index] > _adx.DIMINUS[index] && _adx.DIPLUSHOUR[index] > _adx.DIMINUSHOUR[index] && _adx.DIPLUSHOUR4[index] > _adx.DIMINUSHOUR4[index];
            var adxrshort = _adx.DIMINUS[index] > _adx.DIPLUS[index] && _adx.DIMINUSHOUR[index] > _adx.DIPLUSHOUR[index] && _adx.DIMINUSHOUR4[index] > _adx.DIPLUSHOUR4[index];

            var Fisherlong = Fisher.FISH[index] > Fisher.TRIGGER[index];
            var Fishershort = Fisher.FISH[index] < Fisher.TRIGGER[index];


            //if (Buy && CountOfTradesOfType(TradeType.Buy) == 0 && emalong && emalong1 && emalong2 && emalong3 && emalong4 && adxrtrend && adxrlong && Fisherlong)
            if (Buy && CountOfTradesOfType(TradeType.Buy) == 0 && kamalong && adxrtrend && adxrlong && Fisherlong)
            {
                _openTradeResult = OrderSend(TradeType.Buy, LimitVolume(FirstVolume));
                if (_openTradeResult > 0)
                    _lastBuyTradeTime = MarketSeries.OpenTime.Last(0);
                else
                    Print("First BUY openning error at: ", Symbol.Ask, "Error Type: ", LastResult.Error);
            }
            //if (Sell && CountOfTradesOfType(TradeType.Sell) == 0 && emashort && emashort1 && emashort2 && emashort3 && emashort4 && adxrtrend && adxrshort && Fishershort)
            if (Sell && CountOfTradesOfType(TradeType.Sell) == 0 && kamashort && adxrtrend && adxrshort && Fishershort)
            {
                _openTradeResult = OrderSend(TradeType.Sell, LimitVolume(FirstVolume));
                if (_openTradeResult > 0)
                    _lastSellTradeTime = MarketSeries.OpenTime.Last(0);
                else
                    Print("First SELL opening error at: ", Symbol.Bid, "Error Type: ", LastResult.Error);
            }

            if (CountOfTradesOfType(TradeType.Buy) > 0)
            {
                if (Math.Round(Symbol.Ask, Symbol.Digits) < Math.Round(FindLowestPositionPrice(TradeType.Buy) - PipStep * Symbol.PipSize, Symbol.Digits) && _lastBuyTradeTime != MarketSeries.OpenTime.Last(0))
                {
                    var calculatedVolume = CalculateVolume(TradeType.Buy);
                    _openTradeResult = OrderSend(TradeType.Buy, LimitVolume(calculatedVolume));
                    if (_openTradeResult > 0)
                        _lastBuyTradeTime = MarketSeries.OpenTime.Last(0);
                    else
                        Print("Next BUY opening error at: ", Symbol.Ask, "Error Type: ", LastResult.Error);
                }
            }
            if (CountOfTradesOfType(TradeType.Sell) > 0)
            {
                if (Math.Round(Symbol.Bid, Symbol.Digits) > Math.Round(FindHighestPositionPrice(TradeType.Sell) + PipStep * Symbol.PipSize, Symbol.Digits) && _lastSellTradeTime != MarketSeries.OpenTime.Last(0))
                {
                    var calculatedVolume = CalculateVolume(TradeType.Sell);
                    _openTradeResult = OrderSend(TradeType.Sell, LimitVolume(calculatedVolume));
                    if (_openTradeResult > 0)
                        _lastSellTradeTime = MarketSeries.OpenTime.Last(0);
                    else
                        Print("Next SELL opening error at: ", Symbol.Bid, "Error Type: ", LastResult.Error);
                }
            }
        }



        private int OrderSend(TradeType tradeType, long volumeToUse)
        {
            var returnResult = 0;
            if (volumeToUse > 0)
            {
                var result = ExecuteMarketOrder(tradeType, Symbol, volumeToUse, Label, StopLoss, 0, 0, "smart_grid");

                if (result.IsSuccessful)
                {
                    Print(tradeType, "Opened at: ", result.Position.EntryPrice, result.Position.StopLoss);
                    returnResult = 1;
                }
                else
                    Print(tradeType, "Openning Error: ", result.Error);
            }
            else
                Print("Volume calculation error: Calculated Volume is: ", volumeToUse);
            return returnResult;
        }

        private void AdjustBuyPositionTakeProfits(double averageBuyPositionPrice, int averageTakeProfit)
        {
            foreach (var buyPosition in Positions)
            {
                if (buyPosition.Label == Label && buyPosition.SymbolCode == Symbol.Code)
                {
                    if (buyPosition.TradeType == TradeType.Buy)
                    {
                        double? calculatedTakeProfit = Math.Round(averageBuyPositionPrice + averageTakeProfit * Symbol.PipSize, Symbol.Digits);
                        if (buyPosition.TakeProfit != calculatedTakeProfit)
                            ModifyPosition(buyPosition, buyPosition.StopLoss, calculatedTakeProfit);
                    }
                }
            }
        }

        private void AdjustSellPositionTakeProfits(double averageSellPositionPrice, int averageTakeProfit)
        {
            foreach (var sellPosition in Positions)
            {
                if (sellPosition.Label == Label && sellPosition.SymbolCode == Symbol.Code)
                {
                    if (sellPosition.TradeType == TradeType.Sell)
                    {
                        double? calculatedTakeProfit = Math.Round(averageSellPositionPrice - averageTakeProfit * Symbol.PipSize, Symbol.Digits);
                        if (sellPosition.TakeProfit != calculatedTakeProfit)
                            ModifyPosition(sellPosition, sellPosition.StopLoss, calculatedTakeProfit);
                    }
                }
            }
        }

        private void DisplayStatusOnChart()
        {
            if (CountOfTradesOfType(TradeType.Buy) > 1)
            {
                var y = CalculateAveragePositionPrice(TradeType.Buy);
                ChartObjects.DrawHorizontalLine("bpoint", y, Colors.Yellow, 2, LineStyle.Dots);
            }
            else
                ChartObjects.RemoveObject("bpoint");
            if (CountOfTradesOfType(TradeType.Sell) > 1)
            {
                var z = CalculateAveragePositionPrice(TradeType.Sell);
                ChartObjects.DrawHorizontalLine("spoint", z, Colors.HotPink, 2, LineStyle.Dots);
            }
            else
                ChartObjects.RemoveObject("spoint");
            ChartObjects.DrawText("pan", GenerateStatusText(), StaticPosition.TopLeft, Colors.Tomato);
        }

        private string GenerateStatusText()
        {
            var statusText = "";
            var buyPositions = "";
            var sellPositions = "";
            var spread = "";
            var buyDistance = "";
            var sellDistance = "";
            spread = "\nSpread = " + Math.Round(CurrentSpread, 1);
            buyPositions = "\nBuy Positions = " + CountOfTradesOfType(TradeType.Buy);
            sellPositions = "\nSell Positions = " + CountOfTradesOfType(TradeType.Sell);
            if (CountOfTradesOfType(TradeType.Buy) > 0)
            {
                var averageBuyFromCurrent = Math.Round((CalculateAveragePositionPrice(TradeType.Buy) - Symbol.Bid) / Symbol.PipSize, 1);
                buyDistance = "\nBuy Target Away = " + averageBuyFromCurrent;
            }
            if (CountOfTradesOfType(TradeType.Sell) > 0)
            {
                var averageSellFromCurrent = Math.Round((Symbol.Ask - CalculateAveragePositionPrice(TradeType.Sell)) / Symbol.PipSize, 1);
                sellDistance = "\nSell Target Away = " + averageSellFromCurrent;
            }
            if (CurrentSpread > MaxSpread)
                statusText = "MAX SPREAD EXCEED";
            else
                statusText = "Smart Grid" + buyPositions + spread + sellPositions + buyDistance + sellDistance;
            return (statusText);
        }



        private int CountOfTradesOfType(TradeType tradeType)
        {
            var tradeCount = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                        tradeCount++;
                }
            }

            return tradeCount;
        }

        private double CalculateAveragePositionPrice(TradeType tradeType)
        {
            double result = 0;
            double averagePrice = 0;
            long count = 0;


            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                    {
                        averagePrice += position.EntryPrice * position.Volume;
                        count += position.Volume;
                    }
                }

            }

            if (averagePrice > 0 && count > 0)
                result = Math.Round(averagePrice / count, Symbol.Digits);
            return result;
        }

        private double FindLowestPositionPrice(TradeType tradeType)
        {
            double lowestPrice = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                    {
                        if (lowestPrice == 0)
                        {
                            lowestPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < lowestPrice)
                            lowestPrice = position.EntryPrice;
                    }
                }
            }

            return lowestPrice;
        }

        private double FindHighestPositionPrice(TradeType tradeType)
        {
            double highestPrice = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                    {
                        if (highestPrice == 0)
                        {
                            highestPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice > highestPrice)
                            highestPrice = position.EntryPrice;
                    }
                }
            }

            return highestPrice;
        }

        private double FindPriceOfMostRecentPositionId(TradeType tradeType)
        {
            double price = 0;
            var highestPositionId = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                    {
                        if (highestPositionId == 0 || highestPositionId > position.Id)
                        {
                            price = position.EntryPrice;
                            highestPositionId = position.Id;
                        }
                    }
                }
            }

            return price;
        }

        private long GetMostRecentPositionVolume(TradeType tradeType)
        {
            long mostRecentVolume = 0;
            var highestPositionId = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType)
                    {
                        if (highestPositionId == 0 || highestPositionId > position.Id)
                        {
                            mostRecentVolume = position.Volume;
                            highestPositionId = position.Id;
                        }
                    }
                }
            }

            return mostRecentVolume;
        }

        private int CountNumberOfPositionsOfType(TradeType tradeType)
        {
            var mostRecentPrice = FindPriceOfMostRecentPositionId(tradeType);
            var numberOfPositionsOfType = 0;

            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == tradeType && tradeType == TradeType.Buy)
                    {
                        if (Math.Round(position.EntryPrice, Symbol.Digits) <= Math.Round(mostRecentPrice, Symbol.Digits))
                            numberOfPositionsOfType++;
                    }
                    if (position.TradeType == tradeType && tradeType == TradeType.Sell)
                    {
                        if (Math.Round(position.EntryPrice, Symbol.Digits) >= Math.Round(mostRecentPrice, Symbol.Digits))
                            numberOfPositionsOfType++;
                    }
                }
            }

            return (numberOfPositionsOfType);
        }

        private long CalculateVolume(TradeType tradeType)
        {
            var numberOfPositions = CountNumberOfPositionsOfType(tradeType);
            var mostRecentVolume = GetMostRecentPositionVolume(tradeType);
            var calculatedVolume = Symbol.NormalizeVolume(mostRecentVolume * Math.Pow(VolumeExponent, numberOfPositions));
            return (calculatedVolume);
        }

        private long LimitVolume(long volumeIn)
        {
            var symbolVolumeMin = Symbol.VolumeMin;
            var symbolVolumeMax = Symbol.VolumeMax;
            var result = volumeIn;
            if (result < symbolVolumeMin)
                result = symbolVolumeMin;
            if (result > symbolVolumeMax)
                result = symbolVolumeMax;
            return (result);
        }
    }
}
