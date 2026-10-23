# DingDong project tool source. Licensing: see repository LEGAL-STATUS.md.
# Official/local dependency preparation. Default mode reads only.
[CmdletBinding()]
param(
    [string]$PublicManifest,
    [string]$SteamPath,
    [string]$ExistingModDirectory,
    [switch]$DownloadAuthorAssets,
    [switch]$StageDependencies,
    [string]$ProfilePath,
    [string]$ReportPath,
    [switch]$OpenWorkshopPages,
    [ValidateRange(1,20)][int]$MaxOpenPages=5
)
$ErrorActionPreference='Stop'
if(!$PublicManifest){$PublicManifest=Join-Path $PSScriptRoot 'public-dependencies.json'}
. (Join-Path $PSScriptRoot 'Resolve-OfficialDependencies.ps1')
. (Join-Path $PSScriptRoot 'Build-OfficialOre.ps1')

function Get-DDStructureHelperPin {
    [pscustomobject]@{name='StructureHelper';version='3.1.1';bytes=125868L;sha256='29ae998124f3bb7ba4d064f74b8bd48962864f211f701b83dcd55ee23c0aa078';url='https://github.com/ScalarVector1/StructureHelper/releases/download/3.1.1/StructureHelper.tmod'}
}
function Get-DDAuthorPin([string]$Name) {
    if($Name -ceq 'StructureHelper'){return Get-DDStructureHelperPin}
    throw 'Unreviewed author package.'
}
function Read-DDPublicManifest([string]$Path) {
    $data=Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if($data.format -cne 'dingdong-public-dependencies-v1' -or $data.tModLoaderAppId -ne 1281930){throw 'Unsupported public dependency manifest.'}
    $excluded=@($data.excludedMods | Sort-Object)
    if(($excluded -join ',') -cne 'DingDongBiomeHomes,PackWorldgenFix'){throw 'Public preset exclusions are invalid.'}
    $seen=@{};$pin=Get-DDStructureHelperPin
    foreach($mod in $data.mods) {
        if($mod.name -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,255}$' -or $mod.name -in $excluded -or $seen.ContainsKey($mod.name) -or $mod.sha256 -cnotmatch '^[a-f0-9]{64}$' -or [long]$mod.bytes -le 0){throw 'Invalid or duplicate public dependency identity.'}
        $seen[$mod.name]=$true
        if($mod.name -ceq 'OreExcavator') {
            $ore=Get-DDOreSourcePin
            if($mod.sourceKind -cne 'author-source-build' -or $mod.assetUrl -cne $ore.url -or $mod.sourceSha256 -cne $ore.sourceSha256 -or $mod.sourceCommit -cne $ore.commit -or $mod.sha256 -cne $ore.sha256 -or [long]$mod.bytes -ne $ore.bytes){throw 'Ore source build recipe must match the exact reviewed author and deterministic output.'}
        } elseif($mod.name -ceq 'StructureHelper') {
            $pin=Get-DDAuthorPin $mod.name
            if($mod.sourceKind -cne 'author-github-release' -or $mod.assetUrl -cne $pin.url -or $mod.sha256 -cne $pin.sha256 -or [long]$mod.bytes -ne $pin.bytes){throw 'Author package must use its exact reviewed release identity.'}
        } elseif($mod.sourceKind -cne 'steam-workshop' -or [string]$mod.workshopId -notmatch '^\d{8,12}$'){throw 'Dependency must use its declared official Steam Workshop item.'}
    }
    if(!$seen.Count -or $seen.Count -gt 512){throw 'Invalid public dependency count.'}
    $data
}
function Get-DDLocalCandidates([string]$Directory) {
    if(!$Directory){return}
    Assert-DDRegularPath $Directory
    if(!(Test-Path -LiteralPath $Directory -PathType Container)){throw 'ExistingModDirectory must be an explicitly selected existing Mods folder.'}
    $files=@(Get-ChildItem -LiteralPath $Directory -File -Filter '*.tmod')
    if($files.Count -gt 256){throw 'Local Mods folder exceeds the bounded package limit.'}
    foreach($file in $files) {
        try {$identity=Read-DDTmodIdentity $file.FullName;[pscustomobject]@{path=$file.FullName;identity=$identity}}
        catch {Write-Verbose ('Ignored invalid local package '+$file.Name)}
    }
}
function Find-DDExactLocal($Mod,[object[]]$Candidates) {
    foreach($candidate in $Candidates) {
        if($candidate.identity.name -cne $Mod.name -or $candidate.identity.bytes -ne [long]$Mod.bytes){continue}
        if((Get-FileHash -LiteralPath $candidate.path -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $Mod.sha256){return $candidate.path}
    }
}
function Assert-DDExactPackage([string]$Path,$Identity) {
    $metadata=Read-DDTmodIdentity $Path
    if($metadata.name -cne $Identity.name -or $metadata.bytes -ne [long]$Identity.bytes -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Identity.sha256){throw ('Package identity mismatch: '+$Identity.name)}
    if($Identity.PSObject.Properties['version'] -and $metadata.version -cne $Identity.version){throw 'Author package version mismatch.'}
}
function Assert-DDReleaseAssetUri([uri]$Uri) {
    if($Uri.Scheme -cne 'https' -or !$Uri.IsDefaultPort -or $Uri.UserInfo -or $Uri.Fragment -or $Uri.DnsSafeHost -notin @('github.com','release-assets.githubusercontent.com','objects.githubusercontent.com')){throw 'Author asset redirect is outside allowed GitHub release storage.'}
}
function Open-DDAssetResponse([uri]$Uri) {
    $request=[Net.HttpWebRequest]::Create($Uri);$request.AllowAutoRedirect=$false;$request.UserAgent='DingDong-Public-Dependency-Preflight';$request.Timeout=30000;$request.ReadWriteTimeout=30000
    $request.GetResponse()
}
function Receive-DDStructureHelper([string]$Directory,[string[]]$ForbiddenRoots,[string]$Name='StructureHelper') {
    $pin=Get-DDAuthorPin $Name;$directoryFull=[IO.Path]::GetFullPath($Directory).TrimEnd('\');Assert-DDRegularPath $directoryFull
    foreach($root in $ForbiddenRoots){if(!$root){continue};$rootFull=[IO.Path]::GetFullPath($root).TrimEnd('\');if($directoryFull -ieq $rootFull -or $directoryFull.StartsWith($rootFull+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Author download cache must be outside Steam app data and the live Terraria profile.'}}
    [void][IO.Directory]::CreateDirectory($directoryFull)
    $target=Join-Path $directoryFull ($pin.name+'.tmod')
    if(Test-Path -LiteralPath $target){Assert-DDExactPackage $target $pin;return $target}
    $partial=Join-Path $directoryFull ('.'+$pin.name+'-'+[Guid]::NewGuid().ToString('N')+'.part')
    $tls=[Net.ServicePointManager]::SecurityProtocol
    try {
        [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
        $uri=[uri]$pin.url;$done=$false
        for($redirect=0;$redirect -le 6;$redirect++) {
            Assert-DDReleaseAssetUri $uri;$response=Open-DDAssetResponse $uri
            try {
                if([int]$response.StatusCode -in @(301,302,303,307,308)) {
                    if($redirect -eq 6 -or !$response.Headers['Location']){throw 'Too many or invalid author asset redirects.'}
                    $uri=[uri]::new($uri,[string]$response.Headers['Location']);Assert-DDReleaseAssetUri $uri;continue
                }
                if([int]$response.StatusCode -ne 200){throw 'Author asset did not return HTTP 200.'}
                $inputStream=$response.GetResponseStream();$outputStream=[IO.File]::Open($partial,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
                try {$buffer=New-Object byte[] 32768;$total=0L;while(($read=$inputStream.Read($buffer,0,$buffer.Length)) -gt 0){$total+=$read;if($total -gt $pin.bytes){throw 'Author asset exceeds its pinned byte count.'};$outputStream.Write($buffer,0,$read)}}finally{$outputStream.Dispose();$inputStream.Dispose()}
                Assert-DDExactPackage $partial $pin
                Move-Item -LiteralPath $partial -Destination $target;$done=$true;break
            } finally {$response.Dispose()}
        }
        if(!$done){throw 'Author asset download did not complete.'};$target
    } finally {[Net.ServicePointManager]::SecurityProtocol=$tls;if(Test-Path -LiteralPath $partial){Remove-Item -LiteralPath $partial -Force}}
}
function Invoke-DDPublicPreflight([string]$Manifest,[string]$SteamRoot,[string]$LocalMods,[string]$AuthorDirectory) {
    $policy=Read-DDPublicManifest $Manifest;$layout=Get-DDSteamLayout $SteamRoot
    $local=@(Get-DDLocalCandidates $LocalMods);$author=@(Get-DDLocalCandidates $AuthorDirectory)
    $rows=@(foreach($mod in $policy.mods) {
        $row=Resolve-DDReferenceMod $mod $null $layout.libraries
        $row | Add-Member NoteProperty sourceKind $mod.sourceKind
        $row | Add-Member NoteProperty selectedSource $(if($row.status -ceq 'AVAILABLE_EXACT'){'official-steam-workshop'}else{$null})
        if($row.status -cne 'AVAILABLE_EXACT' -and $LocalMods){$found=Find-DDExactLocal $mod $local;if($found){$row.status='AVAILABLE_EXACT';$row.selectedPath=$found;$row.selectedSource='user-selected-local-migration';$row.action='Exact user-selected local package; local migration only, no redistribution permission.'}}
        if($mod.sourceKind -ceq 'author-github-release') {
            $row.officialSource=$mod.assetUrl
            if($row.status -cne 'AVAILABLE_EXACT'){$found=Find-DDExactLocal $mod $author;if($found){Assert-DDExactPackage $found (Get-DDAuthorPin $mod.name);$row.status='AVAILABLE_EXACT';$row.selectedPath=$found;$row.selectedSource='verified-author-release-cache';$row.action='Exact directly downloaded author release; no mirror.'}else{$row.status='MISSING_AUTHOR_ASSET';$row.action='Use -DownloadAuthorAssets to fetch only the pinned author release into the isolated bootstrap cache, then rerun.'}}
        } elseif($mod.sourceKind -ceq 'author-source-build') {
            $row.officialSource=$mod.assetUrl
            if($row.status -cne 'AVAILABLE_EXACT'){$found=Find-DDExactLocal $mod $author;if($found){Assert-DDExactPackage $found (Get-DDOreSourcePin);$row.status='AVAILABLE_EXACT';$row.selectedPath=$found;$row.selectedSource='pinned-author-source-local-build';$row.action='Deterministic local compilation from the pinned author source, not a mirror.'}else{$row.status='MISSING_LOCAL_SOURCE_BUILD';$row.action='Run Install-DingDong.cmd to download and compile the exact author source with official tModLoader compiler libraries.'}}
        }
        $row
    })
    $blocked=@($rows | Where-Object status -cne 'AVAILABLE_EXACT')
    [pscustomobject]@{format='dingdong-public-preflight-v1';utc=[DateTime]::UtcNow.ToString('o');manifestSha256=(Get-FileHash -LiteralPath $Manifest -Algorithm SHA256).Hash.ToLowerInvariant();steam=$layout;excludedMods=@($policy.excludedMods);dependencies=$rows;exactCount=@($rows | Where-Object status -ceq 'AVAILABLE_EXACT').Count;blockedCount=$blocked.Count;readiness=$(if($layout.ready -and !$blocked.Count){'DEPENDENCIES_RESOLVED_ONLY'}else{'BLOCKED_OFFICIAL_DEPENDENCIES'});completeCleanInstall=$false;copied=$false;enabled=$false;gameLaunched=$false}
}
function Stage-DDPublicDependencies($Inventory,[string]$Target) {
    if($Inventory.readiness -cne 'DEPENDENCIES_RESOLVED_ONLY'){throw 'All official/local pinned dependencies and Steam prerequisites must resolve before staging.'}
    if(!$Target){throw '-StageDependencies requires -ProfilePath pointing to a new or empty profile.'}
    $profile=[IO.Path]::GetFullPath($Target).TrimEnd('\');Assert-DDRegularPath $profile;$parent=Split-Path -Parent $profile
    if(!$parent -or !(Test-Path -LiteralPath $parent -PathType Container)){throw 'The selected profile parent must already exist.'}
    foreach($library in $Inventory.steam.libraries){$protected=[IO.Path]::GetFullPath((Join-Path $library 'steamapps')).TrimEnd('\');if($profile -ieq $protected -or $profile.StartsWith($protected+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Dependency staging must not write into the official Steam library data.'}}
    $existed=Test-Path -LiteralPath $profile
    if($existed -and (!(Test-Path -LiteralPath $profile -PathType Container) -or @(Get-ChildItem -LiteralPath $profile -Force).Count)){throw 'ProfilePath must be new or completely empty; existing profiles and saves are never merged or replaced.'}
    $temporary=Join-Path $parent ('.dingdong-dependencies-'+[Guid]::NewGuid().ToString('N'));$committed=$false
    try {
        $mods=Join-Path $temporary 'Mods';[void][IO.Directory]::CreateDirectory($mods)
        foreach($row in $Inventory.dependencies){$identity=[pscustomobject]@{name=$row.name;bytes=$row.expectedBytes;sha256=$row.expectedSha256};Assert-DDExactPackage $row.selectedPath $identity;$copy=Join-Path $mods ($row.name+'.tmod');Copy-Item -LiteralPath $row.selectedPath -Destination $copy;Assert-DDExactPackage $copy $identity}
        $names=@(@($Inventory.dependencies.name)+@('DingDongBiomeHomes') | Sort-Object)
        [IO.File]::WriteAllText((Join-Path $mods 'enabled.json'),(ConvertTo-Json -InputObject $names),(New-Object Text.UTF8Encoding($false)))
        $receipt=[pscustomobject]@{format='dingdong-dependency-stage-v1';manifestSha256=$Inventory.manifestSha256;dependencyCount=$Inventory.dependencies.Count;ownedModPending=$true;completeCleanInstall=$false;gameLaunched=$false}
        [IO.File]::WriteAllText((Join-Path $temporary 'dependency-stage.json'),($receipt|ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))
        if($existed){Remove-Item -LiteralPath $profile}
        Move-Item -LiteralPath $temporary -Destination $profile;$committed=$true
        [pscustomobject]@{profilePath=$profile;externalDependenciesStaged=$Inventory.dependencies.Count;ownedModPending=$true;completeCleanInstall=$false}
    } finally {
        if(!$committed -and (Test-Path -LiteralPath $temporary)) {
            $resolved=[IO.Path]::GetFullPath($temporary);$expectedParent=[IO.Path]::GetFullPath($parent).TrimEnd('\')+'\'
            if(!$resolved.StartsWith($expectedParent,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $resolved) -notmatch '^\.dingdong-dependencies-[a-f0-9]{32}$'){throw 'Refusing unsafe staging cleanup.'}
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
        if(!$committed -and $existed -and !(Test-Path -LiteralPath $profile)){[void][IO.Directory]::CreateDirectory($profile)}
    }
}
if($MyInvocation.InvocationName -ne '.') {
    try {
        $cache=Join-Path $PSScriptRoot 'output\author-assets'
        if($DownloadAuthorAssets) {
            $layout=Get-DDSteamLayout $SteamPath;$forbidden=@($layout.libraries | ForEach-Object {Join-Path $_ 'steamapps'})
            $documents=[Environment]::GetFolderPath('MyDocuments');if($documents){$forbidden+=Join-Path $documents 'My Games\Terraria'}
            $policy=Read-DDPublicManifest $PublicManifest
            foreach($mod in $policy.mods | Where-Object sourceKind -ceq 'author-github-release'){$null=Receive-DDStructureHelper $cache $forbidden $mod.name}
        }
        $cacheInput=if(Test-Path -LiteralPath $cache -PathType Container){$cache}else{$null}
        $inventory=Invoke-DDPublicPreflight $PublicManifest $SteamPath $ExistingModDirectory $cacheInput
        if($StageDependencies){$stage=Stage-DDPublicDependencies $inventory $ProfilePath;$inventory.copied=$true;$inventory.enabled=$true;$inventory|Add-Member NoteProperty stage $stage}
        if($ReportPath){[IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath),($inventory|ConvertTo-Json -Depth 14),(New-Object Text.UTF8Encoding($false)))}
        Write-Host ('Public dependency preflight: '+$inventory.exactCount+' exact, '+$inventory.blockedCount+' blocked; '+$inventory.readiness)
        $inventory.dependencies|Where-Object status -cne 'AVAILABLE_EXACT'|Select-Object name,status,officialSource,action|Format-List|Out-Host
        if($OpenWorkshopPages){$links=[pscustomobject]@{dependencies=@($inventory.dependencies|Where-Object sourceKind -ceq 'steam-workshop')};Open-DDMissingWorkshopPages $links $MaxOpenPages}
        Write-Host 'This does not install the own mod/engine or prove a complete clean installation. No game or Steam download was launched.'
        if($inventory.readiness -cne 'DEPENDENCIES_RESOLVED_ONLY'){exit 2};exit 0
    } catch {Write-Host $_.Exception.Message -ForegroundColor Yellow;exit 1}
}
