using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class RealPackage
{
    internal static async Task Run(string root,string output)
    {
        Directory.CreateDirectory(output);
        // Real-package validation must never create or replace module identity.
        // Both lab roots and installed modules must already provide a valid owner.
        string identityPath=Path.Combine(root,"SubModule.xml");
        if(!Directory.Exists(Path.Combine(root,"ModuleData")) || !File.Exists(identityPath))
            throw new InvalidDataException("Existing module identity and ModuleData are required; real-package tests do not create them.");
        byte[] identityBytes=File.ReadAllBytes(identityPath);
        using(var identityStream=new MemoryStream(identityBytes))
            if((string)System.Xml.Linq.XDocument.Load(identityStream).Root?.Element("Id")?.Attribute("value")!="AnimusForge")
                throw new InvalidDataException("Not an AnimusForge module root.");
        AnimusForgeModulePaths.Root=root;
        var settings=new DuelSettings {EnableTtsSpeech=true,TtsUseGptSoVits=true};
        var report=new JObject { ["scope"]="source-linked production manager/gateway/catalog with real portable L/U process; not native game playback",["audio"]=new JArray() };
        var clock=Stopwatch.StartNew();
        try
        {
            GptSoVitsManagedRuntime.Observe(settings);
            while(GptSoVitsManagedRuntime.State!="Ready" && GptSoVitsManagedRuntime.State!="Failed" && clock.Elapsed.TotalSeconds<160)await Task.Delay(250);
            if(!GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl,out string endpoint,out string instance)||GptSoVitsManagedRuntime.State!="Ready")throw new Exception("real runtime not ready: "+GptSoVitsManagedRuntime.State+":"+GptSoVitsManagedRuntime.Reason);
            if (!GptSoVitsManagedRuntime.SupportsLanguageSelection) throw new InvalidOperationException("real_package_language_selection_missing");
            report["ready_seconds"]=clock.Elapsed.TotalSeconds;
            report["package_id"]=GptSoVitsManagedRuntime.Reason;
            Console.WriteLine("Real portable L/U process ready in "+clock.Elapsed.TotalSeconds.ToString("F2")+"s");
            var catalog=GptSoVitsVoiceCatalog.LoadDirectory(Path.Combine(root,"VoicePacks"),out var notices);
            report["catalog_diagnostics"]=new JArray(notices);
            report["male_count"]=catalog.ForGender(false).Count;
            report["female_count"]=catalog.ForGender(true).Count;
            using var client=new HttpClient { Timeout=TimeSpan.FromSeconds(180) };
            var gateway=new GptSoVitsTtsGateway(client);
            var cases=new[]{("male-clear",2,"你答应过要听我说完，怎么现在又装作什么都不知道呢？","中文"),("female-sparkle",1,"别这么紧张嘛，我只是想看看，你究竟会不会认真听我说话。","中文"),("female-sparkle",0,"You promised to listen, so please tell me what really happened.","英语"),("male-clear",0,"こんにちは。最後まで話を聞いてください。","日语"),("female-sparkle",0,"你好呀，我哋先讲清楚件事，再决定下一步点做。","粤语")};
            int i=0;
            foreach(var row in cases)
            {
                var voice=catalog.Get(row.Item1).ForMood(row.Item2);clock.Restart();
                var request=new GptSoVitsSynthesisRequest(endpoint,row.Item3,voice,1f,instance,GptSoVitsLanguage.Parse(row.Item4));
                var result=await gateway.SynthesizeAsync(request,CancellationToken.None);
                if(!result.Success)throw new Exception("real synthesis failed: "+result.ErrorCode);
                string filename=Path.Combine(output,(++i).ToString("D2")+"-portable.wav");File.WriteAllBytes(filename,result.AudioBytes);
                ((JArray)report["audio"]).Add(new JObject{["voice"]=row.Item1,["tone"]=row.Item2,["text_lang"]=request.Language.EngineCode,["reference_sha256"]=voice.ReferenceHash,["text"]=row.Item3,["seconds"]=clock.Elapsed.TotalSeconds,["bytes"]=result.AudioBytes.Length,["sha256"]=Convert.ToHexString(SHA256.HashData(result.AudioBytes))});
                Console.WriteLine("PASS portable real voice "+row.Item1+" tone="+row.Item2+" bytes="+result.AudioBytes.Length+" seconds="+clock.Elapsed.TotalSeconds.ToString("F2"));
            }
            using(var bad=new HttpRequestMessage(HttpMethod.Get,endpoint.Replace("/tts","/af/health")))
            {using var reply=await client.SendAsync(bad);if((int)reply.StatusCode!=403)throw new Exception("instance identity not protected");}
            report["unauthorized_health_status"]=403;
        }
        finally
        {
            GptSoVitsManagedRuntime.Shutdown();
            for(int i=0;i<40&&GptSoVitsManagedRuntime.State!="Manual";i++)await Task.Delay(100);
            report["final_state"]=GptSoVitsManagedRuntime.State;
            bool identityUnchanged=File.Exists(identityPath) && identityBytes.SequenceEqual(File.ReadAllBytes(identityPath));
            report["module_identity_unchanged"]=identityUnchanged;
            File.WriteAllText(Path.Combine(output,"real-package-result.json"),report.ToString(Formatting.Indented));
            if(!identityUnchanged) throw new InvalidDataException("Module identity changed during the real-package test; no automatic restoration was attempted.");
        }
    }
}
