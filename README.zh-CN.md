[English](README.md) | 简体中文

# CornCraft
使用Unity开发的Minecraft **Java版**客户端。

## > 关于
**CornCraft**是一个Unity版的MC，可连接到Java版的游戏服务器（支持1.16.2到1.20.4之间所有正式版）并提供基础的游玩功能。

本项目并非意在成为一个“复刻版Minecraft”，而是希望尝试用一种不同的方式重新构想这个游戏，使玩家获得与原版不太一样的游戏体验。

本项目的协议实现基于另一个开源项目[Minecraft Console Client (MCC)](https://github.com/MCCTeam/Minecraft-Console-Client)，并入了其部分代码作为项目中通信系统的基础，以实现跨MC版本的网络通信。然而，由于C#代码后端的差异（MCC使用新版.NET，Unity还在用古早的Mono）以及运行平台的诸多差异，MCC中的许多代码修改或者重写后才能在本项目中使用。

***重要提示！*** **不**推荐使用CornCraft加入公开服/商业服，并且CornCraft**不**对使用此客户端造成的封号负责。目前仍建议自建服务端使用。

CornCraft用来解析原版资源包的代码已独立成一个package并开源到此处：[CraftSharp-Resource](https://github.com/DevBobcorn/CraftSharp-Resource)，如果你想要使用Unity开发结构查看器或者地图编辑工具的话可以参考一下。

## > 服务端搭建
推荐的服务端版本有<code>1.16.5</code>、<code>1.17.1</code>以及<code>1.18.2</code>，纯净服和插件服均可使用。

搭建服务器时，建议将<code>server.properties</code>中的<code>allow-flight</code>选项设置为<code>true</code>，否则可能会在使用此客户端时被踢出游戏（CornCraft没有模拟原版的物理运算，而是使用Unity内置的物理系统）。

对于<code>1.19.1</code>及更高版本的服务器，还需将<code>enforce-secure-profile</code>设置为<code>false</code>以使CornCraft正确登录（Unity里面系统自带的RSA库好像是被阉割了？暂时用不了）。

## > 构建与运行
本项目使用Unity 6000.0.49f1开发测试，建议用此版本或更新的版本打开项目。

**部分第三方资源没有包含在此仓库中**，更多信息请参考[这里](./Assets/Third%20Party%20Assets.zh-CN.md)。

原版游戏资源文件会自动下载，无需手动操作。

## > 控制
大多数基本操作与原版类似，用户暂时无法自己更改，未来会加入到配置选项中。目前支持的操作如下：
键位                                                       | 操作
---                                                       | :---:
<kbd>W</kbd> / <kbd>A</kbd> / <kbd>S</kbd> / <kbd>D</kbd> | 水平移动
<kbd>空格</kbd>                                           | 空中向上移动/跳跃
<kbd>空格</kbd> * 2                                       | 切换飞行（创造模式）
<kbd>左Shift</kbd>                                        | 疾跑/空中向下移动
<kbd>左Ctrl</kbd>                                         | 切换行走/奔跑模式
<kbd>鼠标滚轮</kbd>                                        | 选择快捷栏物品
<kbd>Alt（按下）</kbd> + <kbd>鼠标滚轮</kbd>                | 选择交互选项
<kbd>E</kbd>                                              | 打开玩家物品栏
<kbd>F</kbd>                                              | 交换主副手物品
<kbd>R</kbd>                                              | 切换瞄准模式
<kbd>X</kbd>                                              | 进行交互
<kbd>F3</kbd>                                             | 切换调试信息
<kbd>F3（按下）</kbd> + <kbd>F4</kbd>                      | 显示游戏模式菜单
<kbd>F5（按下）</kbd> + <kbd>C</kbd>                              | 重建区块
<kbd>F5（按下）</kbd> + <kbd>L</kbd>                              | 更新全局光照
<kbd>F6</kbd> / <kbd>F7</kbd> / <kbd>F8</kbd>             | 加载上一个/重载/加载下一个玩家模型
<kbd>F11</kbd>                                            | 切换全屏
<kbd>Shift（按下）</kbd> + <kbd>Tab</kbd>                         | 切换至下一个相机控制器
<kbd>Shift（按下）</kbd> + <kbd>鼠标滚轮</kbd>                     | 调整缩放
<kbd>T</kbd> / <kbd>/</kbd>                               | 显示聊天界面/输入命令
<kbd>P</kbd>                                              | 显示网络数据包界面
<kbd>Tab</kbd>                                            | 命令自动补全
<kbd>Esc</kbd>                                            | 暂停游戏

## > 开源协议
CornCraft在CDDL-1.0协议下开源，与MCC保持一致。除非特殊说明的部分，此协议对本仓库所有代码生效。

本项目使用了部分其他开源项目/代码示例，这些项目不受CDDL-1.0约束，而使用其自身指明的开源协议。列表如下：
* [Welai Glow Sans](https://github.com/welai/glow-sans): 作为常规游戏界面中使用的字体。
* [Cascadia Code/Mono](https://github.com/microsoft/cascadia-code): 作为命令、技术性信息中使用的字体
* [Star Rail NPR Shader](https://github.com/stalomeow/StarRailNPRShader): 作为自定义玩家模型使用的着色器
* [Minecraft Data](https://github.com/PrismarineJS/minecraft-data): 提供网络数据包预览界面中解析器使用的包体结构定义

更多关于CDDL-1.0的信息可在MCC的[主页](https://github.com/MCCTeam/Minecraft-Console-Client)上查看，协议全文请看[这里](./LICENSE.md)。

## > 截图
![CornCraft028.png](https://s2.loli.net/2025/06/04/hHWmExPiLkJs1a9.png)
![CornCraft021.png](https://s2.loli.net/2024/10/28/kas4ZD8cgrfb6xn.png)
![CornCraft020.png](https://s2.loli.net/2024/10/28/xFVCbJNwH6qAZ2E.png)