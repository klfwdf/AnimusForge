param([Parameter(Mandatory=$true)][string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
# Tests the built production helper without native game calls or actual screenshots.
Add-Type -Path (Join-Path $PSScriptRoot 'PassiveScreenshotAudit.cs') -ReferencedAssemblies 'System.Drawing.dll'
[PassiveScreenshotAudit]::Run((Resolve-Path -LiteralPath $AssemblyPath).Path)
