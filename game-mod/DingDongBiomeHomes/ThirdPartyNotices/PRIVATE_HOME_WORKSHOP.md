# Private Home Workshop assets

These files are included only in the owner's local Home build. They must not be
uploaded to Steam Workshop, copied into a public release, or redistributed
without explicit permission from the original author.

## Ziiio NPC Themed Houses World

- Author: Ziiio
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2441486368
- Source world SHA-256:
  `D1AB3865D130180BB1AFC5F5F4E6595B9958D3D985B9F4C4C838CC90328C1D72`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found on the
  Workshop page.

The source world says the NPC builds were created by Ziiio. Five named regions
were captured read-only and converted to the vanilla-data subset of the
Structure Helper 3.0.1 format. Empty exterior sky is encoded as transparent
NullBlock/NullWall cells, preventing rectangular clearing around the building.

| Asset | Source rectangle | Sanitized stateful tile types | SHA-256 |
| --- | --- | --- | --- |
| `steampunker-workshop.shstruct` | `3330,330,150,125` | `21,55,395` | `F6B6B44B59CCA2B350BC0CCE7FDCDC653FD784E8FF8C95EF24E1C85A8BEAF241` |
| `witch-doctor-hut.shstruct` | `6570,325,155,140` | `395,423,573` | `2549F42A873EB4065C60A0F59714C25BBAEF5DE365AEC4E274D6892424A9446F` |
| `barber-shop.shstruct` | `6260,415,115,95` | `21,55,395,423` | `EFAA6ED30FE573D5C58729F575F0D1412FF978ED0BDD9B79F61BC2AEB33AFDEC` |
| `tavern.shstruct` | `7420,325,165,145` | `21,55,88,395` | `EDE39022330847A440EF8FC744B3193C6DDD67ED5DC3ED7BC457D01E4F86602C` |
| `night-club.shstruct` | `8190,360,175,115` | `55,395,470,471` | `70960040E5D28837E0DD35F35B1ABD3BC0C6B099460B87579DD3BB7FFD4D4845` |

All chest, sign and vanilla TileEntity tiles were explicitly removed from the
captured geometry. The runtime will create DingDong-owned signs and gameplay
state only after a separate placement/housing acceptance gate.

### Second surface batch

| Asset | Source rectangle | SHA-256 | Intake role |
| --- | --- | --- | --- |
| `airship-house.shstruct` | `100,300,160,110` | `5844CF227E2B7B39FA830912A40251F6DBF7E51D5019FC6FBBFA77046E7D4C18` | floating residence candidate; unrelated beach/ocean wedge removed by recut |
| `cliff-tree-lodge.shstruct` | `2070,290,210,245` | `7CD122DDC42F0850F17DD5A29F561CB770670C70654CCC5BE9AEAB5FB34A8947` | residence scene candidate |
| `snow-lodge.shstruct` | `2410,340,205,135` | `142CF1B31EC9439AF57D1D547E19FA5CFD0E75E5ABCE7617DA858EBF53F099FD` | residence candidate |
| `vertical-treehouse.shstruct` | `2990,260,185,310` | `FEA2615DD1B6968EC8E10AA842274786D8AA8B1D6146FCBEF7694EFA9F520282` | residence scene candidate |
| `forest-lake-cottage.shstruct` | `3520,345,185,140` | `010C93D787C20CEFFBFB8F05B86931DDBF9EE38FBEB8036D679B4ECC6E0D614C` | residence candidate |
| `crimson-spire.shstruct` | `4000,265,300,290` | `DF965FEDABA32809D9C23F13DF747B523F5F0A08DAB46E23A42188AEAE771789` | large residence/civic candidate |
| `water-laboratory.shstruct` | `4700,345,250,175` | `53085AF71F5B18FDB89DCD92E6F0A38599C6ABF0D2E284CEDEEE67F2A806592C` | residence/workshop candidate |
| `desert-pavilion.shstruct` | `5030,395,190,140` | `2340A6ECB4593D860027BFAC059E1A861E440F623D155CD783A091B6258A5A22` | residence candidate |
| `jungle-watchtower.shstruct` | `6820,300,125,195` | `96A07FF328688BCC93AE0FF5D92EFC7E63336BFB20B0D31F3A62BAD14DE0CAF6` | residence/civic candidate |
| `jungle-portal-house.shstruct` | `6940,345,190,170` | `EAD3FE67AC85B264BAE4B77D20C167D3FE1FE7A01573C1428224E920A9DCD490` | residence/civic candidate |
| `industrial-foundry.shstruct` | `7270,345,150,175` | `6ABA39FBDA01E4E66F35FB26C801DDFF601019147B848EE4BAC501291BA1628F` | residence/workshop candidate |
| `east-house-purple.shstruct` | `7560,345,85,115` | `76CC6C04CBF2A4A80CDCA6AA58E1C4C0D216D6654D1EBB7CDEC037F855496CD7` | village infill, not a 61-home candidate |
| `east-house-tan.shstruct` | `7630,350,85,110` | `7E85E12BB03EEE15173C8B1F01D8C3DE7BC4534BDE83A625C1637BD73A18DAC9` | village infill, not a 61-home candidate |
| `east-workshop.shstruct` | `7700,360,90,105` | `761F9E471F325FD356C5A007C3C02F3708B2E819EB063AAE46355B4F724B58DA` | village infill, not a 61-home candidate |

