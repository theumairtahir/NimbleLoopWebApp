using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using NimbleLoop.Domain.Entities;
using NimbleLoopWebApp.Client.ViewModels;
using NimbleLoopWebApp.Components;
using NimbleLoopWebApp.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents( )
	.AddInteractiveWebAssemblyComponents( );
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
builder.Services.AddAuthorization( );

builder.Services.AddCascadingAuthenticationState( );

builder.Services.AddScoped(sp =>
{
	var options = new DbContextOptionsBuilder<NimbleLoopDbContext>( ).UseMongoDB(builder.Configuration["DbConfig:ConnectionString"]!, builder.Configuration["DbConfig:DatabaseName"]!).Options;
	return new NimbleLoopDbContext(options);
});

var app = builder.Build( );

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment( ))
{
	app.UseWebAssemblyDebugging( );
	app.UseDeveloperExceptionPage( );
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts( );
}

app.UseHttpsRedirection( );


app.UseAntiforgery( );
app.MapStaticAssets( );
app.UseAuthorization( );

app.MapPost("api/contact", async (NimbleLoopDbContext dbContext, [FromBody] HomeContactViewModel model) =>
{
	var prospect = await dbContext.Prospects.FirstOrDefaultAsync(p => p.Email == model.Email.ToLower( ).Trim( ));
	prospect ??= new Prospect
	{
		CompanyName = model.CompanyName?.Trim( ),
		Email = model.Email.ToLower( ).Trim( ),
		Name = model.Name.Trim( ),
	};
	prospect.Queries.Add(new Query
	{
		Budget = model.Budget,
		Message = model.Message,
		ServiceInterestedIn = model.ServiceInterestedIn,
		Type = QueryType.ServiceInformation,
		Timestamp = DateTime.UtcNow,
	});
	var isNew = string.IsNullOrEmpty(prospect.Id);
	if (isNew)
		await dbContext.Prospects.AddAsync(prospect);
	else
		dbContext.Prospects.Update(prospect);
	await dbContext.SaveChangesAsync( );
	return isNew ? Results.Created( ) : Results.Ok( );
});

app.MapPost("api/articles", async (NimbleLoopDbContext dbContext, [FromBody] Article article, ClaimsPrincipal User) =>
{
	var isNew = string.IsNullOrEmpty(article.Id);

	if (isNew)
		await dbContext.Articles.AddAsync(article);
	else
		dbContext.Articles.Update(article);
	await dbContext.SaveChangesAsync(User);
	return isNew ? Results.Created( ) : Results.Ok( );
}).RequireAuthorization( );

app.MapGet("api/editors", async (NimbleLoopDbContext dbContext) => Results.Ok(await dbContext.Editors.ToListAsync( )))
	.RequireAuthorization( );

app.MapRazorComponents<App>( )
	.AddInteractiveWebAssemblyRenderMode( )
	.AddAdditionalAssemblies(typeof(NimbleLoopWebApp.Client._Imports).Assembly);

await app.RunAsync( );
