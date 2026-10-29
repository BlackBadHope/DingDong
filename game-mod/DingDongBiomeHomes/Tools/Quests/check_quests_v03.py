"""Offline quality gates for the Ding Dong v03 quest/lore candidate (First Word Was).

Usage:
    python -I Tools/Quests/check_quests_v03.py <modDir> [--scenarios <dir>] [--json] [--strict]

Reads only (never writes):
    <modDir>/Quests/ResidentStories.json          61 residents x 5 steps (engine file)
    <modDir>/Quests/Lore/<node>.json, codex.json  lore texts (ru/en)
    <modDir>/Localization/**/*.hjson              ru-RU / en-US pairs
    <scenarios>/*-node.json                       optional; default
                                                  <modDir>/../../../Narrative/FirstWordWas/scenarios

Gates (category -> severity):
    dup-title, dup-text, near-dup-text           duplicate / near-duplicate player text
    repeat-mechanic                              same Kind+target twice in one resident
    lore-mismatch                                Kind / title / choice ids differ between
                                                 ResidentStories.json and Lore stage entries
    lore-target-mention (warn)                   stage text does not name its target
    parity / empty / placeholder                 ru/en parity in hjson, lore json, stories
    trial-debuff, trial-availability, early-trial, creature-availability
                                                 vanilla enemy -> debuff -> progression table
    jargon                                       RAM/commit/build jargon in visible strings
    title                                        "First Word Was" visible in en and ru text
    generator-drift (warn)                       engine file differs from scenario source
    reward-progression (warn)                    coins not rising / reward item repeated

Exit code: 0 = no errors (warnings allowed unless --strict), 1 = errors.
Vanilla facts in ENEMIES carry a confidence; "medium" verdicts are reported as warnings
marked "uncertain" and must be confirmed in game or on the wiki before acting on them.
"""
import difflib
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

# ---------------------------------------------------------------- vanilla knowledge
# availability: prehardmode | underworld (pre-hardmode, Underworld only) | hardmode
# inflicts: debuffs the enemy can apply to the player; conf: high | medium
ENEMIES = {
    "Zombie": ("prehardmode", "surface night", set(), "high"),
    "DemonEye": ("prehardmode", "surface night", set(), "high"),
    "Skeleton": ("prehardmode", "caverns", set(), "high"),
    "CaveBat": ("prehardmode", "caverns", set(), "high"),
    "JungleBat": ("prehardmode", "underground jungle", set(), "high"),
    "Harpy": ("prehardmode", "space", set(), "high"),
    "Shark": ("prehardmode", "ocean", set(), "high"),
    "Hornet": ("prehardmode", "underground jungle", {"Poisoned"}, "high"),
    "Piranha": ("prehardmode", "jungle water", {"Bleeding"}, "medium"),
    "Antlion": ("prehardmode", "desert", set(), "high"),
    "BlueSlime": ("prehardmode", "surface", set(), "high"),
    "IceSlime": ("prehardmode", "surface snow", {"Chilled"}, "medium"),
    "SpikedIceSlime": ("prehardmode", "snow / ice caverns", {"Chilled"}, "medium"),
    "IceBat": ("prehardmode", "ice caverns", {"Chilled"}, "medium"),
    "SpikedJungleSlime": ("prehardmode", "underground jungle", {"Poisoned"}, "high"),
    "BlueJellyfish": ("prehardmode", "water", {"Electrified"}, "medium"),
    "PinkJellyfish": ("prehardmode", "ocean", {"Electrified"}, "medium"),
    "GreenJellyfish": ("hardmode", "cavern water", {"Electrified"}, "medium"),
    "Squid": ("prehardmode", "ocean", set(), "high"),
    "GoblinScout": ("prehardmode", "surface outer thirds", set(), "high"),
    "FireImp": ("underworld", "Underworld", {"OnFire"}, "high"),
    "Demon": ("underworld", "Underworld", set(), "high"),
    "VoodooDemon": ("underworld", "Underworld", set(), "high"),
    "Hellbat": ("underworld", "Underworld", {"OnFire"}, "high"),
    "LavaSlime": ("underworld", "Underworld", {"OnFire"}, "high"),
    "BoneSerpentHead": ("underworld", "Underworld", {"OnFire"}, "medium"),
    "WanderingEye": ("hardmode", "surface night", set(), "high"),
    "Wraith": ("hardmode", "surface night", set(), "medium"),
    "Mummy": ("hardmode", "desert", {"Slow"}, "medium"),
    "DarkMummy": ("hardmode", "corrupt/crimson desert", {"Darkness"}, "medium"),
    "LightMummy": ("hardmode", "hallowed desert", {"Confused"}, "medium"),
    "AnglerFish": ("hardmode", "cavern water", {"Bleeding"}, "medium"),
    "Pixie": ("hardmode", "hallow", {"Slow"}, "medium"),
    "ChaosElemental": ("hardmode", "underground hallow", {"Confused"}, "medium"),
    "IceElemental": ("hardmode", "ice caverns", {"Chilled", "Frozen"}, "medium"),
    "IcyMerman": ("hardmode", "ice caverns", {"Chilled", "Frozen"}, "medium"),
}
# Town NPCs and critters are valid Observe targets with no combat profile.
PASSIVE = {"Guide", "Angler", "Bunny", "Goldfish", "Penguin", "Squirrel", "Bird", "Duck", "Frog"}
# Residents whose arrival itself requires hardmode (ResidentEncounterSystem: savedWizard,
# Tortured Soul purification). Every other resident can start its chain pre-hardmode.
RESIDENT_GATE = {"Terraria/Wizard": "hardmode", "Terraria/TaxCollector": "hardmode"}
EARLY_TRIAL_MAX_INDEX = 1  # owner decision: early Trial stages live in the Underworld / hardmode
NEAR_WARN, NEAR_ERROR = 0.55, 0.75  # difflib word-sequence similarity thresholds

