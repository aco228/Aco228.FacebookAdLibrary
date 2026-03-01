// See https://aka.ms/new-console-template for more information

using Aco228.Common;
using Aco228.Common.LocalStorage;
using Aco228.FacebookAdLibrary;
using Aco228.FacebookAdLibrary.Browser;
using Aco228.FacebookAdLibrary.Services;
using Aco228.WService.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

AcoCommonConfigurable.ProjectName = "CKArbo";
AcoCommonConfigurable.DocumentFolderName = "CKArbo";
AcoCommonConfigurable.TempFolderName = "_temp";

var provider = await ServiceProviderHelper.CreateProvider(typeof(Program), (builder) =>
{
    builder.RegisterFacebookAdLibraryServices();

});


var extractor = provider.GetService<IFacebookAdExtractService>()!;
await extractor.Collect(new()
{
    PageIds = new()
    {
        788660804331628
    },
    Domains = new()
    {
        "SMARTDEALSEARCH.COM",
    },
});

for (;;)
{
    
}

Console.WriteLine("Hello, World!");