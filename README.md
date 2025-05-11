English | [简体中文](README.zh-CN.md)

# CornCraft
A Minecraft **Java Edition** client implementation made with Unity.

## > About
**CornCraft** is a Unity version of Minecraft. It connects to Java Edition servers(version 1.16.2 to 1.20.4) and provides basic graphical gameplay support.

This project does not mean to be a Minecraft "clone", but rather an attempt to recreate the game in a somewhat different way, with the overall look and feel distinct from the original one.

The protocol implementation is based on another open source project, [Minecraft Console Client (MCC)](https://github.com/MCCTeam/Minecraft-Console-Client), by incorporating code from this project as a foundation for cross-version networking with Minecraft servers. However, due to different runtime backends(Mono for Unity vs .NET for MCC) and other major parity issues regarding input and rendering, it is not an option to directly use MCC code as a library despite the fact that they're both in C#, and a number of tweaks and rewrites were needed for everything to work fine in this new framework. Currently there's no pre-built binaries available yet because some core features are still missing, but feel free to give it a try in the editor!

***Important!*** It is **NOT** recommended to join a public/commercial server with CornCraft, and CornCraft is **NOT** responsible for server banning due to using this client! The best and safest way to play with this client, at least for now, is to set up a local server and play it with your friends.

The code CornCraft uses to parse vanilla resource packs is also open source as a Unity package called [CraftSharp-Resource](https://github.com/DevBobcorn/CraftSharp-Resource). Check it out if you want to make a Minecraft structure viewer, map editor or something like that with Unity.

## > Server Setup
Recommended server versions for CornCraft are <code>1.16.5</code>, <code>1.17.1</code> and <code>1.18.2</code>, both vanilla servers and plugin servers like Spigot are supported.

When setting up a server for CornCraft, it is recommended to set the <code>allow-flight</code> option to <code>true</code> in <code>server.properties</code> file, otherwise it's likely you'll be kicked from the server when moving around(because CornCraft does not and likely will not simulate vanilla player physics, it uses Unity's physics system).

For <code>1.19.1</code> or higher servers, it is also necessary to set <code>enforce-secure-profile</code> to <code>false</code> so that CornCraft can log in correctly.

## > Building & Running
The project is made and tested with Unity 6000.0.39f1, so it is recommended to use this(or a newer) version of Unity to build this game.

**Some third-party assets are not included in this repo**, for more information please see [here](./Assets/Third%20Party%20Assets.md).

Vanilla resource files will now be automatically downloaded if not present, and manual preparation is no longer necessary.

## > Controls
Most basic controls in CornCraft are similar to vanilla Java Edition. These are hard-coded for now, but will be configurable in the future. Here's a list of currently supported actions:
Key                                                       | Action
---                                                       | :---:
<kbd>W</kbd> / <kbd>A</kbd> / <kbd>S</kbd> / <kbd>D</kbd> | Move Horizontally
<kbd>Space</kbd>                                          | Move Up in Air (Jump if Grounded)
<kbd>Left Shift</kbd>                                     | Move Down in Air
<kbd>Left Ctrl</kbd>                                      | Toggle Walk/Rush Mode
<kbd>Mouse Scroll</kbd>                                   | Select Hotbar Slot or Interaction Option (if more than 1)
<kbd>Mouse Scroll</kbd> + <kbd>Alt</kbd>                  | Select Hotbar Slot
<kbd>E</kbd>                                              | Open Player Inventory
<kbd>F</kbd>                                              | Swap Items on Hands
<kbd>R</kbd>                                              | Toggle Camera Focus Lock (if in Grounded State)
<kbd>X</kbd>                                              | Perform Interaction
<kbd>F3</kbd>                                             | Toggle Debug Info
<kbd>F3</kbd> + <kbd>F4</kbd> (Hold)                      | Show Game Mode Switch
<kbd>F5</kbd> + <kbd>C</kbd>                              | Rebuild Chunks
<kbd>F5</kbd> + <kbd>L</kbd>                              | Update Global Illumination
<kbd>F6</kbd> / <kbd>F7</kbd> / <kbd>F8</kbd>             | Load Previous/Reload/Load Next Player Model
<kbd>F11</kbd>                                            | Toggle Fullscreen
<kbd>Shift</kbd> + <kbd>Tab</kbd>                         | Switch to Next Camera Controller
<kbd>Shift</kbd> + <kbd>Mouse Scroll</kbd>                | Adjust Camera Zoom
<kbd>T</kbd> / <kbd>/</kbd>                               | Show Chat Screen/Input Command
<kbd>P</kbd>                                              | Show Packet Inspector Screen
<kbd>Tab</kbd>                                            | Command Auto-Completion
<kbd>Esc</kbd>                                            | Pause Game

## > License
Like MCC, CornCraft adopts CDDL-1.0 as the license of its code repository, and this license applies to all source code except those mentioning their license or with custom license attached.

Some other open-source projects/code examples are used in the project, which don't fall under CDDL-1.0 and use their own licenses. Here's a list of them:
* [Welai Glow Sans](https://github.com/welai/glow-sans): Used as font for regular game UI
* [Cascadia Code/Mono](https://github.com/microsoft/cascadia-code): Used as font for command input/technical information
* [Star Rail NPR Shader](https://github.com/stalomeow/StarRailNPRShader): Anime character shaders for custom player model rendering
* [Minecraft Data](https://github.com/PrismarineJS/minecraft-data): Protocol data for packet inspection

More information about CDDL-1.0 can be found on MCC's [home page](https://github.com/MCCTeam/Minecraft-Console-Client), in the license section, and the full license can be viewed [here](./LICENSE.md).

## > Screenshots
![CornCraft025.png](https://s2.loli.net/2025/05/10/VxAEyIzS7gU9Ywf.png)
![CornCraft021.png](https://s2.loli.net/2024/10/28/kas4ZD8cgrfb6xn.png)
![CornCraft020.png](https://s2.loli.net/2024/10/28/xFVCbJNwH6qAZ2E.png)