DEBUFF_WORDS = {
    "Chilled": ("chill", "cold", "frost", "freez", "ice", "icy"),
    "Frozen": ("froz", "freez", "ice"),
    "Poisoned": ("poison", "venom", "toxi"),
    "OnFire": ("fire", "burn", "flame", "ablaze", "blaz", "scorch"),
    "Slow": ("slow", "sluggish"),
    "Confused": ("confus", "dizz", "daze"),
    "Bleeding": ("bleed", "blood"),
    "Darkness": ("dark", "blind"),
    "Electrified": ("electr", "shock", "spark", "current", "circuit"),
}
BIOME_WORDS = {
    "Forest": ("forest", "wood", "tree", "grove"),
    "Desert": ("desert", "sand", "dune"),
    "Jungle": ("jungle",),
    "Ocean": ("ocean", "sea", "shore", "beach", "tide", "coast", "wave"),
    "Snow": ("snow", "ice", "frost", "tundra", "cold", "winter"),
}
TILE_WORDS = {
    "Torch": ("torch", "light", "flame", "fire"),
    "TreeSapling": ("sapling", "tree", "seed", "plant"),
    "Planter": ("planter", "box", "herb", "plant", "garden"),
}
JARGON = [
    (r"\bRAM\b", "RAM"), (r"\bОЗУ\b", "ОЗУ"), (r"\b(GiB|GB|MB|MiB)\b", "memory units"),
    (r"\b(ГиБ|ГБ|МБ|МиБ)\b", "memory units"), (r"\bcommit(s|ted|ting)?\b", "commit"),
    (r"коммит", "коммит"), (r"(?i)\bworking set\b", "working set"),
    (r"(?i)page ?file|файл подкачки", "pagefile"), (r"(?i)\bout of memory\b|\bOOM\b", "OOM"),
    (r"\bheap\b", "heap"), (r"(?i)\bgit\b", "git"), (r"(?i)\bseq ?\d+", "release seq"),
    (r"\b(TODO|FIXME|TBD)\b", "todo marker"), (r"(?i)lorem ipsum", "placeholder"),
    (r"оперативн\w* памят", "оперативная память"),
]
JARGON_RE = [(re.compile(p), n) for p, n in JARGON]
# Hardware/world-size UI where memory units are the functional subject: warn, not error.
JARGON_SOFT_KEY = re.compile(r"(?i)config|preset|ram|memory|worldstreaming|worldsize|header|tooltip")
TITLE_EN = "First Word Was"
# No Russian title is established in the narrative docs (they use the Latin name even in
# Russian text), so ru accepts the Latin brand or the literal translation as a title.
TITLE_RU_RE = re.compile(r"First Word Was|[«\"“]Первое слово было[»\"”]|^Первое слово было$", re.M)
TOKEN = re.compile(r"\{[^{}]*\}")

findings = []


def add(sev, cat, where, msg):
    findings.append({"severity": sev, "category": cat, "where": where, "message": msg})


