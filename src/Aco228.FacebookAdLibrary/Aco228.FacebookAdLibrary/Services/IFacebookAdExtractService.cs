using System.Collections.Concurrent;
using System.Net;
using Aco228.Common.Extensions;
using Aco228.Common.Models;
using Aco228.FacebookAdLibrary.Browser;
using Aco228.FacebookAdLibrary.Models;
using Aco228.FacebookAdLibrary.Models.Extract;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aco228.FacebookAdLibrary.Services;

public interface IFacebookAdExtractService : ITransient, IAsyncDisposable
{
    Task<ExtractResult> Collect(ScrapeRequest request);
}

public class FacebookAdExtractService : IFacebookAdExtractService
{
    private readonly IFacebookAdLibraryBrowser _browser;
    private ExtractResult _result = new();
    public HashSet<string> AdIds { get; set; } = new();
    private FetchModel? _fetchModelAds = null;
    private FetchModel? _fetchModelAdDetails = null;

    public FacebookAdExtractService(IFacebookAdLibraryBrowser browser)
    {
        _browser = browser;
    }

    public async Task<ExtractResult> Collect(ScrapeRequest request)
    {
        await _browser.Launch(openAsHeadless: false);
        _browser.Page.Response += async (_, response) => await OnPageResponse(response);

        try
        {
            await Execute(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await _browser.DisposeAsync();   
        }

        return _result;
    }

    public async Task<ExtractResult> Execute(ScrapeRequest request)
    {
        const string pageId = "547168535157118";
        string url = $"https://www.facebook.com/ads/library/?active_status=active&ad_type=all&country=ALL&is_targeted_country=false&media_type=all&search_type=page&sort_data[mode]=total_impressions&sort_data[direction]=desc&view_all_page_id=" + pageId;
        _browser.Page.RouteAsync("**/*", ImplOnRequest).ConfigureAwait(true);
        await _browser.Page.GotoAsync(url, new()
        {
            WaitUntil = WaitUntilState.Load,
        });
        
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        for (;;)
        {
            if (_fetchModelAds != null)
                break;
            
            Console.WriteLine($"Scrolling..");
            await Task.Delay(2500); 
            await _browser.Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight);");
                
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        var elements = _browser.Page.GetByText("See ad details");
        var element = elements.Nth(1);
        await element.WaitForAsync();
        await element.ClickAsync();
        
        for (;;)
        {
            if (_fetchModelAdDetails != null)
                break;
            
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        
        var variables = _fetchModelAds.GetPostData()["variables"];
        var model = JsonConvert.DeserializeObject<FacebookSearchVariable>(WebUtility.UrlDecode(variables));
        
        var adDetailVariable = _fetchModelAdDetails.GetPostData()["variables"];
        var adDetailsModel = JsonConvert.DeserializeObject<FacebookAdDetailVariable>(WebUtility.UrlDecode(adDetailVariable));

        Console.WriteLine($"<<<< Searching for pages");
        foreach (var requestPageId in request.PageIds)
        {
            model.cursor = null;
            model.searchType = "page";
            model.queryString = "";
            model.viewAllPageID = requestPageId.ToString();
            
            Console.WriteLine($" >> Start search for page {requestPageId}");

            for (int i = 0; i < 4; i++)
            {
                if (await ProcessAdLibraryDataAsync(model) == false) 
                    break;
            }
            
            Console.WriteLine(">>>> Searching for ad details");
            foreach (var adId in AdIds)
                await ProcessAdDetail(adId, adDetailsModel);
            
            AdIds.Clear();
        }

        Console.WriteLine($"<<<< Searching for domains");
        foreach (var domain in request.Domains)
        {
            model.cursor = null;
            model.searchType = "KEYWORD_UNORDERED";
            model.queryString = domain;
            model.viewAllPageID = "0";
            
            Console.WriteLine($" >> Start search for domain {domain}");

            for (int i = 0; i < 4; i++)
            {
                if (await ProcessAdLibraryDataAsync(model) == false) 
                    break;
            }
            
            Console.WriteLine(">>>> Searching for ad details");
            foreach (var adId in AdIds)
                await ProcessAdDetail(adId, adDetailsModel);
            
            AdIds.Clear();
        }

        return _result;
    }

    private async Task ProcessAdDetail(string adId, FacebookAdDetailVariable variable)
    {
        if(!_result.AdPageMap.TryGetValue(adId, out var pageId))
            return;
        
        variable.adArchiveID = adId;
        variable.pageID = pageId;
        
        _fetchModelAdDetails.ReplacePostData("variables", JsonConvert.SerializeObject(variable));
        var js = _fetchModelAdDetails.SaveAsFetch();
        var text = await _browser.Page.EvaluateAsync<string>(js);
        if (text.StartsWith("{\"errors\":"))
            return;

        var json = JToken.Parse(text);
        var jsonTxt = json["data"]["ad_library_main"]?["ad_details"]?.ToString();
        
        try
        {
            var data = JsonConvert.DeserializeObject<AdDetailsDTO>(jsonTxt);
            if (data.transparency_by_location == null)
                return;

            if (data.transparency_by_location.uk_transparency != null)
                _result.AddCountryForAd(adId, "GB");

            if (data.transparency_by_location.br_transparency != null)
                _result.AddCountryForAd(adId, "BR");

            if (data.transparency_by_location.eu_transparency != null)
            {
                foreach (var countryBreakdown in data.transparency_by_location.eu_transparency.age_country_gender_reach_breakdown)
                    _result.AddCountryForAd(adId, countryBreakdown.country.ToUpper());
            }
        }
        catch(Exception ex)
        {
            return;
        }
    }

    private async Task<bool> ProcessAdLibraryDataAsync(FacebookSearchVariable model)
    {
        _fetchModelAds.ReplacePostData("variables", JsonConvert.SerializeObject(model));
        var js = _fetchModelAds.SaveAsFetch();
        var text = await _browser.Page.EvaluateAsync<string>(js);
        if (text.StartsWith("{\"errors\":"))
            return false;
        
        var nextCursor = ExtractAndProcessAdLibraryData(text);
        if (string.IsNullOrEmpty(nextCursor))
            return false;

        model.cursor = nextCursor;
        return true;
    }

    private async Task OnPageResponse(IResponse response)
    {
        if(!response.Url.Equals("https://www.facebook.com/api/graphql/"))
            return;
        
        if (response.Status >= 300 && response.Status < 400)
            return;

        var text = await response.TextAsync();
        var json = JToken.Parse(text);
        try
        {
            if (_fetchModelAds == null)
            {
                if (json["data"]["ad_library_main"]?["search_results_connection"]?["edges"] != null)
                {
                    _fetchModelAds = new FetchModel(response.Request);
                }
            }

            if (_fetchModelAdDetails == null)
            {
                if (json["data"]["ad_library_main"]?["ad_details"] != null)
                {
                    _fetchModelAdDetails = new FetchModel(response.Request);
                }
            }
        }
        catch
        {
            Console.WriteLine("Exception serializer");
        }
    }

    private string? ExtractAndProcessAdLibraryData(string text)
    {
        var json = JToken.Parse(text);
        var jsonTxt = json["data"]["ad_library_main"]?["search_results_connection"]?["edges"].ToString();
        var data = JsonConvert.DeserializeObject<List<LibraryAdModel>>(jsonTxt);

        foreach (var dataEntry in data)
        {
            string? id = null;
            foreach (var collatedResult in dataEntry.node.collated_results)
            {
                if(collatedResult.start_date == null)
                    continue;
                
                var startDate = collatedResult.start_date.Value.ToDateTimeSecondsUtc();
                if(startDate.GetDaysDifference() < 10)
                    continue;
                
                id = collatedResult.ad_archive_id;
                if(!string.IsNullOrEmpty(collatedResult.page_id))
                    _result.AdPageMap.AddOrUpdate(id, collatedResult.page_id);
            }

            if (string.IsNullOrEmpty(id))
                continue;
            
            
            AdIds.Add(id);
            _result.Add(dataEntry);
        }
                
        Console.WriteLine("Found ad-library-main");
        var end_cursor = json["data"]["ad_library_main"]?["search_results_connection"]?["page_info"]?["end_cursor"].ToString();
        var has_next_page = json["data"]["ad_library_main"]?["search_results_connection"]?["page_info"]?["has_next_page"].ToString();
        if (has_next_page == "false")
            return null;

        return end_cursor;
    }

    private async Task ImplOnRequest(IRoute route)
    {
        await route.ContinueAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
    }
}