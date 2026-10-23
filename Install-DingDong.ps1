# DingDong GitHub setup: signed own bundle + local Steam runtime + official mods.
[CmdletBinding()]
param([string]$InstallPath,[string]$SteamPath,[string]$ExistingModDirectory,
      [switch]$DownloadOnly,[string]$RollbackBackup,[switch]$NonInteractive)
$ErrorActionPreference='Stop'
$setupRoot=$PSScriptRoot
$selectedSteamPath=$SteamPath;$selectedExistingMods=$ExistingModDirectory
$library=Join-Path $setupRoot 'updater\DingDong-Update.ps1'
if(!(Test-Path -LiteralPath $library)){$library=Join-Path $setupRoot 'DingDong-Update.ps1'}
if(!(Test-Path -LiteralPath $library)){$library=Join-Path $setupRoot 'GitHub-Transport.ps1'}
. $library -DownloadOnly:$DownloadOnly -RollbackBackup $RollbackBackup -NonInteractive:$NonInteractive
$core=Join-Path $setupRoot 'bootstrap\Install-Core.ps1'
if(!(Test-Path -LiteralPath $core)){$core=Join-Path $setupRoot 'Install-Core.ps1'}
$resolver=Join-Path (Split-Path -Parent $core) 'Resolve-OfficialDependencies.ps1'
. $resolver -SteamPath $selectedSteamPath
. $core
$script:GitHubOwner='BlackBadHope';$script:GitHubRepository='DingDong'
if(!$InstallPath){$InstallPath=Join-Path $env:LOCALAPPDATA 'DingDong-GitHub'}
function Invoke-DDSetup {
$lock=$null;$result=0
try {
    $root=[IO.Path]::GetFullPath($InstallPath).TrimEnd('\');Assert-DDRegularPath $root
    if($RollbackBackup){$null=Assert-DDInstallRoot $root @($setupRoot);$r=Restore-DDInstall $root $RollbackBackup;Write-Host ('Rollback complete: '+$r.backup);return 0}
    # Fetching a signed plan and own bundle needs no target installation or Steam.
    $cache=Join-Path $setupRoot 'Downloads';[void][IO.Directory]::CreateDirectory($cache)
    $lock=[IO.File]::Open((Join-Path $cache '.installer.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $release=Get-GitHubRelease $cache
    $asset=Get-ReleaseAsset $release 'install.json'
    if($asset.Bytes -gt 4MB){throw 'Signed installation plan is too large.'}
    $planPath=Join-Path $cache 'install.json';Fetch $asset.Uri ($planPath+'.part') $asset.Bytes
    if((Get-Item -LiteralPath ($planPath+'.part')).Length -ne $asset.Bytes){throw 'Installation plan size mismatch.'}
    $plan=Read-VerifiedEnvelope ($planPath+'.part');Assert-DDInstallPlan $plan $release
    Move-Item -LiteralPath ($planPath+'.part') -Destination $planPath -Force
    $bundle=Get-ReleaseAsset $release $plan.bundle.file
    if($bundle.Bytes -ne [long]$plan.bundle.bytes){throw 'Signed own bundle size differs from GitHub.'}
    $zip=Join-Path $cache $plan.bundle.file
    if(!(Test-Path -LiteralPath $zip) -or (Get-Item -LiteralPath $zip).Length -ne [long]$plan.bundle.bytes -or (Hash $zip) -cne $plan.bundle.sha256){
        Write-Host ('Downloading DingDong '+$plan.version+' from GitHub...')
        Fetch $bundle.Uri ($zip+'.part') $bundle.Bytes
        if((Get-Item -LiteralPath ($zip+'.part')).Length -ne $bundle.Bytes -or (Hash ($zip+'.part')) -cne $plan.bundle.sha256){throw 'Own bundle checksum mismatch.'}
        Move-Item -LiteralPath ($zip+'.part') -Destination $zip -Force
    }
    $stage=Join-Path $cache ('Verified-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($stage)
    Expand-DDInstallBundle $zip $stage $plan
    if($DownloadOnly){Write-Host ('Verified complete GitHub own bundle '+$plan.version+'. No game/profile changes.');return 0}
    # Use only the functions carried by this verified release from here onward.
    . (Join-Path $stage 'Bootstrap\Prepare-PublicDependencies.ps1') -SteamPath $selectedSteamPath -ExistingModDirectory $selectedExistingMods
    $layout=Get-DDSteamLayout $selectedSteamPath
    if(!$layout.ready){throw 'Install Terraria and tModLoader through Steam first.'}
    $official=@(($layout.apps|Where-Object appId -eq '1281930').paths)[0]
    $sourceProtection=@($layout.libraries|ForEach-Object {Join-Path $_ 'steamapps'})
    $sourceProtection+=@((Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Terraria'))
    $protected=@($sourceProtection)+@($setupRoot)
    $root=Assert-DDInstallRoot $root $protected
    Assert-GameClosed $root (Join-Path $root 'Profile')
    Invoke-DDSteamGate $official (Join-Path $stage 'Tools\SteamLicenseGate.dll')
    $manifest=Join-Path $stage 'Bootstrap\public-dependencies.json'
    $policy=Read-DDPublicManifest $manifest
    $authors=Join-Path $cache 'author-assets'
    Write-Host 'Checking 65 pinned dependencies from Steam Workshop and author releases...'
    foreach($mod in $policy.mods|Where-Object sourceKind -ceq 'author-github-release'){$null=Receive-DDStructureHelper $authors $sourceProtection $mod.name}
    foreach($mod in $policy.mods|Where-Object sourceKind -ceq 'author-source-build'){$null=Receive-DDOfficialOre $authors $sourceProtection $official (Join-Path $stage 'Tools\SourceBuild.dll')}
    $inventory=Invoke-DDPublicPreflight $manifest $layout.steamRoot $selectedExistingMods $authors
    $missing=@($inventory.dependencies|Where-Object {$_.sourceKind -ceq 'steam-workshop' -and $_.status -cne 'AVAILABLE_EXACT'})
    if($missing.Count){
        Write-Host ('Steam is downloading '+$missing.Count+' official Workshop items; completed downloads will be reused.')
        Invoke-DDSteamGate $official (Join-Path $stage 'Tools\SteamLicenseGate.dll') (($missing.workshopId|Select-Object -Unique) -join ',')
        $inventory=Invoke-DDPublicPreflight $manifest $layout.steamRoot $selectedExistingMods $authors
    }
    Write-DDJson $inventory (Join-Path $cache 'dependency-readback.json')
    if($inventory.readiness -cne 'DEPENDENCIES_RESOLVED_ONLY'){
        foreach($row in $inventory.dependencies|Where-Object status -cne 'AVAILABLE_EXACT'){Write-Host ($row.name+': '+$row.status+' '+$row.officialSource)}
        throw 'Pinned dependency bytes are unavailable. No installed mods were replaced. See Downloads/dependency-readback.json. Retry after Steam finishes; newer/different bytes are never substituted.'
    }
    Initialize-DDInstallRoot $root
    $installLock=[IO.File]::Open((Join-Path $root '.install.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try {
        # Recover only our interrupted transactions, with the game closed.
        $backups=Join-Path $root 'Backups'
        if(Test-Path -LiteralPath $backups){foreach($dir in Get-ChildItem -LiteralPath $backups -Directory){
            $journal=Join-Path $dir.FullName 'transaction.json'
            if(Test-Path -LiteralPath $journal){$j=Get-Content -LiteralPath $journal -Raw|ConvertFrom-Json;if($j.status -in @('prepared','mods-backed-up','mods-installed')){$null=Restore-DDInstall $root $dir.FullName}}
        }}
        $pointer=Join-Path $root 'installation.json'
        if(Test-Path -LiteralPath $pointer){
            $installed=Get-Content -LiteralPath $pointer -Raw|ConvertFrom-Json
            if([int]$installed.sequence -gt [int]$plan.sequence){throw 'Refusing a release older than this installation.'}
            if($installed.planSha256 -ceq (Hash $planPath)){
                $existingVersion=Safe-Child $root $installed.versionPath
                try {
                    . (Join-Path $stage 'Bootstrap\Ensure-OfficialDependenciesForLaunch.ps1') -savedirectory (Join-Path $root 'Profile')
                    $null=Assert-DDPublicProfile (Join-Path $root 'Profile') $manifest (Join-Path $stage 'Bootstrap\public-owned-mod.json')
                    foreach($file in $plan.files|Where-Object {$_.path -cmatch '^(Tools|Bootstrap)/'}){if((Hash (Safe-Child $existingVersion $file.path)) -cne $file.sha256){throw 'Installed tools changed.'}}
                    if((Hash (Join-Path $existingVersion 'Runtime\tModLoader.dll')) -cne '00d560faa33ce25522e85792cd1b25c8d5be95787a10ae2b03fa33ac496bdf2e'){throw 'Installed runtime changed.'}
                    Write-Host ('Already installed and verified: '+$plan.version);return 0
                }catch{Write-Host 'Installed managed files are incomplete or changed; repairing from verified sources.'}
            }
        }
        $versionRelative='Versions\'+$plan.source.tag+'-'+[Guid]::NewGuid().ToString('N')
        $version=Safe-Child $root $versionRelative;[void][IO.Directory]::CreateDirectory($version)
        foreach($dir in @('Tools','Bootstrap')){Copy-Item -LiteralPath (Join-Path $stage $dir) -Destination (Join-Path $version $dir) -Recurse}
        Write-Host 'Preparing a private runtime from the local official installation...'
        Copy-DDRuntime $official (Join-Path $version 'Runtime') (Join-Path $version 'Tools')
        $prepared=Join-Path $version 'PreparedProfile'
        $null=Stage-DDPublicDependencies $inventory $prepared
        Copy-Item -LiteralPath (Join-Path $stage 'Mods\DingDongBiomeHomes.tmod') -Destination (Join-Path $prepared 'Mods\DingDongBiomeHomes.tmod')
        if(Test-Path -LiteralPath (Join-Path $stage 'Defaults')){foreach($item in Get-ChildItem -LiteralPath (Join-Path $stage 'Defaults')){Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $prepared $item.Name) -Recurse}}
        . (Join-Path $version 'Bootstrap\Ensure-OfficialDependenciesForLaunch.ps1') -savedirectory $prepared
        $null=Assert-DDPublicProfile $prepared $manifest (Join-Path $version 'Bootstrap\public-owned-mod.json')
        $receipt=[ordered]@{format='dingdong-github-installation-v1';version=$plan.version;sequence=[int]$plan.sequence;tag=$plan.source.tag;planSha256=(Hash $planPath);bundleSha256=$plan.bundle.sha256;versionPath=$versionRelative;officialTml=$official;externalMods=65;enabledMods=66;packageFiles=@($plan.files|Where-Object {$_.path -cmatch '^(Tools|Bootstrap)/'});utc=[DateTime]::UtcNow.ToString('o');savesChanged=$false}
        $out=Commit-DDInstall $root $prepared $receipt
        $launchScript=Join-Path $root 'Launch-DingDong.ps1'
        Copy-Item -LiteralPath (Join-Path $version 'Tools\Launch-Installed.ps1') -Destination $launchScript -Force
        [IO.File]::WriteAllText((Join-Path $root 'Launch-DingDong.cmd'),"@echo off`r`npowershell.exe -NoProfile -ExecutionPolicy Bypass -File `"%~dp0Launch-DingDong.ps1`"`r`nif errorlevel 1 pause`r`n",[Text.Encoding]::ASCII)
        Write-DDJson $out (Join-Path $cache 'install-result.json')
        Write-Host ('Installed DingDong '+$plan.version+' with 66 mods. Launch: '+(Join-Path $root 'Launch-DingDong.cmd'))
        Write-Host ('Rollback backup: '+$out.backup)
    } finally {$installLock.Dispose()}
} catch {Write-Host ('DINGDONG_SETUP_FAILED: '+$_.Exception.Message) -ForegroundColor Red;$result=1}
finally {if($lock){$lock.Dispose()}}
return $result
}
if($MyInvocation.InvocationName -ne '.'){exit (Invoke-DDSetup)}
