using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CirclePrefect.Dotnet;

namespace twitchbot;

public class ConsoleHandler
{
	private static DataStore db = new DataStore("channelDb");

	private bool flag = true;

	private List<PluginData> data = new List<PluginData>();

	public ConsoleHandler()
	{
		Load();
		Init();
	}

	private void Init()
	{
		flag = true;
		int num = 0;
		foreach (TwitchBot t in ChatRoom.Plugins)
		{
			if (data.Count((PluginData d) => d.name == t.Name) == 0)
			{
				num++;
				data.Add(new PluginData
				{
					active = true,
					index = num,
					name = t.Name
				});
			}
		}
	}

	public void PluginInit(string directory, bool automated)
	{
		string ini = "." + directory + "/enabled.ini";
		bool exists = File.Exists(ini);
		if (!exists)
		{
			File.Create(ini).Close();
		}
		string text = "";
		if (ChatRoom.Plugins.Count == 0)
		{
			return;
		}
		if (automated && exists)
		{
			using (StreamReader sr = new StreamReader(ini))
			{
				string[] array = sr.ReadToEnd().Split(Environment.NewLine);
				int i;
				for (i = 0; i < data.Count; i++)
				{
					data[i].active = true;
					if (!array.Contains(ChatRoom.Plugins[i].Name))
					{
						data[i].active = false;
						TwitchBot remove = ChatRoom.Plugins.FirstOrDefault((TwitchBot t) => t.Name == data[i].name);
						if (remove != null)
						{
							remove.Active = false;
						}
					}
				}
			}
			TwitchBot[] array2 = (TwitchBot[])ChatRoom.Plugins.ToArray().Clone();
			foreach (TwitchBot bot in array2)
			{
				if (!bot.Active)
				{
					ChatRoom.Plugins.Remove(bot);
					Console.WriteLine("Disabled: " + bot.Name, null);
				}
				else
				{
					bot.Initialize();
					Console.WriteLine("Initialized: " + bot.Name + " " + bot.Version.ToString());
				}
			}
			return;
		}
		if (!exists && automated)
		{
			ChatRoom.ConsoleLog("Initial setup required for generating which plugins are enabled or not.", ConsoleColor.Green, TimeSpan.FromSeconds(5.0));
		}
		do
		{
			Console.Clear();
			if (flag)
			{
				flag = false;
			}
			else
			{
				int.TryParse(text, out var num);
				PluginData first = data.FirstOrDefault((PluginData t) => t.index == num);
				if (first != null)
				{
					first.active = !first.active;
				}
			}
			Console.WriteLine("Enter the plugin index to flip its enabled state:");
			foreach (PluginData d in data)
			{
				Console.WriteLine($"{d.index}) {d.name} enabled ({d.active})");
			}
			Console.WriteLine("Input \"start\" to continue.");
		}
		while ((text = Console.ReadLine()) != "start");
		Console.Clear();
		for (int i = 0; i < data.Count; i++)
		{
			PluginData d = data[i];
			if (d.active)
			{
				ChatRoom.Plugins[d.index - 1].Initialize();
				Console.WriteLine("Initialized: " + ChatRoom.Plugins[d.index - 1].Name + " " + ChatRoom.Plugins[d.index - 1].Version.ToString());
				using (StreamWriter sw = new StreamWriter(ini, append: true))
				{
					sw.WriteLine(d.name);
				}
				continue;
			}
			TwitchBot f = ChatRoom.Plugins.FirstOrDefault((TwitchBot t) => t.Name == d.name);
			if (f != null)
			{
				ChatRoom.Plugins.Remove(f);
			}
		}
	}

	public string ChannelInit(bool flag = false)
	{
		if (flag)
		{
			return string.Empty;
		}
		string text = "";
		Block item = db.GetBlock("channels");
		do
		{
			Console.WriteLine("Enter a channel name or select one from this list:");
			for (int i = 0; i < item.Length; i++)
			{
				Console.WriteLine($"{i + 1}) {item.GetValue(i.ToString() ?? "")}");
			}
			if (!int.TryParse(text, out var _))
			{
				SaveEntry(item.Length, text);
				break;
			}
		}
		while (!IsEntry(text = Console.ReadLine()) && text != "0");
		return text;
	}

	private bool IsEntry(string text)
	{
		return db.GetBlock("channels").HasValue(text);
	}

	private void SaveEntry(int i, string channel)
	{
		db.GetBlock("channel").AddItem(i.ToString() ?? "", channel);
		db.WriteToFile();
	}

	private void Load()
	{
		ChatRoom.LoadPlugins(ChatRoom.PluginPath, ChatRoom.Plugins);
	}

	public static string GetChangelog()
	{
		return Program.HttpGetString("https://raw.githubusercontent.com/ReDuzed/Trotbot-API-Wiki/main/changelog").Result;
	}
}
