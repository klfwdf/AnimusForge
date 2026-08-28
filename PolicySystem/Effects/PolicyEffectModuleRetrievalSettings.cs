using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

internal enum PolicyEffectRetrievalContext
{
	PlayerKingdom,
	PlayerLocal,
	NpcRulerKingdom,
	PlayerVassal
}

internal sealed class PolicyEffectModuleRetrievalState
{
	[JsonProperty("PlayerPolicyEnabled")]
	internal bool PlayerPolicyEnabled { get; set; } = true;

	[JsonProperty("LocalPolicyEnabled")]
	internal bool LocalPolicyEnabled { get; set; } = true;

	[JsonProperty("RulerPolicyEnabled")]
	internal bool RulerPolicyEnabled { get; set; } = true;

	[JsonProperty("VassalPolicyEnabled")]
	internal bool VassalPolicyEnabled { get; set; } = true;

	internal PolicyEffectModuleRetrievalState Clone()
	{
		return new PolicyEffectModuleRetrievalState
		{
			PlayerPolicyEnabled = PlayerPolicyEnabled,
			LocalPolicyEnabled = LocalPolicyEnabled,
			RulerPolicyEnabled = RulerPolicyEnabled,
			VassalPolicyEnabled = VassalPolicyEnabled
		};
	}

	internal bool IsEnabled(PolicyEffectRetrievalContext context)
	{
		switch (context)
		{
			case PolicyEffectRetrievalContext.PlayerKingdom:
				return PlayerPolicyEnabled;
			case PolicyEffectRetrievalContext.PlayerLocal:
				return LocalPolicyEnabled;
			case PolicyEffectRetrievalContext.NpcRulerKingdom:
				return RulerPolicyEnabled;
			case PolicyEffectRetrievalContext.PlayerVassal:
				return VassalPolicyEnabled;
			default:
				return false;
		}
	}
}

internal static class PolicyEffectModuleRetrievalSettings
{
	internal const string FileName = "PolicyEffectModuleRetrievalSettings.json";

	private const int CurrentVersion = 1;
	private const long MaxJsonBytes = 262144L;
	private static readonly object Sync = new object();
	private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
	private static readonly Encoding WriteUtf8 = new UTF8Encoding(false);
	private static RetrievalSnapshot _snapshot;
	private static string _storageDirectoryOverride;

	private sealed class RetrievalSnapshot
	{
		internal RetrievalSnapshot(
			Dictionary<string, PolicyEffectModuleRetrievalState> states,
			Dictionary<string, JToken> rawModuleDocuments,
			Dictionary<PolicyEffectRetrievalContext, IReadOnlyList<IPolicyEffectModule>> enabledModules)
		{
			States = states;
			RawModuleDocuments = rawModuleDocuments;
			EnabledModules = enabledModules;
		}

		internal Dictionary<string, PolicyEffectModuleRetrievalState> States { get; }

		internal Dictionary<string, JToken> RawModuleDocuments { get; }

		internal Dictionary<PolicyEffectRetrievalContext, IReadOnlyList<IPolicyEffectModule>> EnabledModules { get; }
	}

	internal static IReadOnlyList<IPolicyEffectModule> GetEnabledModules(PolicyEffectRetrievalContext context)
	{
		RetrievalSnapshot snapshot = GetSnapshot();
		return snapshot.EnabledModules.TryGetValue(context, out IReadOnlyList<IPolicyEffectModule> modules)
			? modules
			: Array.Empty<IPolicyEffectModule>();
	}

	internal static Dictionary<string, PolicyEffectModuleRetrievalState> CreateEditableStateSnapshot()
	{
		RetrievalSnapshot snapshot = GetSnapshot();
		return PolicyEffectModuleCatalog.Modules.ToDictionary(
			module => module.Id,
			module => snapshot.States.TryGetValue(module.Id, out PolicyEffectModuleRetrievalState state)
				? state.Clone()
				: CreateDefaultState(module),
			StringComparer.Ordinal);
	}

	internal static bool IsContextSupported(IPolicyEffectModule module, PolicyEffectRetrievalContext context)
	{
		return module?.Descriptor?.PromptVisible == true
			&& PolicyEffectModuleCatalog.IsAllowedForScope(module, GetScope(context));
	}

	internal static string GetScope(PolicyEffectRetrievalContext context)
	{
		switch (context)
		{
			case PolicyEffectRetrievalContext.PlayerLocal:
				return PolicyEffectScopes.Local;
			case PolicyEffectRetrievalContext.PlayerVassal:
				return PolicyEffectScopes.Vassal;
			case PolicyEffectRetrievalContext.PlayerKingdom:
			case PolicyEffectRetrievalContext.NpcRulerKingdom:
			default:
				return PolicyEffectScopes.Kingdom;
		}
	}

