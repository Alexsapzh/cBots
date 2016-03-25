// --------------------------------------
// Trust Trader
//---------------------------------------

// Copyright:   Copyright 2015, FXPlan
// Link:        https://www.facebook.com/FOREX-Free-EA-Evaluation-1081254308571646/
// Date:        25/12/2015
// Version:     1.0

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    // [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Trust_Trader : Robot
    {
        [Parameter(DefaultValue = 0)]
        public int TrustTrader { get; set; }
        //////////////////////////////////////////////////
        [Parameter("AutoTradeSettings", DefaultValue = "*Set Order Info*")]
        public string AutoTradeSettings { get; set; }

        [Parameter("FirstLot", DefaultValue = 5000, MinValue = 1000, MaxValue = 100000)]
        public int FirstLot { get; set; }

        [Parameter("MaxLot", DefaultValue = 10000, MinValue = 5000, MaxValue = 1000000)]
        public int MaxLot { get; set; }

        [Parameter("LotStep", DefaultValue = 100, MinValue = 0, MaxValue = 1000)]
        public int LotStep { get; set; }

        [Parameter("Stop_Loss", DefaultValue = 50, MinValue = 0, MaxValue = 200)]
        public int Stop_Loss { get; set; }

        [Parameter("Take_Profit", DefaultValue = 60, MinValue = 0, MaxValue = 300)]
        public int TakeProfit { get; set; }

        [Parameter("Tral_Start", DefaultValue = 50, MinValue = 0, MaxValue = 300)]
        public int Tral_Start { get; set; }

        [Parameter("Tral_Stop", DefaultValue = 50, MinValue = 0, MaxValue = 300)]
        public int Tral_Stop { get; set; }

        [Parameter("Market_Range", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int Market_Range { get; set; }

        [Parameter("PipStep", DefaultValue = 100, MinValue = 0, MaxValue = 1000)]
        public int PipStep { get; set; }

        [Parameter("MaxOrders", DefaultValue = 1, MinValue = 1, MaxValue = 50)]
        public int MaxOrders { get; set; }
        //////////////////////////////////////////////////
// For Auto trade time setting option
        [Parameter("OptionMarketControl", DefaultValue = "*Set Trade Time Option*")]
        public string OptionFridaySummary { get; set; }

        [Parameter("LocalTimeZone", DefaultValue = 9, MinValue = -12, MaxValue = 12)]
        public int LocalTimeZone { get; set; }

        [Parameter("FridaySummary", DefaultValue = false)]
        public bool FridaySummary { get; set; }

        // If AutoTradeCloseTradeOp = True is need preset stop time
        [Parameter("AutoCloseStartHour", DefaultValue = 4, MinValue = 0, MaxValue = 23)]
        public int AutoCloseStartHour { get; set; }

        [Parameter("AutoCloseStartMin", DefaultValue = 30, MinValue = 0, MaxValue = 59)]
        public int AutoCloseStartMin { get; set; }

        [Parameter("AutoOpenStartHour", DefaultValue = 9, MinValue = 0, MaxValue = 23)]
        public int AutoOpenStartHour { get; set; }

        [Parameter("AutoOpenStartMin", DefaultValue = 0, MinValue = 0, MaxValue = 59)]
        public int AutoOpenStartMin { get; set; }

        [Parameter("OpNewsTimePositionClose", DefaultValue = false)]
        public bool OpNewsTimePositionClose { get; set; }

        ////////// NewsTime After 5Min Automachic Restart normal auto trading
        [Parameter("NewsTimeHour", DefaultValue = 22, MinValue = 0, MaxValue = 23)]
        public int NewsTimeHour { get; set; }

        [Parameter("NewsTimeMin", DefaultValue = 30, MinValue = 0, MaxValue = 59)]
        public int NewsTimeMin { get; set; }
        //////////////////////////////////////////////////
// For Preset StopLimit Order option when the time in Economical News 
        [Parameter("OptionStopLimitOrder", DefaultValue = "*Set PlaceOrder Option*")]
        public string OptionStopLimitOrder { get; set; }

        [Parameter("AsStopBuyNum", DefaultValue = 0, MinValue = 0, MaxValue = 10)]
        public int AsStopBuyNum { get; set; }

        [Parameter("AsStopSellNum", DefaultValue = 0, MinValue = 0, MaxValue = 10)]
        public int AsStopSellNum { get; set; }

        [Parameter("AsUnitLots", DefaultValue = 1000, MinValue = 1000, MaxValue = 100000)]
        public double AsUnitLots { get; set; }

        [Parameter("AsOrderOpenTimeHour", DefaultValue = 22, MinValue = 0, MaxValue = 23)]
        public int AsOrderOpenTimeHour { get; set; }

        [Parameter("AsOrederOpenTimeMin", DefaultValue = 30, MinValue = 0, MaxValue = 59)]
        public int AsOrederOpenTimeMin { get; set; }

        [Parameter("AsOrderCloseTimeHour", DefaultValue = 22, MinValue = 0, MaxValue = 23)]
        public int AsOrderCloseTimeHour { get; set; }

        [Parameter("AsOrederCloseTimeMin", DefaultValue = 35, MinValue = 0, MaxValue = 59)]
        public int AsOrederCloseTimeMin { get; set; }

        [Parameter("AsEntryDistancePips", DefaultValue = 20.0, MinValue = 10.0, MaxValue = 100.0)]
        public double AsEntryDistancePips { get; set; }

        [Parameter("AsStopLossPips", DefaultValue = 20.0, MinValue = 10.0, MaxValue = 100.0)]
        public double AsStopLossPips { get; set; }

        [Parameter("AsProfitPips", DefaultValue = 20.0, MinValue = 10.0, MaxValue = 100.0)]
        public double AsProfitPips { get; set; }

        //////////////////////////////////////////////////
// For Risk Management Option
        [Parameter("Risk_Setting", DefaultValue = "*Set RiskManage Option*")]
        public string Risk_Setting { get; set; }

        [Parameter("RiskControl", DefaultValue = true)]
        public bool RiskControl { get; set; }

        [Parameter("NewOrderPercent", DefaultValue = 30.0, MinValue = 0.0, MaxValue = 100.0)]
        public double NewOrderPercent { get; set; }

        [Parameter("StopedBalancePercent", DefaultValue = 10.0, MinValue = 0.0, MaxValue = 100.0)]
        public double StopedBalancePercent { get; set; }
        ////////////////////
// For Multi Currency Chart
        ////////////////////
        [Parameter("Target_PairSetting", DefaultValue = "*Set Target Pair Mark*")]
        public string Target_PairSetting { get; set; }

        // private string botLabel; <--- public parameter Target_Pair(same as Magic Number)
        [Parameter("Target_Pair", DefaultValue = "Trust@EURUSD")]
        public string Target_Pair { get; set; }
        ////////////////////
// Additional variable
        private DateTime AutoClosingTimeZone;
        private DateTime AutoOpenTimeZone;
        private DateTime AutoPlaceOrderOpenTimeZone;
        private DateTime AutoPlaceOrderCloseTimeZone;
        private DateTime AutoNewsTimeCloseTimeZone;
        private DateTime AutoNewsTimeOpenTimeZone;
        private DateTime OnCurrenttimeZone;
        private DateTime OnCurrentLocaltimeZone;
        private DateTime LastDaytimeZone;
        private bool TodayFriday = false;
        private bool TodayMonday = false;
        private Position position;
        private string RobotStatus;
        private string botLabel;
        private int TryCount = 0;
        private int MaxTryCount = 1000;
        private bool TimeCheck = false;
        private bool PlaceOrderOpen = false;
        private bool PlaceOrderClose = false;
// if debug true output log
        private bool debug = true;
        private double TotalAmount = 0.0;
        private int LimitLots = 0;
        private int OrderRetryCount = 0;
//
        private TradeResult StopBuyTradeResult = null;
        private TradeResult StopSellTradeResult = null;

        private bool NewsTimeOption = false;

        int BuyStopOrderID = 0;
        int SellStopOrderID = 0;
        string ymes_Title = "\n *";
        string ymes_RobotStatus = "\n\n ";
        string ymes_NewOrder = "\n\n\n ";
        string ymes_ModifyOrder = "\n\n\n\n ";

        /////////////////////////////////////////////////////////////////////////
        protected override void OnStart()
        {
            RobotStatus = "Running";
            botLabel = Target_Pair;
            TodayFriday = false;
            TodayMonday = false;
            NewsTimeOption = false;
            // Set Label(Same of Symble:Target Currency Pair)
            TryCount = 0;
//            ChartObjects.RemoveAllObjects();
            Print("in OnStart Event--->");
            Print("Trust Trader awakening...");
            Print("*******Paraneters*****************************************************************************************");
            Print("+++++Friday Weekend Los&Profit Position Closed Setttings**************************************************");
            Print("@@@FridaySummary:" + FridaySummary.ToString() + " @AutoCloseStartHour:" + AutoCloseStartHour.ToString() + " @AutoCloseStartMin:" + AutoCloseStartMin.ToString());
            Print("+++++Every Day can auto.manual set StopOrder setting Time when News Report Time Effection++++++++++++++++++");
            Print("@@@AsStopSellNum:" + AsStopSellNum + "@AsStopBuyNum:" + AsStopBuyNum);
            Print("@@@AsOrderOpenTimeHour:" + AsOrderOpenTimeHour.ToString() + " @AutoCloseStartMin" + AsOrederOpenTimeMin.ToString() + " @AsOrderCloseTimeHour:" + AsOrderCloseTimeHour.ToString() + " @AsOrederCloseTimeMin:" + AsOrederCloseTimeMin.ToString());
            Print("@@@AsUnitLots:" + AsUnitLots.ToString() + " @AsEntryDistancePips:" + AsEntryDistancePips.ToString() + "@AsStopLossPips:" + AsStopLossPips.ToString() + " @AsProfitPips" + AsProfitPips.ToString());
            Print("@@@AsStopSellNum:" + AsStopSellNum + " @AsStopBuyNum:" + AsStopBuyNum);
            Print("+++++Risk Management Parameter.............................................................................");
            Print("@@@RiskControl:" + RiskControl.ToString() + " @NewOrderPercent:" + NewOrderPercent.ToString() + " @StopedBalancePercent" + StopedBalancePercent.ToString());
            Print("+++++Current Accoung Information*************************************************************************");
            Print("Account.FreeMargin:", Account.FreeMargin.ToString());
            Print("Account.Balance:" + Account.Balance.ToString());
            Print("Account.Margin:" + Account.Margin.ToString());
            Print("Account.Equity:" + Account.Equity.ToString());
            Print("Account.MarginLevel:" + Account.MarginLevel.ToString());
            Print("Account.Currency:" + Account.Currency.ToString());
            Print("Account.Leverage:" + Account.Leverage);
            Print("*******************Start Trust Trader Logic**************************");
            var triggerTimeInLocalTimeZoneOpenOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, AsOrderOpenTimeHour + LocalTimeZone, AsOrederOpenTimeMin, 0);
            if (triggerTimeInLocalTimeZoneOpenOrder < DateTime.Now)
                triggerTimeInLocalTimeZoneOpenOrder = triggerTimeInLocalTimeZoneOpenOrder.AddDays(1);
            AutoPlaceOrderOpenTimeZone = triggerTimeInLocalTimeZoneOpenOrder;

            var triggerTimeInLocalTimeZoneCloseOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, AsOrderCloseTimeHour + LocalTimeZone, AsOrederCloseTimeMin, 0);
            if (triggerTimeInLocalTimeZoneCloseOrder < DateTime.Now)
                triggerTimeInLocalTimeZoneCloseOrder = triggerTimeInLocalTimeZoneCloseOrder.AddDays(1);
            AutoPlaceOrderCloseTimeZone = triggerTimeInLocalTimeZoneCloseOrder;
            PlaceOrderOpen = true;
            PlaceOrderClose = false;
            OnCurrenttimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TimeZone);
            LastDaytimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TimeZone);

            // Optional Time setting
            MakeTimeCheck();

            if (RiskControl)
            {
                double LoscutLimit = (Account.Balance + Account.Margin) / StopedBalancePercent;
                LoscutLimit = ToHalfAdjust(LoscutLimit, 2);
                Print("LosCut Order Percent:" + LoscutLimit.ToString());
                double MaxOrderLimit = (FirstLot * Account.Leverage * NewOrderPercent / 1000000);
                MaxOrderLimit = ToHalfAdjust(MaxOrderLimit, 2);
                Print("Max Order Limit:" + MaxOrderLimit.ToString());
            }

            ChartObjects.DrawText("optionarea", "Trust Trader:" + botLabel + "\n " + "---------- OptionParameters -----------" + "\n Option Friday Auto Closed Open Position:" + FridaySummary.ToString() + "\n Next Friday will close Time:" + AutoClosingTimeZone.ToString() + " Next Monday will open Time:" + AutoOpenTimeZone.ToString() + "\n News Time Auto Closed All Open Position :" + OpNewsTimePositionClose.ToString() + "\n News Time:" + AutoNewsTimeCloseTimeZone.ToString() + "After Open Time:" + AutoNewsTimeOpenTimeZone.ToString() + "\n Buy StopLimitOrder:" + AsStopBuyNum.ToString() + "  Sell StopLimitOrder:" + AsStopSellNum.ToString() + "\n" + " Order Entry Time:" + AutoPlaceOrderOpenTimeZone.ToString() + " Order Filled Time:" + AutoPlaceOrderCloseTimeZone.ToString(), StaticPosition.BottomRight, Colors.White);
