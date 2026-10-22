# DingDong project tool source. Licensing: see repository LEGAL-STATUS.md.
# DingDong project updater; contains no game, mod payload, credential, or private key.
[CmdletBinding()]
param([switch]$DownloadOnly,[string]$GamePath,[string]$ProfilePath,[string]$RollbackBackup,[switch]$NonInteractive,
    [string]$Owner='BlackBadHope',[string]$Repository='DingDong')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$script:UpdaterRoot=$PSScriptRoot
$script:UpdaterVersion=5
$script:PublicKey='<RSAKeyValue><Modulus>qxGugwJvlEOuTJzDo5k7Tu+98wXRDlc2ZXzGNshTHbOQ3YJpyykE/20fTBQDrsxNX5a7T7k3vWuzpVX/CvmHsh6xWbIUjtYraktmXU0QYP9DlH1uoGfWa73cabPygnDqRZMqg9WnA4+7KM/xmNITxhZqkrmHYCww583+BZTtO+xdYsrbtyL7qkJ2DMA6B3GKl2Ik4w3hY5qMQVEQCZnn2i2ISsdBNk1xV4JaoRbDAJZMAWw2p+85LL+yvB9QlGWoi9v8nm3CWf+xlQNuJ/8qZZ0Ji0GjHTv+UTe4WUbKm5tJ7jzwMDZA4pgGVRqszfO6qIAfBoEhK/8tGOBa8KjFs7pccD6gTFPD6rdnZeS/tmJoXg/gaVJOFdAP2pAbyyHyTuuB1rw6TKxYGzMB2OMcOwXFV9cfP2YzbpLeErrbbeiosQI9ssc5Jz1IMApAl9EsYOojjq/hspfCNZFIEsvPb2meprYR2GomdMNM2+c2FlOOJOtYfKIzRCNlK7CN/dNZ</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>'
$script:GitHubOwner=$Owner
$script:GitHubRepository=$Repository
$script:Utf8=New-Object Text.UTF8Encoding($false)
[AppContext]::SetSwitch('Switch.System.IO.UseLegacyPathHandling',$false)
[AppContext]::SetSwitch('Switch.System.IO.BlockLongPaths',$false)
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Save-Json($Value,[string]$Path) {
    [IO.File]::WriteAllText($Path+'.tmp',($Value | ConvertTo-Json -Depth 12),$script:Utf8)
    Flush-File ($Path+'.tmp')
    if([IO.File]::Exists($Path)) { [IO.File]::Replace($Path+'.tmp',$Path,[NullString]::Value) }
    else { [IO.File]::Move($Path+'.tmp',$Path) }
}
function Flush-File([string]$Path) {
    $handle=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try { $handle.Flush($true) } finally { $handle.Dispose() }
}
function Safe-Child([string]$Root,[string]$Relative) {
    if([string]::IsNullOrWhiteSpace($Relative) -or $Relative -match '[\x00-\x1f:*?"<>|]' -or [IO.Path]::IsPathRooted($Relative)) { throw 'Unsafe file path.' }
    foreach($part in ($Relative -split '[\\/]')) {
        if(!$part -or $part -in @('.','..') -or $part -match '[ .]$' -or $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') { throw 'Unsafe path component.' }
    }
    $rootFull=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    $path=[IO.Path]::GetFullPath((Join-Path $rootFull $Relative))
    if(!$path.StartsWith($rootFull+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Path escapes its root.' }
    $probe=$path
    while($probe) {
        if(Test-Path -LiteralPath $probe) { if((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Junctions and symbolic links are not supported for updates.' } }
        $next=Split-Path -Parent $probe
        if($next -eq $probe) { break }; $probe=$next
    }
    if($path.Length -ge 240 -and !$path.StartsWith('\\?\')) { if($path.StartsWith('\\')) { return '\\?\UNC\'+$path.Substring(2) }; return '\\?\'+$path }
    $path
}
function Stored-File([string]$Root,$File) {
    $algorithm=[Security.Cryptography.SHA256]::Create()
    try { $id=([BitConverter]::ToString($algorithm.ComputeHash($script:Utf8.GetBytes($File.area+'/'+$File.path.Replace('\','/'))))).Replace('-','').ToLowerInvariant() } finally { $algorithm.Dispose() }
    Safe-Child $Root ($id+'.bin')
}
function Pending-File([string]$Root,$File,[string]$TransactionId,[string]$Purpose) {
    if($TransactionId -notmatch '^[a-f0-9]{32}$' -or $Purpose -notin @('pending','restore')) { throw 'Invalid transaction staging path.' }
    $name='.dd-'+$TransactionId+'-'+[IO.Path]::GetFileNameWithoutExtension((Stored-File $Root $File))+'.'+$Purpose
    $parent=Split-Path -Parent $File.path
    Safe-Child $Root $(if($parent){Join-Path $parent $name}else{$name})
}
function Commit-File([string]$Prepared,[string]$Target) {
    # Same-directory staging guarantees the rename/replace stays on the same volume.
    if([IO.File]::Exists($Target)) { [IO.File]::Replace($Prepared,$Target,[NullString]::Value) }
    else { [IO.File]::Move($Prepared,$Target) }
}
function Assert-Managed($File) {
    $rel=[string]$File.path
    $null=Safe-Child $env:TEMP $rel
    if($rel -match '(?i)(^|[\\/])(Players|Worlds|secrets|\.ssh|DingDong-Update)([\\/.]|$)') { throw 'Protected path in update.' }
    if($File.area -eq 'Profile') {
        if($rel -notmatch '^(?i:Mods[/\\][^/\\]+\.(tmod|json)|ModConfigs[/\\][^/\\]+\.json)$') { throw 'Unsupported profile file.' }
        if($rel -match '^(?i:ModConfigs)[/\\]' -and $File.mode -ne 'ifMissing') { throw 'Existing mod configuration cannot be overwritten.' }
    } elseif($File.area -eq 'Game') {
        if($rel -notmatch '^(?i:(Content|DedicatedServerUtils|dotnet|LaunchUtils|Libraries|tModPorter)[/\\]|tModLoader\.(dll|pdb|xml|deps\.json|runtimeconfig(\.dev)?\.json)$|tMLMod\.targets$|tModPorter\.dll\.config$|steam_appid\.txt$|RecentGitHubCommits\.txt$|start-tModLoader[^/\\]*\.(bat|sh)$)') { throw 'Unsupported game file.' }
    } elseif($File.area -eq 'Bootstrap') {
        # Public source/tools are staged for an explicit local build, never executed.
        if($rel -notmatch '^(?i:Source[/\\].+\.(cs|csproj|sln|props|targets|json|txt|md|hjson|png|jpg|jpeg|gif|webp|ogg|wav|fx|csv|xml|resx)|Tools[/\\][^/\\]+\.(ps1|cmd|json|md))$') { throw 'Unsupported bootstrap file.' }
    } else { throw 'Unsupported update area.' }
    if($File.mode -notin @('replace','ifMissing') -or $File.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or [long]$File.bytes -lt 0) { throw 'Invalid file metadata.' }
}
function Read-VerifiedEnvelope([string]$Path) {
    if((Get-Item -LiteralPath $Path).Length -gt 4194304) { throw 'Update feed is too large.' }
    $envelope=[IO.File]::ReadAllText($Path) | ConvertFrom-Json
    $bytes=[Convert]::FromBase64String($envelope.payloadBase64)
    $signature=[Convert]::FromBase64String($envelope.signatureBase64)
    $rsa=New-Object Security.Cryptography.RSACryptoServiceProvider
    try { $rsa.FromXmlString($script:PublicKey); if(!$rsa.VerifyData($bytes,'SHA256',$signature)) { throw 'Update signature is invalid.' } } finally { $rsa.Dispose() }
    $script:Utf8.GetString($bytes) | ConvertFrom-Json
}
function Assert-PublicFile($File) {
    Assert-Managed $File
    if($File.area -eq 'Game') { throw 'Public releases cannot carry the game runtime. Use official Steam.' }
    if($File.area -eq 'Profile' -and $File.path -match '(?i)\.tmod$') {
        if($File.area -cne 'Profile' -or $File.path -cne 'Mods/DingDongBiomeHomes.tmod' -or $File.rights -cne 'project-owned' -or $File.license -cne 'LicenseRef-DingDong-Personal-Use-1.0' -or $File.decision -cne 'approved' -or $File.provenanceId -cnotmatch '^dingdong-owned-v1:sha256:[a-f0-9]{64}$' -or $File.source -cnotmatch '^https://github\.com/BlackBadHope/DingDong/tree/[a-f0-9]{40}/game-mod/DingDongBiomeHomes$') { throw 'Compiled mods require the exact approved project-owned DingDong artifact and pinned provenance metadata.' }
    }
    if($File.license -notmatch '^[A-Za-z0-9][A-Za-z0-9.+-]{0,99}$' -or $File.license -match '^(?i:UNKNOWN|NOASSERTION|NONE)$' -or $File.provenanceId -notmatch '^[A-Za-z0-9][A-Za-z0-9._:/-]{0,159}$') { throw 'Public file lacks reviewed license/provenance metadata.' }
    $source=[uri]$File.source
    if(!$source.IsAbsoluteUri -or $source.Scheme -ne 'https' -or $source.UserInfo -or $source.Fragment) { throw 'Public file lacks an HTTPS source reference.' }
}
function Read-SignedFeed([string]$Path) {
    $feed=Read-VerifiedEnvelope $Path
    if($feed.format -ne 'dingdong-update-github-v1' -or $feed.minUpdater -gt $script:UpdaterVersion -or $feed.sequence -lt 23) { throw 'Unsupported release feed. Download the current updater from this project GitHub repository.' }
    if($feed.source.owner -ine $script:GitHubOwner -or $feed.source.repository -ine $script:GitHubRepository -or $feed.source.tag -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') { throw 'Signed release source does not match this GitHub repository.' }
    # PowerShell 7 converts ISO JSON dates to DateTime automatically. Parsing
    # that value again through a culture-dependent string can swap month/day.
    $expires=if($feed.expiresUtc -is [DateTime]){$feed.expiresUtc.ToUniversalTime()}else{
        [DateTime]::Parse([string]$feed.expiresUtc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    }
    if($expires -lt [DateTime]::UtcNow) { throw 'Update feed has expired. Ask for a renewed release feed.' }
    if($feed.status -eq 'pending') {
        if(!$feed.PSObject.Properties['blockedReason'] -or [string]$feed.blockedReason -notmatch '^[^\x00-\x1f]{1,500}$') { throw 'Pending release lacks a safe, explicit blocked reason.' }
        return $feed
    }
    if($feed.status -ne 'released' -or $feed.version -notmatch '^\d+\.\d+\.\d+$') { throw 'This update is not a released build.' }
    if($feed.distribution.policy -ne 'public-allowlist-v1' -or $feed.distribution.reviewed -isnot [bool] -or !$feed.distribution.reviewed -or $feed.distribution.manifestSha256 -notmatch '^[a-fA-F0-9]{64}$' -or $feed.distribution.manifestFile -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*\.json$') { throw 'Release lacks a reviewed public distribution allowlist.' }
    if($feed.payload.file -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*\.zip$' -or $feed.payload.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or [long]$feed.payload.bytes -le 0 -or [long]$feed.payload.bytes -gt 2GB) { throw 'Invalid payload.' }
    $seen=@{}; $total=[long]0
    foreach($f in $feed.files) { Assert-PublicFile $f; $k=$f.area+'/'+$f.path.Replace('\','/'); if($seen.ContainsKey($k)) { throw 'Duplicate update file.' }; $seen[$k]=$true; $total += [long]$f.bytes }
    if($feed.files.Count -lt 1 -or $feed.files.Count -gt 30000 -or $total -gt 12GB) { throw 'Invalid payload size or file count.' }
    $feed
}
function Assert-GitHubSource {
    if($script:GitHubOwner -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,38}$' -or $script:GitHubRepository -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]{0,99}$') { throw 'Invalid GitHub owner or repository.' }
}
function Get-ReleaseApiUri {
    Assert-GitHubSource
    [uri]('https://api.github.com/repos/'+$script:GitHubOwner+'/'+$script:GitHubRepository+'/releases/latest')
}
function Assert-GitHubUri([uri]$Uri,[string]$Kind,[uri]$Original,[int]$Hop) {
    if(!$Uri.IsAbsoluteUri -or $Uri.Scheme -ne 'https' -or $Uri.Port -ne 443 -or $Uri.UserInfo -or $Uri.Fragment) { throw 'Unsupported GitHub update URL.' }
    if($Kind -eq 'Api') {
        if($Hop -ne 0 -or $Uri.AbsoluteUri -ine (Get-ReleaseApiUri).AbsoluteUri) { throw 'Unexpected GitHub API URL or redirect.' }
    } elseif($Kind -eq 'Asset') {
        $prefix='/'+$script:GitHubOwner+'/'+$script:GitHubRepository+'/releases/download/'
        if($Uri.Host -ieq 'github.com') {
            if(!$Uri.AbsolutePath.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or $Uri.Query -or ($Hop -gt 0 -and $Uri.AbsoluteUri -cne $Original.AbsoluteUri)) { throw 'Asset URL leaves the configured GitHub release.' }
        } elseif($Hop -lt 1 -or $Uri.Host -notin @('release-assets.githubusercontent.com','objects.githubusercontent.com','github-releases.githubusercontent.com')) { throw 'Redirect leaves GitHub release asset storage.' }
    } else { throw 'Invalid download kind.' }
}
function Open-GitHubResponse([uri]$Uri,[string]$Kind) {
    [Net.ServicePointManager]::SecurityProtocol=[Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    $request=[Net.HttpWebRequest]::Create($Uri)
    $request.AllowAutoRedirect=$false; $request.Timeout=20000; $request.ReadWriteTimeout=30000
    $request.UseDefaultCredentials=$false; $request.Credentials=$null
    $request.UserAgent='DingDong-Updater/5 (public GitHub Releases)'
    $request.Accept=if($Kind -eq 'Api'){'application/vnd.github+json'}else{'application/octet-stream'}
    $request.Headers.Add('Cache-Control','no-cache')
    $request.GetResponse()
}
function Fetch([uri]$Uri,[string]$Destination,[long]$Limit,[string]$Kind='Asset') {
    $original=$Uri
    for($hop=0;$hop -le 5;$hop++) {
        Assert-GitHubUri $Uri $Kind $original $hop
        $response=$null; $input=$null; $output=$null
        try {
            $response=Open-GitHubResponse $Uri $Kind
            $status=[int]$response.StatusCode
            if($status -in @(301,302,303,307,308)) {
                if($Kind -eq 'Api' -or $hop -eq 5 -or !$response.Headers['Location']) { throw 'Invalid GitHub redirect.' }
                $Uri=[uri]::new($Uri,[string]$response.Headers['Location']); continue
            }
            if($status -ne 200 -or $response.ContentLength -gt $Limit) { throw 'Invalid GitHub response or size.' }
            $input=$response.GetResponseStream(); $output=[IO.File]::Open($Destination,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
            $buffer=New-Object byte[] 131072; $count=[long]0
            while(($read=$input.Read($buffer,0,$buffer.Length)) -gt 0) { $count += $read; if($count -gt $Limit) { throw 'Download exceeds its size limit.' }; $output.Write($buffer,0,$read) }
            return
        } catch { throw 'GitHub download failed (missing release, API limit, connection, redirect, or size check). No payload was installed.' }
        finally { if($output){$output.Dispose()}; if($input){$input.Dispose()}; if($response){$response.Close()} }
    }
    throw 'Too many GitHub redirects.'
}
function Get-GitHubRelease([string]$Cache) {
    $path=Join-Path $Cache 'release-api.json.part'
    Fetch (Get-ReleaseApiUri) $path 4194304 'Api'
    $release=[IO.File]::ReadAllText($path) | ConvertFrom-Json
    if($release.draft -isnot [bool] -or $release.draft -or $release.prerelease -isnot [bool] -or $release.prerelease -or $release.tag_name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$' -or !$release.published_at -or !$release.assets) { throw 'GitHub has no supported published release.' }
    $release
}
function Get-ReleaseAsset($Release,[string]$Name) {
    if($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') { throw 'Invalid GitHub asset name.' }
    $assets=@($Release.assets | Where-Object { $_.name -ceq $Name })
    if($assets.Count -ne 1 -or $assets[0].state -ne 'uploaded' -or [long]$assets[0].size -le 0) { throw 'Release asset is missing, incomplete, or duplicated.' }
    $uri=[uri]$assets[0].browser_download_url
    $expected='https://github.com/'+$script:GitHubOwner+'/'+$script:GitHubRepository+'/releases/download/'+$Release.tag_name+'/'+$Name
    if($uri.AbsoluteUri -cne $expected) { throw 'Asset is outside the selected repository, tag, or file.' }
    [pscustomobject]@{Uri=$uri;Bytes=[long]$assets[0].size}
}
function Assert-ReleaseAllowlist($Feed,$Release,[string]$Cache) {
    $asset=Get-ReleaseAsset $Release $Feed.distribution.manifestFile
    if($asset.Bytes -gt 4194304) { throw 'Public allowlist is too large.' }
    $path=Join-Path $Cache $Feed.distribution.manifestFile
    Fetch $asset.Uri ($path+'.part') $asset.Bytes
    if((Get-Item -LiteralPath ($path+'.part')).Length -ne $asset.Bytes -or (Hash ($path+'.part')) -cne $Feed.distribution.manifestSha256.ToLowerInvariant()) { throw 'Public allowlist checksum or size mismatch.' }
    $manifest=[IO.File]::ReadAllText($path+'.part') | ConvertFrom-Json
    if($manifest.format -cne 'dingdong-public-allowlist-v1' -or @($manifest.files).Count -ne @($Feed.files).Count) { throw 'Public allowlist format or entry count mismatch.' }
    $entries=@{}
    foreach($entry in $manifest.files) {
        Assert-PublicFile $entry
        $key=$entry.area+'/'+$entry.path.Replace('\','/')
        if($entry.decision -cne 'approved' -or $entries.ContainsKey($key)) { throw 'Public allowlist has an unapproved or duplicate entry.' }
        $entries[$key]=$entry
    }
    foreach($file in $Feed.files) {
        $key=$file.area+'/'+$file.path.Replace('\','/')
        if(!$entries.ContainsKey($key)) { throw 'Signed file is missing from the reviewed public allowlist.' }
        $entry=$entries[$key]
        $fields=@('area','path','mode','sha256','license','source','provenanceId')
        if($file.area -eq 'Profile' -and $file.path -match '(?i)\.tmod$') { $fields += @('rights','decision') }
        foreach($field in $fields) {
            if([string]$entry.$field -cne [string]$file.$field) { throw 'Signed file metadata differs from its approved public allowlist entry.' }
        }
        if([long]$entry.bytes -ne [long]$file.bytes) { throw 'Signed file size differs from its approved public allowlist entry.' }
    }
    Move-Item -LiteralPath ($path+'.part') -Destination $path -Force
}
function Assert-GameClosed([string]$Game,[string]$Profile) {
    if(!$Game -and !$Profile) { return } # Source-only bootstrap touches no running installation.
    foreach($p in @(Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='tModLoader.exe' OR Name='Terraria.exe' OR Name='TerrariaServer.exe'")) {
        $exe=[string]$p.ExecutablePath; $cmd=[string]$p.CommandLine
        if(!$exe -and !$cmd) {
            # A process can exit between the CIM enumeration and its metadata read.
            $still=Get-CimInstance Win32_Process -Filter ('ProcessId='+$p.ProcessId)
            if(!$still) { continue }
            $exe=[string]$still.ExecutablePath; $cmd=[string]$still.CommandLine
            if(!$exe -and !$cmd) { throw 'Cannot verify that the game is closed. Run the updater with the same Windows user as the game.' }
        }
        if(($Game -and $exe -and $exe.StartsWith($Game.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) -or ($cmd -and (($Game -and $cmd.IndexOf($Game,[StringComparison]::OrdinalIgnoreCase) -ge 0) -or ($Profile -and $cmd.IndexOf($Profile,[StringComparison]::OrdinalIgnoreCase) -ge 0)))) { throw 'GAME_RUNNING: close tModLoader and its server, then run this shortcut again. The downloaded update will be reused.' }
    }
}
function Get-AreaRoot([string]$Game,[string]$Profile,[string]$Area) {
    switch -Exact ($Area) {
        'Game' { if(!$Game){throw 'Game path is missing.'}; return $Game }
        'Profile' { if(!$Profile){throw 'Profile path is missing.'}; return $Profile }
        'Bootstrap' { return (Join-Path $script:UpdaterRoot 'Bootstrap') }
        default { throw 'Unsupported update area.' }
    }
}
function Find-SteamTModLoader {
    $steamRoots=@()
    $steam=Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
    if($steam -and $steam.PSObject.Properties['SteamPath']) { $steamRoots += [string]$steam.SteamPath }
    if(${env:ProgramFiles(x86)}) { $steamRoots += Join-Path ${env:ProgramFiles(x86)} 'Steam' }
    $libraries=@($steamRoots)
    foreach($root in $steamRoots) {
        $vdf=Join-Path $root 'steamapps\libraryfolders.vdf'
        if(Test-Path -LiteralPath $vdf -PathType Leaf) {
            foreach($match in [regex]::Matches([IO.File]::ReadAllText($vdf),'"path"\s+"([^"\r\n]+)"')) { $libraries += $match.Groups[1].Value.Replace('\\','\') }
        }
    }
    $found=@(foreach($library in ($libraries | Select-Object -Unique)) {
        $manifest=Join-Path $library 'steamapps\appmanifest_1281930.acf'
        $game=Join-Path $library 'steamapps\common\tModLoader'
        if((Test-Path -LiteralPath $manifest -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $game 'tModLoader.dll') -PathType Leaf)) { [IO.Path]::GetFullPath($game) }
    })
    if($found.Count -eq 1) { return $found[0] }
    if($found.Count -gt 1) { throw 'Multiple Steam tModLoader installations found. Specify GamePath.' }
}
function Resolve-Targets([string]$Game,[string]$Profile) {
    $settings=Join-Path $script:UpdaterRoot 'installation.json'
    if(!$Game -and !$Profile -and (Test-Path -LiteralPath $settings)) { $cfg=Get-Content -LiteralPath $settings -Raw | ConvertFrom-Json; $Game=$cfg.gamePath; $Profile=$cfg.profilePath }
    if(!$Profile) {
        $found=@(); $roots=@([Environment]::GetFolderPath('MyDocuments'),(Join-Path $env:USERPROFILE 'Documents'))
        if($env:OneDrive) { $roots += Join-Path $env:OneDrive 'Documents' }
        foreach($d in ($roots | Select-Object -Unique)) { $p=Join-Path $d 'My Games\Terraria\tModLoader'; if(Test-Path -LiteralPath (Join-Path $p 'Mods\DingDongBiomeHomes.tmod')) { $found += [IO.Path]::GetFullPath($p) } }
        $found=@($found | Select-Object -Unique)
        if($found.Count -eq 1) { $Profile=$found[0] }
        elseif($found.Count -gt 1) { throw 'Multiple DingDong profiles found. Specify ProfilePath in installation.json.' }
    }
    if($Profile -and !$Game) { $marker=Join-Path $Profile '.dingdong-modpack-install.json'; if(Test-Path -LiteralPath $marker) { $m=Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json; $Game=$m.tmlPath } }
    if(!$Game) { $Game=Find-SteamTModLoader }
    if((!$Game -or !$Profile) -and !$NonInteractive) {
        Add-Type -AssemblyName System.Windows.Forms
        if(!$Game) { $dialog=New-Object Windows.Forms.FolderBrowserDialog; $dialog.Description='Select the installed tModLoader folder (contains tModLoader.dll)'; if($dialog.ShowDialog() -eq 'OK') { $Game=$dialog.SelectedPath }; $dialog.Dispose() }
        if(!$Profile) { $dialog=New-Object Windows.Forms.FolderBrowserDialog; $dialog.Description='Select your tModLoader profile (contains Mods/DingDongBiomeHomes.tmod)'; if($dialog.ShowDialog() -eq 'OK') { $Profile=$dialog.SelectedPath }; $dialog.Dispose() }
    }
    if(!$Game -or !$Profile) { throw 'Installation not found. Specify GamePath and ProfilePath.' }
    $Game=[IO.Path]::GetFullPath($Game).TrimEnd('\'); $Profile=[IO.Path]::GetFullPath($Profile).TrimEnd('\')
    foreach($p in @((Safe-Child $Game 'tModLoader.dll'),(Safe-Child $Profile 'Mods\DingDongBiomeHomes.tmod'))) { if(!(Test-Path -LiteralPath $p -PathType Leaf)) { throw 'This folder does not contain an installed DingDong build.' } }
    if($Game -eq $Profile -or $Game.StartsWith($Profile+'\',[StringComparison]::OrdinalIgnoreCase) -or $Profile.StartsWith($Game+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Game and profile folders must be separate.' }
    Save-Json ([ordered]@{gamePath=$Game;profilePath=$Profile}) $settings
    @{Game=$Game;Profile=$Profile}
}
function Expand-Payload([string]$Zip,[string]$Stage,$Feed) {
    $archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        $expected=@{}; foreach($f in $Feed.files) { $expected[$f.area+'/'+$f.path.Replace('\','/')]=$f }
        $seen=@{}
        foreach($entry in $archive.Entries) {
            $name=$entry.FullName.Replace('\','/')
            if(!$expected.ContainsKey($name) -or $seen.ContainsKey($name)) { throw 'Unexpected or duplicate ZIP entry.' }
            $f=$expected[$name]; if($entry.Length -ne [long]$f.bytes) { throw 'ZIP entry size mismatch.' }
            $path=Stored-File $Stage $f; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$path,$false)
            if((Hash $path) -ne $f.sha256) { throw 'ZIP entry checksum mismatch.' }; $seen[$name]=$true
        }
        if($seen.Count -ne $expected.Count) { throw 'The update ZIP is incomplete.' }
    } finally { $archive.Dispose() }
}
function Restore-Transaction([string]$Backup) {
    $backupFull=[IO.Path]::GetFullPath($Backup)
    $allowed=[IO.Path]::GetFullPath((Join-Path $script:UpdaterRoot 'Backups')).TrimEnd('\')+'\'
    if(!$backupFull.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Backup must be inside this updater Backups folder.' }
    $receiptPath=Safe-Child $backupFull 'transaction.json'; $r=Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    if($r.format -notin @('dingdong-update-transaction-v1','dingdong-update-transaction-v2')) { throw 'Invalid backup receipt.' }
    $v2=$r.format -eq 'dingdong-update-transaction-v2'
    if($v2 -and $r.transactionId -notmatch '^[a-f0-9]{32}$') { throw 'Invalid transaction ID.' }
    Assert-GameClosed $r.gamePath $r.profilePath
    foreach($f in $r.files) {
        Assert-Managed $f; $root=Get-AreaRoot $r.gamePath $r.profilePath $f.area; $target=Safe-Child $root $f.path
        $current=if(Test-Path -LiteralPath $target -PathType Leaf){Hash $target}else{''}
        if($current -and $current -ne $f.sha256 -and $current -ne $f.originalSha256) { throw 'A file has changed since this update. Automatic rollback stopped to preserve your changes.' }
        if($f.existed) { $src=Stored-File $backupFull $f; if((Hash $src) -ne $f.originalSha256) { throw 'Backup checksum mismatch.' } }
    }
    $r.status='rolling-back'; Save-Json $r $receiptPath
    foreach($f in $r.files) {
        Assert-GameClosed $r.gamePath $r.profilePath
        $root=Get-AreaRoot $r.gamePath $r.profilePath $f.area; $target=Safe-Child $root $f.path
        $current=if(Test-Path -LiteralPath $target -PathType Leaf){Hash $target}else{''}
        if($current -and $current -ne $f.sha256 -and $current -ne $f.originalSha256) { throw 'A file changed during rollback.' }
        if($f.existed -and $current -ne $f.originalSha256) {
            $transactionId=if($v2){$r.transactionId}else{[Guid]::NewGuid().ToString('N')}
            $prepared=Pending-File $root $f $transactionId 'restore'
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $prepared))
            Copy-Item -LiteralPath (Stored-File $backupFull $f) -Destination $prepared -Force
            Flush-File $prepared
            if((Hash $prepared) -ne $f.originalSha256){throw 'Prepared rollback checksum mismatch.'}
            Assert-GameClosed $r.gamePath $r.profilePath
            Commit-File $prepared $target
            if((Hash $target) -ne $f.originalSha256){throw 'Rollback verification failed.'}
        } elseif(!$f.existed -and (Test-Path -LiteralPath $target)) { Remove-Item -LiteralPath $target -Force }
        if($v2) { foreach($purpose in @('pending','restore')) { $temp=Pending-File $root $f $r.transactionId $purpose; if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Force} } }
    }
    $r.status='rolled-back'; Save-Json $r $receiptPath
    Write-Host 'Rollback complete.'
}
function Install-Payload($Feed,[string]$Stage,$Targets) {
    Assert-GameClosed $Targets.Game $Targets.Profile
    $plan=@()
    foreach($f in $Feed.files) {
        $root=Get-AreaRoot $Targets.Game $Targets.Profile $f.area; $target=Safe-Child $root $f.path
        $exists=Test-Path -LiteralPath $target -PathType Leaf
        if($exists -and $f.mode -eq 'ifMissing') { continue }
        $old=if($exists){Hash $target}else{''}; if($old -eq $f.sha256){continue}
        $src=Stored-File $Stage $f; if((Hash $src) -ne $f.sha256){throw 'Staged file has changed.'}
        $plan += [ordered]@{area=$f.area;path=$f.path;mode=$f.mode;sha256=$f.sha256;bytes=$f.bytes;existed=$exists;originalSha256=$old;phase='prepared'}
    }
    if(!$plan.Count) { Write-Host ('Already applied: '+$Feed.version); return }
    $backup=Join-Path $script:UpdaterRoot ('Backups\'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($backup)
    foreach($f in $plan) { if($f.existed) { $root=Get-AreaRoot $Targets.Game $Targets.Profile $f.area; $dest=Stored-File $backup $f; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $dest)); Copy-Item -LiteralPath (Safe-Child $root $f.path) -Destination $dest; if((Hash $dest) -ne $f.originalSha256){throw 'Backup verification failed.'} } }
    $receipt=[ordered]@{format='dingdong-update-transaction-v2';transactionId=[Guid]::NewGuid().ToString('N');status='in-progress';version=$Feed.version;sequence=$Feed.sequence;gamePath=$Targets.Game;profilePath=$Targets.Profile;files=$plan}
    $receiptPath=Join-Path $backup 'transaction.json'; Save-Json $receipt $receiptPath
    try {
        foreach($f in $plan) {
            Assert-GameClosed $Targets.Game $Targets.Profile
            $root=Get-AreaRoot $Targets.Game $Targets.Profile $f.area; $target=Safe-Child $root $f.path
            $current=if(Test-Path -LiteralPath $target -PathType Leaf){Hash $target}else{''}; if($current -ne $f.originalSha256){throw 'A game file changed during installation.'}
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $target))
            $prepared=Pending-File $root $f $receipt.transactionId 'pending'
            $f.phase='staging'; Save-Json $receipt $receiptPath
            Copy-Item -LiteralPath (Stored-File $Stage $f) -Destination $prepared -Force
            Flush-File $prepared
            if((Hash $prepared) -ne $f.sha256){throw 'Prepared update checksum mismatch.'}
            $f.phase='committing'; Save-Json $receipt $receiptPath
            Assert-GameClosed $Targets.Game $Targets.Profile
            $current=if(Test-Path -LiteralPath $target -PathType Leaf){Hash $target}else{''}; if($current -ne $f.originalSha256){throw 'A game file changed before atomic replacement.'}
            Commit-File $prepared $target
            if((Hash $target) -ne $f.sha256){throw 'Installed checksum mismatch.'}
            $f.phase='committed'; Save-Json $receipt $receiptPath
        }
        Save-Json ([ordered]@{version=$Feed.version;sequence=$Feed.sequence;backup=$backup;installedUtc=[DateTime]::UtcNow.ToString('o')}) (Join-Path $script:UpdaterRoot 'installed.json')
        $receipt.status='complete'; Save-Json $receipt $receiptPath
    } catch {
        $installError=$_.Exception.Message
        try { Restore-Transaction $backup } catch { Write-Host ('Rollback deferred: '+$_.Exception.Message); Write-Host ('Backup: '+$backup) }
        throw ('Update stopped: '+$installError)
    }
    Write-Host ('Applied verified DingDong files '+$Feed.version+'. Backup: '+$backup)
}
function Recover-UnfinishedTransactions {
    $backups=Join-Path $script:UpdaterRoot 'Backups'
    if(!$DownloadOnly -and (Test-Path -LiteralPath $backups)) {
        $pending=@(foreach($dir in Get-ChildItem -LiteralPath $backups -Directory) {
            $journal=Join-Path $dir.FullName 'transaction.json'
            if(Test-Path -LiteralPath $journal) { $r=Get-Content -LiteralPath $journal -Raw | ConvertFrom-Json; if($r.status -in @('in-progress','rolling-back')) { [pscustomobject]@{path=$dir.FullName;receipt=$r} } }
        })
        if($pending.Count) {
            foreach($p in $pending) {
                if($p.receipt.gamePath -or $p.receipt.profilePath) {
                    $recoveryTargets=Resolve-Targets $GamePath $ProfilePath
                    if($p.receipt.gamePath -ne $recoveryTargets.Game -or $p.receipt.profilePath -ne $recoveryTargets.Profile) { throw 'An unfinished transaction belongs to another installation. Use its explicit rollback option.' }
                }
                Restore-Transaction $p.path
            }
        }
    }
}
function Invoke-Updater {
    if($RollbackBackup) { Restore-Transaction $RollbackBackup; return }
    Assert-GitHubSource
    $cache=Join-Path $script:UpdaterRoot 'Downloads'; [void][IO.Directory]::CreateDirectory($cache)
    $release=Get-GitHubRelease $cache
    $feedAsset=Get-ReleaseAsset $release 'latest.json'
    if($feedAsset.Bytes -gt 4194304) { throw 'Release feed is too large.' }
    $feedPath=Join-Path $cache 'latest.json'
    Fetch $feedAsset.Uri ($feedPath+'.part') $feedAsset.Bytes
    if((Get-Item -LiteralPath ($feedPath+'.part')).Length -ne $feedAsset.Bytes) { throw 'Release feed size mismatch.' }
    $feed=Read-SignedFeed ($feedPath+'.part'); Move-Item -LiteralPath ($feedPath+'.part') -Destination $feedPath -Force
    if($feed.source.tag -cne $release.tag_name) { throw 'Signed feed belongs to another release tag.' }
    if($feed.status -eq 'pending') {
        Write-Host ('No installable game release: '+$feed.blockedReason)
        Write-Host 'The signed GitHub feed is verified. Game and profile files were not changed.'
        return
    }
    Assert-ReleaseAllowlist $feed $release $cache
    $payloadAsset=Get-ReleaseAsset $release $feed.payload.file
    if($payloadAsset.Bytes -ne [long]$feed.payload.bytes) { throw 'GitHub asset size does not match its signed manifest.' }
    $installedPath=Join-Path $script:UpdaterRoot 'installed.json'
    if(Test-Path -LiteralPath $installedPath) { $last=Get-Content -LiteralPath $installedPath -Raw | ConvertFrom-Json; if($feed.sequence -lt $last.sequence){throw 'An older release feed was received.'} }
    $zip=Join-Path $cache $feed.payload.file
    if(!(Test-Path -LiteralPath $zip) -or (Get-Item -LiteralPath $zip).Length -ne [long]$feed.payload.bytes -or (Hash $zip) -ne $feed.payload.sha256) {
        Write-Host ('Downloading reviewed release '+$feed.version+'...')
        Fetch $payloadAsset.Uri ($zip+'.part') ([long]$feed.payload.bytes)
        if((Get-Item -LiteralPath ($zip+'.part')).Length -ne [long]$feed.payload.bytes -or (Hash ($zip+'.part')) -ne $feed.payload.sha256) { throw 'Downloaded archive checksum mismatch.' }
        Move-Item -LiteralPath ($zip+'.part') -Destination $zip -Force
    }
    Write-Host ('Verified download: '+$zip)
    if($DownloadOnly){return}
    # Recovery is a write operation and requires a verified released feed and allowlist.
    Recover-UnfinishedTransactions
    $targets=if(@($feed.files | Where-Object { $_.area -ne 'Bootstrap' }).Count){Resolve-Targets $GamePath $ProfilePath}else{@{Game='';Profile=''}}
    Assert-GameClosed $targets.Game $targets.Profile
    $stage=Join-Path $cache ('Stage-'+[Guid]::NewGuid().ToString('N')); [void][IO.Directory]::CreateDirectory($stage)
    Expand-Payload $zip $stage $feed
    Install-Payload $feed $stage $targets
}
if($MyInvocation.InvocationName -ne '.') {
    $lock=$null; $result=0
    try { $lock=[IO.File]::Open((Join-Path $script:UpdaterRoot '.updater.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); Invoke-Updater }
    catch { Write-Host $_.Exception.Message -ForegroundColor Yellow; $result=1 }
    finally { if($lock){$lock.Dispose()} }
    exit $result
}
