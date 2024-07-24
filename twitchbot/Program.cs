using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace twitchbot;

public class Program
{
	public static string[] args;

	public static ChatRoom chat;

	public static Version version = new Version(1, 3, 72, 35);

	[MTAThread]
	public static void Main(string[] args)
	{
		ChatRoom.ConsoleLog($"twitchbot app and API, version {version}", ConsoleColor.Gray, TimeSpan.FromSeconds(1.0));
		Program.args = args;
		if (!args.Contains("--skip-update"))
		{
			if (CheckUpdate(out var latest))
			{
				ChatRoom.ConsoleLog("Restarting this program in 10 seconds...", ConsoleColor.Gray, TimeSpan.FromSeconds(10.0));
				Restart(latest);
				Process.GetCurrentProcess().Close();
				return;
			}
			if (latest != string.Empty)
			{
				ChatRoom.ConsoleLog("\nThis version is up-to-date", ConsoleColor.Green, TimeSpan.FromSeconds(3.0));
			}
			Console.Clear();
		}
		for (int i = 1; i < args.Length; i++)
		{
			if (args.Length >= i && args[i - 1] == "-help")
			{
				ChatRoom.HelpCmd = args[i];
			}
		}
		new ConsoleHandler().PluginInit(ChatRoom.PluginPath, args.Contains("--automated"));
		chat = new ChatRoom(new ChatStream(new IRC("irc.twitch.tv", 6667)));
		chat.Connect();
	}

	private static bool Restart(string latest)
	{
		string a = "";
		if (args != null && args.Length == 3)
		{
			for (int i = 0; i < args.Length; i++)
			{
				a = a + args[i] + " ";
			}
			a = a.TrimEnd(' ');
		}
		bool flag = false;
		try
		{
			return Process.Start(Environment.CurrentDirectory + "/" + latest + "/twitchbot.exe", a).Responding;
		}
		catch
		{
			ChatRoom.ConsoleLog("Restart application from new directory.", ConsoleColor.Green, TimeSpan.FromSeconds(10.0));
			return false;
		}
	}

