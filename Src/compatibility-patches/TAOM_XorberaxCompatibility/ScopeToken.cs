namespace TAOM.XorberaxCompatibility;

internal readonly struct ScopeToken
{
	internal readonly int Depth;

	internal readonly long Serial;

	internal ScopeToken(int depth, long serial)
	{
		Depth = depth;
		Serial = serial;
	}
}
