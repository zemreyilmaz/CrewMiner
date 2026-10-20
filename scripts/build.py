#!/usr/bin/env python3
"""Build genuine reference-based mods without an installed game. Python 3.9+, .NET 8 SDK."""
import argparse, hashlib, json, pathlib, shutil, subprocess, urllib.request, zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
def run(args,log):
    result=subprocess.run(args,cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    log.write(result.stdout);log.flush();print(result.stdout,end='')
    if result.returncode: raise SystemExit(result.returncode)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--refs-zip',type=pathlib.Path);args=parser.parse_args()
    lock=json.loads((ROOT/'dependencies.lock.json').read_text())
    archive=args.refs_zip or ROOT/'dependencies/luacsforbarotrauma_refs.zip'
    if not archive.exists():
        archive=ROOT/'build/refs.zip';archive.parent.mkdir(parents=True,exist_ok=True)
        print('Downloading published Luatrauma references...')
        urllib.request.urlretrieve(lock['reference_asset_url'],archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest()!=lock['reference_sha256']:
        raise SystemExit('Reference SHA256 mismatch. Use the original bundled refs ZIP; do not substitute a different game version.')
    refs=ROOT/'Refs';refs.mkdir(exist_ok=True)
    with zipfile.ZipFile(archive) as z:
        for name in z.namelist():
            if '..' in pathlib.PurePosixPath(name).parts or pathlib.PurePosixPath(name).is_absolute():raise SystemExit('Unsafe ZIP member')
        z.extractall(refs)
    for relative,expected in lock['reference_dlls_sha256'].items():
        if hashlib.sha256((refs/relative).read_bytes()).hexdigest()!=expected:raise SystemExit('Corrupt reference '+relative)
    build=ROOT/'build';build.mkdir(exist_ok=True)
    with (build/'verification.log').open('w',encoding='utf-8') as log:
        run(['dotnet','--version'],log)
        run(['dotnet','run','--project','tests','-c','Release'],log)
        run(['dotnet','run','--project','tools/ApiAudit','-c','Release',f'-p:RefsDir={refs}','--',str(refs)],log)
        package=build/'LocalMods/CrewMiner'
        if package.exists():shutil.rmtree(package)
        shutil.copytree(ROOT/'Assets',package)
        for platform in ['Windows','Linux','OSX']:
            dest=package/'bin/Client'/platform
            run(['dotnet','build','src/CrewMiner.csproj','-c','Release',f'-p:GamePlatform={platform}',f'-p:RefsDir={refs}','--no-incremental','-o',str(dest)],log)
        for name in ['README.md','THIRD_PARTY.md','dependencies.lock.json']:
            shutil.copy2(ROOT/name,package/name)
        shutil.copytree(ROOT/'docs',package/'docs')
        validate_package(package)
        target=build/'CrewMiner-0.1.1-mod.zip'
        with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as z:
            for p in sorted(package.rglob('*')):
                if p.is_file():z.write(p,p.relative_to(package.parent.parent))
        print(f'Mod ZIP: {target}')
def validate_package(package):
    import xml.etree.ElementTree as ET
    for path in package.rglob('*.xml'):ET.parse(path)
    for filename in ['filelist.xml','ModConfig.xml','Orders.xml']:
        for element in ET.parse(package/filename).iter():
            for key,value in element.attrib.items():
                if key.lower() in ['file','texture'] and value.startswith('%ModDir%/') and not (package/value[len('%ModDir%/'):]).is_file():
                    raise SystemExit(f'Missing package asset: {value}')
    if list(package.rglob('Barotrauma.dll')) or list(package.rglob('0Harmony.dll')):raise SystemExit('Reference binaries must not be shipped in runtime mod')
    print('Package XML and asset paths verified.')
if __name__=='__main__':main()
