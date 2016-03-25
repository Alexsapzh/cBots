using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cAlgo4u
{
    #region robot entities

    /// <summary>
    /// holds a single robot that has been paused
    /// </summary>
    public class Robot
    {
        private DateTime newsTime;
        private double minutesToStart;

        public Robot(DateTime newsReleaseTime, double minsToRestartAfterNews, string currency, string newsEvent)
        {
            newsTime = newsReleaseTime;
            minutesToStart = minsToRestartAfterNews;
            Currency = currency;
            NewsEvent = newsEvent;
        }

        public string Currency { get; set; }
        public string NewsEvent { get; set; }

        public DateTime ReStartTime
        {
            get { return newsTime.AddMinutes(minutesToStart); }
        }
    }

    #endregion
}
