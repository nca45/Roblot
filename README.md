# Roblot

A discord bot created for my private discord channel. Mostly a music streaming bot with some additional assistant functions

This project is updated as needed and is written using the DSharpPlus API and C#.

## Features

* Joining and leaving the voice channel
* Stream youtube audio through the voice channel
* Shuffle, skip, and adjust volume of the music tracks
* Automatically disconnects from voice channel if no users detected
* Save and load custom music playlists
* IOwe/OweMe: Keep track of who owes money to whom (pending rewrite)
* (New!) Checks retail stores for Zen 3 stock because I'm tired of refreshing each store's page

## Features Planned

* Voice recognition?
* Some Minecraft stuff?
* Remove money tracker because we don't do that anymore?

# Documentation

The bot uses the `!` command prefix.

## Music Commands

### `!about`

Gets the info about the player (shuffle, repeat, volume, current track).

### `!volume`

Sets the volume of the player (Default = 50%).

### `!shuffle`, `!reshuffle`

Toggles shuffle of the current queue. Tracks added to the queue while shuffled will be added at a random index.
Use `!reshuffle` to reshuffle the queue.

### `!repeat <mode>`

Sets the repeat mode of the player

`<mode>` can be `All`, `Single`, or `None`.

### `!play <optional url/search terms>`

If a valid url is provided, play the url.

Otherwise, searches youtube for the given terms for playback.

If player is currently paused/stopped, `!play` will resume playback.

### `!pause`

Pauses the player.

### `!stop`

Stops the player and resets song to the beginning.

### `!skip`

Skips the current track.

### `!nowplaying`

Gets info about the current track.

### `!queue`

Shows the current playback queue.

### `!remove <index>`

Removes a track from the playback queue at `<index>`.

### `!export <playlist name>`

Saves the current queue and currently playing song (if applicable) as a personal playlist.

### `!load <playlist name>`

Imports the user's playlist that they made.

### `!deleteplaylist <playlist name`

Deletes a playlist

### `!list`

Lists all playlists currently saved and the number of tracks they have.

### `!disconnect`

Empties the queue and disconnects the bot from the channel.

## Iowe/oweMe: 'Yo can you spot me some cash?'

### `!iowe <name> <amount>` 

Updates the database to reflect that you owe `<name>` $`<amount>`.

### `!owesme <name> <amount>`

Requests `<name>` to update the database to reflect that they owe you $`<amount>`. 

`<name>` will get a notification by the bot to confirm if the `<amount>` is correct.

This command will only work if the user `<name>` is online.

### `!whoiowe`

Gets a list of users who you owe money to.

### `!whoowesme`

Gets a list of users who owes you money.

### `!pay <name> <amount>`

Pays the user `<name>` $`<amount>`. This command will only work if `<name>` is on your list of people you owe.

You can also substitute `<amount>` for the word `full`, which will pay the amount in full, or the exact amount you owe to `<name>`.

This command will only work if the user `<name>` is online.

## Other Functions: As proprietary as things come.

Some functions are on cooldown system to avoid comment spam. Because I know there will be comment spam.

### `!drop <map>`, `!pubgdrop <map>`

Can't decide on a place to drop in PUBG? Use arguments `Erangel/Forest` or `Miramar/Desert` to have the bot pick a random spot for you!

### `!listrestaruants`, `!listeats`

Gets a list of restaurants the bot currently has recorded.

### `!pickrestaurant`, `!eats`, `!pickfood`, `!dinner`, `!imhungry`

Picks a random place to eat in case Chris Chow is planning an event.

