bgT5LauncherReworked v0.4.4

# What is it?

This is a reworked version of the bgt5launcher that officially supports bgt5launcher versions 0.1.1 and 0.7, with in between versions most likely also working.

# Why?

Version 0.1.1 had a connection/playerdata issues that were fixed in 0.7, however 0.7 introduced far worse issues that made it not worth using.

This launcher fixes all 0.7 exclusive issues, and all issues that weren't fixed with 0.7.

# How?

Just drag the exe file (from [Releases](https://github.com/smalllbuddy/bgT5Rework/releases)) into either your 0.7 bgt5 black ops folder (recommended) or your 0.1.1 bgt5 black ops folder. This will replace one file, and you can launch everything using the new exe file.

# Bonus Features

You can add a Template folder in BlackOpsRootDirectory/bgData/ (0.7.0)
or BlackOpsRootDirectory/Profiles/ (0.1.1)
and include:
CompressedMetPlayer3.info_ID
globalstatsCompressed_ID
mpstatsBasicTraining_ID
mpstatsCompressed_ID
recentservers.dat_ID
spstatsCompressed_ID

And it will replace ID with your PlayerID (aka GameID) and add that to your profile each time you use a new ID. Make sure your files are named with "ID" at the end instead of your actual ID. This way, you no longer have to join a dedicated server or reload your data each time.

<img width="621" height="331" alt="image" src="https://github.com/user-attachments/assets/246bf4f3-482e-49aa-a88d-65620fae2cc2" />

# Build Instructions

Copy `bgT5lms.dll` from your own bgt5launcher 0.7 install into a `lib` folder next to `bgT5Launcher.csproj`. Open `bgT5Launcher.csproj`, select `Release` / `x86`, and build.

# Credits

bgT5Launcher was made by BGamer (© 2013–2018). This is an unofficial rework of it and isn't affiliated with BGamer. `bgT5lms.dll` belongs to BGamer and isn't included here.

# Changelog

0.4.4: Fixes the `bgData/Friends` handle leak that caused progressive multiplayer hitches.

0.4.3: Optimized launcher appearance
  
0.4.2: Hitch fix attempt - broke LAN connection

0.4.1: Final patch with tested working friends list etc. Strongly recommend using bgt5launcher 0.7. Have fun playing :)

0.3.3: Patches for 0.7, fixed bugs, 0.1.1 now supports Template folder

0.3.2: Support for 0.7, improved UI, gameID support, fixed 127.0.0.1 support, added template system, etc.

0.2.2: Initial framework with an orange UI design.
