using System.Reflection;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; }
    private static TaskCompletionSource<InteractionResult> Pending() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static InteractionResult Success() => new(InteractionStatus.Succeeded,"reply",null,Array.Empty<FactRecord>(),"");
    private static RuntimeConfigSnapshot Config() => new("test",1,new Dictionary<string,bool>{{"core",true}},
        new Dictionary<string,LlmProviderSnapshot>{{"provider",new("provider","https://example.invalid","test",1000,64)}});
    private static InteractionEnvelope Envelope(InteractionChannel channel, string session="session") => new(
        new GameInteractionSnapshot(new InteractionIdentity(session,channel,"npc"),new TraceContext("trace",1,1,"test","1.4"),
            "hello","test",0,0,Array.Empty<InteractionCandidate>(),Array.Empty<string>(),new Dictionary<string,string>()),
        Array.Empty<PromptMessage>());
    private static Task<InteractionResult> Execute(InteractionRequestCoordinator owner, InteractionEnvelope envelope, CancellationToken token=default)
        => owner.ExecuteAsync(envelope,Config(),"core","provider",token);
    private sealed class Pipeline : IInteractionPipeline
    {
        internal Func<InteractionEnvelope,CancellationToken,Task<InteractionResult>> Run;
        internal int Calls;
        public Task<InteractionResult> GenerateAsync(InteractionEnvelope envelope,LlmProviderSnapshot provider,CancellationToken token)
        { Calls++; return Run(envelope,token); }
    }
    private static async Task Common(InteractionChannel channel)
    {
        var pipeline=new Pipeline { Run=(_,_)=>Task.FromResult(Success()) };
        using var owner=new InteractionRequestCoordinator(pipeline,()=>1); var env=Envelope(channel);
        Check((await Execute(owner,env)).VisibleReply=="reply","normal reply "+channel);
        Check((await owner.ExecuteAsync(env,null,"core","provider",default)).ErrorCode=="missing_runtime_config","missing config");
        Check((await owner.ExecuteAsync(env,Config(),"absent","provider",default)).Status==InteractionStatus.SkippedByEligibility,"disabled module");
        Check((await owner.ExecuteAsync(env,Config(),"core","absent",default)).Status==InteractionStatus.DegradedWithoutProvider,"missing provider");
        pipeline.Run=(_,_)=>throw new Exception("private details");
        Check((await Execute(owner,env)).ErrorCode=="pipeline_exception","exception isolation");
        pipeline.Run=(_,_)=>throw new OperationCanceledException();
        Check((await Execute(owner,env)).Status==InteractionStatus.CancelledAsStale,"cooperative cancellation");
        pipeline.Run=(_,_)=>Task.FromResult<InteractionResult>(null);
        Check((await Execute(owner,env)).ErrorCode=="null_pipeline_result","null result");
        using var stale=new InteractionRequestCoordinator(pipeline,()=>2);
        Check((await Execute(stale,env)).ErrorCode=="stale_before_start","generation rejection");
        var pending=Pending(); int calls=0;
        pipeline.Run=(_,_)=>++calls==1?pending.Task:Task.FromResult(Success());
        var first=Execute(owner,env); var newer=await Execute(owner,env); pending.SetResult(Success());
        Check(newer.Status==InteractionStatus.Succeeded && (await first).Status==InteractionStatus.CancelledAsStale,"latest same session wins");
        owner.Dispose(); owner.Dispose();
        try { await Execute(owner,env); Check(false,"disposed owner accepted request"); }
        catch(ObjectDisposedException) { checks++; }
    }
    private static async Task SupersedeThrow(InteractionChannel channel)
    {
        var pending=Pending(); int calls=0;
        var pipeline=new Pipeline { Run=(_,token)=> { if (++calls!=1) return Task.FromResult(Success()); token.Register(()=>throw new InvalidOperationException("test.cancel_callback")); return pending.Task; } };
        var owner=new InteractionRequestCoordinator(pipeline,()=>1);var env=Envelope(channel);var first=Execute(owner,env);
        try {
            InteractionResult second=null; Exception error=null;
            try { second=await Execute(owner,env); } catch(Exception e) { error=e; }
            Check(error==null && second?.Status==InteractionStatus.Succeeded,"throwing old callback blocked replacement "+channel);
            pending.TrySetResult(Success()); Check((await first).Status==InteractionStatus.CancelledAsStale,"old reply escaped");
        } finally { pending.TrySetResult(Success()); try { owner.Dispose(); } catch { } await first; }
    }
    private static async Task DisposeAll()
    {
        var pending=Pending(); var tokens=new List<CancellationToken>();
        var pipeline=new Pipeline { Run=(_,token)=> { tokens.Add(token); if(tokens.Count==1) token.Register(()=>throw new Exception("test.cancel_callback")); return pending.Task; } };
        var owner=new InteractionRequestCoordinator(pipeline,()=>1);
        var tasks=new[]{InteractionChannel.NativeConversation,InteractionChannel.SceneShout,InteractionChannel.Courier}.Select(c=>Execute(owner,Envelope(c))).ToArray();
        try {
            Exception error=null;try {owner.Dispose();}catch(Exception e){error=e;}
            Check(error==null && tokens.All(t=>t.IsCancellationRequested),"one callback prevented all-channel disposal");
            pending.TrySetResult(Success());Check((await Task.WhenAll(tasks)).All(r=>r.Status==InteractionStatus.CancelledAsStale),"disposed results remain committable");
        } finally {pending.TrySetResult(Success());try{owner.Dispose();}catch{}await Task.WhenAll(tasks);}
    }
    private static async Task TokenLifetime(InteractionChannel channel)
    {
        var pending=Pending();CancellationToken captured=default;
        var pipeline=new Pipeline {Run=(_,token)=>{captured=token;return pending.Task;}};
        using var owner=new InteractionRequestCoordinator(pipeline,()=>1);var env=Envelope(channel);var task=Execute(owner,env);
        try {
            owner.Cancel(env.Snapshot.Identity);
            bool usable=true;try{_ = captured.WaitHandle;}catch(ObjectDisposedException){usable=false;}
            Check(usable,"in-flight token disposed before operation completed "+channel);
            pending.TrySetResult(Success());Check((await task).Status==InteractionStatus.CancelledAsStale,"cancelled result escaped");
            bool disposed=false;try{_ = captured.WaitHandle;}catch(ObjectDisposedException){disposed=true;}
            Check(disposed,"completed token not released");
        } finally {pending.TrySetResult(Success());await task;}
    }
    private static async Task CancelCompletionRace()
    {
        var pending=Pending();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();bool tokenAlive=false;
        var pipeline=new Pipeline {Run=(_,token)=>{token.Register(()=>{entered.Set();release.Wait(TimeSpan.FromSeconds(5));try{var handle = token.WaitHandle;tokenAlive=true;}catch(ObjectDisposedException){}});return pending.Task;}};
        using var owner=new InteractionRequestCoordinator(pipeline,()=>1);var env=Envelope(InteractionChannel.SceneShout);var task=Execute(owner,env);
        Task cancel=Task.Run(()=>owner.Cancel(env.Snapshot.Identity));
        try {Check(entered.Wait(TimeSpan.FromSeconds(5)),"cancellation callback did not enter");pending.SetResult(Success());await task.WaitAsync(TimeSpan.FromSeconds(5));}
        finally {release.Set();pending.TrySetResult(Success());await cancel.WaitAsync(TimeSpan.FromSeconds(5));await task;}
        Check(tokenAlive,"completion disposed source while cancellation callback still running");
    }
    private static async Task PreCancelled()
    {
        var pipeline=new Pipeline {Run=(_,_)=>Task.FromResult(Success())};
        using var owner=new InteractionRequestCoordinator(pipeline,()=>1);using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var result=await Execute(owner,Envelope(InteractionChannel.Courier),cancellation.Token);
        Check(pipeline.Calls==0 && result.Status==InteractionStatus.CancelledAsStale,"already-cancelled request started generation");
    }
    private static async Task ReentrantDispose()
    {
        var pending=Pending();InteractionRequestCoordinator owner=null;
        var pipeline=new Pipeline {Run=(env,token)=>{if(env.Snapshot.Identity.Channel==InteractionChannel.NativeConversation)token.Register(()=>owner.Dispose());return pending.Task;}};
        owner=new InteractionRequestCoordinator(pipeline,()=>1);var env=Envelope(InteractionChannel.NativeConversation);
        var first=Execute(owner,env);var second=Execute(owner,Envelope(InteractionChannel.Courier));
        owner.Cancel(env.Snapshot.Identity);pending.SetResult(Success());
        Check((await first).Status==InteractionStatus.CancelledAsStale && (await second).Status==InteractionStatus.CancelledAsStale,"reentrant disposal did not retire both channels");
        owner.Dispose();
    }
    private static async Task Main(string[] args)
    {
        try {
            string scenario=args.FirstOrDefault()??"all";
            if(scenario=="surface") {
                foreach(var member in typeof(InteractionRequestCoordinator).GetMembers(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Select(x=>x.ToString()).OrderBy(x=>x)) Console.WriteLine(member);
                return;
            }
            foreach(var channel in new[]{InteractionChannel.NativeConversation,InteractionChannel.SceneShout,InteractionChannel.Courier}) {
                if(scenario=="common" || scenario=="all") await Common(channel);
                if(scenario=="supersede" || scenario=="all") await SupersedeThrow(channel);
                if(scenario=="token" || scenario=="all") await TokenLifetime(channel);
            }
            if(scenario=="dispose" || scenario=="all") await DisposeAll();
            if(scenario=="race" || scenario=="all") await CancelCompletionRace();
            if(scenario=="precancel" || scenario=="all") await PreCancelled();
            if(scenario=="reentrant" || scenario=="all") await ReentrantDispose();
            Console.WriteLine($"PASS {checks} lifecycle assertions ({scenario}); actual coordinator, deterministic pipeline, no game/API calls.");
        } catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
    }
}
