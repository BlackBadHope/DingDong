[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OfficialTmlPath,
    [Parameter(Mandatory)][string]$RuntimePath,
    [Parameter(Mandatory)][string]$ProfilePath
)
$ErrorActionPreference='Stop'
try {
    $official=[IO.Path]::GetFullPath($OfficialTmlPath).TrimEnd('\')
    $runtime=[IO.Path]::GetFullPath($RuntimePath).TrimEnd('\')
    $profile=[IO.Path]::GetFullPath($ProfilePath).TrimEnd('\')
    if($official -eq $runtime) { throw 'A separately prepared private runtime is required; the official Steam installation must stay intact.' }
    $dotnet=Join-Path $official 'dotnet\dotnet.exe'
    $gate=Join-Path $PSScriptRoot 'bin\Release\net8.0\SteamLicenseGate.dll'
    if(!(Test-Path -LiteralPath $gate -PathType Leaf)) { throw 'Build the own-source SteamLicenseGate helper before launch.' }
    $proofText=& $dotnet $gate --official-tml $official
    if($LASTEXITCODE -ne 0) { throw 'STEAM_LICENSE_UNVERIFIED: start Steam under this Windows user, sign in online, and install licensed Terraria (105600) and tModLoader (1281930). No game was started.' }
    $proof=$proofText | ConvertFrom-Json
    if(!$proof.Verified -or $proof.Code -ne 'STEAM_TERRARIA_ENTITLEMENT_VERIFIED' -or [IO.Path]::GetFullPath($proof.TmlPath).TrimEnd('\') -ne $official) { throw 'Steam ownership verification did not return the expected result.' }
    $engine=Join-Path $runtime 'tModLoader.dll'
    if((Get-FileHash -LiteralPath $engine -Algorithm SHA256 -ErrorAction Stop).Hash -ne '00D560FAA33CE25522E85792CD1B25C8D5BE95787A10AE2B03FA33AC496BDF2E') { throw 'The private runtime does not match the pinned locally reproduced engine.' }
    $dependencies=Join-Path $runtime 'LaunchUtils\Ensure-OfficialDependenciesForLaunch.ps1'
    if(!(Test-Path -LiteralPath $dependencies -PathType Leaf)) { throw 'Local DingDong dependency bootstrap is missing. Finish the licensed local bootstrap first.' }
    & $dependencies -savedirectory $profile
    $workshop=Join-Path $runtime 'DingDongWorkshop'
    if(!(Test-Path -LiteralPath $workshop -PathType Container)) { throw 'The private Workshop isolation directory is missing.' }
    # No arbitrary arguments, -nosteam mode, ownership fallback or dedicated-server route.
    $start=[Diagnostics.ProcessStartInfo]::new()
    $start.FileName=Join-Path $runtime 'dotnet\dotnet.exe'
    $start.WorkingDirectory=$runtime; $start.UseShellExecute=$false
    $start.Arguments='"'+$engine+'" -steamworkshopfolder "'+$workshop+'" -savedirectory "'+$profile+'"'
    $null=[Diagnostics.Process]::Start($start)
} catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
