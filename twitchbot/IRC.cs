using System.Net.Sockets;

namespace twitchbot;

public class IRC
{
	public const string TwitchDomain = "irc.twitch.tv";

	public const int TwitchPort = 6667;

	public TcpClient client;

	public IRC(string domain, int port)
	{
		client = new TcpClient(domain, port);
	}
}
