# Compatible GitHub updater v5

This updater downloads only public assets from `BlackBadHope/DingDong` GitHub
Releases. It never uses credentials or a fallback server. `Update-DingDong.cmd`
uses installed PowerShell 7 in Program Files, then built-in Windows PowerShell.
Errors remain visible. No installed updater is replaced by these source files.

Run the CMD entry point, or use:

```powershell
.\DingDong-Update.ps1 -DownloadOnly -Owner BlackBadHope -Repository DingDong
```

The current public installer uses signed `install.json` and installs the complete
public preset into a separate directory. This v5 updater remains compatible:
its signed `latest.json` installs only the tooling migration into `Bootstrap/Tools`.
Then run `Bootstrap/Tools/Install-DingDong.cmd` for the full public installation.
It does not alter the old FTP installation or import its saves automatically.
The old `v0.1.0-bootstrap` pending feed is historical and remains supported by the
parser; a pending feed never installs a payload.

## Publisher contract

`GET https://api.github.com/repos/{owner}/{repo}/releases/latest` selects one
published, non-draft, non-prerelease release. Tags use letters, digits, dots,
underscores and hyphens. Its unique uploaded `latest.json` asset is an envelope:

```json
{"payloadBase64":"UTF-8 JSON bytes in base64","signatureBase64":"RSA/SHA256 signature in base64"}
```

The pinned public key is inherited from the previously verified updater.
Signing keys must remain outside this repository. A pending payload needs only:

```json
{
  "format": "dingdong-update-github-v1",
  "minUpdater": 5,
  "sequence": 23,
  "status": "pending",
  "expiresUtc": "2026-11-20T00:00:00Z",
  "source": {"owner":"BlackBadHope","repository":"DingDong","tag":"v0.1.0-bootstrap"},
  "blockedReason": "No public game release: content redistribution review is incomplete."
}
```

A `released` feed additionally requires semantic `version`, `distribution` with
`policy: public-allowlist-v1`, boolean `reviewed: true`,
`manifestFile: public-allowlist.json` and `manifestSha256` of
the reviewed public allowlist; `payload` with ZIP `file`, SHA256 and byte count;
and a nonempty `files` array. Every entry requires `area`, `path`, `mode`,
`sha256`, `bytes`, `license` (reviewed license identifier), `source` (HTTPS URL),
and `provenanceId` (entry identifier in that reviewed allowlist). The publisher
must generate that list from the reviewed manifest. Signing a boolean flag is
not a substitute for the rights review.

The updater downloads the named allowlist asset from the same selected release,
checks its bytes against the signed SHA256, then requires
`format: dingdong-public-allowlist-v1` and a `files` array. That array must contain
exactly the same unique entries as the signed feed, with identical `area`,
`path`, `mode`, `sha256`, `bytes`, `license`, `source` and `provenanceId` values.
Every allowlist entry must additionally have `decision: approved`. Missing,
extra, duplicate, unapproved or mismatching entries stop the update. Pending
feeds need no allowlist asset.

Public feeds unconditionally reject `Game` files and third-party compiled mods.
There is one narrow compiled-mod exception: exact `area: Profile` and
`path: Mods/DingDongBiomeHomes.tmod`, with `rights: project-owned`,
`license: LicenseRef-DingDong-Personal-Use-1.0`, `decision: approved`, and
`provenanceId: dingdong-owned-v1:sha256:<64 lowercase hex>`. Its `source` must be
exactly `https://github.com/BlackBadHope/DingDong/tree/<40 lowercase hex>/game-mod/DingDongBiomeHomes`.
The signed feed and actual reviewed allowlist must agree on all metadata,
including rights, decision, provenance and the exact artifact SHA256/byte count.
No alternate mod name, path, upstream runtime or third-party `.tmod` is allowed
by this exception. Existing legacy full-pack feeds remain rejected by format.

The provenance identifier is a **signed operator attestation**, not automatic
proof of copyright or content ownership. Before signing a released feed, the
publisher must inspect the actual public provenance artifact, resource audit,
build inputs and output digest. String validation does not inspect the `.tmod`
for embedded third-party content or prove redistribution rights.

Permitted public payloads contain reviewed JSON profile settings and/or
`Bootstrap/Source/...` source/assets and `Bootstrap/Tools/...` scripts. Bootstrap
files go under this updater's `Bootstrap` folder and are **never executed**.
Use official Steam/tModLoader/Workshop routes for dependencies and an explicit
local build for content that cannot be redistributed. This updater does not
automatically subscribe, download Workshop content, build or launch the game.

Every payload must be an uploaded asset of the same selected repository and
tag; there are no manifest-provided arbitrary download URLs. Only HTTPS and
bounded redirects to GitHub release-asset storage are allowed. API errors,
missing assets, invalid/expired signatures, missing rights metadata, hash/size
mismatches and unexpected ZIP members stop before installation.

The transaction helpers retain support for separately prepared local
`Game`/`Profile` files. The public feed reader cannot authorize those banned
binary files. Transactions preserve saves, extra mods and existing ModConfigs,
use same-directory atomic replacements, keep verified backups, recover
unfinished transactions, and stop rollback if files have subsequently changed.
Automatic recovery starts only after the signed released feed, actual approved
allowlist and payload are verified. Pending or invalid feeds never resolve
targets or restore an unfinished transaction. Explicit `-RollbackBackup` remains
available independently as the user's requested local recovery operation.
`-RollbackBackup` accepts only this updater's `Backups` folder. Existing explicit
installation/profile markers and configured paths remain supported; Steam
discovery inspects only the configured Steam library list and app manifest.

## Offline checks

```powershell
powershell.exe -NoProfile -File ..\tests\updater\Test-GitHubUpdater.ps1
```

The harness never calls the network or launches the game. It verifies a small
historical synthetic fixture using the real pinned **public** key, then uses
an ephemeral, unsaved test key for fake GitHub releases and negative fixtures.
Logs and temporary files remain in `tests/updater` and are ignored by Git.

API contract references: [latest release](https://docs.github.com/en/rest/releases/releases#get-the-latest-release),
[release assets](https://docs.github.com/en/rest/releases/assets#get-a-release-asset).
