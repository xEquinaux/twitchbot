using System.IO;
using System.Net.Sockets;

namespace twitchbot;

public class ChatStream
{
	public StreamReader sr;

	public StreamWriter sw;

	public ChatStream(IRC irc)
	{
		NetworkStream stream = irc.client.GetStream();
		sr = new StreamReader(stream);
		sw = new StreamWriter(stream)
		{
			AutoFlush = true,
			NewLine = "\r\n"
		};
	}
}
