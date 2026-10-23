# Installed public-edition launcher. No arbitrary launch arguments or save migration.
$ErrorActionPreference='Stop'
try {
    $root=$PSScriptRoot
    $marker=Get-Content -LiteralPath (Join-Path $root '.dingdong-root.json') -Raw|ConvertFrom-Json
    if($marker.format -cne 'dingdong-root-v1' -or $marker.root -ine $root){throw 'Invalid installation marker.'}
    $state=Get-Content -LiteralPath (Join-Path $root 'installation.json') -Raw|ConvertFrom-Json
    if($state.format -cne 'dingdong-github-installation-v1' -or $state.versionPath -cnotmatch '^Versions\\[A-Za-z0-9._-]+-[a-f0-9]{32}$'){throw 'Invalid installed version path.'}
    $version=Join-Path $root $state.versionPath
    foreach($file in $state.packageFiles){
        if($file.path -cnotmatch '^(Tools|Bootstrap)/[A-Za-z0-9._-]+\.(dll|json|txt|ps1)$'){throw 'Invalid installed tool path.'}
        $path=Join-Path $version $file.path
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $file.sha256){throw 'Installed tools changed. Run Install-DingDong.cmd again to repair.'}
    }
    & (Join-Path $version 'Tools\Launch-DingDong.ps1') -OfficialTmlPath $state.officialTml -RuntimePath (Join-Path $version 'Runtime') -ProfilePath (Join-Path $root 'Profile') -HelperPath (Join-Path $version 'Tools\SteamLicenseGate.dll') -DependencyScript (Join-Path $version 'Bootstrap\Ensure-OfficialDependenciesForLaunch.ps1')
    if($LASTEXITCODE -and $LASTEXITCODE -ne 0){exit 1}
}catch{Write-Host ('DINGDONG_LAUNCH_FAILED: '+$_.Exception.Message) -ForegroundColor Red;exit 1}
