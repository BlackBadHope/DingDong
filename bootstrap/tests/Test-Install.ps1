param([string]$ReportPath=(Join-Path $PSScriptRoot 'test-results-install.json'))
$ErrorActionPreference='Stop'
$testReportPath=$ReportPath
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $repo 'updater\DingDong-Update.ps1')
. (Join-Path $repo 'bootstrap\Resolve-OfficialDependencies.ps1')
. (Join-Path $repo 'bootstrap\Install-Core.ps1')
$work=Join-Path $PSScriptRoot ('_work\'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($work)
$checks=New-Object 'Collections.Generic.List[object]'
function Check([string]$Name,[scriptblock]$Body){try{& $Body;$checks.Add(@{name=$Name;status='PASS'});Write-Host ('PASS '+$Name)}catch{$checks.Add(@{name=$Name;status='FAIL';error=$_.Exception.Message});throw}}
function Require($Value,[string]$Message){if(!$Value){throw $Message}}
function Reject([scriptblock]$Body){$failed=$false;try{& $Body|Out-Null}catch{$failed=$true};Require $failed 'Expected rejection.'}
function Put([string]$Path,[string]$Value){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $Path));[IO.File]::WriteAllText($Path,$Value)}
function Profile([string]$Name,[string]$Mod){$p=Join-Path $work $Name;Put (Join-Path $p 'Mods\Fixture.tmod') $Mod;$p}
try {
    Check 'All setup PowerShell sources parse' {
        foreach($path in @('Install-DingDong.ps1','bootstrap\Install-Core.ps1','bootstrap\Prepare-PublicDependencies.ps1','launcher\Launch-Installed.ps1','launcher\Launch-DingDong.ps1')){
            $tokens=$null;$errors=$null;$null=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo $path),[ref]$tokens,[ref]$errors)
            Require (!$errors.Count) ($path+': '+(($errors|ForEach-Object {$_.Message}) -join '; '))
        }
    }
    Check 'Nonempty foreign targets and protected ancestors are rejected' {
        $foreign=Join-Path $work 'Foreign';Put (Join-Path $foreign 'keep.txt') 'untouched'
        Reject {Assert-DDInstallRoot $foreign @()};Reject {Assert-DDInstallRoot $work @($foreign)}
        Require ((Get-Content -LiteralPath (Join-Path $foreign 'keep.txt') -Raw) -ceq 'untouched') 'Foreign target changed.'
    }
    $root=Join-Path $work 'Install';Initialize-DDInstallRoot $root
    Put (Join-Path $root 'Profile\Players\keep.plr') 'player';Put (Join-Path $root 'Profile\Worlds\keep.wld') 'world'
    Put (Join-Path $root 'Profile\ModConfigs\Fixture.json') 'custom'
    $saveHashes=@{};foreach($p in Get-ChildItem -LiteralPath (Join-Path $root 'Profile') -Recurse -File){$saveHashes[$p.FullName]=Hash $p.FullName}
    Check 'Fresh install commits managed Mods and keeps saves/configuration' {
        $prepared=Profile 'Fresh' 'v1';Put (Join-Path $prepared 'ModConfigs\Fixture.json') 'default'
        $script:first=Commit-DDInstall $root $prepared ([ordered]@{version='1.0.0';sequence=32;versionPath='Versions\fixture1'})
        Require ((Get-Content -LiteralPath (Join-Path $root 'Profile\Mods\Fixture.tmod') -Raw) -ceq 'v1') 'Fresh Mods missing.'
        foreach($p in $saveHashes.Keys){Require ((Hash $p) -ceq $saveHashes[$p]) 'Existing saves or config changed.'}
    }
    Check 'Update keeps a recoverable old Mods directory and pointer' {
        $prepared=Profile 'Update' 'v2'
        $script:second=Commit-DDInstall $root $prepared ([ordered]@{version='2.0.0';sequence=33;versionPath='Versions\fixture2'})
        Require ((Get-Content -LiteralPath (Join-Path $second.backup 'Mods\Fixture.tmod') -Raw) -ceq 'v1') 'Old Mods backup absent.'
        Require ((Get-Content -LiteralPath (Join-Path $root 'installation.json') -Raw|ConvertFrom-Json).version -ceq '2.0.0') 'Pointer not committed.'
    }
    Check 'Explicit rollback restores both Mods and version pointer' {
        $null=Restore-DDInstall $root $second.backup
        Require ((Get-Content -LiteralPath (Join-Path $root 'Profile\Mods\Fixture.tmod') -Raw) -ceq 'v1') 'Rollback Mods mismatch.'
        Require ((Get-Content -LiteralPath (Join-Path $root 'installation.json') -Raw|ConvertFrom-Json).version -ceq '1.0.0') 'Rollback pointer mismatch.'
        Reject {Restore-DDInstall $root $second.backup}
    }
    Check 'Failure after Mods replacement rolls back and removes new defaults' {
        $prepared=Profile 'Failed' 'broken';Put (Join-Path $prepared 'ModConfigs\New.json') 'default';Put (Join-Path $prepared 'zz-forbidden.plr') 'invalid'
        Reject {Commit-DDInstall $root $prepared ([ordered]@{version='broken'})}
        Require ((Get-Content -LiteralPath (Join-Path $root 'Profile\Mods\Fixture.tmod') -Raw) -ceq 'v1') 'Failed install lost old Mods.'
        Require (!(Test-Path -LiteralPath (Join-Path $root 'Profile\ModConfigs\New.json'))) 'Failed install left default behind.'
        foreach($p in $saveHashes.Keys){Require ((Hash $p) -ceq $saveHashes[$p]) 'Failure changed saves/config.'}
    }
    Check 'Interrupted rename is recoverable before journal advances' {
        $backup=Join-Path $root ('Backups\'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($backup)
        Move-Item -LiteralPath (Join-Path $root 'Profile\Mods') -Destination (Join-Path $backup 'Mods')
        Put (Join-Path $root 'Profile\Mods\Fixture.tmod') 'interrupted'
        Copy-Item -LiteralPath (Join-Path $root 'installation.json') -Destination (Join-Path $backup 'installation-before.json')
        Write-DDJson ([ordered]@{format='dingdong-install-transaction-v1';root=$root;hadMods=$true;status='prepared';createdDefaults=@()}) (Join-Path $backup 'transaction.json')
        $null=Restore-DDInstall $root $backup
        Require ((Get-Content -LiteralPath (Join-Path $root 'Profile\Mods\Fixture.tmod') -Raw) -ceq 'v1') 'Interrupted transaction not restored.'
    }
    Check 'Rollback cannot target another directory' {Reject {Restore-DDInstall $root $work}}
    Check 'Running-game guard executes before transaction writes' {
        $before=@(Get-ChildItem -LiteralPath (Join-Path $root 'Backups') -Directory).Count
        function Assert-GameClosed {param($Game,$Profile);throw 'GAME_RUNNING'}
        Reject {Commit-DDInstall $root (Profile 'Running' 'x') ([ordered]@{version='x'})}
        Require (@(Get-ChildItem -LiteralPath (Join-Path $root 'Backups') -Directory).Count -eq $before) 'Running guard was too late.'
    }
    $plan=[pscustomobject]@{format='dingdong-install-v1';source=@{owner='BlackBadHope';repository='DingDong';tag='v0.29.2-public'};version='0.29.2';sequence=32;minInstaller=1;expiresUtc=[DateTime]::UtcNow.AddDays(1).ToString('o');bundle=@{file='DingDong-Public-0.29.2.zip';sha256=('a'*64);bytes=1};files=@()}
    foreach($path in @('Mods/DingDongBiomeHomes.tmod','Bootstrap/public-dependencies.json','Bootstrap/public-owned-mod.json','Tools/SteamLicenseGate.dll','Tools/EnginePatch.dll','Tools/SourceBuild.dll','Bootstrap/Build-OfficialOre.ps1','Tools/Launch-Installed.ps1')){
        $plan.files+=@{path=$path;sha256=('a'*64);bytes=1;decision='approved';rights='project-owned';sourceCommit=('a'*40)}
    }
    $release=[pscustomobject]@{tag_name='v0.29.2-public'}
    Check 'Signed plan binds repository/tag, expiry and complete own allowlist' {
        Assert-DDInstallPlan $plan $release
        $bad=$plan|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.source.tag='other';Reject {Assert-DDInstallPlan $bad $release}
        $bad=$plan|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.expiresUtc='2020-01-01T00:00:00Z';Reject {Assert-DDInstallPlan $bad $release}
        $bad=$plan|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.files[0].path='../escape';Reject {Assert-DDInstallPlan $bad $release}
        $bad=$plan|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.files+=@($bad.files[0]);Reject {Assert-DDInstallPlan $bad $release}
    }
    Check 'Own bundle rejects corrupt bytes and undeclared ZIP entries' {
        $content=Join-Path $work 'BundleSource';Put (Join-Path $content 'Tools\Fixture.dll') 'owned';$zip=Join-Path $work 'fixture.zip'
        [IO.Compression.ZipFile]::CreateFromDirectory($content,$zip)
        $p=[pscustomobject]@{files=@([pscustomobject]@{path='Tools/Fixture.dll';bytes=5;sha256=('0'*64)})}
        Reject {Expand-DDInstallBundle $zip (Join-Path $work 'Corrupt') $p}
        $p.files=@();Reject {Expand-DDInstallBundle $zip (Join-Path $work 'Extra') $p}
    }
}finally{
    $failed=@($checks|Where-Object status -ne 'PASS')
    $report=[ordered]@{status=$(if($failed.Count){'FAIL'}else{'PASS'});powershell=$PSVersionTable.PSVersion.ToString();checks=$checks.ToArray();scope='Synthetic transaction/validation; no real game or Steam changes';work=$work}
    [IO.File]::WriteAllText($testReportPath,($report|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))
}
