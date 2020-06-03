using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Roblot.Services;

namespace Roblot.Helpers
{
    public static class MusicCommandHelpers
    {
        // Helps do a poll to confirm if you want to delete
        public static async Task<PlaylistResult> DeletePlaylistHelperAsync(CommandContext ctx, string confirmMessage, string playlistName, PasteBinService pasteBinAPI)
        {
            var interactive = ctx.Client.GetInteractivity();
            // If already exists, ask if we want to overwrite
            var pollDuration = TimeSpan.FromSeconds(30);

            var confirmationMessage = await ctx.RespondAsync(confirmMessage);

            // Get the answer from the user
            var okReaction = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            await confirmationMessage.CreateReactionAsync(okReaction);

            var cancelReaction = DiscordEmoji.FromName(ctx.Client, ":x:");
            await confirmationMessage.CreateReactionAsync(cancelReaction);

            var poll_result = await interactive.WaitForReactionAsync(x => x.Emoji == okReaction || x.Emoji == cancelReaction, confirmationMessage, ctx.User, pollDuration);

            // Don't do anything if user cancels or poll times out
            if (poll_result.Result == null || poll_result.Result.Emoji == cancelReaction)
            {
                await confirmationMessage.DeleteAsync();
                return PlaylistResult.Cancelled;
            }

            // If yes, delete the old playlist
            var deleteResult = await pasteBinAPI.deletePlaylistAsync(playlistName);

            // Error handling if deleting failed for some reason
            if (deleteResult == PlaylistResult.Failed)
            {
                return PlaylistResult.Failed;
            }

            return PlaylistResult.Successful;
        }
    }
}
