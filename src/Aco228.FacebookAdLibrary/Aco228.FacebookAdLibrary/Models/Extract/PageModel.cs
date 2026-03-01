namespace Aco228.FacebookAdLibrary.Models.Extract;

public class PageModel
{
    public string id { get; set; }
    public string name { get; set; }
    public PageModelProfilePhoto profile_photo { get; set; }
}

public class PageModelProfilePhoto
{
    public string src { get; set; }
    public string id { get; set; }
}