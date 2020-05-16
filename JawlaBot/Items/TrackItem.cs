using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Roblot.Items
{
    /// <summary>
    /// Represents a single music queue item
    /// </summary>
    public struct TrackItem
    {
        /// <summary>
        /// Gets the track to be played
        /// </summary>
        public LavalinkTrack Track { get; }

        /// <summary>
        /// Gets the member who requested the track
        /// </summary>
        public DiscordMember RequestingMember { get; }

        /// <summary>
        /// Contructs the single music data
        /// </summary>
        /// <param name="track">Track to play</param>
        /// <param name="member">Member who requested</param>
        public TrackItem(LavalinkTrack track, DiscordMember member)
        {
            this.Track = track;
            this.RequestingMember = member;
        }
    }

    // TODO: Make serializable item if we want to save playlists
}
