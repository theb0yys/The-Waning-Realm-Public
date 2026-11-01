using System;

namespace TAOM.XorberaxCompatibility;

internal static class CleaveContext
{
	private struct Hit
	{
		internal long Serial;

		internal object Attacker;

		internal bool HasMomentum;

		internal float Momentum;
	}

	private struct Model
	{
		internal long Serial;

		internal ScopeToken Hit;

		internal bool Qualified;
	}

	private sealed class State
	{
		internal readonly Hit[] Hits = new Hit[64];

		internal readonly Model[] Models = new Model[64];

		internal int HitDepth;

		internal int ModelDepth;

		internal long Serial;
	}

	internal const int Capacity = 64;

	[ThreadStatic]
	private static State state;

	internal static void PrepareThread()
	{
		if (state == null)
		{
			state = new State();
		}
	}

	internal static ScopeToken BeginHit(object attacker)
	{
		PrepareThread();
		ScopeToken result = new ScopeToken(++state.HitDepth, ++state.Serial);
		if (result.Depth <= 64)
		{
			state.Hits[result.Depth - 1] = new Hit
			{
				Serial = result.Serial,
				Attacker = attacker
			};
		}
		return result;
	}

	private static bool IsCurrentHit(ScopeToken token)
	{
		if (state != null && token.Depth > 0 && token.Depth <= 64 && state.HitDepth == token.Depth)
		{
			return state.Hits[token.Depth - 1].Serial == token.Serial;
		}
		return false;
	}

	internal static ScopeToken BeginModel(object attacker, bool isActiveModel)
	{
		if (state == null || state.HitDepth == 0)
		{
			return default(ScopeToken);
		}
		ScopeToken result = new ScopeToken(++state.ModelDepth, ++state.Serial);
		if (result.Depth > 64)
		{
			return result;
		}
		Model model = new Model
		{
			Serial = result.Serial
		};
		if (isActiveModel && attacker != null && state.HitDepth <= 64)
		{
			ref Hit reference = ref state.Hits[state.HitDepth - 1];
			if (attacker == reference.Attacker)
			{
				model.Hit = new ScopeToken(state.HitDepth, reference.Serial);
				reference.HasMomentum = false;
			}
		}
		state.Models[result.Depth - 1] = model;
		return result;
	}

	internal static void ObserveService(float? result)
	{
		if (state != null && state.ModelDepth != 0 && state.ModelDepth <= 64)
		{
			ref Model reference = ref state.Models[state.ModelDepth - 1];
			if (IsCurrentHit(reference.Hit))
			{
				reference.Qualified = result.HasValue;
			}
		}
	}

	internal static void CompleteModel(ScopeToken token, float result)
	{
		if (IsCurrentModel(token))
		{
			ref Model reference = ref state.Models[token.Depth - 1];
			if (IsCurrentHit(reference.Hit))
			{
				ref Hit reference2 = ref state.Hits[reference.Hit.Depth - 1];
				reference2.HasMomentum = reference.Qualified && !float.IsNaN(result) && !float.IsInfinity(result);
				reference2.Momentum = result;
			}
		}
	}

	private static bool IsCurrentModel(ScopeToken token)
	{
		if (state != null && token.Depth > 0 && token.Depth <= 64 && state.ModelDepth == token.Depth)
		{
			return state.Models[token.Depth - 1].Serial == token.Serial;
		}
		return false;
	}

	internal static void EndModel(ScopeToken token)
	{
		if (state == null || token.Depth == 0 || token.Depth != state.ModelDepth)
		{
			return;
		}
		if (token.Depth <= 64)
		{
			if (!IsCurrentModel(token))
			{
				return;
			}
			state.Models[token.Depth - 1] = default(Model);
		}
		state.ModelDepth--;
	}

	internal static void Restore(ScopeToken token, bool ranOriginal, ref float momentum)
	{
		if (ranOriginal && IsCurrentHit(token))
		{
			ref Hit reference = ref state.Hits[token.Depth - 1];
			if (reference.HasMomentum)
			{
				momentum = reference.Momentum;
			}
		}
	}

	internal static void EndHit(ScopeToken token)
	{
		if (state == null || token.Depth == 0 || token.Depth != state.HitDepth)
		{
			return;
		}
		if (token.Depth <= 64)
		{
			if (!IsCurrentHit(token))
			{
				return;
			}
			state.Hits[token.Depth - 1] = default(Hit);
		}
		state.HitDepth--;
	}
}
