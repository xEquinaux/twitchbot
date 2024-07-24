using System;

namespace twitchbot;

public abstract class TwitchBot
{
	internal bool Active = true;

	public virtual Version Version => new Version(0, 1);

	public virtual string Name => "";

	public virtual char CommandChar => '!';

	public abstract void Initialize();

	public abstract void Dispose();
}
