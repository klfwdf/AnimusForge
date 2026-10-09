using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AnimusForge;
class Program {
 static int count; static void Check(bool ok,string what){count++;if(!ok)throw new Exception(what);}
 static int Main(){
  int ui=Environment.CurrentManagedThreadId;
  var engine=(OnnxEmbeddingEngine)Activator.CreateInstance(typeof(OnnxEmbeddingEngine),true);
  bool rejected=false;try {engine.InitializeAsync(null);}catch(ArgumentException){rejected=true;}Check(rejected,"uncaptured root rejected before worker");
  var watch=Stopwatch.StartNew();var task=engine.InitializeAsync("detached-root");watch.Stop();
  try {
   Check(watch.ElapsedMilliseconds<200,"InitializeAsync blocked caller");
   Check(AnimusForgeModelStore.Entered.Wait(5000),"worker not reached");
   Check(!task.IsCompleted,"native/file boundary must actually yield");
   Check(AnimusForgeModelStore.Thread!=ui,"model work ran on UI thread");
   Check(AnimusForgeModelStore.Root=="detached-root","wrong captured path");
   for(int i=0;i<8;i++)Check(ReferenceEquals(task,engine.InitializeAsync("ignored-later-root")),"duplicate worker");
  } finally {AnimusForgeModelStore.Release.Set();}
  Check(task.Wait(5000)&&!task.Result,"missing model must complete false");
  Check(AnimusForgeModelStore.Calls==1,"repeated model load");
  Check(engine.LastError=="fixture missing model","error state lost");
  Check(ReferenceEquals(task,engine.InitializeAsync("detached-root")),"completed worker not reused");
  Console.WriteLine($"Engine checks={count} failures=0; complete production engine source, controlled file/model boundary; no native inference/model loaded");return 0;
 }
}
namespace AnimusForge {
 internal static class AnimusForgeModelStore {
  internal sealed class ModelFiles { public string ModelPath="",TokenizerPath="",ConfigPath=""; }
  public static ManualResetEventSlim Entered=new(),Release=new();public static int Thread,Calls;public static string Root;
  public static ModelFiles ResolveEmbedding()=>throw new Exception("uncaptured module lookup on worker");
  public static ModelFiles ResolveEmbedding(string root){Thread=Environment.CurrentManagedThreadId;Root=root;Interlocked.Increment(ref Calls);Entered.Set();Release.Wait();throw new Exception("fixture missing model");}
 }
 internal static class Logger { public static void Log(string a,string b){} public static void Metric(string a,bool ok,double ms){} public static void Obs(string a,string b,Dictionary<string,object> c){} }
}
