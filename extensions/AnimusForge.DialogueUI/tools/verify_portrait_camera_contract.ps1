param([Parameter(Mandatory=$true)][string]$BannerlordRoot)
# Windows PowerShell metadata inspection only; does not initialize the game engine.
$ErrorActionPreference='Stop'
$script:referenceDirs=@(
    (Join-Path $BannerlordRoot 'bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/Native/bin/Win64_Shipping_Client'))
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($sender,$eventArgs)
    foreach($directory in $script:referenceDirs){
        $file=Join-Path $directory ($eventArgs.Name.Split(',')[0]+'.dll')
        if(Test-Path -LiteralPath $file){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($file)}
    }
    return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)
})
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$view=[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $script:referenceDirs[1] 'TaleWorlds.MountAndBlade.View.dll'))
$tableau=$view.GetType('TaleWorlds.MountAndBlade.View.Tableaus.CharacterTableau',$true)
foreach($pair in @(@('_camPos','TaleWorlds.Library.MatrixFrame'),@('_agentVisuals','TaleWorlds.MountAndBlade.View.AgentVisuals'))){
    if($tableau.GetField($pair[0],$flags).FieldType.FullName -ne $pair[1]){throw "Wrong field: $($pair[0])"}
}
$adjust=$tableau.GetMethod('AdjustCharacterForStanceIndex',$flags)
if(!$adjust -or $adjust.GetParameters().Count -ne 0){throw 'Stance event unavailable'}
$visuals=$view.GetType('TaleWorlds.MountAndBlade.View.AgentVisuals',$true)
$eye=$visuals.GetMethod('GetGlobalStableEyePoint',$flags)
if($eye.ReturnType.FullName -ne 'TaleWorlds.Library.Vec3' -or $eye.GetParameters().Count -ne 1 -or $eye.GetParameters()[0].ParameterType.FullName -ne 'System.Boolean'){throw 'Stable eye contract unavailable'}
if($visuals.GetMethod('GetScale',$flags).ReturnType.FullName -ne 'System.Single'){throw 'Visual scale unavailable'}
$gauntlet=[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $script:referenceDirs[0] 'TaleWorlds.GauntletUI.dll'))
$widget=$gauntlet.GetType('TaleWorlds.GauntletUI.BaseTypes.TextureWidget',$true)
$ready=$widget.GetMethod('SetTextureProviderProperties',$flags)
if(!$ready -or $ready.GetParameters().Count -ne 0 -or !$widget.GetProperty('TextureProvider',$flags)){throw 'Provider lifecycle unavailable'}
$providerAssembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $script:referenceDirs[1] 'TaleWorlds.MountAndBlade.GauntletUI.dll'))
$provider=$providerAssembly.GetType('TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.CharacterTableauTextureProvider',$true)
if($provider.GetField('_characterTableau',$flags).FieldType.FullName -ne $tableau.FullName){throw 'Provider ownership unavailable'}
Write-Output 'PASS: installed game camera fields, stance event, stable eye/scale API and texture provider ownership/lifecycle contracts.'
