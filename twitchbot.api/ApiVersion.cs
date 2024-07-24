using System;

namespace twitchbot.api;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public sealed class ApiVersion : Attribute
{
	private readonly int major;

	private readonly int minor;

	private readonly int build;

	private readonly int revision;

	public ApiVersion(int major = 0, int minor = 0, int build = 0, int revision = 0)
	{
		this.major = major;
		this.minor = minor;
		this.build = build;
		this.revision = revision;
	}
}
