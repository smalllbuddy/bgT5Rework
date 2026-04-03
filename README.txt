bgT5Launcher v0.3.0 source (0.7-targeted)

Base:
- original 0.7 launcher source

Changes:
- robust local IPv4 detection (not only 192.*)
- writes the fuller 0.7-style bgset.ini layout before launch
- persists GameID and updates host/network sections
- keeps 127.0.0.1 as manual connect UI input while writing the selected local IP to 0.7 fields
- version label bumped to v0.3.0
- removed the debug 'Close!' popup on close

Notes:
- this source does not include game binaries or runtime DLLs
- expected runtime files remain external: BlackOps.exe, BlackOpsMP.exe, bgt5.dll, bgT5lms.dll, steam_api.dll


3.0.0rc2:
- removed compile-time dependency on bgT5lms assembly
- added reflection-based compat wrapper so the source builds even when bgT5lms.dll is only present at runtime


3.0.0rc2 rework:
- robust exe detection by preferred names then matching file size
- hostmode always active
- 127.0.0.1 button and copy-IP buttons
- resizable window, not topmost
- game/server launch goes to tray and returns when process exits
- dedicated launch tries server.cfg then bgserver.cfg
- removed by Die_Rache text, updated labels to Zombies/Campaign


3.0.0rc4 compile fix:
- restored MainWindow.changeIP(string) for SList compatibility
- enabled nullable in the project so the nullable annotations stop producing CS8632 warnings
