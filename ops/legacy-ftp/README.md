# Legacy Steam/FTP launcher

This is the owned preflight helper used by the Steam/FTP installation. The
0.29.7 launcher hotfix retains the R36 game payload and its native mod version.

Windows PowerShell 5.1 can inherit a module search path containing PowerShell 7
modules. Its signature command may then fail to auto-load. The helper explicitly
imports the Security module from the active host's `PSHOME` and invokes the
module-qualified signature command. Candidate executables still need a valid
Microsoft signature.

`Test-LaunchHostSignature.ps1` reproduces the Desktop/Core module-path failure,
checks a signed Microsoft executable, rejects an unsigned candidate and restores
the child process environment. It never executes the candidate or opens a game.
The installed helper was also exercised through the real Windows PowerShell 5.1
preflight, with the already current mod, respawn policy and translation.

The public GitHub installer has its own bootstrap route. Its R36 release remains
version 0.29.6; this directory records the separate legacy launcher source.
