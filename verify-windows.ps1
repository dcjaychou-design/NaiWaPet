[CmdletBinding()]
param(
    [string]$Executable = "artifacts/publish/NaiWaPet.exe",
    [string]$ExpectedFileVersion = "",
    [string]$ExpectedProductVersion = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ExecutablePath = Join-Path $ProjectRoot $Executable
if (-not (Test-Path $ExecutablePath -PathType Leaf)) {
    throw "找不到待验证程序：$ExecutablePath"
}

$Process = Start-Process -FilePath $ExecutablePath -ArgumentList "--smoke-test" -PassThru -Wait
if ($Process.ExitCode -ne 0) {
    throw "Windows 冒烟测试失败，退出码：$($Process.ExitCode)"
}

$File = Get-Item $ExecutablePath
if ($File.Length -lt 10MB) {
    throw "单文件发布体积异常：$($File.Length) bytes"
}

if ($ExpectedFileVersion -and $File.VersionInfo.FileVersion -ne $ExpectedFileVersion) {
    throw "程序文件版本不一致：期望 $ExpectedFileVersion，实际 $($File.VersionInfo.FileVersion)"
}
if ($ExpectedProductVersion -and $File.VersionInfo.ProductVersion -ne $ExpectedProductVersion) {
    throw "程序产品版本不一致：期望 $ExpectedProductVersion，实际 $($File.VersionInfo.ProductVersion)"
}

Write-Host "Windows x64 冒烟测试通过：$($File.FullName)"
