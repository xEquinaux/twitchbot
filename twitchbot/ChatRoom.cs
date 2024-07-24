using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using twitchbot.api;

namespace twitchbot;

public class ChatRoom
{
	public class RawReceivedArgs : EventArgs
	{
		public string Channel;

		public string Raw;
	}

	public class MessageReceivedArgs : EventArgs
	{
		public BadgeType[] Badge;

		public bool[] BadgeFlag;

		public string Channel;

		/// <summary>
		/// Contains the chat message sans the username.
		/// </summary>
		public string Message;

		/// <summary>
		/// Contains the raw IRC output.
		/// </summary>
		public string Raw;

		public string UserColor;

		/// <summary>
		/// Chat user's username.
		/// </summary>
		public string Username;
	}

	public class JoinEventArgs : EventArgs
	{
		public string Channel;

		public string Username;
	}

	public class LeaveEventArgs : EventArgs
	{
		public string Channel;

		public string Username;
	}

	public class AllCommandEventArgs : EventArgs
	{
		public BadgeType[] Badge;

		public bool[] BadgeFlag;

		public string Channel;

		public char CommandChar;

		public string CommandName;

		public string CommandText;

		public string Message;

		public string Raw;

		public string Username;
	}

	private class MiscID
	{
		public const byte Unused = 0;

		public const byte Badge = 1;

		public const byte Nonce = 2;

		public const byte Color = 3;

		public const byte Display_Name = 4;

		public const byte Unused2 = 5;

		public const byte Unused3 = 6;

		public const byte ID = 7;

		public const byte Mod_Flag = 8;

		public const byte Room_ID = 9;

		public const byte Sub_Flag = 10;

		public const byte Unused4 = 11;

		public const byte Misc_Turbo = 12;

		public const byte Unused5 = 13;

		public const byte User_Msg = 14;
	}

	public static readonly BadgeType[] PermissionAll = new BadgeType[8]
	{
		BadgeType.Broadcaster,
		BadgeType.None,
		BadgeType.Moderator,
		BadgeType.Partner,
		BadgeType.Prime,
		BadgeType.Subscriber,
		BadgeType.Turbo,
		BadgeType.Vip
	};

	internal ChatStream chat;

	internal StreamWriter write;

	internal StreamReader read;

	public static ChatRoom Instance;

	public string Channel;

	internal static List<Command> Commands = new List<Command>();

	internal static List<TwitchBot> Plugins = new List<TwitchBot>();

	public static char CommandChar = '!';

	public const string Text_PRIVMSG = "PRIVMSG #";

	public static string HelpCmd = "";

	public static string PluginPath = "/Plugins";

	public static event EventHandler<EventArgs> PingEvent;

	public static event EventHandler<MessageReceivedArgs> MessageReceivedEvent;

	public static event EventHandler<JoinEventArgs> UserJoinEvent;

	public static event EventHandler<AllCommandEventArgs> AllCommandEvent;

	public static event EventHandler<RawReceivedArgs> RawReceivedEvent;

	public static event EventHandler<LeaveEventArgs> UserLeaveEvent;

	private static string Duplicate(TwitchBot bot, string name)
	{
		return $"Duplicate commands found in {bot.Name}; command name: {name}, therefore removing each occurrence.";
	}

	private static string Duplicate(TwitchBot[] bot, string name)
	{
		return $"Duplicate commands found in {bot[0].Name} & {bot[1].Name}; command name: {name}; therefore not adding command instance.";
	}

	internal ChatRoom(ChatStream chat)
	{
		Instance = this;
		this.chat = chat;
		write = chat.sw;
		read = chat.sr;
	}

	private ChatRoom(ChatStream chat, string Channel)
	{
		Instance = this;
		this.chat = chat;
		write = chat.sw;
		read = chat.sr;
		this.Channel = Channel;
	}

	public static Command AddCommand(TwitchBot bot, Command command)
	{
		int num = Commands.Count((Command t) => t.Name == command.Name);
		bool flag = false;
		for (int i = 0; i < num; i++)
		{
			TwitchBot w = Commands.Where((Command t) => t.Name == command.Name).ToArray()[0].Bot;
			LogErrors(Duplicate(new TwitchBot[2] { bot, w }, command.Name), ConsoleColor.Yellow, TimeSpan.FromSeconds(2.0));
			flag = true;
		}
		if (!flag)
		{
			Commands.Add(command);
		}
		return command;
	}

