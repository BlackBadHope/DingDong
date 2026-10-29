"""Strict offline check of the Ding Dong lore data contract "dingdong-lore-v1".

Usage: python -I Tools/Quests/check_lore.py <modDir> [--scenarios <dir>] [--json]

Mirrors LoreCatalog.ContractPass (C#). Reads <modDir>/Quests/ResidentStories.json and
<modDir>/Quests/Lore/<node>.json + codex.json. Exit code 0 = PASS, 1 = FAIL.
Icons can only be resolved inside the game (LoreCatalog.ContractPass); offline the
icon name is checked for shape only.
"""
import json
import math
import re
import sys
from pathlib import Path

NODES = {
    "town": ["Terraria/Guide", "Terraria/Merchant", "Terraria/Nurse", "Terraria/ArmsDealer", "Terraria/Demolitionist", "Terraria/GoblinTinkerer", "Terraria/Mechanic"],
    "arts": ["Terraria/Painter", "Terraria/Stylist", "Terraria/PartyGirl", "Terraria/DyeTrader", "Terraria/Clothier", "Terraria/DD2Bartender"],
    "nature": ["Terraria/Dryad", "Terraria/WitchDoctor", "Terraria/Truffle", "Terraria/Angler", "Terraria/Golfer", "Terraria/BestiaryGirl"],
    "wanderers": ["Terraria/Pirate", "Terraria/SantaClaus", "Terraria/TaxCollector", "Terraria/Steampunker", "Terraria/Cyborg", "Terraria/Wizard", "Terraria/Princess"],
    "ocean": ["CalamityMod/SeaKing", "ThoriumMod/Diverman", "FishermanNPC/Fisherman", "UnuBattleRods/Fishylady", "CalValEX/JellyPriestNPC"],
    "alchemist": ["AlchemistNPCLite/Alchemist", "AlchemistNPCLite/Brewer", "AlchemistNPCLite/Jeweler", "AlchemistNPCLite/Operator", "AlchemistNPCLite/Architect", "AlchemistNPCLite/Musician", "AlchemistNPCLite/Tinkerer", "AlchemistNPCLite/YoungBrewer"],
    "calamity": ["CalamityMod/Archmage", "CalamityMod/Bandit", "CalamityMod/BrimstoneWitch", "CalValEX/Jharim", "CalValEX/OracleNPC", "miningcracks_take_on_luiafk/Deeps", "WeaponEnchantments/Witch"],
    "craftsmen": ["ThoriumMod/Blacksmith", "ThoriumMod/Cobbler", "ThoriumMod/Cook", "ThoriumMod/Tracker", "ThoriumMod/DesertAcolyte"],
    "fargo": ["Fargowiltas/Mutant", "Fargowiltas/Deviantt", "Fargowiltas/Abominationn", "Fargowiltas/LumberJack", "Fargowiltas/Squirrel"],
    "thorium": ["ThoriumMod/ConfusedZombie", "ThoriumMod/WeaponMaster", "ThoriumMod/Druid", "ThoriumMod/Spiritualist", "MagicStorage/Golem"],
}
FORCES = {"gardeners", "tiders", "archivists", "tricksters", "unassigned"}
LEANS = {"gardeners", "tiders", "archivists", "none"}
ENDINGS = {"gardeners", "tiders", "archivists", "balance"}
FLAGS = {"hardMode", "downedBoss1", "downedBoss2", "downedBoss3", "downedQueenBee", "downedMechBossAny", "downedPlantBoss", "downedGolemBoss", "downedAncientCultist", "downedMoonlord"}
CHAPTERS = ["world", "forces", "arcs", "finale"]
REQUIRED = {
    "world.first-word": "always", "world.empty-houses": "always", "world.edges": "stages:10",
    "world.ocean-unspoken": "any[flag:downedBoss3,resident:CalamityMod/SeaKing:2,resident:ThoriumMod/Diverman:2]",
    "world.second-phrase": "flag:hardMode", "world.moon-seal": "flag:downedMoonlord",
    "forces.gardeners": "forceStages:gardeners:3", "forces.tiders": "forceStages:tiders:3", "forces.archivists": "forceStages:archivists:3",
    "forces.tricksters": "forceStages:tricksters:2", "forces.personal": "forceStages:unassigned:2",
    "arcs.act1": "always", "arcs.act2": "mainStages:15", "arcs.act3": "mainStages:40", "arcs.act4": "mainStages:70", "arcs.act5": "mainStages:100",
    "finale.gardeners": "lean:gardeners", "finale.tiders": "lean:tiders", "finale.archivists": "lean:archivists", "finale.balance": "lean:balance",
}
ENTRY_ID = re.compile(r"^[a-z]+\.[a-z0-9-]+$")
TOKEN = re.compile(r"\{[^}]*\}")
ICON = re.compile(r"^([A-Za-z0-9_]+/)?[A-Za-z0-9_]+$")
MAIN_COUNT = 20


