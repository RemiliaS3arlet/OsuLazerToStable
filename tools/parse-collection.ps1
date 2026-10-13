$ErrorActionPreference = 'Stop'
$cdb = "$env:LOCALAPPDATA\osu!\collection.db"
$b = [System.IO.File]::ReadAllBytes($cdb)
"fileLen=" + $b.Length
"first 96 bytes: " + (($b[0..95] | ForEach-Object { $_.ToString('X2') }) -join ' ')
$i = 0
function Hex2($arr, $start, $count) { ($arr[$start..($start + $count - 1)] | ForEach-Object { $_.ToString('X2') }) -join ' ' }
$ver = [BitConverter]::ToInt32($b, $i); $i += 4
$n = [BitConverter]::ToInt32($b, $i); $i += 4
"version=$ver collections=$n"
for ($c = 0; $c -lt [Math]::Min($n, 2); $c++) {
    $marker = $b[$i]; $i += 1
    "col[$c] marker=$marker"
    if ($marker -eq 11) {
        $len = $b[$i]; $i += 1
        $name = [System.Text.Encoding]::UTF8.GetString($b, $i, $len); $i += $len
        "  name(len=$len)='$name'"
    } elseif ($marker -eq 0) {
        "  name(empty)"
    } else {
        $name = [System.Text.Encoding]::UTF8.GetString($b, $i, $marker); $i += $marker
        "  name(len=$marker)='$name'"
    }
    $m = [BitConverter]::ToInt32($b, $i); $i += 4
    "  maps=$m"
    $show = [Math]::Min($m, 3)
    for ($j = 0; $j -lt $show; $j++) {
        $m2 = $b[$i]; $i += 1
        $hexBefore = Hex2 $b $i 4
        if ($m2 -eq 11) { $len2 = $b[$i]; $i += 1; $s = [System.Text.Encoding]::UTF8.GetString($b, $i, $len2); $i += $len2 }
        elseif ($m2 -eq 0) { $s = '' }
        else { $s = [System.Text.Encoding]::UTF8.GetString($b, $i, $m2); $i += $m2 }
        "    md5[$j] marker=$m2 next4=$hexBefore val='$s'"
    }
    $skip = ($m - $show) * 33
    $i += $skip
    "  pos after col=$i / $($b.Length)"
}
"final pos=$i len=$($b.Length)"
