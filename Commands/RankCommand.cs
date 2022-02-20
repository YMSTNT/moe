using Discord;
using Discord.WebSocket;
using TNTBot.Services;

namespace TNTBot.Commands
{
  public class RankCommand : SlashCommandBase
  {
    private readonly LevelService service;

    public RankCommand(LevelService service) : base("rank")
    {
      Description = "Get your level";
      Options = new SlashCommandOptionBuilder()
        .AddOption("user", ApplicationCommandOptionType.User, "The user to get the rank of", isRequired: false);
      this.service = service;
    }

    public override async Task Handle(SocketSlashCommand cmd)
    {
      var user = cmd.GetOption<SocketGuildUser>("user") ?? (SocketGuildUser)cmd.User;

      await service.EnsureLevelExists(user);
      var level = await service.GetLevel(user);
      var rank = await service.GetRank(user);
      var percentageOut = Math.Round(level.PercentageToNextLevel * 100);
      var progressBar = GetProgressBar(level.PercentageToNextLevel);

      var embed = new EmbedBuilder()
          .WithAuthor(user)
          .WithDescription($"Rank: {rank}#, Level: {level.LevelNumber}\nNext level progression: {level.XPFromThisLevel} XP / {level.TotalLevelXP} XP {progressBar} {percentageOut}%")
          .WithColor(user.Roles.Last().Color);

      await cmd.RespondAsync(embed: embed.Build());
    }

    private string GetProgressBar(double percentage)
    {
      var barLength = 20;
      var filledLength = (int)(barLength * percentage);
      var emptyLength = barLength - filledLength;
      return $"{new string('█', filledLength)} {new string('░', emptyLength)}";
    }
  }
}
