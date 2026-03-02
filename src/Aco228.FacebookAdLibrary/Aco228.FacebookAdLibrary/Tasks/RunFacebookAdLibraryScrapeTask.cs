using System.Text.Json;
using Aco228.Common.Attributes;
using Aco228.Common.Extensions;
using Aco228.FacebookAdLibrary.Core;
using Aco228.FacebookAdLibrary.Documents;
using Aco228.FacebookAdLibrary.Models;
using Aco228.FacebookAdLibrary.Services;
using Aco228.GoogleServices.Extensions;
using Aco228.MongoDb.Extensions;
using Aco228.MongoDb.Extensions.RepoExtensions;
using Aco228.MongoDb.Models;
using Aco228.MongoDb.Services;
using Aco228.Runners.Core.Tasks;
using Aco228.Runners.Models.Timings;
using MongoDB.Bson;

namespace Aco228.FacebookAdLibrary.Tasks;

public class RunFacebookAdLibraryScrapeTask : TaskBase
{
    [InjectService] public IFacebookAdExtractService FacebookAdExtractService { get; set; } 
    [InjectService] public IFacebookAdLibraryBucket FacebookAdLibraryBucket { get; set; } 
    [InjectService] public IMongoRepo<FbLibPageDocument> PageRepo { get; set; } 
    [InjectService] public IMongoRepo<FbLibDomainDocument> DomainRepo { get; set; }
    [InjectService] public IMongoRepo<FbLibAdDocument> AdRepo { get; set; }

    public override DelayWindow Delay => new(30, DelayType.Minutes);

    protected override async Task InternalExecute()
    {
        var allPages = await PageRepo.Track().Full().ToListAsync();
        var allDomains = await DomainRepo.Track().ToListAsync();
        var allAds = await AdRepo.Track().ToListAsync();
        
        var pageCandidates = allPages.Where(x => x.LastRunUtc == null || x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5).Shuffle().Take(20);
        var domainCandidates = allDomains.Where(x => x.LastRunUtc == null || x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5).Shuffle().Take(20);

        var request = new ScrapeRequest()
        {
            PageIds = pageCandidates.Select(x => x.PageId).ToList(),
            Domains = domainCandidates.Select(x => x.Domain).ToList(),
        };
        
        if(!request.Any())
            return;

        var result = await FacebookAdExtractService.Collect(request);
        await FacebookAdExtractService.DisposeAsync();
        ProcessAdLibrary(result, allAds, allDomains, allPages);

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
        
        
        foreach (var page in allPages.Where(page => !string.IsNullOrEmpty(page.PageProfilePictureUrl) && !page.PageProfilePictureUrl.StartsWith("https://storage.googleapis.com/")))
            stateMachine.Schedule(async () =>
            {
                resourceToDownload++;
                var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(page.PageProfilePictureUrl);
                page.PageProfilePictureUrl = file.GetUrl();
            });

        foreach (var ad in allAds)
        foreach (var adVariation in ad.Variations)
        {
            for (int i = 0; i < adVariation.ImageUrls.Count; i++)
                if(!adVariation.ImageUrls[i].StartsWith("https://storage.googleapis.com/"))
                {
                    var index = i;
                    stateMachine.Schedule(async () =>
                    {
                        resourceToDownload++;
                        var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(adVariation.ImageUrls[index]);
                        adVariation.ImageUrls[index] = file.GetUrl();
                    });
                }

            for (int i = 0; i < adVariation.VideoUrls.Count; i++)
                if(!adVariation.VideoUrls[i].StartsWith("https://storage.googleapis.com/"))
                {
                    var index = i;
                    stateMachine.Schedule(async () =>
                    {
                        resourceToDownload++;
                        var file = await FacebookAdLibraryBucket.UploadFromUrlAsync(adVariation.VideoUrls[index]);
                        adVariation.VideoUrls[index] = file.GetUrl();
                    });
                }
        }

        Console.WriteLine($"Total resource to download: {resourceToDownload}");
        return stateMachine;
    }

    private void ProcessAdLibrary(
        ExtractResult result, 
        List<FbLibAdDocument> allAds,
        List<FbLibDomainDocument> allDomains, 
        List<FbLibPageDocument> allPages)
    {
        foreach (var libraryAd in result.LibraryAds)
        foreach (var libraryRes in libraryAd.node.collated_results)
        {
            if (libraryRes.snapshot == null)
                continue;
            
            var ad = allAds.FirstOrDefault(x => x.AdId == libraryRes.ad_archive_id);
            if (ad == null)
            {
                ConsoleLog($"New.Ad == {libraryRes.ad_archive_id}");
                ad = new() { AdId = libraryRes.ad_archive_id, };
                allAds.Add(ad);
            }
            
            var page = allPages.FirstOrDefault(x => x.PageId.ToString() == libraryRes.page_id);
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

            var snapshot = libraryRes.snapshot;

            page.LastRunUtc = DT.GetUnix();
            page.Name = libraryRes.page_name;
            page.Byline = libraryRes.snapshot.byline;
            page.PageUrl = libraryRes.snapshot.page_profile_uri;
            
            if (string.IsNullOrEmpty(page.PageProfilePictureUrl))
                page.PageProfilePictureUrl = libraryRes.snapshot!.page_profile_picture_url;

            if (ad.Id != ObjectId.Empty)
            {
                UpdateDomainLastRunUtc(allDomains, ad);
                continue;
            }

            try
            {
                ad.Variations.Add(new()
                {
                    Caption = snapshot.caption,
                    CtaText = snapshot.cta_text,
                    Title = snapshot.title,
                    LinkUrl = snapshot.link_url,
                    DomainUrl = snapshot.link_url?.Remove("https://").Remove("www.").Split("?").First().Split("/").First() ?? "",
                    Body = snapshot.body?.text,
                    ImageUrls = snapshot.images?.Select(x => x.original_image_url).ToList() ?? new(),
                });
            }
            catch
            {
                int a = 0;
            }

            if (snapshot.cards?.Count > 0)
                foreach (var cardDto in snapshot.cards)
                    ad.Variations.Add(new()
                    {
                        Caption = cardDto.caption,
                        CtaText = cardDto.cta_text,
                        Title = cardDto.title,
                        LinkUrl = cardDto.link_url,
                        DomainUrl = snapshot.link_url?.Remove("https://").Remove("www.").Split("?").First().Split("/").First() ?? "",
                        Body = cardDto.title, 
                        ImageUrls = new(){ cardDto.original_image_url },
                        VideoUrls = new(){ cardDto.video_sd_url },
                    });

            UpdateDomainLastRunUtc(allDomains, ad);
            
            ad.StartDate = libraryRes.start_date;
            ad.EndDate = libraryRes.end_date;
            ad.PublishPlatforms = libraryRes.publisher_platform;
            ad.Raw = JsonSerializer.Serialize(libraryRes);
        }
    }

    private static void UpdateDomainLastRunUtc(List<FbLibDomainDocument> allDomains, FbLibAdDocument ad)
    {
        foreach (var adVariation in ad.Variations)
        {
            var domain = allDomains.FirstOrDefault(x => x.Domain == adVariation.DomainUrl);
            if(domain != null)
                domain.LastRunUtc = DT.GetUnix();
        }
    }
}