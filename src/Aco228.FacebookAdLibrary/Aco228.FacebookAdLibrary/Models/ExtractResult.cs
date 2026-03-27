using System.Collections.Concurrent;
using Aco228.Common.Models;
using Aco228.FacebookAdLibrary.Models.Extract;

namespace Aco228.FacebookAdLibrary.Models;

public class ExtractResult
{
    public ConcurrentList<LibraryAdModel> LibraryAds { get; set; } = new();
    public ConcurrentDictionary<string, string> AdPageMap { get; set; } = new();
    public HashSet<string> InsertedIds { get; set; } = new();
    public ConcurrentDictionary<string, HashSet<string>> AdCountries { get; set; } = new();
    public ConcurrentList<string> AdErrors { get; set; } = new();

    public void Add(LibraryAdModel ad)
    {
        if (InsertedIds.Contains(ad.id))
            return;
        
        LibraryAds.Add(ad);
        InsertedIds.Add(ad.id);
    }

    public void AddErrorForAd(string adId)
    {
        AdErrors.Add(adId);
    }

    public void AddCountryForAd(string adId, string countryCode)
    {
        if (!AdCountries.ContainsKey(adId))
            AdCountries.TryAdd(adId, new());
        
        AdCountries[adId].Add(countryCode);
    }
}