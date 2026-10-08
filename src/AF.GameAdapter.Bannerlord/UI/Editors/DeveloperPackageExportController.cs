using System;
using System.IO;
using TaleWorlds.Library;

namespace AnimusForge;

// Original package coordination only; domain adapters own file/data preparation.
internal sealed class DeveloperPackageExportController
{
 private readonly PersonaProfileImportExportAdapter _persona;
 private readonly MemoryHistoryImportExportAdapter _memory;
 private readonly DebtImportExportAdapter _debt;
 private readonly VoicePersonaImportExportAdapter _voice;
 private readonly KnowledgeImportExportAdapter _knowledge;
 private readonly WeeklyEventImportExportAdapter _weekly;
 internal DeveloperPackageExportController(PersonaProfileImportExportAdapter persona, MemoryHistoryImportExportAdapter memory,
  DebtImportExportAdapter debt, VoicePersonaImportExportAdapter voice, KnowledgeImportExportAdapter knowledge,
  WeeklyEventImportExportAdapter weekly)
 { _persona=persona; _memory=memory; _debt=debt; _voice=voice; _knowledge=knowledge; _weekly=weekly; }
	internal void ExportAllData(string folderName)
	{
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			_persona.ExportPersonaToDirectory(text, lookup);
			_memory.ExportRawHistoryToDirectory(text, lookup);
			_debt.ExportDebtToDirectory(text, lookup);
			if (!_knowledge.TryExportKnowledgeToDir(text, out var exportedKnowledgeCount, out var knowledgeExportError))
			{
				export.RestoreSubdirectory("knowledge");
				InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导出失败，已保留旧导出。原因：" + knowledgeExportError));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("Knowledge 已导出 " + exportedKnowledgeCount + " 条。"));
			}
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			_voice.ExportVoiceMappingToDirectory(text);
			_weekly.ExportEventDataToDir(text);
			if (KingdomStrategicProfileBehavior.Instance != null && !KingdomStrategicProfileBehavior.Instance.ExportAllToDirectory(text, out var kingdomProfileExportMessage))
			{
				export.RestoreSubdirectory("kingdom_profiles");
				InformationManager.DisplayMessage(new InformationMessage("警告：国家战略与性格导出失败，已保留旧导出。原因：" + kingdomProfileExportMessage));
			}
			export.Publish();
			VoiceMapper.SetPreferredExportFolder(export.FinalPath);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ExportHeroNpcAllData(string folderName)
	{
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			_persona.ExportPersonaToDirectory(text, lookup);
			_memory.ExportCompressedMemoryToDirectory(text, lookup);
			_debt.ExportDebtToDirectory(text, lookup);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}
}
