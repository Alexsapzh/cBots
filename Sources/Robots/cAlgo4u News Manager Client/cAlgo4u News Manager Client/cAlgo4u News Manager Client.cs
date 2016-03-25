using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Microsoft.Win32;
using System.Timers;
using System.Collections.Generic;

/*-------------------------------------------------------------------------------------------------
 *
 * Created by cAlgo4u (c) 2015
 * 
 * It reads signals from the cAlgo4u News Manager Robot to pause trading and closing of open positions.
 * When news release is over the trading restarts automatically.
 * 
 * THIS IS JUST A TEMPLATE FOR YOU TO INCORPORATE INTO YOUR OWN AUTOMATED STRATEGIES.
 * 
 * IF USED INCORRECTLY IT COULD CAUSE LOSS OF MONEY, READ THE SUPPORTING DOCUMENTATION BEFORE USE.
 * PLEASE CONDUCT COMPLETE TESTS ON A DEMO ACCOUNT BEFORE USING ON A LIVE ACCOUNT.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
---------------------------------------------------------------------------------------------------*/

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.Registry)]
    public class cAlgo4uNewsManagerClient : Robot
    {
        [Parameter("Include News Release Pause?", DefaultValue = false)]
        public bool IncludeNewsReleasePause { get; set; }

        [Parameter("Close Positions Before News Release?", DefaultValue = false)]
        public bool ClosePositionsNewsRelease { get; set; }

        #region private fields

        private bool isPause { get; set; }
        private System.Timers.Timer newsTimer;

        #endregion

        #region cTrader events

        protected override void OnStart()
        {
            isPause = false;

            // setup timer to invoke method each minute to check for news releases
            newsTimer = new System.Timers.Timer();
            newsTimer.Elapsed += new ElapsedEventHandler(OnTimedEventForNews);
            newsTimer.Interval = 60000;
            // 60,000 = 60 seconds
            newsTimer.Enabled = true;
        }

        protected override void OnBar()
        {
            // if a news release then pause robot, just stop trading by existing this method.
            if (isPause)
            {
                return;
            }

            // add trading logic here
        }

        protected override void OnTick()
        {
            // if a news release then pause robot, just stop trading by existing this method.
            if (isPause)
            {
                return;
            }

            // add trading logic here
        }

        /// <summary>
        /// When cBot stops disable the timer.
        /// </summary>
        protected override void OnStop()
        {
            newsTimer.Enabled = false;
            newsTimer.Stop();
        }

        #endregion

        #region Timer events

        /// <summary>
        /// Timer event called each minute to check for News Releases
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private void OnTimedEventForNews(object sender, ElapsedEventArgs ev)
        {
            // shall we include a news release pause?
            if (IncludeNewsReleasePause)
            {
                if (IsNewsRelease())
                {
                    isPause = true;
                }
                else
                {
                    isPause = false;
                }

                // if user wants to close positions before a major news release, do it here.
                if (ClosePositionsNewsRelease)
                {
                    if (isPause)
                    {
                        // get all open positions and close.
                        foreach (var position in Positions)
                        {
                            ClosePosition(position);
                        }
                    }

                }
            }
        }

        #endregion

        #region Logic to check for news release

        /// <summary>
        /// Returns true if the is a news release (x) minutes before a news event, during and (x) minutes after
        /// </summary>
        /// <returns></returns>
        private bool IsNewsRelease()
        {
            bool isNews = false;

            try
            {
                // checks for any news releases.
                NewsAlerts alerts = new NewsAlerts();

                // iterate through currency list
                foreach (var currency in alerts.GetCurrencyList())
                {
                    // if the currency pair contains the symbol in the news
                    if (this.Symbol.Code.Contains(currency))
                    {
                        // read the signal
                        isNews = alerts.Read(currency);
                    }
                }

                alerts = null;

            } catch (Exception e)
            {
                Print("Failed reading registry for news release manager:  " + e.InnerException.ToString());
            }

            return isNews;
        }

        #endregion
    }

    #region News Release Signals

    /// <summary>
    /// this class has methods that just read the registry for news releases only
    /// </summary>
    public class NewsAlerts
    {
        public string SubKey = "Software\\cAlgo4u\\News Manager\\Alerts";

        public IList<string> GetCurrencyList()
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(SubKey);

            // Open a subKey as read-only
            IList<string> subKeyNames = rk.GetValueNames().ToList();

            return subKeyNames;
        }

        public bool Read(string KeyName)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;

            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(SubKey);
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return false;
            }
            else
            {
                var result = sk1.GetValue(KeyName.ToUpper());
                return Convert.ToBoolean(result);
            }
        }
    }

    #endregion
}