# ---------------------------------------------------------------- minimal hjson reader
class Hjson:
    def __init__(self, text):
        self.s = text.lstrip("﻿")
        self.i = 0

    def err(self, m):
        line = self.s.count("\n", 0, self.i) + 1
        raise ValueError(f"line {line}: {m}")

    def ws(self):
        s = self.s
        while self.i < len(s):
            c = s[self.i]
            if c in " \t\r\n,":
                self.i += 1
            elif c == "#" or s.startswith("//", self.i):
                j = s.find("\n", self.i)
                self.i = len(s) if j < 0 else j
            elif s.startswith("/*", self.i):
                j = s.find("*/", self.i)
                self.i = len(s) if j < 0 else j + 2
            else:
                break

    def parse(self):
        self.ws()
        if self.i < len(self.s) and self.s[self.i] == "{":
            self.i += 1
            v = self.members("}")
        else:
            v = self.members(None)
        return v

    def members(self, close):
        out = {}
        while True:
            self.ws()
            if self.i >= len(self.s):
                if close:
                    self.err("unterminated object")
                return out
            if close and self.s[self.i] == close:
                self.i += 1
                return out
            if self.s[self.i] == '"':
                key = self.quoted('"')
            else:
                j = self.i
                while self.i < len(self.s) and self.s[self.i] not in ":\n{}":
                    self.i += 1
                key = self.s[j:self.i].strip()
            self.ws_inline()
            if self.i >= len(self.s) or self.s[self.i] != ":":
                self.err(f"expected ':' after key {key!r}")
            self.i += 1
            out[key] = self.value()

    def ws_inline(self):
        while self.i < len(self.s) and self.s[self.i] in " \t":
            self.i += 1

    def value(self):
        s = self.s
        # whitespace (including newlines) may separate ':' from the value
        while self.i < len(s) and s[self.i] in " \t\r\n":
            self.i += 1
        if s.startswith("'''", self.i):
            return self.multiline()
        c = s[self.i] if self.i < len(s) else ""
        if c == "{":
            self.i += 1
            return self.members("}")
        if c == "[":
            self.i += 1
            arr = []
            while True:
                self.ws()
                if self.i < len(s) and s[self.i] == "]":
                    self.i += 1
                    return arr
                arr.append(self.value())
        if c in "\"'":
            return self.quoted(c)
        j = s.find("\n", self.i)
        j = len(s) if j < 0 else j
        v = s[self.i:j].rstrip()
        self.i = j
        return v

    def quoted(self, q):
        s = self.s
        self.i += 1
        out = []
        esc = {"n": "\n", "t": "\t", "r": "\r", "b": "\b", "f": "\f", "/": "/", "\\": "\\", '"': '"', "'": "'"}
        while self.i < len(s):
            c = s[self.i]
            if c == q:
                self.i += 1
                return "".join(out)
            if c == "\\":
                n = s[self.i + 1]
                if n == "u":
                    out.append(chr(int(s[self.i + 2:self.i + 6], 16)))
                    self.i += 6
                    continue
                out.append(esc.get(n, n))
                self.i += 2
                continue
            out.append(c)
            self.i += 1
        self.err("unterminated string")

    def multiline(self):
        s = self.s
        line_start = s.rfind("\n", 0, self.i) + 1
        indent = self.i - line_start
        self.i += 3
        end = s.find("'''", self.i)
        if end < 0:
            self.err("unterminated '''")
        raw = s[self.i:end]
        self.i = end + 3
        lines = raw.split("\n")
        if lines and not lines[0].strip():
            lines = lines[1:]
        res = []
        for ln in lines:
            k = 0
            while k < indent and k < len(ln) and ln[k] in " \t":
                k += 1
            res.append(ln[k:].rstrip("\r"))
        if res and not res[-1].strip():
            res = res[:-1]
        return "\n".join(res)


def flatten(obj, prefix=""):
    out = {}
    for k, v in obj.items():
        key = f"{prefix}.{k}" if prefix else k
        if isinstance(v, dict):
            out.update(flatten(v, key))
        elif isinstance(v, list):
            for n, x in enumerate(v):
                out[f"{key}[{n}]"] = x if isinstance(x, str) else json.dumps(x, ensure_ascii=False)
        else:
            out[key] = v
    return out


# ---------------------------------------------------------------- helpers
def norm(t):
    t = TOKEN.sub(" ", t.lower()).replace("ё", "е")
    t = re.sub(r"[^\w\s]", " ", t)
    return re.sub(r"\s+", " ", t).strip()


def shingles(t, n=3):
    w = norm(t).split()
    return {" ".join(w[i:i + n]) for i in range(max(0, len(w) - n + 1))}


def camel_words(name):
    name = name.split("/")[-1]
    return [w.lower() for w in re.findall(r"[A-Z][a-z]+|[a-z]+|[A-Z]+(?![a-z])", name)]


def mentions(text, words):
    t = text.lower()
    return any(w in t for w in words)


def short(t, n=90):
    t = t.replace("\n", " ")
    return t if len(t) <= n else t[:n - 1] + "…"


# ---------------------------------------------------------------- loaders
def load_lore(mod):
    lore_dir = mod / "Quests/Lore"
    nodes = {}
    for f in sorted(lore_dir.glob("*.json")):
        if f.name == "codex.json":
            continue
        nodes[f.stem] = json.loads(f.read_text("utf-8-sig"))
    codex_path = lore_dir / "codex.json"
    codex = json.loads(codex_path.read_text("utf-8-sig")) if codex_path.exists() else {}
    return nodes, codex


