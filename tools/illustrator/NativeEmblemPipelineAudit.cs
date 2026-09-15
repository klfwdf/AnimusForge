using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

// Native GPU work is injected. Assertions concern actual production coordination,
// lossless image handling and the no-blit widget, not native shader fidelity.
public static class NativeEmblemPipelineAudit
{
    public sealed class Result { public string Name; public bool Passed; public string Evidence; }
    private static readonly List<Result> results = new List<Result>();
    private static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static Type pipeline, image;
    private static object NewPipeline(Func<string,int,bool,CancellationToken,Task<byte[]>> render)
    { return Activator.CreateInstance(pipeline, Instance, null, new object[] { render }, null); }
    private static Task<string> Get(object value, string code, int size, CancellationToken token)
    { return (Task<string>)pipeline.GetMethod("GetAsync", Instance).Invoke(value, new object[] { code, size, false, token }); }
    private static void Reset(object value) { pipeline.GetMethod("Reset", Instance).Invoke(value, null); }
    private static string Encode(byte[] bytes, int size)
    { return (string)image.GetMethod("Encode", Static).Invoke(null, new object[] { bytes, size }); }
    private static void Check(string name, bool pass, string detail)
    { Console.WriteLine("{0} {1}", pass ? "PASS" : "FAIL", name); results.Add(new Result { Name=name, Passed=pass, Evidence=detail }); }
    private static byte[] Png(Bitmap bitmap)
    { using (var ms = new MemoryStream()) { bitmap.Save(ms, ImageFormat.Png); return ms.ToArray(); } }
    private static byte[] NativeConvention(Bitmap expected)
    {
        using (var raw = new Bitmap(expected.Width, expected.Height))
        {
            for (int y=0; y<raw.Height; y++) for (int x=0; x<raw.Width; x++)
            { Color c=expected.GetPixel(x,y); raw.SetPixel(x,y,Color.FromArgb(c.A,c.B,c.G,c.R)); }
            return Png(raw);
        }
    }
    private static Bitmap Flag(int variant)
    {
        var bitmap = new Bitmap(128,128);
        using (var g=Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Purple);
            g.FillRectangle(Brushes.Blue,64,0,64,128); // full two-color background
            g.FillRectangle(Brushes.Red,12,8,104,104); // >55% foreground
            g.FillRectangle(Brushes.Gold,16,12,96,96); // rendered outline preserved
            g.FillRectangle(Brushes.Lime,22,20,12,35);
            g.FillRectangle(Brushes.Black,22,20,33,9);
            g.FillRectangle(Brushes.White,80,60,10,22);
        }
        if (variant==1) bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
        if (variant==2) bitmap.RotateFlip(RotateFlipType.Rotate270FlipX);
        return bitmap;
    }
    private static int Difference(Bitmap a,Bitmap b)
    {
        if(a.Size!=b.Size)return int.MaxValue;
        int differences=0;
        for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)
            if(a.GetPixel(x,y).ToArgb()!=b.GetPixel(x,y).ToArgb())differences++;
        return differences;
    }
    private static async Task<bool> Cancelled(Task task)
    {
        try { await task.ConfigureAwait(false); return false; }
        catch(OperationCanceledException) { return true; }
    }
    public static Result[] Run(string assemblyPath,string directory,string samples)
    { return RunAsync(assemblyPath,directory,samples).GetAwaiter().GetResult(); }
    private static async Task<Result[]> RunAsync(string assemblyPath,string directory,string samples)
    {
        Console.WriteLine("Native audit: loading production assembly");
        results.Clear(); Directory.CreateDirectory(directory);
        var assembly=Assembly.LoadFrom(assemblyPath);
        pipeline=assembly.GetType("AnimusForge.Illustrator.Engine.NativeBannerPipeline",true);
        image=assembly.GetType("AnimusForge.Illustrator.Engine.NativeBannerImage",true);
        byte[] valid;
        using(var expected=Flag(0))valid=NativeConvention(expected);
        for(int variant=0;variant<3;variant++)
        using(var expected=Flag(variant))
        {
            string encoded=Encode(NativeConvention(expected),128);
            using(var stream=new MemoryStream(Convert.FromBase64String(encoded)))
            using(var actual=new Bitmap(stream))
            {
                Check("rendered_pixels_preserved_"+variant,Difference(expected,actual)==0,"Synthetic native-format PNG; full background/outline/rotation/mirror remain pixel-identical after one channel normalization");
                actual.Save(Path.Combine(directory,"native-normalized-"+variant+".png"));
            }
            Check("lossless_png_"+variant,encoded.StartsWith("iVBORw0KGgo"),"PNG, not JPEG");
        }
        Check("truncated_file_rejected",Encode(new byte[]{137,80,78,71},128)==null,"No reference from incomplete output");
        using(var blank=new Bitmap(128,128))
        {
            Check("transparent_rejected",Encode(Png(blank),128)==null,"No opaque render content");
            using(var g=Graphics.FromImage(blank))g.Clear(Color.Purple);
            Check("flat_export_rejected",Encode(Png(blank),128)==null,"Conservative gate also omits deliberately uniform banners");
        }
        using(var rectangle=new Bitmap(128,64)) Check("wrong_dimensions_rejected",Encode(Png(rectangle),128)==null,"Native full banner must be square");
        string fault=Path.Combine(samples,"cell_162_raw.png");
        if(File.Exists(fault))Check("historical_blank_rejected",Encode(File.ReadAllBytes(fault),128)==null,"Actual old failed export fixture");
        string nativeSample=Path.Combine(samples,"native_banner_before_crash.png");
        string nativeEncoded=File.Exists(nativeSample)?Encode(File.ReadAllBytes(nativeSample),256):null;
        Check("actual_native_export_decodes",nativeEncoded!=null,"Actual 341px PNG written before the 2026-09-16 crash; does not verify engine resource retirement");
        if(nativeEncoded!=null)File.WriteAllBytes(Path.Combine(directory,"actual-native-normalized.png"),Convert.FromBase64String(nativeEncoded));
        int calls=0;
        object cache=NewPipeline((code,size,clean,token)=>{calls++; return Task.FromResult(valid);});
        string first=await Get(cache,"banner-A",128,CancellationToken.None);
        string second=await Get(cache,"banner-A",128,CancellationToken.None);
        Check("cache_hit",first!=null && first==second && calls==1,"Same full banner code and size rendered once");
        await Get(cache,"banner-B",128,CancellationToken.None);
        await Get(cache,"banner-A",256,CancellationToken.None);
        Check("cache_identity",calls==3,"Different banner or size never aliases");
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel();
            Check("cancelled_cached_request_rejected",await Cancelled(Get(cache,"banner-A",128,cancelled.Token)),"Even a cached reference cannot escape caller cancellation");
        }
        Reset(cache); await Get(cache,"banner-A",128,CancellationToken.None);
        Check("campaign_cache_reset",calls==4,"Reset invalidates prior results");

        int attempts=0;
        object retry=NewPipeline((code,size,clean,token)=>Task.FromResult(++attempts==1?null:valid));
        Check("failure_not_cached",await Get(retry,"A",128,CancellationToken.None)==null && await Get(retry,"A",128,CancellationToken.None)!=null && attempts==2,"Later explicit request may recover");

        var release=new TaskCompletionSource<byte[]>(); int renders=0;
        object shared=NewPipeline((code,size,clean,token)=>{renders++;return release.Task;});
        Task<string> a=Get(shared,"A",128,CancellationToken.None), b=Get(shared,"A",128,CancellationToken.None);
        release.SetResult(valid);
        await Task.WhenAll(a,b);
        Check("queued_identical_requests_coalesce",renders==1 && a.Result==b.Result,"One native export; second caller consumes cache");

        var held=new TaskCompletionSource<byte[]>();
        object stale=NewPipeline((code,size,clean,token)=>held.Task);
        Task<string> old=Get(stale,"old",128,CancellationToken.None);
        Reset(stale); held.SetResult(valid);
        Check("late_campaign_result_dropped",await old==null && ((IDictionary)pipeline.GetField("_cache",Instance).GetValue(stale)).Count==0,"Renderer ignored cancellation, yet obsolete result cannot populate cache");

        var running=new TaskCompletionSource<byte[]>(); int accepted=0;
        object queue=NewPipeline((code,size,clean,token)=>{accepted++;return running.Task;});
        Task<string> runningTask=Get(queue,"A",128,CancellationToken.None);
        using(var stop=new CancellationTokenSource())
        {
            Task<string> waiting=Get(queue,"B",128,stop.Token); stop.Cancel();
            Check("cancel_before_admission",await Cancelled(waiting) && accepted==1,"Waiting cancelled request never starts native rendering");
        }
        running.SetResult(valid); await runningTask;
        object cancellable=NewPipeline(async (code,size,clean,token)=>{await Task.Delay(Timeout.Infinite,token);return valid;});
        using(var stop=new CancellationTokenSource())
        {
            Task<string> activeRequest=Get(cancellable,"A",128,stop.Token); stop.Cancel();
            Check("cancel_active_request",await Cancelled(activeRequest),"Cancellation is passed into native adapter");
        }

        int cappedCalls=0;
        object bounded=NewPipeline((code,size,clean,token)=>{cappedCalls++;return Task.FromResult(valid);});
        for(int i=0;i<40;i++)await Get(bounded,"banner-"+i,128,CancellationToken.None);
        Check("bounded_cache",((IDictionary)pipeline.GetField("_cache",Instance).GetValue(bounded)).Count==32,"32-entry ceiling");
        await Get(bounded,"banner-0",128,CancellationToken.None);
        Check("evicted_banner_renders_again",cappedCalls==41,"Oldest entry evicted");

        using(var noisy=new Bitmap(512,512))
        {
            var random=new Random(7531);
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)
                noisy.SetPixel(x,y,Color.FromArgb(255,random.Next(256),random.Next(256),random.Next(256)));
            byte[] large=Png(noisy);
            object budget=NewPipeline((code,size,clean,token)=>Task.FromResult(large));
            for(int i=0;i<5;i++)await Get(budget,"large-"+i,512,CancellationToken.None);
            Check("cache_byte_budget",(long)pipeline.GetField("_cacheBytes",Instance).GetValue(budget)<=8*1024*1024 && ((IDictionary)pipeline.GetField("_cache",Instance).GetValue(budget)).Count<5,"High-entropy PNGs evict by bytes before reaching entry limit");
        }

        // Run the actual override with null drawing contexts. A base call or a
        // draw would fail. Construct no UI/game object and request no native view.
        Type widget=assembly.GetType("AnimusForge.Illustrator.Engine.NativeBannerExportWidget",true);
        object exportWidget=FormatterServices.GetUninitializedObject(widget);
        widget.GetMethod("OnRender",Instance).Invoke(exportWidget,new object[]{null,null});
        FieldInfo requested=widget.BaseType.BaseType.GetField("_isRenderRequestedPreviousFrame",Instance);
        Check("no_screen_blit",(bool)requested.GetValue(exportWidget),"OnRender drives next provider update without touching draw context");
        // Populate only the native factory's registration metadata; no provider
        // construction and no full type scan/native initialization in offline test.
        Type factory=widget.BaseType.BaseType.Assembly.GetType("TaleWorlds.GauntletUI.TextureProviderFactory",true);
        var providerRegistry=(IDictionary)factory.GetField("_textureProvidertypes",Static).GetValue(null);
        var supported=widget.GetProperty("SupportsDeferredSceneClear",Static);
        Check("unregistered_provider_rejected",!(bool)supported.GetValue(null,null),"No guessed module Assembly.Load fallback when Gauntlet has not registered the provider");
        Type providerType=Type.GetType("TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.BannerTableauTextureProvider, TaleWorlds.MountAndBlade.GauntletUI",true);
        providerRegistry["BannerTableauTextureProvider"]=providerType;
        Check("runtime_deferred_clear_contract",(bool)supported.GetValue(null,null),"Factory fixture points at real loaded provider type; owner and scene metadata match, without native construction");
        var clear=widget.GetMethod("OnClearTextureProvider",Instance);
        clear.Invoke(exportWidget,null); clear.Invoke(exportWidget,null);
        Check("empty_provider_cleanup_idempotent",clear.DeclaringType==widget,"Override handles duplicate pre-initialization cleanup without native resource calls");

        // Real RenderTargetComponent event metadata, with no native pointer or
        // constructor. Verify one local subscription after the existing callback.
        var bind=widget.GetMethod("BindExportPaintHandler",Instance);
        Type componentType=bind.GetParameters()[0].ParameterType;
        object component=FormatterServices.GetUninitializedObject(componentType);
        GC.SuppressFinalize(component); // Uninitialized fixture must not alter engine object counters.
        var paint=componentType.GetEvent("PaintNeeded",Instance);
        var marker=Delegate.CreateDelegate(paint.EventHandlerType,typeof(NativeEmblemPipelineAudit).GetMethod("FakeVanillaPaint",Static));
        paint.GetAddMethod(true).Invoke(component,new object[]{marker});
        bind.Invoke(exportWidget,new[]{component}); bind.Invoke(exportWidget,new[]{component});
        var callbacks=((Delegate)componentType.GetField("PaintNeeded",Instance).GetValue(component)).GetInvocationList();
        Check("export_callback_is_local_ordered_and_unique",callbacks.Length==2 && callbacks[0].Equals(marker) && callbacks[1].Target==exportWidget && callbacks[1].Method.Name=="PrepareExportFrame","Vanilla callback retained first; export callback appended exactly once to this component");
        Check("unprepared_export_is_blocked",!(bool)widget.GetProperty("ReadyForExport",Instance).GetValue(exportWidget,null),"Attaching a handler alone cannot authorize a native save");
        callbacks[1].DynamicInvoke(new object[]{null,EventArgs.Empty});
        Check("missing_render_does_not_authorize_export",!(bool)widget.GetProperty("ReadyForExport",Instance).GetValue(exportWidget,null),"No native scene/texture means no readiness advancement");

        // Native stage reset/late completion owner checks without engine objects.
        Type helper=assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper",true);
        Type pump=helper.GetNestedType("OffscreenStagePump",BindingFlags.NonPublic);
        object oldPump=Activator.CreateInstance(pump,true), newPump=Activator.CreateInstance(pump,true);
        var active=helper.GetField("_activeStage",Static);
        active.SetValue(null,newPump);
        helper.GetMethod("FinishStage",Static).Invoke(null,new object[]{oldPump});
        Check("late_stage_cleanup_keeps_replacement",ReferenceEquals(active.GetValue(null),newPump),"Old export cannot retire the new active stage");
        helper.GetMethod("FinishStage",Static).Invoke(null,new object[]{newPump});
        Check("stage_cleanup_idempotent",active.GetValue(null)==null && (int)pump.GetField("Finished").GetValue(newPump)==1,"Current stage retired exactly once");

        Type runtime=assembly.GetType("AnimusForge.Illustrator.Core.IllustratorRuntime",true);
        var pending=(ConcurrentQueue<Action>)runtime.GetField("Pending",Static).GetValue(null);
        var pendingCount=runtime.GetField("_pendingCount",Static);
        var mainThread=runtime.GetField("_mainThread",Static);
        var dispatch=helper.GetMethod("RunOnGameThreadAsync",Static).MakeGenericMethod(typeof(int));
        int previousMain=(int)mainThread.GetValue(null);
        mainThread.SetValue(null,0); // Prevent any native Tick; explicitly execute queued delegates.
        try
        {
            int executed=0; Action action;
            using(var stop=new CancellationTokenSource())
            {
                Task<int> queued=(Task<int>)dispatch.Invoke(null,new object[]{new Func<int>(()=>++executed),stop.Token});
                stop.Cancel();
                Check("dispatch_cancel_without_tick",await Cancelled(queued),"Queued request completes cancellation even if game stops ticking");
                if(pending.TryDequeue(out action))action();
                pendingCount.SetValue(null,0);
                Check("dispatch_cancelled_work_skipped",executed==0,"Late queue drain cannot create a cancelled stage");
            }
            using(var stop=new CancellationTokenSource())
            {
                Task<int> admitted=null; bool returnedEarly=false;
                admitted=(Task<int>)dispatch.Invoke(null,new object[]{new Func<int>(()=>{stop.Cancel();returnedEarly=admitted.IsCompleted;return 41;}),stop.Token});
                if(pending.TryDequeue(out action))action();
                pendingCount.SetValue(null,0);
                Check("admitted_dispatch_preserves_ownership",await admitted==41 && !returnedEarly,"Cancellation inside admitted work cannot release its caller before stage creation returns");
            }

            object retiring=Activator.CreateInstance(pump,true);
            pump.GetField("Done").SetValue(retiring,new TaskCompletionSource<string>());
            active.SetValue(null,retiring);
            pendingCount.SetValue(null,32); // Simulate full bounded queue.
            Task retirement=(Task)helper.GetMethod("RetireStageAsync",Static).Invoke(null,new[]{retiring});
            Check("full_queue_keeps_stage_lock",!retirement.IsCompleted && (int)pump.GetField("CancelRequested").GetValue(retiring)==1,"Rejected cleanup post cannot pretend native stage is retired");
            mainThread.SetValue(null,Environment.CurrentManagedThreadId);
            helper.GetMethod("CancelActiveStage",Static).Invoke(null,null);
            Check("reset_unblocks_stage_retirement",retirement.IsCompleted && active.GetValue(null)==null,"Main-thread reset completes retirement even without another queued tick");
        }
        finally
        {
            mainThread.SetValue(null,previousMain);
            pendingCount.SetValue(null,0);
            Action remaining; while(pending.TryDequeue(out remaining)) { }
        }

        Check("cpu_shader_removed",assembly.GetType("AnimusForge.Illustrator.Engine.BannerEmblemComposer",true).GetMethod("TintIconCell",Static)==null,"No fallback to the known incorrect shader reconstruction");
        return results.ToArray();
    }
    private static void FakeVanillaPaint(object texture,EventArgs args) { }
}
