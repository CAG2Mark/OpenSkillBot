# OpenSkillBot
![OpenSkillBot Logo](https://raw.githubusercontent.com/CAG2Mark/OpenSkillBot/master/osb.png)
An open source bot for Discord Servers to calculate and store the TrueSkill of players with Challonge integration.

# Status of Features
This bot is in a state that is usable, but things such as data formats may be changed without notice and break existing installations. This is because the bot is still in an early and volatile stage of development. Therefore, use this bot and update it at your own risk.

More stable, production-ready versions will be available soon.

Currently working features:
* Match calculations, for N vs M matches. N1 vs N2 vs N3 vs ... will be implemened later.
* Starting of matches and auto deafening/undeafening of users
* Saving of state between instances
* Leaderboard channel send
* Undo functions
* Player Discord linking
* Player ranks
* Skill decay (currently manually activated)
* Match history channel
* Skill change detection
* Logging of internal errors and events
* Match Insertion
* Tournaments
  * Challonge integration
  * Self-service sign up
* Achievements

Planned features (non-exhaustive):
* Self-service match confirmation

# Prerequisites
To run OpenSkillBot, you will need the following:
* .NET (`dotnet`). To install, see: https://dotnet.microsoft.com/download
 
# Building and Running
Binaries are not yet provided, so you must clone and run the bot yourself.
### Part 1 - Creating and Inviting the Bot on Discord:
1. Go to Discord Developers and create an application: https://discord.com/developers/applications
2. Open the applicaton. Go to "Bot" in the sidebar. Click "Add Bot".
3. Under "Priviliged Gateway Intents", enable "Server Members Intent".
4. Click "Click to Reveal Token". **Save this token; you will need it later.**
5. To invite the bot to your server, click "OAuth2" in the sidebar, and enable `Bot` under `Scopes`. Select the following permissions:
![Permissions](https://i.imgur.com/KZwNSdN.png) Alternatively, the permissions integer is `297872464` if you're into that.
### Part 2 - Running the Bot
1. Make sure .NET Core is installed. It MUST be .NET Core. You can follow instructions here for your OS: https://dotnet.microsoft.com/download
2. Run the following commands (you can also build it using `dotnet build` if you like):
```bash
git clone https://github.com/CAG2Mark/OpenSkillBot
cd OpenSkillBot
dotnet run Program.cs
```
3. Paste in the Discord bot token you wrote down earlier.
4. Now, create all the relevant channels, ranks, and link them all up to the bot using the commands. The bot can also automatically set up all the required channels using `!setupchannels` if you prefer. 

After you use it, you can see that there will be several JSON files created in the directory. These store the bot's data. You can change certain variables in `config.json`, but it is highly recommended that you do not touch any of the other JSON files, as changing them may cause strange behaviour with the bot.

You can use !help to see what commands are available.

**NOTE**: I would *highly* recommend enabling Developer Mode in Discord, as much of the bot relies on Discord IDs to run. You can enable it under `Discord Settings > Appearance > Developer Mode`.
 
# Disclaimer

* This project is not affiliated with Microsoft's Trueskill project in any way, apart from the use of its model.
* This project is not affiliated with Discord (the social platform), Discord.NET, or any other libraries used.
* This project is not affiliated with Challonge.