def read_hjson(path, rel):
    try:
        return flatten(Hjson(path.read_text("utf-8")).parse())
    except (ValueError, IndexError) as e:
        add("error", "hjson-syntax", rel, str(e))
        return {}


# ---------------------------------------------------------------- checks
def check_titles_and_texts(stories, nodes, texts):
    for lang in ("Ru", "En"):
        exact, near = defaultdict(list), defaultdict(list)
        for r in stories:
            for i, s in enumerate(r["Steps"]):
                t = s.get("Title" + lang, "")
                exact[t].append(f"{r['NpcKey']}:{i}")
                near[norm(t)].append((f"{r['NpcKey']}:{i}", t))
        for t, where in exact.items():
            if len(where) > 1:
                add("error", "dup-title", ", ".join(where), f"{lang.lower()} title repeated {len(where)}x: «{t}»")
        for n, items in near.items():
            if len({t for _, t in items}) > 1:
                add("error", "dup-title", ", ".join(w for w, _ in items),
                    f"{lang.lower()} titles differ only in case/punctuation: " + " | ".join(f"«{t}»" for _, t in items))

    # texts: list of (lang, field, where, text)
    for lang in ("ru", "en"):
        exact = defaultdict(list)
        for (lg, field, where, t) in texts:
            if lg != lang or not t.strip():
                continue
            exact[norm(t)].append((field, where, t))
        uniq = []
        for n, items in exact.items():
            if len(items) > 1 and len(n.split()) >= 3:
                add("error", "dup-text", "; ".join(f"{w} [{f}]" for f, w, _ in items),
                    f"{lang} text repeated {len(items)}x (normalized): «{short(items[0][2])}»")
            if len(n.split()) >= 7:
                uniq.append((n.split(), items[0]))
        # near duplicates: word-sequence similarity (difflib), templates differing in a few words
        for x in range(len(uniq)):
            wa, (fa, pa, ta) = uniq[x]
            for y in range(x + 1, len(uniq)):
                wb, (fb, pb, tb) = uniq[y]
                sm = difflib.SequenceMatcher(None, wa, wb, autojunk=False)
                if sm.real_quick_ratio() < NEAR_WARN or sm.quick_ratio() < NEAR_WARN:
                    continue
                r = sm.ratio()
                if r >= NEAR_WARN:
                    add("error" if r >= NEAR_ERROR else "warn", "near-dup-text", f"{pa} [{fa}] ~ {pb} [{fb}]",
                        f"{lang} near-duplicate (similarity {r:.2f}): «{short(ta, 70)}» ~ «{short(tb, 70)}»")
        # templated openings: the same first words reused by 3+ residents in one field
        groups = defaultdict(list)
        for (lg, field, where, t) in texts:
            if lg != lang or not t.strip():
                continue
            w = norm(t).split()
            n = 3 if field in ("epithet", "quote") else 5
            if len(w) > n:
                groups[(field.split(":")[0], " ".join(w[:n]))].append(where)
        for (field, head), where in groups.items():
            owners = sorted({re.sub(r":\d+$", "", p) for p in where})
            if len(owners) >= 3:
                add("warn", "templated-opening", ", ".join(owners),
                    f"{lang} {field} texts of {len(owners)} residents open with the same words «{head}…»")


def target_of(s):
    return s.get("NpcTarget") or s.get("TileToken") or s.get("Target") or ""


def check_mechanics(stories):
    for r in stories:
        seen = defaultdict(list)
        for i, s in enumerate(r["Steps"]):
            tgt = target_of(s)
            seen[(s["Kind"], tgt)].append(i)
        for (kind, tgt), idx in seen.items():
            if len(idx) > 1:
                if tgt:
                    add("error", "repeat-mechanic", f"{r['NpcKey']}:{','.join(map(str, idx))}",
                        f"same mechanic twice: {kind} -> {tgt}")
                else:
                    add("warn", "repeat-mechanic", f"{r['NpcKey']}:{','.join(map(str, idx))}",
                        f"target-less form {kind} used {len(idx)}x in one chain")
        # reward progression
        coins = [s.get("Coins", 0) for s in r["Steps"]]
        for i in range(1, len(coins)):
            if coins[i] < coins[i - 1]:
                add("warn", "reward-progression", f"{r['NpcKey']}:{i}", f"coins drop {coins[i - 1]} -> {coins[i]}")
        rew = Counter(s.get("Reward") for s in r["Steps"])
        for item, n in rew.items():
            if n > 1:
                add("warn", "reward-progression", r["NpcKey"], f"reward item {item} given {n}x in one chain")
        # field contract per kind
        need = {"Trial": ("NpcTarget", "ForbiddenDebuff"), "Observe": ("NpcTarget", "Seconds"), "Hunt": ("Target",),
                "Deliver": ("Target",), "Expedition": ("Target", "SourceTag"), "GatherEnv": ("TileToken",),
                "Visit": ("Target",), "Delayed": ("Days",), "DialogueChoice": ("Choices",)}
        for i, s in enumerate(r["Steps"]):
            for f in need.get(s["Kind"], ()):
                if not s.get(f):
                    add("error", "lore-mismatch", f"{r['NpcKey']}:{i}", f"{s['Kind']} without {f}")


