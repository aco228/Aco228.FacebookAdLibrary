using Aco228.Common.Models;
using Aco228.FacebookAdLibrary.Models.Extract;

namespace Aco228.FacebookAdLibrary.Models;

public class ExtractResult
{
    public ConcurrentList<LibraryAdModel> LibraryAds { get; set; } = new();
    public HashSet<string> InsertedIds { get; set; } = new();

    public void Add(LibraryAdModel ad)
    {
        if (InsertedIds.Contains(ad.id))
            return;
        
        LibraryAds.Add(ad);
        InsertedIds.Add(ad.id);
    }
}