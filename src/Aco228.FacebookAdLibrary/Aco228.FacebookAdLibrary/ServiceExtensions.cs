using Aco228.Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Aco228.FacebookAdLibrary;

public static class ServiceExtensions
{
    public static void RegisterFacebookAdLibraryServices(this IServiceCollection services)
        => typeof(ServiceExtensions).RegisterIfNot(() =>
        {

        });
}