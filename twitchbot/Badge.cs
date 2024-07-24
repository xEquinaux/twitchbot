using System;
using System.Linq;

namespace twitchbot;

public static class Badge
{
	public static int TotalBadges => Enum.GetNames(typeof(BadgeType)).Length;

	private static string Data(string raw)
	{
		return Convert.GetDataWithID(IrcID.Badges, raw);
	}

	private static BadgeType Cast(string name)
	{
		Enum.TryParse<BadgeType>(name.ToLower(), out var badge);
		return badge;
	}

	public static BadgeType[] GetBadges(string raw)
	{
		BadgeType[] badge = new BadgeType[Enum.GetNames(typeof(BadgeType)).Length];
		string[] split = Convert.GetContent(Data(raw)).Split('/');
		for (int i = 0; i < split.Length; i++)
		{
			badge[i] = Cast(split[i]);
		}
		return badge;
	}

	public static bool HasBadge(BadgeType badge, string raw)
	{
		return GetBadges(raw).Contains(badge);
	}
}
