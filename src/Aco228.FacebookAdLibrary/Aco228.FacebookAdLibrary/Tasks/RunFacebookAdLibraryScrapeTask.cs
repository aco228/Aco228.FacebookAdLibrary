using System.Text;
using System.Text.Json;
using Aco228.Common.Attributes;
using Aco228.Common.Extensions;
using Aco228.FacebookAdLibrary.Core;
using Aco228.FacebookAdLibrary.Documents;
using Aco228.FacebookAdLibrary.Models;
using Aco228.FacebookAdLibrary.Services;
using Aco228.GoogleServices.Extensions;
using Aco228.MongoDb.Extensions;
using Aco228.MongoDb.Extensions.MongoFiltersExtensions;
using Aco228.MongoDb.Extensions.RepoExtensions;
using Aco228.MongoDb.Models;
using Aco228.MongoDb.Services;
using Aco228.Runners.Core.Tasks;
using Aco228.Runners.Models.Timings;
using Lingua;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aco228.FacebookAdLibrary.Tasks;

public class RunFacebookAdLibraryScrapeTask : TaskBase
{
    private const int MAXIMUM_PAGES_PER_TURN = 10;
    private const int MAXIMUM_DOMAINS_PER_TURN = 10;
    private const int MINIMUM_DAYS = 7;
    
    public override DelayWindow Delay => new(30, DelayType.Minutes);
    
    [InjectService] public IFacebookAdExtractService FacebookAdExtractService { get; set; } 
    [InjectService] public IFacebookAdLibraryBucket FacebookAdLibraryBucket { get; set; } 
    [InjectService] public IMongoRepo<FbLibPageDocument> PageRepo { get; set; } 
    [InjectService] public IMongoRepo<FbLibDomainDocument> DomainRepo { get; set; }
    [InjectService] public IMongoRepo<FbLibAdDocument> AdRepo { get; set; }

    public LanguageDetector? LanguageDetector { get; set; }

    protected override async Task InternalExecute()
    {
        var allPages = await PageRepo.Track().Full().ToListAsync();
        var allDomains = await DomainRepo.Track().ToListAsync();
        
        var pageCandidates = allPages
            .Where(x => x.IsIgnored == false && (x.LastRunUtc == null || x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5))
            .Shuffle()
            .Take(MAXIMUM_PAGES_PER_TURN);
        
        var domainCandidates = allDomains
            .Where(x => x.LastRunUtc == null || x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5)
            .Shuffle()
            .Take(MAXIMUM_DOMAINS_PER_TURN);
        
        var request = new ScrapeRequest()
        {
            PageIds = pageCandidates.Select(x => x.PageId).ToList(),
            Domains = domainCandidates.Select(x => x.Domain).ToList(),
        };

        var adsFilters = new List<FilterDefinition<FbLibAdDocument>>();
        adsFilters.Add(Builders<FbLibAdDocument>.Filter.Or(
            Builders<FbLibAdDocument>.Filter.In(x => x.PageId, pageCandidates.Select(x => x.PageId)),
            Builders<FbLibAdDocument>.Filter.In(x => x.DomainUrl, domainCandidates.Select(x => x.Domain))
            ));
        
        var allAds = await AdRepo
            .Track()
            .Full()
            .FilterBy(adsFilters)
            .ToListAsync();
        
        if(!request.Any())
            return;

        FacebookAdExtractService.SetMaximumDays(MINIMUM_DAYS);
        var result = await FacebookAdExtractService.Collect(request);
        await FacebookAdExtractService.DisposeAsync();
        
        LanguageDetector = LanguageDetectorBuilder
            .FromAllLanguages()
            .WithLowAccuracyMode()
            .Build();
        
        await ProcessAdLibrary(result, allAds, allDomains, allPages);

        foreach (var candidate in domainCandidates)
            candidate.LastRunUtc = DT.GetUnix();
        
        foreach (var candidate in pageCandidates) 
            candidate.LastRunUtc = DT.GetUnix();

        var stateMachine = UploadResources(allPages, allAds);
        await stateMachine.Wait();

        await PageRepo.InsertOrUpdateManyAsync(allPages);
        await DomainRepo.InsertOrUpdateManyAsync(allDomains);
        await AdRepo.InsertOrUpdateManyAsync(allAds);
    }

