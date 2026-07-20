# 奶蛙桌宠（NaiWaPet）

NaiWaPet 是一款面向 Windows 11 x64 的透明桌面宠物。单击奶蛙即可完整播放约 15.4 秒的“奶蛙捧腹大笑”动作：拍肚子、扶头、后仰、笑倒在地，再恢复站立。程序默认播放原视频笑声，可随时在托盘或设置中关闭。

![奶蛙捧腹大笑透明预览](docs/preview.png)

## 功能特性

- 从完整参考视频生成 462 帧、30 FPS 透明动画。
- 无白色窗口、无视频矩形背景；每帧具有独立透明通道和像素级点击区域。
- 单文件、自包含的 Windows x64 程序，无需预装 .NET 运行时。
- 原笑声音轨完整保留，第一次启动默认开启，也可一键关闭。
- 默认显示为 80% 大小，并可用鼠标滚轮或设置滑块在 50%～180% 之间调整。
- 拖动移动、快速甩动弹跳、滚轮缩放、偶尔蹦动、始终置顶、鼠标穿透。
- 托盘菜单、设置窗口、单实例、位置记忆、开机启动。
- 便携 ZIP、Inno Setup 安装包、SHA-256 校验文件和 GitHub Actions 自动发布。

## 下载和使用

在 GitHub 仓库的 **Releases** 页面下载：

- `NaiWaPet-版本-win-x64-setup.exe`：推荐，安装到当前用户目录，不需要管理员权限。
- `NaiWaPet-版本-win-x64-portable.zip`：解压后直接运行 `NaiWaPet.exe`。

目前发布物未做商业代码签名，Windows SmartScreen 可能在第一次运行时显示“未知发布者”。请从本项目 Releases 下载，并用同名 `.sha256` 文件核对哈希。

## 操作

| 操作 | 效果 |
| --- | --- |
| 左键单击奶蛙 | 播放完整捧腹大笑动画 |
| 左键拖动 | 移动奶蛙 |
| 快速拖动后松开 | 甩出并产生重力、碰撞和弹跳 |
| 鼠标滚轮 | 缩放奶蛙 |
| 右键奶蛙 | 打开快捷菜单 |
| 双击托盘图标 | 显示或隐藏奶蛙 |

开启“鼠标穿透”后，奶蛙不会阻挡下方窗口；此时请通过任务栏右下角的奶蛙托盘图标恢复。

## 设计与参考

项目设计参考了以下公开项目的交互方式与工程实践：

- [CHENGONGSHUO/Naiwa](https://github.com/CHENGONGSHUO/Naiwa)：参考视频、素材来源与基础 WinUI 3 播放示例。
- [ayangweb/BongoCat](https://github.com/ayangweb/BongoCat)：托盘交互与桌面应用发布体验。
- [liwenka1/bongo-cat-next](https://github.com/liwenka1/bongo-cat-next)：动画资源与桌宠逻辑分层。
- [gil/shimeji-ee](https://github.com/gil/shimeji-ee)：行为式桌宠、漫游与物理反馈。
- [KurtVelasco/Desktop_Gremlin](https://github.com/KurtVelasco/Desktop_Gremlin)：WPF 透明桌宠实现方式。

应用代码使用 C# / WPF 独立实现。动画素材来源与处理过程详见 [ASSETS.md](ASSETS.md)。

## 本地构建

要求：

- Windows 11 x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 可选：Inno Setup 6（生成安装包）

```powershell
./build.ps1
./package.ps1 -Version 1.0.0
./package.ps1 -Version 1.0.0 -SkipTests -BuildInstaller
```

发布程序位于 `artifacts/`。更完整的说明见 [docs/BUILDING.md](docs/BUILDING.md)。

## 项目结构

```text
src/NaiWaPet.Core/       与 UI 无关的动画元数据、设置和物理逻辑
src/NaiWaPet/            WPF 桌宠、托盘、设置、Windows 互操作
src/NaiWaPet/Assets/     已生成的运行时透明动画和音频
tests/                   无第三方测试框架的核心逻辑测试
tools/                   素材生成与完整性校验
assets/source/           可复现动画的源视频
installer/               Inno Setup 安装脚本
.github/workflows/       Windows CI 和 GitHub Release
```

技术设计见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。

## 开源许可

本项目整体按照 [GNU GPL v3](LICENSE) 发布。素材归属、来源哈希和修改说明见 [ASSETS.md](ASSETS.md) 与 [NOTICE.md](NOTICE.md)。

欢迎提交 Issue 和 Pull Request；参与开发前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。
