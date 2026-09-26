using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

// Compile this consumer against pinned main, then execute unchanged IL with the new core.
internal static class LegacyClient
{
    private sealed class Pipeline : IInteractionPipeline
    {
        public Task<InteractionResult> GenerateAsync(InteractionEnvelope envelope,LlmProviderSnapshot provider,CancellationToken token)
            => Task.FromResult(new InteractionResult(InteractionStatus.Succeeded,"old-client",null,Array.Empty<FactRecord>(),""));
    }
    private static async Task<int> Main()
    {
        try {
            using var coordinator=new InteractionRequestCoordinator(new Pipeline(),()=>1);
            var config=new RuntimeConfigSnapshot("test",1,new Dictionary<string,bool>{{"core",true}},new Dictionary<string,LlmProviderSnapshot>{{"provider",new LlmProviderSnapshot("provider","https://example.invalid","test",1000,64)}});
            foreach(var channel in new[]{InteractionChannel.NativeConversation,InteractionChannel.SceneShout,InteractionChannel.Courier}) {
                var id=new InteractionIdentity("session",channel,"npc");
                var envelope=new InteractionEnvelope(new GameInteractionSnapshot(id,new TraceContext("trace",1,1,"test","1.4"),"text","",0,0,Array.Empty<InteractionCandidate>(),Array.Empty<string>(),new Dictionary<string,string>()),Array.Empty<PromptMessage>());
                var result=await coordinator.ExecuteAsync(envelope,config,"core","provider",CancellationToken.None);
                if(result.VisibleReply!="old-client")throw new Exception("FAIL old client result");
                coordinator.Cancel(id);
            }
            coordinator.Dispose(); Console.WriteLine("PASS old main-compiled consumer runs unchanged against replacement core: constructor/Execute/Cancel/Dispose, 3 channels.");return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
}
