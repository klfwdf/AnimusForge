using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AnimusForge;

// One persistent worker per game process. All pipe exchanges and retirement share one gate.
// No game objects, PATH mutations, DLL replacement, background polling or automatic restart loop.
internal sealed class RerankerCudaProcess : IRerankerEncodedBackend
{
    private readonly object _gate = new object();
    private Process _process;
    private BinaryReader _reader;
    private BinaryWriter _writer;
    private bool _disabled;
    private readonly int _requestTimeoutMs;
    private readonly Action<string> _log;

    private RerankerCudaProcess(Action<string> log, int requestTimeoutMs)
    { _log = log; _requestTimeoutMs = requestTimeoutMs; }

    internal static RerankerCudaProcess Start(string executable, string moduleRoot, Action<string> log,
        int startupTimeoutMs = 120000, int requestTimeoutMs = 30000)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException("CUDA optional component missing.");
        var client = new RerankerCudaProcess(log, requestTimeoutMs);
        try
        {
            using (var parent = Process.GetCurrentProcess())
            {
                client._process = new Process { StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(executable), WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executable)),
                    Arguments = parent.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }};
                // Drain errors so a native provider warning cannot fill the pipe and deadlock.
                // Content is not echoed into game logs (may contain paths); failure gets a bounded code.
                client._process.ErrorDataReceived += (_, __) => { };
                if (!client._process.Start()) throw new IOException("CUDA worker did not start.");
                client._process.BeginErrorReadLine();
            }
            client._reader = new BinaryReader(client._process.StandardOutput.BaseStream, Encoding.UTF8, false);
            client._writer = new BinaryWriter(client._process.StandardInput.BaseStream, Encoding.UTF8, false);
            var ready = Task.Run(() =>
            {
                client._writer.Write(RerankerWire.Version);
                client._writer.Write(moduleRoot);
                client._writer.Flush();
                if (client._reader.ReadInt32() != RerankerWire.Version || client._reader.ReadString() != "CUDA_FP32")
                    throw new InvalidDataException("CUDA worker handshake mismatch.");
            });
            Await(ready, startupTimeoutMs);
            log?.Invoke("requested=CUDA active=CUDA worker_ready fp32=true tf32=false");
            return client;
        }
        catch { client.Dispose(); throw; }
    }

    private static void Await(Task operation, int timeoutMs)
    {
        // Observe exceptions even if retirement kills a timed-out exchange later.
        operation.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        if (!operation.Wait(timeoutMs)) throw new TimeoutException("CUDA worker timed out.");
        operation.GetAwaiter().GetResult();
    }

    public bool TryScore(List<List<long>> rows, List<int[]> masks, out List<float> scores)
    {
        scores = null;
        lock (_gate)
        {
            if (_disabled) return false;
            // Oversize batches retain the native CPU path; never trim the candidate list.
            if (rows == null || rows.Count > RerankerWire.MaxRows) return false;
            try
            {
                var operation = Task.Run(() =>
                {
                    RerankerWire.WriteRequest(_writer, rows, masks);
                    return RerankerWire.ReadScores(_reader, rows.Count);
                });
                Await(operation, _requestTimeoutMs);
                scores = operation.Result;
                return true;
            }
            catch (Exception ex)
            {
                Retire();
                _log?.Invoke("requested=CUDA active=CPU fallback=worker_inference_" + ex.GetBaseException().GetType().Name + " retry_gpu=false");
                return false;
            }
        }
    }

    private void Retire()
    {
        _disabled = true;
        try { if (_process != null && !_process.HasExited) _process.Kill(); } catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        try { _writer?.Dispose(); } catch (IOException) { }
        try { _reader?.Dispose(); } catch (IOException) { }
        _process?.Dispose();
    }

    public void Dispose() { lock (_gate) { if (!_disabled) Retire(); } }
}