	public static async Task<string> HttpGetString(string url)
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
		return await new HttpClient().GetStringAsync(url);
	}

	public static async Task WebClientGetFile(string url, string fileName)
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
		byte[] buffer = await new HttpClient().GetByteArrayAsync(url);
		using FileStream fs = new FileStream("./" + fileName, FileMode.Create, FileAccess.Write, FileShare.Write);
		fs.Write(buffer, 0, buffer.Length);
	}

	private static bool CheckUpdate(out string latest)
	{
		ChatRoom.ConsoleLog("Check for updates? y/N", ConsoleColor.Gray);
		latest = string.Empty;
		if (ReadKeyYesNo() == ConsoleKey.N)
		{
			ChatRoom.ConsoleLog("Skipping update...", ConsoleColor.Green, TimeSpan.FromSeconds(3.0));
			return false;
		}
		try
		{
			latest = HttpGetString("https://raw.githubusercontent.com/ReDuzed/Trotbot-API-Wiki/main/version").Result.TrimEnd('\n', '\r');
		}
		catch (Exception e)
		{
			ChatRoom.LogErrors(e, wait: true);
			ChatRoom.ConsoleLog("Could not check version; did not update.", ConsoleColor.Yellow, TimeSpan.FromSeconds(5.0));
			return false;
		}
		if (latest != version.ToString())
		{
			ChatRoom.ConsoleLog("\n" + ConsoleHandler.GetChangelog(), ConsoleColor.Gray);
			Console.Write("\n");
			ChatRoom.ConsoleLog("An update is available: version " + latest + "\nDownload? y/n");
			if (ReadKeyYesNo() != ConsoleKey.Y)
			{
				return false;
			}
			Console.Write("\n");
			if (File.Exists("/twitchbot-v" + latest + ".zip"))
			{
				ChatRoom.ConsoleLog("Latest version file already exists", ConsoleColor.Yellow);
			}
			else
			{
				string url = $"https://github.com/ReDuzed/Trotbot-API-Wiki/releases/download/{latest}/twitchbot-v{latest}.zip";
				try
				{
					using (StreamWriter sw = new StreamWriter("client.log"))
					{
						sw.Write(url);
					}
					WebClientGetFile(url, "twitchbot-v" + latest + ".zip").Wait();
				}
				catch (Exception e2)
				{
					ChatRoom.LogErrors(e2, wait: false);
					ChatRoom.ConsoleLog(url);
					ChatRoom.ConsoleLog("Could not download the archive -- did not update.", ConsoleColor.Yellow, TimeSpan.FromSeconds(5.0));
					return false;
				}
			}
			if (!File.Exists("./twitchbot-v" + latest + ".zip"))
			{
				ChatRoom.ConsoleLog("No file was downloaded -- version did not exist remotely. Returning to loading program.", ConsoleColor.Yellow, TimeSpan.FromSeconds(5.0));
				return false;
			}
			if (HandleFile(latest))
			{
				ChatRoom.ConsoleLog($"verson {latest} zip file has been downloaded and extracted to {Directory.GetCurrentDirectory()}\\{latest}.");
				return true;
			}
		}
		return false;
	}

	private static bool HandleFile(string latest)
	{
		string current = Environment.CurrentDirectory;
		string h = current + "/Plugins";
		IEnumerable<string> enumerable = Directory.EnumerateFiles(current);
		if (!Directory.Exists(h))
		{
			Directory.CreateDirectory(h);
		}
		IEnumerable<string> plugin = Directory.EnumerateFiles(h);
		string d = "/" + version.ToString();
		if (!Directory.Exists("." + d))
		{
			Directory.CreateDirectory("." + d);
		}
		foreach (string f in enumerable)
		{
			if (f.EndsWith(".zip") || f.Contains("wget"))
			{
				continue;
			}
			try
			{
				if (!File.Exists(f.Insert(f.LastIndexOf('/'), d + "/")))
				{
					File.Copy(f, f.Insert(f.LastIndexOf('/'), d + "/"));
					ChatRoom.ConsoleLog(f + " copied to " + d);
				}
			}
			catch
			{
				if (!File.Exists(f.Insert(f.LastIndexOf('\\'), d + "\\")))
				{
					File.Copy(f, f.Insert(f.LastIndexOf('\\'), d + "\\"));
					ChatRoom.ConsoleLog(f + " copied to " + d);
				}
			}
		}
		string g = Environment.CurrentDirectory + "\\" + latest;
		bool flag = false;
		while (true)
		{
			try
			{
				foreach (string f in plugin)
				{
					if (!File.Exists(f.Replace("/Plugins", "/" + latest + "/Plugins")))
					{
						File.Copy(f, f.Replace("/Plugins", "/" + latest + "/Plugins"));
						ChatRoom.ConsoleLog(f + " copied to /" + latest + "/Plugins");
					}
				}
			}
			catch
			{
				Directory.CreateDirectory(g);
				Directory.CreateDirectory(Environment.CurrentDirectory + "/" + latest + "/Plugins");
				if (!flag)
				{
					flag = true;
					continue;
				}
			}
			break;
		}
		try
		{
			ZipFile.ExtractToDirectory(current + "/twitchbot-v" + latest + ".zip", g);
		}
		catch (Exception ex)
		{
			ChatRoom.LogErrors(ex.Message, wait: false);
			return false;
		}
		try
		{
			Directory.CreateDirectory("./bin");
			File.Move(current + "/twitchbot-v" + latest + ".zip", current + "/bin/twitchbot-v" + latest + ".zip");
		}
		catch (Exception ex2)
		{
			ChatRoom.LogErrors(ex2.Message, ConsoleColor.Yellow, TimeSpan.FromSeconds(5.0));
			return true;
		}
		return true;
	}

	private static ConsoleKey ReadKeyYesNo()
	{
		ConsoleKey key;
		while ((key = Console.ReadKey().Key) != ConsoleKey.Y && key != ConsoleKey.N)
		{
		}
		return key;
	}
}
