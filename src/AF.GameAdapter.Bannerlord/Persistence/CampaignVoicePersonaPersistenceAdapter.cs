using System;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;

// Codec and legacy IO only; existing VoiceMapper/unnamed persona remain authoritative.
internal static class CampaignVoicePersonaPersistenceAdapter
{
    internal static void Save(IDataStore dataStore, ref string voiceJson, ref string voiceFolder, ref string unnamedJson, Func<string> exportVoice, Func<string> preferredFolder, Func<string> exportUnnamed)
    {
					try
					{
						voiceJson = exportVoice() ?? "";
				}
				catch (Exception ex7)
				{
					voiceJson = "";
					Logger.Log("VoiceMapper", "[ERROR] Serialize voice mapping for save failed: " + ex7.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_voiceMapping_v1", voiceJson, "VoiceMapper");
				voiceFolder = preferredFolder() ?? "";
				dataStore.SyncData("_voiceMapping_export_folder_v1", ref voiceFolder);
				try
				{
					unnamedJson = exportUnnamed() ?? "";
				}
				catch (Exception ex8)
				{
					unnamedJson = "";
					Logger.Log("UnnamedPersona", "[ERROR] Serialize unnamed persona for save failed: " + ex8.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_unnamed_persona_v1", unnamedJson, "UnnamedPersona");
    }

    internal static void Load(IDataStore dataStore, ref string voiceJson, ref string voiceFolder, ref string unnamedJson, Action<string> setPreferredFolder, Func<string, bool> importVoice, Action<string> importUnnamed)
    {
				voiceFolder = "";
			dataStore.SyncData("_voiceMapping_export_folder_v1", ref voiceFolder);
			setPreferredFolder(voiceFolder);
			voiceJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_voiceMapping_v1", "VoiceMapper");
			if (!string.IsNullOrWhiteSpace(voiceJson))
			{
				try
				{
					if (!importVoice(voiceJson))
					{
						Logger.Log("VoiceMapper", "[WARN] Save-loaded voice mapping was invalid; kept current file-backed mapping.");
					}
				}
				catch (Exception ex8)
				{
					Logger.Log("VoiceMapper", "[ERROR] Restore voice mapping from save failed: " + ex8.Message);
				}
			}
			unnamedJson = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_unnamed_persona_v1", "UnnamedPersona");
			try
			{
				importUnnamed(unnamedJson);
			}
			catch (Exception ex9)
			{
				Logger.Log("UnnamedPersona", "[ERROR] Restore unnamed persona from save failed: " + ex9.Message);
			}
    }
}
