using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using NimbleLoopWebApp.Components;
using NimbleLoopWebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents( )
	.AddInteractiveWebAssemblyComponents( );
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));

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
app.MapRazorComponents<App>( )
	.AddInteractiveWebAssemblyRenderMode( )
	.AddAdditionalAssemblies(typeof(NimbleLoopWebApp.Client._Imports).Assembly);

await app.RunAsync( );
