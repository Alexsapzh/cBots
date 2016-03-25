//+------------------------------------------------------------------+
//+                         Alog developed By Emmanuel Armah--(alleres@live.com) |
//+------------------------------------------------------------------+

using System;
using System.Threading;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.API.Requests;
using cAlgo.Indicators;


namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC)]
    public class Mullti_TimeFrameTrendRev2 : Robot
    {

        [Parameter("RSI_OverBought_Level", DefaultValue = 70)]
        public double _RSI_OverBought_Level { get; set; }
        [Parameter("MaxFreqMinis", DefaultValue = 0)]
        public double _MaxFreqMinis { get; set; }
        [Parameter("RSI_MA", DefaultValue = 14)]
        public double _RSI_MA { get; set; }
        [Parameter("ProfitTargetPips", DefaultValue = 100)]
        public double _ProfitTargetPips { get; set; }
        [Parameter("MaxOpenTrade", DefaultValue = 1)]
        public double _MaxOpenTrade { get; set; }
        [Parameter("TrendDirectionSMA", DefaultValue = 200)]
        public double _TrendDirectionSMA { get; set; }
        [Parameter("MinAdjustmentsPoints", DefaultValue = 500)]
        public double _MinAdjustmentsPoints { get; set; }
        [Parameter("TrailingStopPoints", DefaultValue = 1000)]
        public double _TrailingStopPoints { get; set; }
        [Parameter("RSI_OverSold_Level", DefaultValue = 30)]
        public double _RSI_OverSold_Level { get; set; }
        [Parameter("LotSize", DefaultValue = 0.1)]
        public double _LotSize { get; set; }

        //Global declaration
        private RelativeStrengthIndex i_RSI4Hr;
        private SimpleMovingAverage i_Moving_Average;
        private RelativeStrengthIndex i_RSI;
        double _RSI4Hr;
        double _Weekly_TF;
        double _One_Hour_TF;
        double _Four_Hour_TF;
        double _Moving_Average;
        double _RSI;
        double _Daily_TF;
        bool _AND;
        bool _AND_2;

        DateTime LastTradeExecution = new DateTime(0);

        protected override void OnStart()
        {
            i_RSI4Hr = Indicators.RelativeStrengthIndex(MarketData.GetSeries(TimeFrame.Hour4).Close, (int)_RSI_MA);
            i_Moving_Average = Indicators.SimpleMovingAverage(MarketSeries.Close, (int)_TrendDirectionSMA);
            i_RSI = Indicators.RelativeStrengthIndex(MarketSeries.Close, (int)_RSI_MA);

        }

        protected override void OnTick()
        {
            if (Trade.IsExecuting)
                return;

            //Local declaration
            TriState _BuyTrailingStop = new TriState();
            TriState _SellTrailingStop = new TriState();
            TriState _Sell = new TriState();
            TriState _Buy = new TriState();

            //Step 1
            _RSI4Hr = i_RSI4Hr.Result.Last(0);
            _Weekly_TF = MarketData.GetSeries(TimeFrame.Weekly).Close.Last(0);
            _One_Hour_TF = MarketData.GetSeries(TimeFrame.Hour).Close.Last(0);
            _Four_Hour_TF = MarketData.GetSeries(TimeFrame.Hour4).Close.Last(0);
            _BuyTrailingStop = Simple_Trailing_Stop(1, 0, _TrailingStopPoints, _MinAdjustmentsPoints);
            _SellTrailingStop = Simple_Trailing_Stop(2, 0, _TrailingStopPoints, _MinAdjustmentsPoints);
            _Moving_Average = i_Moving_Average.Result.Last(0);
            _RSI = i_RSI.Result.Last(0);
            _Daily_TF = MarketData.GetSeries(TimeFrame.Daily).Close.Last(0);

            //Step 2

            //Step 3
            _AND = ((_RSI > _RSI_OverBought_Level) && (_Weekly_TF > _Moving_Average) && (_Daily_TF > _Moving_Average) && (_Four_Hour_TF > _Moving_Average) && (_One_Hour_TF > _Moving_Average) && (_RSI4Hr > _RSI_OverBought_Level));
            _AND_2 = ((_One_Hour_TF < _Moving_Average) && (_Four_Hour_TF < _Moving_Average) && (_Daily_TF < _Moving_Average) && (_Weekly_TF < _Moving_Average) && (_RSI < _RSI_OverSold_Level) && (_RSI4Hr < _RSI_OverSold_Level));

            //Step 4
            if (_AND_2)
                _Sell = Sell(2, _LotSize, 1, 0, 1, _ProfitTargetPips, 0, _MaxOpenTrade, _MaxFreqMinis, "");
            if (_AND)
                _Buy = Buy(1, _LotSize, 1, 0, 1, _ProfitTargetPips, 0, _MaxOpenTrade, _MaxFreqMinis, "");

        }

        bool NoOrders(string symbolCode, double[] magicIndecies)
        {
            if (symbolCode == "")
                symbolCode = Symbol.Code;
            string[] labels = new string[magicIndecies.Length];
            for (int i = 0; i < magicIndecies.Length; i++)
            {
                labels[i] = "FxProQuant_" + magicIndecies[i].ToString("F0");
            }
            foreach (Position pos in Positions)
            {
                if (pos.SymbolCode != symbolCode)
                    continue;
                if (labels.Length == 0)
                    return false;
                foreach (var label in labels)
                {
                    if (pos.Label == label)
                        return false;
                }
            }
            foreach (PendingOrder po in PendingOrders)
            {
                if (po.SymbolCode != symbolCode)
                    continue;
                if (labels.Length == 0)
                    return false;
                foreach (var label in labels)
                {
                    if (po.Label == label)
                        return false;
                }
            }
            return true;
        }

        TriState _OpenPosition(double magicIndex, bool noOrders, string symbolCode, TradeType tradeType, double lots, double slippage, double? stopLoss, double? takeProfit, string comment)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            if (noOrders && Positions.Find("FxProQuant_" + magicIndex.ToString("F0"), symbol) != null)
                return new TriState();
            if (stopLoss < 1)
                stopLoss = null;
            if (takeProfit < 1)
                takeProfit = null;
            if (symbol.Digits == 5 || symbol.Digits == 3)
            {
                if (stopLoss != null)
                    stopLoss /= 10;
                if (takeProfit != null)
                    takeProfit /= 10;
                slippage /= 10;
            }
            int volume = Convert.ToInt32(lots * 100000);
            if (!ExecuteMarketOrder(tradeType, symbol, volume, "FxProQuant_" + magicIndex.ToString("F0"), stopLoss, takeProfit, slippage, comment).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            return true;
        }

        TriState _SendPending(double magicIndex, bool noOrders, string symbolCode, PendingOrderType poType, TradeType tradeType, double lots, int priceAction, double priceValue, double? stopLoss, double? takeProfit,
        DateTime? expiration, string comment)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            if (noOrders && PendingOrders.__Find("FxProQuant_" + magicIndex.ToString("F0"), symbol) != null)
                return new TriState();
            if (stopLoss < 1)
                stopLoss = null;
            if (takeProfit < 1)
                takeProfit = null;
            if (symbol.Digits == 5 || symbol.Digits == 3)
            {
                if (stopLoss != null)
                    stopLoss /= 10;
                if (takeProfit != null)
                    takeProfit /= 10;
            }
            int volume = Convert.ToInt32(lots * 100000);
            double targetPrice;
            switch (priceAction)
            {
                case 0:
                    targetPrice = priceValue;
                    break;
                case 1:
                    targetPrice = symbol.Bid - priceValue * symbol.TickSize;
                    break;
                case 2:
                    targetPrice = symbol.Bid + priceValue * symbol.TickSize;
                    break;
                case 3:
                    targetPrice = symbol.Ask - priceValue * symbol.TickSize;
                    break;
                case 4:
                    targetPrice = symbol.Ask + priceValue * symbol.TickSize;
                    break;
                default:
                    targetPrice = priceValue;
                    break;
            }
            if (expiration.HasValue && (expiration.Value.Ticks == 0 || expiration.Value == DateTime.Parse("1970.01.01 00:00:00")))
                expiration = null;
            if (poType == PendingOrderType.Limit)
            {
                if (!PlaceLimitOrder(tradeType, symbol, volume, targetPrice, "FxProQuant_" + magicIndex.ToString("F0"), stopLoss, takeProfit, expiration, comment).IsSuccessful)
                {
                    Thread.Sleep(400);
                    return false;
                }
                return true;
            }
            else if (poType == PendingOrderType.Stop)
            {
                if (!PlaceStopOrder(tradeType, symbol, volume, targetPrice, "FxProQuant_" + magicIndex.ToString("F0"), stopLoss, takeProfit, expiration, comment).IsSuccessful)
                {
                    Thread.Sleep(400);
                    return false;
                }
                return true;
            }
            return new TriState();
        }

        TriState _ModifyPosition(double magicIndex, string symbolCode, int slAction, double slValue, int tpAction, double tpValue)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            var pos = Positions.Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (pos == null)
                return new TriState();
            double? sl, tp;
            if (slValue == 0)
                sl = null;
            else
            {
                switch (slAction)
                {
                    case 0:
                        sl = pos.StopLoss;
                        break;
                    case 1:
                        if (pos.TradeType == TradeType.Buy)
                            sl = pos.EntryPrice - slValue * symbol.TickSize;
                        else
                            sl = pos.EntryPrice + slValue * symbol.TickSize;
                        break;
                    case 2:
                        sl = slValue;
                        break;
                    default:
                        sl = pos.StopLoss;
                        break;
                }
            }
            if (tpValue == 0)
                tp = null;
            else
            {
                switch (tpAction)
                {
                    case 0:
                        tp = pos.TakeProfit;
                        break;
                    case 1:
                        if (pos.TradeType == TradeType.Buy)
                            tp = pos.EntryPrice + tpValue * symbol.TickSize;
                        else
                            tp = pos.EntryPrice - tpValue * symbol.TickSize;
                        break;
                    case 2:
                        tp = tpValue;
                        break;
                    default:
                        tp = pos.TakeProfit;
                        break;
                }
            }
            if (!ModifyPosition(pos, sl, tp).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            return true;
        }

        TriState _ModifyPending(double magicIndex, string symbolCode, int slAction, double slValue, int tpAction, double tpValue, int priceAction, double priceValue, int expirationAction, DateTime? expiration)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            var po = PendingOrders.__Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (po == null)
                return new TriState();
            double targetPrice;
            double? sl, tp;
            if (slValue == 0)
                sl = null;
            else
            {
                switch (slAction)
                {
                    case 0:
                        sl = po.StopLoss;
                        break;
                    case 1:
                        if (po.TradeType == TradeType.Buy)
                            sl = po.TargetPrice - slValue * symbol.TickSize;
                        else
                            sl = po.TargetPrice + slValue * symbol.TickSize;
                        break;
                    case 2:
                        sl = slValue;
                        break;
                    default:
                        sl = po.StopLoss;
                        break;
                }
            }
            if (tpValue == 0)
                tp = null;
            else
            {
                switch (tpAction)
                {
                    case 0:
                        tp = po.TakeProfit;
                        break;
                    case 1:
                        if (po.TradeType == TradeType.Buy)
                            tp = po.TargetPrice + tpValue * symbol.TickSize;
                        else
                            tp = po.TargetPrice - tpValue * symbol.TickSize;
                        break;
                    case 2:
                        tp = tpValue;
                        break;
                    default:
                        tp = po.TakeProfit;
                        break;
                }
            }
            switch (priceAction)
            {
                case 0:
                    targetPrice = po.TargetPrice;
                    break;
                case 1:
                    targetPrice = priceValue;
                    break;
                case 2:
                    targetPrice = po.TargetPrice + priceValue * symbol.TickSize;
                    break;
                case 3:
                    targetPrice = po.TargetPrice - priceValue * symbol.TickSize;
                    break;
                case 4:
                    targetPrice = symbol.Bid - priceValue * symbol.TickSize;
                    break;
                case 5:
                    targetPrice = symbol.Bid + priceValue * symbol.TickSize;
                    break;
                case 6:
                    targetPrice = symbol.Ask - priceValue * symbol.TickSize;
                    break;
                case 7:
                    targetPrice = symbol.Ask + priceValue * symbol.TickSize;
                    break;
                default:
                    targetPrice = po.TargetPrice;
                    break;
            }
            if (expiration.HasValue && (expiration.Value.Ticks == 0 || expiration.Value == DateTime.Parse("1970.01.01 00:00:00")))
                expiration = null;
            if (expirationAction == 0)
                expiration = po.ExpirationTime;
            if (!ModifyPendingOrder(po, targetPrice, sl, tp, expiration).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            return true;
        }

        TriState _ClosePosition(double magicIndex, string symbolCode, double lots)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            var pos = Positions.Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (pos == null)
                return new TriState();
            TradeResult result;
            if (lots == 0)
            {
                result = ClosePosition(pos);
            }
            else
            {
                int volume = Convert.ToInt32(lots * 100000);
                result = ClosePosition(pos, volume);
            }
            if (!result.IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            return true;
        }

        TriState _DeletePending(double magicIndex, string symbolCode)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            var po = PendingOrders.__Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (po == null)
                return new TriState();
            if (!CancelPendingOrder(po).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            return true;
        }

        bool _OrderStatus(double magicIndex, string symbolCode, int test)
        {
            Symbol symbol = (Symbol.Code == symbolCode) ? Symbol : MarketData.GetSymbol(symbolCode);
            var pos = Positions.Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (pos != null)
            {
                if (test == 0)
                    return true;
                if (test == 1)
                    return true;
                if (test == 3)
                    return pos.TradeType == TradeType.Buy;
                if (test == 4)
                    return pos.TradeType == TradeType.Sell;
            }
            var po = PendingOrders.__Find("FxProQuant_" + magicIndex.ToString("F0"), symbol);
            if (po != null)
            {
                if (test == 0)
                    return true;
                if (test == 2)
                    return true;
                if (test == 3)
                    return po.TradeType == TradeType.Buy;
                if (test == 4)
                    return po.TradeType == TradeType.Sell;
                if (test == 5)
                    return po.OrderType == PendingOrderType.Limit;
                if (test == 6)
                    return po.OrderType == PendingOrderType.Stop;
            }
            return false;
        }

        int TimeframeToInt(TimeFrame tf)
        {
            if (tf == TimeFrame.Minute)
                return 1;
            else if (tf == TimeFrame.Minute2)
                return 2;
            else if (tf == TimeFrame.Minute3)
                return 3;
            else if (tf == TimeFrame.Minute4)
                return 4;
            else if (tf == TimeFrame.Minute5)
                return 5;
            else if (tf == TimeFrame.Minute10)
                return 10;
            else if (tf == TimeFrame.Minute15)
                return 15;
            else if (tf == TimeFrame.Minute30)
                return 30;
            else if (tf == TimeFrame.Hour)
                return 60;
            else if (tf == TimeFrame.Hour4)
                return 240;
            else if (tf == TimeFrame.Daily)
                return 1440;
            else if (tf == TimeFrame.Weekly)
                return 10080;
            else if (tf == TimeFrame.Monthly)
                return 43200;
            return 1;
        }

        TriState Sell(double magicIndex, double Lots, int StopLossMethod, double stopLossValue, int TakeProfitMethod, double takeProfitValue, double Slippage, double MaxOpenTrades, double MaxFrequencyMins, string TradeComment)
        {
            double? stopLossPips, takeProfitPips;
            int numberOfOpenTrades = 0;
            var res = new TriState();

            foreach (Position pos in Positions.FindAll("FxProQuant_" + magicIndex.ToString("F0"), Symbol))
            {
                numberOfOpenTrades++;
            }

            if (MaxOpenTrades > 0 && numberOfOpenTrades >= MaxOpenTrades)
                return res;

            if (MaxFrequencyMins > 0)
            {
                if (((TimeSpan)(Server.Time - LastTradeExecution)).TotalMinutes < MaxFrequencyMins)
                    return res;

                foreach (Position pos in Positions.FindAll("FxProQuant_" + magicIndex.ToString("F0"), Symbol))
                {
                    if (((TimeSpan)(Server.Time - pos.EntryTime)).TotalMinutes < MaxFrequencyMins)
                        return res;
                }
            }

            int pipAdjustment = Convert.ToInt32(Symbol.PipSize / Symbol.TickSize);

            if (stopLossValue > 0)
            {
                if (StopLossMethod == 0)
                    stopLossPips = stopLossValue / pipAdjustment;
                else if (StopLossMethod == 1)
                    stopLossPips = stopLossValue;
                else
                    stopLossPips = (stopLossValue - Symbol.Bid) / Symbol.PipSize;
            }
            else
                stopLossPips = null;

            if (takeProfitValue > 0)
            {
                if (TakeProfitMethod == 0)
                    takeProfitPips = takeProfitValue / pipAdjustment;
                else if (TakeProfitMethod == 1)
                    takeProfitPips = takeProfitValue;
                else
                    takeProfitPips = (Symbol.Bid - takeProfitValue) / Symbol.PipSize;
            }
            else
                takeProfitPips = null;

            Slippage /= pipAdjustment;

            long volume = Symbol.NormalizeVolume(Lots * 100000, RoundingMode.ToNearest);

            if (!ExecuteMarketOrder(TradeType.Sell, Symbol, volume, "FxProQuant_" + magicIndex.ToString("F0"), stopLossPips, takeProfitPips, Slippage, TradeComment).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }

            LastTradeExecution = Server.Time;
            return true;
        }


        TriState Simple_Trailing_Stop(double magicIndex, int WaitForProfit, double TrailingStopPoints, double MinAdjustmentPoints)
        {
            double pnlPoints = 0;
            double newSl;
            var res = new TriState();

            foreach (Position pos in Positions.FindAll("FxProQuant_" + magicIndex.ToString("F0"), Symbol))
            {
                if (pos.TradeType == TradeType.Buy)
                {
                    if (WaitForProfit == 0)
                    {
                        pnlPoints = (Symbol.Bid - pos.EntryPrice) / Symbol.TickSize;
                        if (pnlPoints < TrailingStopPoints)
                            continue;
                    }

                    newSl = Math.Round(Symbol.Bid - TrailingStopPoints * Symbol.TickSize, Symbol.Digits);

                    if (pos.StopLoss != null)
                    {
                        if (newSl <= pos.StopLoss)
                            continue;
                        if (newSl <= pos.StopLoss + MinAdjustmentPoints * Symbol.TickSize)
                            continue;
                    }

                    var result = ModifyPosition(pos, newSl, pos.TakeProfit);
                    if (result.IsSuccessful && res.IsNonExecution)
                        res = true;
                    else
                    {
                        Thread.Sleep(400);
                        res = false;
                    }
                }
                else
                {
                    if (WaitForProfit == 0)
                    {
                        pnlPoints = (pos.EntryPrice - Symbol.Ask) / Symbol.TickSize;
                        if (pnlPoints < TrailingStopPoints)
                            continue;
                    }

                    newSl = Math.Round(Symbol.Ask + TrailingStopPoints * Symbol.TickSize, Symbol.Digits);

                    if (pos.StopLoss != null)
                    {
                        if (newSl >= pos.StopLoss)
                            continue;
                        if (newSl >= pos.StopLoss - MinAdjustmentPoints * Symbol.TickSize)
                            continue;
                    }

                    var result = ModifyPosition(pos, newSl, pos.TakeProfit);
                    if (result.IsSuccessful && res.IsNonExecution)
                        res = true;
                    else
                    {
                        Thread.Sleep(400);
                        res = false;
                    }
                }
            }
            return res;
        }


        TriState Buy(double magicIndex, double Lots, int StopLossMethod, double stopLossValue, int TakeProfitMethod, double takeProfitValue, double Slippage, double MaxOpenTrades, double MaxFrequencyMins, string TradeComment)
        {
            double? stopLossPips, takeProfitPips;
            int numberOfOpenTrades = 0;
            var res = new TriState();

            foreach (Position pos in Positions.FindAll("FxProQuant_" + magicIndex.ToString("F0"), Symbol))
            {
                numberOfOpenTrades++;
            }

            if (MaxOpenTrades > 0 && numberOfOpenTrades >= MaxOpenTrades)
                return res;

            if (MaxFrequencyMins > 0)
            {
                if (((TimeSpan)(Server.Time - LastTradeExecution)).TotalMinutes < MaxFrequencyMins)
                    return res;

                foreach (Position pos in Positions.FindAll("FxProQuant_" + magicIndex.ToString("F0"), Symbol))
                {
                    if (((TimeSpan)(Server.Time - pos.EntryTime)).TotalMinutes < MaxFrequencyMins)
                        return res;
                }
            }

            int pipAdjustment = Convert.ToInt32(Symbol.PipSize / Symbol.TickSize);

            if (stopLossValue > 0)
            {
                if (StopLossMethod == 0)
                    stopLossPips = stopLossValue / pipAdjustment;
                else if (StopLossMethod == 1)
                    stopLossPips = stopLossValue;
                else
                    stopLossPips = (Symbol.Ask - stopLossValue) / Symbol.PipSize;
            }
            else
                stopLossPips = null;

            if (takeProfitValue > 0)
            {
                if (TakeProfitMethod == 0)
                    takeProfitPips = takeProfitValue / pipAdjustment;
                else if (TakeProfitMethod == 1)
                    takeProfitPips = takeProfitValue;
                else
                    takeProfitPips = (takeProfitValue - Symbol.Ask) / Symbol.PipSize;
            }
            else
                takeProfitPips = null;

            Slippage /= pipAdjustment;
            long volume = Symbol.NormalizeVolume(Lots * 100000, RoundingMode.ToNearest);

            if (!ExecuteMarketOrder(TradeType.Buy, Symbol, volume, "FxProQuant_" + magicIndex.ToString("F0"), stopLossPips, takeProfitPips, Slippage, TradeComment).IsSuccessful)
            {
                Thread.Sleep(400);
                return false;
            }
            LastTradeExecution = Server.Time;
            return true;
        }

    }
}

