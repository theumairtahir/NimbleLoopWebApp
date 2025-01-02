using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NimbleLoopWebApp.Domain;

public abstract class BaseEntity
{
	[BsonId]
	public ObjectId Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ModifiedAt { get; set; }
	public string CreatedBy { get; set; } = null!;
	public string? ModifiedBy { get; set; }
	[BsonIgnore]
	public DateTime LastModified => ModifiedAt ?? CreatedAt;
}
