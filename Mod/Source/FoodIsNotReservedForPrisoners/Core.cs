
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
				/* Here we're looking for a piece of code that looks like:
						... isSociallyProper(..., ...) ...
				   and replacing it with some code that looks like this:
						... isSociallyProper(..., ...) | true ...
				*/

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
				/* Here we're looking for a piece of code that looks like:
						... isSociallyProper(..., ..., ..., ...) ...
				   and replacing it with some code that looks like this:
						... isSociallyProper(..., ..., ..., ...) | forced ...
				*/

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


	internal static class CompManipulation
	{
		public static readonly AccessTools.FieldRef<ThingWithComps, List<ThingComp>> compsOfThingWithComps = (
			AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps")
		);
		public static readonly AccessTools.FieldRef<ThingWithComps, Dictionary<Type, ThingComp[]>> compsByTypeOfThingWithComps = (
			AccessTools.FieldRefAccess<ThingWithComps, Dictionary<Type, ThingComp[]>>("compsByType")
		);

		internal static bool AddCompTo <Comp> (ThingWithComps thingWithComps, Comp comp, CompProperties props)
		where Comp : ThingComp
		{
			List<ThingComp> comps = (compsOfThingWithComps(thingWithComps) ??= new(1));

			/* This is how `ThingWithComps#InitializeComps` does it. */
			try
			{
				comps.Add(comp);
				comp.Initialize(props);
			}
			catch (Exception error)
			{
				Logger.Error($"Failed to initialise a ThingComp: {error}");
				comps.Remove(comp);
				return false;
			}

			Dictionary<Type, ThingComp[]> compsByType = (compsByTypeOfThingWithComps(thingWithComps) ??= new(1));

			ThingComp[] compsOfType;
			int offset;

			if (compsByType.TryGetValue(typeof(Comp), out compsOfType))
			{
				offset = compsOfType.Length;
				Array.Resize(ref compsOfType, offset + 1);
			}
			else
			{
				compsOfType = new ThingComp[1];
				offset = 0;
			}

			compsOfType[offset] = comp;
			compsByType[typeof(Comp)] = compsOfType;

			return true;
		}

		internal static bool RemoveCompFrom <Comp> (ThingWithComps thingWithComps, Comp comp)
		where Comp : ThingComp
		{
			Dictionary<Type, ThingComp[]>? compsByType = compsByTypeOfThingWithComps(thingWithComps);

			if (compsByType != null)
			{
				if (compsByType.TryGetValue(typeof(Comp), out ThingComp[] compsOfType))
				{
					int count = compsOfType.Length;

					if (count <= 1)
					{
						if (count == 1)
						{
							if (compsOfType[0] != comp)
							{
								goto removedCompFromArray;
							}
						}

						compsByType.Remove(typeof(Comp));
						compsByType.TrimExcess();
					removedCompFromArray: {}
					}
					else
					{
						int offset = 0;

						for (;;)
						{
							if (offset < count)
							{
								if (compsOfType[offset] == comp)
								{
									break;
								}

								++offset;
							}
							else
							{
								goto handledCompsOfTypeArray;
							}
						}

						int excess = count - 1 - offset;

						while (excess-- > 0)
						{
							compsOfType[offset] = compsOfType[offset + 1];
							++offset;
						}

						Array.Resize(ref compsOfType, count - 1);

						compsByType[typeof(Comp)] = compsOfType;
					handledCompsOfTypeArray: {}
					}
				}
			}

			List<ThingComp>? comps = compsOfThingWithComps(thingWithComps);

			if (comps != null)
			{
				return comps.Remove(comp);
			}

			return false;
		}

		internal static void RemoveAlreadyPresentCompFrom <Comp> (ThingWithComps thingWithComps, Comp comp)
		where Comp : ThingComp
		{
			Dictionary<Type, ThingComp[]> compsByType = compsByTypeOfThingWithComps(thingWithComps)!;

			ThingComp[] compsOfType;
			compsByType.TryGetValue(typeof(Comp), out compsOfType);

			if (compsOfType.Length <= 1)
			{
				compsByType.Remove(typeof(Comp));
				compsByType.TrimExcess();
			}
			else
			{
				int offset = 0;
				for (; compsOfType[offset] != comp; ++offset) {}

				int excess = compsOfType.Length - 1 - offset;

				while (excess-- > 0)
				{
					compsOfType[offset] = compsOfType[offset + 1];
					++offset;
				}

				Array.Resize(ref compsOfType, compsOfType.Length - 1);

				compsByType[typeof(Comp)] = compsOfType;
			}

			compsOfThingWithComps(thingWithComps)!.Remove(comp);
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


	internal static class CodeInstructionExtensions
	{
		internal static bool LoadsLocalAddress (this CodeInstruction instruction)
		{
			return instruction.LoadsLocalAddress(out int actualIndex);
		}

		internal static bool LoadsLocalAddress (this CodeInstruction instruction, int localIndex)
		{
			return instruction.LoadsLocalAddress(out int actualIndex) ? localIndex == actualIndex : false;
		}

		internal static bool LoadsLocalAddress (this CodeInstruction instruction, out int localIndex)
		{
			uint opcode = (ushort) instruction.opcode.Value;

			if (opcode == 0x0012) /* Ldloca_S */
			{
				localIndex = instruction.operand is LocalBuilder l ? l.LocalIndex : (byte) instruction.operand;
				return true;
			}
			else if (opcode == 0xFE0D) /* Ldloca */
			{
				localIndex = instruction.operand is LocalBuilder l ? l.LocalIndex : (ushort) instruction.operand;
				return true;
			}

			localIndex = -1;
			return false;
		}
	}
}

