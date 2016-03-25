using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using cAlgo.API;
using LumenWorks.Framework.IO.Csv;
using System.Timers;

/*-------------------------------------------------------------------------------------------------
 *
 * Created by Paul Hayes from cAlgo4u (c) 2015
 * This robot is not for selling, it is free. 
 * It provides signals for you to decide what to do with your open positions and robot instances.
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
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class cAlgo4uNewsManager : Robot
    {
        [Parameter("Pause Only News Related Instance?", DefaultValue = true)]
        public bool IncludeNewsReleaseStop { get; set; }

        [Parameter("Pause All Instances?", DefaultValue = true)]
        public bool IncludePauseAllInstances { get; set; }

        [Parameter("Minutes Before News?", DefaultValue = 30)]
        public int MinsB4News { get; set; }

        [Parameter("Minutes After News?", DefaultValue = 60)]
        public int MinsReStart { get; set; }

        [Parameter("Include High?", DefaultValue = true)]
        public bool IncludeHighLevelNews { get; set; }

        [Parameter("Include Medium?", DefaultValue = false)]
        public bool IncludeMediumLevelNews { get; set; }

        [Parameter("MyFxBook News File Path", DefaultValue = "C:\\Users\\Paul\\Documents\\cAlgo\\News Files\\calendar_statement.csv")]
        public string DailyFxDownloadPath { get; set; }

        [Parameter("Email Notifications?", DefaultValue = true)]
        public bool IncludeEmail { get; set; }

        [Parameter("Email Address (Sender)?", DefaultValue = "Your-email")]
        public string EmailFrom { get; set; }

        [Parameter("Email Address (Recipient)?", DefaultValue = "your-email")]
        public string EmailTo { get; set; }

        #region private fields

        FxNews fxNews;
        IList<string> news;
        private cBotLink botLink;
        private List<cAlgo4u.Robot> pausedRobots;
        private Array CurrencyList { get; set; }

        // This is the latest version
        private string cAlgoVersion = "cAlgo News Manager v1.1";

        #endregion

        #region cAlgo Events
        protected override void OnStart()
        {
            // Initialize NEWS
            news = new List<string>();

            // initialize c-bot link
            botLink = new cBotLink();
            botLink.SubKey = "Software\\cAlgo4u\\News Manager\\Alerts";

            // set up container for all paused robots
            pausedRobots = new List<cAlgo4u.Robot>();

            // iterate through enums
            CurrencyList = Enum.GetValues(typeof(Currencies));

            // reset the instrument flags to false, no news
            foreach (Currencies currency in CurrencyList)
            {
                botLink.Write(currency.ToString(), false);
            }

            if (IncludeNewsReleaseStop)
                InitialiseNewsRelease();

            // setup timer to invoke method each minute
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 60000;
            aTimer.Enabled = true;

            Print(cAlgoVersion);

        }

        /// <summary>
        /// called every minute to check for news releases
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (IncludeNewsReleaseStop)
            {
                // re-open any closed news items after the pause period
                foreach (var myBot in pausedRobots)
                {
                    // re-start robot if time is after paused time
                    if (LocalDateTime >= myBot.ReStartTime.AddMinutes(-1))
                    {
                        Print("** " + myBot.Currency + " **");
                        Print("ROBOT RESTARTED.");
                        botLink.Write(myBot.Currency, false);

                        Notifications.SendEmail(EmailFrom, EmailTo, "UPDATE: " + myBot.NewsEvent, "The news event is over and your cAlgo instance has started trading again.");

                        if (IncludePauseAllInstances)
                        {
                            foreach (Currencies currency in CurrencyList)
                            {
                                botLink.Write(currency.ToString(), false);
                            }

                            pausedRobots.Clear();
                            break;
                        }

                        // remove old news item from list
                        pausedRobots.Remove(myBot);
                        break;
                    }
                }

                // close only trades that relate to the currency a news release relates to.
                if (fxNews.IsSuccess)
                {
                    try
                    {
                        string message = string.Empty;

                        foreach (NewsItem item in fxNews.NewsItems)
                        {
                            DateTime timeBeforeNews = item.UtcDateTime.AddMinutes(-MinsB4News);
                            DateTime localTime = LocalDateTime;

                            // if past news continue
                            if (localTime > item.UtcDateTime)
                                continue;

                            // is there a robot for this currency already paused?
                            var botExists = pausedRobots.Where(x => x.Currency == item.Currency).SingleOrDefault();

                            // has news release occurred
                            if (localTime >= timeBeforeNews && localTime < item.UtcDateTime && botExists == null)
                            {
                                Print("");

                                Print("NEWS RELEASE IN " + MinsB4News.ToString() + " MINUTES.");
                                Print(item.Event);

                                // if user wants all instances to pause
                                if (IncludePauseAllInstances)
                                {
                                    foreach (Currencies currency in CurrencyList)
                                    {
                                        // Stop robots from opening any new trades
                                        botLink.Write(currency.ToString(), true);

                                        // store information about the paused robot and add to list of other robots paused.
                                        cAlgo4u.Robot cBot = new cAlgo4u.Robot(item.UtcDateTime, MinsReStart, currency.ToString(), item.Event);

                                        // add to list
                                        pausedRobots.Add(cBot);
                                    }
                                }
                                else
                                {
                                    // only stop individual currency
                                    botLink.Write(item.Currency, true);

                                    // store information about the paused robot and add to list of other robots paused.
                                    cAlgo4u.Robot cBot = new cAlgo4u.Robot(item.UtcDateTime, MinsReStart, item.Currency, item.Event);

                                    // add to list
                                    pausedRobots.Add(cBot);
                                }

                                if (IncludeEmail)
                                {
                                    message = "NEWS EVENT OCCURRING AT " + item.UtcDateTime.ToShortTimeString();
                                    message += "\n\n" + item.Event;
                                    message += "\n\nVolatility: " + item.Importance.ToString().ToUpper();
                                    message += "\nAll currency pairs with " + item.Currency.ToUpper() + " will stop trading and positions will close.";
                                    message += "\nNews event will occur in " + MinsB4News.ToString() + " minutes.";
                                    message += "\nCurrency pairs will be paused for " + MinsReStart.ToString() + " minutes after the news event has occurred.";

                                    Notifications.SendEmail(EmailFrom, EmailTo, "WARNING: " + item.Event, message);
                                }
                            }
                        }
                    } catch (Exception er)
                    {
                        Print("Error occurred shutting down during a news release: " + er.InnerException.ToString());
                    }
                }
            }
        }

        private void InitialiseNewsRelease()
        {
            try
            {
                // load up news release object
                fxNews = new FxNews(DailyFxDownloadPath, IncludeMediumLevelNews, IncludeHighLevelNews);

                // if the news has loaded successfully
                if (fxNews.IsSuccess)
                {
                    news = fxNews.NewsDescriptions;

                    // Display to the log file the upcoming news items
                    foreach (string n in news)
                    {
                        Print(n);
                    }

                    Print(news.Count().ToString() + " IMPORTANT NEWS ITEMS THIS WEEK.");

                    if (news.Count() <= 0)
                    {
                        Print("No news items have been loaded, please check you have the recent news file from FXStreet.");
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                }
                else
                {
                    Print(fxNews.ErrorMessage);
                }
            } catch (Exception e)
            {
                Print("** LOADING NEWS RELEASE FAILED: " + e.InnerException.ToString());
            }
        }

        protected override void OnStop()
        {
            botLink = null;
            fxNews = null;
        }

        #endregion

        #region utilities

        public DateTime LocalDateTime
        {
            get { return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local); }
        }

        #endregion
    }
}
