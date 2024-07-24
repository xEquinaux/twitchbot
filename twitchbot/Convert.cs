using System.Collections.Generic;
using System.Linq;

namespace twitchbot;

public static class Convert
{
	public static string GetContent(string data)
	{
		return data.Substring(data.IndexOf('=') + 1).Replace(";", "");
	}

	public static string GetDataWithID(IrcID id, string raw)
	{
		string[] split = raw.Split(';');
		int index = 0;
		for (int i = 0; i < split.Length; i++)
		{
			if (split[i].StartsWith(Literal(id)))
			{
				index = i;
				break;
			}
		}
		return split[index];
	}

	public static string GetDataWithIndex(int index, string raw)
	{
		return raw.Split(';')[index];
	}

	public static int Index(string id, string raw)
	{
		List<string> list = raw.Split(';').ToList();
		string first = list.First((string t) => t.StartsWith(id));
		return list.IndexOf(first);
	}

	public static string Literal(IrcID id)
	{
		return id switch
		{
			IrcID.BadgeInfo => "@badge-info", 
			IrcID.Badges => "badges", 
			IrcID.Color => "color", 
			IrcID.DisplayName => "display-name", 
			IrcID.Emotes => "emotes", 
			IrcID.FirstMessage => "first-msg", 
			IrcID.Flags => "flags", 
			IrcID.ID => "id", 
			IrcID.Mod => "mod", 
			IrcID.ReturningChatter => "returning-chatter", 
			IrcID.RoomID => "room-id", 
			IrcID.Subscriber => "subscriber", 
			IrcID.TmiSentTS => "tmi-sent-ts", 
			IrcID.Turbo => "turbo", 
			IrcID.UserID => "user-id", 
			IrcID.UserType => "user-type", 
			_ => string.Empty, 
		};
	}
}
