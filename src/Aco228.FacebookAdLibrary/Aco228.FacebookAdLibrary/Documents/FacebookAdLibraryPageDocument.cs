using Aco228.MongoDb.Models;
using Aco228.MongoDb.Models.Attributes;

namespace Aco228.FacebookAdLibrary.Documents;

[BsonCollection("Pages")]
public class FacebookAdLibraryPageDocument : MongoDocument
{
    public long PageId { get; set; }
    public long? LastRunUtc { get; set; }
}