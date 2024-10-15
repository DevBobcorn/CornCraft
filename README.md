# CornCraft
A Minecraft client implementation made with Unity.

## > About
__CornCraft__ is a Unity version of Minecraft. It connects to vanilla servers(version 1.16.2 to 1.19.4) and provides basic graphical gameplay support. It is based on another open source project, [Minecraft Console Client (MCC)](https://github.com/MCCTeam/Minecraft-Console-Client), and makes heavy use of its cross-version Minecraft protocol implementation.

However, unlike MCC itself which runs on modern .NET, CornCraft is a Unity app/game and runs on Mono/IL2CPP backend, so a large part of the code from MCC has been refactored or rewritten to adapt to Unity's environment.

__*Important!*__ It is __NOT__ recommended to join a public/commercial server with CornCraft, and CornCraft is __NOT__ responsible for server banning due to using this client! The best and safest way to play with this client, at least for now, is to set up a server by yourself or with your friends.

The code CornCraft uses to parse vanilla resource packs is also open source as a Unity package called [CraftSharp-Resource](https://github.com/DevBobcorn/CraftSharp-Resource). Check it out if you want to make a Minecraft structure viewer, map editor or something like that with Unity.

## > Server Setup
Recommended server versions for CornCraft are <code>1.16.5</code>, <code>1.17.1</code> and <code>1.18.2</code>, both vanilla servers and plugin servers like Spigot are supported.

When setting up a server for CornCraft, it is recommended to set the <code>allow-flight</code> option to <code>true</code> in <code>server.properties</code> file, otherwise it's likely you'll be kicked from the server when moving around(because CornCraft does not and likely will not simulate vanilla player physics, it uses Unity's physics system).

For <code>1.19.1</code> or higher servers, it is also necessary to set <code>enforce-secure-profile</code> to <code>false</code> so that CornCraft can log in correctly.

## > Building & Running
The project is made and tested with Unity 2022.3.50f1, so it is recommended to use this version(or newer) of Unity to build this game.

**Some third-party assets are not included in this repo**, for more information please see [here](./Assets/Third%20Party%20Assets.md).

Resource files will now be automatically downloaded if they're not present, so manual downloading is no longer necessary.

## > Controls
Most basic controls in CornCraft are similar to vanilla Java Edition. They're hard-coded for now, but will be configurable in the future. Here's a list of currently supported actions:
Key                                                       | Action
---                                                       | :---:
<kbd>W</kbd> / <kbd>A</kbd> / <kbd>S</kbd> / <kbd>D</kbd> | Move Horizontally
<kbd>Space</kbd>                                          | Move Up in Air (Jump if Grounded)
<kbd>Left Shift</kbd>                                     | Move Down in Air
<kbd>Left Ctrl</kbd>                                      | Toggle Walk/Rush Mode
<kbd>F</kbd>                                              | Perform Interaction
<kbd>F3</kbd>                                             | Toggle Debug Info
<kbd>F3</kbd> + <kbd>F4</kbd> (Hold)                      | Show Game Mode Switch
<kbd>F5</kbd> + <kbd>C</kbd>                              | Rebuild Chunks
<kbd>F5</kbd> + <kbd>L</kbd>                              | Update Global Illumination
<kbd>F6</kbd> / <kbd>F7</kbd> / <kbd>F8</kbd>             | Load Previous/Reload/Load Next Player Model
<kbd>F11</kbd>                                            | Toggle Fullscreen
<kbd>T</kbd> / <kbd>/</kbd>                               | Show Chat Screen/Input Command
<kbd>Tab</kbd>                                            | Command Auto-Completion
<kbd>Esc</kbd>                                            | Pause Game

## > License
Like MCC, CornCraft adopts CDDL-1.0 as the license of its code repository, and this license applies to all source code except those mention their author and license or with specific license attached.

Some other open-source projects/code examples are used in the project, which don't fall under CDDL-1.0 and use their own licenses. Here's a list of them:
* [Welai Glow Sans](https://github.com/welai/glow-sans)
* [Cascadia Code/Mono](https://github.com/microsoft/cascadia-code)
* [Star Rail NPR Shader](https://github.com/stalomeow/StarRailNPRShader)
* [Minecraft Data](https://github.com/PrismarineJS/minecraft-data)

More information about CDDL-1.0 can be found on MCC's [home page](https://github.com/MCCTeam/Minecraft-Console-Client), in the license section, and the full license can be reviewed [here](http://opensource.org/licenses/CDDL-1.0).

## > Screenshots
![Screenshot 1](https://s2.loli.net/2024/10/15/yNopOxURMAjWhdQ.png)
![Screenshot 2](https://s2.loli.net/2022/10/24/pLfmGiEbBOqFzTZ.png)
![Screenshot 3](https://s2.loli.net/2022/10/25/RSZK3FbOdHXkanm.png)