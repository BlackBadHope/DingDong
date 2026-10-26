param([Parameter(Mandatory)][string]$SignedPowerShell,[Parameter(Mandatory)][string]$TestRoot,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Management\Microsoft.PowerShell.Management.psd1') -ErrorAction Stop
Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop
if(Test-Path -LiteralPath $TestRoot){throw 'Preserve earlier signature fixture evidence'}
[void][IO.Directory]::CreateDirectory($TestRoot)
. (Join-Path $PSScriptRoot 'Ensure-OfficialDependenciesForLaunch.ps1')
$savedUser=$env:USERPROFILE;$savedPrograms=$env:ProgramFiles;$savedPath=$env:PATH;$savedModules=$env:PSModulePath
$checks=@();$control=$null
try {
 $modules=Join-Path (Split-Path -Parent $SignedPowerShell) 'Modules'
 Remove-Module Microsoft.PowerShell.Security -Force -ErrorAction SilentlyContinue
 $env:PSModulePath=$modules
 if($PSVersionTable.PSEdition -eq 'Desktop'){
  $failed=$false
  try {$null=Get-AuthenticodeSignature -LiteralPath $SignedPowerShell}catch{$failed=$_.Exception.Message -like '*module could not be loaded*'}
  if(!$failed){throw 'Desktop/Core module-path failure control was not reproduced'}
  $control='REPRODUCED';$checks+='original-desktop-core-module-autoload-failure-reproduced'
 }else{$control='NOT_APPLICABLE_CORE_HOST'}
 $env:USERPROFILE=Join-Path $TestRoot 'User';$env:ProgramFiles=Join-Path $TestRoot 'Programs';$env:PATH=$TestRoot
 $candidate=Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe'
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $candidate))
 Copy-Item -LiteralPath $SignedPowerShell -Destination $candidate
 $resolved=Resolve-DDPreferredPowerShell
 if($resolved -cne [IO.Path]::GetFullPath($candidate)){throw 'Signed Microsoft candidate was not resolved with mixed module paths'}
 $checks+='trusted-host-security-module-verifies-microsoft-signature'
 [IO.File]::WriteAllText($candidate,'unsigned test fixture - never executed')
 if($null -ne (Resolve-DDPreferredPowerShell)){throw 'Unsigned executable was accepted'}
 $checks+='unsigned-candidate-rejected'
}finally{
 $env:USERPROFILE=$savedUser;$env:ProgramFiles=$savedPrograms;$env:PATH=$savedPath;$env:PSModulePath=$savedModules
}
$checks+='process-environment-restored'
$result=[ordered]@{status='PASS';edition=$PSVersionTable.PSEdition;version=$PSVersionTable.PSVersion.ToString();originalFailureControl=$control;checks=$checks;gameStarted=$false;userSavesTouched=$false;helperSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Ensure-OfficialDependenciesForLaunch.ps1') -Algorithm SHA256).Hash.ToLowerInvariant()}
[IO.File]::WriteAllText($Output,($result|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
$result|ConvertTo-Json -Depth 6
