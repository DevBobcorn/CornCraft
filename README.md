# CornCraft
A Minecraft client implementation made with Unity.

## > About
__CornCraft__ is a Unity version of Minecraft. It connects to vanilla servers(1.16 to latest 1.19.2) and provides basic graphical, vanilla-like gameplay support. It is based on another open source project, [Minecraft Console Client (MCC)](https://github.com/MCCTeam/Minecraft-Console-Client), and makes heavy use of its cross-version Minecraft protocol implementation. However, unlike MCC itself which now runs on modern .NET, CornCraft is a Unity app/game and runs on Mono/IL2CPP backend, so a large part of the code from MCC has been refactored/rewritten to adapt to Unity's environment.

The code CornCraft uses to parse vanilla resource packs is also open source as a separate project called CornModel, which is available [here](https://github.com/DevBobcorn/CornModel). Check it out if you want to make a Minecraft structure viewer, map editor or something like that with Unity and C#.

## > Building & Running
The project is made and tested with Unity 2021.3.6f1c1, so it is recommended to use this version(or newer) of Unity to build this game. You'll also need to manually prepare resource packs containing and put it under the <code>Resource Packs</code> folder to actually play it on a server. The path of resources should be like <code>\<Your Project Path\>\Resource Packs\vanilla-\<version\>\assets\XXX</code>, and <code>\<version\></code> can be one of <code>1.16.5</code>, <code>1.17.1</code> and <code>1.19.2</code>(Minecraft 1.18.X should also use 1.17.1 resource pack since there's no new block added in 1.18).

## > Server Setup
When setting up a server for CornCraft, it is recommended to set the <code>allow-flight</code> option to <code>true</code> in <code>server.properties</code> file, otherwise it's likely you'll be kicked from the server when moving around(because CornCraft does not and likely will not simulate vanilla player physics, it uses Unity's physics system). 

It is __NOT__ recommended to join a public/commercial server with CornCraft, and CornCraft is __NOT__ responsible for server banning due to using this client!

## > License
Like MCC, CornCraft adopts CDDL-1.0 as the license of its code repository, and this license applies to all source code except those mention their author and license or with specific license attached.

For in-game texts, CornCraft uses [Welai Glow Sans](https://github.com/welai/glow-sans) and [Cascadia Code/Mono](https://github.com/microsoft/cascadia-code), these font files don't fall under CDDL-1.0 and use their own licenses.

More information about CDDL-1.0 can be found on MCC's [home page](https://github.com/MCCTeam/Minecraft-Console-Client), in the license section, and the full license can be reviewed [here](http://opensource.org/licenses/CDDL-1.0).

## > Screenshot
![Screenshot](https://github.com/DevBobcorn/CornCraft/blob/master/Preview.png?raw=true)