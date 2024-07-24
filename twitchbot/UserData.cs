using System.Linq;

namespace twitchbot;

public static class UserData
{
	public static string Name(string raw)
	{
		return Convert.GetContent(Convert.GetDataWithID(IrcID.DisplayName, raw));
	}

	public static string UsernameColor(string raw)
	{
		return Convert.GetContent(Convert.GetDataWithID(IrcID.Color, raw));
	}

	public static BadgeType[] GetBadges(string raw)
	{
		return Badge.GetBadges(raw);
	}

	public static bool HasBadge(BadgeType badge, string raw)
	{
		return Badge.HasBadge(badge, raw);
	}

	public static bool IsMod(string raw)
	{
		return Convert.GetContent(Convert.GetDataWithID(IrcID.Mod, raw)) == "1";
	}

	public static bool IsSubscriber(string raw)
	{
		return Convert.GetContent(Convert.GetDataWithID(IrcID.Subscriber, raw)) == "1";
	}

	public static bool IsTurbo(string raw)
	{
		return Convert.GetContent(Convert.GetDataWithID(IrcID.Turbo, raw)) == "1";
	}

	public static string ChatMessage(string raw)
	{
		string text = Convert.GetContent(Convert.GetDataWithID(IrcID.UserType, raw));
		int num = 0;
		int index = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == ':')
			{
				num++;
			}
			if (num == 2)
			{
				index = i;
				break;
			}
		}
		return text.Substring(index + 1);
	}

	public static bool HasCommand(string commandName)
	{
		return ChatRoom.Commands.Any((Command t) => t.Name.ToLower() == commandName.ToLower());
	}

	public static string GetCommand(string message)
	{
		foreach (Command c in ChatRoom.Commands)
		{
			if (message.StartsWith(c.CommandChar + c.Name))
			{
				return c.Name;
			}
		}
		return null;
	}

	public static string GetCommandFromRaw(string raw)
	{
		string text = ChatMessage(raw);
		foreach (Command c in ChatRoom.Commands)
		{
			if (text.StartsWith(c.CommandChar + c.Name))
			{
				return c.Name;
			}
		}
		return null;
	}
}
