param([Parameter(Mandatory=$true)][string]$BannerlordRoot,[ValidateSet('1.3','1.4')][string]$Api='1.4',
      # Optional: verify a staged host / UI build before it is deployed (defaults: installed host, build.ps1 output).
      [string]$HostDir='',[string]$UiDir='')
$ErrorActionPreference='Stop'
$module=Split-Path $PSScriptRoot -Parent
if(!$HostDir){$HostDir=Join-Path $BannerlordRoot ('Modules/AnimusForge/bin/Win64_Shipping_Client/versions/'+$Api)}
if(!$UiDir){$UiDir=Join-Path $module ('artifacts/'+$Api)}
$script:dirs=@(
    $UiDir,
    $HostDir,
    (Join-Path $BannerlordRoot 'bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/Native/bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/SandBox/bin/Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client'))
if($Api -eq '1.3'){
    $nuget=Join-Path $env:USERPROFILE '.nuget/packages'
    $script:dirs=@($script:dirs[0],$script:dirs[1],
        (Join-Path $nuget 'bannerlord.referenceassemblies.core/1.3.15.110062/ref/net472'),
        (Join-Path $nuget 'bannerlord.referenceassemblies.native/1.3.15.110062/ref/net472'))+$script:dirs[2..5]
}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($sender,$eventArgs)
    foreach($dir in $script:dirs){
        $path=Join-Path $dir ($eventArgs.Name.Split(',')[0]+'.dll')
        if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)}
    }
    return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)
})
function Load-Assembly($name){
    foreach($dir in $script:dirs){$path=Join-Path $dir ($name+'.dll');if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)}}
    throw ('Assembly unavailable: '+$name)
}
$hostAssembly=Load-Assembly 'AnimusForge'
$ui=Load-Assembly 'AnimusForge.DialogueUI'
$core=Load-Assembly 'TaleWorlds.Core'
$gui=Load-Assembly 'TaleWorlds.GauntletUI'
$mbWidgets=Load-Assembly 'TaleWorlds.MountAndBlade.GauntletUI.Widgets'
$flags=[Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
$hostType=$hostAssembly.GetType('AnimusForge.ShoutBehavior',$true)
foreach($name in @('_shoutTradeOptions','_shoutPendingTradeItems','_shoutPendingTradeItemIndex','_shoutTradeActionOnly','_shoutTradeActionOnlyFinished')){
    if(!$hostType.GetField($name,$flags)){throw ('Missing host field: '+$name)}
}
foreach($pair in @(@('ShoutTradeResourceOption',@('Name','AvailableAmount','IsGold','ItemId','InventoryUnitValue','PartyEntry','SettlementEntry')),
                   @('ShoutPendingTradeItem',@('Amount','IsGold','ItemId','PartyEntry','SettlementEntry')))){
    $type=$hostType.GetNestedType($pair[0],[Reflection.BindingFlags]::NonPublic)
    if(!$type){throw ('Missing host type: '+$pair[0])}
    foreach($name in $pair[1]){if(!$type.GetField($name,$flags)){throw ('Missing nested field: '+$name)}}
}
foreach($pair in @(@('ShowShoutTradeAmountInquiry',0),@('CommitShoutTradeActionOnly',0),@('OnShoutTradeResourcesSelected',1))){
    $method=$hostType.GetMethod($pair[0],$flags)
    if(!$method -or $method.GetParameters().Count -ne $pair[1]){throw ('Host method mismatch: '+$pair[0])}
}
$overlay=$hostAssembly.GetType('AnimusForge.AnimusForgeNativeConversationOverlay',$true)
foreach($name in @('_activeOverlay','_layer','_dataSource','_temporarySystemUiActive','_isSubmitting')){if(!$overlay.GetField($name,$flags)){throw $name}}
foreach($name in @('FocusInputIfVisible','RestoreNativeConversationInputAfterOrdinaryMode')){if(!$overlay.GetMethod($name,$flags)){throw $name}}
$history=$hostAssembly.GetType('AnimusForge.AnimusForgeConversationHistoryLogVM',$true)
if(!$history.GetMethod('CancelDeferredFormatting',$flags)){throw 'History cleanup unavailable'}
$nav=$hostAssembly.GetType('AnimusForge.EncyclopediaEntityLinkNavigationCoordinator',$true).GetMethod('Request',$flags)
if($nav.GetParameters().Count -ne 3){throw 'Navigation callback mismatch'}
$inquiry=$core.GetType('TaleWorlds.Core.MBInformationManager',$true).GetMethod('ShowMultiSelectionInquiry',$flags)
if($inquiry.GetParameters()[0].ParameterType.FullName -ne 'TaleWorlds.Core.MultiSelectionInquiryData'){throw 'Inquiry signature mismatch'}

# Validate every real data binding and command in the auxiliary tree, including list item scopes.
$script:bindingCount=0
$script:widgetCount=0
function Check-Node($node,[Type]$scope){
    if($node.NodeType -ne 'Element'){return}
    $binding=$node.GetAttribute('DataSource')
    if($binding -match '^{(.+)}$'){
        $property=$scope.GetProperty($Matches[1])
        if(!$property){throw ('Unknown datasource '+$scope.Name+'.'+$Matches[1])}
        $scope=$property.PropertyType
        if($scope.IsGenericType){$scope=$scope.GetGenericArguments()[0]}
    }
    foreach($attr in $node.Attributes){
        if($attr.Value.StartsWith('@')){
            if(!$scope.GetProperty($attr.Value.Substring(1))){throw ('Unknown binding '+$scope.Name+'.'+$attr.Value)}
            $script:bindingCount++
        }
        if($attr.Name.StartsWith('Command.')){
            if(!$scope.GetMethod($attr.Value)){throw ('Unknown command '+$scope.Name+'.'+$attr.Value)}
            $script:bindingCount++
        }
    }
    if($node.Name -notin @('Children','ItemTemplate')){
        $widget=$gui.GetType('TaleWorlds.GauntletUI.BaseTypes.'+$node.Name)
        if(!$widget){$widget=$hostAssembly.GetType('AnimusForge.'+$node.Name)}
        if(!$widget){$widget=$mbWidgets.GetType('TaleWorlds.MountAndBlade.GauntletUI.Widgets.'+$node.Name)}
        if(!$widget){throw ('Unknown widget type '+$node.Name)}
        foreach($attr in $node.Attributes){
            if($attr.Name.StartsWith('Brush.')){
                $brushType=$gui.GetType('TaleWorlds.GauntletUI.Brush',$true)
                if(!$brushType.GetProperty($attr.Name.Substring(6))){throw ('Unknown brush property '+$attr.Name)}
            }
            if($attr.Name -eq 'DataSource' -or $attr.Name.Contains('.')){continue}
            if(!$widget.GetProperty($attr.Name) -and !$widget.GetField($attr.Name)){throw ('Unknown widget property '+$node.Name+'.'+$attr.Name)}
        }
        $script:widgetCount++
    }
    foreach($child in $node.ChildNodes){Check-Node $child $scope}
}
$xml=New-Object System.Xml.XmlDocument
$xml.Load((Join-Path $module 'GUI/Prefabs/AFDialogueNativeOverlay.xml'))
$panel=$xml.SelectSingleNode('//*[@Id="AFDialogueAuxiliaryPanel"]')
if(!$panel){throw 'Panel missing'}
Check-Node $panel ($ui.GetType('AnimusForge.DialogueUI.Native.NativeOverlayVM',$true))
# Scene wheel and persistent-session panels: whole movie against its root VM.
foreach($pair in @(@('AFSceneWheel','AnimusForge.DialogueUI.Scene.SceneWheelVM'),
                   @('AFSceneSessionScroll','AnimusForge.DialogueUI.Scene.SceneSessionVM'),
                   @('AFSceneSessionFolio','AnimusForge.DialogueUI.Scene.SceneSessionVM'))){
    $scene=New-Object System.Xml.XmlDocument
    $scene.Load((Join-Path $module ('GUI/Prefabs/'+$pair[0]+'.xml')))
    Check-Node $scene.SelectSingleNode('/Prefab/Window/Widget') ($ui.GetType($pair[1],$true))
}
foreach($name in @('ScenePresentationSessionHook','ScenePresentationBlocksHotkeysHook')){if(!$hostType.GetField($name,$flags)){throw ('Missing host hook: '+$name)}}
foreach($name in @('SubmitScenePresentationTextForExternal','GetScenePresentationParticipantsForExternal','GetScenePresentationHistoryForExternal',
                   'SetScenePresentationAddresseeForExternal','CycleScenePresentationParticipantForExternal','LoadScenePresentationTradeOptionsForExternal',
                   'StageScenePresentationTradeForExternal','CancelScenePresentationTradeForExternal','ConsumeScenePresentationTradeRequestForExternal')){
    if(!$hostType.GetMethod($name,$flags)){throw ('Missing host session API: '+$name)}
}
Write-Output ('PASS '+$Api+': host reflection contract, '+$script:bindingCount+' data/command bindings, '+$script:widgetCount+' widget property contracts. No game engine initialized.')
