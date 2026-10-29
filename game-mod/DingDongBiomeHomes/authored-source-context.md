# Restored source context, not resized houses

`Greaveil_JungleEastHouse` keeps the original house geometry at 1:1 tile scale.
Its source scene now includes the previously cropped tree and complete left
capsule; the neighbouring WestHouse rectangle is not imported.

| Contract | Value |
|---|---|
| Source | TUNADASH, Greaveil, Steam Workshop `3669025349` |
| Pinned source world SHA256 | `B5738D5DA7C8065B069F7EC7DB076D3712E5F9584594F582F1FD7AE10B3FB418` |
| Old source crop | `(7325,395,120,100)` |
| Old core asset SHA256 | `8F25A9325CE30BA869E532D5D7076A89D726A20E12B0BFC676A7F4E1F4F96876` |
| Restored source canvas | `(7289,373,156,122)` |
| Restored asset SHA256 | `AEC823E3C58603CF070A9528D19DC4D3B9C4F9A01247ED62DB58DF4A32FB69BD` |
| Original home bounds inside canvas | `(36,22,120,100)` |
| Original terrain anchor | `73`; translated anchor `95` |
| Primary source centre | old `60`; translated `96`, not canvas centre `78` |
| Original visual-contact bounds | `(0,0,117,100)`; translated `(36,22,117,100)` |
| Translated quarter contacts | `65`, `123` |

The private offline gate preserved all 12000 old core cells byte-for-byte in all
five StructureHelper planes. The supplement restores 1455 original source cells
and uses explicit NullBlock/NullWall outside the retained scene. All 612 original
leaf blocks and 24 complete framed-object components are retained, including the
capsule's bed, bookcases, furniture, walls and supports. Source state is not
replaced with default brightness, wire, paint, actuation or liquid values.

The one original dresser (`TileID.Dressers`, type88, 3x2) is at canvas `(30,70)`.
Its source container has an empty name and 40 empty item slots. Its six authored
tiles must not be removed. The five-plane schematic contains no arbitrary NBT;
the runtime must explicitly register and verify this native empty container.

`AuthoredSourceContext.ReferenceBounds`, `ReferenceCenterOffsetX` and
`ReferenceContactBounds` affect source-reference sampling only. Indexed biome
ranges are re-expressed for the translated primary centre; edge ranges still
project from the original index centre so the offset is not applied twice.
Other presets retain their existing sampling behaviour. Whole-world fallback
ranges retain their full domain.

Collision, full-scene protection, visual footprints, relief checks, liquid
admission, mass/density accounting and framing continue to cover the complete
restored scene. The reference rectangle must never be used to crop those gates.
Primary and restored-capsule housing, native framing and container save/reload
are separate runtime acceptance checks; the offline source gate alone is not
full worldgen acceptance.

Private preparation evidence:
`D:\SteamLibrary\CodexWork\dingdong-x10-world\adaptation-r325\source-retrieval\tree-capsule-candidate-v3\verification.json`.
See its compact source/candidate image and required-container sidecar there.
