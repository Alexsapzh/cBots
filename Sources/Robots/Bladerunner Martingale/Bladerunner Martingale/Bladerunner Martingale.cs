
using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System.Linq;
using cAlgo.Lib;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot("Bladerunner Martingale", AccessRights = AccessRights.None)]
    public class BladerunnerMartingale : Robot
    {
        #region Parameters
        [Parameter()]
        public TimeFrame GlobalTimeFrame { get; set; }

        [Parameter("Money Management (%)", DefaultValue = 1.6, MinValue = 0)]
        public double MoneyManagement { get; set; }

        [Parameter("Take Profit", DefaultValue = 5, MinValue = 2)]
        public double TakeProfit { get; set; }

        [Parameter("Stop Loss Factor", DefaultValue = 5.5, MinValue = 0.1)]
        public double StopLossFactor { get; set; }

        [Parameter("Martingale", DefaultValue = 0.5, MinValue = 0)]
        public double MartingaleCoeff { get; set; }

        [Parameter("Max Orders", DefaultValue = 2, MinValue = 2)]
        public int MaxOrders { get; set; }

        [Parameter(DefaultValue = 10000, Step = 1000, MinValue = 1)]
        public int Volume { get; set; }

        [Parameter("Minimum Global Candle Size", DefaultValue = 0, MinValue = 0)]
        public int MinimumGlobalCandleSize { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableStopLoss { get; set; }

        [Parameter(DefaultValue = 100, MinValue = 1, MaxValue = 1000, Step = 1)]
        public double StopLoss { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, MaxValue = 400, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 1, MaxValue = 400, Step = 1)]
        public double BreakEvenGain { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableTrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, MaxValue = 1000, Step = 1)]
        public double TrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, MaxValue = 1000, Step = 1)]
        public double TrailingStart { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnterOnSyncSignalOnly { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ExitOnOppositeSignal { get; set; }

        [Parameter("Data Source")]
        public DataSeries Price { get; set; }

        [Parameter("Exp Fast Periods", DefaultValue = 5, MinValue = 1, MaxValue = 550, Step = 1)]
        public int FastPeriods { get; set; }

        [Parameter("Period", DefaultValue = 9, MinValue = 1, MaxValue = 100, Step = 1)]
        public int Period { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26, MinValue = 1, MaxValue = 100, Step = 1)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12, MinValue = 1, MaxValue = 100, Step = 1)]
        public int ShortCycle { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Parameter("ADX Trend Strength", DefaultValue = 20, MinValue = 10, MaxValue = 30, Step = 1)]
        public int trend { get; set; }
        #endregion

        private bool isRobotStopped;
        private string botName;
        // le label permet de s'y retrouver parmis toutes les instances possibles.
        private string instanceLabel;

        private double stopLoss;
        private double firstLot;
        private StaticPosition corner_position;
        private string BotVersion = "1.3.2.0";
        private bool DEBUG;


        private MacdHistogram _macd;
        private ExponentialMovingAverage _emaFast;
        //private string label;

        private ADXR _adx;

        private const int indexOffset = 0;
        private int index;
        private CandlestickTendencyII tendency;

        public bool globalTendencyWasLong;
        public bool globalTendencyWasShort;
        public bool localTendencyWasLong;
        public bool localTendencyWasShort;




        public bool globalTendencyIsLong
        {
            get { return tendency.GlobalTrendSignal[index] > 0; }
        }
        public bool localTendencyIsLong
        {
            get { return tendency.LocalTrendSignal[index] > 0; }
        }
        public bool globalTendencyIsShort
        {
            get { return tendency.GlobalTrendSignal[index] < 0; }
        }
        public bool localTendencyIsShort
        {
            get { return tendency.LocalTrendSignal[index] < 0; }
        }

        public bool longSignal
        {
            get { return adxrtrend && adxrlong && macdlong && localTendencyIsLong && globalTendencyIsLong; }
        }
        public bool shortSignal
        {
            get { return adxrtrend && adxrshort && macdshort && localTendencyIsShort && globalTendencyIsShort; }
        }

        public bool macdlong
        {
            get { return _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising(); }
        }

        public bool macdshort
        {
            get { return _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling(); }
        }

        public bool emalong
        {
            get { return MarketSeries.Open.LastValue > _emaFast.Result.LastValue && _emaFast.Result.IsRising() && MarketSeries.Close.Last(1) > MarketSeries.Close.Last(2); }
        }
        public bool emashort
        {
            get { return MarketSeries.Open.LastValue < _emaFast.Result.LastValue && _emaFast.Result.IsFalling() && MarketSeries.Close.Last(1) < MarketSeries.Close.Last(2); }
        }

        public bool adxrlong
        {
            get { return _adx.diminus.Last(1) > _adx.diplus.Last(1) && _adx.diminus.Last(0) <= _adx.diplus.Last(0); }
        }
        public bool adxrshort
        {
            get { return _adx.diminus.Last(1) < _adx.diplus.Last(1) && _adx.diminus.Last(0) >= _adx.diplus.Last(0); }
        }
        public bool adxrtrend
        {
            get { return _adx.adxr.LastValue >= trend; }
        }
        public bool adxrrising
        {
            get { return _adx.adxr.IsRising(); }
        }


        protected override void OnStart()
        {
            tendency = Indicators.GetIndicator<CandlestickTendencyII>(GlobalTimeFrame, MinimumGlobalCandleSize);
            index = MarketSeries.Close.Count - 1;
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            _emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            _adx = Indicators.GetIndicator<ADXR>(Source, interval);

            //Positions.Opened += PositionsOnOpened;
            //Positions.Closed += PositionsOnClosed;

            DEBUG = true;
            botName = ToString();
            instanceLabel = botName + "-" + BotVersion + "-" + Symbol.Code + "-" + TimeFrame.ToString();

            stopLoss = TakeProfit * StopLossFactor;
            Positions.Opened += OnPositionOpened;

            int corner = 1;

            switch (corner)
            {
                case 1:
                    corner_position = StaticPosition.TopLeft;
                    break;
                case 2:
                    corner_position = StaticPosition.TopRight;
                    break;
                case 3:
                    corner_position = StaticPosition.BottomLeft;
                    break;
                case 4:
                    corner_position = StaticPosition.BottomRight;
                    break;
            }

            if (!DEBUG)
                ChartObjects.DrawText("BotVersion", botName + " Version : " + BotVersion, corner_position);


            Print("The current symbol has PipSize of: {0}", Symbol.PipSize);
            Print("The current symbol has PipValue of: {0}", Symbol.PipValue);
            Print("The current symbol has TickSize: {0}", Symbol.TickSize);
            Print("The current symbol has TickSValue: {0}", Symbol.TickValue);



        }


        protected override void OnTick()
        {
            index = MarketSeries.Close.Count - 1;

            if (Trade.IsExecuting)
                return;

            Position[] positions = GetPositions();

            if (positions.Length > 0 && isRobotStopped)
                return;
            else
                isRobotStopped = false;

            if (positions.Length == 0)
            {
                // Calcule le volume en fonction du money management pour un risque maximum et un stop loss donné.
                // Ne tient pas compte des risques sur d'autres positions ouvertes du compte de trading utilisé
                double maxVolume = this.moneyManagement(MoneyManagement, stopLoss);
                firstLot = maxVolume / (MaxOrders + (MartingaleCoeff * MaxOrders * (MaxOrders - 1)) / 2.0);

                if (firstLot <= 0)
                    throw new System.ArgumentException(String.Format("the 'first lot' : {0} parameter must be positive and not null", firstLot));
                else
                    SendFirstOrder(firstLot);
            }
            else

                ControlSeries();
        }

        protected override void OnError(Error CodeOfError)
        {
            if (CodeOfError.Code == ErrorCode.NoMoney)
            {
                isRobotStopped = true;
                Print("ERROR!!! No money for order open, robot is stopped!");
            }
            else if (CodeOfError.Code == ErrorCode.BadVolume)
            {
                isRobotStopped = true;
                Print("ERROR!!! Bad volume for order open, robot is stopped!");
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            double? stopLossPrice = null;
            double? takeProfitPrice = null;

            switch (GetPositionsSide())
            {
                case 0:
                    double averageBuyPrice = GetAveragePrice(TradeType.Buy);
                    takeProfitPrice = averageBuyPrice + TakeProfit * Symbol.PipSize;
                    stopLossPrice = averageBuyPrice - stopLoss * Symbol.PipSize;
                    break;
                case 1:
                    double averageSellPrice = GetAveragePrice(TradeType.Sell);
                    takeProfitPrice = averageSellPrice - TakeProfit * Symbol.PipSize;
                    stopLossPrice = averageSellPrice + stopLoss * Symbol.PipSize;
                    break;
            }

            if (stopLossPrice.HasValue || takeProfitPrice.HasValue)
            {
                Position[] positions = GetPositions();

                foreach (Position position in positions)
                {
                    if (stopLossPrice != position.StopLoss || takeProfitPrice != position.TakeProfit)
                        ModifyPosition(position, stopLossPrice, takeProfitPrice);
                }
            }
        }

        private void SendFirstOrder(double OrderVolume)
        {
            switch (GetSignal())
            {
                case 0:
                    executeOrder(TradeType.Buy, OrderVolume);
                    break;
                case 1:
                    executeOrder(TradeType.Sell, OrderVolume);
                    break;
            }
        }

        private void ControlSeries()
        {
            Position[] positions = GetPositions();

            if (positions.Length < MaxOrders)
            {
                long volume = Symbol.NormalizeVolume(firstLot * (1 + MartingaleCoeff * positions.Length), RoundingMode.ToNearest);
                int countOfBars = (int)(25.0 / positions.Length);

                int pipstep = GetDynamicPipstep(countOfBars, MaxOrders - 1);
                int positionSide = GetPositionsSide();

                switch (positionSide)
                {
                    case 0:
                        double lastBuyPrice = GetLastPrice(TradeType.Buy);

                        if (!DEBUG)
                            ChartObjects.DrawHorizontalLine("gridBuyLine", lastBuyPrice - pipstep * Symbol.PipSize, Colors.Green, 2);

                        if (Symbol.Ask < lastBuyPrice - pipstep * Symbol.PipSize)
                            executeOrder(TradeType.Buy, volume);
                        break;

                    case 1:
                        double lastSellPrice = GetLastPrice(TradeType.Sell);

                        if (!DEBUG)
                            ChartObjects.DrawHorizontalLine("gridSellLine", lastSellPrice + pipstep * Symbol.PipSize, Colors.Red, 2);

                        if (Symbol.Bid > lastSellPrice + pipstep * Symbol.PipSize)
                            executeOrder(TradeType.Sell, volume);
                        break;
                }
            }

            if (!DEBUG)
                ChartObjects.DrawText("MaxDrawdown", "MaxDrawdown: " + Math.Round(GetMaxDrawdown(), 2) + " Percent", corner_position);
        }

        // You can modify the condition of entry here.
        private int GetSignal()
        {
            int Result = -1;

            if (shortSignal)
                Result = 1;

            if (longSignal)
                Result = 0;
            return Result;
        }


        private TradeResult executeOrder(TradeType tradeType, double volume)
        {
            //Print("normalized volume : {0}", Symbol.NormalizeVolume(volume, RoundingMode.ToNearest));
            return ExecuteMarketOrder(tradeType, Symbol, Symbol.NormalizeVolume(volume, RoundingMode.ToNearest), instanceLabel);
        }

        private Position[] GetPositions()
        {
            return Positions.FindAll(instanceLabel, Symbol);
        }

        private double GetAveragePrice(TradeType TypeOfTrade)
        {
            double Result = Symbol.Bid;
            double AveragePrice = 0;
            long count = 0;

            foreach (Position position in GetPositions())
            {
                if (position.TradeType == TypeOfTrade)
                {
                    AveragePrice += position.EntryPrice * position.Volume;
                    count += position.Volume;
                }
            }

            if (AveragePrice > 0 && count > 0)
                Result = AveragePrice / count;

            return Result;
        }

        private int GetPositionsSide()
        {
            int Result = -1;
            int BuySide = 0, SellSide = 0;
            Position[] positions = GetPositions();

            foreach (Position position in positions)
            {
                if (position.TradeType == TradeType.Buy)
                    BuySide++;

                if (position.TradeType == TradeType.Sell)
                    SellSide++;
            }

            if (BuySide == positions.Length)
                Result = 0;

            if (SellSide == positions.Length)
                Result = 1;

            return Result;
        }

        private int GetDynamicPipstep(int CountOfBars, int division)
        {
            int Result;
            double HighestPrice = 0, LowestPrice = 0;
            int StartBar = MarketSeries.Close.Count - 2 - CountOfBars;
            int EndBar = MarketSeries.Close.Count - 2;

            for (int i = StartBar; i < EndBar; i++)
            {
                if (HighestPrice == 0 && LowestPrice == 0)
                {
                    HighestPrice = MarketSeries.High[i];
                    LowestPrice = MarketSeries.Low[i];
                    continue;
                }

                if (MarketSeries.High[i] > HighestPrice)
                    HighestPrice = MarketSeries.High[i];

                if (MarketSeries.Low[i] < LowestPrice)
                    LowestPrice = MarketSeries.Low[i];
            }

            Result = (int)((HighestPrice - LowestPrice) / Symbol.PipSize / division);

            return Result;
        }

        private double savedMaxBalance;
        private List<double> drawdown = new List<double>();
        private double GetMaxDrawdown()
        {
            savedMaxBalance = Math.Max(savedMaxBalance, Account.Balance);

            drawdown.Add((savedMaxBalance - Account.Balance) / savedMaxBalance * 100);
            drawdown.Sort();

            double maxDrawdown = drawdown[drawdown.Count - 1];

            return maxDrawdown;
        }

        private double GetLastPrice(TradeType tradeType)
        {
            double LastPrice = 0;

            foreach (Position position in GetPositions())
            {
                if (tradeType == TradeType.Buy)
                    if (position.TradeType == tradeType)
                    {
                        if (LastPrice == 0)
                        {
                            LastPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < LastPrice)
                            LastPrice = position.EntryPrice;
                    }

                if (tradeType == TradeType.Sell)
                    if (position.TradeType == tradeType)
                    {
                        if (LastPrice == 0)
                        {
                            LastPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice > LastPrice)
                            LastPrice = position.EntryPrice;
                    }
            }

            return LastPrice;
        }



    }
}
