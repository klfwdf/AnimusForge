param([Parameter(Mandatory=$true)][string]$BannerlordRoot)
# Run in Windows PowerShell: metadata-only loading, no native engine initialization.
$ErrorActionPreference='Stop'
$script:referenceDirs=@(
    (Join-Path $BannerlordRoot 'bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/Native/bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/SandBox/bin/Win64_Shipping_Client'))
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($sender,$eventArgs)
    foreach($directory in $script:referenceDirs){
        $file=Join-Path $directory ($eventArgs.Name.Split(',')[0]+'.dll')
        if(Test-Path -LiteralPath $file){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($file)}
    }
    return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)
})
$assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $script:referenceDirs[2] 'SandBox.View.dll'))
$type=$assembly.GetType('SandBox.View.Map.MapConversationTableau',$true)
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$data=$type.GetField('_data',$flags)
$visuals=$type.GetField('_agentVisuals',$flags)
if(!$data -or !$visuals -or $visuals.FieldType.GenericTypeArguments[0].FullName -ne 'TaleWorlds.MountAndBlade.View.AgentVisuals'){throw 'Map fields incompatible'}
$spawn=$type.GetMethod('SpawnOpponentLeader',$flags)
$setData=$type.GetMethod('SetData',$flags)
$finalize=$type.GetMethod('OnFinalize',$flags)
if(!$spawn -or $spawn.GetParameters().Count -ne 0 -or $setData.GetParameters()[0].ParameterType.FullName -ne 'System.Object' -or $finalize.GetParameters()[0].ParameterType.FullName -ne 'System.Boolean'){throw 'Map methods incompatible'}
$partner=$data.FieldType.GetProperty('ConversationPartnerData')
if($partner.PropertyType.GetField('Character').FieldType.FullName -ne 'TaleWorlds.CampaignSystem.CharacterObject'){throw 'Map character accessor incompatible'}
[pscustomobject]@{Assembly=$assembly.Location;Capture=$spawn.Name;Cleanup=@($setData.Name,$finalize.Name);DataIdentity=$data.FieldType.FullName;ContractValid=$true} | ConvertTo-Json