//            Positions.Opened += OnPositionOpened;
            if (debug)
                Print("OnStart---->Starting Trust_Trader:" + botLabel);
            // ****************************************************************************************************************************
            // +++++Title Output Chart Area +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Output Title Line
            ChartObjects.DrawText("TitleLine", ymes_Title + "Trust Trader Activated " + botLabel + TimeZoneInfo.Local, StaticPosition.TopLeft, Colors.Aquamarine);
            ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
            ChartObjects.RemoveObject("StatusLine");
            ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Running:" + botLabel + "  Time:" + DateTime.Now, StaticPosition.TopLeft, Colors.CadetBlue);

            // ***************************************************************************************************************************
        }
        protected override void OnStop()
        {
            if (debug)
                Print("in OnStop-->Trust Robot is Stoped:" + "(" + RobotStatus + ")");
            ChartObjects.DrawText("TitleLine", ymes_Title + "" + botLabel, StaticPosition.TopLeft, Colors.Black);
            ChartObjects.RemoveObject("TitleLine");
            ChartObjects.DrawText("TitleLine", ymes_Title + "Trust_Trader" + botLabel + "(" + RobotStatus + ")" + "was Stoped", StaticPosition.TopLeft, Colors.Red);
        }

        ///*****************************************************************************************************************************************
        protected override void OnTick()
        {
            double Bid = Symbol.Bid;
            double Ask = Symbol.Ask;
            double Point = Symbol.TickSize;
            int TempAllCount = 0;
            int MylabelPositionCount = 0;
            int IndexPosition = 0;

            if (debug)
                Print("In OnTick--->RobotStatus = " + RobotStatus);

            if (RobotStatus == "Stoped")
                return;

            if (RobotStatus == "Closing")
                return;

            //////////////////////////////////////////////////
            // Friday Close & Monday Open Time Calculate & set
            MakeTimeCheck();
            //////////////////////////////////////////////////

            // Auto Trade Stoped Option                
            // Pass the preset closing time ?
            OnCurrenttimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TimeZone);
            if (RobotStatus != "Pausing")
            {
                if (PlaceOrderClose)
                {
                    if (OnCurrenttimeZone >= AutoPlaceOrderCloseTimeZone)
                    {
                        PlaceOrderClose = false;
                        // Close Current Open Position
                        // Fixed Profit Order Closed
                        // Maximum DrawDown Los Prder Closed
                        // But if small amount Loss Order is remaind
                        //////////////////////////////
                        Trust_StopOrderClose();
                        //////////////////////////////
                        BuyStopOrderID = 0;
                        SellStopOrderID = 0;
                        if (AsStopBuyNum > 0 || AsStopSellNum > 0)
                            PlaceOrderOpen = true;
                        TryCount = 0;
                    }
                }
                if (PlaceOrderOpen)
                {
                    if (OnCurrenttimeZone >= AutoPlaceOrderOpenTimeZone)
                    {
                        BuyStopOrderID = 0;
                        SellStopOrderID = 0;
                        PlaceOrderOpen = false;
                        Trust_StopOrderOpen();
                        PlaceOrderOpen = true;
                    }
                }
            }

            // if TimeCheck false ... Next Pass through
            if (TimeCheck && TodayFriday)
            {
                // Check the AutoTrade Pause Time Zone Now
                if (OnCurrenttimeZone > AutoClosingTimeZone)
                {
                    RobotStatus = "Closing";
                    //////////////////////////////////
                    /// Position Closed & Take Profit(Fixed) on Friday
                    Trust_FixProfit(1);
                    //////////////////////////////////
                    Print("in OnTick-->Trust Trader is Pausing");
                    ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                    ChartObjects.RemoveObject("StatusLine");
                    ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Open Position Closed:" + botLabel + "  Time:" + DateTime.Now, StaticPosition.TopLeft, Colors.Red);
                    RobotStatus = "Pausing";
                    TryCount = 0;

                    // End of Open time Step
                }
                // End of TimeCheck
            }

            // This Option is Only One Time, if finished none this option
            if (!NewsTimeOption)
            {
                if (OpNewsTimePositionClose)
                {
                    if (OnCurrenttimeZone >= AutoNewsTimeCloseTimeZone)
                    {
                        RobotStatus = "Closing";
                        // All Open Position Closed
                        ////////////////////
                        Trust_FixProfit(0);
                        ////////////////////
                        RobotStatus = "Pausing";
                    }
                    if (OnCurrenttimeZone >= AutoNewsTimeOpenTimeZone)
                    {
                        RobotStatus = "Running";
                        NewsTimeOption = true;
                    }
                }
            }
            if (TimeCheck)
            {
                // Check the AutoTrade Opene Time Zone Now on Monday
                if (OnCurrenttimeZone > AutoOpenTimeZone)
                {
                    RobotStatus = "Running";
                    TryCount = 0;
                    // End of Open time Step
                }
                // End of TimeCheck
            }
            // Not AutoTradePauseOption(Status not Pausing / NewsTime) is Normal Patern
            /////////////////////////////////////////////////////////////////

            if (RobotStatus == "Running")
            {
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("StatusLine");
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Running:" + botLabel + "  Time:" + DateTime.Now, StaticPosition.TopLeft, Colors.CadetBlue);
                ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("ModifyLine");
            }
            else if (RobotStatus == "MarketOpenWaiting")
            {
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("StatusLine");
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Waiting of Market Open:" + botLabel + "  Time:" + DateTime.Now, StaticPosition.TopLeft, Colors.Red);
                ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("ModifyLine");
                return;
            }
            else if (RobotStatus == "Pausing" || RobotStatus == "NewsTime")
                return;


