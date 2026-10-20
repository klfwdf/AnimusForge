param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$ReferenceDirectory,
    [string[]]$DependencyDirectory = @()
)
$ErrorActionPreference = 'Stop'
# Inspect the real candidate DLL. No generation call, native game, HTTP or paid API.
Add-Type -Path (Join-Path $PSScriptRoot '../audits/OfflineAssemblyResolver.cs')
$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]@(
    (Split-Path -Parent $resolvedAssembly),
    (Resolve-Path -LiteralPath $ReferenceDirectory).Path
    $DependencyDirectory
)))
try {
    $assembly = [Reflection.Assembly]::LoadFrom($resolvedAssembly)
    $client = $assembly.GetType('AnimusForge.Illustrator.Core.UniversalOpenAiImageClient', $true)
    $select = $client.GetMethod('IsChatCompletionProtocol', [Reflection.BindingFlags]'Public,Static')
    $endpoint = $client.GetMethod('ResolveEndpointUrl', [Reflection.BindingFlags]'Public,Static')
    $cases = @(
        @{ Model='gemini-nano-banana-2.1'; Chat=$true },
        @{ Model='GEMINI-NANO-BANANA-2.1'; Chat=$true },
        @{ Model='provider/GeMiNi-custom'; Chat=$true },
        @{ Model='gemini'; Chat=$true },
        @{ Model='gemini-3.1-flash-image'; Chat=$true },
        @{ Model='chat-image'; Chat=$true },
        @{ Model='gpt-image-1'; Chat=$false },
        @{ Model='FLUX.1-schnell'; Chat=$false },
        @{ Model='banana-2.1'; Chat=$false },
        @{ Model=''; Chat=$false }
    )
    $checks = 0
    foreach ($case in $cases) {
        $url = 'https://offline.invalid/v1'
        $chat = [bool]$select.Invoke($null, [object[]]@($case.Model, $url))
        if ($chat -ne $case.Chat) { throw ('Wrong automatic protocol for model: ' + $case.Model) }
        $actual = [string]$endpoint.Invoke($null, [object[]]@($url, $chat))
        $expected = if ($case.Chat) { '/v1/chat/completions' } else { '/v1/images/generations' }
        if (([Uri]$actual).AbsolutePath -ne $expected) { throw ('Wrong endpoint for model: ' + $case.Model) }
        $checks += 2
    }
    if (-not [bool]$select.Invoke($null, [object[]]@('ordinary-model', 'https://offline.invalid/v1/chat/completions'))) {
        throw 'An explicit Chat endpoint must still select Chat.'
    }
    if ([bool]$select.Invoke($null, [object[]]@('ordinary-model', 'https://offline.invalid/v1?route=/chat/completions'))) {
        throw 'Query text must not select the Chat protocol.'
    }
    $checks += 2
    Write-Output ("PASS Gemini automatic protocol: $checks assertions; no HTTP requests.")
} catch {
    Write-Output $_.Exception.ToString()
    exit 1
} finally { $resolver.Dispose() }
