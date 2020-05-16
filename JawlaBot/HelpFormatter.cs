using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using DSharpPlus;
using DSharpPlus.CommandsNext;

using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;

namespace Roblot
{
    /// <summary>
    /// Referenced from Emzi0767's Music Turret Bot
    /// https://github.com/Emzi0767/Discord-Music-Turret-Bot/blob/master/Emzi0767.MusicTurret/TurretHelpFormatter.cs
    /// </summary>
    /// 
    public class HelpFormatter : BaseHelpFormatter
    {
        private StringBuilder finalMessage { get; }
        private StringBuilder roblotCommands { get; }
        private StringBuilder musicCommands { get; }
        private StringBuilder moneyCommands { get; }

        private Roblot bot { get; }
        private VidyaListRandomizer vidya { get; }
        private bool _hasCommand { get; set; } = false;

        public HelpFormatter(CommandContext ctx, Roblot Bot) :base(ctx)
        {
            this.bot = Bot;
            this.finalMessage = new StringBuilder();
            this.roblotCommands = new StringBuilder();
            this.musicCommands = new StringBuilder();
            this.moneyCommands = new StringBuilder();

            vidya = new VidyaListRandomizer();

            this.finalMessage.AppendLine(Formatter.BlockCode(vidya.getCompanyReview(), "fix")).AppendLine();
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this._hasCommand = true;

            this.finalMessage.AppendLine(Formatter.Bold(command.QualifiedName) + ":")
                .AppendLine(Formatter.Italic(command.Description) + "\n");

            if (command.Aliases?.Any() == true)
                this.finalMessage.AppendLine($"{Formatter.Underline("Aliases:")}\n{string.Join(", ", command.Aliases)}\n");

            if(command.Overloads[0].Arguments.Any() == true) //An argument exists
            {
                this.finalMessage.AppendLine($"{Formatter.Underline("Arguments:")}");

                foreach(var overload in command.Overloads)
                {
                    String descriptions = String.Empty;

                    this.finalMessage.Append($"{Formatter.Bold(command.Name)} ");
                    foreach (var argument in overload.Arguments)
                    {
                        var isCatchAll = argument.IsCatchAll;
                        var isOptional = (argument.IsOptional) ? "[OPTIONAL]" : "";
                        var formattedArgument = String.Empty;
                        if (isCatchAll)
                        {
                            formattedArgument = $"[{argument.Name}...]";
                        }
                        else
                        {
                            formattedArgument = $"<{argument.Name}>";
                        }

                        this.finalMessage.Append($"{Formatter.Bold(formattedArgument)} ");                     
                        descriptions += $"{argument.Name}: {Formatter.Italic($"{argument.Description}")} {isOptional}\n";
                    }
                    this.finalMessage.AppendLine("\n"+descriptions);
                }
            }
            return this;
        }


        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (this._hasCommand)
                this.finalMessage.AppendLine()
                    .AppendLine($"{Formatter.Bold("Available subcommands:")}");
            else
                this.finalMessage.AppendLine($"{Formatter.Bold("Available commands:")}\n");

            var maxLen = subcommands.Max(x => x.Name.Length) + 2;
            foreach (var cmd in subcommands)
            {

                //Console.WriteLine(cmd.Module.ModuleType.Name.ToString());

                switch (cmd.Module.ModuleType.Name.ToString())
                {
                    case "RoblotCommands":
                        roblotCommands.AppendLine($"{cmd.Name.ToFixedWidth(maxLen)}  {cmd.Description}");
                        break;

                    case "MusicCommands":
                        musicCommands.AppendLine($"{cmd.Name.ToFixedWidth(maxLen)}  {cmd.Description}");
                        break;
                    case "MoneyTrackerCommands":
                        moneyCommands.AppendLine($"{cmd.Name.ToFixedWidth(maxLen)}  {cmd.Description}");
                        break;
                }                
            }

            this.finalMessage.AppendLine($"{Formatter.Underline("General Roblot Commands")}\n")
                .Append("```css\n" + roblotCommands + "```")
                .AppendLine($"\n{Formatter.Underline("Money Tracking Commands")}\n")
                .Append("```css\n" + moneyCommands + "```")
                .AppendLine($"\n{Formatter.Underline("Music Player Commands")}\n")
                .Append("```css\n" + musicCommands + "```");

            return this;
        }

        
        public override CommandHelpMessage Build()
        {
            this.finalMessage.Append("");
            return new CommandHelpMessage(finalMessage.ToString());
        }
    }

    public sealed class VidyaListRandomizer
    {
        private List<string> BadVidya { get; }
        private List<string> GoodVidya { get; }
        private Random RNG { get; }

        public VidyaListRandomizer()
        {
            BadVidya = new List<string>() { "EA", "Activision", "Epic", "Fortnite", "Konami", "Bethesda", "Todd Howard", "Take-Two", "Sony", "Xbox", "Google"};
            GoodVidya = new List<string>() { "CDPR", "Minecraft", "Geraldo", "Todd Howard", "Valve", "Kojima", "Nintendo", "Cyberpunk means cool future" };
            this.RNG = new Random();
        }
        /// <summary>
        /// EA bad. Geraldo good.
        /// </summary>
        /// <returns></returns>
        public string getCompanyReview()
        {
            var coinFlip = RNG.Next(0, 2);
            var finalString = "";
            // EA BAD
            if(coinFlip == 0)
            {
                var companyBad = BadVidya[RNG.Next(0, BadVidya.Count)];
                finalString += companyBad + " bad.";
            }
            // MINECRAFT GOOD
            else
            {
                var companyGood = GoodVidya[RNG.Next(0, GoodVidya.Count)];
                finalString += companyGood + " good.";
            }
            return finalString;
        }
        
    }

    /// <summary>
    /// Referenced from https://github.com/Emzi0767/Discord-Music-Turret-Bot/blob/master/Emzi0767.MusicTurret/TurretUtilities.cs
    /// </summary>
    public static class Utilities
    {
        public static string ToFixedWidth(this string s, int targetLength)
        {
            if (s == null)
                throw new NullReferenceException();

            if (s.Length < targetLength)
                return s.PadRight(targetLength, ' ');

            if (s.Length > targetLength)
                return s.Substring(0, targetLength);

            return s;
        }
    }
}