// *********************************************************************************//
// Normal Trade Function Continued
// *********************************************************************************//
            TempAllCount = Positions.Count;

            if (debug)
                Print("in OnTick-->AllPositionCount:" + TempAllCount);
            if (TempAllCount > 0)
            {
                foreach (var position in Positions)
                {
                    if (position.Label == botLabel)
                        MylabelPositionCount++;
                }
            }
            if (MylabelPositionCount == 0)
            {
                if (RiskControl && !RightTradeNow(FirstLot, NewOrderPercent))
                {
                    bool orderOK = false;
                    LimitLots = FirstLot;
                    for (int OrderRetryCount = 1; OrderRetryCount < 10; OrderRetryCount++)
                    {
                        if (LimitLots <= 1000)
                            break;
                        LimitLots = LimitLots - 1000;
                        SendFirstOrder(LimitLots);
                        // First Order Control
                        orderOK = true;
                    }
                    if (!orderOK)
                    {
                        RobotStatus = "Stoped";
                        Print("RiskManagement on Not Enough Money Robot Stoped");
                        Stop();
                        return;
                    }
                }
                else
                {
                    SendFirstOrder(FirstLot);
                    // First Order Contro
                }
            }
            //////////////////////////////////////////////     
            else
            {
                ControlSeries();
                // Second(Next) Order Control
            }
            if (RobotStatus == "Stoped")
            {
                Stop();
                return;
            }

            //////////////////////////////////////////////            
            // Start Buy Position
