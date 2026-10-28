using AnimusForge;
using Newtonsoft.Json;

internal static class WeightedCountrySelectionReplay
{
    internal static void Run()
    {
        for (int mode = 0; mode < 3; mode++)
        {
            var counts = Enumerable.Range(0, 100).GroupBy(x => DiplomacyCountryAdmission.SampleCount(mode, x))
                .ToDictionary(x => x.Key, x => x.Count());
            for (int size = 2; size <= 4; size++)
                Test.True(counts[size] == (size == mode + 2 ? 70 : 15), "exact configured probability bands");
        }
        var input = new DiplomacyAdmissionInput { Direct = new() { "a" }, Candidates = new() { "a", "b", "c", "d", "e", "f" } };
        var sizes = new int[5]; var appearances = input.Candidates.ToDictionary(x => x, _ => 0);
        var casts = new HashSet<string>();
        for (int i = 0; i < 5000; i++)
        {
            var result = DiplomacyCountryAdmission.Calculate(input, 1, "selection-" + i);
            sizes[result.Admitted.Count]++;
            foreach (var id in result.Admitted) appearances[id]++;
            casts.Add(string.Join(",", result.Admitted.OrderBy(x => x)));
            Test.True(result.Admitted.Count is >= 2 and <= 4 && result.Admitted.Distinct().Count() == result.Admitted.Count
                && result.Admitted.Contains("a"), "zero-score pool always fills a unique cast and preserves the sole direct party");
            if (i < 20)
            {
                var loaded = JsonConvert.DeserializeObject<DiplomacyAdmissionResult>(JsonConvert.SerializeObject(result))!;
                var repeat = DiplomacyCountryAdmission.Calculate(loaded.Input, loaded.Preference, loaded.DrawSeed);
                Test.True(repeat.Admitted.SequenceEqual(result.Admitted) && repeat.SampledCount == result.SampledCount, "persisted seed replays deterministically");
            }
        }
        Test.True(sizes[3] is > 3300 and < 3700 && sizes[2] is > 600 and < 900 && sizes[4] is > 600 and < 900,
            "deterministic 5000-event sample reflects standard 70/15/15 distribution");
        Test.True(casts.Count > 20 && appearances.Where(x => x.Key != "a").All(x => x.Value > 0 && x.Value < 5000),
            "zero-score countries all have a chance and none is mandatory");

        input.Pairs.Add(new() { Country = "b", Origin = "a", Border = true, War = true });
        int favored = 0, neutral = 0;
        for (int i = 0; i < 5000; i++)
        {
            var result = DiplomacyCountryAdmission.Calculate(input, 0, "weighted-" + i);
            if (result.Admitted.Contains("b")) favored++;
            if (result.Admitted.Contains("c")) neutral++;
        }
        Test.True(favored > neutral * 2 && favored < 5000 && neutral > 0, "relations improve odds without automatic admission or zero-weight exclusion");
        input.Candidates.Reverse();
        var first = DiplomacyCountryAdmission.Calculate(input, 1, "order"); input.Candidates.Reverse();
        Test.True(first.Admitted.SequenceEqual(DiplomacyCountryAdmission.Calculate(input, 1, "order").Admitted), "candidate enumeration order does not alter the draw");
        for (int available = 1; available <= 6; available++)
        {
            var pool = new DiplomacyAdmissionInput { Direct = new() { "a" }, Candidates = input.Candidates.Take(available).ToList() };
            var result = DiplomacyCountryAdmission.Calculate(pool, 2, "capacity");
            Test.True(result.TargetCount == Math.Min(available, result.SampledCount), "small legal pools are capped without phantom countries");
            pool.Direct = pool.Candidates.ToList();
            result = DiplomacyCountryAdmission.Calculate(pool, 0, "direct");
            Test.True(result.Admitted.Count == available, "all direct parties retained even above sampled count");
        }
        Console.WriteLine($"Weighted selection: 5000 zero-score events sizes 2/3/4={sizes[2]}/{sizes[3]}/{sizes[4]}; weighted inclusion={favored}, neutral={neutral}.");
    }
}