The second batch passed 14/14 independent binary parses. Across both batches
the private catalog contains 19 distinct assets: 16 residence candidates and
3 village-infill components. No item is assigned to an NPC yet.

## The world of buildings

- Authors: KIRA and Zilot
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2441025960
- Source world SHA-256:
  `86C0A4BFCE01A8C39E9C4E9095FE5AFEE62B823DF4AABC2DC83D978D4669919E`
- Rights status: `PRIVATE_HOME_ONLY`; the page identifies the creators but
  contains no redistribution grant.

The source world was loaded read-only as version 236. All selected regions use
only IDs supported by the local Terraria 1.4.4.9 runtime. Chest, sign and
TileEntity-bearing tiles were removed explicitly; exterior sky uses the same
boundary-connected NullBlock/NullWall mask as the Ziiio intake.

| Asset | Source rectangle | Sanitized stateful tile types | SHA-256 |
| --- | --- | --- | --- |
| `west-galleon.shstruct` | `80,245,105,85` | `21` | `41EB0283BE5F48C1DDF94995DD6E832E625EADDB566FF4C2864A28E29003AE98` |
| `harbor-west-house.shstruct` | `175,260,55,70` | `21,55,88,520` | `5C9477D74C0DF537BB50F05B47985802077F87EDE895BA595CB17582BB5B7B50` |
| `harbor-center-house.shstruct` | `220,250,55,80` | `21,88,475,520,597` | `62E09C8352EA6875B3666745A35B1B23CF5B14DF826809AB8420234B5D7EFEDE` |
| `jungle-treehouse.shstruct` | `2510,205,130,130` | `85,88,475,520,597` | `4E66939603B824832E78804535358A7177D6958A948B98188A747DA3B1107290` |
| `desert-palace.shstruct` | `2715,240,115,90` | `88,470,597` | `D8835641668B2BDF0AA60504BC1A6328DE0DF8E93363E1CDFED49B7627B13BE8` |

All five assets passed an independent Structure Helper binary parser: header,
dimensions, `NBT=false`, mappings, five expected blocks, exact block lengths
and EOF were verified. Trost remains a single district candidate and is not
miscounted as several NPC homes.

## Greaveil

- Author: TUNA DASH
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=3669025349
- Source world SHA-256:
  `B5738D5DA7C8065B069F7EC7DB076D3712E5F9584594F582F1FD7AE10B3FB418`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

