using System.Net;
using Aco228.Common.Models;
using Aco228.FacebookAdLibrary.Browser;
using Aco228.FacebookAdLibrary.Core;
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
    private FetchModel? _fetchModelAds = null;

    public FacebookAdExtractService(IFacebookAdLibraryBrowser browser)
    {
        _browser = browser;
    }


    public async Task<ExtractResult> Collect(ScrapeRequest request)
    {
        const string pageId = "547168535157118";
        string url = $"https://www.facebook.com/ads/library/?active_status=active&ad_type=all&country=ALL&is_targeted_country=false&media_type=all&search_type=page&sort_data[mode]=total_impressions&sort_data[direction]=desc&source=page-transparency-widget&view_all_page_id=" + pageId;

        await _browser.Launch(openAsHeadless: false);
        _browser.Page.Response += async (_, response) => await OnPageResponse(response);
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
        
        var variables = _fetchModelAds.GetPostData()["variables"];
        var model = JsonConvert.DeserializeObject<FacebookSearchVariable>(WebUtility.UrlDecode(variables));

        Console.WriteLine($"<<<< Searching for pages");
        foreach (var requestPageId in request.PageIds)
        {
            model.cursor = null;
            model.searchType = "page";
            model.queryString = "";
            model.viewAllPageID = requestPageId.ToString();
            
            Console.WriteLine($" >> Start search for page {requestPageId}");

            for (int i = 0; i < 3; i++)
            {
                if (await ProcessAdLibraryDataAsync(model) == false) 
                    break;
            }
        }

        Console.WriteLine($"<<<< Searching for domains");
        foreach (var domain in request.Domains)
        {
            model.cursor = null;
            model.searchType = "KEYWORD_UNORDERED";
            model.queryString = domain;
            model.viewAllPageID = "0";
            
            Console.WriteLine($" >> Start search for domain {domain}");

            for (int i = 0; i < 3; i++)
            {
                if (await ProcessAdLibraryDataAsync(model) == false) 
                    break;
            }
        }

        return _result;
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
            if (json["data"]["pages"] != null)
            {
                var jsonTxt = json["data"]["user"].ToString();
                var page = JsonConvert.DeserializeObject<PageModel>(jsonTxt);
                if (!_result.Pages.Any(x => x.id == page.id))
                {
                    _result.Pages.Add(page);
                    Console.WriteLine($"Page {page.name} is extracted");
                }
            }

            if (json["data"]["ad_library_main"]?["search_results_connection"]?["edges"] != null)
            {
                if (_fetchModelAds == null)
                {
                    _fetchModelAds = new FetchModel(response.Request);
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
            if(_result.LibraryAds.All(x => x.id != dataEntry.id))
                _result.LibraryAds.Add(dataEntry);
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
        // if (!route.Request.Url.Equals("https://www.facebook.com/api/graphql/"))
        // {
        //     await route.ContinueAsync();
        //     return;
        // }
        //
        // var postData = route.Request.PostData;
        // if (postData.Contains("&fb_api_req_friendly_name=PolarisStoriesV3SeenMutation"))
        // {
        //     await route.AbortAsync();
        //     return;
        // }
        
        await route.ContinueAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
    }
}