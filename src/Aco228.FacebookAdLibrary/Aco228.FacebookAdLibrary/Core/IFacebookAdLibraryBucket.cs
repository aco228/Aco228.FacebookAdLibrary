using Aco228.Common.Models;
using Aco228.GoogleServices.Services;

namespace Aco228.FacebookAdLibrary.Core;

public interface IFacebookAdLibraryBucket : IGoogleBucket, ITransient
{
    
}

public class FacebookAdLibraryBucket : GoogleBucket, IFacebookAdLibraryBucket
{
    public override string BucketName => "arbo-facebook-ad-library";
    public FacebookAdLibraryBucket(IGoogleClientProvider googleClientProvider) : base(googleClientProvider)
    {
    }

}