using Aco228.Common.Models;
using Aco228.MongoDb.Services;

namespace Aco228.FacebookAdLibrary.Core;

public interface IFacebookAdLibraryDbContext : IMongoDbContext, ISingleton
{
    
}

public class FacebookAdLibraryDbContext : MongoDbContext, IFacebookAdLibraryDbContext
{
    public override string DatabaseName => "FacebookAdLibrary";
    protected override string GetConnectionString()
        => GetConnectionStringFromEnv("MONGO_CONNECTION_STRING");
}