# DingDong project tool source. Licensing: see repository LEGAL-STATUS.md.
# Offline launch preflight. No download, copying, repair or process launch.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$savedirectory,
    [string]$PublicManifest,
    [string]$OwnedModManifest
)
$ErrorActionPreference='Stop'
if(!$PublicManifest){$PublicManifest=Join-Path $PSScriptRoot 'public-dependencies.json'}
if(!$OwnedModManifest){$OwnedModManifest=Join-Path $PSScriptRoot 'public-owned-mod.json'}
. (Join-Path $PSScriptRoot 'Prepare-PublicDependencies.ps1')
function Assert-DDPublicProfile([string]$Profile,[string]$Manifest,[string]$OwnManifest) {
    $policy=Read-DDPublicManifest $Manifest;$owned=Get-Content -LiteralPath $OwnManifest -Raw|ConvertFrom-Json
    if($owned.format -cne 'dingdong-public-owned-mod-v1' -or $owned.mod.name -cne 'DingDongBiomeHomes' -or $owned.mod.sha256 -cnotmatch '^[a-f0-9]{64}$' -or [long]$owned.mod.bytes -le 0){throw 'Final reviewed own mod identity is absent or invalid.'}
    $mods=Join-Path ([IO.Path]::GetFullPath($Profile)) 'Mods';Assert-DDRegularPath $mods
    $enabledPath=Join-Path $mods 'enabled.json';Assert-DDRegularPath $enabledPath
    # Windows PowerShell 5.1 emits a JSON root array as one pipeline object.
    # Normalize the returned array explicitly before counting/comparing names.
    $enabled=Get-Content -LiteralPath $enabledPath -Raw|ConvertFrom-Json
    $enabled=@($enabled)
    $expected=@(@($policy.mods.name)+@('DingDongBiomeHomes')|Sort-Object)
    if($enabled.Count -ne $expected.Count -or @($enabled|Select-Object -Unique).Count -ne $expected.Count -or ((@($enabled|Sort-Object) -join ',') -cne ($expected -join ','))){throw 'Enabled mods must exactly match the public preset; standalone PackWorldgenFix and extra mods are excluded.'}
    $candidates=@(Get-DDLocalCandidates $mods)
    if(@($candidates|Where-Object {$_.identity.name -ceq 'PackWorldgenFix'}).Count){throw 'Remove the standalone PackWorldgenFix from this prepared public profile; its guards are already in the own mod.'}
    foreach($identity in @($policy.mods)+@($owned.mod)) {
        $matches=@($candidates|Where-Object {$_.identity.name -ceq $identity.name})
        if($matches.Count -ne 1){throw ('Exactly one installed package is required for '+$identity.name)}
        Assert-DDExactPackage $matches[0].path $identity
    }
    [pscustomobject]@{format='dingdong-public-launch-preflight-v1';presetAndPackageIdentityVerified=$true;externalDependencies=$policy.mods.Count;ownModVerified=$true;networkUsed=$false;filesChanged=$false;gameLaunched=$false;completeCleanInstall=$false}
}
if($MyInvocation.InvocationName -ne '.') {
    try {$null=Assert-DDPublicProfile $savedirectory $PublicManifest $OwnedModManifest;Write-Host 'Public preset and exact installed package bytes verified. No files changed.'}
    catch {throw ('PUBLIC_DEPENDENCIES_UNVERIFIED: '+$_.Exception.Message)}
}
