[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $ProjectRoot
try {
    dotnet restore NaiWaPet.slnx
    dotnet build NaiWaPet.slnx --configuration $Configuration --no-restore
    dotnet run --project tests/NaiWaPet.Core.Tests/NaiWaPet.Core.Tests.csproj --configuration $Configuration --no-build

    $PythonCommand = Get-Command python -ErrorAction SilentlyContinue
    if ($null -ne $PythonCommand) {
        & $PythonCommand.Source tools/verify_assets.py
    } else {
        Write-Warning "未找到 Python，已跳过提交素材的离线校验。"
    }
} finally {
    Pop-Location
}
