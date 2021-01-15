# OpenTrueskillBot
An open source bot for Discord Servers to calculate and store the TrueSkill of players with Challonge integration.

# Status of Features
This bot is currently in very early development. Right now, it is in no way finished nor ready for production.

Currently working features:
* Match calculations, including teams
* Starting of matches and auto deafening/undeafening of users
* Saving of state between instances
* Leaderboard channel send
* Undo functions
* Player Discord linking
* Player ranks
* Match history channel
* Skill change detection
* Logging of internal errors and events
* Match insertion

Planned features (non-exhaustive):
* Challonge integration.
* Achievements.
* Self-service match confirmation.
 
# Building and Running
Binaries are not yet provided, so you must clone and run the bot yourself.
## Part 1 - Creatning and Inviting the Bot in Discord:
1. Go to Discord Developers and create an application: https://discord.com/developers/applications
2. Open the applicaton. Go to "Bot" in the sidebar. Click "Add Bot".
3. Under "Priviliged Gateway Intents", enable "Server Members Intent".
4. Click "Click to Reveal Token". Save this token; you will need it later.
5. To invite the bot to your server, click "OAuth2" in the sidebar, and enable "Bot" under "Scopes". Select the following permissions:
![Permissions](https://i.imgur.com/KZwNSdN.png)
Alternatively, the permissions integer is `297872464` if you're into that.
## Part 2 - Running the Bot
1. Make sure .NET Core is installed. It MUST be .NET Core. You can follow instructins here: https://dotnet.microsoft.com/download
2. Run the following commands:
```bash
git clone https://github.com/CAG2Mark/OpenTrueskillBot
cd OpenTrueskillBot
dotnet run Program.cs
```
3. Paste in the Discord bot token you wrote down earlier.
4. Now, create all the relevant channels, ranks, and link them all up to the bot using the commands. You can use !help to get started.

After you use it, you can see that there will be several JSON files created in the directory. These store the bot's data. You can change certain variables in `BotConfig.json`, but it is highly recommended that you do not touch any of the other JSON files, as changing them may cause strange behaviour with the bot.

**NOTE**: I would *highly* recommend enabling Developer Mode in Discord, as much of the bot relies on Discord IDs to run. You can enable it under `Discord Settings > Appearance > Developer Mode`.
 
# Disclaimer

This project is not affiliated with Microsoft's Trueskill project in any way, apart from the use of its model. 
