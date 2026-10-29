# Barebones ASP.NET Core Web API (Kestrel self-host)

A minimal, self-hosted Web API for .NET 10, built from scratch on top of a
Console application, without relying on Visual Studio or IIS. This project aims
to demonstrate how the different ASP.NET Core components fit together in the
`WebApplication`/Kestrel middleware pipeline.

## Features
+ Builds on the bare minimum .NET 10 Console application (`OutputType=Exe`).
+ Self-hosted Kestrel-based Web API with all startup logic contained in a single
file (Program.cs).
+ Implements a simple cookie based user authentication / session using raw http
headers, backed by a session table in the database.
+ Avoids the complexity and ceremony of Dapper/EFCore/ORMs in favor of raw sql
queries/ADO.NET.

### Notes
+ Built and run with the .NET SDK (10.0 or later) — no Visual Studio required:

```shell
#!/usr/bin/zsh
set -euo pipefail

dotnet build api.csproj
dotnet run --project api.csproj

```
+ Tested with `Microsoft SQL Server 2019 (RTM) - 15.0.2000.5 (X64)`.
+ Needs MultipleActiveResultSets to be enabled, so include
`MultipleActiveResultSets=true` in your connection string.
+ Listening host and port, plus the database connection string, are defined in
an `appsettings.json` file in root. See `sample_appsettings.json`. Any of these
can also be supplied via environment variables (e.g.
`ConnectionStrings__dbconn`) or command-line arguments.
+  x.sh is a simple shell script is intended to be run in a zsh shell on
MSYS2/Cygwin.
Reading it should help you understand how to use the `dotnet` CLI to build and
run this project from any cli such as Powershell or cmd.

## Endpoints

### Unauthenticated endpoints

#### hailstone
POST: `/hailstone`

Returns the Collatz (Hailstone) sequence for a given positive integer.


Parameters:
```shell
{
    "n": int (required)
}
```

#### env
POST: `/env`

List of environment variables as key-value pairs

#### now
POST: `/now`

Returns current timestamps sourced from the database (local time, Unix epoch,
formatted timestamp) and the server's UTC time.


#### ecco
POST: `/ecco`

Echoes posted content with an optional delay parameter.

Parameters:
```shell
{
    "delay": int (optional),
    ...
}

```

#### login
POST: `/login`

Example request:
```shell
curl -vs -X POST \
  --cookie ${COOKIE_FILE_PATH} \
  --cookie-jar ${COOKIE_FILE_PATH} \
  -H 'content-type: application/json' \
  -H 'accept: application/json' \
  --data-binary "$(cat <<EOL
{
  "username": string (required),
  "passwd": string (required)
}
EOL
)" \
'http://0.0.0.0:8000/login'

```

### Authenticated endpoints

#### logout
POST: `/logout`

Example request:
```shell
curl -vs -X POST \
  --cookie ${COOKIE_FILE_PATH} \
  --cookie-jar ${COOKIE_FILE_PATH} \
  -H 'content-type: application/json' \
  -H 'accept: application/json' \
'http://0.0.0.0:8000/logout'

```

#### userid
POST: `/userid`

Returns the ID of the current session user.