Greaveil is a version 318 world. The unmodified exporter correctly rejects it
because several regions use post-1.4.4.9 decorative IDs. The explicit
`--export-shstruct-1449` path has a small whitelist: decorative cobweb becomes
vanilla cobweb, Pine Wood Wall becomes Boreal Wood Wall, new unsafe stone wall
becomes Stone Wall, and unsupported furniture/cave decoration is removed.
Every changed cell is counted; any unlisted newer ID still fails closed.

| Asset | Source rectangle | 1.4.4.9 compatibility changes | SHA-256 |
| --- | --- | --- | --- |
| `west-galleon.shstruct` | `55,295,145,130` | `tile726->empty:9` | `FC0E0D038D1FE1E20D86C8B554C1C00E229F2E487683D38E39F0CF2CB8F90536` |
| `west-beach-cottage.shstruct` | `185,345,110,85` | `tile726->empty:3` | `385D6F7D76ED3ED9EF3CA2DEEF601AA1573D693B2C587446BDE52EC67B546D95` |
| `west-lighthouse.shstruct` | `285,305,75,125` | none | `DBB9C093F9D4B649FB76BC63F5DF401C2D752C86740F50E058D5D1CD3D252A42` |
| `desert-palace.shstruct` | `1300,340,160,110` | `tile699->empty:16; wall358->149:130` | `A057AA9CA2CEE2CA3618CC78E5904CCF12F94F9E89D92EC3A8AF281A18E194A5` |
| `snow-steampunk-manor.shstruct` | `2380,305,180,160` | `tile693->empty:3; tile694->empty:2; tile721->empty:6; tile733->empty:9` | `3D4AF7925629F140C64F3245B6939B200EA465A44B7F25336DBAF0080310D213` |
| `snow-lodge.shstruct` | `2670,390,160,125` | `tile720->empty:6` | `6D44D58C1EF412F01B3B5601AAE73664E6B05D6215F73D195B0CC7CDF04DABD6` |
| `hill-manor.shstruct` | `3320,400,165,115` | `wall349->1:16` | `62079813471A4C80BE3803BBD6061F796D4CC60F1902687CE75A4B1CBA645759` |
| `spawn-forest-street.shstruct` | `4170,400,170,100` | none | `A14C762E78CFA44628A66DD2B77225C29117A43BBF89D30F7EF77AF9D538EDB3` |
| `spawn-tree-tower.shstruct` | `4330,375,100,130` | `tile693->empty:32; tile694->empty:34` | `39A3994DEA31CDAE1150CE8281FCADCF6343E08E615D32CE1E7CC5401B24E47F` |
| `mushroom-spire.shstruct` | `5680,370,120,125` | none beyond stateful-tile sanitation | `335D7EA00B3319275431E277E54C9A85E1EB18F77BF19A9FDA75027FA6CB5356` |
| `jungle-west-house.shstruct` | `7210,385,110,125` | none | `CA07AB68A73649150637C44F56DAD90384436BC25280BDF2937238B98066FE0E` |
| `jungle-east-house.shstruct` | `7325,395,120,100` | none | `8F25A9325CE30BA869E532D5D7076A89D726A20E12B0BFC676A7F4E1F4F96876` |
| `underworld-castle.shstruct` | `780,2180,360,190` | `tile697->51:87; tile699->empty:16` | `B6D872344AABC1F9C9F66BF81F6D7293C727561A75E4CE19028337E4333A8186` |

All thirteen assets passed independent binary parsing `13/13 PASS`. The forest
street remains one authored ensemble; overlapping crops were deliberately not
counted as separate buildings.

## Aegis8 v1.13

- Author: TerraMonolith
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2789072123
- Source world SHA-256:
  `F14FE6860EB3811E74A4E7657EBDE3B3EAC819311F6614DEA85C16736FA6008C`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

The Workshop page identifies Aegis8 as TerraMonolith's construction world
containing their creations. Version 248 loaded read-only as `8400x2400`.
Sixteen crops were selected from six reviewed surface bands; neighbouring
buildings, unpainted tutorial copies and alternative presentations were not
split into extra catalog entries. All source IDs are within the local Terraria
1.4.4.9 limits, so no compatibility remap was used. The removed tile IDs below
are stateful furniture/chest/sign/TileEntity carriers, not visual palette
substitutions.

