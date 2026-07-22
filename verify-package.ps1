[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
[xml]$BuildProperties = Get-Content (Join-Path $ProjectRoot "Directory.Build.props") -Raw
$SourceVersion = [string]$BuildProperties.Project.PropertyGroup.Version
$FileVersion = [string]$BuildProperties.Project.PropertyGroup.FileVersion
if ($PSBoundParameters.ContainsKey("Version") -and $Version -ne $SourceVersion) {
    throw "待验证版本 $Version 与源码版本 $SourceVersion 不一致。"
}
$Version = $SourceVersion
$Artifacts = Join-Path $ProjectRoot "artifacts"
$ExecutableName = "NaiWaPet-$Version-win-x64.exe"
$ZipName = "NaiWaPet-$Version-win-x64-portable.zip"
$Executable = Join-Path $Artifacts $ExecutableName
$PortableZip = Join-Path $Artifacts $ZipName

function Assert-Checksum([string]$Path) {
    if (-not (Test-Path $Path -PathType Leaf)) {
        throw "缺少发布文件：$Path"
    }
    $ChecksumPath = "$Path.sha256"
    if (-not (Test-Path $ChecksumPath -PathType Leaf)) {
        throw "缺少校验文件：$ChecksumPath"
    }
    $Hash = (Get-FileHash $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $Expected = "$Hash  $(Split-Path $Path -Leaf)`n"
    $Actual = [System.IO.File]::ReadAllText($ChecksumPath)
    if ($Actual -cne $Expected) {
        throw "SHA-256 校验文件内容不正确：$ChecksumPath"
    }
}

Assert-Checksum $Executable
Assert-Checksum $PortableZip
& (Join-Path $ProjectRoot "verify-windows.ps1") `
    -Executable "artifacts/$ExecutableName" `
    -ExpectedFileVersion $FileVersion `
    -ExpectedProductVersion $Version

$ExpectedEntries = @(
    "ASSETS.md",
    "DOTNET-LICENSE.txt",
    "DOTNET-THIRD-PARTY-NOTICES.txt",
    "LICENSE",
    "NaiWaPet.exe",
    "NOTICE.md",
    "README.md",
    "THIRD_PARTY_NOTICES.md",
    "WPF-LICENSE.txt",
    "WPF-THIRD-PARTY-NOTICES.txt"
)
$Archive = [System.IO.Compression.ZipFile]::OpenRead($PortableZip)
$TemporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "NaiWaPet-package-$PID-$([guid]::NewGuid().ToString('N'))"
try {
    $ActualEntries = @($Archive.Entries | ForEach-Object FullName | Sort-Object)
    if (($ActualEntries -join "`n") -cne (($ExpectedEntries | Sort-Object) -join "`n")) {
        throw "便携 ZIP 文件清单不正确。实际内容：$($ActualEntries -join ', ')"
    }

    New-Item -ItemType Directory -Path $TemporaryDirectory | Out-Null
    $ZipExecutable = Join-Path $TemporaryDirectory "NaiWaPet.exe"
    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($Archive.GetEntry("NaiWaPet.exe"), $ZipExecutable)
    if ((Get-FileHash $Executable -Algorithm SHA256).Hash -ne (Get-FileHash $ZipExecutable -Algorithm SHA256).Hash) {
        throw "便携 ZIP 内的 NaiWaPet.exe 与独立 EXE 不一致。"
    }

    $ExpectedDocuments = @(
        @{ Name = "README.md"; Source = "README.md" }
        @{ Name = "LICENSE"; Source = "LICENSE" }
        @{ Name = "NOTICE.md"; Source = "NOTICE.md" }
        @{ Name = "ASSETS.md"; Source = "ASSETS.md" }
        @{ Name = "THIRD_PARTY_NOTICES.md"; Source = "THIRD_PARTY_NOTICES.md" }
        @{ Name = "DOTNET-LICENSE.txt"; Source = "licenses/dotnet/10.0.10/LICENSE.txt" }
        @{ Name = "DOTNET-THIRD-PARTY-NOTICES.txt"; Source = "licenses/dotnet/10.0.10/THIRD-PARTY-NOTICES.txt" }
        @{ Name = "WPF-LICENSE.txt"; Source = "licenses/wpf/10.0.10/LICENSE.txt" }
        @{ Name = "WPF-THIRD-PARTY-NOTICES.txt"; Source = "licenses/wpf/10.0.10/THIRD-PARTY-NOTICES.txt" }
    )
    foreach ($Document in $ExpectedDocuments) {
        $Entry = $Archive.GetEntry($Document.Name)
        if ($null -eq $Entry -or $Entry.Length -le 0) {
            throw "便携 ZIP 内的文档为空或缺失：$($Document.Name)"
        }
        $Copy = Join-Path $TemporaryDirectory $Document.Name
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($Entry, $Copy)
        $Source = Join-Path $ProjectRoot $Document.Source
        if ((Get-FileHash $Source -Algorithm SHA256).Hash -ne (Get-FileHash $Copy -Algorithm SHA256).Hash) {
            throw "便携 ZIP 内的文档与源码文件不一致：$($Document.Name)"
        }
    }

    $LicenseCopy = Join-Path $TemporaryDirectory "LICENSE"
    if ([System.IO.File]::ReadAllBytes($LicenseCopy) -contains 13) {
        throw "便携 ZIP 内的 LICENSE 必须使用 LF 换行。"
    }
} finally {
    $Archive.Dispose()
    if (Test-Path $TemporaryDirectory) {
        Remove-Item $TemporaryDirectory -Recurse -Force
    }
}

Write-Host "发布包验证通过：版本 $Version，ZIP 内 10 个文件，EXE 与校验文件一致。"
