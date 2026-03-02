using Aco228.MongoDb.Models;
using Aco228.MongoDb.Models.Attributes;

namespace Aco228.FacebookAdLibrary.Documents;

[BsonCollection("Domains")]
public class FbLibDomainDocument : MongoDocument
{
    public string Domain { get; set; }
    public long? LastRunUtc { get; set; }
}