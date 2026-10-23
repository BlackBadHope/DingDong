# Own installation transaction. No user worlds/players enter its managed paths.
Set-StrictMode -Version Latest
function Write-DDJson($Value,[string]$Path) {
    $tmp=$Path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    [IO.File]::WriteAllText($tmp,($Value|ConvertTo-Json -Depth 14),(New-Object Text.UTF8Encoding($false)))
    if([IO.File]::Exists($Path)){[IO.File]::Replace($tmp,$Path,[NullString]::Value)}else{[IO.File]::Move($tmp,$Path)}
}
function Assert-DDInstallRoot([string]$Path,[string[]]$ProtectedRoots) {
    $full=[IO.Path]::GetFullPath($Path).TrimEnd('\');Assert-DDRegularPath $full
    if(!$full -or $full -eq [IO.Path]::GetPathRoot($full).TrimEnd('\')){throw 'Choose a dedicated DingDong installation folder.'}
    foreach($root in $ProtectedRoots){
        if(!$root){continue};$protected=[IO.Path]::GetFullPath($root).TrimEnd('\')
        if($full -ieq $protected -or $full.StartsWith($protected+'\',[StringComparison]::OrdinalIgnoreCase) -or $protected.StartsWith($full+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Installation overlaps Steam, the setup kit, or existing Terraria saves. Choose a separate folder.'}
    }
    $marker=Join-Path $full '.dingdong-root.json'
    if(Test-Path -LiteralPath $full){
        if(!(Test-Path -LiteralPath $full -PathType Container)){throw 'InstallPath is not a directory.'}
        if(!(Test-Path -LiteralPath $marker)){
            if(@(Get-ChildItem -LiteralPath $full -Force).Count){throw 'InstallPath must be empty or an existing DingDong GitHub installation.'}
        }else{
            $m=Get-Content -LiteralPath $marker -Raw|ConvertFrom-Json
            if($m.format -cne 'dingdong-root-v1' -or $m.root -ine $full){throw 'The installation marker belongs to another path.'}
        }
    }
    $full
}
function Initialize-DDInstallRoot([string]$Root) {
    [void][IO.Directory]::CreateDirectory($Root)
    $marker=Join-Path $Root '.dingdong-root.json'
    if(!(Test-Path -LiteralPath $marker)){Write-DDJson ([ordered]@{format='dingdong-root-v1';root=$Root}) $marker}
}
function Assert-DDInstallPlan($Plan,$Release) {
    if($Plan.format -cne 'dingdong-install-v1' -or $Plan.source.owner -cne 'BlackBadHope' -or $Plan.source.repository -cne 'DingDong' -or $Plan.source.tag -cne $Release.tag_name -or $Plan.version -cnotmatch '^\d+\.\d+\.\d+$' -or [int]$Plan.sequence -lt 32 -or [int]$Plan.minInstaller -gt 1){throw 'Unsupported signed installation plan.'}
    $expires=if($Plan.expiresUtc -is [DateTime]){$Plan.expiresUtc}else{[DateTime]::Parse([string]$Plan.expiresUtc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind)}
    if($expires.ToUniversalTime() -lt [DateTime]::UtcNow){throw 'The signed installation plan has expired.'}
    if($Plan.bundle.file -cnotmatch '^DingDong-Public-[A-Za-z0-9._-]+\.zip$' -or $Plan.bundle.sha256 -cnotmatch '^[a-f0-9]{64}$' -or [long]$Plan.bundle.bytes -le 0 -or [long]$Plan.bundle.bytes -gt 100MB){throw 'Invalid own bundle identity.'}
    $seen=@{};$total=0L
    foreach($file in $Plan.files){
        $null=Safe-Child $env:TEMP $file.path
        if($seen.ContainsKey($file.path) -or $file.path -cnotmatch '^(Tools/[A-Za-z0-9._-]+\.(dll|json|txt|ps1)|Bootstrap/[A-Za-z0-9._-]+\.(ps1|json)|Mods/DingDongBiomeHomes\.tmod|Defaults/(config\.json|ModConfigs/[A-Za-z0-9._-]+\.json))$' -or $file.sha256 -cnotmatch '^[a-f0-9]{64}$' -or [long]$file.bytes -lt 0 -or $file.decision -cne 'approved' -or $file.sourceCommit -cnotmatch '^[a-f0-9]{40}$'){throw 'Bundle file is outside the reviewed own-file allowlist.'}
        if($file.rights -cnotin @('project-owned','Mono.Cecil-MIT')){throw 'Unreviewed bundle rights.'}
        $seen[$file.path]=$true;$total+=[long]$file.bytes
    }
    foreach($required in @('Mods/DingDongBiomeHomes.tmod','Bootstrap/public-dependencies.json','Bootstrap/public-owned-mod.json','Tools/SteamLicenseGate.dll','Tools/EnginePatch.dll','Tools/SourceBuild.dll','Bootstrap/Build-OfficialOre.ps1','Tools/Launch-Installed.ps1')){if(!$seen.ContainsKey($required)){throw 'Required own bundle file is absent.'}}
    if($seen.Count -gt 128 -or $total -gt 200MB){throw 'Own bundle exceeds its expansion budget.'}
}
function Expand-DDInstallBundle([string]$Zip,[string]$Stage,$Plan) {
    $expected=@{};foreach($file in $Plan.files){$expected[$file.path]=$file};$seen=@{}
    $archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        foreach($entry in $archive.Entries){
            if(!$expected.ContainsKey($entry.FullName) -or $seen.ContainsKey($entry.FullName)){throw 'Unexpected or duplicate own bundle entry.'}
            $file=$expected[$entry.FullName];if($entry.Length -ne [long]$file.bytes){throw 'Own bundle entry size mismatch.'}
            $path=Safe-Child $Stage $file.path;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$path,$false)
            if((Hash $path) -cne $file.sha256){throw 'Own bundle entry checksum mismatch.'};$seen[$entry.FullName]=$true
        }
        if($seen.Count -ne $expected.Count){throw 'Own bundle is incomplete.'}
    } finally {$archive.Dispose()}
}
function Invoke-DDSteamGate([string]$Official,[string]$Helper,[string]$WorkshopIds='') {
    $args=@($Helper,'--official-tml',$Official)
    if($WorkshopIds){$args+=@('--download-workshop',$WorkshopIds)}
    $previousErrorAction=$ErrorActionPreference
    try {
        # Windows PowerShell 5 treats native SDK stderr as ErrorRecord objects.
        # Capture those diagnostics without turning a successful helper into an
        # exception, and never display or persist SDK/account output.
        $ErrorActionPreference='Continue'
        $raw=@(& (Join-Path $Official 'dotnet\dotnet.exe') @args 2>&1);$exitCode=$LASTEXITCODE
    }finally{$ErrorActionPreference=$previousErrorAction}
    # Native SDK account diagnostics are neither printed nor persisted.
    $tag='DINGDONG_LICENSE_RESULT='
    $records=@($raw|Where-Object {$_ -is [string] -and $_.StartsWith($tag,[StringComparison]::Ordinal)})
    if($exitCode -ne 0 -or $records.Count -ne 1){throw 'Steam verification/download failed. Sign in online and install Terraria plus the supported tModLoader version in Steam, then retry.'}
    $proof=$records[0].Substring($tag.Length)|ConvertFrom-Json
    if(!$proof.Verified -or $proof.Code -cne 'STEAM_TERRARIA_ENTITLEMENT_VERIFIED' -or [IO.Path]::GetFullPath($proof.TmlPath).TrimEnd('\') -ine $Official){throw 'Steam did not verify this official installation and Terraria entitlement.'}
    if($WorkshopIds -and !@($raw|Where-Object {$_ -is [string] -and $_.StartsWith('DINGDONG_WORKSHOP_RESULT=')}).Count){throw 'Steam download completion receipt is missing.'}
}
function Copy-DDRuntime([string]$Official,[string]$Target,[string]$Tools) {
    $inputHash=Hash (Join-Path $Official 'tModLoader.dll')
    if($inputHash -cnotin @('fcc6a9624b12191be4a15d714a3675440911feb8c1d374642ab1fd686f0da0a5','00d560faa33ce25522e85792cd1b25c8d5be95787a10ae2b03fa33ac496bdf2e')){throw 'Unsupported tModLoader build. Install Steam tModLoader 2026.8.3.0; the official installation will not be patched in place.'}
    [void][IO.Directory]::CreateDirectory($Target)
    foreach($folder in @('Content','Libraries','dotnet','DedicatedServerUtils')){
        $source=Join-Path $Official $folder
        if(!(Test-Path -LiteralPath $source -PathType Container)){if($folder -ceq 'DedicatedServerUtils'){continue};throw ('Missing official runtime folder: '+$folder)}
        Assert-DDRegularPath $source
        if(@(Get-ChildItem -LiteralPath $source -Recurse -Force -Attributes ReparsePoint).Count){throw 'Official runtime contains unsupported linked content.'}
        & robocopy.exe $source (Join-Path $Target $folder) /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if($LASTEXITCODE -ge 8){throw ('Official runtime copy failed: '+$folder)}
    }
    foreach($name in @('tModLoader.deps.json','tModLoader.runtimeconfig.json','tModLoader.runtimeconfig.dev.json','tModLoader.xml','tMLMod.targets','steam_appid.txt')){
        $source=Join-Path $Official $name;if(Test-Path -LiteralPath $source -PathType Leaf){Assert-DDRegularPath $source;Copy-Item -LiteralPath $source -Destination (Join-Path $Target $name)}
    }
    $output=Join-Path $Target 'tModLoader.dll'
    if($inputHash -ceq 'fcc6a9624b12191be4a15d714a3675440911feb8c1d374642ab1fd686f0da0a5'){
        & (Join-Path $Official 'dotnet\dotnet.exe') (Join-Path $Tools 'EnginePatch.dll') (Join-Path $Official 'tModLoader.dll') $output | Out-Null
        if($LASTEXITCODE -ne 0){throw 'Local engine patch failed.'}
    }else{Copy-Item -LiteralPath (Join-Path $Official 'tModLoader.dll') -Destination $output}
    if((Hash $output) -cne '00d560faa33ce25522e85792cd1b25c8d5be95787a10ae2b03fa33ac496bdf2e'){throw 'Locally prepared runtime identity mismatch.'}
    [void][IO.Directory]::CreateDirectory((Join-Path $Target 'DingDongWorkshop'))
}
function Restore-DDInstall([string]$Root,[string]$Backup) {
    $base=[IO.Path]::GetFullPath((Join-Path $Root 'Backups')).TrimEnd('\')+'\'
    $full=[IO.Path]::GetFullPath($Backup).TrimEnd('\');Assert-DDRegularPath $full
    if(!$full.StartsWith($base,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $full) -cnotmatch '^[a-f0-9]{32}$'){throw 'Rollback backup is outside this installation.'}
    $journal=Get-Content -LiteralPath (Join-Path $full 'transaction.json') -Raw|ConvertFrom-Json
    if($journal.format -cne 'dingdong-install-transaction-v1' -or $journal.root -ine $Root -or $journal.status -cnotin @('prepared','mods-backed-up','mods-installed','committed')){throw 'Invalid rollback journal.'}
    Assert-GameClosed $Root (Join-Path $Root 'Profile')
    $mods=Join-Path $Root 'Profile\Mods';$saved=Join-Path $full 'Mods'
    if($journal.hadMods -and !(Test-Path -LiteralPath $saved -PathType Container) -and $journal.status -ne 'prepared'){throw 'Required Mods backup is absent; rollback did not change files.'}
    if(Test-Path -LiteralPath $saved){
        if(Test-Path -LiteralPath $mods){Move-Item -LiteralPath $mods -Destination (Join-Path $full 'ReplacedMods')}
        Move-Item -LiteralPath $saved -Destination $mods
    }elseif(!$journal.hadMods -and $journal.status -in @('mods-backed-up','mods-installed','committed') -and (Test-Path -LiteralPath $mods)){
        Move-Item -LiteralPath $mods -Destination (Join-Path $full 'ReplacedMods')
    }
    $pointer=Join-Path $Root 'installation.json';$before=Join-Path $full 'installation-before.json'
    if(Test-Path -LiteralPath $before){Copy-Item -LiteralPath $before -Destination $pointer -Force}
    elseif(Test-Path -LiteralPath $pointer){Remove-Item -LiteralPath $pointer}
    if($journal.PSObject.Properties['createdDefaults']){foreach($file in $journal.createdDefaults){
        if($file.path -cnotmatch '^(config\.json|ModConfigs\\[A-Za-z0-9._-]+\.json|dependency-stage\.json)$'){throw 'Invalid default rollback path.'}
        $path=Safe-Child (Join-Path $Root 'Profile') $file.path
        if((Test-Path -LiteralPath $path -PathType Leaf) -and (Hash $path) -ceq $file.sha256){Remove-Item -LiteralPath $path}
    }}
    $journal.status='rolled-back';Write-DDJson $journal (Join-Path $full 'transaction.json')
    [pscustomobject]@{status='ROLLED_BACK';backup=$full;savesChanged=$false}
}
function Commit-DDInstall([string]$Root,[string]$PreparedProfile,$Receipt) {
    Assert-GameClosed $Root (Join-Path $Root 'Profile')
    $backup=Join-Path $Root ('Backups\'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($backup)
    $profile=Join-Path $Root 'Profile';[void][IO.Directory]::CreateDirectory($profile)
    $mods=Join-Path $profile 'Mods';$pointer=Join-Path $Root 'installation.json';$journalPath=Join-Path $backup 'transaction.json'
    $journal=[ordered]@{format='dingdong-install-transaction-v1';root=$Root;hadMods=(Test-Path -LiteralPath $mods);status='prepared';createdDefaults=@()}
    if(Test-Path -LiteralPath $pointer){Copy-Item -LiteralPath $pointer -Destination (Join-Path $backup 'installation-before.json')}
    Write-DDJson $journal $journalPath
    try {
        if($journal.hadMods){Move-Item -LiteralPath $mods -Destination (Join-Path $backup 'Mods')}
        $journal.status='mods-backed-up';Write-DDJson $journal $journalPath
        Move-Item -LiteralPath (Join-Path $PreparedProfile 'Mods') -Destination $mods
        $journal.status='mods-installed';Write-DDJson $journal $journalPath
        # Configuration defaults are additive. Existing settings, worlds, players,
        # keybinds and maps are outside the transaction and never replaced.
        foreach($file in Get-ChildItem -LiteralPath $PreparedProfile -File -Recurse|Sort-Object FullName){
            $relative=$file.FullName.Substring($PreparedProfile.TrimEnd('\').Length+1)
            if($relative -cnotmatch '^(config\.json|ModConfigs\\[A-Za-z0-9._-]+\.json|dependency-stage\.json)$'){throw 'Unexpected profile default.'}
            $target=Safe-Child $profile $relative
            if(!(Test-Path -LiteralPath $target)){
                $journal.createdDefaults+=@{path=$relative;sha256=(Hash $file.FullName)};Write-DDJson $journal $journalPath
                [void][IO.Directory]::CreateDirectory((Split-Path -Parent $target));Copy-Item -LiteralPath $file.FullName -Destination $target
            }
        }
        Write-DDJson $Receipt $pointer
        $journal.status='committed';Write-DDJson $journal $journalPath
        [pscustomobject]@{status='INSTALLED';version=$Receipt.version;backup=$backup;savesChanged=$false}
    }catch{
        $original=$_.Exception.Message
        $null=Restore-DDInstall $Root $backup
        throw ('Installation rolled back: '+$original)
    }
}
