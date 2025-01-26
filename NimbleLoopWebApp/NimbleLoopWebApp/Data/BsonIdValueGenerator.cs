using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using MongoDB.Bson;

namespace NimbleLoopWebApp.Data;

public class BsonIdValueGenerator : ValueGenerator
{
	public override bool GeneratesTemporaryValues => false;
	protected override object NextValue(EntityEntry entry) => ObjectId.GenerateNewId( ).ToString( );
}
