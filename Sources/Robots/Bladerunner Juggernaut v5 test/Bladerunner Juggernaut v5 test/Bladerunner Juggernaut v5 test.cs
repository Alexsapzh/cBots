#region Licence
//The MIT License (MIT)
//Copyright (c) 2014 abdallah HACID, https://www.facebook.com/ab.hacid

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//and associated documentation files (the "Software"), to deal in the Software without restriction,
//including without limitation the rights to use, copy, modify, merge, publish, distribute,
//sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or
//substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// Project Hosting for Open Source Software on Github : https://github.com/abhacid/Robot_Forex
#endregion

#region Description
//The Mechanic Bot uses the Candlestick Tendency indicator II as a decision driver for entering trades. The basic idea 
//is to trade higher order timeframe with current local timeframe. It trades a single position at a time and 
//has mechanisms for trailing stops and basic money management (Stop Losss and Take Profit.

#endregion
using System;
using System.Linq;
using System.Reflection;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using cAlgo.Lib;

namespace cAlgo
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BladerunnerJuggernautv5test : Robot
    {
        #region cBot Parameters
        [Parameter()]
        public TimeFrame GlobalTimeFrame { get; set; }

        [Parameter()]
        public TimeFrame GlobalTimeFrame2 { get; set; }

        [Parameter("Minimum Global Candle Size", DefaultValue = 0, MinValue = 0)]
        public int MinimumGlobalCandleSize { get; set; }

        [Parameter("Minimum Global Candle Size 2", DefaultValue = 0, MinValue = 0)]
        public int MinimumGlobalCandleSize2 { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableStopLoss { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableTrailingStop { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableTakeProfit { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnterOnSyncSignalOnly { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ExitOnOppositeSignal { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Parameter("ADX Trend Strength", DefaultValue = 20, MinValue = 1, MaxValue = 30, Step = 1)]
        public int trend { get; set; }

        //[Parameter("ADX Trend Reverse", DefaultValue = 30, MinValue = 20, MaxValue = 70, Step = 1)]
        //public int trendrev { get; set; }

        [Parameter("Period", DefaultValue = 1)]
        public int HeikenPeriod { get; set; }

        //[Parameter("COG Length", DefaultValue = 10)]
        //public int Length { get; set; }

        [Parameter("Fisher Length", DefaultValue = 13, MinValue = 2)]
        public int Len { get; set; }

        [Parameter("TP Factor", DefaultValue = 2.43, MinValue = 0.1)]
        public double TPFactor { get; set; }

        [Parameter("Volatility Factor", DefaultValue = 2.7, MinValue = 0.1)]
        public double VolFactor { get; set; }

        [Parameter("Trail Factor", DefaultValue = 2.7, MinValue = -10)]
        public double TrailFactor { get; set; }

        [Parameter("Trail Factor2", DefaultValue = 2.7, MinValue = -10)]
        public double TrailFactor2 { get; set; }

        [Parameter("ATR Period", DefaultValue = 20, MinValue = 1)]
        public int AtrPeriod { get; set; }

        [Parameter("ATR MAType", DefaultValue = 4)]
        public MovingAverageType AtrMaType { get; set; }

        [Parameter("MM Factor", DefaultValue = 5, MinValue = 0.1)]
        public double MMFactor { get; set; }

        //KAMA
        [Parameter("KAMA Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("KAMA FastPeriod", DefaultValue = 2)]
        public int Fast { get; set; }

        [Parameter("KAMA SlowPeriod", DefaultValue = 30)]
        public int Slow { get; set; }

        #endregion


        #region cBot Variables
        double minPipsATR;
        double maxPipsATR;
        double ceilSignalPipsATR;

        private OnBalanceVolume _onBalanceVolume;
        private KAMASignal _kama;
        private PipsATRIndicator pipsATR;
        private FisherSignal Fisher;
        //private CenterOfGravityOscillator COG;
        private HeikenAshi2 _heiken;
        private ADXRSignal _adx;
        private string _botName;
        private string _botVersion = Assembly.GetExecutingAssembly().FullName.Split(',')[1].Replace("Version=", "").Trim();
        private string _instanceLabel;

        private CandlestickTendencyII tendency;
        private CandlestickTendencyII_2 tendency2;
        private MarketSeries ADXRSeries;

        private int savedIndex;
        #endregion

        protected override void OnStart()
        {

            ADXRSeries = MarketData.GetSeries(TimeFrame.Hour4);
            _botName = ToString();
            _instanceLabel = string.Format("{0}-{1}-{2}-{3}-{4}", _botName, _botVersion, Symbol.Code, TimeFrame.ToString(), GlobalTimeFrame.ToString());
            tendency = Indicators.GetIndicator<CandlestickTendencyII>(GlobalTimeFrame, MinimumGlobalCandleSize);
            tendency2 = Indicators.GetIndicator<CandlestickTendencyII_2>(GlobalTimeFrame2, MinimumGlobalCandleSize2);
            //_emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            _adx = Indicators.GetIndicator<ADXRSignal>(Source, interval);
            _heiken = Indicators.GetIndicator<HeikenAshi2>(1);
            _kama = Indicators.GetIndicator<KAMASignal>(Source, Fast, Slow, Period);
            Fisher = Indicators.GetIndicator<FisherSignal>(Len);
            //COG = Indicators.GetIndicator<CenterOfGravityOscillator>(Length);
            pipsATR = Indicators.GetIndicator<PipsATRIndicator>(TimeFrame, AtrPeriod, AtrMaType);
            _onBalanceVolume = Indicators.OnBalanceVolume(Source);

            minPipsATR = pipsATR.Result.Minimum(pipsATR.Result.Count);
            maxPipsATR = pipsATR.Result.Maximum(pipsATR.Result.Count);


        }

        protected override void OnTick()
        {

            manageTrailingStops();

            int index = MarketSeries.Close.Count - 1;

            if (index <= savedIndex)
                return;

            savedIndex = index;

            Position position = CurrentPosition();

            if (ExitOnOppositeSignal && position != null && isCloseSignal(index))
                ClosePosition(position);

            TradeType? tradeType = signal(index);
            if (tradeType.HasValue)
                executeOrder(tradeType.Value);


            minPipsATR = Math.Min(minPipsATR, pipsATR.Result.LastValue);
            maxPipsATR = Math.Max(maxPipsATR, pipsATR.Result.LastValue);
            ceilSignalPipsATR = minPipsATR + ((maxPipsATR - minPipsATR) / 9) * VolFactor;

            RefreshData();
        }


        private TradeType? signal(int index)
        {

            TradeType? tradeType = null;

            // this occur when the preceding boolean test instruction is true or there is no active position.

            //if (CurrentPosition() == null)

            if (pipsATR.Result.LastValue <= ceilSignalPipsATR)
            {
                bool isShortSignal = tendency.GlobalTrendSignal[index] < 0;
                bool isLongSignal = tendency.GlobalTrendSignal[index] > 0;
                bool isShortPreviewSignal = tendency.LocalTrendSignal[index - 1] < 0;
                bool isLongPreviewSignal = tendency.LocalTrendSignal[index - 1] > 0;

                bool isShortSignal2 = tendency2.GlobalTrendSignal[index] < 0;
                bool isLongSignal2 = tendency2.GlobalTrendSignal[index] > 0;
                bool isShortPreviewSignal2 = tendency2.LocalTrendSignal[index - 1] < 0;
                bool isLongPreviewSignal2 = tendency2.LocalTrendSignal[index - 1] > 0;

                //bool COGlong = COG.cg[index] > COG.lag[index];
                //bool COGshort = COG.cg[index] < COG.lag[index];

                bool Fisherlong = Fisher.FISH[index] > Fisher.TRIGGER[index];
                bool Fishershort = Fisher.FISH[index] < Fisher.TRIGGER[index];

                //bool Fisherlong = Fisher.FISH[index] > Fisher.TRIGGER[index] && Fisher.FISHHOUR[index] > Fisher.TRIGGERHOUR[index] && Fisher.FISHHOUR4[index] > Fisher.TRIGGERHOUR4[index];
                //bool Fishershort = Fisher.FISH[index] < Fisher.TRIGGER[index] && Fisher.FISHHOUR[index] < Fisher.TRIGGERHOUR[index] && Fisher.FISHHOUR4[index] < Fisher.TRIGGERHOUR[index];


                bool emalong = _heiken.xClose[index] > _heiken.xOpen[index];
                bool emashort = _heiken.xClose[index] < _heiken.xOpen[index];

                // Heiken Ashi EMA HTF Signal
                bool emalong1 = _kama.KAMA5.IsRising();
                bool emashort1 = _kama.KAMA5.IsFalling();
                bool emalong2 = _kama.KAMA15.IsRising();
                bool emashort2 = _kama.KAMA15.IsFalling();
                bool emalong3 = _heiken.xClose[index] > _kama.KAMA15[index];
                bool emashort3 = _heiken.xClose[index] < _kama.KAMA15[index];
                bool emalong4 = _heiken.xClose[index] > _kama.KAMA5[index];
                bool emashort4 = _heiken.xClose[index] < _kama.KAMA5[index];

                //ADXR Trend Signal
                bool adxrtrend = _adx.ADXRHOUR[index] >= trend && _adx.ADXRHOUR.IsRising();

                //bool adxrlong = _adx.diplus[index] > _adx.diminus[index];
                //bool adxrshort = _adx.diminus[index] > _adx.diplus[index];

                bool adxrlong = _adx.DIPLUS[index] > _adx.DIMINUS[index] && _adx.DIPLUSHOUR[index] > _adx.DIMINUSHOUR[index] && _adx.DIPLUSHOUR4[index] > _adx.DIMINUSHOUR4[index];
                bool adxrshort = _adx.DIMINUS[index] > _adx.DIPLUS[index] && _adx.DIMINUSHOUR[index] > _adx.DIPLUSHOUR[index] && _adx.DIMINUSHOUR4[index] > _adx.DIPLUSHOUR4[index];

                bool obvlong = _onBalanceVolume.Result.IsRising();
                bool obvshort = _onBalanceVolume.Result.IsFalling();

                if (EnterOnSyncSignalOnly)
                {
                    if (isShortPreviewSignal && isShortPreviewSignal2 && isLongSignal && isLongSignal2 && emalong && emalong1 && emalong2 && emalong3 && emalong4 && adxrlong && adxrtrend && Fisherlong && obvlong)
                        tradeType = TradeType.Buy;
                    else if (isLongPreviewSignal && isLongPreviewSignal2 && isShortSignal && isShortSignal2 && emashort && emashort1 && emashort2 && emashort3 && emashort4 && adxrshort && adxrtrend && Fishershort && obvshort)
                        tradeType = TradeType.Sell;
                }
                else
                {

                    if (isLongSignal && isLongSignal2 && emalong1 && emalong2 && emalong3 && emalong4 && adxrlong && Fisherlong && obvlong && adxrtrend)
                        tradeType = TradeType.Buy;
                    else if (isShortSignal && isShortSignal2 && emashort && emashort1 && emashort2 && emashort3 && emashort4 && adxrshort && Fishershort && obvshort && adxrtrend)
                        tradeType = TradeType.Sell;

                }
            }

            return tradeType;

        }


        protected TradeResult executeOrder(TradeType tradeType)
        {

            double volatility = pipsATR.Result.lastRealValue(0);
            double stopLoss = (VolFactor * volatility);
            double volume = this.moneyManagement(MMFactor / 100, stopLoss);
            long normalizedVolume = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);

            RefreshData();
            //if (pipsATR.Result.LastValue <= ceilSignalPipsATR)

            if (!EnableStopLoss && EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, normalizedVolume, _instanceLabel, null, TPFactor * stopLoss);

            if (!EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, normalizedVolume, _instanceLabel, null, null);

            if (EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, normalizedVolume, _instanceLabel, stopLoss, null);

            return ExecuteMarketOrder(tradeType, Symbol, normalizedVolume, _instanceLabel, stopLoss, TPFactor * stopLoss);
        }

        private Position CurrentPosition()
        {
            return Positions.Find(_instanceLabel);
        }

        private bool isShort()
        {
            Position position = CurrentPosition();
            return position != null && position.TradeType == TradeType.Sell;
        }

        private bool isLong()
        {
            Position position = CurrentPosition();
            return position != null && position.TradeType == TradeType.Buy;
        }


        private bool isCloseSignal(int index)
        {

            bool _isShortSignal = tendency.GlobalTrendSignal[index] < 0;
            bool _isLongSignal = tendency.GlobalTrendSignal[index] > 0;

            bool _isShortSignal2 = tendency2.GlobalTrendSignal[index] < 0;
            bool _isLongSignal2 = tendency2.GlobalTrendSignal[index] > 0;

            //bool _COGlong = COG.cg[index] > COG.lag[index];
            //bool _COGshort = COG.cg[index] < COG.lag[index];
            bool _Fisherlong = Fisher.FISH[index] > Fisher.TRIGGER[index];
            bool _Fishershort = Fisher.FISH[index] < Fisher.TRIGGER[index];

            //bool _Fisherlong = Fisher.FISH[index] > Fisher.TRIGGER[index] && Fisher.FISHHOUR[index] > Fisher.TRIGGERHOUR[index] && Fisher.FISHHOUR4[index] > Fisher.TRIGGERHOUR4[index];
            //bool _Fishershort = Fisher.FISH[index] < Fisher.TRIGGER[index] && Fisher.FISHHOUR[index] < Fisher.TRIGGERHOUR[index] && Fisher.FISHHOUR4[index] < Fisher.TRIGGERHOUR[index];

            bool _emalong = _heiken.xClose[index] > _heiken.xOpen[index];
            bool _emashort = _heiken.xClose[index] < _heiken.xOpen[index];

            // Heiken Ashi EMA HTF Signal
            bool _emalong1 = _kama.KAMA5.IsRising();
            bool _emashort1 = _kama.KAMA5.IsFalling();
            bool _emalong2 = _kama.KAMA15.IsRising();
            bool _emashort2 = _kama.KAMA15.IsFalling();
            bool _emalong3 = _heiken.xClose[index] > _kama.KAMA15[index];
            bool _emashort3 = _heiken.xClose[index] < _kama.KAMA15[index];
            bool _emalong4 = _heiken.xClose[index] > _kama.KAMA5[index];
            bool _emashort4 = _heiken.xClose[index] < _kama.KAMA5[index];

            //bool _adxrtrendrev = _adx.adxrhour[index] >= trendrev && _adx.adxrhour.IsFalling();
            bool _adxrlong = _adx.DIPLUS[index] > _adx.DIMINUS[index] && _adx.DIPLUSHOUR[index] > _adx.DIMINUSHOUR[index] && _adx.DIPLUSHOUR4[index] > _adx.DIMINUSHOUR4[index];
            bool _adxrshort = _adx.DIMINUS[index] > _adx.DIPLUS[index] && _adx.DIMINUSHOUR[index] > _adx.DIPLUSHOUR[index] && _adx.DIMINUSHOUR4[index] > _adx.DIPLUSHOUR4[index];

            bool _obvlong = _onBalanceVolume.Result.IsRising();
            bool _obvshort = _onBalanceVolume.Result.IsFalling();

            Position position = CurrentPosition();

            if (position != null)
                return ((position.TradeType == TradeType.Sell) ? _isLongSignal && _isLongSignal2 && _emalong && _emalong1 && _emalong2 && _emalong3 && _emalong4 && _adxrlong && _Fisherlong && _obvlong : _isShortSignal && _isShortSignal2 && _emashort && _emashort1 && _emashort2 && _emashort3 && _emashort4 && _adxrshort && _Fishershort && _obvshort);
            else
                return false;
        }


        protected void manageTrailingStops()
        {
            double _volatility = pipsATR.Result.lastRealValue(0);
            double trailstop = TrailFactor * _volatility;
            double _trailstart = TrailFactor2 * _volatility;


            if (!EnableTrailingStop)
                return;

            foreach (Position position in Positions.FindAll(_instanceLabel))
            {
                if (position.Pips >= _trailstart)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        var newStopLoss = Symbol.Bid - trailstop * Symbol.PipSize;
                        if (position.StopLoss < newStopLoss)
                            ModifyPosition(position, newStopLoss, position.TakeProfit);
                        //Print("Trailing Stop Loss triggered...BUY");
                    }

                    else if (position.TradeType == TradeType.Sell)
                    {
                        var newStopLoss = Symbol.Ask + trailstop * Symbol.PipSize;
                        if (position.StopLoss > newStopLoss)
                            ModifyPosition(position, newStopLoss, position.TakeProfit);
                        //Print("Trailing Stop Loss triggered...SELL");
                    }
                }
            }
        }

    }
}
