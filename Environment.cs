using Moe.Services;

namespace Moe;

public class Environment
{
  public const string ProductionEnvFile = "production.env";
  public const string DevelopmentEnvFile = "development.env";

  public string Token { get; set; } = default!;
  public ulong? ServerID { get; set; } = default!;
  public List<ulong> Owners { get; set; } = default!;
  public int StatusPort { get; set; } = default!;

  public static async Task<Environment> Load()
  {
    string envFile = default!;
    if (ConfigService.Options.IsDevelopment)
    {
      envFile = DevelopmentEnvFile;
    }
    else if (ConfigService.Options.IsProduction)
    {
      envFile = ProductionEnvFile;
    }

    await LogService.LogToFileAndConsole($"Using {envFile} for environment variables");
    DotNetEnv.Env.Load(envFile);

    return new Environment()
    {
      Token = GetEnv("TOKEN"),
      ServerID = ConfigService.Options.IsProduction ? null : ulong.Parse(GetEnv("SERVERID")),
      Owners = GetEnv("OWNERS")
        .Split(',')
        .Select(x => ulong.Parse(x))
        .ToList(),
      StatusPort = int.Parse(GetEnv("STATUS_PORT")),
    };
  }

  private static string GetEnv(string name)
  {
    return System.Environment.GetEnvironmentVariable(name) ??
      throw new InvalidOperationException($"Environment variable {name} is not set");
  }
}
