$bin = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client'
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($s, $e)
    try { return [Reflection.Assembly]::ReflectionOnlyLoad($e.Name) } catch {}
    $p = Join-Path $bin ($e.Name.Split(',')[0] + '.dll')
    if (Test-Path $p) { return [Reflection.Assembly]::ReflectionOnlyLoadFrom($p) }
    return $null
})
foreach ($dll in @('TaleWorlds.Core.dll','TaleWorlds.Engine.dll','TaleWorlds.GauntletUI.dll','TaleWorlds.MountAndBlade.dll')) {
    $a = [Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $bin $dll))
    try { $types = $a.GetTypes() } catch { $types = $_.Exception.Types | Where-Object { $_ -ne $null } }
    foreach ($t in $types) {
        if ($t.Name -match 'BannerVisual|IBannerVisual|ImageIdentifierTextureProvider|TableauView$|BannerTableau') {
            Write-Output "=== $dll : $($t.FullName) ==="
            $t.GetMethods([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object {
                $ps = ($_.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
                Write-Output ("  " + $_.ReturnType.Name + " " + $_.Name + "(" + $ps + ")")
            }
            $t.GetProperties([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object {
                Write-Output ("  prop " + $_.PropertyType.Name + " " + $_.Name)
            }
        }
    }
}
