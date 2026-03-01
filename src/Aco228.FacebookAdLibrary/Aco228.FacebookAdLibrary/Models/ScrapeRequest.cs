namespace Aco228.FacebookAdLibrary.Models;

public class ScrapeRequest
{
    public List<long> PageIds { get; set; } = new();
    public List<string> Domains { get; set; } = new();

    public bool Any()
    {
        return PageIds.Any() || Domains.Any();
    }
}