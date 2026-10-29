using auth;
using static util.Util;

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

sealed class XXX {
  private static readonly string[] noauth = {
    "login", "hailstone", "ecco", "env", "now"
  };

  public static async Task Main(string[] args) {
    cl(String.Join("", collatz(new int[]{(new Random()).Next(1, 79)})
        .Select(e => $"\u001b[;38;5;{e % 256};1m|")) + "\u001b[0m");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<ApiHostOptions>(builder.Configuration);
    builder.Services.AddSingleton<IAuth, Auth>();

    builder.Services
      .AddControllers(o => {
        // the original pipeline used a JSON-only MediaTypeFormatter
        // (config.Formatters.Clear(); config.Formatters.Add(new JsonMediaTypeFormatter()))
        o.RespectBrowserAcceptHeader = false;

        // Web API 5 serialized a literal `null` body with 200; ASP.NET Core's
        // HttpNoContentOutputFormatter would turn it into 204 No Content.
        // Removing it preserves the original /hailstone response for n <= 0.
        o.OutputFormatters
          .RemoveType<Microsoft.AspNetCore.Mvc.Formatters.HttpNoContentOutputFormatter>();
      })
      .AddNewtonsoftJson()
      .ConfigureApiBehaviorOptions(o => {
        // [ApiController] would otherwise auto-return 400 ProblemDetails on an
        // invalid model; _login must keep returning 403 with the message list.
        o.SuppressModelStateInvalidFilter = true;
      });

    var hostOptions = builder.Configuration.Get<ApiHostOptions>() ?? new ApiHostOptions();
    var url = $"http://{hostOptions.host}:{hostOptions.port}";
    builder.WebHost.UseUrls(url);

    var app = builder.Build();

    app.Use(async (ctx, next) => {
      var authService = ctx.RequestServices.GetRequiredService<IAuth>();

      if ((await authService.SessionUser(ctx.Request.Cookies["_id"]) != null)
        || noauth.Contains(ctx.Request.Path.ToString().Substring(1))) {
        cl($"\u001b[;38;5;27;1m[{ctx.Request.Path}]\u001b[0m");
        await next();
      } else {
        cl($"\u001b[;38;5;9;1m[{ctx.Request.Path}]\u001b[0m");
        ctx.Response.StatusCode = 403;

        var responseFeature = ctx.Features.Get<IHttpResponseFeature>();
        if (responseFeature != null) {
          responseFeature.ReasonPhrase = "Not Authorized";
        }

        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{\"error\": \"verboten\"}");
      }
    });

    app.MapControllers();

    Console.WriteLine(":: listening on " + url);
    await app.RunAsync();
  }
}

public sealed class ApiHostOptions {
  public string host { get; set; } = "0.0.0.0";
  public string port { get; set; } = "8000";
}
