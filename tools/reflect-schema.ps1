$ErrorActionPreference = 'Continue'
$lazerDir = "$env:LOCALAPPDATA\osulazer\current"
"LAZER DIR: $lazerDir  exists=" + (Test-Path $lazerDir)

$onResolve = {
    param($s, $e)
    $name = ($e.Name -split ',')[0].Trim() + '.dll'
    $p = Join-Path $lazerDir $name
    if (Test-Path $p) { return [Reflection.Assembly]::LoadFrom($p) }
    $p2 = Join-Path $lazerDir ('runtimes\win-x64\native\' + $name)
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($onResolve)

"=========== 1) REALM.DLL API ==========="
$nuget = Join-Path $env:USERPROFILE '.nuget\packages\realm\20.1.0'
$realmDll = Get-ChildItem $nuget -Recurse -Filter 'Realm.dll' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch 'ref' } | Select-Object -First 1
"REALM DLL: " + $(if ($realmDll) { $realmDll.FullName } else { 'NOT FOUND' })
if ($realmDll) {
    $rasm = [Reflection.Assembly]::LoadFrom($realmDll.FullName)
    $rtypes = @()
    try { $rtypes = $rasm.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $rtypes = @($_.Exception.Types | Where-Object { $_ }) }
    "realm types loaded: " + $rtypes.Count
    $names = @('RealmSchema', 'DynamicObjectApi', 'RealmObjectBase')
    foreach ($n in $names) {
        $t = $rtypes | Where-Object { $_.Name -eq $n -and $_.IsPublic } | Select-Object -First 1
        if ($t) {
            "---- type: $($t.FullName) ----"
            $t.GetMembers([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object { "  " + $_.ToString() }
        } else { "---- type $n not found ----" }
    }
    # nested Builder of RealmSchema
    $rs = $rtypes | Where-Object { $_.Name -eq 'RealmSchema' } | Select-Object -First 1
    if ($rs) {
        foreach ($nt in $rs.GetNestedTypes()) {
            "---- nested: $($nt.Name) ----"
            $nt.GetMembers([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object { "  " + $_.ToString() }
        }
    }
    # Realm.Dynamic nested type
    $realmType = $rtypes | Where-Object { $_.Name -eq 'Realm' -and $_.IsPublic } | Select-Object -First 1
    if ($realmType) {
        foreach ($nt in $realmType.GetNestedTypes()) {
            "---- nested in Realm: $($nt.Name) ----"
            $nt.GetMembers([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly') | ForEach-Object { "  " + $_.ToString() }
        }
        $dynProp = $realmType.GetProperty('DynamicApi')
        "Realm.DynamicApi prop: " + $(if ($dynProp) { $dynProp.PropertyType.FullName } else { 'n/a' })
    }
    # RealmObjectBase.DynamicApi
    $rob = $rtypes | Where-Object { $_.Name -eq 'RealmObjectBase' } | Select-Object -First 1
    if ($rob) {
        $p = $rob.GetProperty('DynamicApi')
        "RealmObjectBase.DynamicApi: " + $(if ($p) { $p.PropertyType.FullName } else { 'n/a' })
    }
}

"=========== 2) OSU.GAME.DLL MODELS ==========="
try {
    $og = [Reflection.Assembly]::LoadFrom((Join-Path $lazerDir 'osu.Game.dll'))
    "osu.Game.dll loaded: " + $og.FullName
    $otypes = @()
    try { $otypes = @($og.GetTypes()) } catch [Reflection.ReflectionTypeLoadException] {
        "LOADER EXCEPTIONS:"
        $_.Exception.LoaderExceptions | ForEach-Object { "  " + $_.Message }
        $otypes = @($_.Exception.Types | Where-Object { $_ })
    }
    "osu types: " + $otypes.Count
    $modelNames = @('BeatmapSetInfo', 'BeatmapInfo', 'BeatmapMetadata', 'BeatmapCollection', 'RealmUser', 'RulesetInfo', 'BeatmapDifficulty', 'BeatmapUserSettings', 'RealmNamedFileUsage', 'RealmFile', 'BeatmapUserRating')
    foreach ($n in $modelNames) {
        $t = $otypes | Where-Object { $_.Name -eq $n } | Select-Object -First 1
        if (-not $t) { "---- $n NOT FOUND ----"; continue }
        "==== class $($t.FullName) (base: $($t.BaseType.Name)) ===="
        foreach ($p in $t.GetProperties([Reflection.BindingFlags]'Public,Instance,DeclaredOnly')) {
            $flags = @()
            foreach ($attr in $p.GetCustomAttributes($false)) {
                $an = $attr.GetType().Name
                if ($an -in @('PrimaryKeyAttribute','IndexedAttribute','IgnoredAttribute','BacklinkAttribute','MapToAttribute','RequiredAttribute')) {
                    if ($an -eq 'MapToAttribute') { $flags += ('MapTo:' + $attr.PropertyName) } else { $flags += $an }
                }
            }
            $pt = $p.PropertyType
            $ptName = $pt.Name
            if ($pt.IsGenericType) {
                $gargs = ($pt.GetGenericArguments() | ForEach-Object { $_.Name }) -join ','
                $ptName = $pt.Name.Split('`')[0] + "<$gargs>"
            }
            "  prop {0,-28} {1}   {2}" -f $ptName, $p.Name, ($flags -join ' ')
        }
    }
} catch { "osu.Game.dll load failed: $($_.Exception.Message)" }
