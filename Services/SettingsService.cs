using Discord.WebSocket;
using Moe.Models;

namespace Moe.Services;

public class SettingsService
{
  public SettingsService()
  {
    CreateSettingsTable().Wait();
    CreateModranksTable().Wait();
    CreateAutorolesTable().Wait();

    DiscordService.Discord.UserLeft += OnUserLeft;
  }

  public async Task<SocketTextChannel?> GetPinChannel(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'pin_channel'";
    var channelId = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
    if (channelId == 0)
    {
      return null;
    }
    return guild.GetTextChannel(channelId);
  }

  public async Task SetPinChannel(SocketTextChannel channel)
  {
    var guild = channel.Guild;
    await LogService.LogToFileAndConsole(
      $"Setting pin channel to {channel}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'pin_channel'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'pin_channel', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, channel.Id);
  }

  public async Task<bool> HasPinChannel(SocketGuild guild)
  {
    return await GetPinChannel(guild) != null;
  }

  public async Task<SocketTextChannel?> GetLogChannel(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'log_channel'";
    var channelId = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
    if (channelId == 0)
    {
      return null;
    }
    return guild.GetTextChannel(channelId);
  }

  public async Task SetLogChannel(SocketTextChannel channel)
  {
    var guild = channel.Guild;
    await LogService.LogToFileAndConsole(
      $"Setting log channel to {channel}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'log_channel'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'log_channel', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, channel.Id);
  }

  public async Task<bool> HasLogChannel(SocketGuild guild)
  {
    return await GetLogChannel(guild) != null;
  }

  public async Task<string> GetCommandPrefix(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'command_prefix'";
    var prefix = await DatabaseService.QueryFirst<string>(sql, guild.Id);
    if (prefix is null)
    {
      return "!";
    }
    return prefix;
  }

  public async Task SetCommandPrefix(SocketGuild guild, string prefix)
  {
    await LogService.LogToFileAndConsole(
      $"Setting command prefix to {prefix}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'command_prefix'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'command_prefix', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, prefix);
  }

  public async Task<List<(SocketRole Role, ModrankLevel Level)>> GetModranks(SocketGuild guild)
  {
    var sql = "SELECT role_id, level FROM modranks WHERE guild_id = $0";
    var result = await DatabaseService.Query<ulong, int>(sql, guild.Id);
    await RemoveBrokenModranks(guild, result);
    return result.ConvertAll(x => (guild.GetRole(x.Item1), (ModrankLevel)x.Item2));
  }

  public bool IsAuthorized(SocketGuildUser user, ModrankLevel requiredLevel, out string? error)
  {
    error = null;
    if (requiredLevel > GetModrankLevel(user).Result)
    {
      error = $"This action requires at least {requiredLevel} modrank";
      return false;
    }

    return true;
  }

  public bool IsAuthorizedDMSilent(SocketUser user, ModrankLevel requiredLevel)
  {
    var guilds = DiscordService.Discord.Guilds;
    var guildUsers = guilds
      .Select(x => x.GetUser(user.Id))
      .Where(x => x is not null);
    var bestModrank = guildUsers
      .Select(x => GetModrankLevel(x).Result)
      .Max();

    return requiredLevel <= bestModrank;
  }

  public async Task SetModrank(SocketRole role, ModrankLevel level)
  {
    await RemoveModrank(role);
    if (level > 0)
    {
      await AddModrank(role, level);
    }
  }

  private async Task RemoveBrokenModranks(SocketGuild guild, List<(ulong RoleID, int Level)> modranks)
  {
    var sql = "DELETE FROM modranks WHERE guild_id = $0 AND role_id = $1";
    foreach (var modrank in modranks)
    {
      if (guild.GetRole(modrank.RoleID) is null)
      {
        await DatabaseService.NonQuery(sql, guild.Id, modrank.RoleID);
        modranks.Remove(modrank);
      }
    }
  }

  private async Task<ModrankLevel> GetModrankLevel(SocketGuildUser user)
  {
    if (ConfigService.Environment.Owners.Contains(user.Id))
    {
      return ModrankLevel.Owner;
    }

    var modranks = await GetModranksForUser(user);
    if (user.GuildPermissions.Administrator)
    {
      return ModrankLevel.Administrator;
    }
    if (modranks.Any(x => x.Level == ModrankLevel.Administrator))
    {
      return ModrankLevel.Administrator;
    }

    if (modranks.Any(x => x.Level == ModrankLevel.Moderator))
    {
      return ModrankLevel.Moderator;
    }

    return ModrankLevel.None;
  }

  private async Task<List<(SocketRole Role, ModrankLevel Level)>> GetModranksForUser(SocketGuildUser user)
  {
    var modranks = await GetModranks(user.Guild);
    return modranks.Where(x => user.Roles.Contains(x.Role)).ToList();
  }

  private async Task AddModrank(SocketRole role, ModrankLevel level)
  {
    await LogService.LogToFileAndConsole(
      $"Adding modrank for role {role} with level {level}", role.Guild);

    var sql = "INSERT INTO modranks(guild_id, role_id, level) VALUES($0, $1, $2)";
    await DatabaseService.NonQuery(sql, role.Guild.Id, role.Id, level);
  }

  private async Task RemoveModrank(SocketRole role)
  {
    await LogService.LogToFileAndConsole(
      $"Removing modrank for role {role}", role.Guild);

    var sql = "DELETE FROM modranks WHERE guild_id = $0 AND role_id = $1";
    await DatabaseService.NonQuery(sql, role.Guild.Id, role.Id);
  }

  public async Task<SocketRole?> GetNoXPRole(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'no_xp_role'";
    var roleId = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
    if (roleId == 0)
    {
      return null;
    }
    return guild.GetRole(roleId);
  }

  public async Task SetNoXPRole(SocketRole role)
  {
    var guild = role.Guild;
    await LogService.LogToFileAndConsole(
      $"Setting no xp role to {role}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'no_xp_role'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'no_xp_role', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, role.Id);
  }

  public async Task<bool> HasNoXPRole(SocketGuild guild)
  {
    return await GetNoXPRole(guild) != null;
  }

  public async Task SetLeaveMessage(SocketTextChannel channel, string message)
  {
    var guild = channel.Guild;
    await LogService.LogToFileAndConsole(
      $"Setting leave message to {message} for channel {channel}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'leave_message_channel'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'leave_message_text'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'leave_message_channel', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, channel.Id);
    insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'leave_message_text', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, message);
  }

  public async Task<(SocketTextChannel Channel, string Message)?> GetLeaveMessage(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'leave_message_channel'";
    var channel = await DatabaseService.QueryFirst<ulong>(sql, guild.Id);
    sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'leave_message_text'";
    var message = await DatabaseService.QueryFirst<string>(sql, guild.Id);
    if (channel == 0 || message is null)
    {
      return null;
    }

    return (guild.GetTextChannel(channel), message);
  }

  private async Task OnUserLeft(SocketGuild guild, SocketUser user)
  {
    var leaveMessage = await GetLeaveMessage(guild);
    if (leaveMessage is null)
    {
      return;
    }

    var message = leaveMessage.Value.Message.Replace("$user", user.Mention);
    await leaveMessage.Value.Channel.SendMessageAsync(message);
  }

  public async Task SetTimeZone(SocketGuild guild, TimeZoneTime timeZone)
  {
    var timeZoneString = timeZone.TimeZoneString;

    await LogService.LogToFileAndConsole(
      $"Setting time zone to {timeZoneString} for guild {guild}", guild);

    var deleteSql = "DELETE FROM settings WHERE guild_id = $0 AND name = 'timezone'";
    await DatabaseService.NonQuery(deleteSql, guild.Id);
    var insertSql = "INSERT INTO settings(guild_id, name, value) VALUES($0, 'timezone', $1)";
    await DatabaseService.NonQuery(insertSql, guild.Id, timeZoneString);
  }

  public async Task<TimeZoneTime?> GetTimeZone(SocketGuild guild)
  {
    var sql = "SELECT value FROM settings WHERE guild_id = $0 AND name = 'timezone'";
    var timeZoneString = await DatabaseService.QueryFirst<string>(sql, guild.Id);
    if (timeZoneString is null)
    {
      return null;
    }

    return TimeZoneTime.Parse(timeZoneString);
  }

  public async Task<bool> HasAutorole(SocketGuild guild, SocketRole role)
  {
    var sql = "SELECT COUNT(*) FROM autoroles WHERE guild_id = $0 AND role_id = $1";
    var count = await DatabaseService.QueryFirst<int>(sql, guild.Id, role.Id);
    return count > 0;
  }

  public async Task<List<SocketRole>> GetAutoroles(SocketGuild guild)
  {
    var sql = "SELECT role_id FROM autoroles WHERE guild_id = $0";
    var roleIds = await DatabaseService.Query<ulong>(sql, guild.Id);
    var roles = new List<SocketRole>();
    foreach (var roleId in roleIds)
    {
      var role = guild.GetRole(roleId);
      if (role is null)
      {
        await RemoveAutorole(guild, roleId: roleId);
      }
      else
      {
        roles.Add(role);
      }
    }

    return roles;
  }

  public async Task AddAutorole(SocketGuild guild, SocketRole role)
  {
    await LogService.LogToFileAndConsole($"Adding autorole {role}", guild);

    var sql = "INSERT INTO autoroles(guild_id, role_id) VALUES($0, $1)";
    await DatabaseService.NonQuery(sql, guild.Id, role.Id);
  }

  public async Task RemoveAutorole(SocketGuild guild, SocketRole? role = null, ulong? roleId = null)
  {
    await LogService.LogToFileAndConsole($"Removing autorole {role?.ToString() ?? roleId.ToString()}", guild);

    var sql = "DELETE FROM autoroles WHERE guild_id = $0 AND role_id = $1";
    await DatabaseService.NonQuery(sql, guild.Id, role?.Id ?? roleId);
  }

  private async Task CreateSettingsTable()
  {
    var sql = @"
      CREATE TABLE IF NOT EXISTS settings(
      guild_id INTEGER NOT NULL,
      name TEXT NOT NULL,
      value TEXT NOT NULL
    )";
    await DatabaseService.NonQuery(sql);
  }

  private async Task CreateModranksTable()
  {
    var sql = @"
      CREATE TABLE IF NOT EXISTS modranks(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        guild_id INTEGER NOT NULL,
        role_id INTEGER NOT NULL,
        level INTEGER NOT NULL
      )";
    await DatabaseService.NonQuery(sql);
  }

  private async Task CreateAutorolesTable()
  {
    var sql = @"
      CREATE TABLE IF NOT EXISTS autoroles(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        guild_id INTEGER NOT NULL,
        role_id INTEGER NOT NULL
      )";
    await DatabaseService.NonQuery(sql);
  }
}
