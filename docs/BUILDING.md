# 构建与发布

## 一般构建

在 Windows 11 x64 上安装 .NET 10 SDK，然后运行：

```powershell
./build.ps1
```

脚本会还原、以 Release 编译、运行核心测试，并在找到 Python 时校验所有已提交素材。

## 生成便携版和安装包

```powershell
./package.ps1 -Version 1.0.0
```

这会生成单文件、自包含的 `win-x64` 程序，以及便携 ZIP 和 SHA-256。安装 Inno Setup 6 后可再运行：

```powershell
./package.ps1 -Version 1.0.0 -SkipTests -BuildInstaller
```

安装包按当前用户安装到 `%LOCALAPPDATA%\Programs\NaiWaPet`，无需管理员权限。

## 重新生成动画素材

只有修改源视频或抠图算法时才需要这一步：

```powershell
py -3 -m venv tools/.venv
tools/.venv/Scripts/pip install -r tools/requirements-assets.txt
tools/.venv/Scripts/python tools/build_assets.py --source assets/source/Naiwa.mp4
tools/.venv/Scripts/python tools/verify_assets.py
```

生成后必须人工检查 `docs/preview.png`，特别是肚皮、牙齿、手脚边缘和倒地阶段。源文件改变时同时更新 `ASSETS.md` 中的来源与 SHA-256。

## GitHub 发布

推送普通提交或 Pull Request 会运行 `CI`。创建版本标签会自动构建便携版和安装包：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

`release.yml` 会运行测试、构建 Windows x64 发布物、执行单文件冒烟测试、生成哈希，并创建 GitHub Release。
