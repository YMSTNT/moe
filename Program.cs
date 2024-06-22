using Discord;
using Discord.WebSocket;
using Moe;
using Moe.Commands;
using Moe.Services;

await ConfigService.Init(args);
DiscordService.Init();

bool isReadyEventFired = false;
DiscordService.Discord.Ready += async () =>
{
  if (isReadyEventFired)
  {
    return;
  }
  isReadyEventFired = true;

  var settingsService = new SettingsService();
  var customCommandService = new CustomCommandService(settingsService);
  var customRoleService = new CustomRoleService(settingsService);
  var levelService = new LevelService(settingsService);
  var rngService = new RngService(settingsService);
  var rngCommand = new RngCommand(rngService);
  var customCommandHandler = new CustomCommandHandler(customCommandService);
  var pinService = new PinService(settingsService);
  LogService.Instance = new LogService(settingsService);
  var discordLogService = new DiscordLogService();
  discordLogService.Register();
  var roleRememberService = new RoleRememberService();
  await roleRememberService.Register();
  var purgeService = new PurgeService(settingsService);
  var sayService = new SayService(settingsService);
  var userInfoService = new UserInfoService();
  var templateService = new TemplateService(settingsService);
  var snapshotService = new SnapshotService(settingsService);
  var heartbeatService = new HeartbeatService();
  heartbeatService.Init();
  var commandLoggerService = new CommandLoggerService();
  var humanTimeService = new HumanTimeService(settingsService);
  var customEmbedService = new CustomEmbedService(settingsService);
  var autoroleService = new AutoroleService(settingsService);
  autoroleService.Register();

  var slashCommands = new List<SlashCommandBase>
  {
    new PingCommand(),
    rngCommand,
    new CustomCommandCommand(customCommandService),
    new CustomRoleCommand(customRoleService),
    new SettingsCommand(settingsService),
    new RankCommand(levelService),
    new LevelsCommand(levelService),
    new PinSlashCommand(pinService),
    new LevelupMessage(levelService),
    new PurgeCommand(purgeService),
    new SayCommand(sayService),
    new UserinfoCommand(userInfoService),
    new ServerinfoCommand(settingsService),
    new TemplateCommand(templateService),
    new AnnounceCommand(templateService),
    new SnapshotCommand(snapshotService),
    new HumanTimeCommand(humanTimeService),
    new CustomEmbedCommand(customEmbedService),
};

  var messageCommands = new List<MessageCommandBase>
  {
    new PinMessageCommand(pinService),
    new EditEmbedMessageCommand(customEmbedService),
  };

  var userCommands = new List<UserCommandBase>
  {
    new NoXPUserCommand(levelService),
  };

  if (args.Contains("--register-commands"))
  {
    await LogService.LogToFileAndConsole("Registering commands");
    var commandProperties = slashCommands
      .Select(x => x.GetCommandProperties())
      .Cast<ApplicationCommandProperties>()
      .Concat(messageCommands.Select(x => x.GetCommandProperties()))
      .Concat(userCommands.Select(x => x.GetCommandProperties()))
      .ToArray();

    if (ConfigService.Options.IsProduction)
    {
      await DiscordService.Discord.BulkOverwriteGlobalApplicationCommandsAsync(commandProperties);
    }
    else if(ConfigService.Options.IsDevelopment)
    {
      var guild = DiscordService.Discord.GetGuild(ConfigService.Environment.ServerID!.Value);
      await guild.BulkOverwriteApplicationCommandAsync(commandProperties);
    }
  }

  DiscordService.Discord.SlashCommandExecuted += async (cmd) =>
  {
    if (cmd.Channel.GetChannelType() != ChannelType.Text)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Slash commands are not allowed in DMs");
      return;
    }

    var loggingTask = commandLoggerService.LogSlashCommand(cmd);

    var command = slashCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
    await loggingTask;
  };

  DiscordService.Discord.MessageCommandExecuted += async (cmd) =>
  {
    if (cmd.Channel.GetChannelType() != ChannelType.Text)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Context menu commands are not allowed in DMs");
      return;
    }

    var guild = ((SocketGuildChannel)cmd.Channel).Guild;
    await LogService.LogToFileAndConsole(
      $"{cmd.User} executed message command {cmd.CommandName} on message {cmd.Data.Message}", guild);

    var command = messageCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.UserCommandExecuted += async (cmd) =>
  {
    if (cmd.Channel.GetChannelType() != ChannelType.Text)
    {
      await cmd.RespondAsync($"{Emotes.ErrorEmote} Context menu commands are not allowed in DMs");
      return;
    }

    var guild = ((SocketGuildChannel)cmd.Channel).Guild;
    await LogService.LogToFileAndConsole(
      $"{cmd.User} executed user command {cmd.CommandName} on user {cmd.Data.Member}", guild);

    var command = userCommands.First(x => cmd.CommandName == x.Name);
    await command.Handle(cmd);
  };

  DiscordService.Discord.MessageReceived += async (msg) =>
  {
    if (msg.Author.IsBot)
    {
      return;
    }

    var tokens = msg.Content.Split(' ');
    var args = tokens.Skip(1).ToList();
    if (msg.Channel.GetChannelType() == ChannelType.DM)
    {
      var commandName = tokens[0];
      await rngCommand.HandleDM(msg, commandName, args);
    }
    else if (msg.Channel.GetChannelType() == ChannelType.Text)
    {
      var guild = ((SocketGuildChannel)msg.Channel).Guild;
      var commandName = await customCommandService.CleanCommandName(guild, tokens[0]);
      await levelService.HandleMessage(msg);

      var prefix = await settingsService.GetCommandPrefix(guild);
      if (!msg.Content.StartsWith(prefix))
      {
        return;
      }

      await customCommandHandler.TryHandleCommand(msg, commandName, args);
    }
  };
};

await DiscordService.Start();
