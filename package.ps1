[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Artifacts = Join-Path $ProjectRoot "artifacts"
$PublishDirectory = Join-Path $Artifacts "publish"
$DirectExecutable = Join-Path $Artifacts "NaiWaPet-$Version-win-x64.exe"
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
if (Test-Path "$PortableZip.sha256") {
    Remove-Item "$PortableZip.sha256" -Force
}
if (Test-Path $DirectExecutable) {
    Remove-Item $DirectExecutable -Force
}
if (Test-Path "$DirectExecutable.sha256") {
    Remove-Item "$DirectExecutable.sha256" -Force
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

Copy-Item (Join-Path $PublishDirectory "NaiWaPet.exe") $DirectExecutable
New-Item -ItemType Directory -Path $PortableDirectory -Force | Out-Null
Copy-Item (Join-Path $PublishDirectory "NaiWaPet.exe") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "README.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "LICENSE") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "NOTICE.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "ASSETS.md") $PortableDirectory
Copy-Item (Join-Path $ProjectRoot "THIRD_PARTY_NOTICES.md") $PortableDirectory
Compress-Archive -Path (Join-Path $PortableDirectory "*") -DestinationPath $PortableZip -CompressionLevel Optimal

$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$Hash = Get-FileHash $PortableZip -Algorithm SHA256
$Checksum = "$($Hash.Hash.ToLowerInvariant())  $(Split-Path $PortableZip -Leaf)`n"
[System.IO.File]::WriteAllText("$PortableZip.sha256", $Checksum, $Utf8NoBom)

$Hash = Get-FileHash $DirectExecutable -Algorithm SHA256
$Checksum = "$($Hash.Hash.ToLowerInvariant())  $(Split-Path $DirectExecutable -Leaf)`n"
[System.IO.File]::WriteAllText("$DirectExecutable.sha256", $Checksum, $Utf8NoBom)

Write-Host "发布完成：$DirectExecutable"
Write-Host "便携压缩包：$PortableZip"
