namespace Cetus;

public static class Return
{
	public static bool True(Action action)
	{
		action();
		return true;
	}
}