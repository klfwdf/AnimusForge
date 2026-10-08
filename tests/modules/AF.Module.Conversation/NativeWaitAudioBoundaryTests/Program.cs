using AnimusForge;
try { ShoutBehavior.RunNativeAudioBoundary(); await NativeWholeConsumer.RunAsync(); } catch (Exception e) { Console.WriteLine(e); Environment.ExitCode=1; }
