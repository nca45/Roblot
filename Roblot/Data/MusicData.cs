using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Roblot.Services;
using Roblot.Items;

namespace Roblot.Data
{

    public sealed class MusicData
    {

        public int Volume { get; set; } = 100;
        public TrackItem NowPlaying { get; set; } = default;
        public bool IsPlaying { get; set; } = false;
        public IReadOnlyCollection<TrackItem> PublicQueue { get; }
        public bool IsShuffled { get; set; } = false;
        public string RepeatMode { get; set; } = "none";


        /// <summary>
        /// set Read only property https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/expression-bodied-members#read-only-properties
        /// </summary>
        public DiscordChannel VoiceChannel => this.Player?.Channel;

        public DiscordChannel TextChannel { get; set; }

        private List<TrackItem> Queue { get; set; }
        private List<string> RepeatList { get; } = new List<string>{ "all", "single", "none" };
        private Random RNG { get; }
        private LavalinkService Lavalink { get; }
        private LavalinkGuildConnection Player { get; set; }

        public MusicData(LavalinkService lavalink)
        {
            this.Lavalink = lavalink;
            this.Queue = new List<TrackItem>();
            this.RNG = new Random();
            // Any items added to the internal queue will be wrapped with the readonlycollection!
            // In other words, adding an item to the Queue variable will also add it to the PublicQueue
            this.PublicQueue = new ReadOnlyCollection<TrackItem>(this.Queue);
        }
        
        /// <summary>
        /// Queues a track for playback
        /// </summary>
        /// <param name="item">The music track to be queued</param>
        public void QueueTrack(TrackItem item)
        {
            lock (Queue)
            {
                // Queue is 1 and repeat mode is all means that there is only one track being repeated
                // We move the new track to the front to prevent the repeating track from being played twice
                if(RepeatMode == "all" && Queue.Count == 1)
                {
                    Queue.Insert(0, item);
                }
                // If it is not shuffled or isn't empty
                else if(!this.IsShuffled || !this.Queue.Any())
                {
                    this.Queue.Add(item);
                }
                else if (this.IsShuffled)
                {
                    //Get a random index to insert the track
                    var index = RNG.Next(0, Queue.Count);
                    this.Queue.Insert(index, item);
                }
            }
        }

        /// <summary>
        /// Plays tracks
        /// </summary>
        public async Task Play()
        {
            // do nothing if the player is not connected
            if(this.Player == null || !this.Player.IsConnected)
            {
                return;
            }

            // If there is no track playing right now play the lavalink player
            if(this.NowPlaying.Track == null)
            {
                await this.PlayHandler();
            }
        }

        /// <summary>
        /// Stops the player
        /// </summary>
        public async Task Stop()
        {
            // Do nothing if the player is not connected
            if(this.Player == null || !this.Player.IsConnected)
            {
                return;
            }

            this.NowPlaying = default;
            await Player.StopAsync();
        }

        /// <summary>
        /// Pauses the player
        /// </summary>
        public async Task Pause()
        {
            if(this.Player == null || !this.Player.IsConnected)
            {
                return;
            }
            this.IsPlaying = false;
            await this.Player.PauseAsync();
        }

        /// <summary>
        /// Resumes the player
        /// </summary>
        public async Task Resume()
        {
            if(this.Player == null || !this.Player.IsConnected)
            {
                return;
            }
            this.IsPlaying = true;
            await this.Player.ResumeAsync();
        }

        /// <summary>
        /// Dequeues next track for playback
        /// </summary>
        /// <returns>Dequeued item or null if dequeue fails</returns>
        public TrackItem? DequeueTrack()
        {
            lock (Queue)
            {
                // Queue is empty: nothing to dequeue
                if (Queue.Count == 0)
                {
                    return null;
                }

                // No repeat mode so just remove the item from the queue
                if(RepeatMode == "none")
                {
                    var item = Queue[0];
                    Queue.RemoveAt(0);
                    return item;
                }

                // Repeat this current track - aka do not delete
                if(RepeatMode == "single")
                {
                    var item = Queue[0];
                    return item;
                }

                // Send track to the back to be repeated
                if(RepeatMode == "all")
                {
                    var item = Queue[0];
                    Queue.RemoveAt(0);
                    Queue.Add(item);
                    return item;
                }

            }
            return null;
        }

        /// <summary>
        /// Empties the queue
        /// </summary>
        /// <returns>The number of items removed from the queue</returns>
        public int EmptyQueue()
        {
            // Semaphores! Lock to avoid race conditions!
            lock (this.Queue)
            {
                var items = this.Queue.Count;
                this.Queue.Clear();
                return items;
            }
        }

