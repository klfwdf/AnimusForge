using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Adapters;

internal sealed class TtsStreamException : IOException
{
    internal string Code { get; }
    internal TtsStreamException(string code) : base(code) => Code = code;
}

// Batches audio, not HTTP packets. First audible batch is 0.5s, then 2s.
internal sealed class TtsPcmSegmenter : IDisposable
{
    private readonly Action<byte[], int, long?> _publish;
    private readonly int _rate;
    private readonly long? _samples;
    private readonly MemoryStream _pending = new MemoryStream();
    private long _total;
    private bool _audible, _started;
    internal TtsPcmSegmenter(int rate, long? samples, Action<byte[], int, long?> publish)
    { _rate = rate; _samples = samples; _publish = publish; }
    internal void Append(byte[] bytes, int offset, int count)
    {
        if ((_total += count) > 32 * 1024 * 1024) throw new TtsStreamException("tts_response_too_large");
        while (count > 0)
        {
            int target = _started ? _rate * 4 : _rate;
            int oldLength = (int)_pending.Length;
            int take = Math.Min(count, oldLength < target ? target - oldLength : 8192);
            _pending.Write(bytes, offset, take); offset += take; count -= take;
            if (!_audible)
            {
                byte[] buffer = _pending.GetBuffer();
                for (int i = oldLength & ~1; i + 1 < _pending.Length; i += 2)
                    if (buffer[i] != 0 || buffer[i + 1] != 0) { _audible = true; break; }
            }
            if (_pending.Length >= target && (_pending.Length & 1) == 0 && (_started || _audible)) Flush();
        }
    }
    private void Flush()
    {
        if (_pending.Length == 0) return;
        byte[] bytes = _pending.ToArray(); _pending.SetLength(0); _pending.Position = 0;
        _publish(bytes, _rate, _samples); _started = true;
    }
    internal void Complete()
    {
        if ((_total & 1) != 0) throw new TtsStreamException("tts_audio_pcm_invalid");
        if (!_audible) throw new TtsStreamException(_total == 0 ? "tts_audio_empty" : "tts_audio_silent");
        Flush();
    }
    public void Dispose() => _pending.Dispose();
}

internal static class TtsWaveStream
{
    internal static async Task ReadAsync(Stream stream, Action<byte[], int, long?> publish, CancellationToken cancel)
    {
        int wire = 0;
        async Task<byte[]> Read(int count)
        {
            byte[] bytes = new byte[count]; int offset = 0;
            while (offset < count)
            {
                int n = await stream.ReadAsync(bytes, offset, count - offset, cancel).ConfigureAwait(false);
                if (n == 0) throw new TtsStreamException("tts_openai_audio_invalid");
                offset += n; wire += n;
                if (wire > 32 * 1024 * 1024) throw new TtsStreamException("tts_response_too_large");
            }
            return bytes;
        }
        byte[] header = await Read(12).ConfigureAwait(false);
        if (Encoding.ASCII.GetString(header, 0, 4) != "RIFF" || Encoding.ASCII.GetString(header, 8, 4) != "WAVE")
            throw new TtsStreamException("tts_openai_audio_invalid");
        int channels = 0, rate = 0;
        for (int chunks = 0; chunks < 128; chunks++)
        {
            byte[] chunk = await Read(8).ConfigureAwait(false);
            string kind = Encoding.ASCII.GetString(chunk, 0, 4);
            uint length = BitConverter.ToUInt32(chunk, 4);
            if (kind == "data")
            {
                if (channels == 0 || (length != uint.MaxValue && length % (channels * 2) != 0)) throw new TtsStreamException("tts_openai_audio_invalid");
                // Empty/sentinel lengths occur in live WAV streams; HTTP EOF ends those streams.
                bool unknown = length == 0 || length == uint.MaxValue;
                using var output = new TtsPcmSegmenter(rate, unknown ? (long?)null : length / (channels * 2), publish);
                byte[] input = new byte[8192]; byte[] mono = new byte[8192]; byte[] frame = new byte[channels * 2];
                int pending = 0; long remaining = length;
                while (unknown || remaining > 0)
                {
                    int count = await stream.ReadAsync(input, 0, unknown ? input.Length : (int)Math.Min(input.Length, remaining), cancel).ConfigureAwait(false);
                    if (count == 0) { if (!unknown && remaining != 0) throw new TtsStreamException("tts_openai_audio_invalid"); break; }
                    wire += count; remaining -= count;
                    if (wire > 32 * 1024 * 1024) throw new TtsStreamException("tts_response_too_large");
                    int written = 0;
                    for (int i = 0; i < count; i++)
                    {
                        frame[pending++] = input[i];
                        if (pending != frame.Length) continue;
                        int value = BitConverter.ToInt16(frame, 0);
                        if (channels == 2) value = (value + BitConverter.ToInt16(frame, 2)) / 2;
                        mono[written++] = (byte)value; mono[written++] = (byte)(value >> 8); pending = 0;
                    }
                    output.Append(mono, 0, written);
                }
                if (pending != 0) throw new TtsStreamException("tts_openai_audio_invalid");
                output.Complete(); return;
            }
            if (length > 65536) throw new TtsStreamException("tts_openai_audio_invalid");
            byte[] content = await Read((int)length + (int)(length & 1)).ConfigureAwait(false);
            if (kind != "fmt ") continue;
            if (channels != 0 || length < 16 || BitConverter.ToUInt16(content, 0) != 1 || BitConverter.ToUInt16(content, 14) != 16)
                throw new TtsStreamException("tts_openai_audio_invalid");
            channels = BitConverter.ToUInt16(content, 2); rate = BitConverter.ToInt32(content, 4);
            if (channels < 1 || channels > 2 || rate < 8000 || rate > 48000 || BitConverter.ToUInt16(content, 12) != channels * 2 || BitConverter.ToInt32(content, 8) != rate * channels * 2)
                throw new TtsStreamException("tts_openai_audio_invalid");
        }
        throw new TtsStreamException("tts_openai_audio_invalid");
    }
}
