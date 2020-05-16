using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.EventArgs;

namespace Roblot.Services
{
    public sealed class LavalinkService
    {
        public LavalinkNodeConnection lavaNode { get; private set; }
        public LavalinkRest lavaRest { get; private set; }
        private String IPAddress { get; }
        private String Port { get; }

        public LavalinkService(DiscordClient client, String ipAddress, String port)
        {
            // Run this task when the discord bot is ready
            this.IPAddress = ipAddress;
            this.Port = port;

            client.Ready += Client_Ready;
        }
        private async Task Client_Ready(ReadyEventArgs e)
        {
            // Only run this if we do not have a lavalink node connection
            if(this.lavaNode != null)
            {
                return;
            }

            var lava = e.Client.GetLavalink();

            this.lavaNode = await lava.ConnectAsync(new LavalinkConfiguration
            {
                RestEndpoint = new ConnectionEndpoint { Hostname = IPAddress, Port = Int32.Parse(this.Port) },
                SocketEndpoint = new ConnectionEndpoint { Hostname = IPAddress, Port = Int32.Parse(this.Port) },
                Password = "xd"
            }).ConfigureAwait(false);

            lavaRest = this.lavaNode.Rest;

        }
    }
}
