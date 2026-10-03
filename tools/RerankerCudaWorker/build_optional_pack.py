"""Build a local, optional worker pack. Never writes to the game or changes its CPU DLLs."""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[2]


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(4 * 1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--deps', required=True, help='Existing prepare_dependencies.py output')
    p.add_argument('--output', required=True, help='Fresh directory below repository artifacts')
    p.add_argument('--zip', action='store_true')
    args = p.parse_args()
    deps = pathlib.Path(args.deps).resolve()
    output = pathlib.Path(args.output).resolve()
    if not output.is_relative_to(REPO / 'artifacts') or output.exists():
        raise ValueError('Output must be a new directory below repository artifacts.')
    receipts = json.loads((deps / 'dependency-receipt.json').read_text(encoding='utf8'))
    # Reuse pinned packages from the independent benchmark, verifying archive hashes
    # and extracting again from those archives instead of trusting a loose DLL folder.
    for receipt in receipts:
        archive = deps / 'downloads' / receipt['package']
        if digest(archive) != receipt['sha256']:
            raise ValueError('Dependency archive hash mismatch: ' + receipt['package'])
    output.mkdir(parents=True)
    pack = output / 'AnimusForge' / 'OptionalRuntimes' / 'RerankerCuda'
    pack.mkdir(parents=True)
    subprocess.run(['dotnet', 'build', str(REPO / 'tools/RerankerCudaWorker/RerankerCudaWorker.csproj'),
                    '-c', 'Release', '-o', str(pack),
                    '-p:RestorePackagesPath=' + str(output / 'nuget'),
                    '-p:BaseIntermediateOutputPath=' + str(output / 'obj') + '/'], check=True, cwd=REPO)
    licenses = pack / 'LICENSES'
    licenses.mkdir()
    for receipt in receipts:
        with zipfile.ZipFile(deps / 'downloads' / receipt['package']) as archive:
            for member in archive.infolist():
                name = pathlib.PurePosixPath(member.filename).name
                if not name:
                    continue
                if name.lower().endswith('.dll'):
                    if receipt['package'].endswith('.nupkg') and not member.filename.startswith('runtimes/win-x64/native/'):
                        continue
                    with archive.open(member) as src, (pack / name).open('wb') as dst:
                        shutil.copyfileobj(src, dst)
                elif any(term in name.lower() for term in ['license', 'notice', 'eula']):
                    (licenses / (receipt['package'] + '-' + name)).write_bytes(archive.read(member))
    # Preserve managed dependency license metadata from their original NuGet packages.
    for archive_path in (output / 'nuget').rglob('*.nupkg'):
        with zipfile.ZipFile(archive_path) as archive:
            for member in archive.infolist():
                name = pathlib.PurePosixPath(member.filename).name
                if name and (name.endswith('.nuspec') or any(t in name.lower() for t in ['license', 'notice'])):
                    (licenses / (archive_path.name + '-' + name)).write_bytes(archive.read(member))
    shutil.copy2(REPO / 'tools/RerankerCudaWorker/README.md', pack / 'README.md')
    manifest = {p.relative_to(pack).as_posix(): digest(p) for p in pack.rglob('*') if p.is_file()}
    (pack / 'manifest.json').write_text(json.dumps({'protocol': 1, 'ort': '1.22.0', 'files': manifest,
                                                  'sourcePackages': receipts}, indent=2), encoding='utf8')
    if args.zip:
        with zipfile.ZipFile(output / 'AnimusForge-Reranker-CUDA-optional.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as archive:
            for path in pack.rglob('*'):
                if path.is_file():
                    archive.write(path, path.relative_to(output))
    print('Optional pack:', pack)


if __name__ == '__main__':
    main()
