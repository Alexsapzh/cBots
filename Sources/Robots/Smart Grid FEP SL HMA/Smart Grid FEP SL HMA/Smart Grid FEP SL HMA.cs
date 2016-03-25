//+------------------------------------------------------------------+
//|                                                  Smart Grid      |
//|                                      Copyright 2014, MD SAIF     |
//|                                   http://www.facebook.com/cls.fx |
//+------------------------------------------------------------------+
//-Grid trader cBot based on Bar-Time & Trend. For range market & 15 minute TimeFrame is best.

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SmartGridSL : Robot
    {
        [Parameter("Buy", DefaultValue = true)]
        public bool Buy { get; set; }

        [Parameter("Sell", DefaultValue = true)]
        public bool Sell { get; set; }

        [Parameter("Pip Step", DefaultValue = 10, MinValue = 1)]
        public int PipStep { get; set; }

        [Parameter("First Volume", DefaultValue = 1000, MinValue = 1000, Step = 1000)]
        public int FirstVolume { get; set; }

        [Parameter("Volume Exponent", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0)]
        public double VolumeExponent { get; set; }

        [Parameter("Max Spread", DefaultValue = 3.0)]
        public double MaxSpread { get; set; }

        [Parameter("Average TP", DefaultValue = 3, MinValue = 1)]
        public int AverageTP { get; set; }

        [Parameter("Stop Loss", DefaultValue = 10)]
        public int StopLoss { get; set; }

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

        [Parameter("CCI Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int CCI_period { get; set; }

        [Parameter("Heiken Period", DefaultValue = 1)]
        public int HeikenPeriod { get; set; }

        [Parameter("EMA HFT Period", DefaultValue = 20, MinValue = 1, MaxValue = 250, Step = 1)]
        public int EMAPeriod { get; set; }


        private string Label = "clsSL";
        private Position position;
        private DateTime tc_31;
        private DateTime tc_32;
        private int gi_21;
        private double sp_d;
        private bool is_12 = true;
        private bool cStop = false;

        private HeikenAshi2 _heiken;
        private CCI _cci;
        private ADXR _adx;
        private MacdHistogram _macd;
        private ExponentialMovingAverage _emaFast;
        private ExponentialSignal _emasignal;
        private int index;

        protected override void OnStart()
        {
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            _emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            _adx = Indicators.GetIndicator<ADXR>(Source, interval);
            _cci = Indicators.GetIndicator<CCI>(CCI_period);
            _heiken = Indicators.GetIndicator<HeikenAshi2>(1);
            _emasignal = Indicators.GetIndicator<ExponentialSignal>(EMAPeriod);
        }
        protected override void OnTick()
        {
            sp_d = (Symbol.Ask - Symbol.Bid) / Symbol.PipSize;
            if (o_tm(TradeType.Buy) > 0)
                f0_86(pnt_12(TradeType.Buy), AverageTP);
            if (o_tm(TradeType.Sell) > 0)
                f0_88(pnt_12(TradeType.Sell), AverageTP);
            if (MaxSpread >= sp_d && !cStop)
                Open_24();
            RCN();
        }
        protected override void OnError(Error error)
        {
            if (error.Code == ErrorCode.NoMoney)
            {
                cStop = true;
                Print("openning stopped because: not enough money");
            }
        }
        protected override void OnBar()
        {
            //double i = hmaSignal.hma.LastValue;
            //RefreshData();
        }
        protected override void OnStop()
        {
            ChartObjects.RemoveAllObjects();
        }
        private void Open_24()
        {

            index = MarketSeries.Close.Count - 1;

            bool _macdlong = _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising();
            bool _macdshort = _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling();

            //Heiken EMA
            //var _emalong = _heiken.xOpen[index] > _emaFast.Result[index] && _emaFast.Result.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            //var _emashort = _heiken.xOpen[index] < _emaFast.Result[index] && _emaFast.Result.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

            //var _ematrendlong = _emaFast.Result.LastValue > _emaFast.Result.Last(2);
            //var _ematrendshort = _emaFast.Result.LastValue < _emaFast.Result.Last(2);

            bool emalong = _heiken.xOpen[index] > _emaFast.Result[index] && _emaFast.Result.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool emashort = _heiken.xOpen[index] < _emaFast.Result[index] && _emaFast.Result.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

            // Heiken Ashi EMA HTF Signal
            bool emalong1 = _heiken.xOpen[index] > _emasignal.EMAhour[index] && _emasignal.EMAhour.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool emashort1 = _heiken.xOpen[index] < _emasignal.EMAhour[index] && _emasignal.EMAhour.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];
            bool emalong2 = _heiken.xOpen[index] > _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool emashort2 = _heiken.xOpen[index] < _emasignal.EMAhour4[index] && _emasignal.EMAhour4.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];


            bool _adxrtrend = _adx.adxr[index] >= trend && _adx.adxr.IsRising();
            bool _adxrlong = _adx.diplus[index] > _adx.diminus[index];
            bool _adxrshort = _adx.diminus[index] > _adx.diplus[index];
            bool _CCIlong = _cci.CCIa[index] >= 0;
            bool _CCIshort = _cci.CCIa[index] <= 0;

            if (is_12)
            {
                if (Buy && o_tm(TradeType.Buy) == 0 && emalong1 && emalong2 && _macdlong && _adxrlong && _adxrtrend && _CCIlong)
                {
                    gi_21 = OrderSend(TradeType.Buy, fer(FirstVolume, 0));
                    if (gi_21 > 0)
                        tc_31 = MarketSeries.OpenTime.Last(0);
                    else
                        Print("First BUY openning error at: ", Symbol.Ask, "Error Type: ", LastResult.Error);
                }
                if (Sell && o_tm(TradeType.Sell) == 0 && emashort1 && emashort2 && _macdshort && _adxrshort && _adxrtrend && _CCIshort)
                {
                    gi_21 = OrderSend(TradeType.Sell, fer(FirstVolume, 0));
                    if (gi_21 > 0)
                        tc_32 = MarketSeries.OpenTime.Last(0);
                    else
                        Print("First SELL openning error at: ", Symbol.Bid, "Error Type: ", LastResult.Error);
                }
            }
            N_28();
        }
        private void N_28()
        {
            if (o_tm(TradeType.Buy) > 0)
            {
                if (Math.Round(Symbol.Ask, Symbol.Digits) < Math.Round(D_TD(TradeType.Buy) - PipStep * Symbol.PipSize, Symbol.Digits) && tc_31 != MarketSeries.OpenTime.Last(0))
                {
                    long gl_57 = n_lt(TradeType.Buy);
                    gi_21 = OrderSend(TradeType.Buy, fer(gl_57, 2));
                    if (gi_21 > 0)
                        tc_31 = MarketSeries.OpenTime.Last(0);
                    else
                        Print("Next BUY openning error at: ", Symbol.Ask, "Error Type: ", LastResult.Error);
                }
            }
            if (o_tm(TradeType.Sell) > 0)
            {
                if (Math.Round(Symbol.Bid, Symbol.Digits) > Math.Round(U_TD(TradeType.Sell) + PipStep * Symbol.PipSize, Symbol.Digits))
                {
                    long gl_59 = n_lt(TradeType.Sell);
                    gi_21 = OrderSend(TradeType.Sell, fer(gl_59, 2));
                    if (gi_21 > 0)
                        tc_32 = MarketSeries.OpenTime.Last(0);
                    else
                        Print("Next SELL openning error at: ", Symbol.Bid, "Error Type: ", LastResult.Error);
                }
            }
        }
        private int OrderSend(TradeType TrdTp, long iVol)
        {
            int cd_8 = 0;
            if (iVol > 0)
            {
                TradeResult result = ExecuteMarketOrder(TrdTp, Symbol, iVol, Label, StopLoss, 0, 0, "smart_grid_SL");

                if (result.IsSuccessful)
                {
                    Print(TrdTp, "Opened at: ", result.Position.EntryPrice, result.Position.StopLoss);
                    cd_8 = 1;
                }
                else
                    Print(TrdTp, "Openning Error: ", result.Error);
            }
            else
                Print("Volume calculation error: Calculated Volume is: ", iVol);
            return cd_8;
        }
        private void f0_86(double ai_4, int ad_8)
        {
            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        double? li_16 = Math.Round(ai_4 + ad_8 * Symbol.PipSize, Symbol.Digits);
                        if (position.TakeProfit != li_16)
                            ModifyPosition(position, position.StopLoss, li_16);
                    }
                }
            }
        }
        private void f0_88(double ai_4, int ad_8)
        {
            foreach (var position in Positions)
            {
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Sell)
                    {
                        double? li_16 = Math.Round(ai_4 - ad_8 * Symbol.PipSize, Symbol.Digits);
                        if (position.TakeProfit != li_16)
                            ModifyPosition(position, position.StopLoss, li_16);
                    }
                }
            }
        }
        private void RCN()
        {
            if (o_tm(TradeType.Buy) > 1)
            {
                double y = pnt_12(TradeType.Buy);
                ChartObjects.DrawHorizontalLine("bpoint", y, Colors.Yellow, 2, LineStyle.Dots);
            }
            else
                ChartObjects.RemoveObject("bpoint");
            if (o_tm(TradeType.Sell) > 1)
            {
                double z = pnt_12(TradeType.Sell);
                ChartObjects.DrawHorizontalLine("spoint", z, Colors.HotPink, 2, LineStyle.Dots);
            }
            else
                ChartObjects.RemoveObject("spoint");
            ChartObjects.DrawText("pan", A_cmt_calc(), StaticPosition.TopLeft, Colors.Tomato);
        }
        private string A_cmt_calc()
        {
            string gc_78 = "";
            string wn_7 = "";
            string wn_8 = "";
            string sp_4 = "";
            string ppb = "";
            string lpb = "";
            string nb_6 = "";
            double dn_7 = 0;
            double dn_9 = 0;
            sp_4 = "\nSpread = " + Math.Round(sp_d, 1);
            nb_6 = "\nwww.facebook.com/cls.fx\n";
            if (dn_7 > 0)
                wn_7 = "\nBuy Positions = " + o_tm(TradeType.Buy);
            if (dn_9 > 0)
                wn_8 = "\nSell Positions = " + o_tm(TradeType.Sell);
            if (o_tm(TradeType.Buy) > 0)
            {
                double igl = Math.Round((pnt_12(TradeType.Buy) - Symbol.Bid) / Symbol.PipSize, 1);
                ppb = "\nBuy Target Away = " + igl;
            }
            if (o_tm(TradeType.Sell) > 0)
            {
                double osl = Math.Round((Symbol.Ask - pnt_12(TradeType.Sell)) / Symbol.PipSize, 1);
                lpb = "\nSell Target Away = " + osl;
            }
            if (sp_d > MaxSpread)
                gc_78 = "MAX SPREAD EXCEED";
            else
                gc_78 = "Smart Grid" + nb_6 + wn_7 + sp_4 + wn_8 + ppb + lpb;
            return (gc_78);
        }
        private int cnt_16()
        {
            int ASide = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                    ASide++;
            }
            return ASide;
        }
        private int o_tm(TradeType TrdTp)
        {
            int TSide = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                        TSide++;
                }
            }
            return TSide;
        }
        private double pnt_12(TradeType TrdTp)
        {
            double Result = 0;
            double AveragePrice = 0;
            long Count = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        AveragePrice += position.EntryPrice * position.Volume;
                        Count += position.Volume;
                    }
                }
            }
            if (AveragePrice > 0 && Count > 0)
                Result = Math.Round(AveragePrice / Count, Symbol.Digits);
            return Result;
        }
        private double D_TD(TradeType TrdTp)
        {
            double D_TD = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (D_TD == 0)
                        {
                            D_TD = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < D_TD)
                            D_TD = position.EntryPrice;
                    }
                }
            }
            return D_TD;
        }
        private double U_TD(TradeType TrdTp)
        {
            double U_TD = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (U_TD == 0)
                        {
                            U_TD = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice > U_TD)
                            U_TD = position.EntryPrice;
                    }
                }
            }
            return U_TD;
        }
        private double f_tk(TradeType TrdTp)
        {
            double prc_4 = 0;
            int tk_4 = 0;
            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (tk_4 == 0 || tk_4 > position.Id)
                        {
                            prc_4 = position.EntryPrice;
                            tk_4 = position.Id;
                        }
                    }
                }
            }
            return prc_4;
        }
        private long lt_8(TradeType TrdTp)
        {
            long lot_4 = 0;
            int tk_4 = 0;
            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (tk_4 == 0 || tk_4 > position.Id)
                        {
                            lot_4 = position.Volume;
                            tk_4 = position.Id;
                        }
                    }
                }
            }
            return lot_4;
        }
        private long clt(TradeType TrdTp)
        {
            long Result = 0;
            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                        Result += position.Volume;
                }
            }
            return Result;
        }
        private int Grd_Ex(TradeType ai_0, TradeType ci_0)
        {
            double prc_4 = f_tk(ci_0);
            int tk_4 = 0;
            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                position = Positions[i];
                if (position.Label == Label && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == ai_0 && ai_0 == TradeType.Buy)
                    {
                        if (Math.Round(position.EntryPrice, Symbol.Digits) <= Math.Round(prc_4, Symbol.Digits))
                            tk_4++;
                    }
                    if (position.TradeType == ai_0 && ai_0 == TradeType.Sell)
                    {
                        if (Math.Round(position.EntryPrice, Symbol.Digits) >= Math.Round(prc_4, Symbol.Digits))
                            tk_4++;
                    }
                }
            }
            return (tk_4);
        }
        private long n_lt(TradeType ca_8)
        {
            int ic_g = Grd_Ex(ca_8, ca_8);
            long gi_c = lt_8(ca_8);
            long ld_4 = Symbol.NormalizeVolume(gi_c * Math.Pow(VolumeExponent, ic_g));
            return (ld_4);
        }
        private long fer(long ic_9, int bk_4)
        {
            long ga_i = Symbol.VolumeMin;
            long gd_i = Symbol.VolumeStep;
            long dc_i = Symbol.VolumeMax;
            long ic_8 = ic_9;
            if (ic_8 < ga_i)
                ic_8 = ga_i;
            if (ic_8 > dc_i)
                ic_8 = dc_i;
            return (ic_8);
        }
    }
}
