using Aco228.MongoDb.Models;
using Aco228.MongoDb.Models.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace Aco228.FacebookAdLibrary.Documents;

[BsonCollection("Pages")]
public class FbLibPageDocument : MongoDocument
{
    public long PageId { get; set; }
    public long? LastRunUtc { get; set; }
    
    public string? Name { get; set; }
    public string? Byline { get; set; }
    public string? PageUrl { get; set; }
    [BsonIgnore]public string? PageProfilePictureUrl { get; set; }
}