namespace Aco228.FacebookAdLibrary.Models;

public class FacebookSearchVariable
{
    public string? activeStatus { get; set; }
    public string? adType { get; set; }
    public List<object>? bylines { get; set; }
    public object? collationToken { get; set; }
    public List<object>? contentLanguages { get; set; }
    public List<string>? countries { get; set; }
    public string? cursor { get; set; }
    public object? excludedIDs { get; set; }
    public int? first { get; set; }
    public bool? isTargetedCountry { get; set; }
    public object? location { get; set; }
    public string? mediaType { get; set; }
    public object? multiCountryFilterMode { get; set; }
    public List<object>? pageIDs { get; set; }
    public object? potentialReachInput { get; set; }
    public List<object>? publisherPlatforms { get; set; }
    public string? queryString { get; set; }
    public object? regions { get; set; }
    public string? searchType { get; set; }
    public string? sessionID { get; set; }
    public SortdataDTO? sortData { get; set; }
    public string? source { get; set; }
    public object? startDate { get; set; }
    public string? v { get; set; }
    public string? viewAllPageID { get; set; }
}

public class SortdataDTO
{
    public string direction { get; set; }
    public string mode { get; set; }
}