def text(t, lo, hi, path, errors):
    if not isinstance(t, dict) or not isinstance(t.get("ru"), str) or not isinstance(t.get("en"), str) or not t["ru"].strip() or not t["en"].strip():
        errors.append(f"{path}: ru/en required")
        return
    ru, en = t["ru"], t["en"]
    if not lo <= len(ru) <= hi:
        errors.append(f"{path}: ru length {len(ru)} outside {lo}..{hi}")
    if len(en) > math.ceil(hi * 1.2):
        errors.append(f"{path}: en length {len(en)} over {math.ceil(hi * 1.2)}")
    for s in (ru, en):
        for m in TOKEN.findall(s):
            if m not in ("{player}", "{resident}"):
                errors.append(f"{path}: token {m}")
        stripped = s.replace("{player}", "").replace("{resident}", "")
        if "\t" in s or "[c/" in s or "[i:" in s or "{" in stripped or "}" in stripped:
            errors.append(f"{path}: forbidden markup")
    letters = [c for c in TOKEN.sub("", ru) if c.isalpha()]
    cyr = sum(1 for c in letters if 0x0400 <= ord(c) <= 0x04FF)
    if letters and cyr * 100 < len(letters) * 85:
        errors.append(f"{path}: ru is not Russian enough ({cyr}/{len(letters)})")
    if ru.strip() == en.strip():
        errors.append(f"{path}: ru == en")


def parse_cond(c, path, errors):
    if not isinstance(c, dict) or len(c) != 1:
        errors.append(f"{path}: condition needs exactly one key")
        return None
    (k, v), = c.items()
    try:
        if k == "always":
            if v is not True:
                errors.append(f"{path}: always must be true")
                return None
            return ("always",)
        if k in ("mainStages", "stages"):
            if not isinstance(v, int) or isinstance(v, bool):
                raise ValueError
            return (k, v)
        if k == "forceStages":
            return (k, str(v["force"]), int(v["count"]))
        if k == "resident":
            return (k, str(v["npcKey"]), int(v["stage"]))
        if k == "choice":
            return (k, str(v["npcKey"]), int(v["stage"]), str(v["id"]))
        if k in ("lean", "flag"):
            if not isinstance(v, str):
                raise ValueError
            return (k, v)
        if k in ("all", "any"):
            if not isinstance(v, list):
                raise ValueError
            kids = [parse_cond(x, f"{path}.{k}[{i}]", errors) for i, x in enumerate(v)]
            if any(x is None for x in kids):
                return None
            return (k, kids)
    except (KeyError, TypeError, ValueError):
        errors.append(f"{path}: malformed {k}")
        return None
    errors.append(f"{path}: unknown condition {k}")
    return None


def canonical(c):
    k = c[0]
    if k == "always":
        return "always"
    if k in ("mainStages", "stages", "lean", "flag"):
        return f"{k}:{c[1]}"
    if k == "forceStages":
        return f"forceStages:{c[1]}:{c[2]}"
    if k == "resident":
        return f"resident:{c[1]}:{c[2]}"
    if k == "choice":
        return f"choice:{c[1]}:{c[2]}:{c[3]}"
    return f"{k}[" + ",".join(sorted(canonical(x) for x in c[1])) + "]"


def feasible(c, path, stories, errors):
    k = c[0]
    if k == "mainStages" and not 0 <= c[1] <= MAIN_COUNT * 5:
        errors.append(f"{path}: mainStages out of range")
    elif k == "stages" and not 0 <= c[1] <= len(stories) * 5:
        errors.append(f"{path}: stages out of range")
    elif k == "forceStages":
        members = sum(1 for s in stories.values() if s["Force"] == c[1])
        if c[1] not in FORCES or not 0 <= c[2] <= members * 5:
            errors.append(f"{path}: forceStages unreachable")
    elif k == "resident" and (c[1] not in stories or not 1 <= c[2] <= 5):
        errors.append(f"{path}: resident condition unreachable")
    elif k == "choice":
        steps = stories.get(c[1], {}).get("Steps", [])
        step = steps[c[2]] if 0 <= c[2] < len(steps) else None
        if not step or step.get("Kind") != "DialogueChoice" or c[3] not in [x["Id"] for x in step.get("Choices", [])]:
            errors.append(f"{path}: choice condition references no authored choice")
    elif k == "lean" and c[1] not in ENDINGS:
        errors.append(f"{path}: unknown lean {c[1]}")
    elif k == "flag" and c[1] not in FLAGS:
        errors.append(f"{path}: unknown flag {c[1]}")
    elif k in ("all", "any"):
        if not c[1]:
            errors.append(f"{path}: empty {k}")
        for i, x in enumerate(c[1]):
            feasible(x, f"{path}[{i}]", stories, errors)


