using Discord;
using Discord.WebSocket;
using TNTBot.Models;

namespace TNTBot.Services
{
  public class LogService
  {
    public static LogService Instance { get; set; } = default!;

    private readonly SettingsService settingsService;

    public LogService(SettingsService settingsService)
    {
      this.settingsService = settingsService;
    }

    public async Task LogToDiscord(SocketGuild guild, string? text = null, Embed? embed = null)
    {
      var logChannel = await settingsService.GetLogChannel(guild);
      if (logChannel is null)
      {
        return;
      }

      await logChannel.SendMessageAsync(text: text, embed: embed);
    }

    public async Task LogToFileAndConsole(string message, SocketGuild? guild, LogLevel level = LogLevel.Info)
    {
      var formattedMessage = FormatMessage(message, guild, level);
      Console.WriteLine(formattedMessage);
      await WriteToLogFile(formattedMessage);
    }

    private string FormatMessage(string message, SocketGuild? guild, LogLevel level)
    {
      var timePart = $"[{DateTime.Now}] ";
      var levelPart = $"[{level.ToString().ToUpper()}] ";
      var guildPart = guild is null ? string.Empty : $"[{guild.Name}#{guild.Id}] ";
      return timePart + levelPart + guildPart + message;
    }

    private async Task WriteToLogFile(string message)
    {
      var logsDirPath = "logs";
      Directory.CreateDirectory(logsDirPath);

      var currentLogPath = Path.Combine(logsDirPath, $"{DateTime.Now:yyyy-MM-dd}.log");
      await File.AppendAllTextAsync(currentLogPath, message + Environment.NewLine);
    }
  }
}