# Playerdom
Proof-of-concept multiplayer RPG with advanced NPC AI and randomly-generated worlds.
Stage: Classic

**An overview of each project in the solution:

**Playerdom.Server**: .NET Core command that runs the multiplayer game server on the host machine at port 25565 (arbitrarily set to Minecraft Java Edition default's port for the time being). Logs debug info on the console until the host user terminates it by pressing a key

**Playerdom.UWP/Playerdom.Win32**: Contains platform-specific startup code for the Universal Windows Platform, and classic Win32 applications, respectively

**Playerdom.Shared**: A MonoGame project containing the majority of code used to run on the client and the server. Platform-specific code is seperated and substituted by preprocessor directives

**PngToPldms**: A .NET Core command that converts PNG schematics into the Playerdom Structure format (pldms), a custom file type that can be loaded into the game when generating worlds

----

**Getting build errors about missing freetype6.dll?**: You're missing Visual Studio Redist 2012. Download here: https://www.microsoft.com/en-us/download/details.aspx?id=30679

This project is built on MonoGame. You may need to download and install it to build: http://www.monogame.net/downloads/


----

**How do I host a server?!?!**:
Open command prompt to ..\Playerdom-master\Playerdom.Server.Core\bin\Release\netcoreapp2.2\
Type in `dotnet.exe .\Playerdom.Server.Core.dll`
If you see "Playerdom Test Server", you have started the server on port 25565.

**Why does nothing happen when I run the executable?!?!**:
You need a valid server to connect to otherwise it will throw an internal exception and quit. See above. Then put the IP address in `connection.txt`. Do not put the port. It is configured to connect to the IP via 25565 by default.

=======
Welcome to Playerdom!

Disclaimer: This is early in development, and is primarily a side project. Pull Requests with bug fixes and efficiency upgrades are always welcome! If you wish to become more involved and develop new featrues, graphics, content, algorithms, etc, get ahold of @Dylan-P-Green and open an Issue
