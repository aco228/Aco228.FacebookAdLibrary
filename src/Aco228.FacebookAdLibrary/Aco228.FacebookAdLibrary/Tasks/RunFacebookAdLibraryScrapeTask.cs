using Aco228.Common.Attributes;
using Aco228.Common.Extensions;
using Aco228.FacebookAdLibrary.Documents;
using Aco228.FacebookAdLibrary.Models;
using Aco228.FacebookAdLibrary.Services;
using Aco228.MongoDb.Extensions;
using Aco228.MongoDb.Services;
using Aco228.Runners.Core.Tasks;
using Aco228.Runners.Models.Timings;

namespace Aco228.FacebookAdLibrary.Tasks;

public class RunFacebookAdLibraryScrapeTask : TaskBase
{
    [InjectService] public IFacebookAdExtractService FacebookAdExtractService { get; set; } 
    [InjectService] public IMongoRepo<FacebookAdLibraryPageDocument> PageRepo { get; set; } 
    [InjectService] public IMongoRepo<FacebookAdLibraryDomainDocument> DomainRepo { get; set; }

    public override DelayWindow Delay => new(30, DelayType.Minutes);

    protected override async Task InternalExecute()
    {
        var allPages = await PageRepo.NoTrack().ToListAsync();
        var allDomains = await DomainRepo.NoTrack().ToListAsync();
        
        var pageCandidates = allPages.Where(x => x.LastRunUtc != null && x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5).Shuffle().Take(20);
        var domainCandidates = allDomains.Where(x => x.LastRunUtc != null && x.LastRunUtc.Value.ToDateTimeUtc().GetDaysDifferenceUtc() > 1.5).Shuffle().Take(20);

        var request = new ScrapeRequest()
        {
            PageIds = pageCandidates.Select(x => x.PageId).ToList(),
            Domains = domainCandidates.Select(x => x.Domain).ToList(),
        };
        
        if(!request.Any())
            return;

        var result = await FacebookAdExtractService.Collect(request);

        foreach (var libraryAd in result.LibraryAds)
        foreach (var libraryRes in libraryAd.node.collated_results)
        {
            
        }

    }
}