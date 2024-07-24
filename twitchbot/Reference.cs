using System.Linq;
using CirclePrefect.Dotnet;

namespace twitchbot;

public class Reference
{
	private static DataStore db = new DataStore("refDb");

	public static Command Translate(string alternate)
	{
		if (db.BlockExists(alternate, out var item))
		{
			string o = item.GetValue("command");
			return ChatRoom.Commands.FirstOrDefault((Command t) => t.Name == o);
		}
		return Command.Empty;
	}

	public static bool Save(string original, string alternate)
	{
		bool flag;
		if (!db.BlockExists(alternate))
		{
			DataStore dataStore = db;
			string[] array = new string[1] { "command" };
			object[] values = new string[1] { original };
			dataStore.NewBlock(array, values, alternate);
			flag = true;
		}
		else
		{
			db.GetBlock(alternate).WriteValue("command", original);
			flag = false;
		}
		db.WriteToFile();
		return flag;
	}
}
