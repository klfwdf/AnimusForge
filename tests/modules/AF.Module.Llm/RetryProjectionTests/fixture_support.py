"""Real retry projection closure and embedded resources for detached production replays."""
from pathlib import Path
from xml.sax.saxutils import escape

def retry_members(source, declaration, signatures):
    members = [declaration(source, signature) for signature in signatures]
    if 'private static class RetryErrorProjection' in source:
        members.append(declaration(source, 'private static class RetryErrorProjection'))
    return '\n'.join(members)

def add_retry_resources(root, project, deps=None):
    root, project = Path(root), Path(project)
    deps = Path(deps) if deps else root / '_deps_auto'
    text = project.read_text(encoding='utf-8')
    items = '<Compile Include="' + escape(str(root / 'src/modules/AF.Module.Llm/Protocol/LlmSourceTexts.cs')) + '" />'
    items += '<EmbeddedResource Include="' + escape(str(root / 'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json')) + '" LogicalName="AnimusForge.Llm.RuntimeSources.json" />'
    if 'AnimusForge.Foundation.StaleRequestError.txt' not in text:
        items += '<EmbeddedResource Include="' + escape(str(root / 'content/foundation/AF.Foundation.Localization/ModuleData/RuntimeStaleRequestCompatibility.txt')) + '" LogicalName="AnimusForge.Foundation.StaleRequestError.txt" />'
    for name in ['TaleWorlds.Localization', 'TaleWorlds.Library', 'Newtonsoft.Json']:
        if 'Reference Include="' + name + '"' not in text:
            path = (root / '.tmp/build_check/1.4' if name == 'Newtonsoft.Json' else deps) / (name + '.dll')
            if not path.exists(): path = root / '.tmp/build_check/1.4' / (name + '.dll')
            items += '<Reference Include="' + name + '"><HintPath>' + escape(str(path)) + '</HintPath></Reference>'
    project.write_text(text.replace('</Project>', '<ItemGroup>' + items + '</ItemGroup></Project>'), encoding='utf-8')
