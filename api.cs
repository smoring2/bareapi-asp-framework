using auth;
using static util.Util;

using System;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

[ApiController]
public sealed class XXXController: ControllerBase {
  private readonly IAuth _auth;
  private readonly ApiHostOptions _hostOptions;
  private readonly string _connectionString;

  public XXXController(
    IAuth auth,
    IOptions<ApiHostOptions> hostOptions,
    IConfiguration configuration
  ) {
    _auth = auth;
    _hostOptions = hostOptions.Value;
    _connectionString = configuration.GetConnectionString("dbconn");
  }

  [HttpPost]
  [Route("env")]
  public async Task<IActionResult> _env() =>
    Ok(await Task.Run(() => env()));

  [HttpPost]
  [Route("ecco")]
  public async Task<IActionResult> _echo([FromBody] JObject o) {
    if (o.Value<int>("delay") is int n && n > 0) {
      await Task.Delay(n);
    }

    return Ok(
      new {t = DateTimeOffset.UtcNow.ToLocalTime().ToString(), o}
    );
  }

  [HttpPost]
  [Route("userid")]
  public async Task<IActionResult> _session_user() {
    var userid = await _auth.SessionUser(Request);
    return Ok(new {userid});
  }

  [HttpPost]
  [Route("hailstone")]
  public IActionResult _hailstone([FromBody] JObject o) =>
    Ok((o.Value<int>("n") is int n && n > 0) ? collatz(new int[]{n}) : null);

  [HttpPost]
  [Route("now")]
  public async Task<IActionResult> _now() {
    IDictionary<string, object> database;

    using (var conn = new SqlConnection(_connectionString)) {
      await conn.OpenAsync();

      using (var cmd = conn.CreateCommand()) {
        cmd.CommandText = @"
          select
          current_timezone() as tz,
          sysdatetimeoffset() as local,
          datediff(second, '19700101', sysutcdatetime()) as unix_timestamp,
          sysutcdatetime() as unix_timestamp_str";

        database = (await cmd.ExecuteReaderAsync())
          .ToDictArray().FirstOrDefault();
      }
    }

    var ut = DateTimeOffset.UtcNow;

    return Ok(new {
      user = await _auth.SessionUser(Request),
      server = new {
        tz = TimeZoneInfo.Local.DisplayName,
        local = ut.ToLocalTime().ToString(),
        unix_timestamp = ut.ToUnixTimeSeconds(),
        unix_timestamp_str = ut.ToString()
      },
      database
    });
  }

  [HttpPost]
  [Route("login")]
  public async Task<IActionResult> _login([FromBody] ModelLogin login) {
    if (await _auth.SessionUser(Request) != null) {
      return Ok();
    }

    if (!ModelState.IsValid) {
      return StatusCode(
        403,
        ModelState
          .Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage))
          .Where(e => e != "")
      );
    }

    IDictionary<string, object> user;

    using (var conn = new SqlConnection(_connectionString)) {
      await conn.OpenAsync();

      using (var user_cmd = conn.CreateCommand()) {
        user_cmd.CommandText
          = "select id, passwd from usuario where username=@u";
        user_cmd.Parameters.Add(
            new SqlParameter("u", SqlDbType.VarChar){Value = login.username});

        user = (await user_cmd.ExecuteReaderAsync())
          .ToDictArray().FirstOrDefault();
      }
    }

    if(user is null
      || (user["passwd"]?.ToString()?.Split(':') is string[] arr
        && !FixedTimeEquals(
              deriveKey(
                password: login.passwd,
                salt: Convert.FromBase64String(arr[0])
              ),
              Convert.FromBase64String(arr[1])
            ))
      ) {
      return StatusCode(
        403,
        new {error = "incorrect user/pass"}
      );
    }

    int userid = (int) user["id"];
    await _auth.SessionClear(userid);

    Response.Headers.Append("set-cookie", "_id="
      + await _auth.SessionSet(userid)
      + ";domain=" + _hostOptions.host
      + ";path=/;httponly;samesite=lax;max-age=604800"
    );

    return Ok();
  }

  [HttpPost]
  [Route("logout")]
  public async Task<IActionResult> _logout() {
    await _auth.SessionClear(await _auth.SessionUser(Request));

    Response.Headers.Append("set-cookie", "_id="
      + ";domain=" + _hostOptions.host
      + ";path=/;httponly;samesite=lax;max-age=0"
    );

    return Ok();
  }

}

public sealed class ModelLogin {
  [Required(ErrorMessage = "username cannot be blank")]
  public string username {get; set;}

  [Required(ErrorMessage = "password cannot be blank")]
  public string passwd {get; set;}
}
