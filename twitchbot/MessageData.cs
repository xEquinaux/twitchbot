namespace twitchbot;

public static class MessageData
{
	public static char CommandChar => ChatRoom.CommandChar;

	public static string CommandName(string raw)
	{
		string text = Convert.GetContent(Convert.GetDataWithID(IrcID.UserType, raw));
		if (text.Split(' ')[4].Contains(CommandChar))
		{
			return text.Split(' ')[4].Substring(2);
		}
		return text.Split(' ')[4].Substring(1);
	}

	public static string CommandName(string raw, Command command)
	{
		string text = Convert.GetContent(Convert.GetDataWithID(IrcID.UserType, raw));
		if (text.Split(' ')[4].Contains(command.CommandChar))
		{
			return text.Split(' ')[4].Substring(2);
		}
		return text.Split(' ')[4].Substring(1);
	}

	public static string CommandText(string raw)
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
		string text2 = text.Substring(index + 1);
		return text2.Substring(text2.IndexOf(' ') + 1);
	}
}