    private TaskStateMachine UploadResources(List<FbLibPageDocument> allPages, List<FbLibAdDocument> allAds)
    {
        var stateMachine = new TaskStateMachine().SetLimit(20);
        int resourceToDownload = 0;


        foreach (var page in allPages.Where(page =>
                     !string.IsNullOrEmpty(page.PageProfilePictureUrl) &&
                     !page.PageProfilePictureUrl.StartsWith("https://storage.googleapis.com/")))
        {
            resourceToDownload++;
            stateMachine.Schedule(async () =>
            {
                resourceToDownload++;
                var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(page.PageProfilePictureUrl);
                page.PageProfilePictureUrl = file.GetUrl();
            });
        }

        foreach (var ad in allAds)
        foreach (var adVariation in ad.Variations)
        {
            for (int i = 0; i < adVariation.ImageUrls.Count; i++)
                if(!adVariation.ImageUrls[i].StartsWith("https://storage.googleapis.com/"))
                {
                    var index = i;
                    resourceToDownload++;
                    stateMachine.Schedule(async () =>
                    {
                        var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(adVariation.ImageUrls[index]);
                        adVariation.ImageUrls[index] = file.GetUrl();
                    });
                }
            
            for (int i = 0; i < adVariation.VideoPreview.Count; i++)
                if(!adVariation.VideoPreview[i].StartsWith("https://storage.googleapis.com/"))
                {
                    var index = i;
                    resourceToDownload++;
                    stateMachine.Schedule(async () =>
                    {
                        var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(adVariation.VideoPreview[index]);
                        adVariation.VideoPreview[index] = file.GetUrl();
                    });
                }

            for (int i = 0; i < adVariation.VideoUrls.Count; i++)
                if(!adVariation.VideoUrls[i].StartsWith("https://storage.googleapis.com/"))
                {
                    var index = i;
                    resourceToDownload++;
                    stateMachine.Schedule(async () =>
                    {
                        var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(adVariation.VideoUrls[index]);
                        adVariation.VideoUrls[index] = file.GetUrl();
                    });
                }
        }

        Console.WriteLine($"Total resource to download: {resourceToDownload}");
        return stateMachine;
    }

