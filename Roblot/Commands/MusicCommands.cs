using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.Lavalink;
using Roblot.Services;
using Roblot.Data;
using Roblot.Items;
using System.Xml;
using PastebinAPI;
using Roblot.Helpers;

namespace Roblot
{
    public sealed class MusicCommands : BaseCommandModule
    {
        private LavalinkService Lavalink { get; }
        private MusicData MusicData { get; set; }
        private PasteBinService PasteBin { get; }

        public MusicCommands(MusicData music, LavalinkService lavalink, PasteBinService pastebin)
        {
            
            this.MusicData = music;
            this.Lavalink = lavalink;
            this.PasteBin = pastebin;
        }
        // Lavalink service to handle all lavalink configuration and init
        // MusicData service to handle all the options and attributes of the player (volume setting, shuffle setting, queue, now playing string)
        // This class will ONLY handle commands

        //TODO:
        // Add support for inserting youtube playlists
        // Add database support for discord playlists

        // Overrride the task before execution
        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            var userChannel = ctx.Member.VoiceState?.Channel;
            if (userChannel == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} You need to be in a voice channel").ConfigureAwait(false);
                throw new Exception("Command was cancelled due to unmet criteria");
            }
            var guildChannel = ctx.Guild.CurrentMember?.VoiceState?.Channel;
            if (guildChannel != null && guildChannel != userChannel)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} You need to be in the same voice channel").ConfigureAwait(false);
                throw new Exception("Command was cancelled due to unmet criteria");
            }
            this.MusicData.TextChannel = ctx.Channel;
            await base.BeforeExecutionAsync(ctx).ConfigureAwait(false);
        }

        [Command("play")]
        [Priority(2)]
        [Description("Resumes/Plays the current queue if a song is already playing")]
        public async Task PlayAsync(CommandContext ctx)
        {
            var currentTrack = MusicData.NowPlaying;
            if (currentTrack.Track == null || currentTrack.Track.TrackString == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} The queue currently seems to be empty. Add a song to play by using the command {Formatter.InlineCode($"{ctx.Prefix}play <url/search terms>")}");
                return;
            }

            await MusicData.Resume();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Playing track  {Formatter.Bold(currentTrack.Track.Title)} by {Formatter.Bold(currentTrack.Track.Author)}");
        }

        [Command("play")]
        [Aliases("p")]
        [Priority(1)]
        [Description("Plays a specified URL")]
        public async Task PlayAsync(CommandContext ctx, [Description("The url to play from")] Uri url) //There's no error handling on this yet
        {
            var getTracks = await Lavalink.lavaRest.GetTracksAsync(url).ConfigureAwait(false);
            var tracks = getTracks.Tracks;
            if (getTracks.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any())
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":cry:")} I couldn't find any tracks with that url").ConfigureAwait(false);
                return;
            }
            foreach (var trackElement in tracks)
            {
                MusicData.QueueTrack(new TrackItem(trackElement, ctx.Member));
            }

            await this.MusicData.CreatePlayerAsync(ctx.Member.VoiceState.Channel).ConfigureAwait(false);

            await MusicData.Play();

            var track = tracks.First();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Added {Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)} - ({Time_Convert.CompressLavalinkTime(track.Length)})");

            // Acknowledge that we loaded a playlist
            if(getTracks.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                await ctx.RespondAsync($"Plus an additional {Formatter.Bold((tracks.Count() - 1).ToString())} tracks from the playlist {Formatter.Bold(getTracks.PlaylistInfo.Name)}");
            }
            //await playHandler(ctx, url.ToString());
        }

        [Command("play")]
        [Priority(0)]
        [Description("Searches youtube with the given terms for playback")]
        public async Task PlayAsync(CommandContext ctx, [RemainingText, Description("Terms to search for")] string terms)
        {
            var interactivity = ctx.Client.GetInteractivity();

            var results = await Lavalink.lavaRest.GetTracksAsync(terms, LavalinkSearchType.Youtube);
            if(results.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":cry:")} I couldn't find anything with those search keys");
            }
            var trackResults = results.Tracks.Take(5);

            var pollDuration = TimeSpan.FromSeconds(30);

            List<DiscordEmoji> listOfSelections = new List<DiscordEmoji>() { DiscordEmoji.FromName(ctx.Client, ":one:"), DiscordEmoji.FromName(ctx.Client, ":two:"), DiscordEmoji.FromName(ctx.Client, ":three:"), DiscordEmoji.FromName(ctx.Client, ":four:"), DiscordEmoji.FromName(ctx.Client, ":five:") };

            // present the poll

            var foo = string.Join("\n", trackResults.Select((x, i) => $"Track {i + 1}: {Formatter.Bold(x.Title)} by {Formatter.Bold(x.Author)} - ({Time_Convert.CompressLavalinkTime(x.Length)})"));
            var searchResults = await ctx.RespondAsync($"Here is what I found:\n\n{foo}");

            // add the options as reactions and add a cancel button
            foreach (DiscordEmoji emoji in listOfSelections)
            {
                await searchResults.CreateReactionAsync(emoji);
            }

            var cancelEmoji = DiscordEmoji.FromName(ctx.Client, ":x:");
            await searchResults.CreateReactionAsync(cancelEmoji);
            // get reactions
            var poll_result = await interactivity.WaitForReactionAsync(xm => listOfSelections.Contains(xm.Emoji) || xm.Emoji == cancelEmoji, searchResults, ctx.User, pollDuration);

            if (poll_result.Result != null)
            {
                await searchResults.DeleteAsync();
                if (poll_result.Result.Emoji == cancelEmoji)
                {
                    await ctx.RespondAsync("Query Cancelled");
                    return;
                }
                int parseSelect = Int32.Parse(poll_result.Result.Emoji.Name.Substring(0, 1)) - 1;
                var track = trackResults.ElementAt(parseSelect);

                MusicData.QueueTrack(new TrackItem(track, ctx.Member));

                await this.MusicData.CreatePlayerAsync(ctx.Member.VoiceState.Channel).ConfigureAwait(false);
                await MusicData.Play();

                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Added {Formatter.Bold(track.Title)} by {Formatter.Bold(track.Author)} - ({Time_Convert.CompressLavalinkTime(track.Length)})");
            }
            else
            {
                await searchResults.DeleteAsync();
                await ctx.RespondAsync("Request Timed Out.");
            }
        }

        [Command("stop")]
        [Aliases("s")]
        [Description("Stops the song and resets it to the beginning")]
        public async Task StopPlayerAsync(CommandContext ctx)
        {
            if(this.MusicData.NowPlaying.Track == null || this.MusicData.NowPlaying.Track.TrackString == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} No song is currently playing");
                return;
            }
            await this.MusicData.Pause();
            await this.MusicData.SeekPlayerAsync(new TimeSpan(0,0,0));
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Stopping track - playback reset");
        }

        [Command("disconnect")]
        [Aliases("d", "leave")]
        [Description("Empties the queue and disconnects the bot from the channel")]
        public async Task DisconnectAsync(CommandContext ctx)
        {
            // Empties the queue
            int numRemoved = MusicData.EmptyQueue();

            await this.MusicData.Stop();

            await this.MusicData.DestroyPlayerAsync().ConfigureAwait(false);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Removed {numRemoved:#,##0} tracks from the queue").ConfigureAwait(false);

        }

        [Command("volume")]
        [Aliases("v")]
        [Description("Sets the volume for the player")]
        public async Task SetVolumeAsync(CommandContext ctx, [Description("Volume to be set. Volume range is 0-150. Default is 100")] int volume = 100)
        {
            if (volume > 150 || volume < 0)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Volume must be greater than 0 or less than 150").ConfigureAwait(false);
                return;
            }

            await this.MusicData.SetVolume(volume);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Volume set to {volume}%").ConfigureAwait(false);
        }

        [Command("remove")]
        [Aliases("rm")]
        [Description("Removes a track from the playback queue")]
        public async Task RemoveTrackAsync(CommandContext ctx, [Description("Which track to remove")] int index)
        {
            var queueItem = MusicData.Remove(index - 1);
            if (queueItem == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":cry:")} Track does not exist").ConfigureAwait(false);
                return;
            }

            var track = queueItem.Value;
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Removed track {Formatter.Bold($"{track.Track.Title}")} by {Formatter.Bold($"{track.Track.Author}")}").ConfigureAwait(false);
        }

        [Command("pause")]
        [Description("Pauses the player")]
        public async Task PausePlayerAsync(CommandContext ctx)
        {
            await this.MusicData.Pause();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Playback paused. Use {Formatter.InlineCode($"{ctx.Prefix}play")} to resume playback").ConfigureAwait(false);
        }

        [Command("repeat")]
        [Aliases("loop", "l")]
        [Description("Sets the repeat mode of the player")]
        public async Task RepeatAsync(CommandContext ctx, [Description("Repeat mode to set. Can be All, Single or None")] string mode)
        {
            // set input to lower
            var modeLower = mode.ToLower();
            // Check if repeat mode is valid
            if (!MusicData.checkRepeatInput(modeLower))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Not a valid repeat setting");
                return;
            }
            else
            {
                MusicData.SetRepeat(modeLower);
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Repeat mode set to {modeLower}");
            }
        }

        [Command("skip")]
        [Aliases("next", "n")]
        [Description("Skips the current track in the queue")]
        public async Task SkipTrackAsync(CommandContext ctx)
        {
            await this.MusicData.Stop();
            await ctx.RespondAsync("Skipping track").ConfigureAwait(false);
        }

        [Command("shuffle")]
        [Description("Toggles shuffle of current playback queue. Tracks added to the queue while shuffled will be added at a random index")]
        public async Task ShuffleAsync(CommandContext ctx)
        {
            // Turn on the shuffle
            if (!MusicData.IsShuffled)
            {
                MusicData.Shuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Shuffle on");
            }
            // nah fuck it, turn it off
            else
            {
                MusicData.StopShuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Shuffle off");
            }
        }

        [Command("reshuffle")]
        [Description("Reshuffles the queue if in shuffle mode")]
        public async Task ReshuffleAsync(CommandContext ctx)
        {
            if (MusicData.IsShuffled)
            {
                MusicData.Reshuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Queue Reshuffled").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Shuffle not enabled").ConfigureAwait(false);
            }
        }

        [Command("queue")]
        [Description("Shows the current playback queue")]
        [Aliases("q")]
        public async Task SeeQueueAsync(CommandContext ctx)
        {
            var queueString = String.Empty;
            var num = 1;
            var interactivity = ctx.Client.GetInteractivity();
            if (MusicData.RepeatMode == "single")
            {
                var repeatingTrack = MusicData.NowPlaying;
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Queue currently is repeating {Formatter.Bold($"{repeatingTrack.Track.Title}")} by {Formatter.Bold($"{repeatingTrack.Track.Author}")}").ConfigureAwait(false);
                return;
            }

            if (MusicData.PublicQueue.Count == 0)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Queue is currently empty!");
                return;
            }

            try
            {
                foreach (var track in MusicData.PublicQueue)
                {
                    queueString += $"{num}. {Formatter.Bold($"{track.Track.Title}")} by {Formatter.Bold($"{track.Track.Author}")} - ({Time_Convert.CompressLavalinkTime(track.Track.Length)})\n";
                    num++;
                }
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Here is the list of currently queued tracks: \n");
                var queuePages = interactivity.GeneratePagesInEmbed(queueString, SplitType.Line);

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, queuePages, null, PaginationBehaviour.WrapAround);
            }
            catch(ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }
        }

        [Command("nowplaying")]
        [Aliases("np")]
        [Description("Gets information about the current track")]
        public async Task NowPlayingAsync(CommandContext ctx)
        {
            var track = MusicData.NowPlaying;

            if (MusicData.NowPlaying.Track == null || MusicData.NowPlaying.Track.TrackString == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} I'm not playing anything right now!").ConfigureAwait(false);
            }
            else
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Now playing: {Formatter.Bold($"{track.Track.Title}")} by {Formatter.Bold($"{track.Track.Author}")} - ({Time_Convert.CompressLavalinkTime(track.Track.Length)})").ConfigureAwait(false);
            }
        }

        // Save to database

        [Command("export")]
        [Aliases("save")]
        [Description("Saves the current queue and currently playing song (if applicable) as a personal playlist")]
        public async Task ExportAsync(CommandContext ctx, [RemainingText, Description("Name of the Playlist")] string playlistName)
        {
            var interactive = ctx.Client.GetInteractivity();

            List<string> trackQueue = new List<string>();

            //Check if queue is empty and there's nothing playing 
            if (MusicData.PublicQueue.Count == 0 && MusicData.NowPlaying.Track == null)
            {
                await ctx.RespondAsync($"{ DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There's nothing in the queue or playing right now to save!").ConfigureAwait(false);
                return;
            }

            if(String.IsNullOrWhiteSpace(playlistName))
            {
                await ctx.RespondAsync($"{ DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Playlist name is empty!").ConfigureAwait(false);
                return;
            }

            // If there's a song playing currently add it to the queue
            if (MusicData.NowPlaying.Track != null || MusicData.NowPlaying.Track.TrackString != null)
            {
                trackQueue.Add(MusicData.NowPlaying.Track.TrackString);
            }

            // Add each song in the queue
            foreach (var track in MusicData.PublicQueue)
            {
                trackQueue.Add(track.Track.TrackString);
            }

            // Check if playlist name already exists
            if(await PasteBin.playlistExists(playlistName))
            {
                string overwriteConfirm = $"{DiscordEmoji.FromName(ctx.Client, ":warning:")} There's already a playlist with the name {Formatter.Bold(playlistName)}. Would you like to overwrite it?";
                PlaylistResult deleteResult = await MusicCommandHelpers.DeletePlaylistHelperAsync(ctx, overwriteConfirm, playlistName, PasteBin);

                switch(deleteResult)
                {
                    case PlaylistResult.Cancelled:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":warning:")} Overwrite cancelled");
                        return;
                    case PlaylistResult.Failed:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There was a problem deleting the playlist");
                        return;
                    case PlaylistResult.Successful:
                        // Don't say anything if delete goes through - delete is just an intermediate step to overwriting
                        break;
                    default:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":REEE:")} You done goofed");
                        return;
                }
            }

            // Save the playlist
            var saveResult = await PasteBin.saveTracksAsync(trackQueue, playlistName);

            if (saveResult == PlaylistResult.Successful)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Playlist {Formatter.Bold(playlistName)} saved!");
            }
            else
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There was a problem saving the playlist {Formatter.Bold(playlistName)}.");
            }
        }

        [Command("load")]
        [Aliases("import")]
        [Description("Imports the user's playlist that they made")]
        public async Task ImportAsync(CommandContext ctx, [RemainingText, Description("Playlist name to load")] string playlistName)
        {
            // If the user inputs no playlist name in discord
            if(playlistName == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Please specify the playlist name you want to load!");
                return;
            }

            IEnumerable<String> trackStrings = await PasteBin.loadTracksAsync(playlistName);

            // Check if there is actually a match in playlist name
            if(trackStrings != null)
            {
                int trackCount = trackStrings.Count();

                try
                {
                    IEnumerable<LavalinkTrack> tracks = await Lavalink.lavaRest.DecodeTracksAsync(trackStrings.ToArray());
                    foreach (var track in tracks)
                    {
                        MusicData.QueueTrack(new TrackItem(track, ctx.Member));
                    }
                        await this.MusicData.CreatePlayerAsync(ctx.Member.VoiceState.Channel).ConfigureAwait(false);
                        await MusicData.Play();
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Loaded playlist {Formatter.Bold(playlistName)} with {Formatter.Bold(trackCount.ToString())} tracks");
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Now playing: {Formatter.Bold($"{tracks.First().Title}")} by {Formatter.Bold($"{tracks.First().Author}")} - ({Time_Convert.CompressLavalinkTime(tracks.First().Length)})").ConfigureAwait(false);
                        return;
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine($"Exception {e} when decoding tracks");
                    await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There was an error loading the playlist");
                }
            }
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Playlist name {Formatter.Bold(playlistName)} does not exist!");
        }

        [Command("deletePlaylist")]
        [Aliases("deletepl", "delete")]
        [Description("Deletes a playlist")]
        public async Task DeletePlaylist(CommandContext ctx, [RemainingText, Description("Playlist name to delete")] string playlistName)
        {
            var interactive = ctx.Client.GetInteractivity();
            if(playlistName == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Please specify the playlist name you want to delete!");
                return;
            }

            if(await PasteBin.playlistExists(playlistName))
            {
                string deleteConfirm = $"{DiscordEmoji.FromName(ctx.Client, ":warning:")} Are you sure you want to delete the playlist {Formatter.Bold(playlistName)}?";
                PlaylistResult deleteResult = await MusicCommandHelpers.DeletePlaylistHelperAsync(ctx, deleteConfirm, playlistName, PasteBin);

                switch(deleteResult)
                {
                    case PlaylistResult.Cancelled:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":warning:")} Deletion cancelled");
                        break;

                    case PlaylistResult.Failed:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There was a problem deleting the playlist");
                        break;
                    case PlaylistResult.Successful:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Deleted the playlist {Formatter.Bold(playlistName)}");
                        break;
                    default:
                        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":REEE:")} You done goofed");
                        break;
                }
            }
            else
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} Playlist {Formatter.Bold(playlistName)} doesn't exist!");
            }
        }

        [Command("list")]
        [Aliases("listplaylists", "listpl")]
        [Description("Lists all playlists currently saved and the number of tracks they have")]
        public async Task ListPlaylists(CommandContext ctx)
        {
            Dictionary<String, int> listOfPlaylists = await PasteBin.listPlaylistsAsync();

            if (listOfPlaylists.Count == 0)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":no_entry:")} There are no playlists currently saved");
                return;
            }

            var interactivity = ctx.Client.GetInteractivity();
            string playlistInfo = string.Join('\n', listOfPlaylists.Select(x => $"{Formatter.Bold(x.Key)} - {Formatter.Bold(x.Value.ToString())} tracks"));

            try
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":musical_note:")} Here is the list of playlists currently saved: \n");
                var queuePages = interactivity.GeneratePagesInEmbed(playlistInfo, SplitType.Line);

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, queuePages, null, PaginationBehaviour.WrapAround);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }
        }

        [Command("about")]
        [Aliases("info", "i")]
        [Description("Gets info about the player")]
        public async Task AboutPlayerAsync(CommandContext ctx)
        {
            var embeddedSettings = $"{Formatter.Bold("Queue Length:")} {MusicData.PublicQueue.Count}\n{Formatter.Bold("Is Shuffled:")} {MusicData.IsShuffled.ToString()}\n{Formatter.Bold("Repeat Mode:")} {MusicData.RepeatMode.First().ToString().ToUpper() + MusicData.RepeatMode.Substring(1)}\n{Formatter.Bold("Volume:")} {MusicData.Volume}%";

            var embed = new DiscordEmbedBuilder()
            {
                Title = "Here are the current settings for the player",
                Description = embeddedSettings,
                Color = DiscordColor.Aquamarine,
            };
            await ctx.RespondAsync(embed: embed).ConfigureAwait(false);
        }
    }
}
