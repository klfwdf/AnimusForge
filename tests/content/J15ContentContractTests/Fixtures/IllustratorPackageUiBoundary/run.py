"""Compile production package/UI helpers; game TextObject only is a controlled fixture."""
from pathlib import Path
import subprocess, tempfile, shutil
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
MODULE = ROOT / 'content/modules/AnimusForge.Illustrator/ModuleData'
subprocess.run(['dotnet', 'build', str(HERE / 'IllustratorPackageUiBoundary.csproj'), '--nologo', '-v:q'], check=True, cwd=ROOT)
dll = Path(subprocess.check_output(['dotnet', 'msbuild', str(HERE / 'IllustratorPackageUiBoundary.csproj'), '-getProperty:TargetPath'], cwd=ROOT, text=True).strip())
assert dll.is_file(), dll
ui = {'CN': MODULE / 'Languages/CNs/illustrator_strings-zh-CN.xml', 'EN': MODULE / 'Languages/illustrator_strings.xml'}
with tempfile.TemporaryDirectory(prefix='illustrator-package-ui-', dir=ROOT / 'artifacts/localization') as scratch:
    for pack, filename in [('CN', 'IllustratorPromptTexts.json'), ('EN', 'IllustratorPromptTexts.en.json')]:
        directory = Path(scratch) / pack
        directory.mkdir()
        shutil.copy2(MODULE / filename, directory / 'IllustratorPromptTexts.json')
        shutil.copy2(MODULE / 'IllustratorCompatibilityTexts.json', directory / 'IllustratorCompatibilityTexts.json')
        for language, path in ui.items():
            other = ui['EN' if language == 'CN' else 'CN']
            subprocess.run(['dotnet', str(dll), str(directory), str(path), str(other)], check=True, cwd=ROOT)
            print('PASS package=' + pack + ' / UI=' + language + ' and UI-switch isolation', flush=True)
print('PASS four package/UI combinations; production loader and projection; full game UI NOT RUN')
