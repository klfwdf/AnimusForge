using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    // Input aliases are bilingual compatibility data, not translated output. A nested type
    // owns initialization so partial-class field order cannot expose an uninitialized cache.
    private static class GeneratedRpVocabulary
    {
        private const string ResourceName = "AnimusForge.Defaults.GeneratedRpVocabulary.json";
        private static readonly VocabularyData Data = Load();

        private sealed class VocabularyData
        {
            public int SchemaVersion;
            public Dictionary<string, string[]> StringLists;
            public SuffixData[] EquipmentSuffixRules;
            public SuffixData[] FoodSuffixRules;
        }

        private sealed class SuffixData
        {
            public string Kind;
            public bool RequiresEnglishWordBoundary;
            public string[] Suffixes;
        }

        private static VocabularyData Load()
        {
            using (Stream stream = typeof(RewardSystemBehavior).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null) throw new InvalidDataException("Missing generated-item vocabulary resource: " + ResourceName);
                using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
                {
                    var data = JsonConvert.DeserializeObject<VocabularyData>(reader.ReadToEnd(),
                        new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 16 });
                    if (data == null || data.SchemaVersion != 1 || data.StringLists == null ||
                        data.EquipmentSuffixRules == null || data.EquipmentSuffixRules.Length == 0 ||
                        data.FoodSuffixRules == null || data.FoodSuffixRules.Length == 0)
                        throw new InvalidDataException("Invalid generated-item vocabulary schema.");
                    foreach (var entry in data.StringLists) Validate(entry.Value, entry.Key);
                    return data;
                }
            }
        }

        private static void Validate(string[] values, string key)
        {
            if (values == null || values.Length == 0)
                throw new InvalidDataException("Empty generated-item vocabulary: " + key);
            foreach (string value in values)
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException("Empty generated-item alias: " + key);
        }

        public static string[] Strings(string key)
        {
            if (!Data.StringLists.TryGetValue(key, out string[] values))
                throw new InvalidDataException("Unknown generated-item vocabulary: " + key);
            return values;
        }

        public static GeneratedRpEquipmentSuffixRule[] EquipmentRules()
        {
            var rules = new GeneratedRpEquipmentSuffixRule[Data.EquipmentSuffixRules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                SuffixData row = Data.EquipmentSuffixRules[i];
                if (row == null || !Enum.TryParse(row.Kind, out GeneratedRpEquipmentKind kind) ||
                    kind == GeneratedRpEquipmentKind.None || !Enum.IsDefined(typeof(GeneratedRpEquipmentKind), kind))
                    throw new InvalidDataException("Invalid equipment suffix kind.");
                Validate(row.Suffixes, row.Kind);
                rules[i] = new GeneratedRpEquipmentSuffixRule(kind, row.RequiresEnglishWordBoundary, row.Suffixes);
            }
            return rules;
        }

        public static GeneratedRpFoodSuffixRule[] FoodRules()
        {
            var rules = new GeneratedRpFoodSuffixRule[Data.FoodSuffixRules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                SuffixData row = Data.FoodSuffixRules[i];
                if (row == null || !Enum.TryParse(row.Kind, out GeneratedRpFoodKind kind) ||
                    kind == GeneratedRpFoodKind.None || !Enum.IsDefined(typeof(GeneratedRpFoodKind), kind))
                    throw new InvalidDataException("Invalid food suffix kind.");
                Validate(row.Suffixes, row.Kind);
                rules[i] = new GeneratedRpFoodSuffixRule(kind, row.RequiresEnglishWordBoundary, row.Suffixes);
            }
            return rules;
        }
    }
}
