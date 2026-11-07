"""Prints the serialized field values of prefabs (GameObjects whose name fully matches PREFABS):
every component, and for MonoBehaviours (Humanoid, ItemDrop, SpawnAbility, ...) their fields,
read with UnityPy's TypeTreeGenerator from the game's own assemblies. References to other
objects print as their name. Usage: prefab_dump.py <valheim dir> (env PREFABS, SKIP)."""
import os
import re
import sys

import UnityPy
from UnityPy.classes import PPtr
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

root = sys.argv[1]
data_dir = os.path.join(root, "valheim_server_Data")
pattern = re.compile(os.environ["PREFABS"])
skip = re.compile(os.environ.get("SKIP") or r"^$a")
MAX_DEPTH = 9
MAX_LIST = 40


def ref_name(p):
    if not p or p.m_PathID == 0:
        return "null"
    try:
        r = p.deref()
        n = r.peek_name()
        if not n and r.type.name in ("MonoBehaviour", "Transform", "Animator", "Rigidbody"):
            try:
                n = r.parse_as_object().m_GameObject.deref().peek_name()
            except Exception:
                pass
        return f"<{r.type.name} {n}>"
    except Exception as e:
        return f"<ref {p.m_FileID}:{p.m_PathID} unresolved>"


def fields(o):
    if hasattr(o, "__attrs_attrs__"):
        return [(a.name, getattr(o, a.name, None)) for a in o.__attrs_attrs__]
    if hasattr(o, "__dict__"):
        return [(k, v) for k, v in vars(o).items() if not k.startswith("_") and k not in ("object_reader", "assets_file")]
    return None


def dump(name, v, depth, out):
    pad = "  " * depth
    if skip.search(name):
        return
    if isinstance(v, PPtr):
        out.append(f"{pad}{name}: {ref_name(v)}")
    elif isinstance(v, (list, tuple)):
        if len(v) == 0:
            out.append(f"{pad}{name}: []")
            return
        if all(isinstance(x, (int, float, str, bool)) for x in v):
            out.append(f"{pad}{name}: {list(v)[:MAX_LIST]}")
            return
        out.append(f"{pad}{name}: [{len(v)}]")
        if depth >= MAX_DEPTH:
            return
        for i, x in enumerate(v[:MAX_LIST]):
            dump(f"[{i}]", x, depth + 1, out)
    elif isinstance(v, (int, float, str, bool)) or v is None:
        s = repr(v)
        out.append(f"{pad}{name}: {s[:200]}")
    elif isinstance(v, dict):
        out.append(f"{pad}{name}: {{dict {len(v)}}}")
        if depth < MAX_DEPTH:
            for k, x in list(v.items())[:MAX_LIST]:
                dump(str(k), x, depth + 1, out)
    else:
        fs = fields(v)
        if fs is None:
            out.append(f"{pad}{name}: {str(v)[:200]}")
            return
        out.append(f"{pad}{name}:")
        if depth >= MAX_DEPTH:
            return
        for k, x in fs:
            dump(k, x, depth + 1, out)


paths = []
for r, _, files in os.walk(data_dir):
    for f in files:
        p = os.path.join(r, f)
        if "Managed" in p or "MonoBleedingEdge" in p or f.endswith((".so", ".json", ".dll", ".txt", ".config", ".resource", ".resS")):
            continue
        paths.append(p)
env = UnityPy.load(*paths)
version = None
for obj in env.objects:
    version = obj.assets_file.unity_version
    break
print("Unity version", version)
gen = TypeTreeGenerator(version)
gen.load_local_game(root)
env.typetree_generator = gen

found = 0
for obj in env.objects:
    if obj.type.name != "GameObject":
        continue
    try:
        name = obj.peek_name()
    except Exception:
        continue
    if not name or not pattern.fullmatch(name):
        continue
    found += 1
    go = obj.parse_as_object()
    print(f"=================== prefab {name} ===================")
    comps = getattr(go, "m_Component", None) or getattr(go, "m_Components", [])
    for c in comps:
        p = getattr(c, "component", None) or getattr(c, "second", None) or c
        try:
            r = p.deref()
        except Exception as e:
            print("  component unresolved", e)
            continue
        if r.type.name != "MonoBehaviour":
            print(f"  [{r.type.name}]")
            continue
        try:
            mb = r.parse_as_object()
            script = ref_name(mb.m_Script)
            out = []
            for k, x in fields(mb) or []:
                if k in ("m_GameObject", "m_Enabled", "m_Script", "m_Name", "m_EditorHideFlags", "m_EditorClassIdentifier", "m_ObjectHideFlags"):
                    continue
                dump(k, x, 2, out)
            print(f"  [MonoBehaviour {script}]")
            print("\n".join(out))
        except Exception as e:
            print(f"  [MonoBehaviour] failed to read: {e!r}")
print(f"{found} prefab(s) matched")