| Asset | Source rectangle | Sanitized stateful tile IDs | SHA-256 |
| --- | --- | --- | --- |
| `desert-oasis-settlement.shstruct` | `165,330,155,115` | `21,88,395,467,471,597` | `82DCBFBF6995ECD0530582E6F3C238F69700BBB3E47B96583BDE108F17F6A027` |
| `desert-gothic-manor.shstruct` | `325,330,60,105` | `21` | `29CBF294A8CA441FFD22CA46D7992A8AFC9910A1277D9EB77DC71AE51FB4B94B` |
| `mountain-lodge.shstruct` | `750,330,125,95` | `88,475` | `55269E55CF61A0BA65134655D784C7E19D9D46BFB51A5CB84D34746A1DECCB97` |
| `floating-pagoda.shstruct` | `1125,260,155,130` | none | `60D854DE603E02BBD5B163F8C0BE5FE2064724966ABBCBF287B4973568CD8084` |
| `forest-hill-manor.shstruct` | `1265,335,120,125` | `88,475` | `BD456BA0115F36A6506D15DCB40460092880F57BBC742C9771F38FD7A05C7594` |
| `cavern-lake-smithy.shstruct` | `400,480,175,200` | `21,55,88,395,471` | `ED5214980AF8052D94F7F5B877F448EE42CE24DE7F4FDA67E42323D0B4EFE3ED` |
| `cavern-lake-manor.shstruct` | `560,485,175,195` | `21,88,395,520` | `3162F3F5F346F0386BB1623B9F8176531BA2DCD55A7EDACEB3C98ABD571A53BF` |
| `snow-castle.shstruct` | `1410,260,215,390` | `21,55,88,423` | `C6AB1CCEF17232DCFDA6C8DAD17C6EC88074DBB0ECDC93AE6DFCECD030DC632E` |
| `snow-spire.shstruct` | `2195,290,120,360` | none | `303254B465EBA73B435575B0E6D55784329C9E1E507544111618B573CBFE48E9` |
| `hallow-cliff-manor.shstruct` | `3690,310,185,240` | `88` | `61D2455F8F8454523D0110238B31615C4A1BF42477264450171374C9063FFBA2` |
| `hallow-ridge-village.shstruct` | `3970,310,230,290` | `55,88,475,597` | `9B7E359E65EFCD1FCA6884BF4CFA31C4E4B28D5E4ADD977BE4DEABA1A6B06335` |
| `jungle-tree-city.shstruct` | `4170,300,270,350` | `21,55,395` | `0E63436C67850664217F8751B49A8D0340629E0D340D24F44EA478A687FD2541` |
| `desert-sky-palace.shstruct` | `4590,280,360,230` | `21,88,395,467,471,597` | `EC921473D8109A1712588F4328C710781ADC41FA299FE8E0CCC63DC1B6086C0D` |
| `steampunk-orb.shstruct` | `6550,380,260,240` | `21,395,520` | `46EF219EBD55EE150A07C6988E1A1F2E641CB684FF1300E0ECE7682DF9A77ED9` |
| `forest-treehouse.shstruct` | `7600,350,250,270` | `21,55,88,395,597` | `E46CA6772092D7DF66B71199D55CAC798FEB49F8C05F223CDE924A7F8B4674B1` |
| `forest-windmill.shstruct` | `7850,420,170,180` | `21` | `99DCC52828C5EF0AF1F0AF8F5C6CB5CA7B21209C722C337687293DEA6564CE65` |

All sixteen assets passed an independent Structure Helper binary parser
`16/16 PASS`; byte-identical copies in the mod source also matched `16/16` by
SHA-256. Ground anchors and housing capacities remain zero until their separate
placement and housing gates pass.

### Aegis8 showcase supplement

Five additional non-overlapping large scenes were exported from the same
private Aegis8 source. Three pass Terraria housing; two are POI-only.

