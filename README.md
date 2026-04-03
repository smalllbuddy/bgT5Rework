bgT5LauncherReworked v0.3.2

# What is it?

This is a reworked version of the bgt5launcher that officially supports bgt5launcher versions 0.1.1 and 0.7, with in between versions most likely also working.

# Why?

Version 0.1.1 had a connection/playerdata issues that were fixed in 0.7, however 0.7 introduced far worse issues that made it not worth using.

This launcher fixes all 0.7 exclusive issues, and all issues that weren't fixed with 0.7.

# How?

Just drag the exe file into either your 0.7 bgt5 black ops folder (recommended) or your 0.1.1 bgt5 black ops folder. This will replace one file, and you can launch everything using the new exe file.

# Fixes

-127.0.0.1 no longer crashes on multiplayer launch
-Clicking launcher no longer crashes with "Close!" error
-Dedicated server can use bgserver.cfg or server.cfg
-PlayerID is consistent with username unless overwritten
-Launch options (hover over version # to see)
-Hostmode always activates
-Resizable window
-Improved UI
-Minimizes to system tray
-And much more

# Bonus Features

You can add a Template folder in BlackOpsRootDirectory/bgData/ and include:
CompressedMetPlayer3.info_ID
globalstatsCompressed_ID
mpstatsBasicTraining_ID
mpstatsCompressed_ID
recentservers.dat_ID
spstatsCompressed_ID

And it will replace ID with your PlayerID (aka GameID) and add that to your profile each time you use a new ID. Make sure your files are named with "ID" at the end instead of your actual ID. This way, you no longer have to join a dedicated server or reload your data each time.

<img width="621" height="331" alt="image" src="https://github.com/user-attachments/assets/246bf4f3-482e-49aa-a88d-65620fae2cc2" />

# Changelog

0.3.2: Support for 0.7, improved UI, gameID support, fixed 127.0.0.1 support, added template system, etc.

0.2.2: Initial framework with an orange UI design.