def enemy(key):
    return key.split("/", 1)[1] if key.startswith("Terraria/") else None


TRIAL_TABLE = []


def check_creatures(stories):
    for r in stories:
        gate = RESIDENT_GATE.get(r["NpcKey"], "prehardmode")
        for i, s in enumerate(r["Steps"]):
            kind = s["Kind"]
            if kind not in ("Trial", "Hunt", "Observe"):
                continue
            key = s.get("NpcTarget") if kind != "Hunt" else s.get("Target")
            where = f"{r['NpcKey']}:{i}"
            name = enemy(key or "")
            if name is None:
                add("warn", "creature-availability", where, f"{kind} target {key} is modded: availability not in vanilla table")
                continue
            if name in PASSIVE and kind == "Observe":
                continue
            info = ENEMIES.get(name)
            if info is None:
                add("warn", "creature-availability", where, f"{kind} target {name} not in vanilla table (uncertain)")
                continue
            avail, biome, inflicts, conf = info
            if avail == "hardmode" and gate != "hardmode":
                add("error", "trial-availability" if kind == "Trial" else "creature-availability", where,
                    f"{kind} {name} is hardmode-only ({biome}) but stage {i} has no hardmode gate "
                    f"(engine has no per-stage gate; resident arrives pre-hardmode)")
            if kind != "Trial":
                continue
            deb = s.get("ForbiddenDebuff", "")
            TRIAL_TABLE.append({"stage": where, "enemy": name, "debuff": deb, "availability": avail, "biome": biome,
                                "residentGate": gate, "enemyInflicts": sorted(inflicts), "confidence": conf,
                                "debuffPossible": deb in inflicts})
            if deb not in inflicts:
                sev = "error" if conf == "high" else "warn"
                tag = "" if conf == "high" else " [uncertain: confirm on wiki/in game]"
                known = ", ".join(sorted(inflicts)) or "nothing"
                add(sev, "trial-debuff", where,
                    f"Trial: {name} never inflicts {deb} (inflicts {known}); 'kill without {deb}' is trivially free{tag}")
            elif conf != "high":
                add("warn", "trial-debuff", where, f"Trial: {name} -> {deb} believed possible [uncertain: confirm on wiki/in game]")
            if i <= EARLY_TRIAL_MAX_INDEX and avail == "prehardmode" and gate != "hardmode":
                add("error", "early-trial", where,
                    f"early Trial (stage {i}) uses {name} ({biome}, pre-hardmode surface/caverns); owner decision: "
                    f"early Trials belong to the Underworld or hardmode")


def check_lore_vs_stories(stories, nodes):
    lore_res = {}
    for node, doc in nodes.items():
        for r in doc.get("residents", []):
            if r.get("npcKey") in lore_res:
                add("error", "lore-mismatch", r["npcKey"], f"resident in two lore nodes: {lore_res[r['npcKey']][0]}, {node}")
            lore_res[r.get("npcKey")] = (node, r)
    keys = {r["NpcKey"] for r in stories}
    for k in sorted(set(lore_res) - keys):
        add("error", "lore-mismatch", k, "lore resident has no story in ResidentStories.json")
    for r in stories:
        k = r["NpcKey"]
        if k not in lore_res:
            add("error", "lore-mismatch", k, "story has no lore entry")
            continue
        node, lr = lore_res[k]
        if lr.get("force") != r.get("Force"):
            add("error", "lore-mismatch", k, f"force differs: stories={r.get('Force')} lore={lr.get('force')}")
        stages = {st.get("index"): st for st in lr.get("stages", [])}
        if len(stages) != len(r["Steps"]):
            add("error", "lore-mismatch", k, f"lore has {len(stages)} stages, stories {len(r['Steps'])}")
        for i, s in enumerate(r["Steps"]):
            where = f"{k}:{i}"
            st = stages.get(i)
            if st is None:
                add("error", "lore-mismatch", where, "no lore stage entry")
                continue
            if st.get("kind") != s["Kind"]:
                add("error", "lore-mismatch", where, f"kind stories={s['Kind']} lore={st.get('kind')}")
            if st.get("titleRu") != s.get("TitleRu"):
                add("error", "lore-mismatch", where, f"titleRu stories=«{s.get('TitleRu')}» lore=«{st.get('titleRu')}»")
            ids = [c["Id"] for c in s.get("Choices") or []]
            lids = list((st.get("choices") or {}).keys())
            if s["Kind"] == "DialogueChoice":
                if sorted(ids) != sorted(lids):
                    add("error", "lore-mismatch", where, f"choice ids stories={ids} lore={lids}")
            elif lids:
                add("error", "lore-mismatch", where, f"{s['Kind']} stage carries lore choices {lids}")
            if s["Kind"] == "Delayed" and not st.get("waiting"):
                add("error", "lore-mismatch", where, "Delayed stage without waiting text")
            # does the player-facing text name the mechanic's target?
            en = " ".join((st.get(f) or {}).get("en", "") for f in ("description", "offer"))
            kind, words = s["Kind"], None
            if kind in ("Trial", "Observe", "Hunt"):
                tgt = s.get("NpcTarget") if kind != "Hunt" else s.get("Target")
                cw = camel_words(tgt or "")
                words = tuple(w.rstrip("s") for w in cw if len(w) >= 3) or None
                if kind == "Trial":
                    deb = s.get("ForbiddenDebuff", "")
                    dw = DEBUFF_WORDS.get(deb, (deb.lower(),))
                    if not mentions(en, dw):
                        add("warn", "lore-target-mention", where, f"Trial text never mentions the forbidden state {deb}")
            elif kind in ("Deliver", "Expedition"):
                alts = (s.get("Target") or "").split("|")
                words = tuple(w.rstrip("s") for a in alts for w in camel_words(a) if len(w) >= 4) or None
            elif kind == "Visit":
                words = BIOME_WORDS.get(s.get("Target"))
            elif kind == "GatherEnv":
                words = TILE_WORDS.get(s.get("TileToken"))
            if words and not mentions(en, words):
                add("warn", "lore-target-mention", where,
                    f"{kind} -> {target_of(s)}: en description/offer never names it (looked for {'/'.join(words)})")


