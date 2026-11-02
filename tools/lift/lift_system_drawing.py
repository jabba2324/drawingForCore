#!/usr/bin/env python3
"""Lift the System.Drawing assembly from WebFormsForCore (webformsforcore/WebFormsForCore, MIT) into drawingForCore.

On .NET Framework, System.Drawing.dll holds GDI+'s classes; on .NET they are in System.Drawing.Common, and the shared
framework's System.Drawing.dll facade forwards to it. WebFormsForCore's src/WebFormsForCore.Drawing replaces that facade
with an assembly of the same name and a higher version (13.5.0.0) that forwards GDI+'s types to System.Drawing.Common.
WebFormsForCore also defined its own FontConverter, ToolboxBitmapAttribute (a stub) and
BitmapSuffixInSatelliteAssemblyAttribute, with their forwards commented out; System.Drawing.Common 6.0 defines them
too, so code that named one of them didn't compile (CS0433). Here those three files aren't lifted and the forwards are
restored (FIXUPS). AssemblyInfo.cs is copied byte for byte from the commit below, then the FIXUPS are applied; the
project file is generated.

Usage: lift_system_drawing.py <WebFormsForCore checkout> <repository root>
The checkout may be at any commit that has COMMIT (read with git show). Writes tools/lift/manifests/System.Drawing.tsv.
"""
import os, shutil, subprocess, sys

NAME = "System.Drawing"
PROJECT = "drawingForCore.System.Drawing"
COMMIT = "52f4172dbc09b3ad0b849c97642720fa7a49aa22"  # webformsforcore/WebFormsForCore, 29 Oct 2026
SOURCE = "src/WebFormsForCore.Drawing"
FILES = ["AssemblyInfo.cs"]
# Not lifted: System.Drawing.Common defines these types, and System.Drawing forwards them (below).
EXCLUDED = ["BitmapSuffixInSatelliteAssemblyAttribute.cs", "FontConverter.cs", "ToolboxBitmapAttribute.cs"]

# (file, old, new). Nested types (FontConverter.FontNameConverter, ...) are forwarded with their containing type.
FIXUPS = [
    ("AssemblyInfo.cs", "//[assembly: TypeForwardedTo(typeof (" + name + "))]\n",
     "[assembly: TypeForwardedTo(typeof (" + name + "))] // drawingForCore: System.Drawing.Common defines it (was a duplicate, CS0433)\n")
    for name in ("BitmapSuffixInSatelliteAssemblyAttribute", "FontConverter", "ToolboxBitmapAttribute")
]


def main():
    upstream, root = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
    dest = os.path.join(root, "src", PROJECT)
    os.makedirs(dest, exist_ok=True)
    for entry in os.listdir(dest):
        if entry not in ("bin", "obj"):
            full = os.path.join(dest, entry)
            shutil.rmtree(full) if os.path.isdir(full) else os.remove(full)
    manifest = [f"# webformsforcore/WebFormsForCore {COMMIT}: source path (relative to repo root)\tdestination (relative to the repository root)"]
    for f in FILES:
        data = subprocess.check_output(["git", "-C", upstream, "show", f"{COMMIT}:{SOURCE}/{f}"])
        for name, old, new in FIXUPS:
            if name == f:
                assert data.count(old.encode()) == 1, (f, old)
                data = data.replace(old.encode(), new.encode())
                print(f"   fixup {f}: {new.split('typeof (')[1].split(')')[0]}")
        target = os.path.join(dest, f)
        open(target, "wb").write(data)
        manifest.append(f"{SOURCE}/{f}\t{os.path.relpath(target, root)}")

    open(os.path.join(dest, PROJECT + ".csproj"), "w").write(f"""<Project Sdk="Microsoft.NET.Sdk">

  <!-- System.Drawing (webformsforcore/WebFormsForCore {COMMIT[:7]}, {SOURCE}), lifted by
       tools/lift/lift_system_drawing.py: type forwards to System.Drawing.Common.
       Generated; do not edit. -->
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <AssemblyName>System.Drawing</AssemblyName>
    <RootNamespace>System.Drawing</RootNamespace>
    <!-- AssemblyInfo.cs has the version (13.5.0.0, above the shared framework's facade) and attributes. -->
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
    <DefineConstants>$(DefineConstants);WebFormsForCore</DefineConstants>
    <PackageId>drawingForCore</PackageId>
    <Description>System.Drawing for .NET 11 on Linux, macOS and Windows: the System.Drawing assembly (type forwards to System.Drawing.Common) over drawingForCore.Common, Microsoft's System.Drawing.Common 6.0 with GDI+ on SkiaSharp.</Description>
    <!-- System.Drawing's identity: public signed with its public key (keys/). -->
    <AssemblyOriginatorKeyFile>..\\..\\keys\\System.Drawing.snk</AssemblyOriginatorKeyFile>
    <SignAssembly>true</SignAssembly>
    <PublicSign>true</PublicSign>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\drawingForCore.System.Drawing.Common\\drawingForCore.System.Drawing.Common.csproj" />
  </ItemGroup>
</Project>
""")
    os.makedirs(os.path.join(root, "tools/lift/manifests"), exist_ok=True)
    open(os.path.join(root, "tools/lift/manifests", NAME + ".tsv"), "w").write("\n".join(manifest) + "\n")
    print(f"{len(FILES)} files")


if __name__ == "__main__":
    main()
