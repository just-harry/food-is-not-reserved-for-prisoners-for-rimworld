
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;


namespace FoodIsNotReservedForPrisoners
{
	public class FoodIsNotReservedForPrisonersMod : Mod
	{
		public Harmony harmony;

		public FoodIsNotReservedForPrisonersMod (ModContentPack contentPack) : base(contentPack)
		{
			this.harmony = new Harmony("com.just-harry.food-is-not-reserved-for-prisoners");
			this.harmony.PatchAll();
		}
	}


	[Serializable]
	internal class TranspilerFailedException : Exception
	{
		public TranspilerFailedException ()
		{}

		public TranspilerFailedException (string message) : base(message)
		{}

		public TranspilerFailedException (string message, Exception innerException) : base (message, innerException)
		{}
	}


	public static class HarmonyPatches
	{
		[HarmonyPatch(
			typeof(FloatMenuOptionProvider_Ingest),
			"GetSingleOptionFor",
			new[] {typeof(Thing), typeof(FloatMenuContext)}
		)]
		public static class AllowIngestion
		{
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> IgnoreIsSociallyProper (
				IEnumerable<CodeInstruction> theInstructions,
				ILGenerator il
			)
			{
				MethodInfo isSociallyProper = typeof(SocialProperness).GetMethod(
					nameof(SocialProperness.IsSociallyProper),
					new[] {typeof(Thing), typeof(Pawn)}
				);

				using IEnumerator<CodeInstruction> instructions = theInstructions.GetEnumerator();

				uint patchStage = 0;

				CodeInstruction instruction;
			findCallOfIsSociallyProper:
				if (!instructions.MoveNext()) goto noMoreInstructions;
				instruction = instructions.Current;

				yield return instruction;

				if (!instruction.Calls(isSociallyProper))
				{
					goto findCallOfIsSociallyProper;
				}

				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				yield return new CodeInstruction(OpCodes.Or);

				++patchStage;
			yieldRestOfCode:
				if (!instructions.MoveNext()) goto noMoreInstructions;
				instruction = instructions.Current;

				yield return instruction;

				goto yieldRestOfCode;
			noMoreInstructions:
				if (patchStage == 1)
				{
					yield break;
				}

				throw new TranspilerFailedException("The transpiler patch for `FloatMenuOptionProvider_Ingest.GetSingleOptionFor` failed to apply.");
			}
		}

		[HarmonyPatch(
			typeof(HaulAIUtility),
			nameof(HaulAIUtility.PawnCanAutomaticallyHaulFast),
			new[] {typeof(Pawn), typeof(Thing), typeof(bool)}
		)]
		public static class AllowHauling
		{
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> IgnoreIsSociallyProper (
				IEnumerable<CodeInstruction> theInstructions,
				ILGenerator il
			)
			{
				MethodInfo isSociallyProper = typeof(SocialProperness).GetMethod(
					nameof(SocialProperness.IsSociallyProper),
					new[] {typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool)}
				);

				using IEnumerator<CodeInstruction> instructions = theInstructions.GetEnumerator();

				uint patchStage = 0;

				CodeInstruction instruction;
			findCallOfIsSociallyProper:
				if (!instructions.MoveNext()) goto noMoreInstructions;
				instruction = instructions.Current;

				yield return instruction;

				if (!instruction.Calls(isSociallyProper))
				{
					goto findCallOfIsSociallyProper;
				}

				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return new CodeInstruction(OpCodes.Or);

				++patchStage;
			yieldRestOfCode:
				if (!instructions.MoveNext()) goto noMoreInstructions;
				instruction = instructions.Current;

				yield return instruction;

				goto yieldRestOfCode;
			noMoreInstructions:
				if (patchStage == 1)
				{
					yield break;
				}

				throw new TranspilerFailedException("The transpiler patch for `HaulAIUtility.PawnCanAutomaticallyHaulFast` failed to apply.");
			}
		}
	}


	internal static class Logger
	{
		internal static void Error (string text)
		{
			Log.Error(Text(text));
		}

		internal static void Warning (string text)
		{
			Log.Warning(Text(text));
		}

		internal static void Debug (string text)
		{
			Log.Message(Text(text));
		}

		internal static string Text (string text)
		{
			return $"|Food Is Not Reserved For Prisoners| {text}";
		}
	}
}

