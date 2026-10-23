#!/usr/bin/env python3
"""Unity oyun katmanını (Assets/_Project/Scripts/Game) Unity lisansı olmadan derler.

Unity editörünün içindeki UnityEngine DLL'lerini, uGUI ve Input System paketlerinin
kaynaklarını kullanarak geçici bir .NET projesi kurar. Derleyici hatası ya da uyarı
varsa çıkış kodu 1 olur. Oyunun çalıştığını göstermez; yalnızca kodun gerçek Unity
API'lerine karşı derlendiğini doğrular.

Gerekenler: .NET 8 SDK, ayıklanmış bir Unity Linux editörü.
Kullanım:   python3 tools/unity_compile_check.py [UNITY_EDITOR_KLASÖRÜ]
            (varsayılan: $UNITY_EDITOR ya da /tmp/unity/Editor)
"""
import glob
import os
import subprocess
import sys
import tarfile
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFINES = ";".join([f"UNITY_{v}_OR_NEWER" for v in (
    "6000_3", "6000_2", "6000_1", "6000_0", "2023_3", "2023_2", "2023_1", "2022_3", "2022_2", "2022_1",
    "2021_3", "2021_2", "2021_1", "2020_3", "2020_2", "2020_1", "2019_4", "2019_3", "2019_2", "2019_1",
    "2018_4", "2018_3", "2018_2", "2018_1", "2017_4", "2017_3", "2017_2", "2017_1", "5_6", "5_5", "5_4", "5_3")]
    + ["ENABLE_INPUT_SYSTEM", "CSHARP_7_3_OR_NEWER"])
QUIET = "CS0618;CS0649;CS0414;CS0169;CS0067;CS1998;CS0108;CS0114;CS0672;CS0809;CS0219;CS0162;CS8632;CS0436"


def project(path, name, sources, extra="", refs=(), quiet=True, unsafe=False):
    data = os.path.join(EDITOR, "Data")
    unity_refs = "\n".join(f'    <Reference Include="{d}" Private="false" />'
                           for d in sorted(glob.glob(os.path.join(data, "Managed", "UnityEngine", "UnityEngine*.dll"))))
    compile_items = "\n".join(f'    <Compile Include="{s}" />' for s in sources)
    project_refs = "\n".join(f'    <ProjectReference Include="{r}" />' for r in refs)
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AssemblyName>{name}</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <AllowUnsafeBlocks>{str(unsafe).lower()}</AllowUnsafeBlocks>
    <NoWarn>{QUIET if quiet else ""}</NoWarn>
    <TreatWarningsAsErrors>{str(not quiet).lower()}</TreatWarningsAsErrors>
    <DefineConstants>{DEFINES};{extra}</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
{unity_refs}
{compile_items}
{project_refs}
  </ItemGroup>
</Project>
""")


def main():
    global EDITOR
    EDITOR = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("UNITY_EDITOR", "/tmp/unity/Editor")
    pm = os.path.join(EDITOR, "Data", "Resources", "PackageManager")
    if not os.path.isdir(pm):
        sys.exit(f"Unity editörü bulunamadı: {EDITOR}")
    work = tempfile.mkdtemp(prefix="pixtrain-unity-check-")
    ugui = os.path.join(pm, "BuiltInPackages", "com.unity.ugui", "Runtime", "UGUI")
    tgz = sorted(glob.glob(os.path.join(pm, "Editor", "com.unity.inputsystem-*.tgz")))[-1]
    with tarfile.open(tgz) as t:
        try:
            t.extractall(work, filter="data")
        except TypeError:  # Python < 3.12
            t.extractall(work)
    inputsystem = os.path.join(work, "package", "InputSystem")

    project(os.path.join(work, "ugui.csproj"), "UnityEngine.UI", [f"{ugui}/**/*.cs"],
            "PACKAGE_PHYSICS2D;PACKAGE_ANIMATION;PACKAGE_UITOOLKIT;PACKAGE_INPUTSYSTEM")
    project(os.path.join(work, "inputsystem.csproj"), "Unity.InputSystem", [f"{inputsystem}/Runtime/**/*.cs"],
            "UNITY_INPUT_SYSTEM_ENABLE_UI", refs=["ugui.csproj"], unsafe=True)
    with open(os.path.join(work, "inputsystem.csproj")) as f:
        text = f.read().replace(f'<Compile Include="{inputsystem}/Runtime/**/*.cs" />',
                                f'<Compile Include="{inputsystem}/**/*.cs" Exclude="{inputsystem}/Editor/**/*.cs;{inputsystem}/Runtime/Plugins/InputForUI/**/*.cs" />')
    with open(os.path.join(work, "inputsystem.csproj"), "w") as f:
        f.write(text)
    code = 0
    # Her hedef platformun tanımlarıyla ayrı derle (#if UNITY_WEBGL gibi dallar da kontrol edilsin).
    for platform in ("UNITY_ANDROID", "UNITY_WEBGL"):
        name = "game-" + platform.lower() + ".csproj"
        project(os.path.join(work, name), "PixTrain.Game",
                [os.path.join(ROOT, "unity/PixTrain/Assets/_Project/Scripts/Game/**/*.cs")], platform,
                refs=["ugui.csproj", "inputsystem.csproj",
                      os.path.join(ROOT, "dotnet/PixTrain.Core/PixTrain.Core.csproj"),
                      os.path.join(ROOT, "dotnet/PixTrain.Render/PixTrain.Render.csproj")],
                quiet=False)
        result = subprocess.run(["dotnet", "build", os.path.join(work, name), "-nologo"],
                                capture_output=True, text=True)
        lines = [l for l in result.stdout.splitlines() if " error " in l or " warning " in l or "Warning(s)" in l or "Error(s)" in l]
        print("== " + platform)
        print("\n".join(dict.fromkeys(lines)))
        code = code or result.returncode
    sys.exit(code)


if __name__ == "__main__":
    main()
