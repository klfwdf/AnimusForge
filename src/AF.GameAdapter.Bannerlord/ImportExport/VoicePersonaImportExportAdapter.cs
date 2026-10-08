using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge;

// Original manual file flow; mutations remain with the existing domain authority.
internal sealed class VoicePersonaImportExportAdapter
{
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 private readonly Action<string> _persistVoiceJson;
 internal VoicePersonaImportExportAdapter(Action<string,string,Action,Action,Action> showDuplicate, Action<string> persistVoiceJson)
 { _showDuplicate=showDuplicate; _persistVoiceJson=persistVoiceJson; }
	internal void ExportVoiceMappingData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportVoiceMappingToDirectory(text);
			export.Publish();
			VoiceMapper.SetPreferredExportFolder(export.FinalPath);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportVoiceMappingData(string folderName) => ImportVoiceMappingDataScoped(folderName, null);

	internal void ImportVoiceMappingDataScoped(string folderName, OnboardingImportStep completion)
	{
        if (completion != null && !completion.CanApply()) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			string text = null;
			string text2 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
			string text3 = Path.Combine(importDir, "VoiceMapping.json");
			if (File.Exists(text2))
			{
				text = text2;
			}
			else if (File.Exists(text3))
			{
				text = text3;
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 voice_mapping/VoiceMapping.json。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			string json = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(json))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：VoiceMapping.json 为空。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			bool flag = false;
			try
			{
				flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text);
				if (flag2)
				{
					_persistVoiceJson(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));

                completion?.Finish(flag2 ? OnboardingImportResult.Completed : OnboardingImportResult.Failed);
};
			Action onSkipDuplicates = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text, overwriteExisting: false);
				if (flag2)
				{
					_persistVoiceJson(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成（已合并）：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));

                completion?.Finish(flag2 ? OnboardingImportResult.Completed : OnboardingImportResult.Failed);
};
            if (completion != null) { action = completion.Guard(action); onSkipDuplicates = completion.Guard(onSkipDuplicates); }
			if (flag)
			{
				completion?.Pending();
				_showDuplicate("检测到重复 - VoiceMapping", "当前已存在声音映射。\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
                    if (completion != null && !completion.CanApply()) return;
                    completion?.Finish(OnboardingImportResult.Cancelled);
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
            completion?.Finish(OnboardingImportResult.Failed);
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	internal void ExportVoiceMappingToDirectory(string exportDir)
	{
		string text = exportDir;
			string text2 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			string path2 = Path.Combine(text2, "VoiceMapping.json");
			string text3 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "{}";
			}
			File.WriteAllText(path2, text3, Encoding.UTF8);
	}

 internal void PrepareVoiceMapping(string importDir, out string vmJson, out string vmPath, out int num11, out int num12)
 {
 vmJson=null; vmPath=null; num11=0; num12=0;
			try
			{
				string path6 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
				if (!File.Exists(path6))
				{
					path6 = Path.Combine(importDir, "VoiceMapping.json");
				}
				if (File.Exists(path6))
				{
					string text11 = File.ReadAllText(path6, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(text11))
					{
						vmPath = path6;
						vmJson = text11;
						num12 = 1;
						bool flag = false;
						try
						{
							flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
						}
						catch
						{
							flag = false;
						}
						if (flag)
						{
							num11 = 1;
						}
					}
				}
			}
			catch
			{
				num11 = 0;
				num12 = 0;
				vmJson = null;
			}
 }

 internal void ApplyPreparedVoice(string vmJson,string vmPath,bool overwrite,Action refreshVoiceStorage)
 {
bool flag3 = true;
				if (!string.IsNullOrWhiteSpace(vmJson))
				{
					flag3 = ((!string.IsNullOrWhiteSpace(vmPath) && File.Exists(vmPath)) ? VoiceMapper.ImportMappingFromFile(vmPath, overwriteExisting: overwrite) : VoiceMapper.ImportMappingJson(vmJson, overwriteExisting: overwrite));
					if (flag3)
					{
						refreshVoiceStorage();
					}
				}
				if (!flag3)
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
 }

internal int CountVoiceMappingForDev()
	{
		try
		{
			return VoiceMapper.GetTotalVoiceCount();
		}
		catch
		{
			return 0;
		}
	}
	internal void ClearVoiceMappingDataForCurrentSave()
	{
		try
		{
			VoiceMapper.ImportMappingJson("{\"male_young\":[],\"male_middle\":[],\"male_old\":[],\"female_young\":[],\"female_middle\":[],\"female_old\":[],\"fallback\":\"\"}", overwriteExisting: true, saveToFile: false);
			_persistVoiceJson(VoiceMapper.ExportMappingJson(pretty: false) ?? "");
		}
		catch (Exception ex)
		{
			_persistVoiceJson("");
			Logger.Log("DevDataManagement", "[WARN] Clear VoiceMapping failed: " + ex.Message);
		}
	}

}
