<h1 align="center">Moe</h1>
<h4 align="center">A multi-purpose (private) Discord bot made using Discord.NET</h4>

Moe is a Discord bot made for my friend's private server. The main goal was to utilise Discord's new bot API features such as slash commands, message and user commands, modals, select menus, buttons, responses and ephemeral responses and so on. It turned out to be pretty usable, and while it might not be as feature-rich as the other multi-purpose bots out there, it could replace almost all of the bots on that small server.

The bot is self-hosted on a Raspberry Pi, and because it is made for a private server, it cannot be invited to other servers. However, you are free to take a look at its source code and run it yourself.

# Features

The bot has numerous features, many of which can be found in other bots, but there are some unique features as well.

- Moderation
  - Message purging
  - Logging to a specified channel (message remove, edit, voice channel leave/join/change, member join/leave, member ban/unban)
  - <span id="modranks">Modranks</span>: a three-level permission system, where each command can only be used by specified moderator ranks "modranks".
- Leave messages
- Remembers roles after a user leaves, so the roles can be given back if the user joins again
- Applicable roles: users can apply and remove a set of roles from themselves
- Leveling: users gain XP and level up based on their chat activity
  - Also has No-XP feature to restrict users with a specified role from gaining XP
  - Optionally, a user can toggle the levelup message for themselves (if they find it annoying)
- Custom Commands: moderators can add custom commands to a server which prints message(s) out
  - Custom commands are not slash commands, they are simple text-based commands with a prefix which can be set in the settings.
- RNG: a simple random number generator
- User Info: prints information about the user's account and their server profile.
  - Also stores the date and time when the user first joined the server.
- Server Info: prints information about the server.
- Timezone converter
- Say: the bot repeats your message
- Embed: moderators can use a built-in embed builder and send an embed using the bot
- Message Pinning: users can pin messages to a specified pin channel
  - Works sort of like a starboard, except the message only requires a single message command to pin
  - Note: there is no limitation on who can pin messages
- Announce: administrators can create templates made out of an embed that later can be announced to a specified channel
  - Works sort of like the embed builder, except the embed is saved as a template
    - Placeholders can be placed in the embed with `$(placeholder name)`, which later can be filled upon running the announce command
    - Templates need to have a name and a channel to send into
    - Templates can also have an optional mention argument, where the bot mentions a specified role with the announcement
    - Templates can be optionally hidden, so they won't show up in the template list
- Snapshot: administrators can save the state of the server (name, picture, channel names, voice channel names, role names) and restore it later
  - A snapshot must have a name as identifier
  - The snapshot command always saves the server's name, the others are optional
- Settings: administrators can change the settings of the bot for the server
  - Pin channel: channel where the pinned messages get sent (using the Message Pinning feature)
  - Log channel: channel where to log certain events
  - Custom command prefix
  - Bot Administrator ranks: the Administrator "modrank" required to run dangerous commands
  - Bot Moderator ranks: the Moderator "modrank" required to run moderation commands
  - No-XP role: the role that cannot receive XP in the leveling system
  - Leave message: what the bot should say after a user leaves the server and in which channel
  - Time zone: set a default timezone for the timezone converter command
  - Autorole: set roles that will be applied to new users when they join

# Running Moe

## Create a [Discord Bot](https://discord.com/developers/docs/intro#bots-and-apps)

Create a new application in the [Discord Developer Portal](https://discord.com/developers/applications). Make sure it has the following **application permissions**: `bot, applications.commands, guilds.members.read, messages.read` and **bot permissions**: `Manage Server, Manage Roles, Manage Channels, Ban Members, Read Messages/View Channels, Send Messages, Manage Messages, Embed Links, Attach Files, Use External Emojis`. Check out [Making Your First Bot with Discord.Net](https://discordnet.dev/guides/getting_started/first-bot.html) for a detailed guide on how to do it.
You will need the access token later.

## Configuration

Create **development.env** file where you cloned this repository and paste the following content there:

```
TOKEN=<DISCORD_BOT_ACCESS_TOKEN>
SERVERID=<DISCORD_SERVER_ID>
OWNERS=<DISCORD_USER_ID>
STATUS_PORT=8000
```

Replace the placeholders enclosed in angle brackets with their appropriate value. You can get the **Server ID** by right clicking on a server and choosing Copy ID. Same concept applies to **User ID**s. You probably want to assign your user's ID to **OWNERS**.

For more information, see [Configuration in detail](#configuration-in-detail).

## Run Moe with dotnet

Install [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

Run the command in a command prompt or terminal for starting the bot:

`dotnet run -- --development --register-commands`

You should see the bot online on your server. By typing **/** in the message box, various commands should appear from your bot.

# Configuration in detail

You can have separate configurations for developing the bot and using it in production.
A development configuration is in **development.env** file and looks like [this](#configuration).
Same concept applies to production configuration too, except it doesn't use the `SERVERID` field.
To tell the bot which configuration should it use, run it with `--development` or `--production`. It defaults to production in case no environment was specified.

## Options

- TOKEN: Your Discord bot's access token. Anyone with possession of this token can act on your bot's behalf.
- SERVERID: The Server ID where guild scoped commands can be registered. Only used when running in development mode.
- OWNERS: Comma separated list of User IDs who have full access to the bot. Overrides [modranks](#modranks).
- STATUS_PORT: Start a web server on this port to appear online for status services.

Commands (including slash commands and context commands) are registered at guild (discord server) scope when using development mode, and global scope when using production mode. Global command registration may take a few seconds or minutes due to Discord's API, but guild scoped commands are almost instant. In order to register commands, you need to run the bot with `--register-commands` parameter. The reason why registering commands is not the default is Discord can rate limit the bot when it tries to register commands over and over again in a short period of time. This can be annoying when debugging.

# Credits

- [Discord.Net](https://github.com/discord-net/Discord.Net) - The library used to interact with the Discord API.
- [DotNetEnv](https://github.com/tonerdo/dotnet-env) - The library used for the bot's configuration file.
- The formula to calculate leveling XP is based on [Mee6](https://mee6.xyz)'s formula.
