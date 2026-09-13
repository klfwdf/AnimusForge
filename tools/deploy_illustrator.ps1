# Deploy script for AnimusForge.Illustrator
param(
    [string]$GamePath = "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir
$SourceDir = Join-Path $RepoRoot "extensions\AnimusForge.Illustrator"
$TargetDir = Join-Path $GamePath "Modules\AnimusForge_Illustrator"

Write-Host "==> [Illustrator] Building AnimusForge.Illustrator (Release)..." -ForegroundColor Cyan
dotnet build "$SourceDir\src\AnimusForge.Illustrator.csproj" -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "==> [Illustrator] Deploying to $TargetDir..." -ForegroundColor Cyan

New-Item -ItemType Directory -Path "$TargetDir\bin\Win64_Shipping_Client" -Force | Out-Null
New-Item -ItemType Directory -Path "$TargetDir\GUI\Prefabs" -Force | Out-Null

Copy-Item -Path "$SourceDir\SubModule.xml" -Destination "$TargetDir\SubModule.xml" -Force
try {
    Copy-Item -Path "$SourceDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll" -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    Copy-Item -Path "$SourceDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.pdb" -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
}
catch {
    Write-Warning "DLL is locked. Attempting rename-and-replace fallback..."
    $backupDll = "$TargetDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll.old"
    Remove-Item $backupDll -Force -ErrorAction SilentlyContinue
    Move-Item -Path "$TargetDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll" -Destination $backupDll -Force -ErrorAction SilentlyContinue
    Copy-Item -Path "$SourceDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll" -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    Copy-Item -Path "$SourceDir\bin\Win64_Shipping_Client\AnimusForge.Illustrator.pdb" -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
}
Copy-Item -Path "$SourceDir\GUI\Prefabs\*" -Destination "$TargetDir\GUI\Prefabs\" -Force

Write-Host "==> [Illustrator] Deployment complete!" -ForegroundColor Green
Get-ChildItem -Path $TargetDir -Recurse | Where-Object { -not $_.PSIsContainer } | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
