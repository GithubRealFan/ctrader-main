// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Automate API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//    
//    All changes to this file might be lost on the next application update.
//    If you are going to modify this file please make a copy using the "Duplicate" command.
//
//    The "Sample RSI cBot" will create a buy order when the Relative Strength Index indicator crosses the  level 30, 
//    and a Sell order when the RSI indicator crosses the level 70. The order is closed be either a Stop Loss, defined in 
//    the "Stop Loss" parameter, or by the opposite RSI crossing signal (buy orders close when RSI crosses the 70 level 
//    and sell orders are closed when RSI crosses the 30 level). 
//
//    The cBot can generate only one Buy or Sell order at any given time.
//
// -------------------------------------------------------------------------------------------------

using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SampleRSIcBot : Robot
    {
        [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Source", Group = "RSI")]
        public DataSeries Source { get; set; }

        [Parameter("Periods", Group = "RSI", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter("Lowlevel", Group = "Level", DefaultValue = 30)]
        public int Lowlevel { get; set; }

        [Parameter("Highlevel", Group = "Level", DefaultValue = 70)]
        public int Highlevel { get; set; }

        [Parameter("Stoplossinpips", Group = "Stoploss", DefaultValue = 100)]
        public double Stoplossinpips { get; set; }

        [Parameter("Trailingstopenabled", Group = "Stoploss", DefaultValue = false)]
        public bool Trailingstopenabled { get; set; }

        [Parameter("Waitingtime", Group = "Timedelay", DefaultValue = 5)]
        public int Waitingtime { get; set; }

        [Parameter("Mustafacustom", Group = "Custom", DefaultValue = false)]
        public bool Mustafacustom { get; set; }

        private RelativeStrengthIndex rsi;

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
        }

        protected override void OnTick()
        {
            if (Mustafacustom == false)
            {
                if (rsi.Result.LastValue < Lowlevel)
                {
                    Close(TradeType.Sell);
                    Open(TradeType.Buy);
                }
                else if (rsi.Result.LastValue > Highlevel)
                {
                    Close(TradeType.Buy);
                    Open(TradeType.Sell);
                }
            }
            else
            {
                var longPosition = Positions.Find("CustomRSI", SymbolName, TradeType.Buy);
                var shortPosition = Positions.Find("CustomRSI", SymbolName, TradeType.Sell);
                if (longPosition == null && rsi.Result.LastValue > Highlevel)
                {
                    Close(TradeType.Buy);
                    Close(TradeType.Sell);
                    Open(TradeType.Buy);
                }
                if (longPosition != null && rsi.Result.LastValue < Highlevel)
                {
                    if ((Bars.LastBar.OpenTime - longPosition.EntryTime).TotalMinutes < Waitingtime)
                    {
                        return;
                    }
                    Close(TradeType.Buy);
                }
                if (shortPosition == null && rsi.Result.LastValue < Lowlevel)
                {
                    Close(TradeType.Buy);
                    Close(TradeType.Sell);
                    Open(TradeType.Sell);
                }
                if (shortPosition != null && rsi.Result.LastValue > Lowlevel)
                {
                    if ((Bars.LastBar.OpenTime - shortPosition.EntryTime).TotalMinutes < Waitingtime)
                    {
                        return;
                    }
                    Close(TradeType.Sell);
                }
            }
        }

        private void Close(TradeType tradeType)
        {
            foreach (var position in Positions.FindAll("CustomRSI", SymbolName, tradeType))
                ClosePosition(position);
        }

        private void Open(TradeType tradeType)
        {
            var position = Positions.Find("CustomRSI", SymbolName, tradeType);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);

            if (position == null)
            {
                position = ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, "CustomRSI").Position;
                if (position.TradeType == TradeType.Buy)
                {
                    ModifyPosition(position, position.EntryPrice - Stoplossinpips * Symbol.PipValue, null, Trailingstopenabled);
                }
                else if (position.TradeType == TradeType.Sell)
                {
                    ModifyPosition(position, position.EntryPrice + Stoplossinpips * Symbol.PipValue, null, Trailingstopenabled);
                }
            }
        }
    }
}