def walk_bilingual(obj, path, sink):
    if isinstance(obj, dict):
        if set(obj) & {"ru", "en"} and all(isinstance(v, (str, type(None))) for v in obj.values()):
            sink.append((path, obj))
            return
        for k, v in obj.items():
            walk_bilingual(v, f"{path}.{k}", sink)
    elif isinstance(obj, list):
        for n, v in enumerate(obj):
            label = v.get("npcKey") or v.get("id") or v.get("index") if isinstance(v, dict) else None
            walk_bilingual(v, f"{path}[{label if label is not None else n}]", sink)


def tokens(t):
    return sorted(TOKEN.findall(t or ""))


def check_bilingual_pair(where, ru, en, cat="parity"):
    if not (ru or "").strip() and not (en or "").strip():
        add("error", "empty", where, "ru and en empty")
        return
    if not (ru or "").strip():
        add("error", "empty", where, "ru empty")
    if not (en or "").strip():
        add("error", "empty", where, "en empty")
    if ru and en and tokens(ru) != tokens(en):
        add("error", "placeholder", where, f"placeholders differ ru={tokens(ru)} en={tokens(en)}")


def collect_texts(stories, nodes, codex):
    """Return (texts for dup checks, visible strings for jargon/title)."""
    texts, visible = [], []
    for r in stories:
        for i, s in enumerate(r["Steps"]):
            w = f"{r['NpcKey']}:{i}"
            check_bilingual_pair(w + ".title", s.get("TitleRu"), s.get("TitleEn"))
            visible += [("ru", w + ".TitleRu", s.get("TitleRu", "")), ("en", w + ".TitleEn", s.get("TitleEn", ""))]
            for c in s.get("Choices") or []:
                check_bilingual_pair(f"{w}.choice.{c.get('Id')}", c.get("TextRu"), c.get("TextEn"))
                visible += [("ru", f"{w}.choice", c.get("TextRu", "")), ("en", f"{w}.choice", c.get("TextEn", ""))]
    sink = []
    for node, doc in nodes.items():
        walk_bilingual(doc, f"lore/{node}", sink)
    walk_bilingual(codex, "lore/codex", sink)
    nullable = re.compile(r"\.(waiting|findHint|choices)$")
    for path, d in sink:
        ru, en = d.get("ru"), d.get("en")
        if ru is None and en is None and nullable.search(path):
            continue
        check_bilingual_pair(path, ru, en)
        if ru and en and ru.strip() == en.strip():
            add("error", "parity", path, f"ru == en: «{short(ru)}»")
        visible += [("ru", path, ru or ""), ("en", path, en or "")]
        m = re.search(r"\.stages\[(\d+)\]\.(description|offer|complete|waiting)$", path) or \
            re.search(r"\.stages\[(\d+)\]\.choices\.([^.]+)\.outcome$", path)
        res = re.search(r"\.residents\[([^\]]+)\]", path)
        if m and res:
            field = m.group(2) if m.re.pattern.endswith("waiting)$") else "outcome:" + m.group(2)
            w = f"{res.group(1)}:{m.group(1)}"
            texts += [("ru", field, w, ru or ""), ("en", field, w, en or "")]
        elif res and re.search(r"\.(epithet|quote|intro|findHint|epilogue)$", path):
            field = path.rsplit(".", 1)[1]
            texts += [("ru", field, res.group(1), ru or ""), ("en", field, res.group(1), en or "")]
    return texts, visible


