# JawlaBot

A discord bot created for my friend's discord channel. Features of this bot include funny memes and some actual useful functionality. 

This project is currently in progress and is written using the DSharpPlus API and C#.

## Features

* Joining and leaving the voice channel
* Broadcasting audio through the voice channel
* Yeahboi: Ask the bot to show you it's longest 'yeah boy' ever
* Cooldown system to reduce command spam
* IOwe/OweMe: Keep track of who owes money to whom 
* Meme generator

## Features Planned

* Stream music from youtube with playlist saving
* Grab text body from top posts of subreddits

# Documentation

The bot uses the `!` command prefix.

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

## Jawla Functions: As proprietary as things come.

Some functions are on cooldown system to avoid comment spam. Because I know there will be comment spam.

### `!drop <map>`, `!pubgdrop <map>`

Can't decide on a place to drop in PUBG? Use arguments `Erangel/Forest` or `Miramar/Desert` to have the bot pick a random spot for you!

### `!listrestaruants`, `!listeats`

Gets a list of restaurants the bot currently has recorded.

### `!pickrestaurant`, `!eats`, `!pickfood`, `!dinner`, `!imhungry`

Picks a random place to eat in case Chris Chow is planning an event.

### `!yeahboi` - YOU MUST BE IN A VOICE CHANNEL

Ask the bot to show you its longest yeah boy ever. 

### `!stop`, `!timetostop`, `!frankstop`, `!itstimetostop`, `!notokay` - YOU MUST BE IN A VOICE CHANNEL

Ask the bot to let your friends know that this is not okay, and this needs to stop. Now.

### `!pranked`, `!frankprank`, `!prank`, `!gotem` - YOU MUST BE IN A VOICE CHANNEL

Ask the bot to let your friends know that they just got pranked.

### `!frank`, `!filthyfrank` - YOU MUST BE IN A VOICE CHANNEL

Just some random Filthy Frank audio bites.

### `!memes<(optional) copypasta>`, `!copypasta <(optional) copypasta>`

Displays a random copypasta meme. If you do not add an argument after `!memes/!copypasta` the bot will pick a random meme for you.

Current copypasta meme arguments are:

`Despactio`
`RickandMorty`
`Fortnite`

