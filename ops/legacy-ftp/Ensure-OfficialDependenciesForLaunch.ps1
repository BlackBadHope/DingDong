# Intentionally a simple script: unrecognized tModLoader switches remain in $args.
# The native launcher receives the original argument list unchanged.
param()
$ErrorActionPreference = 'Stop'
$script:DDReleaseLaunchRoot = $PSScriptRoot

# Use the same installed PowerShell runtime as Update-DingDong.cmd before
# finalization. Never change Defender settings, retry a denied write through
# another tool, or require Codex/PowerShell 7 on machines that use Desktop.
function Resolve-DDPreferredPowerShell {
    # Import this host's built-in module explicitly. A Desktop host can inherit
    # a Core-only PSModulePath, whose auto-loader cannot import Security 7.x.
    $securityManifest = Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Security\Microsoft.PowerShell.Security.psd1'
    Import-Module -Name $securityManifest -Scope Local -ErrorAction Stop
    $candidates = @()
    if ($env:USERPROFILE) { $candidates += (Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe') }
    if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe') }
    $command = Get-Command pwsh.exe -CommandType Application -ErrorAction SilentlyContinue
    if ($command) { $candidates += $command.Source }
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (!(Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
        $signature = Microsoft.PowerShell.Security\Get-AuthenticodeSignature -LiteralPath $candidate
        if ($signature.Status -eq 'Valid' -and $signature.SignerCertificate.Subject -match 'O=Microsoft Corporation(?:,|$)') {
            return [IO.Path]::GetFullPath($candidate)
        }
    }
    return $null
}


function Resolve-DDLaunchProfile([string[]]$TmlArguments, [string]$ExplicitProfile) {
    $selectedProfile = $ExplicitProfile
    for ($index = 0; $index -lt $TmlArguments.Count; $index++) {
        if ($TmlArguments[$index] -ieq '-savedirectory') {
            if ($index + 1 -ge $TmlArguments.Count -or !$TmlArguments[$index + 1]) { throw '-savedirectory requires a profile path.' }
            $selectedProfile = $TmlArguments[++$index]
        }
    }
    if (!$selectedProfile) {
        $documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
        if (!$documents) { throw 'Windows did not return the default tModLoader profile. Use the DingDong launcher or -savedirectory.' }
        $selectedProfile = Join-Path $documents 'My Games\Terraria\tModLoader'
    }
    return [IO.Path]::GetFullPath($selectedProfile).TrimEnd('\')
}

function Assert-DDRespawnMigrationGameClosed {
    # This check is needed only for a pending config write. Once finalized,
    # Host & Play may start a child server while the client is running.
    $processes = @(Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='tModLoader.exe'" -ErrorAction Stop)
    if (@($processes | Where-Object { $_.CommandLine -match '(?i)(?:^|[\\/\s"''])tModLoader(?:\.dll|\.exe)(?:\s|"|$)' }).Count) {
        throw 'Close tModLoader before the one-time RC7 respawn configuration migration. No game process was stopped.'
    }
}

function Invoke-DDReleaseFinalization([string]$ProfilePath) {
    $profile = [IO.Path]::GetFullPath($ProfilePath).TrimEnd('\')
    $patch = Join-Path $script:DDReleaseLaunchRoot 'Apply-OwnedModPatch.ps1'
    $merge = Join-Path $script:DDReleaseLaunchRoot 'Set-BetterMultiplayerRespawnPolicy.ps1'
    $external = Join-Path $script:DDReleaseLaunchRoot 'Bootstrap-OfficialDependencies.ps1'
    foreach ($required in @($patch, $merge, $external)) {
        if (!(Test-Path -LiteralPath $required -PathType Leaf)) { throw 'RC7 launch finalization is incomplete. Close the game and run the current FTP updater again.' }
    }
    # The signed overlay contains this own patch/helper, never a mirrored
    # third-party package. The patch must verify the exact base/target bytes.
    $global:LASTEXITCODE = 0
    & $patch -ProfilePath $profile | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The exact owned mod patch could not be finalized. The game was not started.' }

    $policy = & $merge -ProfilePath $profile
    if (!$policy.configExisted -or $policy.savedSetting -ne $false) {
        Assert-DDRespawnMigrationGameClosed
        $policy = & $merge -ProfilePath $profile -Apply
    }
    $configPath = Join-Path $profile 'ModConfigs\BetterMultiplayer_BMConfig.json'
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    if ($config.NoBossFightRespawn -ne $false) { throw 'RC7 respawn policy readback failed. The game was not started.' }

    # Keep the checked official-Steam route. Do not replace it with the old
    # R22 verifier which claimed that the translation was bundled.
    $global:LASTEXITCODE = 0
    $env:DINGDONG_TRANSLATION_STATUS='UNKNOWN'
    & $external -ProfilePath $profile | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The official Steam dependency bootstrap did not complete. The game was not started.' }
    Write-Host "DINGDONG_RC7_LAUNCH_FINALIZATION|ownedPatch=PASS|respawnPolicy=PASS|translation=$env:DINGDONG_TRANSLATION_STATUS|runtimeAcceptance=NOT_RUN"
}

if ($MyInvocation.InvocationName -ne '.') {
    try {
        $selectedProfile = Resolve-DDLaunchProfile @($args) $env:DINGDONG_PROFILE_PATH
        if ($PSVersionTable.PSEdition -eq 'Desktop') {
            $preferred = Resolve-DDPreferredPowerShell
            if ($preferred) {
                Write-Host ('DINGDONG_POWERSHELL_HOST: ' + $preferred)
                & $preferred -NoLogo -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath @args
                exit $LASTEXITCODE
            }
        }
        Invoke-DDReleaseFinalization $selectedProfile
        exit 0
    } catch {
        if ($_.Exception.Message -match 'WriteAllBytes') {
            Write-Host 'DINGDONG_PROFILE_WRITE_BLOCKED: Windows could not create the staged mod in the profile Mods folder. The original mod is preserved. Check Windows Security Protection history for an application block; no security setting was changed.' -ForegroundColor Red
        }
        Write-Host ('DINGDONG_START_CHECK_FAILED: ' + $_.Exception.Message) -ForegroundColor Red
        exit 1
    }
}