def hjson_prefix(path):
    """tModLoader: '<locale>_Mods.X.Y.hjson' prefixes every key with 'Mods.X.Y'."""
    stem = path.stem
    return stem.split("_", 1)[1] if "_" in stem else ""


def load_language(mod, locale):
    merged = {}
    for f in sorted((mod / "Localization").rglob("*.hjson")):
        if not f.name.startswith(locale):
            continue
        rel = f.relative_to(mod).as_posix()
        pre = hjson_prefix(f)
        for k, v in read_hjson(f, rel).items():
            full = f"{pre}.{k}" if pre else k
            old = merged.get(full)
            if old is None or (not str(old[0]).strip() and str(v).strip()):
                merged[full] = (v, rel)  # a real value wins over an auto-generated empty one
            elif str(v).strip() and str(old[0]) != str(v):
                add("warn", "parity", f"{rel}:{full}",
                    f"{locale} key defined twice with different values (also in {old[1]}); load order decides")
    return merged


OWN = "Mods.DingDongBiomeHomes."


def check_hjson(mod, visible):
    en, ru = load_language(mod, "en-US"), load_language(mod, "ru-RU")
    total = {"files": len(list((mod / "Localization").rglob("*.hjson"))), "keys_en": len(en), "keys_ru": len(ru)}
    for k in sorted(set(en) - set(ru)):
        v, rel = en[k]
        sev = "error" if str(v).strip() and k.startswith(OWN) else "warn"
        add(sev, "parity", f"{rel}:{k}", f"no ru translation in any ru-RU file (en: «{short(str(v), 60)}»)")
    for k in sorted(set(ru) - set(en)):
        v, rel = ru[k]
        add("error" if k.startswith(OWN) else "warn", "parity", f"{rel}:{k}", f"ru key has no en counterpart (ru: «{short(str(v), 60)}»)")
    for k in sorted(set(en) & set(ru)):
        e, r = str(en[k][0]), str(ru[k][0])
        where = f"{ru[k][1]}:{k}"
        if not e.strip() and not r.strip():
            continue  # intentionally empty in both languages (e.g. Tooltip: "")
        if not e.strip() or not r.strip():
            add("error" if k.startswith(OWN) else "warn", "empty", where,
                f"empty value (ru={'empty' if not r.strip() else 'ok'}, en={'empty' if not e.strip() else 'ok'})")
            continue
        pe, pr = set(re.findall(r"\{\d+\}", e)), set(re.findall(r"\{\d+\}", r))
        if pe != pr:
            add("error" if k.startswith(OWN) else "warn", "placeholder", where,
                f"numbered placeholders differ ru={sorted(pr)} en={sorted(pe)}")
        letters = [c for c in re.sub(r"\[[^\]]*\]|\{[^}]*\}", "", r) if c.isalpha()]
        if k.startswith(OWN) and e == r and len(letters) >= 12 and not any(0x400 <= ord(c) <= 0x4FF for c in letters):
            add("warn", "parity", where, f"ru value identical to en (untranslated?): «{short(e, 60)}»")
    for k, (v, rel) in en.items():
        visible.append(("en", f"{rel}:{k}", str(v)))
    for k, (v, rel) in ru.items():
        visible.append(("ru", f"{rel}:{k}", str(v)))
    return total


def check_jargon(visible):
    for lang, where, t in visible:
        if not t:
            continue
        hits = sorted({n for rx, n in JARGON_RE if rx.search(t)})
        if not hits:
            continue
        is_hjson = ".hjson:" in where
        own = (not is_hjson) or (":" + OWN) in where
        if is_hjson and not own:
            continue  # other mods' own strings carried in the merged override file
        key = where.split(":", 1)[-1]
        hard = {"commit", "коммит", "git", "release seq", "todo marker", "placeholder", "heap", "working set"}
        sev = "warn" if is_hjson and JARGON_SOFT_KEY.search(key) and not set(hits) & hard else "error"
        add(sev, "jargon", where, f"{lang} visible text contains {', '.join(hits)}: «{short(t, 80)}»")


