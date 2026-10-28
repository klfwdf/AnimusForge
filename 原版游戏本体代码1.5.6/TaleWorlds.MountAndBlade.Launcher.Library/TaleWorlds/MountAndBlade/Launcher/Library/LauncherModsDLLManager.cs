using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherModsDLLManager
{
	private Dictionary<SubModuleInfo, LauncherDLLData> _subModulesWithDLLs;

	private UserData _userData;

	public bool ShouldUpdateSaveData { get; private set; }

	public LauncherModsDLLManager(UserData userData, List<SubModuleInfo> allSubmodules)
	{
		TaleWorlds.Library.Debug.Print("Init LauncherModsDLLManager");
		_userData = userData;
		_subModulesWithDLLs = new Dictionary<SubModuleInfo, LauncherDLLData>();
		List<SubModuleInfo> list = new List<SubModuleInfo>();
		for (int i = 0; i < allSubmodules.Count; i++)
		{
			SubModuleInfo subModuleInfo = allSubmodules[i];
			if (subModuleInfo.DLLExists && !subModuleInfo.IsTWCertifiedDLL)
			{
				uint num = (uint)new FileInfo(subModuleInfo.DLLPath).Length;
				uint? dLLLatestSizeInBytes = userData.GetDLLLatestSizeInBytes(subModuleInfo.DLLName);
				_subModulesWithDLLs.Add(subModuleInfo, new LauncherDLLData(subModuleInfo, isDangerous: false, "", num));
				if (!dLLLatestSizeInBytes.HasValue || dLLLatestSizeInBytes != num)
				{
					TaleWorlds.Library.Debug.Print("Need to verify: " + subModuleInfo.DLLName);
					list.Add(subModuleInfo);
				}
				else
				{
					_subModulesWithDLLs[subModuleInfo].SetIsDLLDangerous(userData.GetDLLLatestIsDangerous(subModuleInfo.DLLName));
					_subModulesWithDLLs[subModuleInfo].SetDLLVerifyInformation(userData.GetDLLLatestVerifyInformation(subModuleInfo.DLLName));
				}
				_subModulesWithDLLs[subModuleInfo].SetDLLSize(num);
			}
		}
		VerifySubModules(list);
		UpdateUserDataLatestValues();
		ShouldUpdateSaveData = list.Count > 0;
	}

	private void UpdateUserDataLatestValues()
	{
		foreach (KeyValuePair<SubModuleInfo, LauncherDLLData> subModulesWithDLL in _subModulesWithDLLs)
		{
			_userData.SetDLLLatestIsDangerous(subModulesWithDLL.Key.DLLName, subModulesWithDLL.Value.IsDangerous);
			_userData.SetDLLLatestSizeInBytes(subModulesWithDLL.Key.DLLName, subModulesWithDLL.Value.Size);
			_userData.SetDLLLatestVerifyInformation(subModulesWithDLL.Key.DLLName, subModulesWithDLL.Value.VerifyInformation);
		}
	}

	private void VerifySubModules(List<SubModuleInfo> subModulesToVerify)
	{
		if (subModulesToVerify.Count <= 0)
		{
			return;
		}
		ResultData dLLVerifyReport = GetDLLVerifyReport(subModulesToVerify.Select((SubModuleInfo s) => s.DLLPath).ToArray());
		if (dLLVerifyReport != null)
		{
			int i;
			for (i = 0; i < subModulesToVerify.Count; i++)
			{
				DLLResult dLLResult = dLLVerifyReport.DLLs.FirstOrDefault((DLLResult r) => r.DLLName == subModulesToVerify[i].DLLPath);
				_subModulesWithDLLs[subModulesToVerify[i]].SetIsDLLDangerous(!dLLResult.IsSafe);
				_subModulesWithDLLs[subModulesToVerify[i]].SetDLLVerifyInformation(dLLResult.Information);
				TaleWorlds.Library.Debug.Print(dLLResult.Information);
			}
		}
		else
		{
			for (int num = 0; num < subModulesToVerify.Count; num++)
			{
				_subModulesWithDLLs[subModulesToVerify[num]].SetDLLSize(0u);
				_subModulesWithDLLs[subModulesToVerify[num]].SetIsDLLDangerous(isDangerous: true);
			}
		}
	}

	private ResultData GetDLLVerifyReport(string[] dlls)
	{
		try
		{
			new List<bool>();
			string text = "";
			string text2 = "";
			TaleWorlds.Library.Debug.Print("Starting verifying DLLs");
			Process obj = new Process
			{
				StartInfo = 
				{
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "..\\ModVerifier\\ModVerifier.exe",
					Arguments = string.Join(" ", dlls.Where((string c) => !string.IsNullOrEmpty(c))) + " -ld1",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};
			StringBuilder outputString = new StringBuilder();
			StringBuilder errorString = new StringBuilder();
			obj.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					outputString.AppendLine(e.Data);
				}
			};
			obj.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					errorString.AppendLine(e.Data);
				}
			};
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			obj.Start();
			obj.BeginOutputReadLine();
			obj.BeginErrorReadLine();
			obj.WaitForExit();
			stopwatch.Stop();
			TaleWorlds.Library.Debug.Print(((float)stopwatch.ElapsedMilliseconds / 1000f).ToString("0.0000") + " seconds");
			text += outputString;
			text2 += errorString;
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(ResultData));
				object obj2;
				using (TextReader textReader = new System.IO.StringReader(text))
				{
					obj2 = xmlSerializer.Deserialize(textReader);
				}
				return obj2 as ResultData;
			}
			catch (Exception)
			{
				TaleWorlds.Library.Debug.Print("Error while verifying DLLs");
				TaleWorlds.Library.Debug.Print("Verify Tool output:");
				TaleWorlds.Library.Debug.Print(text);
				TaleWorlds.Library.Debug.Print("Verify Tool error output:");
				TaleWorlds.Library.Debug.Print(text2);
				return null;
			}
		}
		catch (Exception ex2)
		{
			TaleWorlds.Library.Debug.Print("Couldn't verify dlls");
			TaleWorlds.Library.Debug.Print(ex2.Message);
			TaleWorlds.Library.Debug.Print(ex2.StackTrace);
			return null;
		}
	}

	public LauncherDLLData GetSubModuleVerifyData(SubModuleInfo subModuleInfo)
	{
		if (_subModulesWithDLLs.ContainsKey(subModuleInfo))
		{
			return _subModulesWithDLLs[subModuleInfo];
		}
		return null;
	}
}
