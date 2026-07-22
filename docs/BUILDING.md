# 构建与发布

## 固定工具版本

正式构建使用 Windows 11 x64、.NET 10 SDK 10.0.302 和 .NET/WPF 运行时 10.0.10。`global.json` 禁止 SDK 自动前滚；运行时版本和应用版本统一记录在 `Directory.Build.props`。

## 一般构建

在 Windows 11 x64 上运行：

```powershell
./build.ps1
```

脚本会还原依赖、以 Release 配置编译、运行核心测试，并在找到 Python 时检查发布元数据、Markdown 本地链接和全部已提交素材。项目启用推荐级 .NET 分析器、NuGet 安全审计和 warnings-as-errors。

## 生成并验证发布物

```powershell
./package.ps1
```

打包脚本从 `Directory.Build.props` 读取版本，不接受与源码版本不同的外部版本。它会生成以下四个文件：

- `NaiWaPet-1.0.1-win-x64.exe`
- `NaiWaPet-1.0.1-win-x64.exe.sha256`
- `NaiWaPet-1.0.1-win-x64-portable.zip`
- `NaiWaPet-1.0.1-win-x64-portable.zip.sha256`

`verify-package.ps1` 会执行程序冒烟测试、核对 Windows 文件版本和两个 SHA-256 文件，并检查 ZIP 文件清单、独立 EXE 与 ZIP 内 EXE 的字节一致性、`LICENSE` 的 LF 换行及全部运行时许可文件。显式传入 `./package.ps1 -Version 1.0.1` 仅用于额外断言，不能覆盖源码版本。

## 重新生成动画素材

只有修改源视频或抠图算法时才需要这一步：

```powershell
py -3 -m venv tools/.venv
tools/.venv/Scripts/pip install -r tools/requirements-assets.txt
tools/.venv/Scripts/python tools/build_assets.py --source assets/source/Naiwa.mp4
tools/.venv/Scripts/python tools/verify_assets.py
```

源视频必须放在项目目录内，以便生成可移植的相对路径。生成脚本会把实际源文件路径和 SHA-256 写入动画清单。生成后必须人工检查 `docs/preview.png`，特别是肚皮、牙齿、手脚边缘和倒地阶段。源文件改变时还需同步更新 `ASSETS.md` 中供使用者查阅的来源与许可信息。

`tools/verify_assets.py` 不依赖第三方 Python 包，可单独校验清单、图集集合、点击掩码、音频、预览和源视频哈希。

## GitHub 发布

普通提交和 Pull Request 运行 `CI`。只有 CI 与 CodeQL 均通过、分支保持最新且对话已解决时，变更才能通过 Pull Request 合并到 `main`。

发布前需要完成以下准备：

1. `main` 的 CI 和 CodeQL 通过，工作区没有未提交改动。
2. `Directory.Build.props`、README、CHANGELOG 和 `docs/releases/版本号.md` 已同步。
3. 仓库的 Release Immutability 保持启用。
4. 创建指向当前 `main` 的版本标签并推送，例如 `v1.0.1`。

Release 工作流首先在不检出源码的情况下验证标签格式、标签提交和当前 `main`；随后在只读权限任务中检出 `refs/tags/...`、构建并完整验证发布包；最后在独立的最小写权限任务中创建草稿、上传并重新下载比对四个文件。全部一致后才公开 Release。

已经存在的 Release（包括草稿）不会被覆盖。发布成功后，工作流要求 GitHub 返回 `immutable=true`，并使用 `gh release verify` 和 `gh release verify-asset` 验证发布证明。`v1.0.0` 是启用不可变发布前的历史版本，不应修改或重新发布。
