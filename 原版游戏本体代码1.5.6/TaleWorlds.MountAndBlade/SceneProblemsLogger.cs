using System;
using System.IO;
using System.Text;
using TaleWorlds.Engine;

public class SceneProblemsLogger
{
	private string _logPath = "";

	private string _fileName = "";

	public SceneProblemsLogger(string fileName)
	{
		_logPath = Utilities.GetLocalOutputPath() + "logs/";
		if (!Directory.Exists(_logPath))
		{
			Directory.CreateDirectory(_logPath);
		}
		int num = fileName.LastIndexOf('.');
		if (num != -1)
		{
			fileName.Substring(num);
		}
		else
		{
			fileName += ".txt";
		}
		_fileName = fileName;
		MBDebug.Print("Log file creating.. (" + _logPath + fileName + ")");
		string text = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");
		string contents = "==================== Scene Problems Log ====================" + Environment.NewLine + "Created : " + text + Environment.NewLine + "============================================================" + Environment.NewLine;
		File.WriteAllText(_logPath + fileName, contents, Encoding.UTF8);
	}

	public void LogScene(int sceneIndex, string sceneId, string log)
	{
		string text = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss");
		string contents = Environment.NewLine + "------------------------------------------------------------" + Environment.NewLine + $"Index   : {sceneIndex}" + Environment.NewLine + "Scene   : " + sceneId + Environment.NewLine + "Logged  : " + text + Environment.NewLine + "------------------------------------------------------------" + Environment.NewLine + log.TrimEnd(Array.Empty<char>()) + Environment.NewLine;
		File.AppendAllText(_logPath + _fileName, contents, Encoding.UTF8);
	}

	public void FinishLogging()
	{
		string contents = Environment.NewLine + "============================================================";
		File.AppendAllText(_logPath + _fileName, contents);
	}
}
