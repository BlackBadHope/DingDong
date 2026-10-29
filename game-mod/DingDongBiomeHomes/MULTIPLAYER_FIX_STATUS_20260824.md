# DingDong multiplayer fix status — 2026-08-24

## Scope and safety boundary

- Source root: `C:\Users\ish50\Documents\my games\Terraria\tModLoader\ModSources\DingDongBiomeHomes`
- Live mod was not replaced: `C:\Users\ish50\Documents\my games\Terraria\tModLoader\Mods\DingDongBiomeHomes.tmod`
- Live SHA-256 after verification: `EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`
- Live enabled set SHA-256 after verification: `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1` (67 entries)
- No graphical client was launched. No user world or player file was read, copied, or modified.
- Runtime validation used a previously disposable copied test world under the task temp directory and a dedicated server on port 7788.

## Confirmed startup failures

The 2026-08-24 multiplayer server log proved that the remote player reached the server three times and received configuration data. Two attempts then failed inside `DingDongBiomeHomes.HandlePacket` with:

`System.IO.IOException: Read underflow 1 of 34 bytes caused by DingDongBiomeHomes in HandlePacket`

The quest identity packet is exactly 34 bytes in this case: one packet-kind byte, one string-length byte, and a 32-character GUID. The old handler consumed only the kind before checking whether the player slot was active. tModLoader detected the 33 unread payload bytes as an underflow. Because the hello had no retry/ack path, the client remained indefinitely at `Quest identity is still syncing with the server`.

The same log also recorded a flood of `DINGDONG_DAMAGE_REJECT` with `MissingCommit` and `MissingApproval`, reaching count 1000. The validator could see the vanilla damage packet before the custom commit/approval sequence, and it also treated legitimate damage without a DingDong transaction as invalid.

The remote timeout is not evidence of a closed port: the server was listening on 7777, the private inbound .NET Host rule was enabled, and the remote player reached the configuration phase. The screenshot showing `Adding Recipes...` also indicates that a 67-mod client reload can outlast the join timeout; both computers must preload the identical enabled set to the main menu before joining.

## Implemented source fixes

- `NpcQuestPlayer` no longer sends identity prematurely from `SyncPlayer`.
- The local multiplayer client sends identity on entering the world, retries every 120 ticks until acknowledged, and marks it confirmed after a quest snapshot arrives.
- Quest interaction, flag claim, and identity handlers now consume and validate the complete payload before testing the player slot.
- The quest network contract now round-trips the identity payload and verifies its exact length.
- Damage authority is fail-open for `Missing`, `Expired`, and `MissingCommit`: the vanilla packet is processed, while a receipt resets the optional ledger.
- `IdentityMismatch` and `PayloadMismatch` remain fail-closed.
- `StrictMultiplayerDamageValidation` now defaults to `false`.
- The damage protocol probe asserts both fallback and hard-reject cases.

## Separate candidate artifact

- Folder: `C:\Users\ish50\Desktop\DingDong-MULTIPLAYER-FIX-TEST-20260824-2315`
- File: `DingDongBiomeHomes.tmod`
- Display name: `DingDong: Biome Homes [MULTIPLAYER FIX TEST]`
- Version: `0.20.2`
- Size: 2,132,140 bytes
- SHA-256: `BED893309004E5F0257424AE63E9F4759B54AA97C4ED653A5F6F73C688D7B806`

## Evidence

- Headless build: PASS, exit 0, 9.578 s, one command call, 0 cached, `0 errors and 0 warnings`.
- Build log: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\multiplayer-fix-20260824-2315\logs\run-dingdong-multiplayer-fix-headless-build-v1-20260824T200749Z-217c68e8d13e-d65422a8.log`
- Build-log SHA-256: `601FB9C11E318986CDBADFD1DFC92EE856F2B7D802911A8439F1BC8FD871E8AA`
- Full-profile dedicated server log: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\multiplayer-fix-20260824-2315\logs\full-profile-server.log`
- Full-profile log SHA-256: `D7976292747C1F21C082A4DB43401C9E8D6F0AE04D640BC4B5ECAD1DF383569D`
- `DINGDONG_DAMAGE_PROTOCOL_AUTOPROBE|verdict=PASS`; disposable world unchanged by the probe.
- `DINGDONG_GAMEPLAY_CONTRACT|verdict=PASS`; residents 61/61, quest rotation `GUIDED_PASS`, quest protocol 2, localization PASS, failures none.
- No `Read underflow`, `DingDongBiomeHomes in HandlePacket`, or `DINGDONG_DAMAGE_REJECT` was emitted by the fixed full-profile startup.

Remaining startup warnings are non-fatal and originate outside the DingDong source tree: tModLoader clone-safety warnings for Thorium/SOTS/ImproveGame/WeaponEnchantments and several other mods; obsolete Calamity `SetDamageReductionSpecific` calls; and two missing FishermanNPC cross-mod fish types. The full server still reached `Server started`, emitted no ERROR line, and passed both DingDong gates. No `SetDamageReductionSpecific`, `FishofLight`, or `FishofNight` reference exists in the DingDong source/build copy.

## Remaining external acceptance gate

The headless server validates compilation, full mod loading, packet contracts, quest catalog, and the damage protocol. A real brother-to-host join and interactive quest acceptance still require both users to install the same candidate and perform one normal client session. This was deliberately not automated because launching the graphical client and touching user saves require separate permission. If the join still fails, collect the brother's `client.log` immediately after one attempt; it is not present on this host.

