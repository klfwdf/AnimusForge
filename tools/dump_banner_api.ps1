$bin = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client'
[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $bin 'TaleWorlds.Library.dll')) | Out-Null
try { [Reflection.Assembly]::ReflectionOnlyLoadFrom('netstandard') | Out-Null } catch {}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($s, $e)
    try { return [Reflection.Assembly]::ReflectionOnlyLoad($e.Name) } catch {}
    $p = Join-Path $bin ($e.Name.Split(',')[0] + '.dll')
    if (Test-Path $p) { return [Reflection.Assembly]::ReflectionOnlyLoadFrom($p) }
    return $null
})
$a = [Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $bin 'TaleWorlds.Core.dll'))
foreach ($name in @('TaleWorlds.Core.BannerManager','TaleWorlds.Core.Banner','TaleWorlds.Core.BannerData','TaleWorlds.Core.BannerIconGroup','TaleWorlds.Core.BannerIconData','TaleWorlds.Core.BannerCode')) {
    $t = $a.GetType($name)
    if ($null -eq $t) { Write-Output "$name : NOT FOUND"; continue }
    Write-Output "=== $name ==="
    $t.GetMethods([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object {
        $ps = ($_.GetParameters() | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
        Write-Output ("  " + $_.ReturnType.Name + " " + $_.Name + "(" + $ps + ")")
    }
    $t.GetProperties([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object {
        Write-Output ("  prop " + $_.PropertyType.Name + " " + $_.Name)
    }
    $t.GetFields([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object {
        Write-Output ("  field " + $_.FieldType.Name + " " + $_.Name)
    }
}
