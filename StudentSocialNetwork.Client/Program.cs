using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using StudentSocialNetwork.Client.Configuration;
using StudentSocialNetwork.Client.Services;
using StudentSocialNetwork.Client.Services.Social;

const string externalOAuthScheme = "ExternalOAuth";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<BackendApiOptions>(
    builder.Configuration.GetSection(BackendApiOptions.SectionName));

builder.Services.AddHttpClient<IBackendApiProxy, BackendApiProxy>((sp, client) =>
{
    var options = sp.GetRequiredService<IConfiguration>()
        .GetSection(BackendApiOptions.SectionName)
        .Get<BackendApiOptions>() ?? new BackendApiOptions();

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Keep redirects in the proxy layer so Authorization can be preserved safely.
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient(nameof(BackendApiOptions), (sp, client) =>
{
    var options = sp.GetRequiredService<IConfiguration>()
        .GetSection(BackendApiOptions.SectionName)
        .Get<BackendApiOptions>() ?? new BackendApiOptions();

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ssn.client.auth";
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddCookie(externalOAuthScheme, options =>
    {
        options.Cookie.Name = "ssn.client.external";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = externalOAuthScheme;
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = context =>
            {
                if (context.User.TryGetProperty("picture", out var pictureElement))
                {
                    var avatarUrl = pictureElement.GetString();
                    if (!string.IsNullOrWhiteSpace(avatarUrl))
                    {
                        context.Identity?.AddClaim(new Claim("urn:google:picture", avatarUrl));
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
