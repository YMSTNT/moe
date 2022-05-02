using Discord;
using Discord.WebSocket;
using TNTBot.Models;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class SettingsCommand : SlashCommandBase
  {
    private readonly SettingsService service;

    public SettingsCommand(SettingsService service) : base("settings")
    {
      Description = "View or change settings";
      Options = new SlashCommandOptionBuilder()
        .AddOption(new SlashCommandOptionBuilder()
          .WithName("list")
          .WithDescription("List all settings")
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("pinchannel")
          .WithDescription("Set a channel where the bot will pin messages")
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("logchannel")
          .WithDescription("Set a channel where the bot will log messages")
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("commandprefix")
          .WithDescription("Set the command prefix for custom commands")
          .AddOption("prefix", ApplicationCommandOptionType.String, "The prefix", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("modrank")
          .WithDescription("Change which discord roles are considered mods and admins by this bot. Server admins are bot admins")
          .AddOption("role", ApplicationCommandOptionType.Role, "The role", isRequired: true)
          .AddOption(new SlashCommandOptionBuilder()
            .WithName("level")
            .WithDescription("Level of the modrank")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.Integer)
            .AddChoice(nameof(ModrankLevel.None), (int)ModrankLevel.None)
            .AddChoice(nameof(ModrankLevel.Moderator), (int)ModrankLevel.Moderator)
            .AddChoice(nameof(ModrankLevel.Administrator), (int)ModrankLevel.Administrator)
          ).WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("leavemessage")
          .WithDescription("Set the message that will be sent when a user leaves the server")
          .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
          .AddOption("message", ApplicationCommandOptionType.String, "The message. Use $user where the the name of the user should be substituted", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).AddOption(new SlashCommandOptionBuilder()
          .WithName("timezone")
          .WithDescription("Set the default time zone for /humantime")
          .AddOption("timezone", ApplicationCommandOptionType.String, "The time zone", isRequired: true)
          .WithType(ApplicationCommandOptionType.SubCommand)
        ).WithType(ApplicationCommandOptionType.SubCommandGroup);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = (SocketGuildUser)cmd.User;
      var guild = user.Guild;
      var subcommand = cmd.GetSubcommand();

      if (!Authorize(user, subcommand.Name, out var error))
      {
        await cmd.RespondAsync(error);
        return;
      }

      var handle = subcommand.Name switch
      {
        "list" => ListSettings(cmd, guild),
        "pinchannel" => SetPinChannel(cmd, subcommand, guild),
        "logchannel" => SetLogChannel(cmd, subcommand, guild),
        "commandprefix" => SetCommandPrefix(cmd, subcommand, guild),
        "modrank" => SetModrank(cmd, subcommand),
        "leavemessage" => SetLeaveMessage(cmd, subcommand, guild),
        "timezone" => SetTimeZone(cmd, subcommand, guild),
        _ => throw new InvalidOperationException($"{Emotes.ErrorEmote} Unknown subcommand {subcommand.Name}")
      };

      await handle;
    }

    private bool Authorize(SocketGuildUser user, string subcommand, out string? error)
    {
      if (subcommand == "list")
      {
        if (!service.IsAuthorized(user, ModrankLevel.Moderator, out error))
        {
          return false;
        }
      }
      else
      {
        if (!service.IsAuthorized(user, ModrankLevel.Administrator, out error))
        {
          return false;
        }
      }

      return true;
    }

    private async Task ListSettings(SocketSlashCommand cmd, SocketGuild guild)
    {
      var pinChannel = await service.GetPinChannel(guild);
      var logChannel = await service.GetLogChannel(guild);
      var prefix = await service.GetCommandPrefix(guild);
      var modranks = await service.GetModranks(guild);
      var leaveMessage = await service.GetLeaveMessage(guild);
      var timeZone = await service.GetTimeZone(guild);

      string modrankAdminString = "", modrankModString = "";
      foreach (var modrank in modranks)
      {
        if (modrank.Level.ToString() == "Administrator")
        {
          modrankAdminString += $" {modrank.Role.Mention}";
        }
        else if (modrank.Level.ToString() == "Moderator")
        {
          modrankModString += $"{modrank.Role.Mention}";
        }
      }

      string leaveMessageString = leaveMessage is null ? "None" : $"{leaveMessage.Value.Message} in {leaveMessage.Value.Channel.Mention}";

      var embed = new EmbedBuilder()
        .WithAuthor(guild.Name, iconUrl: guild.IconUrl)
        .WithTitle("Bot Settings")
        .AddField("Pin chanel", pinChannel?.Mention ?? "None", inline: true)
        .AddField("Log channel", logChannel?.Mention ?? "None", inline: true)
        .AddField("Custom command prefix", prefix, inline: true)
        .AddField("Bot Administrator ranks", string.IsNullOrEmpty(modrankAdminString) ? "None" : modrankAdminString, inline: true)
        .AddField("Moderator ranks", string.IsNullOrEmpty(modrankModString) ? "None" : modrankModString, inline: true)
        .AddField("Leave message", leaveMessageString, inline: true)
        .AddField("Time zone", timeZone is null ? "None" : timeZone.TimeZoneString, inline: true)
        .WithColor(Colors.Blurple);

      await cmd.RespondAsync(embed: embed.Build());
    }

    private async Task SetPinChannel(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      await service.SetPinChannel(guild, channel);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Pin channel set to {channel.Mention}");
    }

    private async Task SetLogChannel(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      await service.SetLogChannel(guild, channel);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Log channel set to {channel.Mention}");
    }

    private async Task SetCommandPrefix(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var prefix = subcommand.GetOption<string>("prefix")!;
      await service.SetCommandPrefix(guild, prefix);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Command prefix set to {prefix}");
    }

    private async Task SetModrank(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand)
    {
      var role = subcommand.GetOption<SocketRole>("role")!;
      var level = (ModrankLevel)subcommand.GetOption<long>("level")!;
      await service.SetModrank(role, level);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Modrank is now {level} for role {role.Mention}");
    }

    private async Task SetLeaveMessage(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var channel = subcommand.GetOption<SocketTextChannel>("channel")!;
      var message = subcommand.GetOption<string>("message")!;

      if (!message.Contains("$user"))
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} The message must contain the placeholder $user");
        return;
      }

      await service.SetLeaveMessage(guild, channel, message);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Leave message set to {message} in {channel.Mention}");
    }

    private async Task SetTimeZone(SocketSlashCommand cmd, SocketSlashCommandDataOption subcommand, SocketGuild guild)
    {
      var timeZone = subcommand.GetOption<string>("timezone")!;

      TimeZoneTime parsed;
      try
      {
        parsed = TimeZoneTime.Parse(timeZone);
      }
      catch (FormatException ex)
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} {ex.Message}");
        return;
      }

      if (parsed.TimeZoneBase is null)
      {
        await cmd.RespondAsync($"{Emotes.ErrorEmote} Time zone is not specified");
        return;
      }

      await service.SetTimeZone(guild, parsed);
      await cmd.RespondAsync($"{Emotes.SuccessEmote} Time zone set to {parsed.TimeZoneString}");
    }
  }
}
