from pathlib import Path
import re,subprocess,sys
repo=Path(__file__).resolve().parents[4]
out=repo/'.tmp/tts-native-cleanup-tests-20261103';out.mkdir(parents=True,exist_ok=True)
source=(repo/'src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs').read_text(encoding='utf-8-sig')
def method(name):
 m=re.search(r'(?m)^\s*(?:internal|private) (?:static )?[^\n]+ '+name+r'\(',source);start=m.start();brace=source.index('{',m.end());depth=1;end=brace+1
 while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
methods=['StopAgentRhubarbRecordIfPossible','CancelAgentSpeechForRemoval','CancelTrackedAgentPlayback','InterruptAgentSpeechForCombat','StopAllLipSyncPlaybackAndCleanup','DeleteStoppedAudioFile','CleanupSceneLipSyncAfterPlaybackFinished','SafeStopAndReleaseSoundEvent','HandleRequestPlaybackCancelled']
(out/'Production.cs').write_text('using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;using TaleWorlds.MountAndBlade;using TaleWorlds.Engine;namespace AnimusForge {partial class SceneAudioLipSyncController {'+'\n'.join(method(x) for x in methods)+'}}')
adapter=(repo/'src/AF.GameAdapter.Bannerlord/Scene/NativeConversationSpeechAdapter.cs').read_text(encoding='utf-8-sig')
m=re.search(r'    internal void CancelMapPlayback\(',adapter);start=m.start();brace=adapter.index('{',m.end());depth=1;end=brace+1
while depth:depth+=(adapter[end]=='{')-(adapter[end]=='}');end+=1
(out/'MapProduction.cs').write_text('using System;using TaleWorlds.CampaignSystem;namespace AnimusForge {partial class NativeConversationSpeechAdapter {'+adapter[start:end]+'}}')
host=(repo/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
for hook in ['OnAgentRemoved','OnAgentDeleted','OnEndMission']:
 assert hook in host
assert 'tts.CancelRequests(scene, keepCurrent: false)' in adapter
print('PASS actual host death/delete/mission-end hooks and conversation-end cancel-current wiring')
(out/'Fixture.cs').write_text((Path(__file__).parent/'Fixture.cs.txt').read_text())
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
r=subprocess.run(['dotnet','run','--project',str(out/'Probe.csproj')],capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'run.log').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);sys.exit(r.returncode)
