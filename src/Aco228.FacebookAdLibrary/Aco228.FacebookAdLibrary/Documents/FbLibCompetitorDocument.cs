using Aco228.MongoDb.Models;
using Aco228.MongoDb.Models.Attributes;
using MongoDB.Bson;

namespace Aco228.FacebookAdLibrary.Documents;

[BsonCollection("Competitors")]
public class FbLibCompetitorDocument : MongoDocument
{
    public string Vertical { get; set; }
    public string LanguageCode { get; set; }
    public List<string> CandidateCountryCodes { get; set; }
    public string Description { get; set; }
    public string Title { get; set; }
    public List<ObjectId> AdIds { get; set; } = new();
}