| Asset | Source rectangle | SHA-256 | Housing result |
| --- | --- | --- | --- |
| `snow-harbor-settlement.shstruct` | `1780,390,240,190` | `BAD76F45CDC08A80DD6A8E2CF4DA83A7DD7FFBCEA48E9BF856BF131D085F8FEC` | PASS, 4 rooms |
| `moon-citadel.shstruct` | `2630,250,300,330` | `2DC033B9AFB74AAB0BA6B5155A5C2E507BDF6B132DE2B206AC3C8640DD07565C` | PASS, 4 rooms |
| `east-airship.shstruct` | `6810,250,190,190` | `07D4941B1D50DFA1A3071E0462082E275DDFC54925D7D0800F5188BDE8C0DCA3` | POI, 0 rooms |
| `cloud-city.shstruct` | `7000,250,550,300` | `756DC65922DDFCA219F0BD9DFFA09933E1DE670DA3C7B9C437C38A40DDCEB277` | PASS, 2 rooms |
| `ocean-galleon.shstruct` | `8150,350,245,230` | `07EE686E733B1324D4C5C9F50535E711DBEE5A00A53C7EF1A51C8DA024D21429` | POI, 0 rooms |

## Updated huadiao world

- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2883756316
- Source world SHA-256:
  `54BB112A3A9FDF6E8F948662B6A12AFB7352D30E4A4CF542BC315B16BE339D3E`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

| Asset | Source rectangle | SHA-256 | Housing result |
| --- | --- | --- | --- |
| `Huadiao_ElementalSkyHouse.shstruct` | `2040,260,190,160` | `D9099D4B8B3E17006F1E20661E64F44361157DC7B87A8B254D3B0D8EEFDE3BF1` | PASS, 2 rooms |
| `Huadiao_FireSkyVillage.shstruct` | `4120,195,170,165` | `E15716FC84DB7412ED95551589D9D8BE7859D3B29948961625DF7642F5099A2E` | PASS, 5 rooms |
| `Huadiao_CrescentVillage.shstruct` | `5050,240,220,220` | `322E00FF729C324A1EB3E0A1DE60556980A5FF16F7511FACEB71A148E7C46F8B` | PASS, 2 rooms |
| `Huadiao_JungleGothicManor.shstruct` | `6270,450,330,180` | `E21F6768F211A57F89E3FD465D6806F34219F5087840BC70069617307B5A6305` | PASS, 3 rooms |

## Barton 1.4.4.9

- Author: XORandom
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2865484106
- Source world SHA-256:
  `F0D09B348AB0AC8938ADF0D4D1F60CD464A1D3D6906B91394F2EEA6C9BB48251`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

`Barton_SkyTreehouse.shstruct` (`7790,100,280,200`, SHA-256
`21E12B2D29FE504F5520C2EF66FA13242582D8DC7462F38F713AA1F010A9B7B9`)
is a visually accepted standalone landmark but has zero valid Terraria rooms.
It remains POI-only and is not counted toward the 61 residences.

## HANNIBAL'S HOUSE

- Author: HANNIBAL
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=3358990936
- Source world SHA-256:
  `5B7F4B18182429FA69CC4155CF5A146771BD87FF1E276D026126474200CBC00E`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

`Hannibal_SnowResidence.shstruct` was captured from `4125,415,290,120`.
Asset SHA-256:
`EC4191560B2897AF30E61D9F0B790F868341F606FE51CF52761823BF37140692`.
Terraria dedicated-server housing found 3 valid rooms.

## Magoozy Palace

- Author: SallyMagoozy
- Source: https://steamcommunity.com/sharedfiles/filedetails/?id=2459200280
- Source world SHA-256:
  `53F688539A9AC7139AEF1878F5C462EE77B4438A79775B9841857641B91C750F`
- Rights status: `PRIVATE_HOME_ONLY`; no redistribution grant was found.

