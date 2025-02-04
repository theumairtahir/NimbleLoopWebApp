using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using MongoDB.Driver;
using NimbleLoop.Domain.Entities;
using NimbleLoopWebApp.Client.Extensions;
using NimbleLoopWebApp.Client.ViewModels;
using NimbleLoopWebApp.Components;
using NimbleLoopWebApp.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using Constants = NimbleLoopWebApp.Constants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents( )
	.AddInteractiveWebAssemblyComponents( );
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
builder.Services.AddAuthorization( );

builder.Services.AddCascadingAuthenticationState( );

builder.Services.AddTransient(sp =>
{
	var client = new MongoClient(builder.Configuration["DbConfig:ConnectionString"]!);
	return client.GetDatabase(builder.Configuration["DbConfig:DatabaseName"]!);
});

builder.Services.AddMongoDB<NimbleLoopDbContext>(new MongoClient(builder.Configuration["DbConfig:ConnectionString"]!), builder.Configuration["DbConfig:DatabaseName"]!);

builder.Services.AddHttpClient(Constants.FUNCTIONS_CLIENT, x =>
{
	x.BaseAddress = new Uri(builder.Configuration["Functions:BaseUrl"]!);
	x.DefaultRequestHeaders.Add("x-functions-key", builder.Configuration["Functions:FunctionKey"]);
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

app.MapPost("api/articles", async (NimbleLoopDbContext dbContext, [FromBody] Article article, ClaimsPrincipal user) =>
{
	var editor = article.Editor;
	dbContext.Entry(article.Editor).State = EntityState.Detached;
	article.Editor = null!;
	var trackedEditor = dbContext.ChangeTracker.Entries<Editor>( )
		.FirstOrDefault(e => e.Entity.Id == editor.Id)?.Entity;

	if (trackedEditor is not null)
	{
		dbContext.Entry(trackedEditor).CurrentValues.SetValues(editor);
	}
	else
	{
		var existingEditor = await dbContext.Editors
			.AsNoTracking( )
			.FirstOrDefaultAsync(e => e.Id == editor.Id);

		if (existingEditor is not null)
		{
			dbContext.Editors.Attach(editor);
			dbContext.Entry(editor).State = EntityState.Modified;
		}
		else
		{
			await dbContext.Editors.AddAsync(editor);
		}
	}
	await dbContext.SaveChangesAsync(user);
	article.EditorId = editor.Id;

	var isNewArticle = string.IsNullOrEmpty(article.Id);
	if (isNewArticle)
	{
		var isDuplicateKey = await dbContext.Articles.AnyAsync(a => a.Key == article.Key);
		if (isDuplicateKey)
			return Results.BadRequest("Key must be unique");

		await dbContext.Articles.AddAsync(article);
	}
	else
	{
		dbContext.Articles.Update(article);
	}

	await dbContext.SaveChangesAsync(user);
	return isNewArticle ? Results.Created( ) : Results.Ok( );
}).RequireAuthorization( );


app.MapGet("api/articles/{id}", async (NimbleLoopDbContext dbContext, [FromRoute] string id) =>
{
	var article = await dbContext.Articles.FirstOrDefaultAsync(a => a.Id == id);
	if (article is not null)
	{
		article.Editor = ( await dbContext.Editors.FindAsync(article.EditorId) )!;
		return Results.Ok(article);
	}
	else
	{
		return Results.NotFound( );
	}
}).RequireAuthorization( );

app.MapGet("api/validate-unique-key/{key}", async (NimbleLoopDbContext dbContext, string key, [FromQuery] string? articleId) =>
{
	var slug = key.ToSlug( );
	if (string.IsNullOrEmpty(articleId))
		return Results.Ok(!await dbContext.Articles.AnyAsync(a => a.Key == slug));
	return Results.Ok(!await dbContext.Articles.AnyAsync(a => a.Key == slug && a.Id != articleId));

}).RequireAuthorization( );

app.MapGet("api/editors", async (NimbleLoopDbContext dbContext) => Results.Ok(await dbContext.Editors.ToListAsync( )))
	.RequireAuthorization( );

app.MapGet("api/list-gallery", async (IHttpClientFactory httpFactory, IConfiguration configuration) =>
{
	var client = httpFactory.CreateClient(Constants.FUNCTIONS_CLIENT);
	var fileNames = await client.GetFromJsonAsync<List<string>>($"/api/ReadImageNames?path={Constants.GALLERY_PATH}/1920x1080") ?? [ ];
	var images = new List<string>( );
	var baseUrl = configuration["Functions:BaseUrl"]!;
	foreach (var image in fileNames)
	{
		var imageName = image.Split('/').Last( );
		var uri = new UriBuilder(baseUrl)
		{
			Path = $"api/ReadImage/{imageName}",
			Query = "path=" + Constants.GALLERY_PATH
		};
		images.Add(uri.Uri.ToString( ));
	}
	return Results.Ok(images);
}).RequireAuthorization( );

app.MapPost("api/save-gallery-image/{imageName}", async (IHttpClientFactory httpFactory, [FromForm(Name = "image")] IFormFile file, [FromRoute] string imageName) =>
{
	var client = httpFactory.CreateClient(Constants.FUNCTIONS_CLIENT);
	using var content = new MultipartFormDataContent( );

	using var fileStream = file.OpenReadStream( );
	var streamContent = new StreamContent(fileStream);
	streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
	content.Add(streamContent, "image", file.FileName);

	var response = await client.PostAsync($"/api/SaveImage/{imageName}?path={Constants.GALLERY_PATH}", content);
	return response.IsSuccessStatusCode ? Results.Ok( ) : Results.StatusCode((int)response.StatusCode);
}).RequireAuthorization( ).DisableAntiforgery( );

app.MapGet("api/categories", async (NimbleLoopDbContext dbContext) =>
{
	var tags = await dbContext.Articles.Select(a => a.Tags).ToListAsync( );
	return Results.Ok(tags.SelectMany(t => t).Distinct( ));
});

app.MapGet("api/comments/{id}", async (NimbleLoopDbContext dbContext, string id) => Results.Ok(await dbContext.Articles.Where(x => x.Id == id).Select(x => x.Comments).FirstAsync( )));

app.MapStaticAssets( );

app.MapRazorComponents<App>( )
	.AddInteractiveWebAssemblyRenderMode( )
	.AddAdditionalAssemblies(typeof(NimbleLoopWebApp.Client._Imports).Assembly);

await app.RunAsync( );
