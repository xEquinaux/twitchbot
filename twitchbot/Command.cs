using System;
using System.Linq;

namespace twitchbot;

public class Command
{
	public class CommandArgs : EventArgs
	{
		public BadgeType[] Badge;

		public bool[] BadgeFlag;

		public char CommandChar;

		public string Channel;

		public string CommandName;

		public string CommandText;

		public string Message;

		public string Raw;

		public string Username;

		public Command Command;

		public HelpMessage helpMessage;
	}

	private delegate EventHandler<CommandArgs> handler();

	public char CommandChar;

	public string Name;

	public BadgeType[] Permission;

	public HelpMessage helpMessage;

	public TwitchBot Bot;

	public static Command Empty => new Command();

	public event EventHandler<CommandArgs> OnCommandEvent;

	private Command()
	{
		Bot = null;
		Name = "";
		helpMessage = new HelpMessage("", autoHandleHelp: false);
		CommandChar = '!';
		Permission = new BadgeType[1] { BadgeType.None };
	}

	public Command(TwitchBot bot, string name, HelpMessage helpMessage, char CommandChar, BadgeType[] permission)
	{
		Bot = bot;
		Name = name;
		this.helpMessage = helpMessage;
		this.CommandChar = CommandChar;
		Permission = permission;
	}

	internal bool HelpMessageReply(string chatMessage, BadgeType[] badge)
	{
		if (!helpMessage.AutoHandle)
		{
			return false;
		}
		for (int i = 0; i < badge.Length; i++)
		{
			if (Permission.Contains(badge[i]) && chatMessage == $"{ChatRoom.HelpCmd} {Bot.Name.Replace(" ", ".")} {helpMessage.CommandName}")
			{
				ChatRoom.Instance.SendMessage(helpMessage.Message);
				return true;
			}
			if (Permission.Contains(badge[i]) && chatMessage == ChatRoom.HelpCmd + " " + helpMessage.CommandName)
			{
				ChatRoom.Instance.SendMessage(helpMessage.Message);
				return true;
			}
		}
		return false;
	}

	internal void Invoke(object sender, CommandArgs e)
	{
		this.OnCommandEvent?.Invoke(sender, e);
	}

	private void Register(EventHandler<CommandArgs> handler)
	{
		throw new NotImplementedException();
	}

	private void Unregister()
	{
		throw new NotImplementedException();
	}
}
