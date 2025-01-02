namespace NimbleLoopWebApp.Domain;

public class Prospect : BaseEntity
{
	public string Name { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string? CompanyName { get; set; } = null!;
	public List<Query> Queries { get; set; } = [ ];
}

public class Query
{
	public QueryType Type { get; set; }
	public string? ServiceInterestedIn { get; set; }
	public int? Budget { get; set; }
	public string? Message { get; set; }
	public DateTime Timestamp { get; set; }
}


public enum QueryType
{
	ServiceInformation = 1
}