def check_title(visible):
    en_hits = [w for lg, w, t in visible if lg == "en" and TITLE_EN in t]
    ru_hits = [w for lg, w, t in visible if lg == "ru" and TITLE_RU_RE.search(t)]
    if not en_hits:
        add("error", "title", "en visible text", f"title \"{TITLE_EN}\" appears nowhere in player-visible en text")
    if not ru_hits:
        add("error", "title", "ru visible text",
            "title appears nowhere in player-visible ru text (accepted: «First Word Was» or «Первое слово было»; "
            "no Russian title is established in Narrative/FirstWordWas docs)")
    return {"en": en_hits[:10], "ru": ru_hits[:10]}


def check_generator(stories, scen_dir):
    if not scen_dir or not scen_dir.is_dir():
        add("warn", "generator-drift", str(scen_dir), "scenario source not found; drift not checked")
        return None
    rows = {}
    for f in sorted(scen_dir.glob("*-node.json")):
        for r in json.loads(f.read_text("utf-8-sig"))["residents"]:
            steps = []
            for i, s in enumerate(r["Steps"]):
                s = dict(s)
                if s["Kind"] == "EscortLite":  # mirrors Tools/BuildResidentStories.py
                    dest = s.pop("DestinationBiome", "")
                    s.update(Kind="Visit", Target=dest, Count=1800)
                s.setdefault("Count", 1)
                s.setdefault("Target", "")
                steps.append(s)
            rows[r["NpcKey"]] = (r.get("Force", ""), steps)
    drift = []
    for r in stories:
        src = rows.get(r["NpcKey"])
        if src is None:
            drift.append(f"{r['NpcKey']}: not in scenarios")
            continue
        if src[0] != r.get("Force"):
            drift.append(f"{r['NpcKey']}: force {src[0]} -> {r.get('Force')}")
        for i, (a, b) in enumerate(zip(src[1], r["Steps"])):
            diff = sorted(k for k in set(a) | set(b) if a.get(k) != b.get(k))
            if diff:
                drift.append(f"{r['NpcKey']}:{i} [{','.join(diff)}]")
    for d in drift:
        add("warn", "generator-drift", d.split(" ")[0],
            "ResidentStories.json differs from scenario source (re-running Tools/BuildResidentStories.py would revert): " + d)
    return len(drift)


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    as_json, strict = "--json" in argv, "--strict" in argv
    if not args:
        print(__doc__)
        return 2
    mod = Path(args[0]).resolve()
    scen = None
    if "--scenarios" in argv:
        scen = Path(argv[argv.index("--scenarios") + 1]).resolve()
        args = [a for a in args if Path(a).resolve() != scen]
    else:
        scen = (mod / "../../../Narrative/FirstWordWas/scenarios").resolve()
    stories = json.loads((mod / "Quests/ResidentStories.json").read_text("utf-8-sig"))
    nodes, codex = load_lore(mod)

    texts, visible = collect_texts(stories, nodes, codex)
    check_titles_and_texts(stories, nodes, texts)
    check_mechanics(stories)
    check_creatures(stories)
    check_lore_vs_stories(stories, nodes)
    hj = check_hjson(mod, visible)
    check_jargon(visible)
    title = check_title(visible)
    drift = check_generator(stories, scen)

    sev = Counter(f["severity"] for f in findings)
    cats = defaultdict(Counter)
    for f in findings:
        cats[f["category"]][f["severity"]] += 1
    ok = sev["error"] == 0 and (not strict or sev["warn"] == 0)
    report = {
        "result": "PASS" if ok else "FAIL",
        "residents": len(stories), "stages": sum(len(r["Steps"]) for r in stories),
        "loreNodes": len(nodes), "hjson": hj, "generatorDriftSteps": drift,
        "titleFoundAt": title, "trialTable": TRIAL_TABLE,
        "counts": {"error": sev["error"], "warn": sev["warn"]},
        "byCategory": {k: dict(v) for k, v in sorted(cats.items())},
        "findings": sorted(findings, key=lambda f: (f["severity"] != "error", f["category"], f["where"])),
    }
    if as_json:
        print(json.dumps(report, ensure_ascii=False, indent=1))
    else:
        print(f"{report['result']}  errors={sev['error']} warnings={sev['warn']}  "
              f"residents={report['residents']} stages={report['stages']} lore nodes={len(nodes)} "
              f"hjson files={hj['files']} keys en/ru={hj['keys_en']}/{hj['keys_ru']} driftSteps={drift}")
        print("  trial table (stage | enemy | debuff | availability | possible | confidence):")
        for t in TRIAL_TABLE:
            print(f"    {t['stage']} | {t['enemy']} | {t['debuff']} | {t['availability']} ({t['biome']}) | "
                  f"{'yes' if t['debuffPossible'] else 'NO'} | {t['confidence']}")
        for k, v in report["byCategory"].items():
            print(f"  {k}: " + ", ".join(f"{s}={n}" for s, n in sorted(v.items())))
        for f in report["findings"]:
            print(f"[{f['severity'].upper()}] {f['category']} | {f['where']} | {f['message']}")
    return 0 if ok else 1


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
