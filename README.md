# LCEAuth
LCEAuth is an up-and-coming server security plugin designed to protect your users.
It is a per-player password system that guards user data while simultaneously preventing bots.

## Setup
To setup LCEAuth, you must make a new [FourKit Minecraft server](https://github.com/sylvessa/MinecraftConsoles/releases/tag/nightly-dedicated-server)
Download the DLL release under releases and put it in the plugins folder.
Dependencies include BCrypt and LiteDB, which also need to be put in the plugins folder. This can and will change in the future.

That's it.

## Build
To build LCEAuth, get the FourKit dll, BCrypt dll (included with release), and LiteDB dll (also included) and put them one directory above the LCEAuth folder.
Run this command:
```bash
dotnet build LCEAuth.csproj
```

The build will appear in bin/Debug/net8.0
Copy the LCEAuth.dll, BCrypt.Net-Next.dll, and LiteDB.dll and paste them all in the plugins folder.
<img width="732" height="188" alt="image" src="https://github.com/user-attachments/assets/cb1d4106-6417-4a3d-a277-295756077e2d" />

## Admin
To utilize admin privileges for cases such as account recovery (uid stolen before account creation, etc.) and password changing, the server owner can go into the console and input /authadmin.
Current commands with authadmin:
```bash
authadmin recover <username>
```
^ Randomly generates a secure password for the affected user to use
```bash
authadmin changepass <username> <newpassword>
```
^ Changed the password of a user to a new one. Useful for if the password is compromised or unsecure.

### THIS IS A WORK IN PROGRESS, THERE MAY BE BUGS OR SECURITY VULNERABILITIES. IF YOU FIND A VULNERABILITY, FOLLOW THE INSTRUCTIONS IN SECURITY.MD
