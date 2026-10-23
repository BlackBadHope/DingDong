# Synthetic offline fixtures; never launches a game/Steam or contacts Workshop.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '..\Resolve-OfficialDependencies.ps1')
$work=Join-Path $PSScriptRoot ('_work\'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($work)
$utf8=New-Object Text.UTF8Encoding($false)
$results=New-Object 'Collections.Generic.List[object]'
function Assert($Value,[string]$Message){if(!$Value){throw $Message}}
function Case([string]$Name,[scriptblock]$Body){try{& $Body;$results.Add([ordered]@{name=$Name;result='PASS'});Write-Host ('PASS '+$Name)}catch{$results.Add([ordered]@{name=$Name;result='FAIL';error=$_.Exception.Message});throw}}
function Reject([scriptblock]$Body){$rejected=$false;try{& $Body|Out-Null}catch{$rejected=$true};Assert $rejected 'Expected rejection.'}
function Text([string]$Path,[string]$Value){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path));[IO.File]::WriteAllText($Path,$Value,$utf8)}
function Json($Value,[string]$Path){Text $Path ($Value|ConvertTo-Json -Depth 12)}
function Fake-Tmod([string]$Path,[string]$Name) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    $data=New-Object IO.MemoryStream;$writer=New-Object IO.BinaryWriter($data)
    $writer.Write($Name);$writer.Write('1.2.3');$writer.Write([int]0);$writer.Flush();$body=$data.ToArray();$writer.Dispose()
    $stream=[IO.File]::Create($Path);$writer=New-Object IO.BinaryWriter($stream)
    try{$writer.Write([Text.Encoding]::ASCII.GetBytes('TMOD'));$writer.Write('2026.8.0.0');$writer.Write((New-Object byte[] 276));$writer.Write([int]$body.Length);$writer.Write($body)}finally{$writer.Dispose();$stream.Dispose()}
}
$steam=Join-Path $work 'Steam';$library=Join-Path $work 'Library'
Text (Join-Path $steam 'steam.exe') 'synthetic marker only - never execute'
Text (Join-Path $steam 'steamapps\libraryfolders.vdf') ('"libraryfolders" { "1" { "path" "'+$library.Replace('\','\\')+'" } }')
foreach($a in @(@{id='105600';dir='Terraria';file='Terraria.exe'},@{id='1281930';dir='tModLoader';file='tModLoader.dll'})) {
    Text (Join-Path $library ('steamapps\appmanifest_'+$a.id+'.acf')) ('"AppState" { "appid" "'+$a.id+'" "installdir" "'+$a.dir+'" }')
    Text (Join-Path $library ('steamapps\common\'+$a.dir+'\'+$a.file)) 'synthetic marker only - never execute'
}
$source=Join-Path $library 'steamapps\workshop\content\1281930\1234567890\2026.8\RenamedPackage.tmod'
Fake-Tmod $source 'FixtureMod'
$mod=[pscustomobject]@{name='FixtureMod';file='DifferentName.tmod';workshopId='1234567890';officialSource='https://steamcommunity.com/sharedfiles/filedetails/?id=1234567890';bytes=(Get-Item -LiteralPath $source).Length;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()}
$hints=Join-Path $work 'hints.json';Json ([ordered]@{format='dingdong-official-source-hints-v1';sources=@()}) $hints
$manifest=Join-Path $work 'reference.json'
$reference=[ordered]@{format='dingdong-reference-inventory-v1';tModLoaderAppId=1281930;mods=@($mod,[pscustomobject]@{name='DingDongBiomeHomes';workshopId=$null;bytes=1;sha256=('0'*64)})}
Json $reference $manifest
$script:Opened=New-Object 'Collections.Generic.List[string]'
function Start-Process {param($FilePath);$script:Opened.Add($FilePath)}
try {
    Case 'Official Steam layout uses only declared libraries and both app manifests' {$s=Get-DDSteamLayout $steam;Assert ($s.ready -and $s.libraries.Count -eq 2) 'Steam layout not resolved.'}
    Case 'Internal metadata name and exact SHA resolve despite different filename' {$r=Resolve-DDReferenceMod $mod $null @($library);Assert ($r.status -eq 'AVAILABLE_EXACT' -and $r.selectedPath -eq $source) 'Exact package not resolved.'}
    Case 'Same-size SHA mismatch never selects current or latest version' {$bad=$mod|ConvertTo-Json|ConvertFrom-Json;$bad.sha256='0'*64;$r=Resolve-DDReferenceMod $bad $null @($library);Assert ($r.status -eq 'INCOMPATIBLE_OFFICIAL' -and !$r.selectedPath) 'Hash mismatch accepted.'}
    Case 'Metadata name mismatch rejected even when filename and bytes are available' {$bad=$mod|ConvertTo-Json|ConvertFrom-Json;$bad.name='OtherName';$r=Resolve-DDReferenceMod $bad $null @($library);Assert ($r.status -eq 'INCOMPATIBLE_OFFICIAL') 'Wrong internal name accepted.'}
    Case 'Missing official source and known but uninstalled item remain distinct' {$bad=$mod|ConvertTo-Json|ConvertFrom-Json;$bad.workshopId=$null;Assert ((Resolve-DDReferenceMod $bad $null @($library)).status -eq 'UNRESOLVED_OFFICIAL_SOURCE') 'Unknown source not blocked.';$bad.workshopId='9876543210';Assert ((Resolve-DDReferenceMod $bad $null @($library)).status -eq 'MISSING_OFFICIAL') 'Missing official item not reported.'}
    Case 'Source hint provides ID without overriding pinned expected hash' {$bad=$mod|ConvertTo-Json|ConvertFrom-Json;$bad.workshopId=$null;$bad.sha256='0'*64;$r=Resolve-DDReferenceMod $bad ([pscustomobject]@{workshopId='1234567890'}) @($library);Assert ($r.status -eq 'INCOMPATIBLE_OFFICIAL') 'Hint bypassed pinned bytes.'}
    Case 'Malformed TMOD header fails without loading or executing code' {$bad=Join-Path $work 'bad.tmod';Text $bad 'not a tmod';Reject {Read-DDTmodIdentity $bad}}
    Case 'Steam markers required and unsafe appmanifest path rejected' {Reject {Get-DDSteamLayout (Join-Path $work 'NoSteam')};$p=Join-Path $library 'steamapps\appmanifest_1281930.acf';$before=[IO.File]::ReadAllText($p);try{Text $p '"AppState" { "appid" "1281930" "installdir" "../outside" }';Reject {Get-DDSteamLayout $steam}}finally{Text $p $before}}
    Case 'Default inventory excludes old own binary and never opens links or writes profile' {$before=Get-FileHash -LiteralPath $source;$r=Invoke-DDDependencyInventory $manifest $steam $hints;Assert ($r.exactCount -eq 1 -and $r.dependencies.Count -eq 1 -and !$r.completeCleanInstall) 'Incorrect inventory.';Assert ($script:Opened.Count -eq 0 -and !$r.copied -and !$r.enabled -and !$r.gameLaunched) 'Default inventory mutated state.';Assert ((Get-FileHash -LiteralPath $source).Hash -eq $before.Hash) 'Official source changed.'}
    Case 'Explicit link helper opens only bounded official missing-item HTTPS pages' {$fake=[pscustomobject]@{dependencies=@([pscustomobject]@{status='MISSING_OFFICIAL';officialSource='https://steamcommunity.com/sharedfiles/filedetails/?id=1234567890'},[pscustomobject]@{status='MISSING_OFFICIAL';officialSource='https://steamcommunity.com/sharedfiles/filedetails/?id=9876543210'})};Open-DDMissingWorkshopPages $fake 1;Assert ($script:Opened.Count -eq 1) 'Link limit failed.'}
    Case 'PS5 CLI resolves default manifest after parameter binding using isolated Steam fixture' {
        $report=Join-Path $work 'cli-default.json';$output=Join-Path $work 'cli-default.log'
        & (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot '..\Resolve-OfficialDependencies.ps1') -SteamPath $steam -ReportPath $report *> $output
        Assert ($LASTEXITCODE -eq 2 -and (Test-Path -LiteralPath $report)) 'CLI default manifest did not produce the expected blocked inventory.'
        $r=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
        Assert (!$r.completeCleanInstall -and !$r.copied -and !$r.gameLaunched) 'CLI claimed or changed an installation.'
    }
} finally {
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'test-results.json'),([ordered]@{utc=[DateTime]::UtcNow.ToString('o');resolverSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '..\Resolve-OfficialDependencies.ps1') -Algorithm SHA256).Hash;network='NOT_RUN';SteamLaunch='NOT_RUN';results=@($results.ToArray())}|ConvertTo-Json -Depth 10),$utf8)
}
Write-Host ('Completed '+$results.Count+' offline checks.')
# The negative CLI fixture intentionally exits 2. Do not leak that native exit
# code into the GitHub Actions PowerShell wrapper after all assertions passed.
exit 0
