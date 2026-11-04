# ======================================================================
# Környezeti változók ellenőrzése a visszaállítás ELŐTT
#
# Megnézi, hogy a mentésből visszaállítandó Path-sorok és mappát mutató
# rendszerváltozók közül melyik mappa létezik még a gépen. SEMMIT NEM ÍR
# a registrybe - csak két fájlt tesz az Asztalra:
#   - ellenorzes_eredmenye.txt         : a teljes lista (megvan / hiányzik)
#   - kornyezeti_valtozok_szurt.reg    : a visszaállító .reg fájl, de CSAK
#                                        a ma is létező mappákkal
# A szűrt .reg-et utána dupla kattintással lehet importálni (majd újraindítás).
#
# Futtatás: jobb klikk a fájlon -> "Futtatás a PowerShell-lel"
# (vagy PowerShellben:  powershell -ExecutionPolicy Bypass -File .\kornyezet_ellenorzes.ps1)
# ======================================================================

$ErrorActionPreference = 'Stop'

# --- A mentés (2026-10-29) rendszerváltozói, a Path nélkül ---
$valtozok = @(
    @{ Nev = '__PSLockDownPolicy'; Bovitheto = $false; Ertek = '0' }
    @{ Nev = 'ACSetupSvcPort'; Bovitheto = $false; Ertek = '23210' }
    @{ Nev = 'ACSvcPort'; Bovitheto = $false; Ertek = '17532' }
    @{ Nev = 'AMIGAFOREVERDATA'; Bovitheto = $false; Ertek = 'C:\Users\Public\Documents\Amiga Files\' }
    @{ Nev = 'AMIGAFOREVERROOT'; Bovitheto = $false; Ertek = 'C:\Program Files (x86)\Cloanto\Amiga Forever\' }
    @{ Nev = 'BRAINGINES_PATH'; Bovitheto = $false; Ertek = 'C:\ProgramData\GPU AUDIO' }
    @{ Nev = 'C64FOREVERDATA'; Bovitheto = $false; Ertek = 'C:\Users\Public\Documents\CBM Files\' }
    @{ Nev = 'C64FOREVERROOT'; Bovitheto = $false; Ertek = 'C:\Program Files (x86)\Cloanto\C64 Forever\' }
    @{ Nev = 'ComSpec'; Bovitheto = $true; Ertek = '%SystemRoot%\system32\cmd.exe' }
    @{ Nev = 'DokanLibrary1'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-1.5.1\' }
    @{ Nev = 'DokanLibrary1_LibraryPath_x64'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-1.5.1\lib\' }
    @{ Nev = 'DokanLibrary1_LibraryPath_x86'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-1.5.1\x86\lib\' }
    @{ Nev = 'DokanLibrary2'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-2.2.0\' }
    @{ Nev = 'DokanLibrary2_LibraryPath_x64'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-2.2.0\lib\' }
    @{ Nev = 'DokanLibrary2_LibraryPath_x86'; Bovitheto = $false; Ertek = 'C:\Program Files\Dokan\Dokan Library-2.2.0\x86\lib\' }
    @{ Nev = 'DriverData'; Bovitheto = $false; Ertek = 'C:\Windows\System32\Drivers\DriverData' }
    @{ Nev = 'EnableLog'; Bovitheto = $false; Ertek = 'VERBOSE' }
    @{ Nev = 'INTEL_DEV_REDIST'; Bovitheto = $false; Ertek = 'C:\Program Files (x86)\Common Files\Intel\Shared Libraries\' }
    @{ Nev = 'NUMBER_OF_PROCESSORS'; Bovitheto = $false; Ertek = '20' }
    @{ Nev = 'OS'; Bovitheto = $false; Ertek = 'Windows_NT' }
    @{ Nev = 'PATHEXT'; Bovitheto = $false; Ertek = '.COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC' }
    @{ Nev = 'PROCESSOR_ARCHITECTURE'; Bovitheto = $false; Ertek = 'AMD64' }
    @{ Nev = 'PROCESSOR_IDENTIFIER'; Bovitheto = $false; Ertek = 'Intel64 Family 6 Model 183 Stepping 1, GenuineIntel' }
    @{ Nev = 'PROCESSOR_LEVEL'; Bovitheto = $false; Ertek = '6' }
    @{ Nev = 'PROCESSOR_REVISION'; Bovitheto = $false; Ertek = 'b701' }
    @{ Nev = 'PSModulePath'; Bovitheto = $true; Ertek = '%ProgramFiles%\WindowsPowerShell\Modules;%SystemRoot%\system32\WindowsPowerShell\v1.0\Modules' }
    @{ Nev = 'RETROPLATFORMDATA'; Bovitheto = $false; Ertek = 'C:\ProgramData\Cloanto\RetroPlatform\' }
    @{ Nev = 'RlsSvcPort'; Bovitheto = $false; Ertek = '22112' }
    @{ Nev = 'TEMP'; Bovitheto = $true; Ertek = '%SystemRoot%\TEMP' }
    @{ Nev = 'TMP'; Bovitheto = $true; Ertek = '%SystemRoot%\TEMP' }
    @{ Nev = 'USERNAME'; Bovitheto = $false; Ertek = 'SYSTEM' }
    @{ Nev = 'windir'; Bovitheto = $true; Ertek = '%SystemRoot%' }
    @{ Nev = 'ZES_ENABLE_SYSMAN'; Bovitheto = $false; Ertek = '1' }
    @{ Nev = 'FONTCONFIG_FILE'; Bovitheto = $false; Ertek = 'C:\Windows\fonts.conf' }
    @{ Nev = 'VBOX_MSI_INSTALL_PATH'; Bovitheto = $false; Ertek = 'C:\Program Files\Oracle\VirtualBox\' }
    @{ Nev = 'ILOK_COMMON'; Bovitheto = $false; Ertek = 'C:\Program Files\Common Files\PACE\Fusion\6\lib\' }
    @{ Nev = 'C_EM64T_REDIST11'; Bovitheto = $false; Ertek = 'C:\Program Files (x86)\Common Files\Intel\Shared Files\cpp\' }
    @{ Nev = 'POWERSHELL_DISTRIBUTION_CHANNEL'; Bovitheto = $false; Ertek = 'MSI:Windows 10 Pro' }
)

# --- A visszaállítandó Path (a mentés szerint + a Borland C++Builder 5 két mappája) ---
$pathSorok = @(
    '%C_EM64T_REDIST11%bin\Intel64'
    'C:\Program Files (x86)\Borland\Delphi7\Bin'
    'C:\Program Files (x86)\Borland\Delphi7\Projects\Bpl'
    'C:\Program Files (x86)\Embarcadero\Studio\37.0\bin'
    'C:\Users\Public\Documents\Embarcadero\Studio\37.0\Bpl'
    'C:\Program Files (x86)\Embarcadero\Studio\37.0\bin64'
    'C:\Users\Public\Documents\Embarcadero\Studio\37.0\Bpl\Win64'
    'C:\Program Files (x86)\Common Files\Intel\Shared Libraries\redist\intel64_win\compiler'
    'C:\Program Files\Common Files\Oracle\Java\javapath'
    'C:\Program Files\dotnet'
    'C:\WINDOWS'
    'C:\WINDOWS\system32'
    'C:\WINDOWS\System32\Wbem'
    'C:\WINDOWS\System32\WindowsPowerShell\v1.0'
    'C:\WINDOWS\System32\OpenSSH'
    'C:\Program Files (x86)\NVIDIA Corporation\PhysX\Common'
    'C:\Program Files (x86)\Pinnacle\Shared Files'
    'C:\Program Files\Calibre2'
    'c:\Program Files (x86)\Acustica\Framework'
    'c:\Program Files\Acustica\Framework'
    'C:\Program Files\NVIDIA Corporation\NVIDIA app\NvDLISR'
    'C:\Portable\Sjasmplus'
    'C:\Users\Viktor\AppData\Local\Muse Hub\lib'
    'C:\Program Files\Antelope Audio\Antelope Launcher'
    'C:\Program Files\Acustica\Medusa Framework'
    'C:\Program Files (x86)\HP\HP OCR\DB_Lib'
    'C:\Program Files\HP\Common\HPDestPlgIn'
    'C:\Program Files (x86)\HP\Common\HPDestPlgIn'
    'C:\Program Files (x86)\Common Files\Propellerhead Software\ReWire'
    'C:\Program Files\Common Files\Propellerhead Software\ReWire'
    'C:\Program Files\PyManager'
    '%USERPROFILE%\AppData\Local\Muse Hub\lib'
    'C:\Program Files (x86)\AOMEI\AOMEI Backupper\8.4.0'
    'C:\Program Files\nodejs'
    'C:\Program Files\PowerShell\7'
    'C:\Program Files\Virtual MIDI Cables\USBip'
    'C:\Program Files (x86)\Common Files\Acronis\SnapAPI'
    'C:\Program Files (x86)\Common Files\Acronis\VirtualFile'
    'C:\Program Files (x86)\Common Files\Acronis\VirtualFile64'
    'C:\Program Files (x86)\Common Files\Acronis\FileProtector'
    'C:\Program Files (x86)\Common Files\Acronis\FileProtector64'
    'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit'
    'C:\Program Files\Git\cmd'
    'C:\Program Files\TortoiseSVN\bin'
    '%ILOK_COMMON%'
    'C:\Program Files\WSL\'
    'C:\Borland\CBuilder5\Projects\Bpl'
    'C:\Borland\CBuilder5\Bin'
)

# A %NEV% hivatkozásokat ELŐBB a visszaállítandó változókból oldjuk fel (pl.
# %ILOK_COMMON% most hiányzik a gépről), utána a futó környezetből.
$terkep = @{}
foreach ($v in $valtozok) { $terkep[$v.Nev.ToUpper()] = $v.Ertek }

function Kibontas([string]$szoveg) {
    for ($i = 0; $i -lt 5; $i++) {
        $uj = [regex]::Replace($szoveg, '%([^%]+)%', {
            param($m)
            $nev = $m.Groups[1].Value
            if ($terkep.ContainsKey($nev.ToUpper())) { return $terkep[$nev.ToUpper()] }
            $e = [Environment]::GetEnvironmentVariable($nev)
            if ($e) { return $e }
            if ($nev -ieq 'SystemRoot') { return $env:windir }
            return $m.Value
        })
        if ($uj -eq $szoveg) { break }
        $szoveg = $uj
    }
    return $szoveg
}

function MappaLetezik([string]$ut) {
    $k = Kibontas $ut
    if ($k -match '%') { return $false }
    return (Test-Path -LiteralPath $k -PathType Container)
}

$sorok = New-Object System.Collections.Generic.List[string]
$sorok.Add("KÖRNYEZETI VÁLTOZÓK - ELLENŐRZÉS ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
$sorok.Add('')

# --- 1. Path ---
$maradPath = @(); $kiesikPath = @()
foreach ($p in $pathSorok) {
    if (MappaLetezik $p) { $maradPath += $p } else { $kiesikPath += $p }
}
$sorok.Add("1. PATH: $($pathSorok.Count) sorból $($maradPath.Count) marad, $($kiesikPath.Count) kiesik")
$sorok.Add('')
$sorok.Add('   MEGVAN:')
foreach ($p in $maradPath) {
    $k = Kibontas $p
    $sorok.Add("     [OK]      $p" + $(if ($k -ne $p) { "   (= $k)" } else { '' }))
}
$sorok.Add('')
$sorok.Add('   NEM LÉTEZIK (kimarad a szűrt .reg-ből):')
if ($kiesikPath.Count -eq 0) { $sorok.Add('     - nincs ilyen -') }
foreach ($p in $kiesikPath) {
    $k = Kibontas $p
    $sorok.Add("     [HIÁNYZIK] $p" + $(if ($k -ne $p) { "   (= $k)" } else { '' }))
}
$sorok.Add('')

# --- 2. Mappát mutató változók (pl. C:\...); a többi (szám, szöveg) mindig marad ---
$maradValt = @(); $kiesikValt = @()
foreach ($v in $valtozok) {
    # Több mappát is tartalmazhat (pl. PSModulePath): ha BÁRMELYIK létezik, marad.
    $reszek = @((Kibontas $v.Ertek) -split ';' | Where-Object { $_ })
    $mappak = @($reszek | Where-Object { ($_ -match '^[A-Za-z]:\\') -and ($_ -notmatch '\.[A-Za-z0-9]{2,4}$') })
    $vanLetezo = @($mappak | Where-Object { Test-Path -LiteralPath $_ -PathType Container }).Count -gt 0
    if ($mappak.Count -gt 0 -and -not $vanLetezo) { $kiesikValt += $v } else { $maradValt += $v }
}
$sorok.Add("2. TÖBBI VÁLTOZÓ: $($valtozok.Count) db-ból $($maradValt.Count) marad, $($kiesikValt.Count) kiesik")
$sorok.Add('   (csak a mappát mutatókat ellenőrzi; a fájlt mutatók, pl. ComSpec, és a nem útvonal értékek mindig maradnak)')
$sorok.Add('')
$sorok.Add('   NEM LÉTEZŐ MAPPÁRA MUTAT (kimarad a szűrt .reg-ből):')
if ($kiesikValt.Count -eq 0) { $sorok.Add('     - nincs ilyen -') }
foreach ($v in $kiesikValt) { $sorok.Add("     [HIÁNYZIK] $($v.Nev) = $($v.Ertek)") }
$sorok.Add('')

# --- A szűrt .reg fájl ---
function Hex2([string]$s) {
    $b = [Text.Encoding]::Unicode.GetBytes($s + [char]0)
    return 'hex(2):' + (($b | ForEach-Object { '{0:x2}' -f $_ }) -join ',')
}
function RegSzoveg([string]$s) { return $s.Replace('\', '\\').Replace('"', '\"') }

$reg = New-Object System.Collections.Generic.List[string]
$reg.Add('Windows Registry Editor Version 5.00')
$reg.Add('')
$reg.Add('[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment]')
foreach ($v in $maradValt) {
    if ($v.Bovitheto) { $reg.Add('"' + (RegSzoveg $v.Nev) + '"=' + (Hex2 $v.Ertek)) }
    else { $reg.Add('"' + (RegSzoveg $v.Nev) + '"="' + (RegSzoveg $v.Ertek) + '"') }
}
$reg.Add('"Path"=' + (Hex2 ($maradPath -join ';')))

$asztal = [Environment]::GetFolderPath('Desktop')
$regFajl = Join-Path $asztal 'kornyezeti_valtozok_szurt.reg'
$txtFajl = Join-Path $asztal 'ellenorzes_eredmenye.txt'
[IO.File]::WriteAllText($regFajl, (($reg -join "`r`n") + "`r`n"), [Text.Encoding]::Unicode)
$sorok.Add("Elkészült: $regFajl")
$sorok.Add('Importálás: dupla kattintás a fájlon, majd a gép újraindítása.')
[IO.File]::WriteAllText($txtFajl, (($sorok -join "`r`n") + "`r`n"), (New-Object Text.UTF8Encoding($true)))

# --- Kiírás a képernyőre is ---
foreach ($s in $sorok) {
    if ($s -match '\[HIÁNYZIK\]') { Write-Host $s -ForegroundColor Yellow }
    elseif ($s -match '\[OK\]') { Write-Host $s -ForegroundColor Green }
    else { Write-Host $s }
}
Write-Host ''
Write-Host "Az eredmény az Asztalon: ellenorzes_eredmenye.txt" -ForegroundColor Cyan
Read-Host 'Nyomj Entert a bezáráshoz'
