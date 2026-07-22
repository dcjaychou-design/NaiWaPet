[CmdletBinding()]
param(
    [string]$Version,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
[xml]$BuildProperties = Get-Content (Join-Path $ProjectRoot "Directory.Build.props") -Raw
$SourceVersion = [string]$BuildProperties.Project.PropertyGroup.Version
$FileVersion = [string]$BuildProperties.Project.PropertyGroup.FileVersion
$RuntimeVersion = [string]$BuildProperties.Project.PropertyGroup.PinnedRuntimeVersion

$VersionPattern = '^(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$'
if ($SourceVersion -notmatch $VersionPattern) {
    throw "Directory.Build.props 中的版本号无效：$SourceVersion"
}
if ($PSBoundParameters.ContainsKey("Version") -and $Version -ne $SourceVersion) {
    throw "传入版本 $Version 与源码版本 $SourceVersion 不一致。请先更新 Directory.Build.props。"
}
$Version = $SourceVersion
if ($FileVersion -ne "$($Version.Split('-')[0].Split('+')[0]).0") {
    throw "文件版本 $FileVersion 与应用版本 $Version 不一致。"
}
if ($RuntimeVersion -ne "10.0.10") {
    throw "当前发布必须固定使用 .NET 运行时 10.0.10，实际为 $RuntimeVersion。"
}

$Artifacts = Join-Path $ProjectRoot "artifacts"
$PublishDirectory = Join-Path $Artifacts "publish"
$DirectExecutable = Join-Path $Artifacts "NaiWaPet-$Version-win-x64.exe"
$PortableDirectory = Join-Path $Artifacts "NaiWaPet-$Version-win-x64"
$PortableZip = Join-Path $Artifacts "NaiWaPet-$Version-win-x64-portable.zip"

if (-not $SkipTests) {
    & (Join-Path $ProjectRoot "build.ps1") -Configuration Release
}

foreach ($Path in @(
    $PublishDirectory,
    $PortableDirectory,
    $PortableZip,
    "$PortableZip.sha256",
    $DirectExecutable,
    "$DirectExecutable.sha256"
)) {
    if (Test-Path $Path) {
        Remove-Item $Path -Recurse -Force
    }
}

dotnet publish (Join-Path $ProjectRoot "src/NaiWaPet/NaiWaPet.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $PublishDirectory `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败，退出码：$LASTEXITCODE"
}
$AssetsFile = Join-Path $ProjectRoot "src/NaiWaPet/obj/project.assets.json"
$ResolvedAssets = Get-Content $AssetsFile -Raw | ConvertFrom-Json -AsHashtable
foreach ($RuntimePack in @(
    "Microsoft.NETCore.App.Runtime.win-x64/$RuntimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.win-x64/$RuntimeVersion"
)) {
    if (-not $ResolvedAssets.libraries.ContainsKey($RuntimePack)) {
        throw "发布未使用固定运行时包：$RuntimePack"
    }
}

& (Join-Path $ProjectRoot "verify-windows.ps1") `
    -Executable "artifacts/publish/NaiWaPet.exe" `
    -ExpectedFileVersion $FileVersion `
    -ExpectedProductVersion $Version

Copy-Item (Join-Path $PublishDirectory "NaiWaPet.exe") $DirectExecutable
New-Item -ItemType Directory -Path $PortableDirectory -Force | Out-Null

$PortableFiles = @(
    @{ Source = (Join-Path $PublishDirectory "NaiWaPet.exe"); Name = "NaiWaPet.exe" }
    @{ Source = (Join-Path $ProjectRoot "README.md"); Name = "README.md" }
    @{ Source = (Join-Path $ProjectRoot "LICENSE"); Name = "LICENSE" }
    @{ Source = (Join-Path $ProjectRoot "NOTICE.md"); Name = "NOTICE.md" }
    @{ Source = (Join-Path $ProjectRoot "ASSETS.md"); Name = "ASSETS.md" }
    @{ Source = (Join-Path $ProjectRoot "THIRD_PARTY_NOTICES.md"); Name = "THIRD_PARTY_NOTICES.md" }
    @{ Source = (Join-Path $ProjectRoot "licenses/dotnet/10.0.10/LICENSE.txt"); Name = "DOTNET-LICENSE.txt" }
    @{ Source = (Join-Path $ProjectRoot "licenses/dotnet/10.0.10/THIRD-PARTY-NOTICES.txt"); Name = "DOTNET-THIRD-PARTY-NOTICES.txt" }
    @{ Source = (Join-Path $ProjectRoot "licenses/wpf/10.0.10/LICENSE.txt"); Name = "WPF-LICENSE.txt" }
    @{ Source = (Join-Path $ProjectRoot "licenses/wpf/10.0.10/THIRD-PARTY-NOTICES.txt"); Name = "WPF-THIRD-PARTY-NOTICES.txt" }
)
foreach ($File in $PortableFiles) {
    if (-not (Test-Path $File.Source -PathType Leaf)) {
        throw "便携包所需文件不存在：$($File.Source)"
    }
    Copy-Item $File.Source (Join-Path $PortableDirectory $File.Name)
}

Compress-Archive -Path (Join-Path $PortableDirectory "*") -DestinationPath $PortableZip -CompressionLevel Optimal

$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
foreach ($Path in @($DirectExecutable, $PortableZip)) {
    $Hash = (Get-FileHash $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $Checksum = "$Hash  $(Split-Path $Path -Leaf)`n"
    [System.IO.File]::WriteAllText("$Path.sha256", $Checksum, $Utf8NoBom)
}

& (Join-Path $ProjectRoot "verify-package.ps1") -Version $Version

Write-Host "发布完成：$DirectExecutable"
Write-Host "便携压缩包：$PortableZip"
