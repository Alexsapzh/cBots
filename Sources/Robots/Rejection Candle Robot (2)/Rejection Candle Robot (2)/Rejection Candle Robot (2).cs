using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RejectionCandleRobot : Robot
    {
        [Parameter("Start Percent", DefaultValue = 50, MaxValue = 50)]
        public int startPercent { get; set; }

        [Parameter("Check Previous Candles", DefaultValue = true)]
        public bool checkPreviousCandles { get; set; }

        [Parameter("Previous Opposite Candles", DefaultValue = 1, MinValue = 1, MaxValue = 10)]
        public int previousOppositeCandles { get; set; }

        [Parameter("Left Space", DefaultValue = true)]
        public bool checkLeftSpace { get; set; }

        [Parameter("Left Space Period", DefaultValue = 1)]
        public int leftSpacePeriod { get; set; }

        [Parameter("Check Volume", DefaultValue = false)]
        public bool checkVolume { get; set; }

        [Parameter("Volume Period", DefaultValue = 1, MinValue = 1, MaxValue = 50)]
        public int volumePeriod { get; set; }

        [Parameter("ATR Based SL", DefaultValue = true)]
        public bool atrSl { get; set; }

        [Parameter("ATR Periods", DefaultValue = 14)]
        public int atrPeriod { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 2)]
        public int atrMultiplier { get; set; }

        [Parameter("Bollinger Bands Filter", DefaultValue = false)]
        public bool bBandFilter { get; set; }

        [Parameter("Bollinger Bands Deviations", DefaultValue = 2)]
        public double bBandDeviations { get; set; }

        [Parameter("Bollinger Bands Periods", DefaultValue = 20)]
        public int bBandPeriods { get; set; }

        [Parameter("MA Trend Filter", DefaultValue = false)]
        public bool maFilter { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Parameter("Source")]
        public DataSeries SourceSeries { get; set; }

        //KAMA
        [Parameter("KAMA Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("KAMA FastPeriod", DefaultValue = 2)]
        public int Fast { get; set; }

        [Parameter("KAMA SlowPeriod", DefaultValue = 30)]
        public int Slow { get; set; }

        [Parameter("MAs Distance(pip)", DefaultValue = 1)]
        public int maDistance { get; set; }

        [Parameter("Check ACR", DefaultValue = false)]
        public bool checkACR { get; set; }

        [Parameter("ACR Period", DefaultValue = 10, MinValue = 2, MaxValue = 100)]
        public int acrPeriod { get; set; }

        [Parameter("Hourly ACR Filter", DefaultValue = false)]
        public bool hourlyFilter { get; set; }

        [Parameter("RR Based Exit", DefaultValue = false)]
        public bool rrBasedExit { get; set; }

        [Parameter("RR", DefaultValue = 2)]
        public int rrAmount { get; set; }

        [Parameter("Candle Based Exit", DefaultValue = false)]
        public bool candleBasedExit { get; set; }

        [Parameter("Same Exit", DefaultValue = false)]
        public bool sameExit { get; set; }

        [Parameter("Stop Trailing", DefaultValue = true)]
        public bool slTrail { get; set; }

        [Parameter("RR Based SL Trailing", DefaultValue = true)]
        public bool rrSlTrail { get; set; }

        [Parameter("Reward Multiplier", DefaultValue = 2.5)]
        public double rrMultiplier { get; set; }

        [Parameter("Time Filter", DefaultValue = false)]
        public bool timeFilter { get; set; }

        [Parameter("Start Hour", DefaultValue = 7, MinValue = 0, MaxValue = 23)]
        public int startHour { get; set; }

        [Parameter("End Hour", DefaultValue = 13, MinValue = 0, MaxValue = 23)]
        public int endHour { get; set; }

        [Parameter("Order Distance", DefaultValue = 5, MinValue = 0.1, MaxValue = 20)]
        public double orderDistance { get; set; }

        [Parameter("% Risk Per Trade", DefaultValue = 0.5, MinValue = 0.1, MaxValue = 10.0)]
        public double riskPercentage { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        public int tradeIndex = 0;

        public double stopLoss = 0.0;

        public double? takeProfit = 0.0;

        public bool breakEven = false;

        //private MovingAverage slowMa;
        //private MovingAverage mediumMa;
        //private MovingAverage fastMa;
        private AverageTrueRange atr;
        private BollingerBands bBand;
        private KAMASignal _kama;

        protected override void OnStart()
        {
            //fastMa = Indicators.MovingAverage(SourceSeries, FastPeriods, MAType);
            //mediumMa = Indicators.MovingAverage(SourceSeries, mediumPeriods, MAType);
            //slowMa = Indicators.MovingAverage(SourceSeries, SlowPeriods, MAType);
            atr = Indicators.AverageTrueRange(atrPeriod, MAType);
            bBand = Indicators.BollingerBands(SourceSeries, bBandPeriods, bBandDeviations, MAType);
            _kama = Indicators.GetIndicator<KAMASignal>(Source, Fast, Slow, Period);
        }

        protected override void OnTick()
        {
            var position = Positions.Find(string.Format("{0}", tradeIndex), Symbol);
            if (position != null && position.NetProfit > 0 && slTrail)
            {
                stopTrailing(position);
            }
        }


        // This method will be called after creation of each new bar instead of ticks
        protected override void OnBar()
        {
            int index = MarketSeries.Close.Count - 1;

            foreach (var order in PendingOrders)
            {
                if (order.Label != string.Format("{0}", index) && order.SymbolCode == Symbol.Code)
                {
                    CancelPendingOrder(order);
                }
            }

            var previousCandleHigh = MarketSeries.High.Last(2);
            var previousCandleLow = MarketSeries.Low.Last(2);
            var previousCandleOpen = MarketSeries.Open.Last(2);
            var previousCandleClose = MarketSeries.Close.Last(2);
            var previousCandleRange = Math.Round((previousCandleHigh - previousCandleLow), Symbol.Digits);

            var RejectionCandleHigh = MarketSeries.High.Last(1);
            var RejectionCandleLow = MarketSeries.Low.Last(1);
            var RejectionCandleOpen = MarketSeries.Open.Last(1);
            var RejectionCandleClose = MarketSeries.Close.Last(1);
            var RejectionCandleRange = Math.Round((RejectionCandleHigh - RejectionCandleLow), Symbol.Digits);


            // Rejection Check
            int Rejection = isItRejection(RejectionCandleHigh, RejectionCandleLow, RejectionCandleOpen, RejectionCandleClose);
            bool candleRejection = false;
            bool bullishRejection = false;
            bool bearishRejection = false;
            if (Rejection == 1)
            {
                candleRejection = true;
                bullishRejection = true;
            }
            else if (Rejection == 2)
            {
                candleRejection = true;
                bearishRejection = true;
            }



            // Checking number of Previous opposite candles
            bool previousCandleCheck = false;
            if (checkPreviousCandles)
            {

                if (bullishRejection)
                {
                    for (int i = 2; i <= previousOppositeCandles + 1; i++)
                    {
                        if (MarketSeries.Close.Last(i) < MarketSeries.Open.Last(i))
                        {
                            previousCandleCheck = true;
                        }
                        else
                        {
                            previousCandleCheck = false;
                            break;
                        }
                    }
                }

                if (bearishRejection)
                {
                    for (int i = 2; i <= previousOppositeCandles + 1; i++)
                    {
                        if (MarketSeries.Close.Last(i) > MarketSeries.Open.Last(i))
                        {
                            previousCandleCheck = true;
                        }
                        else
                        {
                            previousCandleCheck = false;
                            break;
                        }
                    }
                }

            }
            else
                previousCandleCheck = true;

            // Left Space
            bool leftSpace = false;
            if (checkLeftSpace)
            {
                if (bullishRejection)
                {
                    if (previousCandleLow < RejectionCandleOpen && previousCandleLow > RejectionCandleLow)
                    {
                        for (int i = leftSpacePeriod + 1; i > 1; i--)
                        {
                            if (MarketSeries.Low.Last(i) >= previousCandleLow)
                            {
                                leftSpace = true;
                            }
                            else
                            {
                                leftSpace = false;
                                break;
                            }
                        }

                    }
                    else if (previousCandleLow >= RejectionCandleOpen)
                    {
                        for (int i = leftSpacePeriod + 1; i > 1; i--)
                        {
                            if (MarketSeries.Low.Last(i) >= RejectionCandleOpen)
                            {
                                leftSpace = true;
                            }
                            else
                            {
                                leftSpace = false;
                                break;
                            }

                        }
                    }

                }
                else if (bearishRejection)
                {
                    if (previousCandleHigh > RejectionCandleOpen && previousCandleHigh < RejectionCandleHigh)
                    {
                        for (int i = leftSpacePeriod + 1; i > 1; i--)
                        {
                            if (MarketSeries.High.Last(i) <= previousCandleHigh)
                            {
                                leftSpace = true;
                            }
                            else
                            {
                                leftSpace = false;
                                break;
                            }
                        }
                    }
                    else if (previousCandleHigh <= RejectionCandleOpen)
                    {
                        for (int i = leftSpacePeriod + 1; i > 1; i--)
                        {
                            if (MarketSeries.Low.Last(i) <= RejectionCandleOpen)
                            {
                                leftSpace = true;
                            }
                            else
                            {
                                leftSpace = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
                leftSpace = true;

            // Volume Check
            bool isVolumeOk = false;
            if (checkVolume)
            {
                for (int i = 2; i <= (volumePeriod + 1); i++)
                {
                    if (MarketSeries.TickVolume.Last(1) > MarketSeries.TickVolume.Last(i))
                        isVolumeOk = true;
                    else
                    {
                        isVolumeOk = false;
                        break;
                    }
                }
            }
            else
                isVolumeOk = true;


            // Acr Check
            bool isAcrOk = false;
            if (checkACR)
            {
                if (hourlyFilter)
                    isAcrOk = Acr(24 + MarketSeries.OpenTime.Last(1).Hour);
                else
                    isAcrOk = Acr(acrPeriod);
            }
            else
                isAcrOk = true;

            // Time Filter
            bool isTimeCorrect = false;
            if (timeFilter)
                isTimeCorrect = timeFilterCheck();
            else
                isTimeCorrect = true;

            // MA Filter
            bool isMaOk = false;
            if (maFilter)
            {
                bool mDistance = false;
                if (bullishRejection && _kama.KAMA15.Last(0) < Symbol.Ask && _kama.KAMA5.Last(0) < Symbol.Ask)
                {
                    mDistance = (_kama.KAMA15.Last(0) - _kama.KAMA5.Last(0)) * Math.Pow(10, Symbol.Digits - 1) >= maDistance;
                    if (mDistance)
                        isMaOk = true;
                }
                else if (bearishRejection && _kama.KAMA15.Last(0) > Symbol.Bid && _kama.KAMA5.Last(0) > Symbol.Bid)
                {
                    mDistance = (_kama.KAMA15.Last(0) - _kama.KAMA5.Last(0)) * Math.Pow(10, Symbol.Digits - 1) >= maDistance;
                    if (mDistance)
                        isMaOk = true;
                }
                else
                    isMaOk = false;
            }
            else
                isMaOk = true;

            // bBand Filter
            bool isbBandOk = false;
            if (bBandFilter)
            {
                if (bullishRejection && RejectionCandleHigh < bBand.Main.Last(1))
                    isbBandOk = true;
                else if (bearishRejection && RejectionCandleLow > bBand.Main.Last(1))
                    isbBandOk = true;

            }
            else
                isbBandOk = true;


            // If the order was executed then it will throw it to tradeManager method
            var position = Positions.Find(string.Format("{0}", tradeIndex), Symbol);
            if (position != null)
            {
                tradeManager(position);
                return;
            }

            // Placing The stop order
            if (candleRejection && previousCandleCheck && isAcrOk && isTimeCorrect && leftSpace && isVolumeOk && isMaOk && isbBandOk)
            {
                // Order Attributes
                if (atrSl)
                {
                    stopLoss = Math.Round((atr.Result.LastValue * Math.Pow(10, Symbol.Digits - 1)) * atrMultiplier, 1);
                }
                else
                    stopLoss = RejectionCandleRange * Math.Pow(10, Symbol.Digits - 1);


                if (rrBasedExit && !candleBasedExit)
                    takeProfit = stopLoss * rrAmount;

                long posVolume = PositionVolume(stopLoss);
                breakEven = false;
                string label = string.Format("{0}", index);
                if (bullishRejection)
                {
                    tradeIndex = index;
                    if (takeProfit.Value != 0.0)
                    {
                        PlaceStopOrder(TradeType.Buy, Symbol, posVolume, RejectionCandleHigh + (Symbol.PipSize * orderDistance), label, stopLoss, takeProfit.Value);
                        takeProfit = 0.0;
                    }
                    else
                        PlaceStopOrder(TradeType.Buy, Symbol, posVolume, RejectionCandleHigh + (Symbol.PipSize * orderDistance), label, stopLoss, null);
                }
                else if (bearishRejection)
                {
                    tradeIndex = index;
                    if (takeProfit.Value != 0.0)
                    {
                        PlaceStopOrder(TradeType.Sell, Symbol, posVolume, RejectionCandleLow - (Symbol.PipSize * orderDistance), label, stopLoss, takeProfit.Value);
                        takeProfit = 0.0;
                    }
                    else
                        PlaceStopOrder(TradeType.Sell, Symbol, posVolume, RejectionCandleLow - (Symbol.PipSize * orderDistance), label, stopLoss, null);
                }

            }
        }


        // Manage the trade
        private void tradeManager(Position position)
        {
            if (candleBasedExit && !rrBasedExit && position.TradeType == TradeType.Buy)
            {
                if (sameExit)
                {
                    if (isEngulfing(position.TradeType) || isRejection(position.TradeType) || isDoji() || isTwoOpposite(position.TradeType))
                        ClosePosition(position);

                }
                else
                {
                    if ((position.NetProfit > 0) && (isEngulfing(position.TradeType) || isRejection(position.TradeType) || isDoji() || isTwoOpposite(position.TradeType)))
                        ClosePosition(position);
                }

            }
            else if (candleBasedExit && !rrBasedExit && position.TradeType == TradeType.Sell)
            {
                if (sameExit)
                {
                    if (isEngulfing(position.TradeType) || isRejection(position.TradeType) || isDoji() || isTwoOpposite(position.TradeType))
                        ClosePosition(position);

                }
                else
                {
                    if ((position.NetProfit > 0) && (isEngulfing(position.TradeType) || isRejection(position.TradeType) || isDoji() || isTwoOpposite(position.TradeType)))
                        ClosePosition(position);
                }
            }


        }

        // It will trail SL to break even
        private void stopTrailing(Position pos)
        {
            double sl_pip = 0.0;
            if (pos.TradeType == TradeType.Buy)
            {
                sl_pip = (pos.EntryPrice - pos.StopLoss.Value) * Math.Pow(10, Symbol.Digits - 1);
                if (pos.Pips >= sl_pip && pos.StopLoss.Value < pos.EntryPrice && sl_pip >= stopLoss)
                    ModifyPosition(pos, ((sl_pip / 2) * Symbol.PipSize) + pos.StopLoss, null);
                if (pos.Pips >= (sl_pip * 1.5) && pos.StopLoss.Value < pos.EntryPrice && !breakEven)
                {
                    ModifyPosition(pos, pos.EntryPrice + Symbol.PipSize, null);
                    breakEven = true;
                }

                if (rrSlTrail && ((Symbol.Bid - pos.StopLoss.Value) * Math.Pow(10, Symbol.Digits - 1)) == (stopLoss * rrMultiplier))
                {
                    ModifyPosition(pos, pos.StopLoss.Value + (stopLoss * Symbol.PipSize), null);
                }
                if (isInsideBar())
                {
                    ModifyPosition(pos, MarketSeries.Low.Last(2), null);
                }

            }
            else if (pos.TradeType == TradeType.Sell)
            {
                sl_pip = (pos.StopLoss.Value - pos.EntryPrice) * Math.Pow(10, Symbol.Digits - 1);
                if (pos.Pips >= sl_pip && pos.StopLoss.Value > pos.EntryPrice && sl_pip >= stopLoss)
                    ModifyPosition(pos, pos.StopLoss - ((sl_pip / 2) * Symbol.PipSize), null);
                if (pos.Pips >= (sl_pip * 1.5) && pos.StopLoss.Value > pos.EntryPrice && !breakEven)
                {
                    ModifyPosition(pos, pos.EntryPrice - Symbol.PipSize, null);
                    breakEven = true;
                }

                if (rrSlTrail && ((pos.StopLoss.Value - Symbol.Bid) * Math.Pow(10, Symbol.Digits - 1)) == (stopLoss * rrMultiplier))
                {
                    ModifyPosition(pos, pos.StopLoss.Value - (stopLoss * Symbol.PipSize), null);
                }

                if (isInsideBar())
                {
                    ModifyPosition(pos, MarketSeries.High.Last(2), null);
                }

            }


        }

        // Candle Types for Exit
        private bool isEngulfing(TradeType type)
        {
            string currentCandle_type = null;
            string previousCandle_type = null;
            if (MarketSeries.Close.Last(1) > MarketSeries.Open.Last(1))
            {
                currentCandle_type = "Bullish";
            }
            else if (MarketSeries.Close.Last(1) < MarketSeries.Open.Last(1))
            {
                currentCandle_type = "Bearish";
            }

            if (MarketSeries.Close.Last(2) > MarketSeries.Open.Last(2))
            {
                previousCandle_type = "Bullish";
            }
            else if (MarketSeries.Close.Last(2) < MarketSeries.Open.Last(2))
            {
                previousCandle_type = "Bearish";
            }

            if (type == TradeType.Buy)
            {
                if (previousCandle_type == "Bullish" && currentCandle_type == "Bearish")
                {
                    if (MarketSeries.Open.Last(1) >= MarketSeries.Close.Last(2) && MarketSeries.Close.Last(1) <= MarketSeries.Open.Last(2))
                        return true;
                    else
                        return false;
                }


            }
            else if (type == TradeType.Sell)
            {
                if (previousCandle_type == "Bearish" && currentCandle_type == "Bullish")
                {
                    if (MarketSeries.Open.Last(1) <= MarketSeries.Close.Last(2) && MarketSeries.Close.Last(1) >= MarketSeries.Open.Last(2))
                        return true;
                    else
                        return false;
                }

            }
            return false;
        }


        private bool isRejection(TradeType type)
        {
            var candleHalf = Math.Abs(((MarketSeries.High.Last(1) - MarketSeries.Low.Last(1)) / 2) + MarketSeries.Low.Last(1));
            if (type == TradeType.Buy)
                if (isItRejection(MarketSeries.High.Last(1), MarketSeries.Low.Last(1), MarketSeries.Open.Last(1), MarketSeries.Close.Last(1)) == 2)
                    return true;
                else
                    return false;
            else if (type == TradeType.Sell)
                if (isItRejection(MarketSeries.High.Last(1), MarketSeries.Low.Last(1), MarketSeries.Open.Last(1), MarketSeries.Close.Last(1)) == 1)
                    return true;
                else
                    return false;
            return false;
        }

        private bool isDoji()
        {
            if (MarketSeries.Open.Last(1) == MarketSeries.Close.Last(1))
                return true;
            else
                return false;
        }

        private bool isInsideBar()
        {
            if (MarketSeries.High.Last(2) > MarketSeries.High.Last(1) && MarketSeries.Low.Last(2) < MarketSeries.Low.Last(1))
            {
                return true;
            }
            else
                return false;

        }

        private bool isTwoOpposite(TradeType type)
        {
            if (type == TradeType.Buy && MarketSeries.Open.Last(2) > MarketSeries.Close.Last(2) && MarketSeries.Open.Last(1) > MarketSeries.Close.Last(1) && MarketSeries.Low.Last(1) < MarketSeries.Low.Last(2))
                return true;
            else if (type == TradeType.Sell && MarketSeries.Open.Last(2) < MarketSeries.Close.Last(2) && MarketSeries.Open.Last(1) < MarketSeries.Close.Last(1) && MarketSeries.High.Last(1) > MarketSeries.High.Last(2))
                return true;
            else
                return false;

        }


        // Position volume calculator
        private long PositionVolume(double stopLossInPips)
        {

            double costPerPip = (double)((int)(Symbol.PipValue * 10000000)) / 100;
            double positionSizeForRisk = Math.Round((Account.Balance * riskPercentage / 100) / (stopLossInPips * costPerPip), 2);

            if (positionSizeForRisk < 0.01)
                positionSizeForRisk = 0.01;
            return Symbol.QuantityToVolume(positionSizeForRisk);

        }


        // Average previous candles range calculator
        private bool Acr(int period)
        {
            double range_current = 0;
            double range_acr = 0;


            range_current = (MarketSeries.High.Last(1) - MarketSeries.Low.Last(1)) * Math.Pow(10, Symbol.Digits - 1);
            range_current = Math.Round(range_current, 0);


            for (int i = period + 1; i > 1; i--)
                if (hourlyFilter && MarketSeries.OpenTime.Last(i).Hour >= startHour && MarketSeries.OpenTime.Last(i).Hour < endHour)
                    range_acr += (MarketSeries.High.Last(i) - MarketSeries.Low.Last(i)) * Math.Pow(10, Symbol.Digits - 1);
                else if (!hourlyFilter)
                    range_acr += (MarketSeries.High.Last(i) - MarketSeries.Low.Last(i)) * Math.Pow(10, Symbol.Digits - 1);

            range_acr /= period;
            range_acr = Math.Round(range_acr, 0);

            if (range_current > range_acr && range_acr != 0)
            {
                return true;
            }
            else
                return false;
        }

        // Checking Rejection
        private int isItRejection(double candleHigh, double candleLow, double candleOpen, double candleClose)
        {
            // Rule Number one: The candle open and close must be above or below the 50% of candle range
            bool rule1 = false;
            bool bullishRejection = false;
            bool bearishRejection = false;
            var candleRangePips = (candleHigh - candleLow) * Math.Pow(10, Symbol.Digits - 1);
            var candleStartPercent = (candleRangePips * startPercent) / 100;
            var candleSixtyFivePercent = (candleRangePips * 65) / 100;
            var candleSeventyPercent = (candleRangePips * 70) / 100;
            if (candleOpen >= ((candleStartPercent * Symbol.PipSize) + candleLow) && candleClose >= ((candleStartPercent * Symbol.PipSize) + candleLow))
            {
                rule1 = true;
                bullishRejection = true;

            }
            else if (candleOpen <= ((candleStartPercent * Symbol.PipSize) + candleLow) && candleClose <= ((candleStartPercent * Symbol.PipSize) + candleLow))
            {
                rule1 = true;
                bearishRejection = true;
            }
            // Rule Number Two: If candle Open is between 50% - 60%(Bullish) or 50% - 40%(Bearish) of the candle range then the close must be above 70% or below 30%
            bool rule2 = false;
            if (rule1 && bullishRejection && (candleOpen >= ((candleStartPercent * Symbol.PipSize) + candleLow) && candleOpen <= ((candleSixtyFivePercent * Symbol.PipSize) + candleLow)))
            {
                var candleSeventyPrice = (candleSeventyPercent * Symbol.PipSize) + candleLow;
                if (candleClose >= candleSeventyPrice)
                    rule2 = true;
            }
            else if (rule1 && bearishRejection && (candleOpen <= (candleHigh - (candleStartPercent * Symbol.PipSize)) && candleOpen >= (candleHigh - (candleSixtyFivePercent * Symbol.PipSize))))
            {
                var candleSeventyPrice = candleHigh - (candleSeventyPercent * Symbol.PipSize);
                if (candleClose <= candleSeventyPrice)
                    rule2 = true;
            }
            else
                rule2 = true;
            if (rule1 && rule2 && bullishRejection)
                return 1;
            else if (rule1 && rule2 && bearishRejection)
                return 2;
            else
                return 0;

        }

        // Checking the opening time of candle
        private bool timeFilterCheck()
        {
            if (TimeFrame == TimeFrame.Hour4 && MarketSeries.OpenTime.Last(1).Hour != 2)
                return true;
            else if (MarketSeries.OpenTime.Last(1).Hour >= startHour && MarketSeries.OpenTime.Last(1).Hour <= endHour)
                return true;
            else
                return false;
        }


        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
