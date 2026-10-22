#!/usr/bin/env python3
"""Unity .meta dosyalarını üretir.

Unity, Assets altındaki her dosya ve klasör için bir .meta dosyası ister ve
eksikse açılışta rastgele GUID ile kendisi üretir. Editör açılmadan (bulutta,
CI'da) eklenen dosyalar için bu betik, yola göre deterministik GUID'li
.meta dosyaları yazar; böylece GUID'ler makineden makineye değişmez.

Kullanım:  python3 tools/unity_meta.py [unity/PixTrain/Assets]
Var olan .meta dosyalarına dokunmaz.
"""
import hashlib
import os
import sys

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASMDEF = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

TEXT = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

DEFAULT = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FONT = """fileFormatVersion: 2
guid: {guid}
TrueTypeFontImporter:
  externalObjects: {{}}
  serializedVersion: 4
  fontSize: 16
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames: []
  fallbackFontReferences: []
  customCharacters: 
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

TEMPLATES = {".cs": SCRIPT, ".asmdef": ASMDEF, ".asmref": ASMDEF, ".md": TEXT, ".txt": TEXT, ".json": TEXT,
             ".ttf": FONT, ".otf": FONT}
# Bu uzantıların importer ayarları (doku, ses, sahne) Unity'ye bırakılır.
SKIP = {".png", ".jpg", ".wav", ".ogg", ".mp3", ".prefab", ".asset", ".mat", ".anim", ".controller"}


def guid_for(rel_path: str) -> str:
    return hashlib.md5(("pixtrain:" + rel_path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def write_meta(path: str, rel: str, template: str) -> bool:
    meta = path + ".meta"
    if os.path.exists(meta):
        return False
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(template.format(guid=guid_for(rel)))
    return True


def main() -> None:
    root = sys.argv[1] if len(sys.argv) > 1 else os.path.join("unity", "PixTrain", "Assets")
    project = os.path.dirname(os.path.abspath(root))
    created = 0
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if not d.startswith(".") and not d.endswith("~")]
        for d in dirnames:
            full = os.path.join(dirpath, d)
            created += write_meta(full, os.path.relpath(full, project), FOLDER)
        for name in filenames:
            if name.endswith(".meta") or name.startswith("."):
                continue
            ext = os.path.splitext(name)[1].lower()
            if ext in SKIP:
                continue
            full = os.path.join(dirpath, name)
            created += write_meta(full, os.path.relpath(full, project), TEMPLATES.get(ext, DEFAULT))
    print(f"{created} .meta dosyası oluşturuldu")


if __name__ == "__main__":
    main()