//                if (Positions == null)
//                    return;
            for (IndexPosition = 0; IndexPosition < MylabelPositionCount; IndexPosition++)
            {
                if (Positions[IndexPosition] == null)
                    break;
                if (Positions[IndexPosition].TradeType == TradeType.Buy && Positions[IndexPosition].Label == botLabel)
                {
                    if (Bid - GetAveragePrice(TradeType.Buy, Positions[IndexPosition].Label) >= Tral_Start * Point)
                        if (Bid - Tral_Stop * Point >= Positions[IndexPosition].StopLoss)
                        {
                            ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "", StaticPosition.TopLeft, Colors.Black);
                            ChartObjects.RemoveObject("ModifyLine");
                            ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "Modify Current BuyOrder:" + botLabel + "StopLoss:" + (Bid - Tral_Stop * Point).ToString() + " Profit:" + (Positions[IndexPosition].TakeProfit).ToString(), StaticPosition.TopLeft, Colors.Red);
                            // public TradeOperation ModifyPositionAsync(Position position, double? stopLoss, double? takeProfit, [optional] Action callback)
                            TradeOperation BuyModOpe = ModifyPositionAsync(Positions[IndexPosition], Bid - Tral_Stop * Point, Positions[IndexPosition].TakeProfit);
                            if (BuyModOpe.IsExecuting)
                                TryCount = 0;
                        }
                    TryCount++;
                    if (RobotStatus == "MarketOpenWaiting")
                        TryCount = 0;
                }
            }
            //////////////////////////////////////////////            
            // Start Sell Position
            for (IndexPosition = 0; IndexPosition < MylabelPositionCount; IndexPosition++)
            {
                if (Positions[IndexPosition] == null)
                    break;
                if (Positions[IndexPosition].TradeType == TradeType.Sell && Positions[IndexPosition].Label == botLabel)
                    if (GetAveragePrice(TradeType.Sell, Positions[IndexPosition].Label) - Ask >= Tral_Start * Point)
                        if (Ask + Tral_Stop * Point <= Positions[IndexPosition].StopLoss || Positions[IndexPosition].StopLoss == 0)
                        {
                            ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "", StaticPosition.TopLeft, Colors.Black);
                            ChartObjects.RemoveObject("ModifyLine");
                            ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "Modify Current Buyorder:" + botLabel + "StopLoss:" + (Ask + Tral_Stop * Point).ToString() + " Profit:" + (Positions[IndexPosition].TakeProfit).ToString(), StaticPosition.TopLeft, Colors.Red);
                            // public TradeOperation ModifyPositionAsync(Position position, double? stopLoss, double? takeProfit, [optional] Action callback)
                            TradeOperation SellModOpe = ModifyPositionAsync(Positions[IndexPosition], Ask + Tral_Stop * Point, Positions[IndexPosition].TakeProfit);
                            if (SellModOpe.IsExecuting)
                                TryCount = 0;
                        }
                TryCount++;
                if (RobotStatus == "MarketOpenWaiting")
                    TryCount = 0;
            }

        }

        protected override void OnError(Error CodeOfError)
        {
            if (debug)
                Print("in OnError Event-->" + CodeOfError.Code);
            if (CodeOfError.Code == ErrorCode.NoMoney)
            {
                if (debug)
                {
                    RobotStatus = "Running";
                    return;
                }
                if (OrderRetryCount > 0 && OrderRetryCount < 10)
                {
                    RobotStatus = "Running";
                    return;
                }
                RobotStatus = "Stoped";
                Print("ERROR!!! No money for order open, robot is stopped!");
            }
            else if (CodeOfError.Code == ErrorCode.BadVolume)
            {
                // RobotStatus = "Stoped";
                // retry because if position same value is error occoured
                if (TryCount > MaxTryCount)
                {
                    RobotStatus = "Stoped";
                    Print("ERROR!!! Bad volume for order open, robot is stopped!");
                }
                else
                {
                    RobotStatus = "running";
                    return;
                }
            }
            else if (CodeOfError.Code == ErrorCode.TechnicalError)
            {
                // RobotStatus = "Stoped";
                // retry because if position same value or server not waiting is error(TechnicalError) occoured
                if (TryCount > MaxTryCount)
                {
                    RobotStatus = "Stoped";
                    Print("ERROR!!! TechnicalError, robot is stopped!");
                }
                else
                {
                    RobotStatus = "running";
                    return;
                }
            }
            else if (CodeOfError.Code == ErrorCode.MarketClosed)
            {
                // RobotStatus = "Stoped";
                // retry because if matketclose time in, waiting marekt open
                if (TryCount > MaxTryCount)
                {
                    Print("ERROR!!! Market Closed, robot is waiting Market Open time!");
                    Print("Count of Try:", TryCount);
                }
                else
                {
                    if (RobotStatus != "Stoped")
                    {
                        if (RobotStatus == "MarketOpenWaiting")
                        {
                            if (debug)
                                Print("Now Market Closed can't Auto Trading...");
                            ChartObjects.DrawText("StatusLine", ymes_RobotStatus + " ", StaticPosition.TopLeft, Colors.Black);
                            ChartObjects.RemoveObject("StatusLine");
                            ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Waiting Market Open, local time:" + DateTime.Now, StaticPosition.TopLeft, Colors.Red);
                        }
                        RobotStatus = "MarketOpenWaiting";
                        TryCount = 0;
                        return;
                    }
                }
            }
            else if (CodeOfError.Code == ErrorCode.Timeout)
            {
                // RobotStatus = "Stoped";
                // retry because if position same value or server not response is error(Timeout) occoured
                if (TryCount > MaxTryCount)
                {
                    RobotStatus = "Stoped";
                    Print("ERROR!!! TechnicalError, robot is stopped!");
                }
                else
                {
                    RobotStatus = "Running";
                    return;
                }
            }
            if (RobotStatus == "Stoped")
            {
                Print("Error happend Robot is stopped!");
                Stop();
            }
        }

        private void SendFirstOrder(int OrderVolume)
        {
            int Signal = GetStdIlanSignal();
            double? dStopLoss = 0.0;
            double? dProfit = 0.0;
            double? dMarket_Range = 0.0;
            dStopLoss = Stop_Loss;
            dProfit = TakeProfit;
            dMarket_Range = Market_Range;
            if (debug)
                Print("is SendFirstOrder Event->Status" + RobotStatus + " Volume:" + OrderVolume.ToString());
            if (OrderVolume <= 0)
                return;

            if (!(Signal < 0))
                switch (Signal)
                {
                    case 0:
                        if (debug)
                            Print("SendFirstOrder---->Symble: " + Symbol + "---botLabel: " + botLabel);
                        // public TradeResult ExecuteMarketOrder(TradeType tradeType, Symbol symbol, long volume, string label, double? stopLossPips, double? takeProfitPips, double? marketRangePips, string comment)
                        TradeResult ResultBuy = ExecuteMarketOrder(TradeType.Buy, Symbol, OrderVolume, botLabel, dStopLoss, dProfit, dMarket_Range, botLabel);
                        if (ResultBuy.IsSuccessful)
                        {
                            TryCount = 0;
                            Print("SendFirstOrderBuy Lots:" + OrderVolume);
                            ChartObjects.DrawText("orderline", ymes_NewOrder + "", StaticPosition.TopLeft, Colors.Black);
                            ChartObjects.RemoveObject("orderline");
                            ChartObjects.DrawText("orderline", ymes_NewOrder + "Send First Order Buy(" + ResultBuy.Position.Id.ToString() + ")" + " Lots:" + OrderVolume, StaticPosition.TopLeft, Colors.White);
                        }
                        else if (RobotStatus == "MarketOpenWaiting")
                            TryCount = 0;
                        break;
                    case 1:
                        if (debug)
                            Print("SendFirstOrder---->Symble: " + Symbol + "---botLabel: " + botLabel);
                        // public TradeResult ExecuteMarketOrder(TradeType tradeType, Symbol symbol, long volume, string label, double? stopLossPips, double? takeProfitPips, double? marketRangePips, string comment)
                        TradeResult ResultSell = ExecuteMarketOrder(TradeType.Sell, Symbol, OrderVolume, botLabel, dStopLoss, dProfit, dMarket_Range, botLabel);
                        if (ResultSell.IsSuccessful)
                        {
                            TryCount = 0;
                            Print("SendFirstOrderSell Lots:" + OrderVolume);
                            ChartObjects.DrawText("orderline", ymes_NewOrder + "", StaticPosition.TopLeft, Colors.Black);
                            ChartObjects.RemoveObject("orderline");
                            ChartObjects.DrawText("orderline", ymes_NewOrder + "Send First Order Sell(" + ResultSell.Position.Id.ToString() + ")" + " Lots:" + OrderVolume, StaticPosition.TopLeft, Colors.White);
                        }
                        else if (RobotStatus == "MarketOpenWaiting")
                            TryCount = 0;
                        break;
                    default:
                        break;
                }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            double? StopLossPrice = 0.0;
            double? TakeProfitPrice = 0.0;
            if (Positions.Count == 1)
            {
                position = args.Position;
                if (position.Label == botLabel)
                {
                    if (position.TradeType == TradeType.Buy)
                        TakeProfitPrice = position.EntryPrice + TakeProfit * Symbol.TickSize;
                    if (position.TradeType == TradeType.Sell)
                        TakeProfitPrice = position.EntryPrice - TakeProfit * Symbol.TickSize;
                }
                else
                    switch (GetPositionsSide(position.Label))
                    {
                        case 0:
                            TakeProfitPrice = GetAveragePrice(TradeType.Buy, position.Label) + TakeProfit * Symbol.TickSize;
                            break;
                        case 1:
                            TakeProfitPrice = GetAveragePrice(TradeType.Sell, position.Label) - TakeProfit * Symbol.TickSize;
                            break;
                    }
                for (int i = 0; i < Positions.Count; i++)
                {
                    position = Positions[i];
                    if (position.Label == botLabel)
                    {
                        if (StopLossPrice != 0.0 || TakeProfitPrice != 0.0)
                        {
                            if (position != null)
                            {
                                // public TradeResult ModifyPosition(Position position, double? stopLoss, double? takeProfit)
                                TradeResult tradeOperateMod = ModifyPosition(position, position.StopLoss, TakeProfitPrice);
                                if (tradeOperateMod.IsSuccessful)
                                    TryCount++;
                            }
                        }
                    }
                }
            }
        }

        private double GetAveragePrice(TradeType TypeOfTrade, string TradeLabel)
        {
            double Result = Symbol.Bid;
            double AveragePrice = 0;
            long Count = 0;
            for (int i = 0; i < Positions.Count; i++)
            {
                position = Positions[i];

                if (position.TradeType == TypeOfTrade && position.Label == TradeLabel)
                {
                    AveragePrice += position.EntryPrice * position.Volume;
                    Count += position.Volume;
                }
            }
            if (AveragePrice > 0 && Count > 0 && position.Label == TradeLabel)
                Result = AveragePrice / Count;
            return Result;

        }

        private int GetPositionsSide(string TradeLabel)
        {
            int Result = -1;
            int i, BuySide = 0, SellSide = 0;
            for (i = 0; i < Positions.Count; i++)
            {
                if (Positions[i].TradeType == TradeType.Buy && Positions[i].Label == TradeLabel)
                    BuySide++;
                if (Positions[i].TradeType == TradeType.Sell && Positions[i].Label == TradeLabel)
                    SellSide++;
            }
            if (BuySide == Positions.Count)
                Result = 0;
            if (SellSide == Positions.Count)
                Result = 1;
            return Result;
        }
        /// <summary>
        /// The gradient variable is a dynamic value that represente an equidistant grid between
        /// the high value and the low value of price.
        /// </summary>
        /// 
        private void ControlSeries()
        {
            int _pipstep, NewVolume, Rem;
            int BarCount = 25;
            int Del = MaxOrders - 1;

            if (PipStep == 0)
                _pipstep = GetDynamicPipstep(BarCount, Del);
            else
                _pipstep = PipStep;

            if (Positions.Count < MaxOrders)
                switch (GetPositionsSide(botLabel))
                {
                    case 0:
                        if (Symbol.Ask < FindLastPrice(TradeType.Buy, botLabel) - _pipstep * Symbol.TickSize)
                        {
                            NewVolume = Math.DivRem((int)(FirstLot + FirstLot * Positions.Count), LotStep, out Rem) * LotStep;
                            if (RiskControl && !RightTradeNow(NewVolume, NewOrderPercent))
                            {
                                RobotStatus = "Stoped";
                                return;
                            }
                            if (!(NewVolume < LotStep))
                            {
                                if (RiskControl == true && RightTradeNow(NewVolume, StopedBalancePercent) == false)
                                    NewVolume = (int)(Account.FreeMargin / (StopedBalancePercent * 1000));
                                if (NewVolume <= 0)
                                    NewVolume = (FirstLot / 10000);
                                if (debug)
                                    Print("in ControlSeries-->BuyTrade AddOrder:" + Symbol + "Volume:" + NewVolume);
                                ChartObjects.DrawText("Orderline", ymes_NewOrder + "", StaticPosition.Left, Colors.Black);
                                ChartObjects.RemoveObject("Orderline");
                                // public TradeResult ExecuteMarketOrder(TradeType tradeType, Symbol symbol, long volume, string label)
                                TradeResult NewOrderBuy = ExecuteMarketOrder(TradeType.Buy, Symbol, NewVolume, botLabel);
                                if (NewOrderBuy.IsSuccessful)
                                {
                                    ChartObjects.DrawText("Orderline", ymes_NewOrder + "Buy Order Position Modify(" + NewOrderBuy.Position.Id.ToString() + ")" + " :" + Symbol + "Volume:" + NewVolume, StaticPosition.Left, Colors.White);
                                    TryCount = 0;
                                    if (debug)
                                        Print("in ControlSeries-->New Buy Order:" + NewOrderBuy.Position.Id + ":" + Symbol + ":" + "Volue:" + NewVolume);
                                }
                                else if (RobotStatus == "MarketOpenWaiting")
                                {
                                    TryCount = 0;
                                }
                            }
                        }
                        break;
                    case 1:
                        if (Symbol.Bid > FindLastPrice(TradeType.Sell, botLabel) + _pipstep * Symbol.TickSize)
                        {
                            NewVolume = Math.DivRem((int)(FirstLot + FirstLot * Positions.Count), LotStep, out Rem) * LotStep;
                            if (RiskControl && !RightTradeNow(NewVolume, NewOrderPercent))
                            {
                                RobotStatus = "Stoped";
                                return;
                            }
                            if (!(NewVolume < LotStep))
                            {
                                if (RiskControl == true && RightTradeNow(NewVolume, StopedBalancePercent) == false)
                                    NewVolume = (int)(Account.FreeMargin / (StopedBalancePercent * 1000));
                                if (NewVolume <= 0)
                                    NewVolume = (FirstLot / 10000);
                                if (debug)
                                    Print("in ControlSeries-->SellTrade AddOrder:" + Symbol + "Volume:" + NewVolume);
                                ChartObjects.DrawText("Orderline", ymes_NewOrder + "", StaticPosition.Left, Colors.Black);
                                ChartObjects.RemoveObject("Orderline");
                                // public TradeResult ExecuteMarketOrder(TradeType tradeType, Symbol symbol, long volume, string label)
                                TradeResult NewOrderSell = ExecuteMarketOrder(TradeType.Sell, Symbol, NewVolume, botLabel);
                                if (NewOrderSell.IsSuccessful)
                                {
                                    ChartObjects.DrawText("Orderline", ymes_NewOrder + "Sell Order Position Modify(" + NewOrderSell.Position.Id.ToString() + ")" + " :" + Symbol + "Volume:" + NewVolume, StaticPosition.Left, Colors.White);
                                    TryCount = 0;
                                    if (debug)
                                        Print("in ControlSeries-->New Buy Order:" + NewOrderSell.Position.Id + ":" + Symbol + ":" + "Volue:" + NewVolume);
                                }
                                else if (RobotStatus == "MarketOpenWaiting")
                                    TryCount = 0;
                            }
                        }
                        break;
                }
        }

        // ----- If AutoTradePauseOption =True
        ///flg = 0: All Position  flg = 1: Profit Position & loscutting Position
        private void Trust_FixProfit(int flg)
        {
            bool CloseFlg = true;

            if (debug)
                Print("in Trust_FixProfit->" + "flg:" + flg.ToString());
            // All Position Closed
            if (flg == 0)
            {

                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("StatusLine");
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status:  Closing Current All Posion Before News Time Pausing:" + ":" + botLabel + ":" + AutoClosingTimeZone.ToString(), StaticPosition.TopLeft, Colors.Green);
                ChartObjects.DrawText("OrderLine", ymes_NewOrder + "", StaticPosition.TopLeft, Colors.Black);
                CloseFlg = false;
                foreach (var position in Positions)
                {
                    if (position.SymbolCode == Symbol.Code && position.Label == botLabel)
                    {
                        // public TradeResult ClosePosition(Position position)
                        TradeResult CloseResut = ClosePosition(position);
                    }
                }
            }
            // Flg=1: Risk Control Position Closed
            else
            {
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.RemoveObject("StatusLine");
                ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status:  Closing Current Posion Fixed Before Weekend Period Pausing:" + ":" + botLabel + ":" + AutoClosingTimeZone.ToString(), StaticPosition.TopLeft, Colors.Green);
                ChartObjects.DrawText("OrderLine", ymes_NewOrder + "", StaticPosition.TopLeft, Colors.Black);
                if (RiskControl)
                {
                    if (Account.UnrealizedNetProfit > 0)
                    {
                        ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "", StaticPosition.TopLeft, Colors.Black);
                        ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Closing All Position:" + botLabel, StaticPosition.TopLeft, Colors.Green);
                    }
                    else if (Account.UnrealizedNetProfit > Account.FreeMargin)
                    {
                        CloseFlg = false;
                        ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "                                                         ", StaticPosition.TopLeft, Colors.Black);
                        ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Closing Only Profit Position(Fixed):" + botLabel, StaticPosition.TopLeft, Colors.Green);
                    }
                }
                else
                {
                    ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "                                                         ", StaticPosition.TopLeft, Colors.Black);
                    ChartObjects.DrawText("StatusLine", ymes_RobotStatus + "Status: Closing All Position:" + botLabel, StaticPosition.TopLeft, Colors.Green);
                }
                foreach (var position in Positions)
                {
                    if (position.SymbolCode == Symbol.Code && position.Label == botLabel)
                    {
                        // Profit Position is force closed(Current Fixed)
                        if (position.NetProfit > 0.0)
                        {
                            TotalAmount = TotalAmount + position.NetProfit;
                            // public TradeResult ClosePosition(Position position)
                            TradeResult CloseResut = ClosePosition(position);
                        }
                        else
                        {
                            TotalAmount = TotalAmount + position.NetProfit;
                            if (!CloseFlg)
                            {
                                double CloseRisk = ((System.Math.Abs(position.NetProfit)) / Account.FreeMargin) * 100;
                                // --- Only Big Loss Closed
                                if (CloseRisk > NewOrderPercent)
                                {
                                    // public TradeResult ClosePosition(Position position)
                                    TradeResult CloseResut = ClosePosition(position);
                                }
                            }
                            else
                            {
                                // public TradeResult ClosePosition(Position position)
                                TradeResult CloseResut = ClosePosition(position);
                            }
                        }
                    }
                }
                ChartObjects.RemoveObject("OrderLine");
                ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "", StaticPosition.TopLeft, Colors.Black);
                ChartObjects.DrawText("ModifyLine", ymes_ModifyOrder + "Current Total Profit : " + TotalAmount.ToString(), StaticPosition.TopLeft, Colors.Cyan);
            }
        }

        private void Trust_StopOrderOpen()
        {
            double Bid = Symbol.Bid;
            double Ask = Symbol.Ask;
            double TargetPrice;
            long EntryVolume;

            if (RiskControl)
            {
                if (!RightTradeNow(AsUnitLots * AsStopBuyNum, NewOrderPercent))
                    return;
                if (!RightTradeNow(AsUnitLots * AsStopSellNum, NewOrderPercent))
                    return;
            }
            Print("in Trust_NewsOrderOpen--->Time:" + DateTime.Now.ToString());
            ChartObjects.DrawText("OrderLine", ymes_ModifyOrder + "", StaticPosition.Left, Colors.Black);
            if (AsStopBuyNum > 0)
            {

                TargetPrice = Bid + (AsEntryDistancePips / 1000);
                EntryVolume = (long)(AsUnitLots * AsStopBuyNum);
                // public TradeResult PlaceStopOrder(TradeType tradeType, Symbol symbol, long volume, double targetPrice, string label, double? stopLossPips, double? takeProfitPips, DateTime? expiration, string comment)
                StopBuyTradeResult = PlaceStopOrder(TradeType.Buy, Symbol, EntryVolume, TargetPrice, botLabel, AsStopLossPips / 10, AsProfitPips / 10, AutoClosingTimeZone, "BuyStopOrder by TrustTrader");
                if (StopBuyTradeResult.ToString() != null)
                {
                    if (StopBuyTradeResult.PendingOrder.Label == botLabel)
                        BuyStopOrderID = StopBuyTradeResult.PendingOrder.Id;
                }

                // ChartObjects.RemoveObject("OrderLine");
                Print("BuyStopOrderID :" + BuyStopOrderID);
                ChartObjects.RemoveObject("OrderLine");
                ChartObjects.DrawText("OrderLine", ymes_ModifyOrder + "Buy Stop Reverse limit Order ID:" + BuyStopOrderID.ToString() + "  Volume:" + EntryVolume.ToString(), StaticPosition.Left, Colors.Red);
            }
            else
            {
                TargetPrice = Ask - (AsEntryDistancePips / 1000);
                EntryVolume = (long)(AsUnitLots * AsStopBuyNum);
                // public TradeResult PlaceStopOrder(TradeType tradeType, Symbol symbol, long volume, double targetPrice, string label, double? stopLossPips, double? takeProfitPips, DateTime? expiration, string comment)
                StopSellTradeResult = PlaceStopOrder(TradeType.Sell, Symbol, EntryVolume, TargetPrice, botLabel, AsStopLossPips / 10, AsProfitPips / 10, AutoClosingTimeZone, "SellStopOrder by TrustTrader");
                if (StopSellTradeResult.ToString() != null)
                {
                    if (StopSellTradeResult.PendingOrder.Label == botLabel)
                        SellStopOrderID = StopSellTradeResult.PendingOrder.Id;
                }
                // ChartObjects.RemoveObject("OrderLine");
                Print("NewsOrderID:" + SellStopOrderID);
                ChartObjects.RemoveObject("OrderLine");
                ChartObjects.DrawText("OrderLine", ymes_ModifyOrder + "Sell Stop Reverse limit Order ID:" + SellStopOrderID + "  Volume:" + EntryVolume.ToString(), StaticPosition.Left, Colors.Red);
            }
        }

        private void Trust_StopOrderClose()
        {
            Print("in Trust_NewsOrderClose--->Time:" + DateTime.Now.ToString());
            bool stat = false;
            if (AsStopBuyNum > 0)
            {
                if (StopBuyTradeResult.ToString() != null)
                {
                    if (StopBuyTradeResult.PendingOrder.Id == BuyStopOrderID)
                    {
                        if (StopBuyTradeResult.Position != null)
                        {
                            // public TradeResult ClosePosition(Position position)
                            ClosePosition(StopBuyTradeResult.Position);
                            stat = true;
                        }
                        else
                        {
                            // If Stop Order is not co（ntracted yet, Target order pending
                            // public TradeResult CancelPendingOrder(PendingOrder pendingOrder)
                            CancelPendingOrder(StopBuyTradeResult.PendingOrder);
                            stat = true;
                        }
                    }
                }
                if (stat == true)
                    BuyStopOrderID = 0;
            }
            else if (AsStopSellNum > 0)
            {

                if (StopSellTradeResult.ToString() != null)
                {
                    if (StopSellTradeResult.PendingOrder.Id == BuyStopOrderID)
                    {
                        if (StopSellTradeResult.Position != null)
                        {
                            // public TradeResult ClosePosition(Position position)
                            ClosePosition(StopSellTradeResult.Position);
                            stat = true;
                        }
                        else
                        {
                            // If Stop Order is not co（ntracted yet, Target order pending
                            // public TradeResult CancelPendingOrder(PendingOrder pendingOrder)
                            CancelPendingOrder(StopSellTradeResult.PendingOrder);
                            stat = true;
                        }
                    }
                }
                if (stat == true)
                    SellStopOrderID = 0;
            }

        }

        // Risk Management Automatically
        // If Return false Robot will be Auto Trade Stoped
        private bool RightTradeNow(double lots, double risk)
        {
            double CalcRisktemp;
            double LoscutLimit;
            if (debug)
                Print("in RightTradeNow--->lots:" + lots.ToString() + " risk:" + risk.ToString());
//            if (lots < 0)
//                return false;
            CalcRisktemp = (lots * Account.Leverage * risk / 10000000);
            CalcRisktemp = ToHalfAdjust(CalcRisktemp, 2);
            if (debug)
            {
                Print("Account.FreeMargin:", Account.FreeMargin.ToString());
                Print("Current Order Margin :" + CalcRisktemp.ToString());
                Print("Account.Balance:" + Account.Balance.ToString());
                Print("Account.Margin:" + Account.Margin.ToString());
            }
            // public IAccount Account{ get; }
            // public IAccount Account{ get; }
            if (Account.FreeMargin < CalcRisktemp)
            {
                Print("It's Not Enough Money Robot Stoped");
                return false;
            }
            else
            {
                LoscutLimit = (Account.Balance + Account.Margin) / StopedBalancePercent;
                LoscutLimit = ToHalfAdjust(LoscutLimit, 2);
                CalcRisktemp = ((lots / 10000 * Account.Leverage) / (Account.Balance + Account.Margin) / 100);
                CalcRisktemp = ToHalfAdjust(CalcRisktemp, 2);
                if (debug)
                    Print("LoscutLimit:" + LoscutLimit.ToString() + "  OrderRiskPercent:" + CalcRisktemp.ToString());
                {
                    if (LoscutLimit < CalcRisktemp)
                    {
                        Print("It's Not Enough Money Robot Stoped");
                        return false;
                    }
                }
            }
            return true;
        }

        private int GetDynamicPipstep(int CountOfBars, int gradient)
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
            Result = (int)((HighestPrice - LowestPrice) / Symbol.TickSize / gradient);
            return Result;
        }
        private double FindLastPrice(TradeType TypeOfTrade, string TradeLabel)
        {
            double LastPrice = 0;
            for (int i = 0; i < Positions.Count; i++)
            {
                position = Positions[i];
                if (TypeOfTrade == TradeType.Buy && position.Label == TradeLabel)
                    if (position.TradeType == TypeOfTrade)
                    {
                        if (LastPrice == 0)
                        {
                            LastPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < LastPrice)
                            LastPrice = position.EntryPrice;
                    }
                if (TypeOfTrade == TradeType.Sell && position.Label == TradeLabel)
                    if (position.TradeType == TypeOfTrade)
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
        private int GetStdIlanSignal()
        {
            int Result = -1;
            int LastBarIndex = MarketSeries.Close.Count - 2;
            int PrevBarIndex = LastBarIndex - 1;
            if (MarketSeries.Close[LastBarIndex] > MarketSeries.Open[LastBarIndex])
                if (MarketSeries.Close[PrevBarIndex] > MarketSeries.Open[PrevBarIndex])
                    Result = 0;
            if (MarketSeries.Close[LastBarIndex] < MarketSeries.Open[LastBarIndex])
                if (MarketSeries.Close[PrevBarIndex] < MarketSeries.Open[PrevBarIndex])
                    Result = 1;
            return Result;
        }
        private static double ToHalfAdjust(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);
            return dValue > 0 ? System.Math.Floor((dValue * dCoef) + 0.5) / dCoef : System.Math.Ceiling((dValue * dCoef) - 0.5) / dCoef;
        }
        ////////////////////////////////
        private void MakeTimeCheck()
        {
            int TempNewsTimeHour = 0;
            int TempNewsTimeMin = 0;
            int passday = 0;
            int TempCalcHour = 0;
            TimeCheck = true;

            TempCalcHour = AutoCloseStartHour;
            passday = 0;
            if ((TempCalcHour + LocalTimeZone) > 24)
            {
                passday = 1;
                TempCalcHour = TempCalcHour - 24;
            }
            else if ((TempCalcHour + LocalTimeZone) < 0)
            {
                passday = -1;
                TempCalcHour = TempCalcHour + 24;
            }
            OnCurrentLocaltimeZone = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempCalcHour, AutoCloseStartMin, 0);
            if (passday != 0)
                OnCurrentLocaltimeZone = OnCurrentLocaltimeZone.AddDays(passday);
            // Optional AutoTrade Stop Time ---> All Position and Order Close(Fixed) Allways Friday
            if (FridaySummary)
            {
                DayOfWeek dowclose;
                for (int i = 0; i < 6; i++)
                {
                    dowclose = OnCurrentLocaltimeZone.DayOfWeek;
                    if (dowclose == DayOfWeek.Friday)
                    {
                        TodayFriday = true;
                        break;
                    }
                    OnCurrentLocaltimeZone = OnCurrentLocaltimeZone.AddDays(1);
                }
                if (AutoOpenStartHour >= 0 && AutoOpenStartHour <= 9)
                    AutoClosingTimeZone = OnCurrentLocaltimeZone.AddDays(1);
                else
                    AutoClosingTimeZone = OnCurrentLocaltimeZone;

                if (debug)
                    Print("@@@@AutoClosingTimeZone(Friday Night):" + AutoClosingTimeZone.ToString());
                //
                // Optional AutoTrade Open Time allways Monday
                passday = 0;
                TempCalcHour = AutoOpenStartHour;
                if ((TempCalcHour + LocalTimeZone) > 24)
                {
                    passday = 1;
                    TempCalcHour = TempCalcHour - 24;
                }
                else if ((TempCalcHour + LocalTimeZone) < 0)
                {
                    passday = -1;
                    TempCalcHour = TempCalcHour + 24;
                }
                OnCurrentLocaltimeZone = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempCalcHour, AutoOpenStartMin, 0);
                if (passday != 0)
                    OnCurrentLocaltimeZone = OnCurrentLocaltimeZone.AddDays(passday);
                OnCurrentLocaltimeZone = OnCurrentLocaltimeZone.AddDays(1);
                DayOfWeek dowopen;
                for (int i = 0; i < 6; i++)
                {
                    dowopen = OnCurrentLocaltimeZone.DayOfWeek;
                    if (dowopen == DayOfWeek.Monday)
                    {
                        TodayMonday = true;
                        break;
                    }
                    OnCurrentLocaltimeZone = OnCurrentLocaltimeZone.AddDays(1);
                }
                AutoOpenTimeZone = OnCurrentLocaltimeZone;

                if (debug)
                    Print("@@@@AutoOpenTimeZone(Monday Morning):" + AutoOpenTimeZone.ToString());
            }
            ////////////////////////////////////////////////////////////////////////
            if (TodayMonday)
            {
                if (AsStopBuyNum > 0 || AsStopSellNum > 0)
                {
                    TempCalcHour = AsOrderOpenTimeHour;
                    if ((TempCalcHour + LocalTimeZone) > 24)
                    {
                        passday = 1;
                        TempCalcHour = TempCalcHour - 24;
                    }
                    else if ((TempCalcHour + LocalTimeZone) < 0)
                    {
                        passday = -1;
                        TempCalcHour = TempCalcHour + 24;
                    }
                    var triggerTimeInLocalTimeZoneOpenOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempCalcHour, AsOrederOpenTimeMin, 0);
                    if (passday != 0)
                        triggerTimeInLocalTimeZoneOpenOrder = triggerTimeInLocalTimeZoneOpenOrder.AddDays(passday);
                    if (triggerTimeInLocalTimeZoneOpenOrder < DateTime.Now)
                        triggerTimeInLocalTimeZoneOpenOrder = triggerTimeInLocalTimeZoneOpenOrder.AddDays(1);
                    AutoPlaceOrderOpenTimeZone = triggerTimeInLocalTimeZoneOpenOrder;

                    passday = 0;
                    TempCalcHour = AsOrderCloseTimeHour;
                    if ((TempCalcHour + LocalTimeZone) > 24)
                    {
                        passday = 1;
                        TempCalcHour = TempCalcHour - 24;
                    }
                    else if ((TempCalcHour + LocalTimeZone) < 0)
                    {
                        passday = -1;
                        TempCalcHour = TempCalcHour + 24;
                    }
                    var triggerTimeInLocalTimeZoneCloseOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempCalcHour, AsOrederCloseTimeMin, 0);
                    if (passday != 0)
                        triggerTimeInLocalTimeZoneCloseOrder = triggerTimeInLocalTimeZoneCloseOrder.AddDays(passday);
                    if (triggerTimeInLocalTimeZoneCloseOrder < DateTime.Now)
                        triggerTimeInLocalTimeZoneCloseOrder = triggerTimeInLocalTimeZoneCloseOrder.AddDays(1);
                    AutoPlaceOrderCloseTimeZone = triggerTimeInLocalTimeZoneCloseOrder;
                    PlaceOrderOpen = true;
                    PlaceOrderClose = false;
                }
                OnCurrenttimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TimeZone);
                LastDaytimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, TimeZone);
            }
            ////////////////////////////////////////////////////////////////////////
            if (OpNewsTimePositionClose)
            {
                passday = 0;
                TempCalcHour = NewsTimeHour;
                if ((TempCalcHour + LocalTimeZone) > 24)
                {
                    passday = 1;
                    TempCalcHour = TempCalcHour - 24;
                }
                else if ((TempCalcHour + LocalTimeZone) < 0)
                {
                    passday = -1;
                    TempCalcHour = TempCalcHour + 24;
                }
                var triggerTimeInLocalTimeNewsCloseOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempCalcHour, NewsTimeMin, 0);
                if (passday != 0)
                    triggerTimeInLocalTimeNewsCloseOrder = triggerTimeInLocalTimeNewsCloseOrder.AddDays(passday);
                if (triggerTimeInLocalTimeNewsCloseOrder < DateTime.Now)
                    triggerTimeInLocalTimeNewsCloseOrder = triggerTimeInLocalTimeNewsCloseOrder.AddDays(1);
                AutoNewsTimeCloseTimeZone = triggerTimeInLocalTimeNewsCloseOrder;

                TempNewsTimeHour = NewsTimeHour;
                TempNewsTimeMin = NewsTimeMin;
                if ((TempNewsTimeMin + 5) >= 60)
                {
                    TempNewsTimeMin = (TempNewsTimeMin + 5) - 60;
                    if (TempNewsTimeHour == 23)
                        TempNewsTimeHour = 0;
                    else
                        TempNewsTimeHour = TempNewsTimeHour + 1;
                }
                else
                {
                    TempNewsTimeMin = TempNewsTimeMin + 5;
                }

                passday = 0;
                TempCalcHour = TempNewsTimeHour;
                if ((TempCalcHour + LocalTimeZone) > 24)
                {
                    passday = 1;
                    TempCalcHour = TempCalcHour - 24;
                }
                else if ((TempCalcHour + LocalTimeZone) < 0)
                {
                    passday = -1;
                    TempCalcHour = TempCalcHour + 24;
                }
                var triggerTimeInLocalTimeNewsOpenOrder = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TempNewsTimeHour, TempNewsTimeMin, 0);
                if (passday != 0)
                    triggerTimeInLocalTimeNewsOpenOrder = triggerTimeInLocalTimeNewsOpenOrder.AddDays(passday);
                if (triggerTimeInLocalTimeNewsOpenOrder < DateTime.Now)
                    triggerTimeInLocalTimeNewsOpenOrder = triggerTimeInLocalTimeNewsOpenOrder.AddDays(1);
                AutoNewsTimeOpenTimeZone = triggerTimeInLocalTimeNewsOpenOrder;

            }

        }
    }
}

