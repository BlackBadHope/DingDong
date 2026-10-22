# DingDong project tool source. Licensing: see repository LEGAL-STATUS.md.
# Source-only inventory. Never downloads, copies, enables, builds or launches mods.
[CmdletBinding()]
param(
    [string]$ReferenceManifest,
    [string]$SteamPath,
    [string]$ReportPath,
    [switch]$OpenWorkshopPages,
    [ValidateRange(1,20)][int]$MaxOpenPages=5
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
if(!$ReferenceManifest){$ReferenceManifest=Join-Path $PSScriptRoot '..\manifests\r22-reference.json'}

function Assert-DDRegularPath([string]$Path) {
    $current=[IO.Path]::GetFullPath($Path)
    while($current) {
        if(Test-Path -LiteralPath $current) {
            if((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Steam source paths must not contain junctions or symbolic links.' }
        }
        $next=Split-Path -Parent $current
        if($next -eq $current){break};$current=$next
    }
}
function Read-DDBoundedString([IO.BinaryReader]$Reader,[int]$Maximum=256) {
    $length=0; $shift=0
    do {
        if($shift -gt 28){throw 'Invalid TMOD string length.'}
        $value=$Reader.ReadByte();$length=$length -bor (($value -band 127) -shl $shift);$shift+=7
    } while($value -band 128)
    if($length -lt 0 -or $length -gt $Maximum){throw 'TMOD metadata string is too large.'}
    $bytes=$Reader.ReadBytes($length)
    if($bytes.Length -ne $length){throw 'Truncated TMOD metadata.'}
    (New-Object Text.UTF8Encoding($false,$true)).GetString($bytes)
}
function Read-DDTmodIdentity([string]$Path) {
    Assert-DDRegularPath $Path
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    $reader=New-Object IO.BinaryReader($stream)
    try {
        if([Text.Encoding]::ASCII.GetString($reader.ReadBytes(4)) -cne 'TMOD'){throw 'Not a TMOD package.'}
        $tml=[version](Read-DDBoundedString $reader 80)
        if($tml -lt [version]'0.11'){throw 'Legacy compressed TMOD metadata is unsupported.'}
        $null=$reader.ReadBytes(20);$signature=$reader.ReadBytes(256)
        if($signature.Length -ne 256){throw 'Truncated TMOD signature field.'}
        $dataLength=$reader.ReadInt32()
        if($dataLength -lt 0 -or $dataLength -ne ($stream.Length-$stream.Position)){throw 'TMOD data length mismatch.'}
        $name=Read-DDBoundedString $reader 256
        $version=[version](Read-DDBoundedString $reader 80)
        if($name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,255}$'){throw 'Invalid internal mod name.'}
        [pscustomobject]@{name=$name;version=[string]$version;tModLoaderVersion=[string]$tml;bytes=$stream.Length}
    } finally {$reader.Dispose();$stream.Dispose()}
}
function Get-DDSteamLayout([string]$Root) {
    if(!$Root) {
        $registered=Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
        if($registered -and $registered.PSObject.Properties['SteamPath']){$Root=[string]$registered.SteamPath}
        elseif(${env:ProgramFiles(x86)}){$Root=Join-Path ${env:ProgramFiles(x86)} 'Steam'}
    }
    if(!$Root){throw 'Steam installation is not registered. Install Steam and its official Terraria/tModLoader apps first.'}
    $Root=[IO.Path]::GetFullPath($Root).TrimEnd('\'); Assert-DDRegularPath $Root
    $vdf=Join-Path $Root 'steamapps\libraryfolders.vdf'
    if(!(Test-Path -LiteralPath (Join-Path $Root 'steam.exe') -PathType Leaf) -or !(Test-Path -LiteralPath $vdf -PathType Leaf)){throw 'SteamPath must be an installed Steam client root with steam.exe and steamapps/libraryfolders.vdf.'}
    Assert-DDRegularPath $vdf
    $libraries=@($Root)
    foreach($match in [regex]::Matches([IO.File]::ReadAllText($vdf),'"path"\s+"([^"\r\n]+)"')) {
        $library=[IO.Path]::GetFullPath($match.Groups[1].Value.Replace('\\','\')).TrimEnd('\')
        Assert-DDRegularPath $library; $libraries+=$library
    }
    $libraries=@($libraries | Select-Object -Unique)
    $apps=@(foreach($app in @(@{id='105600';file='Terraria.exe'},@{id='1281930';file='tModLoader.dll'})) {
        $found=@(foreach($library in $libraries) {
            $manifest=Join-Path $library ('steamapps\appmanifest_'+$app.id+'.acf')
            if(!(Test-Path -LiteralPath $manifest -PathType Leaf)){continue}
            Assert-DDRegularPath $manifest
            $acf=[IO.File]::ReadAllText($manifest)
            if($acf -notmatch ('"appid"\s+"'+$app.id+'"')){continue}
            $match=[regex]::Match($acf,'"installdir"\s+"([^"\r\n]+)"')
            if(!$match.Success -or $match.Groups[1].Value -notmatch '^[A-Za-z0-9][A-Za-z0-9 ._-]{0,100}$' -or $match.Groups[1].Value -match '[ .]$'){throw 'Unsafe official app installation directory.'}
            $path=Join-Path $library ('steamapps\common\'+$match.Groups[1].Value)
            Assert-DDRegularPath $path
            if(Test-Path -LiteralPath (Join-Path $path $app.file) -PathType Leaf){$path}
        })
        [pscustomobject]@{appId=$app.id;installed=($found.Count -gt 0);paths=$found;storeUrl=('https://store.steampowered.com/app/'+$app.id+'/')}
    })
    [pscustomobject]@{steamRoot=$Root;libraries=$libraries;apps=$apps;ready=(@($apps | Where-Object {!$_.installed}).Count -eq 0);validation='registered/explicit Steam root, official libraryfolders and appmanifest layout; no ownership or binary-signature attestation'}
}
function Get-DDWorkshopFiles([string]$ItemRoot) {
    if(!(Test-Path -LiteralPath $ItemRoot -PathType Container)){return}
    Assert-DDRegularPath $ItemRoot
    $queue=New-Object 'Collections.Generic.Queue[object]'
    $queue.Enqueue(@{path=$ItemRoot;depth=0});$count=0
    while($queue.Count) {
        $dir=$queue.Dequeue()
        foreach($item in Get-ChildItem -LiteralPath $dir.path -Force) {
            if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Workshop source contains a junction or symbolic link.'}
            if($item.PSIsContainer){if($dir.depth -lt 4){$queue.Enqueue(@{path=$item.FullName;depth=$dir.depth+1})};continue}
            if($item.Extension -ine '.tmod'){continue}
            $count++;if($count -gt 256){throw 'Workshop item exceeds the bounded package inventory limit.'}
            $item
        }
    }
}
function Resolve-DDReferenceMod($Mod,$Hint,[string[]]$Libraries) {
    $id=[string]$Mod.workshopId
    if(!$id -and $Hint){$id=[string]$Hint.workshopId}
    $url=if($id){'https://steamcommunity.com/sharedfiles/filedetails/?id='+$id}else{$null}
    $result=[ordered]@{name=$Mod.name;expectedSha256=$Mod.sha256;expectedBytes=[long]$Mod.bytes;workshopId=$id;officialSource=$url;status='UNRESOLVED_OFFICIAL_SOURCE';selectedPath=$null;observed=@();action='No exact primary source is known. Obtain an owner-reviewed source/provenance decision; do not substitute another mod.'}
    if(!$id){return [pscustomobject]$result}
    if($id -notmatch '^\d{8,12}$'){throw 'Invalid Workshop item ID.'}
    $observed=@();$exact=@()
    foreach($library in $Libraries) {
        $itemRoot=Join-Path $library ('steamapps\workshop\content\1281930\'+$id)
        foreach($file in @(Get-DDWorkshopFiles $itemRoot)) {
            try {$metadata=Read-DDTmodIdentity $file.FullName}
            catch {$observed += [pscustomobject]@{path=$file.FullName;state='INVALID_OR_UNSUPPORTED_TMOD';reason=$_.Exception.Message};continue}
            if($metadata.name -cne $Mod.name){$observed += [pscustomobject]@{path=$file.FullName;name=$metadata.name;state='INTERNAL_NAME_MISMATCH'};continue}
            $hash=$null
            if($metadata.bytes -eq [long]$Mod.bytes){$hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
            $match=$hash -and $hash -ceq $Mod.sha256.ToLowerInvariant()
            $observed += [pscustomobject]@{path=$file.FullName;name=$metadata.name;version=$metadata.version;tModLoaderVersion=$metadata.tModLoaderVersion;bytes=$metadata.bytes;sha256=$hash;state=$(if($match){'EXACT'}elseif($hash){'SHA256_MISMATCH'}else{'SIZE_MISMATCH_NOT_HASHED'})}
            if($match){$exact += $file.FullName}
        }
    }
    $result.observed=$observed
    if($exact.Count){$result.status='AVAILABLE_EXACT';$result.selectedPath=@($exact | Sort-Object)[0];$result.action='Exact referenced bytes found in an official Steam Workshop item path. No file was copied or enabled.'}
    elseif($observed.Count){$result.status='INCOMPATIBLE_OFFICIAL';$result.action='Installed official item does not match the pinned internal name/bytes/SHA256. It may be newer, older, or the reference may contain modified bytes. Stop; obtain a reviewed compatible official version or explicit reference update. Never select the latest version silently.'}
    else{$result.status='MISSING_OFFICIAL';$result.action='Open the listed Workshop page, subscribe/download through Steam yourself, then rerun. Current Workshop versions may still differ from this historical reference.'}
    [pscustomobject]$result
}
function Invoke-DDDependencyInventory([string]$Manifest,[string]$SteamRoot,[string]$Hints=(Join-Path $PSScriptRoot 'official-sources.json')) {
    $reference=Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
    if($reference.format -cne 'dingdong-reference-inventory-v1' -or $reference.tModLoaderAppId -ne 1281930){throw 'Unsupported reference manifest.'}
    $hintData=Get-Content -LiteralPath $Hints -Raw | ConvertFrom-Json
    if($hintData.format -cne 'dingdong-official-source-hints-v1'){throw 'Unsupported official source hints.'}
    $layout=Get-DDSteamLayout $SteamRoot
    $seen=@{};$mods=@(foreach($mod in $reference.mods) {
        if($mod.name -ceq 'DingDongBiomeHomes'){continue}
        if($mod.name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,255}$' -or $mod.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or [long]$mod.bytes -le 0 -or $seen.ContainsKey($mod.name)){throw 'Invalid or duplicate reference dependency.'}
        $seen[$mod.name]=$true
        $hint=@($hintData.sources | Where-Object name -ceq $mod.name)
        if($hint.Count -gt 1){throw 'Duplicate official source hint.'}
        Resolve-DDReferenceMod $mod $(if($hint.Count){$hint[0]}else{$null}) $layout.libraries
    })
    $blocked=@($mods | Where-Object status -ne 'AVAILABLE_EXACT')
    [pscustomobject]@{format='dingdong-official-dependency-inventory-v1';utc=[DateTime]::UtcNow.ToString('o');mode='SOURCE_ONLY_READ_ONLY';referenceSha256=(Get-FileHash -LiteralPath $Manifest -Algorithm SHA256).Hash.ToLowerInvariant();steam=$layout;ownDingDongExcluded=$true;dependencies=$mods;exactCount=@($mods | Where-Object status -eq 'AVAILABLE_EXACT').Count;blockedCount=$blocked.Count;readiness=$(if($layout.ready -and !$blocked.Count){'DEPENDENCIES_RESOLVED_ONLY'}else{'BLOCKED_OFFICIAL_DEPENDENCIES'});completeCleanInstall=$false;copied=$false;enabled=$false;gameLaunched=$false}
}
function Open-DDMissingWorkshopPages($Inventory,[int]$Limit) {
    $urls=@($Inventory.dependencies | Where-Object {$_.status -ne 'AVAILABLE_EXACT' -and $_.officialSource} | ForEach-Object {$_.officialSource} | Select-Object -Unique -First $Limit)
    foreach($url in $urls){Start-Process -FilePath $url}
}
if($MyInvocation.InvocationName -ne '.') {
    try {
        $inventory=Invoke-DDDependencyInventory $ReferenceManifest $SteamPath
        if($ReportPath){[IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath),($inventory | ConvertTo-Json -Depth 14),(New-Object Text.UTF8Encoding($false)))}
        Write-Host ('Source-only dependency check: '+$inventory.exactCount+' exact, '+$inventory.blockedCount+' blocked; '+$inventory.readiness)
        $inventory.dependencies | Where-Object status -ne 'AVAILABLE_EXACT' | Select-Object name,status,officialSource,action | Format-List | Out-Host
        foreach($app in $inventory.steam.apps | Where-Object {!$_.installed}){Write-Host ('Install official Steam app '+$app.appId+': '+$app.storeUrl)}
        if($OpenWorkshopPages){Open-DDMissingWorkshopPages $inventory $MaxOpenPages}
        Write-Host 'No dependency was downloaded, copied or enabled. This is not a complete clean installation.'
        if($inventory.readiness -eq 'BLOCKED_OFFICIAL_DEPENDENCIES'){exit 2};exit 0
    } catch {Write-Host $_.Exception.Message -ForegroundColor Yellow;exit 1}
}