def load(path, errors):
    try:
        raw = path.read_bytes()
        if raw.startswith(b"\xef\xbb\xbf"):
            errors.append(f"{path.name}: UTF-8 BOM is not allowed")
            raw = raw[3:]
        return json.loads(raw.decode("utf-8"))
    except FileNotFoundError:
        errors.append(f"{path.name}: missing")
    except (UnicodeDecodeError, json.JSONDecodeError) as e:
        errors.append(f"{path.name}: unreadable ({e})")
    return None


def check(mod_dir, scenarios=None):
    errors = []
    stories = {r["NpcKey"]: r for r in json.loads(Path(mod_dir, "Quests", "ResidentStories.json").read_text("utf-8-sig"))}
    lore_dir = Path(mod_dir, "Quests", "Lore")
    present = sorted(p.stem for p in lore_dir.glob("*.json")) if lore_dir.is_dir() else []
    extra = sorted(set(present) - set(NODES) - {"codex"})
    if extra:
        errors.append("unexpected lore files: " + ", ".join(extra))
    if scenarios:
        for node, members in NODES.items():
            sp = Path(scenarios, f"{node}-node.json")
            if sp.exists():
                listed = [r["NpcKey"] for r in json.loads(sp.read_text("utf-8"))["residents"]]
                if sorted(listed) != sorted(members):
                    errors.append(f"{node}: scenarios membership differs from the embedded map")
    seen, residents_ok, stages_ok = {}, 0, 0
    for node, members in NODES.items():
        d = load(lore_dir / f"{node}.json", errors)
        if d is None:
            continue
        if d.get("format") != "dingdong-lore-node-v1":
            errors.append(f"{node}: format")
        if d.get("node") != node:
            errors.append(f"{node}: node name")
        page = d.get("nodePage") or {}
        text(page.get("title"), 1, 40, f"{node}.nodePage.title", errors)
        text(page.get("body"), 300, 900, f"{node}.nodePage.body", errors)
        for r in d.get("residents") or []:
            key = r.get("npcKey", "")
            if key not in members:
                errors.append(f"{node}: foreign resident {key}")
            if key in seen:
                errors.append(f"{node}: duplicate {key} (also in {seen[key][0]})")
            seen[key] = (node, r)
        for key in members:
            if key not in seen or seen[key][0] != node:
                errors.append(f"{node}: resident must appear once {key}")
    for key, story in stories.items():
        if key not in seen:
            errors.append(f"{key}: lore missing")
            continue
        r = seen[key][1]
        before = len(errors)
        if r.get("force") != story["Force"]:
            errors.append(f"{key}: force {r.get('force')} != {story['Force']}")
        text(r.get("epithet"), 1, 40, f"{key}.epithet", errors)
        text(r.get("quote"), 1, 110, f"{key}.quote", errors)
        text(r.get("intro"), 200, 480, f"{key}.intro", errors)
        if r.get("findHint") is not None:
            text(r.get("findHint"), 1, 200, f"{key}.findHint", errors)
        text(r.get("epilogue"), 150, 400, f"{key}.epilogue", errors)
        stages = r.get("stages") or []
        if len(stages) != len(story["Steps"]):
            errors.append(f"{key}: stages {len(stages)} != {len(story['Steps'])}")
        for i, (s, step) in enumerate(zip(stages, story["Steps"])):
            p, sb = f"{key}:{i}", len(errors)
            if not isinstance(s, dict):
                errors.append(f"{p}: null stage")
                continue
            if s.get("index") != i or s.get("titleRu") != step["TitleRu"] or s.get("kind") != step["Kind"]:
                errors.append(f"{p}: index/titleRu/kind misaligned")
            text(s.get("description"), 80, 220, f"{p}.description", errors)
            text(s.get("offer"), 1, 160, f"{p}.offer", errors)
            text(s.get("complete"), 1, 160, f"{p}.complete", errors)
            offer = s.get("offer") or {}
            if any(isinstance(offer.get(l), str) and offer[l].count("{player}") > 1 for l in ("ru", "en")):
                errors.append(f"{p}.offer: {{player}} more than once")
            delayed, choice = step["Kind"] == "Delayed", step["Kind"] == "DialogueChoice"
            if delayed != (s.get("waiting") is not None):
                errors.append(f"{p}: waiting must exist exactly for Delayed")
            if s.get("waiting") is not None:
                text(s["waiting"], 1, 120, f"{p}.waiting", errors)
            if choice != (s.get("choices") is not None):
                errors.append(f"{p}: choices must exist exactly for DialogueChoice")
            if s.get("choices") is not None:
                ids = sorted(c["Id"] for c in step.get("Choices", []))
                if sorted(s["choices"]) != ids:
                    errors.append(f"{p}: choice ids differ from ResidentStories")
                for cid, c in s["choices"].items():
                    if not isinstance(c, dict):
                        errors.append(f"{p}.choices.{cid}: null")
                        continue
                    text(c.get("outcome"), 1, 160, f"{p}.choices.{cid}.outcome", errors)
                    if c.get("lean") not in LEANS:
                        errors.append(f"{p}.choices.{cid}: lean {c.get('lean')}")
            if len(errors) == sb:
                stages_ok += 1
        if len(errors) == before:
            residents_ok += 1
    codex_ok = 0
    codex = load(lore_dir / "codex.json", errors)
    if codex is not None:
        if codex.get("format") != "dingdong-lore-codex-v1":
            errors.append("codex: format")
        chapters = codex.get("chapters") or []
        ids = [c.get("id") for c in chapters]
        if len(set(ids)) != len(ids):
            errors.append("codex: duplicate chapter")
        for cid in CHAPTERS:
            if cid not in ids:
                errors.append(f"codex: chapter missing {cid}")
        for c in chapters:
            if c.get("id") not in CHAPTERS + ["places"]:
                errors.append(f"codex: unknown chapter {c.get('id')}")
            if not isinstance(c.get("order"), int):
                errors.append(f"codex.chapter.{c.get('id')}: order")
            text(c.get("title"), 1, 30, f"codex.chapter.{c.get('id')}", errors)
        entries = codex.get("entries") or []
        eids = [e.get("id") for e in entries]
        if len(set(eids)) != len(eids):
            errors.append("codex: duplicate entry id")
        by_id, always = {}, 0
        for e in entries:
            before = len(errors)
            eid, chapter = e.get("id") or "", e.get("chapter") or ""
            if not ENTRY_ID.match(eid) or not eid.startswith(chapter + ".") or chapter not in CHAPTERS:
                errors.append(f"{eid}: id/chapter")
            if not isinstance(e.get("order"), int):
                errors.append(f"{eid}: order")
            if not isinstance(e.get("icon"), str) or not ICON.match(e["icon"]):
                errors.append(f"{eid}: icon {e.get('icon')}")
            text(e.get("title"), 1, 40, f"{eid}.title", errors)
            text(e.get("body"), 300, 1200, f"{eid}.body", errors)
            text(e.get("lockedHint"), 1, 120, f"{eid}.lockedHint", errors)
            cond = parse_cond(e.get("unlock"), eid, errors)
            if cond is not None:
                feasible(cond, eid, stories, errors)
                by_id[eid] = cond
                always += cond[0] == "always"
            if len(errors) == before:
                codex_ok += 1
        for rid, want in REQUIRED.items():
            if rid not in eids:
                errors.append(f"codex: required entry missing {rid}")
            elif rid not in by_id or canonical(by_id[rid]) != want:
                errors.append(f"codex: {rid} unlock must be {want}")
        count = lambda ch: sum(1 for e in entries if e.get("chapter") == ch)
        if len(entries) < 30:
            errors.append("codex: fewer than 30 entries")
        if count("world") < 6:
            errors.append("codex: world needs 6+")
        for ch, n in (("forces", 5), ("arcs", 5), ("finale", 4)):
            if count(ch) != n:
                errors.append(f"codex: {ch} must be {n}")
        if always < 3:
            errors.append("codex: fewer than 3 always entries")
    return {"verdict": "PASS" if not errors else "FAIL", "residents": residents_ok, "stages": stages_ok,
            "codex": codex_ok, "errors": errors}


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    mod_dir = Path(argv[1])
    scenarios = None
    if "--scenarios" in argv:
        scenarios = Path(argv[argv.index("--scenarios") + 1])
    res = check(mod_dir, scenarios)
    if "--json" in argv:
        print(json.dumps(res, ensure_ascii=False, indent=1))
    else:
        print(f"DINGDONG_LORE_CHECK|verdict={res['verdict']}|residents={res['residents']}/61|stages={res['stages']}/305|codex={res['codex']}|errors={len(res['errors'])}")
        for e in res["errors"][:60]:
            print("  " + e)
        if len(res["errors"]) > 60:
            print(f"  ... +{len(res['errors']) - 60} more")
    return 0 if res["verdict"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
