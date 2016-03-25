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
// Author		: gorkroitor 
// link			: http://ctdn.com/algos/cbots/show/657
// Modified		: by Abdallah HACID

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

namespace cAlgo
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BladerunnerMechanic : Robot
    {
        #region cBot Parameters
        [Parameter()]
        public TimeFrame GlobalTimeFrame { get; set; }

        [Parameter("Minimum Global Candle Size", DefaultValue = 0, MinValue = 0)]
        public int MinimumGlobalCandleSize { get; set; }

        [Parameter(DefaultValue = 10000, Step = 1000, MinValue = 1000)]
        public int Volume { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableStopLoss { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 1, Step = 1)]
        public double StopLoss { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableTrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, Step = 1)]
        public double TrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, Step = 1)]
        public double TrailingStart { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableTakeProfit { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 0)]
        public int TakeProfit { get; set; }

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

        [Parameter("ADX Trend Reverse", DefaultValue = 30, MinValue = 20, MaxValue = 50, Step = 1)]
        public int trendrev { get; set; }

        [Parameter("CCI Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int CCI_period { get; set; }

        [Parameter("Period", DefaultValue = 1)]
        public int HeikenPeriod { get; set; }

        [Parameter("EMA HFT Period", DefaultValue = 20, MinValue = 1, MaxValue = 250, Step = 1)]
        public int EMAPeriod { get; set; }
        #endregion


        #region cBot Variables
        private ExponentialSignal _emasignal;
        private HeikenAshi2 _heiken;
        private CCI _cci;
        private ADXR _adx;
        private MacdHistogram _macd;
        private ExponentialMovingAverage _emaFast;
        private string _botName;
        private string _botVersion = Assembly.GetExecutingAssembly().FullName.Split(',')[1].Replace("Version=", "").Trim();
        private string _instanceLabel;

        private CandlestickTendencyII tendency;

        private int savedIndex;
        #endregion

        protected override void OnStart()
        {
            _botName = ToString();
            _instanceLabel = string.Format("{0}-{1}-{2}-{3}-{4}", _botName, _botVersion, Symbol.Code, TimeFrame.ToString(), GlobalTimeFrame.ToString());
            tendency = Indicators.GetIndicator<CandlestickTendencyII>(GlobalTimeFrame, MinimumGlobalCandleSize);
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            _emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            _adx = Indicators.GetIndicator<ADXR>(Source, interval);
            _cci = Indicators.GetIndicator<CCI>(CCI_period);
            _heiken = Indicators.GetIndicator<HeikenAshi2>(1);
            _emasignal = Indicators.GetIndicator<ExponentialSignal>(20);

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
        }


        private TradeType? signal(int index)
        {

            TradeType? tradeType = null;

            // this occur when the preceding boolean test instruction is true or there is no active position.

            if (CurrentPosition() == null)
            {
                bool isShortSignal = tendency.GlobalTrendSignal[index] < 0;
                bool isLongSignal = tendency.GlobalTrendSignal[index] > 0;
                bool isShortPreviewSignal = tendency.LocalTrendSignal[index - 1] < 0;
                bool isLongPreviewSignal = tendency.LocalTrendSignal[index - 1] > 0;
                bool macdlong = _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising();
                bool macdshort = _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling();

                // The Best EMA
                //bool emalong = MarketSeries.Open.LastValue > _emaFast.Result.LastValue && _emaFast.Result.IsRising() && MarketSeries.Close.Last(1) > MarketSeries.Close.Last(2);
                //bool emashort = MarketSeries.Open.LastValue < _emaFast.Result.LastValue && _emaFast.Result.IsFalling() && MarketSeries.Close.Last(1) < MarketSeries.Close.Last(2);

                // Heiken Ashi EMA
                //bool emalong = _heiken.xClose[index] > _heiken.xOpen[index];
                //bool emashort = _heiken.xClose[index] < _heiken.xOpen[index];
                bool emalong = _heiken.xOpen[index] > _emaFast.Result[index] && _emaFast.Result.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
                bool emashort = _heiken.xOpen[index] < _emaFast.Result[index] && _emaFast.Result.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

                // Heiken Ashi EMA HTF Signal
                bool emalong1 = _heiken.xOpen[index] > _emasignal.EMAhour[index] && _emasignal.EMAhour.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
                bool emashort1 = _heiken.xOpen[index] < _emasignal.EMAhour[index] && _emasignal.EMAhour.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];
                bool emalong2 = _heiken.xOpen[index] > _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
                bool emashort2 = _heiken.xOpen[index] < _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

                //bool emalong = MarketSeries.Close.Last(1) > _emaFast.Result.Last(1);
                //bool emashort = MarketSeries.Close.Last(1) < _emaFast.Result.Last(1);
                //bool ematrendlong = _emaFast.Result.LastValue > _emaFast.Result.Last(2);
                //bool ematrendshort = _emaFast.Result.LastValue < _emaFast.Result.Last(2);

                //ADXR Trend Signal
                bool adxrtrend = _adx.adxr[index] >= trend && _adx.adxr.IsRising();

                //bool adxrlong = _adx.diminus[index - 1] > _adx.diplus[index - 1] && _adx.diminus[index] <= _adx.diplus[index];
                //bool adxrshort = _adx.diminus[index - 1] < _adx.diplus[index - 1] && _adx.diminus[index] >= _adx.diplus[index];

                bool adxrlong = _adx.diplus[index] > _adx.diminus[index];
                bool adxrshort = _adx.diminus[index] > _adx.diplus[index];
                bool CCIlong = _cci.CCIa[index] >= 0;
                bool CCIshort = _cci.CCIa[index] <= 0;

                //var longPosition = Positions.Find(_instanceLabel, Symbol, TradeType.Buy);
                //var shortPosition = Positions.Find(_instanceLabel, Symbol, TradeType.Sell);

                if (EnterOnSyncSignalOnly)
                {
                    if (isShortPreviewSignal && macdlong && isLongSignal && emalong1 && emalong2 && adxrlong && adxrtrend && CCIlong)
                        tradeType = TradeType.Buy;
                    else if (isLongPreviewSignal && macdshort && isShortSignal && emashort1 && emashort2 && adxrshort && adxrtrend && CCIshort)
                        tradeType = TradeType.Sell;
                }
                else
                {

                    if (isLongSignal && macdlong && emalong1 && emalong2 && adxrlong && adxrtrend && CCIlong)
                        tradeType = TradeType.Buy;
                    else if (isShortSignal && macdshort && emashort1 && emashort2 && adxrshort && adxrtrend && CCIshort)
                        tradeType = TradeType.Sell;

                }
            }

            return tradeType;

        }


        protected TradeResult executeOrder(TradeType tradeType)
        {

            if (!EnableStopLoss && EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, Volume, _instanceLabel, null, TakeProfit);

            if (!EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, Volume, _instanceLabel, null, null);

            if (EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(tradeType, Symbol, Volume, _instanceLabel, StopLoss, null);

            return ExecuteMarketOrder(tradeType, Symbol, Volume, _instanceLabel, StopLoss, TakeProfit);
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
            bool _macdlong = _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising();
            bool _macdshort = _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling();

            //The Best EMA
            //var _emalong = MarketSeries.Open.LastValue > _emaFast.Result.LastValue && _emaFast.Result.IsRising() && MarketSeries.Close.Last(1) > MarketSeries.Close.Last(2);
            //var _emashort = MarketSeries.Open.LastValue < _emaFast.Result.LastValue && _emaFast.Result.IsFalling() && MarketSeries.Close.Last(1) < MarketSeries.Close.Last(2);

            //Heiken EMA
            //var _emalong = _heiken.xClose[index] > _heiken.xOpen[index];
            //var _emashort = _heiken.xClose[index] < _heiken.xOpen[index];
            bool _emalong = _heiken.xOpen[index] > _emaFast.Result[index] && _emaFast.Result.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool _emashort = _heiken.xOpen[index] < _emaFast.Result[index] && _emaFast.Result.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

            // Heiken Ashi EMA HTF Signal
            bool _emalong1 = _heiken.xOpen[index] > _emasignal.EMAhour[index] && _emasignal.EMAhour.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool _emashort1 = _heiken.xOpen[index] < _emasignal.EMAhour[index] && _emasignal.EMAhour.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];
            bool _emalong2 = _heiken.xOpen[index] > _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool _emashort2 = _heiken.xOpen[index] < _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

            //bool _ematrendlong = _emaFast.Result.LastValue > _emaFast.Result.Last(2);
            //bool _ematrendshort = _emaFast.Result.LastValue < _emaFast.Result.Last(2);
            bool _adxrtrendrev = _adx.adxr[index] >= trendrev && _adx.adxr.IsFalling();
            bool _adxrlong = _adx.diplus[index] > _adx.diminus[index];
            bool _adxrshort = _adx.diminus[index] > _adx.diplus[index];
            bool _CCIlong = _cci.CCIa[index] >= 0;
            bool _CCIshort = _cci.CCIa[index] >= 0;

            Position position = CurrentPosition();

            if (position != null)
                return ((position.TradeType == TradeType.Sell) ? _isLongSignal && _emalong1 && _emalong2 && _macdlong && _adxrlong && _adxrtrendrev && _CCIlong : _isShortSignal && _emashort1 && _emashort2 && _macdshort && _adxrshort && _adxrtrendrev && _CCIshort);
            else
                return false;
        }


        //private bool isCloseSignal(int index)
        //{
        //Position position = CurrentPosition();

        //if (position != null)
        //return ((position.TradeType == TradeType.Sell) ? tendency.GlobalTrendSignal[index] > 0 : tendency.GlobalTrendSignal[index] < 0);
        //else
        //return false;
        //}


        protected void manageTrailingStops()
        {
            if (!EnableTrailingStop)
                return;

            foreach (Position position in Positions.FindAll(_instanceLabel))
            {
                if (position.Pips >= TrailingStart)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        var newStopLoss = Symbol.Bid - TrailingStop * Symbol.PipSize;
                        if (position.StopLoss < newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                    else if (position.TradeType == TradeType.Sell)
                    {
                        var newStopLoss = Symbol.Ask + TrailingStop * Symbol.PipSize;
                        if (position.StopLoss > newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                }
            }
        }

    }
}
