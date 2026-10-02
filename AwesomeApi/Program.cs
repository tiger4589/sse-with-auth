using AwesomeApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using System.Runtime.CompilerServices;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
const string SameOriginCookieScheme = "SameOriginCookie";
const string CrossOriginCookieScheme = "CrossOriginCookie";

builder.AddServiceDefaults();

builder.Services.AddSingleton<ShortLivedTokenStore>();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalBlazor", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                {
                    return false;
                }

                return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                       uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "demo-realm",
        options =>
        {
            options.Audience = "demo-api";

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        })
    .AddCookie(SameOriginCookieScheme, options =>
    {
        options.Cookie.Name = "sse_same_origin_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie(CrossOriginCookieScheme, options =>
    {
        options.Cookie.Name = "sse_cross_origin_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SameOriginCookiePolicy", policy =>
    {
        policy.AddAuthenticationSchemes(SameOriginCookieScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("CrossOriginCookiePolicy", policy =>
    {
        policy.AddAuthenticationSchemes(CrossOriginCookieScheme);
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowLocalBlazor");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/events", (CancellationToken cancellationToken) =>
    {
        int count = 0;

        async IAsyncEnumerable<int> StreamEvents([EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                yield return count++;

                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    yield break;
                }
            }
        }

        return Results.ServerSentEvents(StreamEvents(cancellationToken));
    }).WithName("GetEvents")
    .RequireAuthorization();

app.MapGet("/request-slt", (HttpContext context, ShortLivedTokenStore store) =>
    {
        string? userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest("Empty Username");
        }

        return Results.Ok(store.GetToken(userId));
    })
    .WithName("RequestShortLivedToken")
    .RequireAuthorization();

app.MapGet("/events-slt", (string shortLivedToken, ShortLivedTokenStore store, CancellationToken cancellationToken) =>
{
    if (!store.IsTokenValid(shortLivedToken))
    {
        return Results.Unauthorized();
    }

    int count = 0;

    async IAsyncEnumerable<int> StreamEvents([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return count++;

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                yield break;
            }
        }
    }

    return Results.ServerSentEvents(StreamEvents(cancellationToken));
}).WithName("GetEventsWithShortLivedToken");

app.MapPost("/cookie/same/login", async (HttpContext context) =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "cookie-same-origin-demo"),
        new Claim(ClaimTypes.Name, "Cookie Same-Origin Demo")
    };

    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SameOriginCookieScheme));
    await context.SignInAsync(SameOriginCookieScheme, principal);
    return Results.NoContent();
}).WithName("LoginSameOriginCookieDemo");

app.MapPost("/cookie/cross/login", async (HttpContext context) =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "cookie-cross-origin-demo"),
        new Claim(ClaimTypes.Name, "Cookie Cross-Origin Demo")
    };

    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CrossOriginCookieScheme));
    await context.SignInAsync(CrossOriginCookieScheme, principal);
    return Results.NoContent();
}).WithName("LoginCrossOriginCookieDemo");

app.MapPost("/cookie/same/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(SameOriginCookieScheme);
    return Results.NoContent();
}).WithName("LogoutSameOriginCookieDemo");

app.MapPost("/cookie/cross/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CrossOriginCookieScheme);
    return Results.NoContent();
}).WithName("LogoutCrossOriginCookieDemo");

app.MapGet("/events-cookie-same", (CancellationToken cancellationToken) =>
{
    int count = 0;

    async IAsyncEnumerable<int> StreamEvents([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return count++;

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                yield break;
            }
        }
    }

    return Results.ServerSentEvents(StreamEvents(cancellationToken));
})
.WithName("GetEventsWithSameOriginCookie")
.RequireAuthorization("SameOriginCookiePolicy");

app.MapGet("/events-cookie-cross", (CancellationToken cancellationToken) =>
{
    int count = 0;

    async IAsyncEnumerable<int> StreamEvents([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return count++;

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                yield break;
            }
        }
    }

    return Results.ServerSentEvents(StreamEvents(cancellationToken));
})
.WithName("GetEventsWithCrossOriginCookie")
.RequireAuthorization("CrossOriginCookiePolicy");

app.Run();
