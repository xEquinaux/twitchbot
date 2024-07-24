namespace twitchbot;

public class HelpMessage
{
	public bool AutoHandle = true;

	public string CommandName;

	public string Message;

	public HelpMessage(string message, bool autoHandleHelp = true)
	{
		Message = message;
		AutoHandle = autoHandleHelp;
	}

	public HelpMessage(string commandName, string message, bool autoHandleHelp = true)
	{
		CommandName = commandName;
		Message = message;
		AutoHandle = autoHandleHelp;
	}
}
