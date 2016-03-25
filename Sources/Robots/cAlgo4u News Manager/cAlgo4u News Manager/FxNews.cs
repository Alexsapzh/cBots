using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using cAlgo.API;
using LumenWorks.Framework.IO.Csv;

public class FxNews
{
    public List<NewsItem> NewsItems;

    public int MinsBefore { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public IList<string> NewsDescriptions;

    public FxNews(string fxDownloadPath, bool includeMediumLevelNews, bool includeHighLevelNews)
    {
        try
        {
            var downloader = new FxDownloader(fxDownloadPath);
            var allNewsItems = downloader.Download();

            NewsItems = new List<NewsItem>();
            NewsDescriptions = new List<string>();

            NewsItems = NewsRepository.FilterNews(allNewsItems, false, includeMediumLevelNews, includeHighLevelNews);

            if (allNewsItems.Count > -1)
            {
                AddNewsItems();
                IsSuccess = true;
            }
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            NewsItems = new List<NewsItem>();
            IsSuccess = false;
        }
    }

    public void AddNewsItems()
    {
        // get all news items this week
        var upcomingNews = NewsItems.Where(x => x.UtcDateTime >= LocalDateTime).ToList();

        foreach (NewsItem newsItem in upcomingNews)
        {
            string news = string.Empty;

            if (newsItem.Importance == Importance.High)
            {
                NewsDescriptions.Add(newsItem.UtcDateTime.ToLongDateString() + " : " + newsItem.UtcDateTime.ToShortTimeString() + " : " + newsItem.Event + " : " + "HIGH VOLATILITY");
            }

            if (newsItem.Importance == Importance.Medium)
            {
                NewsDescriptions.Add(newsItem.UtcDateTime.ToLongDateString() + " : " + newsItem.UtcDateTime.ToShortTimeString() + " : " + newsItem.Event + " : " + "MEDIUM VOLATILITY");
            }
        }
    }

    public DateTime LocalDateTime
    {
        get { return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local); }
    }

}

public class FxDownloader
{
    private string FxDownloadPath = string.Empty;

    public FxDownloader(string fxDownloadPath)
    {
        FxDownloadPath = fxDownloadPath;
    }

    public List<NewsItem> Download()
    {
        var result = new List<NewsItem>();

        var newsItems = DownloadAndParse();
        result.AddRange(newsItems);

        return result;
    }

    private List<NewsItem> DownloadAndParse()
    {
        var date = LocalDateTime;
        var tmpFileNamePath = FxDownloadPath;

        string csvData = string.Empty;
        //caching
        if (File.Exists(tmpFileNamePath))
        {
            csvData = File.ReadAllText(tmpFileNamePath);
        }

        //parse data
        List<NewsItem> newsItems = ParseDailyFxCsv(date, csvData);
        return newsItems;
    }

    /// <summary>
    /// Parses DailyFx csv data
    /// </summary>
    /// <param name="fileDate"></param>
    /// <param name="csvData"></param>
    /// <returns></returns>
    private List<NewsItem> ParseDailyFxCsv(DateTime fileDate, string csvData)
    {
        var list = new List<NewsItem>();

        // get regional settings for users machine.
        CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;

        // if user has selected another timezone then override the timezones.
        int utcOffset = TimeZoneInfo.Local.BaseUtcOffset.Hours;

        using (var reader = new StringReader(csvData))
        {
            using (var fields = new CsvReader(reader, true))
            {
                while (fields.ReadNextRecord())
                {
                    var newsItem = new NewsItem();

                    var dateTimeStr = fields[0];

                    // news dates in GMT
                    DateTime gmtTime = DateTime.ParseExact(dateTimeStr, "yyyy, MMMM d, HH:mm", currentCulture); 

                    // convert GMT time to Local time.
                    newsItem.UtcDateTime = gmtTime; //.AddHours(utcOffset);

                    // Country for news release
                    switch (fields[6])
                    {
                        case "AUD":
                            newsItem.Currency = Currencies.AUD.ToString();
                            break;
                        case "CAD":
                            newsItem.Currency = Currencies.CAD.ToString();
                            break;
                        case "EUR":
                            newsItem.Currency = Currencies.EUR.ToString();
                            break;
                        case "USD":
                            newsItem.Currency = Currencies.USD.ToString();
                            break;
                        case "GBP":
                            newsItem.Currency = Currencies.GBP.ToString();
                            break;
                        case "JPY":
                            newsItem.Currency = Currencies.JPY.ToString();
                            break;
                        case "CHF":
                            newsItem.Currency = Currencies.CHF.ToString();
                            break;
                    }

                    var newsEvent = fields[1];

                    newsItem.Event = newsEvent;

                    //parse importance
                    var importance = fields[2];
                    newsItem.Importance = ParsingUtil.ParseImportance(importance);

                    newsItem.Actual = fields[3];
                    newsItem.Forecast = fields[4];
                    newsItem.Previous = fields[5];
                    list.Add(newsItem);
                }
            }
        }

        return list;
    }

    public DateTime LocalDateTime
    {
        get { return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local); }
    }
}

public class ParsingUtil
{
    public static Importance? ParseImportance(string importance)
    {
        if (importance == null)
            return null;

        importance = importance.ToLower();

        if (importance == "high")
            return Importance.High;

        if (importance == "medium")
            return Importance.Medium;

        return null;
    }
}

public class NewsRepository
{
    public static List<T> FilterNews<T>(List<T> newsItems, bool showLow, bool showMedium, bool showHigh) where T : INewsItem
    {
        // importance filter               
        var importanceFilter = new List<Importance>();

        if (showMedium)
            importanceFilter.Add(Importance.Medium);

        if (showHigh)
            importanceFilter.Add(Importance.High);

        newsItems = newsItems.Where(x => x.Importance.HasValue && importanceFilter.Contains(x.Importance.Value)).OrderBy(x => x.UtcDateTime).ToList();

        return newsItems;
    }
}

public interface INewsItem
{
    string Event { get; }
    string Currency { get; }
    DateTime UtcDateTime { get; }
    Importance? Importance { get; }
}

public class NewsItem : INewsItem
{
    public DateTime UtcDateTime { get; set; }
    public string TimeZone { get; set; }
    public string Currency { get; set; }
    public string Event { get; set; }
    public Importance? Importance { get; set; }
    public string Actual { get; set; }
    public string Forecast { get; set; }
    public string Previous { get; set; }
}

public class NewsGroup<T> where T : INewsItem
{
    public DateTime Time { get; set; }

    /// <summary>
    /// News for base currency
    /// </summary>
    public CurrencyNews<T> BaseCurrencyNews { get; set; }

    /// <summary>
    /// News for quote currency
    /// </summary>
    public CurrencyNews<T> QuoteCurrencyNews { get; set; }
}

public class CurrencyNews<T> where T : INewsItem
{
    public DateTime Time { get; set; }
    public string Currency { get; set; }
    public List<T> NewsList { get; set; }
}

public class SymbolWrapper
{
    public string BaseCurrency { get; private set; }
    public string QuoteCurrency { get; private set; }

    public SymbolWrapper(string code)
    {
        BaseCurrency = code.Substring(0, 3);
        QuoteCurrency = code.Substring(3, 3);
    }
}

public enum Importance
{
    Low,
    Medium,
    High
}

public enum Currencies
{
    EUR,
    USD,
    GBP,
    CAD,
    AUD,
    JPY,
    CHF
}



