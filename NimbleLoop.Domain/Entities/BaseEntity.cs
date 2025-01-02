namespace NimbleLoop.Domain.Entities;

public abstract class BaseEntity
{
	public string Id { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime? ModifiedAt { get; set; }
	public string CreatedBy { get; set; } = null!;
	public string? ModifiedBy { get; set; }
	public DateTime LastModified => ModifiedAt ?? CreatedAt;
}
