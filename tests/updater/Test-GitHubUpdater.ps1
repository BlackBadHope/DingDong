# DingDong project tool source. Licensing: see repository LEGAL-STATUS.md.
# Offline synthetic fixtures only. No network, game launch, private key files or credentials.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$testRoot=$PSScriptRoot
$updater=Join-Path $testRoot '..\..\updater\DingDong-Update.ps1'
. $updater -NonInteractive
$work=Join-Path $testRoot ('_work\'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($work)
$script:UpdaterRoot=Join-Path $work 'Updater'
[void][IO.Directory]::CreateDirectory($script:UpdaterRoot)
$results=New-Object 'Collections.Generic.List[object]'
$started=[DateTime]::UtcNow
$testKey=$null
function Assert($Value,[string]$Message) { if(!$Value){throw $Message} }
function Check([string]$Name,[scriptblock]$Body) {
    try { & $Body; $results.Add([ordered]@{name=$Name;result='PASS'}); Write-Host ('PASS '+$Name) }
    catch { $results.Add([ordered]@{name=$Name;result='FAIL';error=$_.Exception.Message}); throw }
}
function Reject([scriptblock]$Body) { $failed=$false; try{& $Body | Out-Null}catch{$failed=$true}; Assert $failed 'Expected a closed gate.' }
function Text([string]$Path,[string]$Content) { [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path)); [IO.File]::WriteAllText($Path,$Content,$script:Utf8) }
function Clone($Value) { $Value | ConvertTo-Json -Depth 20 | ConvertFrom-Json }
function Sign($Feed,[string]$Name='signed.json') {
    $path=Join-Path $work $Name
    $bytes=$script:Utf8.GetBytes(($Feed | ConvertTo-Json -Depth 20 -Compress))
    Save-Json ([ordered]@{payloadBase64=[Convert]::ToBase64String($bytes);signatureBase64=[Convert]::ToBase64String($testKey.SignData($bytes,'SHA256'))}) $path
    $path
}
# Hard mock of the sole network function: an unregistered URL always fails.
$script:Routes=@{}; $script:Calls=New-Object 'Collections.Generic.List[string]'
function Route([string]$Url,[byte[]]$Body,[int]$Status=200,[string]$Location='') {
    $script:Routes[$Url]=@{Body=$Body;Status=$Status;Location=$Location}
}
function Open-GitHubResponse([uri]$Uri,[string]$Kind) {
    $script:Calls.Add($Uri.AbsoluteUri)
    if(!$script:Routes.ContainsKey($Uri.AbsoluteUri)){throw 'Offline mock has no such URL.'}
    $entry=$script:Routes[$Uri.AbsoluteUri]
    $response=[pscustomobject]@{StatusCode=$entry.Status;ContentLength=[long]$entry.Body.Length;Headers=@{Location=$entry.Location};Body=$entry.Body}
    $response | Add-Member ScriptMethod GetResponseStream { New-Object IO.MemoryStream(,$this.Body) }
    $response | Add-Member ScriptMethod Close { }
    $response
}
$script:FakeProcesses=@()
function Get-CimInstance { param($ClassName,$Filter); $script:FakeProcesses }
function Configure-Release($Feed,[string]$SignedPath) {
    $script:Routes=@{}; $script:Calls.Clear()
    $base='https://github.com/BlackBadHope/DingDong/releases/download/'+$Feed.source.tag+'/'
    $assets=@([ordered]@{name='latest.json';state='uploaded';size=(Get-Item -LiteralPath $SignedPath).Length;browser_download_url=$base+'latest.json'})
    Route ($base+'latest.json') ([byte[]]@()) 302 'https://release-assets.githubusercontent.com/fake/latest'
    Route 'https://release-assets.githubusercontent.com/fake/latest' ([IO.File]::ReadAllBytes($SignedPath))
    if($Feed.status -eq 'released') {
        $assets += [ordered]@{name=$Feed.distribution.manifestFile;state='uploaded';size=(Get-Item -LiteralPath $manifestPath).Length;browser_download_url=$base+$Feed.distribution.manifestFile}
        Route ($base+$Feed.distribution.manifestFile) ([IO.File]::ReadAllBytes($manifestPath))
        $assets += [ordered]@{name=$Feed.payload.file;state='uploaded';size=$Feed.payload.bytes;browser_download_url=$base+$Feed.payload.file}
        Route ($base+$Feed.payload.file) ([byte[]]@()) 302 'https://release-assets.githubusercontent.com/fake/payload'
        Route 'https://release-assets.githubusercontent.com/fake/payload' ([IO.File]::ReadAllBytes($zip))
    }
    $release=[ordered]@{tag_name=$Feed.source.tag;draft=$false;prerelease=$false;published_at='2026-10-22T00:00:00Z';assets=$assets}
    Route ((Get-ReleaseApiUri).AbsoluteUri) ($script:Utf8.GetBytes(($release | ConvertTo-Json -Depth 12 -Compress)))
    $release
}
try {
    $legacy=Join-Path $testRoot 'fixtures\legacy-signed-public-fixture.json'
    Check 'Historical synthetic fixture verifies with the real pinned public key' {
        $old=Read-VerifiedEnvelope $legacy
        Assert ($old.format -eq 'dingdong-update-v1' -and $old.files.Count -eq 4) 'Unexpected signed fixture.'
    }
    Check 'Legacy full-pack feed rejected despite valid pinned signature' { Reject { Read-SignedFeed $legacy } }
    Check 'Tampered pinned signature rejected' {
        $e=Get-Content -LiteralPath $legacy -Raw | ConvertFrom-Json
        $s=[Convert]::FromBase64String($e.signatureBase64); $s[0]=$s[0] -bxor 1; $e.signatureBase64=[Convert]::ToBase64String($s)
        $p=Join-Path $work 'tampered.json'; Save-Json $e $p; Reject { Read-VerifiedEnvelope $p }
    }
    # Private test material stays in memory and is discarded at process exit.
    $testKey=New-Object Security.Cryptography.RSACryptoServiceProvider(2048)
    $testKey.PersistKeyInCsp=$false
    $script:PublicKey=$testKey.ToXmlString($false)
    $pending=[ordered]@{format='dingdong-update-github-v1';minUpdater=5;sequence=23;status='pending';expiresUtc=[DateTime]::UtcNow.AddDays(1).ToString('o');source=[ordered]@{owner='BlackBadHope';repository='DingDong';tag='v0.1.0-bootstrap'};blockedReason='Rights review blocks a playable public pack. Use the documented local build route.'}
    $pendingPath=Sign $pending 'pending.json'
    Check 'Signed pending feed needs no payload or files' { Assert ((Read-SignedFeed $pendingPath).status -eq 'pending') 'Pending feed not recognized.' }
    Check 'Fake GitHub latest and controlled asset redirect show pending without target writes' {
        $null=Configure-Release $pending $pendingPath
        Invoke-Updater
        Assert ($script:Calls.Count -eq 3) 'Unexpected pending network requests.'
        Assert (!(Test-Path -LiteralPath (Join-Path $script:UpdaterRoot 'installation.json'))) 'Pending resolved an installation.'
        Assert (!(Test-Path -LiteralPath (Join-Path $script:UpdaterRoot 'Backups'))) 'Pending created an installation transaction.'
    }
    Check 'Pending with unfinished transaction never resolves targets or restores files' {
        $journal=Join-Path $script:UpdaterRoot 'Backups\unfinished\transaction.json'
        Text $journal '{"status":"in-progress","gamePath":"synthetic-game","profilePath":"synthetic-profile"}'
        $before=Hash $journal
        $resolve=(Get-Item Function:\Resolve-Targets).ScriptBlock
        $restore=(Get-Item Function:\Restore-Transaction).ScriptBlock
        $script:ResolveCalls=0; $script:RestoreCalls=0
        try {
            Set-Item Function:\Resolve-Targets { $script:ResolveCalls++; throw 'Pending attempted target resolution.' }
            Set-Item Function:\Restore-Transaction { $script:RestoreCalls++; throw 'Pending attempted transaction restoration.' }
            $null=Configure-Release $pending $pendingPath
            Invoke-Updater
            Assert ($script:ResolveCalls -eq 0 -and $script:RestoreCalls -eq 0) 'Pending touched transaction recovery.'
            Assert ((Hash $journal) -eq $before) 'Pending changed the unfinished transaction.'
        } finally {
            Set-Item Function:\Resolve-Targets $resolve; Set-Item Function:\Restore-Transaction $restore
            Remove-Item -LiteralPath $journal -Force
            Remove-Item -LiteralPath (Split-Path -Parent $journal)
        }
    }
    Check 'Missing actual GitHub release fails closed' { $script:Routes=@{}; Reject { Invoke-Updater } }
    Check 'Draft and prerelease discovery rejected' {
        foreach($flag in @('draft','prerelease')) {
            $release=Configure-Release $pending $pendingPath; $release[$flag]=$true
            Route ((Get-ReleaseApiUri).AbsoluteUri) ($script:Utf8.GetBytes(($release | ConvertTo-Json -Depth 12 -Compress)))
            Reject { Get-GitHubRelease (Join-Path $script:UpdaterRoot 'Downloads') }
        }
    }
    Check 'Asset must be unique and bound to selected repository and tag' {
        $release=Clone (Configure-Release $pending $pendingPath)
        $release.assets[0].browser_download_url='https://github.com/other/DingDong/releases/download/v0.1.0-bootstrap/latest.json'
        Reject { Get-ReleaseAsset $release 'latest.json' }
        $release=Clone (Configure-Release $pending $pendingPath); $release.assets += $release.assets[0]
        Reject { Get-ReleaseAsset $release 'latest.json' }
    }
    Check 'HTTP, userinfo, foreign host, direct storage and API redirects rejected' {
        $out=Join-Path $work 'bad-download'
        foreach($url in @('http://github.com/BlackBadHope/DingDong/releases/download/v1/a.zip','https://user@github.com/BlackBadHope/DingDong/releases/download/v1/a.zip','https://github.com.evil.test/BlackBadHope/DingDong/releases/download/v1/a.zip','https://release-assets.githubusercontent.com/direct')) { Reject { Fetch ([uri]$url) $out 100 } }
        $u='https://github.com/BlackBadHope/DingDong/releases/download/v1/a.zip'
        Route $u ([byte[]]@()) 302 'https://evil.test/download'; Reject { Fetch ([uri]$u) $out 100 }
        Route ((Get-ReleaseApiUri).AbsoluteUri) ([byte[]]@()) 302 $u; Reject { Fetch (Get-ReleaseApiUri) $out 100 'Api' }
    }
    Check 'Download streaming size limit enforced' {
        $u='https://github.com/BlackBadHope/DingDong/releases/download/v1/large.zip'
        Route $u ($script:Utf8.GetBytes('too-large')); Reject { Fetch ([uri]$u) (Join-Path $work 'large.part') 1 }
    }
    $game=Join-Path $work 'Game'; $profile=Join-Path $work 'Profile'; $payload=Join-Path $work 'Payload'
    Text (Join-Path $game 'tModLoader.dll') 'synthetic-old-runtime'
    Text (Join-Path $profile 'Mods\DingDongBiomeHomes.tmod') 'synthetic-old-mod'
    Text (Join-Path $profile 'Mods\UserExtra.tmod') 'synthetic-extra-mod'
    Text (Join-Path $profile 'ModConfigs\Example.json') 'user-config'
    Text (Join-Path $profile 'Players\test.plr') 'synthetic-player'
    Text (Join-Path $profile 'Worlds\test.wld') 'synthetic-world'
    Text (Join-Path $payload 'Bootstrap\Source\Example.cs') '// synthetic public source'
    Text (Join-Path $payload 'Bootstrap\Tools\Build.ps1') '# synthetic build script; never run'
    Text (Join-Path $payload 'Profile\ModConfigs\Example.json') '{}'
    Text (Join-Path $payload 'Profile\Mods\enabled.json') '[]'
    $files=@(foreach($file in Get-ChildItem -LiteralPath $payload -Recurse -File) {
        $parts=$file.FullName.Substring($payload.Length+1).Replace('\','/') -split '/',2
        [ordered]@{area=$parts[0];path=$parts[1];mode=$(if($parts[1] -like 'ModConfigs/*'){'ifMissing'}else{'replace'});sha256=(Hash $file.FullName);bytes=$file.Length;license='MIT';source='https://github.com/BlackBadHope/DingDong';provenanceId='synthetic:'+ $parts[0]+'/'+$parts[1]}
    })
    $zip=Join-Path $work 'public-synthetic.zip'; [IO.Compression.ZipFile]::CreateFromDirectory($payload,$zip)
    $approved=[ordered]@{format='dingdong-public-allowlist-v1';files=@(foreach($entry in $files){$copy=Clone $entry; $copy | Add-Member NoteProperty decision 'approved'; $copy})}
    $manifestPath=Join-Path $work 'public-allowlist.json'; Save-Json $approved $manifestPath
    $manifestBytes=[IO.File]::ReadAllBytes($manifestPath)
    $feed=[ordered]@{format='dingdong-update-github-v1';minUpdater=5;sequence=23;status='released';version='0.1.0';expiresUtc=[DateTime]::UtcNow.AddDays(1).ToString('o');source=[ordered]@{owner='BlackBadHope';repository='DingDong';tag='v0.1.0-test'};distribution=[ordered]@{policy='public-allowlist-v1';reviewed=$true;manifestFile='public-allowlist.json';manifestSha256=(Hash $manifestPath)};payload=[ordered]@{file='public-synthetic.zip';sha256=(Hash $zip);bytes=(Get-Item -LiteralPath $zip).Length};files=$files}
    $signed=Sign $feed 'release.json'
    Check 'Reviewed public allowlist and signed source accepted' { Assert ((Read-SignedFeed $signed).files.Count -eq 4) 'Public fixture rejected.' }
    Check 'Actual approved allowlist asset matches signed digest and every file entry' {
        $null=Configure-Release $feed $signed
        $release=Get-GitHubRelease (Join-Path $script:UpdaterRoot 'Downloads')
        Assert-ReleaseAllowlist (Read-SignedFeed $signed) $release (Join-Path $script:UpdaterRoot 'Downloads')
    }
    Check 'Tampered actual allowlist rejected against its signed SHA256' {
        $release=Configure-Release $feed $signed
        $bad=[byte[]]$manifestBytes.Clone(); $bad[0]=$bad[0] -bxor 1
        Route ('https://github.com/BlackBadHope/DingDong/releases/download/'+$feed.source.tag+'/public-allowlist.json') $bad
        Reject { Assert-ReleaseAllowlist (Read-SignedFeed $signed) $release (Join-Path $script:UpdaterRoot 'Downloads') }
    }
    Check 'Approved allowlist metadata mismatch, missing, duplicate or unapproved entries rejected' {
        try {
            foreach($case in @('metadata','missing','duplicate','unapproved')) {
                $manifest=Clone $approved
                switch($case){'metadata'{$manifest.files[0].provenanceId='different-provenance'};'missing'{$manifest.files=@($manifest.files | Select-Object -Skip 1)};'duplicate'{$manifest.files[1]=$manifest.files[0]};'unapproved'{$manifest.files[0].decision='private-only'}}
                Save-Json $manifest $manifestPath
                $copy=Clone $feed; $copy.distribution.manifestSha256=Hash $manifestPath
                $path=Sign $copy ('allowlist-'+$case+'.json'); $release=Configure-Release $copy $path
                Reject { Assert-ReleaseAllowlist (Read-SignedFeed $path) $release (Join-Path $script:UpdaterRoot 'Downloads') }
            }
        } finally { [IO.File]::WriteAllBytes($manifestPath,$manifestBytes) }
    }
    Check 'Missing actual reviewed allowlist release asset fails closed' {
        $release=Configure-Release $feed $signed
        $release.assets=@($release.assets | Where-Object { $_.name -ne 'public-allowlist.json' })
        Reject { Assert-ReleaseAllowlist (Read-SignedFeed $signed) $release (Join-Path $script:UpdaterRoot 'Downloads') }
    }
    Check 'Expired, wrong repository, older sequence and newer updater gates reject' {
        foreach($case in @('expired','source','sequence','minUpdater')) {
            $copy=Clone $feed
            switch($case){'expired'{$copy.expiresUtc=[DateTime]::UtcNow.AddDays(-1).ToString('o')};'source'{$copy.source.repository='elsewhere'};'sequence'{$copy.sequence=22};'minUpdater'{$copy.minUpdater=6}}
            Reject { Read-SignedFeed (Sign $copy ($case+'.json')) }
        }
    }
    Check 'Reviewed flag cannot authorize game runtime or compiled mods' {
        foreach($entry in @(@{area='Game';path='tModLoader.dll'},@{area='Profile';path='Mods/DingDongBiomeHomes.tmod'})) {
            $copy=Clone $feed; $copy.files[0].area=$entry.area; $copy.files[0].path=$entry.path
            Reject { Read-SignedFeed (Sign $copy 'binary.json') }
        }
    }
    Check 'Missing reviewed policy or file license/provenance rejected' {
        foreach($field in @('policy','license','source','provenanceId')) {
            $copy=Clone $feed
            if($field -eq 'policy'){$copy.distribution.policy='other'}else{$copy.files[0].PSObject.Properties.Remove($field)}
            Reject { Read-SignedFeed (Sign $copy 'rights.json') }
        }
    }
    Check 'Path traversal, ADS, reserved names and protected saves rejected' {
        foreach($bad in @('../outside','a/../../outside','C:\outside','Mods/file.tmod:evil','Mods/CON.tmod','Mods/evil.')) { Reject { Safe-Child $work $bad } }
        $copy=Clone $feed; $copy.files[0].area='Profile'; $copy.files[0].path='Worlds/actual.wld'
        Reject { Read-SignedFeed (Sign $copy 'save.json') }
    }
    Check 'Unexpected/incomplete ZIP entries and duplicate manifest entries rejected' {
        $bad=Join-Path $work 'bad.zip'; $archive=[IO.Compression.ZipFile]::Open($bad,[IO.Compression.ZipArchiveMode]::Create); $null=$archive.CreateEntry('../outside'); $archive.Dispose()
        Reject { Expand-Payload $bad (Join-Path $work 'BadStage') (Read-SignedFeed $signed) }
        $empty=Join-Path $work 'empty.zip'; $archive=[IO.Compression.ZipFile]::Open($empty,[IO.Compression.ZipArchiveMode]::Create); $archive.Dispose()
        Reject { Expand-Payload $empty (Join-Path $work 'EmptyStage') (Read-SignedFeed $signed) }
        $copy=Clone $feed; $copy.files += $copy.files[0]; Reject { Read-SignedFeed (Sign $copy 'duplicate.json') }
    }
    $GamePath=$game; $ProfilePath=$profile
    Check 'Fake GitHub release installs reviewed source and preserves saves/config/extra mods' {
        $null=Configure-Release $feed $signed
        Invoke-Updater
        Assert ((Get-Content -LiteralPath (Join-Path $script:UpdaterRoot 'Bootstrap\Source\Example.cs') -Raw) -eq '// synthetic public source') 'Bootstrap missing.'
        foreach($p in @{'ModConfigs\Example.json'='user-config';'Players\test.plr'='synthetic-player';'Worlds\test.wld'='synthetic-world';'Mods\UserExtra.tmod'='synthetic-extra-mod'}.GetEnumerator()) { Assert ((Get-Content -LiteralPath (Join-Path $profile $p.Key) -Raw) -eq $p.Value) 'Protected file changed.' }
        Assert ((Get-Content -LiteralPath (Join-Path $game 'tModLoader.dll') -Raw) -eq 'synthetic-old-runtime') 'Game runtime changed.'
    }
    $backup=@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory)[0].FullName
    Check 'Source transaction rollback removes new source and preserves existing config' {
        Restore-Transaction $backup
        Assert (!(Test-Path -LiteralPath (Join-Path $script:UpdaterRoot 'Bootstrap\Source\Example.cs'))) 'Bootstrap rollback failed.'
        Assert (!(Test-Path -LiteralPath (Join-Path $profile 'Mods\enabled.json'))) 'Added profile file survived rollback.'
    }
    Check 'Same-size corrupted release archive rejected before target changes' {
        $null=Configure-Release $feed $signed
        $cached=Join-Path $script:UpdaterRoot 'Downloads\public-synthetic.zip'; Remove-Item -LiteralPath $cached -Force
        $badBytes=[IO.File]::ReadAllBytes($zip); $badBytes[0]=$badBytes[0] -bxor 1
        Route 'https://release-assets.githubusercontent.com/fake/payload' $badBytes
        Reject { Invoke-Updater }
        Assert (!(Test-Path -LiteralPath (Join-Path $profile 'Mods\enabled.json'))) 'Corrupt payload changed a target.'
    }
    Check 'Game-closed guard detects a synthetic matching process without stopping it' {
        $script:FakeProcesses=@([pscustomobject]@{ProcessId=999999;ExecutablePath=(Join-Path $game 'dotnet.exe');CommandLine='dotnet '+$game+'\tModLoader.dll'})
        Reject { Assert-GameClosed $game $profile }; Assert ($script:FakeProcesses.Count -eq 1) 'Guard changed process fixture.'; $script:FakeProcesses=@()
    }
    Check 'Local prepared files retain atomic install, idempotence and rollback' {
        $stage=Join-Path $work 'LocalStage'; [void][IO.Directory]::CreateDirectory($stage)
        $f=[pscustomobject]@{area='Game';path='tModLoader.dll';mode='replace';sha256='';bytes=21}
        Text (Stored-File $stage $f) 'synthetic-new-runtime'; $f.sha256=Hash (Stored-File $stage $f)
        $local=[pscustomobject]@{version='0.1.0';sequence=24;files=@($f)}
        Install-Payload $local $stage @{Game=$game;Profile=$profile}
        $before=@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory).Count
        Install-Payload $local $stage @{Game=$game;Profile=$profile}
        Assert (@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory).Count -eq $before) 'Idempotence failed.'
        $b=@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory | Where-Object {(Get-Content -LiteralPath (Join-Path $_.FullName 'transaction.json') -Raw | ConvertFrom-Json).sequence -eq 24})[0].FullName
        Restore-Transaction $b
        Assert ((Get-Content -LiteralPath (Join-Path $game 'tModLoader.dll') -Raw) -eq 'synthetic-old-runtime') 'Runtime rollback failed.'
    }
    Check 'Concurrent updater lock excludes another writer' {
        $p=Join-Path $script:UpdaterRoot '.updater.lock'; $lock=[IO.File]::Open($p,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
        try { Reject { $other=[IO.File]::Open($p,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $other.Dispose() } } finally { $lock.Dispose() }
    }
    Check 'Rollback preserves later user modifications' {
        $stage=Join-Path $work 'LocalStage'
        $f=[pscustomobject]@{area='Game';path='tModLoader.dll';mode='replace';sha256='';bytes=21}; $f.sha256=Hash (Stored-File $stage $f)
        $local=[pscustomobject]@{version='0.1.0';sequence=24;files=@($f)}
        Install-Payload $local $stage @{Game=$game;Profile=$profile}
        $b=@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory | Where-Object {(Get-Content -LiteralPath (Join-Path $_.FullName 'transaction.json') -Raw | ConvertFrom-Json).status -eq 'complete'})[-1].FullName
        Text (Join-Path $game 'tModLoader.dll') 'synthetic-user-change'
        Reject { Restore-Transaction $b }
        Assert ((Get-Content -LiteralPath (Join-Path $game 'tModLoader.dll') -Raw) -eq 'synthetic-user-change') 'User change was overwritten.'
    }
    # Synthetic bytes only: this tests the narrow trust gate, not a real mod or rights audit.
    $ownedPayload=Join-Path $work 'OwnedPayload'
    $ownedMod=Join-Path $ownedPayload 'Profile\Mods\DingDongBiomeHomes.tmod'
    Text $ownedMod 'synthetic-public-owned-mod'
    $ownedZip=Join-Path $work 'owned-synthetic.zip'; [IO.Compression.ZipFile]::CreateFromDirectory($ownedPayload,$ownedZip)
    $ownedEntry=[ordered]@{area='Profile';path='Mods/DingDongBiomeHomes.tmod';mode='replace';sha256=(Hash $ownedMod);bytes=(Get-Item -LiteralPath $ownedMod).Length;license='LicenseRef-DingDong-Personal-Use-1.0';source=('https://github.com/BlackBadHope/DingDong/tree/'+('a'*40)+'/game-mod/DingDongBiomeHomes');provenanceId=('dingdong-owned-v1:sha256:'+('b'*64));rights='project-owned';decision='approved'}
    $ownedManifest=[ordered]@{format='dingdong-public-allowlist-v1';files=@($ownedEntry)}
    $ownedManifestPath=Join-Path $work 'owned-public-allowlist.json'; Save-Json $ownedManifest $ownedManifestPath
    $ownedFeed=Clone $feed; $ownedFeed.sequence=25; $ownedFeed.source.tag='v0.1.0-owned'; $ownedFeed.files=@(Clone $ownedEntry)
    $ownedFeed.distribution.manifestFile='owned-public-allowlist.json'; $ownedFeed.distribution.manifestSha256=Hash $ownedManifestPath
    $ownedFeed.payload.file='owned-synthetic.zip'; $ownedFeed.payload.sha256=Hash $ownedZip; $ownedFeed.payload.bytes=(Get-Item -LiteralPath $ownedZip).Length
    $ownedSigned=Sign $ownedFeed 'owned-signed.json'
    Check 'Exact approved project-owned DingDong mod passes signed feed and actual allowlist gate' {
        $manifestPath=$ownedManifestPath; $zip=$ownedZip
        $release=Configure-Release $ownedFeed $ownedSigned
        Assert-ReleaseAllowlist (Read-SignedFeed $ownedSigned) $release (Join-Path $script:UpdaterRoot 'Downloads')
    }
    Check 'Owned metadata cannot authorize another mod, path spelling or Game binary' {
        foreach($case in @('foreign','path','game')) {
            $copy=Clone $ownedFeed
            switch($case){'foreign'{$copy.files[0].path='Mods/OtherMod.tmod'};'path'{$copy.files[0].path='Mods/dingdongbiomehomes.tmod'};'game'{$copy.files[0].area='Game';$copy.files[0].path='tModLoader.dll'}}
            Reject { Read-SignedFeed (Sign $copy ('owned-'+$case+'.json')) }
        }
    }
    Check 'Owned mod requires exact rights, personal-use license, commit source, decision and lowercase provenance' {
        foreach($case in @('missingRights','rights','license','source','decision','provenance')) {
            $copy=Clone $ownedFeed
            switch($case){'missingRights'{$copy.files[0].PSObject.Properties.Remove('rights')};'rights'{$copy.files[0].rights='third-party'};'license'{$copy.files[0].license='MIT'};'source'{$copy.files[0].source='https://github.com/BlackBadHope/DingDong/tree/main/game-mod/DingDongBiomeHomes'};'decision'{$copy.files[0].decision='pending'};'provenance'{$copy.files[0].provenanceId='dingdong-owned-v1:sha256:'+('B'*64)}}
            Reject { Read-SignedFeed (Sign $copy ('owned-'+$case+'.json')) }
        }
    }
    Check 'Owned mod feed and allowlist must match rights and valid provenance exactly' {
        $manifestPath=Join-Path $work 'owned-mismatch-allowlist.json'; $zip=$ownedZip
        foreach($case in @('rights','provenance')) {
            $manifest=Clone $ownedManifest
            if($case -eq 'rights'){$manifest.files[0].rights='third-party'}else{$manifest.files[0].provenanceId='dingdong-owned-v1:sha256:'+('c'*64)}
            Save-Json $manifest $manifestPath
            $copy=Clone $ownedFeed; $copy.distribution.manifestSha256=Hash $manifestPath
            $path=Sign $copy ('owned-mismatch-'+$case+'.json'); $release=Configure-Release $copy $path
            Reject { Assert-ReleaseAllowlist (Read-SignedFeed $path) $release (Join-Path $script:UpdaterRoot 'Downloads') }
        }
    }
    Check 'Fake GitHub approved owned-mod release installs and rolls back only the synthetic profile mod' {
        $manifestPath=$ownedManifestPath; $zip=$ownedZip
        $null=Configure-Release $ownedFeed $ownedSigned
        Invoke-Updater
        Assert ((Get-Content -LiteralPath (Join-Path $profile 'Mods\DingDongBiomeHomes.tmod') -Raw) -eq 'synthetic-public-owned-mod') 'Owned mod not applied.'
        Assert ((Get-Content -LiteralPath (Join-Path $game 'tModLoader.dll') -Raw) -eq 'synthetic-user-change') 'Owned mod changed Game.'
        $b=@(Get-ChildItem -LiteralPath (Join-Path $script:UpdaterRoot 'Backups') -Directory | Where-Object {(Get-Content -LiteralPath (Join-Path $_.FullName 'transaction.json') -Raw | ConvertFrom-Json).sequence -eq 25})[0].FullName
        Restore-Transaction $b
        Assert ((Get-Content -LiteralPath (Join-Path $profile 'Mods\DingDongBiomeHomes.tmod') -Raw) -eq 'synthetic-old-mod') 'Owned mod rollback failed.'
        Assert ((Get-Content -LiteralPath (Join-Path $profile 'Players\test.plr') -Raw) -eq 'synthetic-player') 'Owned mod changed a save.'
    }
} finally {
    if($testKey){$testKey.Dispose()}
    $resultPath=Join-Path $testRoot ('test-results-ps'+$PSVersionTable.PSVersion.Major+'.json')
    [IO.File]::WriteAllText($resultPath,([ordered]@{utc=[DateTime]::UtcNow.ToString('o');elapsedSeconds=([DateTime]::UtcNow-$started).TotalSeconds;runtime=[string]$PSVersionTable.PSVersion;updaterSha256=(Hash $updater);fixtureRoot=$work;network='MOCK_ONLY';gameLaunch='NOT_RUN';results=@($results.ToArray())} | ConvertTo-Json -Depth 12),$script:Utf8)
}
Write-Host ('Completed: '+$results.Count+' targeted offline checks.')
