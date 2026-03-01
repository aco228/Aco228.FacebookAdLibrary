using Aco228.Common.Models;
using Aco228.FacebookAdLibrary.Models.Extract;

namespace Aco228.FacebookAdLibrary.Models;

public class ExtractResult
{
    public ConcurrentList<PageModel> Pages { get; set; } = new();
    public ConcurrentList<LibraryAdModel> LibraryAds { get; set; } = new();
}