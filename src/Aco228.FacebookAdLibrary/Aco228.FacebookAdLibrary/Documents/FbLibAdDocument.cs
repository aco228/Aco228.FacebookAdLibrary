using Aco228.MongoDb.Models;
using Aco228.MongoDb.Models.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace Aco228.FacebookAdLibrary.Documents;

[BsonCollection("Ads")]
public class FbLibAdDocument : MongoDocument
{
    public string AdId { get; set; }
    public List<string> PublishPlatforms { get; set; }
    public long? StartDate { get; set; }
    public long? EndDate { get; set; }

    public List<FbLibAdDocumentVariation> Variations { get; set; } = new();
    
    public string Raw { get; set; }
}

public class FbLibAdDocumentVariation
{
    public string Caption { get; set; }
    public string CtaText { get; set; }
    public string Title { get; set; }
    public string LinkUrl { get; set; }
    public string DomainUrl { get; set; }
    public string? Body { get; set; }

    public List<string> ImageUrls { get; set; } = new();
    public List<string> VideoUrls { get; set; } = new();
}