`Magoozy_Palace.shstruct` was captured from `4500,300,405,225`. The rectangular
underground maze was deliberately excluded from the residence asset and remains
POI-only. Asset SHA-256:
`9E6CAD8630C7F44220F45CC59A526762E24D17FE19CD6C2C7D09349509DCE2E1`.
Terraria dedicated-server housing found 8 valid rooms.

## Roadmap R7 independent Workshop residences

All entries below are `PRIVATE_HOME_ONLY`. Their Workshop pages identify the
uploaded build/world but provide no redistribution permission. Source worlds
were read headlessly; only the exact tight crop was exported. No compatibility
remap, palette variant, procedural geometry or settlement splitting was used.

| Asset | Author attribution | Workshop source | Source world SHA-256 | Source rectangle | Asset SHA-256 | Housing |
| --- | --- | --- | --- | --- | --- | --- |
| `R7_NadikiMansion.shstruct` | Nadiki | `2456777286` | `B7CCF635CA9F7C52340327AB5C13CEF1227FA6C263BBC381393CC9223976C651` | `1980,190,300,220` | `1E07B629FC2839177031F2314D230B90C85BBFCD90D609AA95E920D550DF92DC` | 6 rooms |
| `R7_SnowMansion.shstruct` | Steam creator `76561199008443415` | `2461816371` | `9C0BE7720DB17E00741B6713720F86803EE29956C95C470B61FC0DA2F64D1C93` | `2600,300,450,350` | `C44AF81FA8E6A1B6FFDB63D8B1A6D452B3F6EEAFE96948DAC54E1DD76036F995` | 8 rooms |
| `R7_PrincessCastle.shstruct` | Steam creator `76561198033511916` | `3129951489` | `FEF13461385019CE85F55320A505DD9B02B9A4FD18CE646894714AFD9F8FFF2F` | `5500,150,600,250` | `4154B54EB1B8FA7943B5708CEFA474B6A742366C3E6B0A43426741897FEC86FA` | 11 rooms |
| `R7_CastleCoolResidence.shstruct` | Steam creator `76561198267237675` | `2580275865` | `C80F5EECD763FF03B67B20778DFA647BFECFF2E05945B68CF56E1D431286160A` | `3900,250,700,450` | `1BF4EB868411425C0E5317E72563E9D0856A9B4297A12274A71F39B3C5DD4ACA` | 18 rooms |
| `R7_EsonoraHome.shstruct` | Steam creator `76561198977544804` | `2848278947` | `A12FA73C9C52B04A6880414C80C8EB720FA57F3762DDF045126F66381C1A0881` | `4050,350,500,300` | `D105F24EC0CBDDB596DACC9123A46549CA177A4149B75EEA0AF6A7E3183AD35F` | 10 rooms |
| `R7_ScarletDevilMansion.shstruct` | Steam creator `76561198081703326` | `2445047728` | `5A689C25DBF6EF97FCB1C7A298EDD3ECE06B36EDC14224E1F91C4F9A510E3086` | `2150,50,650,350` | `E34F27364B4522A60ED3C25E122CDEF51D604F294820FDA3D11D3DACE33FB016` | 19 rooms |
| `R7_UnrealDeltaCastle.shstruct` | Steam creator `76561198990976764` | `3004032818` | `3D6C08D781DE4FB10B3E7913751BF242E2CE0EDF0BDEAC63D45CDB15A91710A8` | `1900,150,600,250` | `006E5CBA3D68D4190081C301A2D335F1533D45BC17762EEAD6C628BC7AA4B667` | 13 rooms |
| `R7_MansionAlmost.shstruct` | Steam creator `76561198382584470` | `2493801540` | `FF0ADFF5EE33E7A80E51FD549F8189AE3E53B48B5BAAD64453AB0447CB8007BB` | `3950,250,600,300` | `ECC4E105710A0F8F53969BE9AFF61EF7614B533545E89A0BA7BE6782CA1620DC` | 9 rooms; reserve |

The `R7_MansionAlmost` row is retained as a server-valid reserve because its
Workshop description calls the mansion incomplete. It is not one of the seven
R7 entries used to close the 61-residence candidate mapping.