## Follow-up TEST2 — escort/WorldData blocker

The local `client.log` contained an additional repeated connection blocker that was not visible in the server-only audit:

- `WorldData(7): System.IO.InvalidDataException: Invalid escort stage.`
- `DINGDONG_GAMEPLAY_PACKET_REJECT|sender=256|kind=67|reason=InvalidDataException`
- `Read underflow 12 of 294/356/369 bytes caused by DingDongBiomeHomes in HandlePacket`
- repeated `Receiving tile data`, showing that world synchronization restarted instead of completing.

Root cause: `WriteState` serialized `NpcKey` before `Stage`, while `ReadState` read `Stage` before `NpcKey`. The first byte of the length-prefixed NPC key was therefore interpreted as an escort enum value.

Follow-up changes:

- `ReadState` now consumes fields in exactly the order emitted by `WriteState`.
- All state fields are read before semantic validation, string lengths and owner GUIDs are bounded, and negative revisions normalize to zero.
- A byte-exact synthetic escort-state round trip is now part of `DINGDONG_GAMEPLAY_CONTRACT` as `escortProtocol=PASS`.
- Rejected custom gameplay/damage packets consume their remaining bounded packet stream before returning, preventing a secondary tModLoader underflow report.

TEST2 candidate:

- Folder: `C:\Users\ish50\Desktop\DingDong-MULTIPLAYER-FIX-TEST2-20260824-2323`
- Version/display: `0.20.3`, `DingDong: Biome Homes [MULTIPLAYER FIX TEST 2]`
- `.tmod` SHA-256: `C86FC719AA14E77003EF001F69E526B4642CBFD7975810B8D4CDE8C8CF459B91`
- Headless build: PASS, 0 errors, 0 warnings, one executed call, 2.844 s.
- Build log SHA-256: `67C18A2A49A1A15191C9EF3FD80C3156D1B5518C930911D70A95ACAFB4CB661A`
- Full-profile server log: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\escort-fix-20260824-2323\logs\full-profile-server.log`
- Full-profile log SHA-256: `DD89B872FDB6C1D2F65E9E6CC27540723E72CB2E48933E832503ABF80E794EA5`
- Full 67-entry enabled set loaded; damage autoprobe PASS; gameplay contract PASS with `escortProtocol=PASS`; zero ERROR, `Invalid escort stage`, `Read underflow`, gameplay packet reject, or damage reject lines.
- Live `.tmod` remained byte-identical at `EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`.

## TEST3 — live combat rollback + Russian journal

The next live session proved that main/live was still the old `0.20.0` build.
After the player joined, `server.log` emitted `DINGDONG_DAMAGE_REJECT` with
`MissingCommit`/`MissingApproval` up to at least `rejectedTotal=400`. This is
the direct cause of the apparent instant healing, rubber-banding and immortal
NPCs: the client predicts a hit, the old server rejects it, then authoritative
NPC health and position are restored. `NothingImmune v1.2` was also loaded,
but it is not the source of those reject receipts.

TEST3 candidate:

- Version/display: `0.20.4`, `DingDong: Biome Homes [MULTIPLAYER COMBAT UI TEST 3]`.
- `.tmod` SHA-256: `3B1DBD6BBAF03F94DDD1054E562C6E33CB9D04406DDB020ACC8EC96BCAFFCDAC`.
- Enabled set: 66 entries; SHA-256 `9D98DC3F92B7D22CE4703A8BFD3F4FB478F2D2D8F56181D73F0A5ACBDED38814`.
- `NothingImmune` is absent from both `enabled.json` and the full package payload.
- Patch ZIP: `C:\Users\ish50\Desktop\DingDong-MULTIPLAYER-COMBAT-UI-TEST3-20260825.zip`.
- Patch ZIP SHA-256: `EEAFA3AA1C565D87F8C57D06D652FC877E6446D9DAE05DECAA9F309BDAA89AD1`.
- Full manual ZIP: `C:\Users\ish50\Desktop\DingDong-Modpack-0.2.1-RU-Manual-TEST3-20260825.zip`.
- Full ZIP SHA-256: `15F656FA18056F84D0F9D4DA4624572A9F028CE4AA77C79E2B5F6DF97123C746`.

Evidence:

- Static combat/UI/package preflight: PASS.
- Headless build: PASS, one executed call, 8.906 s, 0 errors, 0 warnings.
- Build log: `C:\CodexEvidence\DingDongTEST3\logs\run-dingdong-test3-build-20260825-20260824T210036Z-9b86823e5257-62f7b8b8.log`.
- Build-log SHA-256: `EEEA35A51AF301494E12608A795600B01AB4456E63FFBCDF74B21EB7E9CB0802`.
- Full package: 66/66 enabled/tmod, 86 checksum entries PASS, zero unlisted files,
  zero forbidden scripts/installers, zero Worlds/Players paths, and no
  `NothingImmune` archive entry.
- Live `.tmod` was re-hashed after the isolated build and remains byte-identical
  at `EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`.

Real multiplayer join, quest interaction, NPC death and graphical layout are
still owner-run acceptance gates. They were not automated while the live server
was in use and because the graphical client/saves require separate permission.