	internal static bool TrySave(
		IReadOnlyDictionary<string, PolicyEffectModuleRetrievalState> editedStates,
		out string error)
	{
		error = string.Empty;
		try
		{
			lock (Sync)
			{
				RetrievalSnapshot current = _snapshot ?? LoadSnapshot();
				Dictionary<string, PolicyEffectModuleRetrievalState> normalizedStates
					= new Dictionary<string, PolicyEffectModuleRetrievalState>(StringComparer.Ordinal);
				JObject moduleDocuments = new JObject();
				foreach (KeyValuePair<string, JToken> raw in current.RawModuleDocuments)
				{
					moduleDocuments[raw.Key] = raw.Value?.DeepClone();
				}
				foreach (IPolicyEffectModule module in PolicyEffectModuleCatalog.Modules)
				{
					PolicyEffectModuleRetrievalState state = editedStates != null
						&& editedStates.TryGetValue(module.Id, out PolicyEffectModuleRetrievalState edited)
						&& edited != null
						? edited.Clone()
						: current.States.TryGetValue(module.Id, out PolicyEffectModuleRetrievalState existing)
							? existing.Clone()
							: CreateDefaultState(module);
					NormalizeUnsupportedContexts(module, state);
					normalizedStates[module.Id] = state;
					moduleDocuments[module.Id] = BuildStateDocument(state);
				}

				JObject document = new JObject
				{
					["Version"] = CurrentVersion,
					["Modules"] = moduleDocuments
				};
				WriteJsonAtomically(GetSettingsPath(requireDirectory: true), document);
				RetrievalSnapshot replacement = CreateSnapshot(normalizedStates, ReadRawModuleDocuments(moduleDocuments));
				Volatile.Write(ref _snapshot, replacement);
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			PolicySystemLog.Failure("Effect", "policy-effect-retrieval-settings-save-failed", ex.Message, ex.ToString());
			return false;
		}
	}

	internal static void SetStorageDirectoryOverrideForContractTests(string directory)
	{
		lock (Sync)
		{
			_storageDirectoryOverride = string.IsNullOrWhiteSpace(directory) ? null : directory;
			Volatile.Write(ref _snapshot, null);
		}
	}

	internal static void ReloadForContractTests()
	{
		lock (Sync)
		{
			Volatile.Write(ref _snapshot, LoadSnapshot());
		}
	}

	private static RetrievalSnapshot GetSnapshot()
	{
		RetrievalSnapshot snapshot = Volatile.Read(ref _snapshot);
		if (snapshot != null)
		{
			return snapshot;
		}
		lock (Sync)
		{
			snapshot = _snapshot;
			if (snapshot == null)
			{
				snapshot = LoadSnapshot();
				Volatile.Write(ref _snapshot, snapshot);
			}
			return snapshot;
		}
	}

	private static RetrievalSnapshot LoadSnapshot()
	{
		Dictionary<string, PolicyEffectModuleRetrievalState> states
			= new Dictionary<string, PolicyEffectModuleRetrievalState>(StringComparer.Ordinal);
		Dictionary<string, JToken> rawDocuments = new Dictionary<string, JToken>(StringComparer.Ordinal);
		string path = GetSettingsPath(requireDirectory: false);
		if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
		{
			try
			{
				FileInfo info = new FileInfo(path);
				if (info.Length < 0 || info.Length > MaxJsonBytes)
				{
					throw new InvalidOperationException("政策效果模块检索设置文件大小无效。");
				}
				JObject document = JObject.Parse(File.ReadAllText(path, StrictUtf8));
				if (document.Value<int?>("Version") != CurrentVersion || document["Modules"] is not JObject modules)
				{
					throw new InvalidOperationException("政策效果模块检索设置版本或 Modules 结构无效。");
				}
				rawDocuments = ReadRawModuleDocuments(modules);
				foreach (IPolicyEffectModule module in PolicyEffectModuleCatalog.Modules)
				{
					PolicyEffectModuleRetrievalState state = modules[module.Id] is JObject stateDocument
						? ReadState(stateDocument)
						: CreateDefaultState(module);
					NormalizeUnsupportedContexts(module, state);
					states[module.Id] = state;
				}
			}
			catch (Exception ex)
			{
				PolicySystemLog.Failure("Effect", "policy-effect-retrieval-settings-invalid", ex.Message, ex.ToString());
				states.Clear();
				rawDocuments.Clear();
			}
		}
		foreach (IPolicyEffectModule module in PolicyEffectModuleCatalog.Modules)
		{
			if (!states.ContainsKey(module.Id))
			{
				states[module.Id] = CreateDefaultState(module);
			}
		}
		return CreateSnapshot(states, rawDocuments);
	}

	private static RetrievalSnapshot CreateSnapshot(
		Dictionary<string, PolicyEffectModuleRetrievalState> states,
		Dictionary<string, JToken> rawDocuments)
	{
		Dictionary<string, PolicyEffectModuleRetrievalState> frozenStates = states.ToDictionary(
			pair => pair.Key,
			pair => pair.Value?.Clone() ?? new PolicyEffectModuleRetrievalState(),
			StringComparer.Ordinal);
		Dictionary<PolicyEffectRetrievalContext, IReadOnlyList<IPolicyEffectModule>> enabled
			= new Dictionary<PolicyEffectRetrievalContext, IReadOnlyList<IPolicyEffectModule>>();
		foreach (PolicyEffectRetrievalContext context in Enum.GetValues(typeof(PolicyEffectRetrievalContext)))
		{
			enabled[context] = Array.AsReadOnly(PolicyEffectModuleCatalog.Modules
				.Where(module => IsContextSupported(module, context)
					&& frozenStates.TryGetValue(module.Id, out PolicyEffectModuleRetrievalState state)
					&& state.IsEnabled(context))
				.ToArray());
		}
		return new RetrievalSnapshot(
			frozenStates,
			rawDocuments.ToDictionary(pair => pair.Key, pair => pair.Value?.DeepClone(), StringComparer.Ordinal),
			enabled);
	}

	private static PolicyEffectModuleRetrievalState CreateDefaultState(IPolicyEffectModule module)
	{
		PolicyEffectModuleRetrievalState state = new PolicyEffectModuleRetrievalState();
		NormalizeUnsupportedContexts(module, state);
		return state;
	}

	private static void NormalizeUnsupportedContexts(IPolicyEffectModule module, PolicyEffectModuleRetrievalState state)
	{
		if (state == null)
		{
			return;
		}
		if (!IsContextSupported(module, PolicyEffectRetrievalContext.PlayerKingdom))
		{
			state.PlayerPolicyEnabled = false;
		}
		if (!IsContextSupported(module, PolicyEffectRetrievalContext.PlayerLocal))
		{
			state.LocalPolicyEnabled = false;
		}
		if (!IsContextSupported(module, PolicyEffectRetrievalContext.NpcRulerKingdom))
		{
			state.RulerPolicyEnabled = false;
		}
		if (!IsContextSupported(module, PolicyEffectRetrievalContext.PlayerVassal))
		{
			state.VassalPolicyEnabled = false;
		}
	}

	private static PolicyEffectModuleRetrievalState ReadState(JObject document)
	{
		return new PolicyEffectModuleRetrievalState
		{
			PlayerPolicyEnabled = ReadBoolean(document, "PlayerPolicyEnabled"),
			LocalPolicyEnabled = ReadBoolean(document, "LocalPolicyEnabled"),
			RulerPolicyEnabled = ReadBoolean(document, "RulerPolicyEnabled"),
			VassalPolicyEnabled = ReadBoolean(document, "VassalPolicyEnabled")
		};
	}

	private static bool ReadBoolean(JObject document, string name)
	{
		JToken token = document?[name];
		return token == null || token.Type == JTokenType.Null
			? true
			: token.Type == JTokenType.Boolean && token.Value<bool>();
	}

	private static JObject BuildStateDocument(PolicyEffectModuleRetrievalState state)
	{
		PolicyEffectModuleRetrievalState value = state ?? new PolicyEffectModuleRetrievalState();
		return new JObject
		{
			["PlayerPolicyEnabled"] = value.PlayerPolicyEnabled,
			["LocalPolicyEnabled"] = value.LocalPolicyEnabled,
			["RulerPolicyEnabled"] = value.RulerPolicyEnabled,
			["VassalPolicyEnabled"] = value.VassalPolicyEnabled
		};
	}

	private static Dictionary<string, JToken> ReadRawModuleDocuments(JObject modules)
	{
		return (modules?.Properties() ?? Enumerable.Empty<JProperty>())
			.Where(property => !string.IsNullOrWhiteSpace(property.Name))
			.ToDictionary(
				property => property.Name,
				property => property.Value?.DeepClone(),
				StringComparer.Ordinal);
	}

	private static string GetSettingsPath(bool requireDirectory)
	{
		string directory = !string.IsNullOrWhiteSpace(_storageDirectoryOverride)
			? _storageDirectoryOverride
			: DuelSettings.GetCustomPromptTextStoreDirectoryForPolicyPrompts();
		if (string.IsNullOrWhiteSpace(directory))
		{
			if (requireDirectory)
			{
				throw new InvalidOperationException("无法定位政策效果模块检索设置目录。");
			}
			return string.Empty;
		}
		return Path.Combine(directory, FileName);
	}

	private static void WriteJsonAtomically(string path, JObject document)
	{
		string directory = Path.GetDirectoryName(path);
		if (string.IsNullOrWhiteSpace(directory))
		{
			throw new InvalidOperationException("政策效果模块检索设置目录为空。");
		}
		Directory.CreateDirectory(directory);
		string tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
		try
		{
			using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (StreamWriter writer = new StreamWriter(stream, WriteUtf8, 4096, leaveOpen: true))
			{
				writer.Write((document ?? new JObject()).ToString(Formatting.Indented));
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}
			JObject.Parse(File.ReadAllText(tempPath, StrictUtf8));
			if (File.Exists(path))
			{
				File.Replace(tempPath, path, null);
			}
			else
			{
				File.Move(tempPath, path);
			}
		}
		finally
		{
			try
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
			catch
			{
			}
		}
	}
}
