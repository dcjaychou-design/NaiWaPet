[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [switch]$SkipTests,
    [switch]$BuildInstaller
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Artifacts = Join-Path $ProjectRoot "artifacts"
$PublishDirectory = Join-Path $Artifacts "publish"
$PortableDirectory = Join-Path $Artifacts "NaiWaPet-$Version-win-x64"
$PortableZip = Join-Path $Artifacts "NaiWaPet-$Version-win-x64-portable.zip"

if (-not $SkipTests) {
    & (Join-Path $ProjectRoot "build.ps1") -Configuration Release
}

if (Test-Path $PublishDirectory) {
    Remove-Item $PublishDirectory -Recurse -Force
}
if (Test-Path $PortableDirectory) {
    Remove-Item $PortableDirectory -Recurse -Force
}
if (Test-Path $PortableZip) {
    Remove-Item $PortableZip -Force
}

dotnet publish (Join-Path $ProjectRoot "src/NaiWaPet/NaiWaPet.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $PublishDirectory `
    -p:Version=$Version `
    -p:DebugSymbols=false `
    -p:DebugType=None

& (Join-Path $ProjectRoot "verify-windows.ps1") -Executable "artifacts/publish/NaiWaPet.exe"

New-Item -ItemType Directory -Path $PortableDirectory -Force | Out-Null
Copy-Item (Join-Path $PublishDirectory "NaiWaPet.exe") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "README.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "LICENSE") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "NOTICE.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "ASSETS.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "THIRD_PARTY_NOTICES.md") $PortableDirectory
Compress-Archive -Path (Join-Path $PortableDirectory "*") -DestinationPath $PortableZip -CompressionLevel Optimal

$Hash = Get-FileHash $PortableZip -Algorithm SHA256
Set-Content -Path "$PortableZip.sha256" -Value "$($Hash.Hash.ToLowerInvariant())  $(Split-Path $PortableZip -Leaf)" -Encoding utf8NoBOM

if ($BuildInstaller) {
    $Compiler = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($null -eq $Compiler) {
        $Candidates = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
        )
        $CompilerPath = $Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    } else {
        $CompilerPath = $Compiler.Source
    }

    if ([string]::IsNullOrWhiteSpace($CompilerPath)) {
        throw "未找到 Inno Setup 6。请安装后重试 -BuildInstaller。"
    }

    & $CompilerPath "/DMyAppVersion=$Version" (Join-Path $ProjectRoot "installer/NaiWaPet.iss")
}

Write-Host "发布完成：$PortableZip"
