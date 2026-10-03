using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge;

// Versioned local pipe contract. Encoded inputs only; never sends prompts to a network service.
internal static class RerankerWire
{
    internal const int Version = 1;
    internal const int MaxRows = 1024;
    internal const int MaxTokens = 512;

    internal static void WriteRequest(BinaryWriter writer, List<List<long>> rows, List<int[]> masks)
    {
        Validate(rows, masks);
        writer.Write(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            writer.Write(rows[i].Count);
            for (int j = 0; j < rows[i].Count; j++) { writer.Write(rows[i][j]); writer.Write(masks[i][j]); }
        }
        writer.Flush();
    }

    internal static void ReadRequest(BinaryReader reader, out List<List<long>> rows, out List<int[]> masks)
    {
        int count = reader.ReadInt32();
        if (count < 1 || count > MaxRows) throw new InvalidDataException("Invalid reranker batch size.");
        rows = new List<List<long>>(count); masks = new List<int[]>(count);
        for (int i = 0; i < count; i++)
        {
            int length = reader.ReadInt32();
            if (length < 1 || length > MaxTokens) throw new InvalidDataException("Invalid reranker token count.");
            var row = new List<long>(length); var mask = new int[length];
            for (int j = 0; j < length; j++) { row.Add(reader.ReadInt64()); mask[j] = reader.ReadInt32(); }
            rows.Add(row); masks.Add(mask);
        }
        Validate(rows, masks);
    }

    internal static void Validate(List<List<long>> rows, List<int[]> masks)
    {
        if (rows == null || masks == null || rows.Count < 1 || rows.Count > MaxRows || masks.Count != rows.Count)
            throw new InvalidDataException("Invalid reranker batch size.");
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null || masks[i] == null || rows[i].Count < 1 || rows[i].Count > MaxTokens || masks[i].Length != rows[i].Count)
                throw new InvalidDataException("Invalid reranker token count.");
            for (int j = 0; j < rows[i].Count; j++)
                if (rows[i][j] < 0 || (masks[i][j] != 0 && masks[i][j] != 1))
                    throw new InvalidDataException("Invalid reranker token or mask.");
        }
    }

    internal static List<float> ReadScores(BinaryReader reader, int count)
    {
        if (reader.ReadInt32() != count) throw new InvalidDataException("Reranker score count mismatch.");
        var scores = new List<float>(count);
        for (int i = 0; i < count; i++)
        {
            float score = reader.ReadSingle();
            if (float.IsNaN(score) || float.IsInfinity(score) || score < 0f || score > 1f)
                throw new InvalidDataException("Invalid reranker score.");
            scores.Add(score);
        }
        return scores;
    }
}

internal interface IRerankerEncodedBackend : IDisposable
{
    bool TryScore(List<List<long>> rows, List<int[]> masks, out List<float> scores);
}