        /// <summary>
        /// Removes an item from the queue at a specific location
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The track to be removed, or null if unable to</returns>
        public TrackItem? Remove(int index)
        {
            lock (this.Queue)
            {
                if(index < 0 || index >= this.Queue.Count)
                {
                    return null;
                }

                var track = Queue[index];
                this.Queue.RemoveAt(index);
                return track;
            }
        }
        /// <summary>
        /// Shuffles the playback queue
        /// </summary>
        public void Shuffle()
        {
            // Do nothing if it is already shuffled
            if (this.IsShuffled)
            {
                return;
            }

            this.IsShuffled = true;
            // reshuffle the queue
            this.Reshuffle();
        }

        /// <summary>
        /// Stops the shuffle mode
        /// </summary>
        public void StopShuffle()
        {
            this.IsShuffled = false;
        }

        /// <summary>
        /// Reshuffles the queue with a new random seed
        /// </summary>
        public void Reshuffle()
        {
            lock (this.Queue)
            {
                // Shuffled based on Fisher-Yates shuffle
                int n = Queue.Count;
                while(n > 1)
                {
                    n--;
                    int k = RNG.Next(n + 1);
                    var track = Queue[k];
                    Queue[k] = Queue[n];
                    Queue[n] = track;
                }
            }
        }

        /// <summary>
        /// Sets the repeat mode of the player
        /// </summary>
        /// <param name="mode"></param>
        /// 
        /// BUG - repeat single while playing and queuing another song... after turning repeat off it will delete the other queued song
        /// 
        public void SetRepeat(string mode)
        {
            var previousMode = this.RepeatMode;
            this.RepeatMode = mode;

            // if we are currently playing a track
            if(this.NowPlaying.Track.TrackString != null)
            {
                // if we want to repeat the current track playing and we weren't in single repeat before
                if (mode == "single" && previousMode != mode)
                {
                    lock (Queue)
                    {
                        // Going from all to single, remove the last queued item which was inserted by Repeat All
                        if (previousMode == "all")
                        {
                            Queue.RemoveAt(Queue.Count - 1);
                        }
                        // Insert the current track to the front of the queue to be repeated
                        Queue.Insert(0, this.NowPlaying);
                    }
                }
                // we change the repeat mode from single to another option
                else if (mode == "all" && previousMode != mode)
                {
                    lock (Queue)
                    {
                        if (previousMode == "single")
                        {
                            // remove the repeating track in the front of the queue
                            Queue.RemoveAt(0);
                        }

                        // Currently playing a song and set repeat to all - add the current track back into queue
                        Queue.Add(this.NowPlaying);
                    }
                }
                else if (mode == "none" && previousMode != mode)
                {
                    lock (Queue)
                    {
                        if(previousMode == "all")
                        {
                            Queue.RemoveAt(Queue.Count - 1);
                        }
                        else if(previousMode == "single")
                        {
                            Queue.RemoveAt(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the volume of the player
        /// </summary>
        /// <param name="volume"></param>
        public async Task SetVolume(int volume)
        {
            // update our volume variable
            this.Volume = volume;

            // do nothing if player is not connected
            if(Player == null || !Player.IsConnected)
            {
                return;
            }
            // set the volume of the player
            await Player.SetVolumeAsync(volume);
        }

        /// <summary>
        /// Destroys the player
        /// </summary>
        /// <returns>The completed task</returns>
        public async Task DestroyPlayerAsync()
        {
            if(Player == null)
            {
                await Task.CompletedTask;
            }

            if (Player.IsConnected)
            {
                await Player.DisconnectAsync();
            }
            Player = null;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Seeks the song to a specific location
        /// </summary>
        public async Task SeekPlayerAsync(System.TimeSpan time)
        {
            await Player.SeekAsync(time);
        }
        
        
        /// <summary>
        /// Creates the player for playback
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task CreatePlayerAsync(DiscordChannel channel)
        {
            // Do nothing if player is connected
            if(this.Player != null && this.Player.IsConnected)
            {
                return;
            }

            this.Player = await this.Lavalink.lavaNode.ConnectAsync(channel).ConfigureAwait(false);
            if(this.Volume != 100)
            {
                await this.Player.SetVolumeAsync(this.Volume);
            }
            this.Player.PlaybackFinished += this.Playback_Done;
        }

        /// <summary>
        /// Checks the input for a valid repeat setting
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool checkRepeatInput(string input)
        {
            if (RepeatList.Contains(input))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Dequeues an item from the queue and plays it
        /// </summary>
        private async Task PlayHandler()
        {
            var currItem = DequeueTrack();
            if(currItem == null)
            {
                this.NowPlaying = default;
                return;
            }

            var track = currItem.Value;
            this.NowPlaying = track;
            this.IsPlaying = true;
            await this.Player.PlayAsync(track.Track);
        }
        
        /// <summary>
        /// Event handler when the player is finished playing a track
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task Playback_Done(TrackFinishEventArgs e)
        {
            await Task.Delay(500).ConfigureAwait(false);
            this.IsPlaying = false;
            await this.PlayHandler();

        }

    }
}
