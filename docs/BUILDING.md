# 构建与发布

## 一般构建

在 Windows 11 x64 上安装 .NET 10 SDK，然后运行：

```powershell
./build.ps1
```

脚本会还原、以 Release 编译、运行核心测试，并在找到 Python 时校验所有已提交素材。

## 生成便携发布物

```powershell
./package.ps1 -Version 1.0.0
```

版本号必须符合语义化版本格式，例如 `1.0.0` 或 `1.0.0-beta.1`。脚本会同步设置包版本、程序集版本、文件版本和信息版本，并生成可直接运行的单文件、自包含 `win-x64` 程序、便携 ZIP，以及对应的 SHA-256 校验文件。当前正式版本保持为 `1.0.0`。

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

推送普通提交或 Pull Request 会运行 `CI`。创建版本标签会自动构建单文件 EXE 和便携 ZIP：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

`release.yml` 会运行测试、构建 Windows x64 发布物、核对程序文件版本、执行单文件冒烟测试、生成哈希，并创建 GitHub Release。

普通 CI 也可在 GitHub Actions 页面手动运行。同一分支有新提交时，旧的未完成 CI 会自动取消；单次任务最多运行 30 分钟。
