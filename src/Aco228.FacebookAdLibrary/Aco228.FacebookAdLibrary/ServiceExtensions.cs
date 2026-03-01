using Aco228.Common.Extensions;
using Aco228.Common.Infrastructure;
using Aco228.FacebookAdLibrary.Core;
using Aco228.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Aco228.FacebookAdLibrary;

public static class ServiceExtensions
{
    public static void RegisterFacebookAdLibraryServices(this IServiceCollection services)
        => typeof(ServiceExtensions).RegisterIfNot(() =>
        {
            services.RegisterRepositoriesFromAssembly<IFacebookAdLibraryDbContext>();
            services.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);
        });
}