public struct TriState
{
    public static readonly TriState NonExecution = new TriState(0);
    public static readonly TriState False = new TriState(-1);
    public static readonly TriState True = new TriState(1);
    sbyte value;
    TriState(int value)
    {
        this.value = (sbyte)value;
    }
    public bool IsNonExecution
    {
        get { return value == 0; }
    }
    public static implicit operator TriState(bool x)
    {
        return x ? True : False;
    }
    public static TriState operator ==(TriState x, TriState y)
    {
        if (x.value == 0 || y.value == 0)
            return NonExecution;
        return x.value == y.value ? True : False;
    }
    public static TriState operator !=(TriState x, TriState y)
    {
        if (x.value == 0 || y.value == 0)
            return NonExecution;
        return x.value != y.value ? True : False;
    }
    public static TriState operator !(TriState x)
    {
        return new TriState(-x.value);
    }
    public static TriState operator &(TriState x, TriState y)
    {
        return new TriState(x.value < y.value ? x.value : y.value);
    }
    public static TriState operator |(TriState x, TriState y)
    {
        return new TriState(x.value > y.value ? x.value : y.value);
    }
    public static bool operator true(TriState x)
    {
        return x.value > 0;
    }
    public static bool operator false(TriState x)
    {
        return x.value < 0;
    }
    public static implicit operator bool(TriState x)
    {
        return x.value > 0;
    }
    public override bool Equals(object obj)
    {
        if (!(obj is TriState))
            return false;
        return value == ((TriState)obj).value;
    }
    public override int GetHashCode()
    {
        return value;
    }
    public override string ToString()
    {
        if (value > 0)
            return "True";
        if (value < 0)
            return "False";
        return "NonExecution";
    }
}

public static class PendingEx
{
    public static PendingOrder __Find(this cAlgo.API.PendingOrders pendingOrders, string label, Symbol symbol)
    {
        foreach (PendingOrder po in pendingOrders)
        {
            if (po.SymbolCode == symbol.Code && po.Label == label)
                return po;
        }
        return null;
    }
}
