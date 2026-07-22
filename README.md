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
- 托盘菜单、设置窗口、内置开源许可、单实例、位置记忆、开机启动。
- 可直接运行的单文件 EXE、便携 ZIP、SHA-256 校验文件和 GitHub Actions 自动发布。

## 下载和使用

请从 [NaiWaPet 1.0.1 发布页面](https://github.com/dcjaychou-design/NaiWaPet/releases/tag/v1.0.1) 下载：

- `NaiWaPet-1.0.1-win-x64.exe`：推荐，下载后直接双击运行，无需安装。
- `NaiWaPet-1.0.1-win-x64.exe.sha256`：独立 EXE 的 SHA-256 校验文件。
- `NaiWaPet-1.0.1-win-x64-portable.zip`：压缩包版本，解压后运行 `NaiWaPet.exe`。
- `NaiWaPet-1.0.1-win-x64-portable.zip.sha256`：便携 ZIP 的 SHA-256 校验文件。

当前应用版本为 **1.0.1**，支持 Windows 11 x64。

目前发布物未做商业代码签名，Windows SmartScreen 可能在第一次运行时显示“未知发布者”。请只从上述发布页面下载，并用同名 `.sha256` 文件核对哈希。发布页面中的 EXE、ZIP、校验文件和说明均由 `v1.0.1` 标签对应的同一份源码自动生成；发布后标签和文件不可替换，并可使用 GitHub CLI 的 `gh release verify v1.0.1` 验证发布证明。

## 操作

| 操作 | 效果 |
| --- | --- |
| 左键单击奶蛙 | 播放完整捧腹大笑动画 |
| 左键拖动 | 移动奶蛙 |
| 快速拖动后松开 | 甩出并产生重力、碰撞和弹跳 |
| 鼠标滚轮 | 缩放奶蛙 |
| 右键奶蛙 | 打开快捷菜单 |
| 双击托盘图标 | 显示或隐藏奶蛙 |
| 托盘或设置中的“关于与开源许可” | 查看程序版本、项目许可及随附运行时许可 |

开启“鼠标穿透”后，奶蛙不会阻挡下方窗口；此时请通过任务栏右下角的奶蛙托盘图标恢复。

## 数据与隐私

NaiWaPet 不连接网络，不收集遥测，也不上传任何使用数据。设置保存在 `%LOCALAPPDATA%\NaiWaPet\settings.json`；发生未处理错误时，诊断日志保存在 `%LOCALAPPDATA%\NaiWaPet\Logs`。启用开机启动后，程序只会写入当前用户的 Windows `Run` 注册表项，不需要管理员权限。

如果移动了程序文件，请在设置中重新启用“开机启动”，以更新保存的程序路径。

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

```powershell
./build.ps1
./package.ps1
```

发布程序位于 `artifacts/`。应用版本统一读取自 `Directory.Build.props`；如显式传入 `-Version`，必须与源码版本完全一致。更完整的说明见 [docs/BUILDING.md](docs/BUILDING.md)。

## 项目结构

```text
src/NaiWaPet.Core/       与 UI 无关的动画元数据、设置和物理逻辑
src/NaiWaPet/            WPF 桌宠、托盘、设置、Windows 互操作
src/NaiWaPet/Assets/     已生成的运行时透明动画和音频
tests/                   无第三方测试框架的核心逻辑测试
tools/                   素材生成与完整性校验
assets/source/           可复现动画的源视频
.github/workflows/       Windows CI 和 GitHub Release
```

技术设计见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。

## 开源许可

本项目整体按照 [GNU GPL v3](LICENSE) 发布。素材归属、来源哈希和修改说明见 [ASSETS.md](ASSETS.md) 与 [NOTICE.md](NOTICE.md)。便携 ZIP 和程序内的“关于与开源许可”还包含自包含 .NET/WPF 运行时的完整官方许可与第三方声明。

欢迎提交 Issue 和 Pull Request；参与开发前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。
