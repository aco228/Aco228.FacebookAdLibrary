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
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aco228.FacebookAdLibrary.Tasks;

public class RunFacebookAdLibraryDeleteTask : TaskBase
{
    private const int MAXIMUM_PER_TURN = 300;
     
    [InjectService] public IFacebookAdLibraryBucket FacebookAdLibraryBucket { get; set; }
    [InjectService] public IMongoRepo<FbLibAdDocument> AdRepo { get; set; }

    public override DelayWindow Delay => new(30, DelayType.Minutes);

    protected override async Task InternalExecute()
    {
        var lastScanUtc = DateTime.UtcNow.AddDays(-2).ToDT();
        
        var ads = await AdRepo.Track().Full().Lt(x => x.LastScanUtc, lastScanUtc).OrderByPropertyDesc(x => x.LastScanUtc).Limit(MAXIMUM_PER_TURN).ToListAsync();
        foreach (var ad in ads)
        {
            foreach (var variation in ad.Variations)
            {
                //https://storage.googleapis.com/arbo-facebook-ad-library1/649650318_1550561902689482_3359284673420269233_n.jpg
                foreach (var url in variation.ImageUrls)
                {
                    var fileName = url.Remove("https://storage.googleapis.com/").Remove(FacebookAdLibraryBucket.BucketName + "/");
                    await FacebookAdLibraryBucket.DeleteFileByName(fileName);
                }
                foreach (var url in variation.VideoPreview)
                {
                    var fileName = url.Remove("https://storage.googleapis.com/").Remove(FacebookAdLibraryBucket.BucketName + "/");
                    await FacebookAdLibraryBucket.DeleteFileByName(fileName);
                }
                foreach (var url in variation.VideoUrls)
                {
                    var fileName = url.Remove("https://storage.googleapis.com/").Remove(FacebookAdLibraryBucket.BucketName + "/");
                    await FacebookAdLibraryBucket.DeleteFileByName(fileName);
                }
            }
        }

        await AdRepo.DeleteManyAsync(ads);
    }
}