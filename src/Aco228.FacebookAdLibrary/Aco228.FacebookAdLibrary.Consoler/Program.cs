// See https://aka.ms/new-console-template for more information

using Aco228.Common;
using Aco228.Common.LocalStorage;
using Aco228.FacebookAdLibrary;
using Aco228.FacebookAdLibrary.Browser;
using Microsoft.Extensions.DependencyInjection;

AcoCommonConfigurable.ProjectName = "CKArbo";
AcoCommonConfigurable.DocumentFolderName = "CKArbo";
AcoCommonConfigurable.TempFolderName = "_temp";

var provider = await ServiceProviderHelper.CreateProvider(typeof(Program), (builder) =>
{
    builder.RegisterFacebookAdLibraryServices();

});

var userDirLocation = StorageManager.Instance.GetFolder("FbAdLibrary").GetFolder("user-dir");
var browser = provider.GetService<IFacebookAdLibraryBrowser>()!;
await browser.Launch(userDirLocation.GetDirectoryInfo().FullName, false);
await browser.Page.GotoAsync("https://www.facebook.com/ads/library/?active_status=active&ad_type=all&country=ALL&is_targeted_country=false&media_type=all&q=%22Job%20Seekers%20Hub%22&search_type=keyword_exact_phrase&sort_data[direction]=desc&sort_data[mode]=total_impressions&source=page-transparency-widget");

for (;;)
{
    
}

Console.WriteLine("Hello, World!");