	public static void RemoveCommand(TwitchBot bot, Command command)
	{
		Command[] w = Commands.Where((Command t) => t.Name == command.Name && t.Bot == bot).ToArray();
		int num = w.Count();
		if (num > 1)
		{
			LogErrors(Duplicate(bot, command.Name), ConsoleColor.Yellow, TimeSpan.FromSeconds(1.0));
			for (int i = 0; i < num; i++)
			{
				Commands.Remove(w[i]);
			}
		}
		else
		{
			Commands.Remove(command);
		}
	}

	protected static void Register(TwitchBot bot)
	{
		if (!Plugins.Contains(bot))
		{
			Plugins.Add(bot);
		}
	}

	internal static void LoadPlugins(string directory, List<TwitchBot> list, string ext = "*.dll", bool reload = false)
	{
		try
		{
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + directory);
			foreach (string item in Directory.EnumerateFiles(Environment.CurrentDirectory + directory, ext, SearchOption.TopDirectoryOnly))
			{
				Type[] array = Assembly.LoadFile(item)?.GetExportedTypes();
				foreach (Type type in array)
				{
					if (!type.IsSubclassOf(typeof(TwitchBot)) || !type.IsPublic || type.IsAbstract)
					{
						continue;
					}
					object[] customAttributes = type.GetCustomAttributes(typeof(ApiVersion), inherit: true);
					if (customAttributes.Length != 0 && ((ApiVersion)customAttributes[0]).Match(new ApiVersion(0, 3)))
					{
						TwitchBot pluginInstance = (TwitchBot)Activator.CreateInstance(type);
						if (reload)
						{
							pluginInstance.Initialize();
						}
						Register(pluginInstance);
						Console.WriteLine("Loaded: " + pluginInstance.Name + " " + pluginInstance.Version.ToString());
					}
				}
			}
		}
		catch (Exception e)
		{
			LogErrors(e, TimeSpan.FromSeconds(5.0));
		}
	}

	protected static void UnloadPlugins(string directory, List<TwitchBot> list)
	{
		foreach (TwitchBot bot in list)
		{
			try
			{
				Console.WriteLine("Unloaded: " + bot.Name + " " + bot.Version.ToString());
				bot?.Dispose();
			}
			catch (Exception e)
			{
				LogErrors(e, TimeSpan.FromSeconds(5.0));
			}
		}
		Commands.Clear();
		list.Clear();
	}

	internal void Connect(bool reconnect = false)
	{
		string text = "";
		if (Program.args != null && Program.args.Length >= 3)
		{
			try
			{
				if (reconnect)
				{
					Process.GetCurrentProcess().Kill();
					return;
				}
				text = Program.args[2];
				write.WriteLine("PASS " + Program.args[0]);
				write.WriteLine("NICK " + Program.args[1]);
				write.WriteLine("USER " + Program.args[1] + " 8 * :" + Program.args[1]);
				write.WriteLine("CAP REQ :twitch.tv/membership");
				write.WriteLine("CAP REQ :twitch.tv/tags");
				write.WriteLine("CAP REQ :twitch.tv/commands");
				write.WriteLine("JOIN #" + Program.args[2]);
			}
			catch (Exception e)
			{
				LogErrors(e, TimeSpan.FromSeconds(10.0));
				UnloadPlugins(PluginPath, Plugins);
				Connect(reconnect: true);
			}
		}
		else
		{
			Program.args = new string[3];
			try
			{
				while (!text.Contains("oauth"))
				{
					Console.Write("Twitch bot OAuth: ");
					write.WriteLine("PASS " + (Program.args[0] = (text = Console.ReadLine())));
				}
				text = "";
				while (text.Length < 4)
				{
					Console.Write("Twitch bot username: ");
					write.WriteLine("NICK " + (Program.args[1] = (text = Console.ReadLine())));
					write.WriteLine("USER " + text + " 8 * :" + text);
				}
				text = "";
				write.WriteLine("CAP REQ :twitch.tv/membership");
				write.WriteLine("CAP REQ :twitch.tv/tags");
				write.WriteLine("CAP REQ :twitch.tv/commands");
				while (text.Length < 4)
				{
					Console.Write("Destination channel name: ");
					write.WriteLine("JOIN #" + (Program.args[2] = (text = Console.ReadLine())));
				}
			}
			catch (Exception e2)
			{
				LogErrors(e2, TimeSpan.FromSeconds(10.0));
				Connect(reconnect: true);
			}
		}
		Reaper(text);
		while ((text = Console.ReadLine()) != "exit")
		{
			switch (text)
			{
			case "reload":
				Reload();
				break;
			case "unload":
				if (text.Contains(" "))
				{
					string name = text.Substring(text.IndexOf(" ") + 1).ToLower();
					TwitchBot[] list = Plugins.Where((TwitchBot t) => t.Name.ToLower().StartsWith(name)).ToArray();
					if (list.Length != 0)
					{
						Plugins[Plugins.IndexOf(list[0])].Dispose();
						Plugins.Remove(list[0]);
						Console.WriteLine(list[0].Name + " removed from plugin pool.");
					}
				}
				break;
			default:
				SendMessageRaw(text);
				break;
			case "compile":
				break;
			}
		}
		UnloadPlugins(PluginPath, Plugins);
	}

	internal static void Reload()
	{
		UnloadPlugins(PluginPath, Plugins);
		LoadPlugins(PluginPath, Plugins, "*.dll", reload: true);
	}

	public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Green, TimeSpan timeout = default(TimeSpan))
	{
		Console.ForegroundColor = color;
		Console.WriteLine(message);
		Console.ForegroundColor = ConsoleColor.Gray;
		if (timeout != default(TimeSpan))
		{
			Thread.Sleep(timeout);
		}
	}

	public static void LogErrors(string message, ConsoleColor color, TimeSpan timeout)
	{
		Console.ForegroundColor = color;
		Console.WriteLine(message);
		Console.ForegroundColor = ConsoleColor.Gray;
		Thread.Sleep(timeout);
	}

	public static void LogErrors(Exception e, TimeSpan timeout)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(e);
		Console.ForegroundColor = ConsoleColor.Gray;
		Thread.Sleep(timeout);
	}

	public static void LogErrors(Exception e, bool wait)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(e);
		Console.ForegroundColor = ConsoleColor.Gray;
		if (wait)
		{
			Console.WriteLine("Press any key to continue . . .");
			Console.ReadKey();
		}
	}

	public static void LogErrors(string message, bool wait)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(message);
		Console.ForegroundColor = ConsoleColor.Gray;
		if (wait)
		{
			Console.WriteLine("Press any key to continue . . .");
			Console.ReadKey();
		}
	}

	public void Reaper(string channel)
	{
		Channel = channel;
		receive(this);
	}

	public void SendMessage(string message)
	{
		write.WriteLine($"PRIVMSG #{Channel} :{message}");
	}

	public void SendMessageRaw(string message)
	{
		write.WriteLine(message);
	}

	private async void receive(object sender)
	{
		await Task.Run(delegate
		{
			ChatRoom sender2 = (ChatRoom)sender;
			string text = "";
			try
			{
				string text2 = read.ReadLine();
				while (true)
				{
					if (!string.IsNullOrWhiteSpace(text2))
					{
						Console.WriteLine(text2);
						if (text2.StartsWith("PING"))
						{
							write.WriteLine("PONG");
							if (ChatRoom.PingEvent != null)
							{
								ChatRoom.PingEvent(sender2, new EventArgs());
							}
							Console.WriteLine(text2);
						}
						else
						{
							if (text2.Contains("JOIN") && !text2.Contains("badges") && !text2.Contains("turbo") && ChatRoom.UserJoinEvent != null)
							{
								text = text2.Substring(text2.IndexOf("!"));
								ChatRoom.UserJoinEvent(sender2, new JoinEventArgs
								{
									Channel = Channel,
									Username = text.Substring(1, text.IndexOf("@") - 1)
								});
							}
							if (text2.Contains("PART") && !text2.Contains("badges") && !text2.Contains("turbo") && ChatRoom.UserLeaveEvent != null)
							{
								text = text2.Substring(text2.IndexOf("!"));
								ChatRoom.UserLeaveEvent(sender2, new LeaveEventArgs
								{
									Channel = Channel,
									Username = text.Substring(1, text.IndexOf("@") - 1)
								});
							}
							ChatRoom.RawReceivedEvent?.Invoke(sender2, new RawReceivedArgs
							{
								Raw = text2,
								Channel = Channel
							});
							if (text2.Contains(Convert.Literal(IrcID.UserType)) && !text2.Contains("USERSTATE") && !text2.Contains("ROOMSTATE"))
							{
								bool[] array = new bool[Badge.TotalBadges];
								array[0] = Badge.HasBadge(BadgeType.Broadcaster, text2);
								array[1] = Badge.HasBadge(BadgeType.Moderator, text2);
								array[2] = Badge.HasBadge(BadgeType.Vip, text2);
								array[3] = Badge.HasBadge(BadgeType.Subscriber, text2);
								array[4] = Badge.HasBadge(BadgeType.Turbo, text2);
								array[5] = Badge.HasBadge(BadgeType.Prime, text2);
								array[6] = Badge.HasBadge(BadgeType.Partner, text2);
								array[7] = true;
								ChatRoom.MessageReceivedEvent?.Invoke(sender2, new MessageReceivedArgs
								{
									Message = UserData.ChatMessage(text2),
									Username = UserData.Name(text2),
									BadgeFlag = array,
									Badge = Badge.GetBadges(text2),
									UserColor = UserData.UsernameColor(text2),
									Raw = text2,
									Channel = Channel
								});
								if (UserData.ChatMessage(text2).StartsWith(MessageData.CommandChar.ToString()))
								{
									ChatRoom.AllCommandEvent?.Invoke(sender2, new AllCommandEventArgs
									{
										BadgeFlag = array,
										Badge = Badge.GetBadges(text2),
										Channel = Channel,
										CommandName = MessageData.CommandName(text2),
										CommandText = MessageData.CommandText(text2),
										CommandChar = CommandChar,
										Message = UserData.ChatMessage(text2),
										Username = UserData.Name(text2),
										Raw = text2
									});
								}
								for (int i = 0; i < Commands.Count; i++)
								{
									if (Commands[i].Name == MessageData.CommandName(text2))
									{
										Commands[i]?.Invoke(sender2, new Command.CommandArgs
										{
											BadgeFlag = array,
											Badge = Badge.GetBadges(text2),
											Channel = Channel,
											Command = Commands[i],
											CommandName = MessageData.CommandName(text2, Commands[i]),
											CommandText = MessageData.CommandText(text2),
											CommandChar = Commands[i].CommandChar,
											helpMessage = Commands[i].helpMessage,
											Message = UserData.ChatMessage(text2),
											Raw = text2,
											Username = UserData.Name(text2)
										});
										break;
									}
								}
								bool flag = false;
								for (int j = 0; j < Commands.Count; j++)
								{
									if (Commands[j].HelpMessageReply(UserData.ChatMessage(text2), Badge.GetBadges(text2)))
									{
										flag = true;
										break;
									}
								}
								if (!flag && !string.IsNullOrWhiteSpace(HelpCmd) && UserData.ChatMessage(text2).StartsWith(HelpCmd) && (array[0] || array[1]))
								{
									if (UserData.ChatMessage(text2) == HelpCmd)
									{
										text = "@" + UserData.Name(text2) + " list of plugins: ";
										for (int k = 0; k < Plugins.Count; k++)
										{
											text = text + Plugins[k].Name.Replace(" ", ".") + ", ";
										}
										text = text.TrimEnd(',', ' ');
										SendMessage(text);
									}
									else
									{
										for (int l = 0; l < Plugins.Count; l++)
										{
											text = $"@{UserData.Name(text2)} list of commands for {Plugins[l].Name}: ";
											if (UserData.ChatMessage(text2).Contains(Plugins[l].Name.Replace(" ", ".")))
											{
												foreach (Command current in Commands)
												{
													if (current.Bot == Plugins[l])
													{
														text = text + current.Name + ", ";
													}
												}
												text = text.TrimEnd(',', ' ');
												SendMessage(text);
											}
										}
									}
								}
							}
						}
					}
					text2 = read.ReadLine();
				}
			}
			catch (Exception e)
			{
				LogErrors(e, TimeSpan.FromSeconds(5.0));
				UnloadPlugins(PluginPath, Plugins);
				Program.chat.Connect(reconnect: true);
			}
		});
	}
}