    private async Task ProcessAdLibrary(
        ExtractResult result, 
        List<FbLibAdDocument> allAds,
        List<FbLibDomainDocument> allDomains, 
        List<FbLibPageDocument> allPages)
    {
        foreach (var libraryAd in result.LibraryAds)
        foreach (var libraryRes in libraryAd.node.collated_results)
        {
            if (libraryRes.snapshot == null) continue;
            if (libraryRes.start_date == null) continue;

            if (result.AdErrors.Contains(libraryRes.ad_id)) 
                continue;

            var startDate = libraryRes.start_date.Value.ToDateTimeSecondsUtc();
            if (startDate.GetDaysDifference() < MINIMUM_DAYS)
                continue;

            var maximumDays = 4 * 31;
            if (startDate.GetDaysDifference() > maximumDays)
                continue;

            var reach = result.AdReach.TryGetValue(libraryRes.ad_archive_id, out var reachCollection) ? reachCollection :  new List<long>();
            var totalReach = reach.Sum();
                
            var page = allPages.FirstOrDefault(x => x.PageId.ToString() == libraryRes.page_id);
            if (page == null)
            {
                page = await PageRepo.NoTrack().Full().Eq(x => x.PageId, libraryRes.PageId).FirstOrDefaultAsync();
                if (page == null)
                {
                    ConsoleLog($"Found page == {libraryRes.page_name}");
                    page = new()
                    {
                        PageId = long.Parse(libraryRes.page_id),
                        Name = libraryRes.page_name,
                    };
                    allPages.Add(page);                    
                }
                
                if (page.IsIgnored == true)
                    continue;
            }
            else
            {
                page.LastRunUtc = DT.GetUnix();
            }
            
            page.Name = libraryRes.page_name;
            page.Byline = libraryRes.snapshot.byline;
            
            if (string.IsNullOrEmpty(page.PageProfilePictureUrl))
                page.PageProfilePictureUrl = libraryRes.snapshot!.page_profile_picture_url;
            
            var ad = allAds.FirstOrDefault(x => x.AdId == libraryRes.ad_archive_id);
            if (ad == null)
            {
                ad = new() { AdId = libraryRes.ad_archive_id, PageId = page.PageId };
            }

            var snapshot = libraryRes.snapshot;
            result.AdCountries.TryGetValue(libraryRes.ad_archive_id, out var countries);

            ad.Countries = countries ?? new();
            ad.LastScanUtc = DT.GetUnix();
            ad.TotalReach = totalReach;
            ad.SearchBy = $"{snapshot.title} {snapshot.caption} {snapshot.body?.text}";
            ad.DomainUrl = snapshot.link_url?.Remove("https://").Remove("www.").Split("?").First().Split("/").First().Trim().ToLower() ?? "";
            
            if (ad.Id != ObjectId.Empty)
            {
                UpdateDomainLastRunUtc(allDomains, ad);
                continue;
            }

            ad.Variations.Add(new()
            {
                Caption = snapshot.caption,
                CtaText = snapshot.cta_text,
                Title = snapshot.title,
                LinkUrl = snapshot.link_url,
                Body = snapshot.body?.text,
                ImageUrls = snapshot.images?.Select(x => x.original_image_url).ToList() ?? new(),
                VideoPreview = snapshot.videos?.Select(x => x.video_preview_image_url).ToList() ?? new(),
                VideoUrls = snapshot.videos?.Select(x => x.video_sd_url).ToList() ?? new(),
            });

            if (snapshot.cards?.Count > 0)
                foreach (var cardDto in snapshot.cards)
                    ad.Variations.Add(new()
                    {
                        Caption = cardDto.caption,
                        CtaText = cardDto.cta_text,
                        Title = cardDto.title,
                        LinkUrl = cardDto.link_url,
                        Body = cardDto.title, 
                        ImageUrls = string.IsNullOrEmpty(cardDto.original_image_url) ? new() :new(){ cardDto.original_image_url },
                        VideoPreview = string.IsNullOrEmpty(cardDto.video_preview_image_url) ? new() : new(){ cardDto.video_preview_image_url },
                        VideoUrls = string.IsNullOrEmpty(cardDto.video_sd_url) ? new() : new(){ cardDto.video_sd_url },
                    });

            if (!ad.AreVariationsCorrect())
                continue;

            var txtsb = new StringBuilder();
            foreach (var adVariation in ad.Variations)
                txtsb.Append($"{adVariation.Title} {adVariation.Body} ");
            
            var lng = LanguageDetector!.ComputeLanguageConfidenceValues(txtsb.ToString());
            ad.LanguageCode = lng.FirstOrDefault().Key.IsoCode6391().ToString().ToUpper();

            // UpdateDomainLastRunUtc(allDomains, ad);
            
            ad.StartDate = libraryRes.start_date;
            ad.EndDate = libraryRes.end_date;
            ad.PublishPlatforms = libraryRes.publisher_platform;
            ad.Raw = JsonSerializer.Serialize(libraryRes);

            if (ad.Id == ObjectId.Empty)
            {
                ConsoleLog($"New.Ad == {libraryRes.ad_archive_id}");
                allAds.Add(ad);
            }
        }
    }

    private static void UpdateDomainLastRunUtc(List<FbLibDomainDocument> allDomains, FbLibAdDocument ad)
    {
        var domain = allDomains.FirstOrDefault(x => x.Domain.Equals(ad.DomainUrl, StringComparison.InvariantCultureIgnoreCase));
        if(domain != null)
            domain.LastRunUtc = DT.GetUnix();
    }
}