from pathlib import Path
import subprocess, json, uuid, hashlib, sys
root=Path(__file__).resolve().parents[4]
here=Path(__file__).parent
out=root/"artifacts/tests/persona-evolution"/("run-"+uuid.uuid4().hex)
out.mkdir(parents=True)
src=(root/"src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
start=src.index("internal class NpcPersonaProfile")
opening=src.index("{",start); depth=1; end=opening+1
while depth:
    depth += (src[end]=="{")-(src[end]=="}"); end+=1
(out/"Profile.cs").write_text("using System;using System.Collections.Generic;namespace AnimusForge;public partial class MyBehavior {"+src[start:end]+"}",encoding="utf-8")
sources=[
 "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaEvolution.cs",
 "src/modules/AF.Module.Persona/Profiles/PersonaEvolutionPolicy.cs",
 "src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs",
 "src/modules/AF.Module.Persona/Import/PersonaImportOwner.cs",
 "src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs",
 "src/AF.GameAdapter.Bannerlord/Persistence/CampaignPersonaPersistenceAdapter.cs",
 "src/AF.Persistence/CampaignSaveChunkHelper.cs"]
for name in ["Program.cs","Stubs.cs"]:
    (out/name).write_bytes((here/name).read_bytes())
items="".join('<Compile Include="'+str(root/p).replace("\\","/")+'" />' for p in sources)
(out/"Replay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><EnableDefaultNoneItems>false</EnableDefaultNoneItems></PropertyGroup><ItemGroup>'+items+'<Reference Include="Newtonsoft.Json"><HintPath>$(MSBuildToolsPath)/Newtonsoft.Json.dll</HintPath></Reference></ItemGroup></Project>',encoding="utf-8")
result=subprocess.run(["dotnet","run","--project",str(out/"Replay.csproj"),"--",str(root)],cwd=root,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding="utf-8",errors="replace")
(out/"output.log").write_text(result.stdout,encoding="utf-8")
print(result.stdout[-8000:]);print(out)
(out/"receipt.json").write_text(json.dumps({"exit":result.returncode,"sources":{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in sources},"profile":"actual production nested DTO extracted; game/provider/scheduler declarations are fixtures"},indent=2),encoding="utf-8")
sys.exit(result.returncode)
