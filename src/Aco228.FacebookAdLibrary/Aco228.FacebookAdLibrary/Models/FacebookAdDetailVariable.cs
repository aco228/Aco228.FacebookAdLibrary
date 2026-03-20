namespace Aco228.FacebookAdLibrary.Models;

public class FacebookAdDetailVariable
{
    public string adArchiveID { get; set; }
    public string pageID { get; set; }
    public string country { get; set; } = "ALL";
    public string sessionID { get; set; }
    public string source { get; set; } = "PAGE_TRANSPARENCY_WIDGET";
    public bool isAdNonPolitical { get; set; } = true;
    public bool isAdNotAAAEligible { get; set; } = true;
}



