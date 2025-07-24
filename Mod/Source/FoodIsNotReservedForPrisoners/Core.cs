
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;


namespace FoodIsNotReservedForPrisoners
{
	public static class Global
	{
		public static int droppedFoodReservedForTicks;
		public static bool allowingAutomaticHauling;
		public static bool manuallyDroppedFoodIsTemporarilyReservedForPrisoners;

		public static void ReifyState (bool initialising, FoodIsNotReservedForPrisonersMod mod)
		{
			FoodIsNotReservedForPrisonersMod.Settings option = mod.settings;

			int droppedFoodTicks = (
				  option.droppedFoodReservedForTicks >= 0
				? option.droppedFoodReservedForTicks
				: FoodIsNotReservedForPrisonersMod.Settings.Defaults.droppedFoodReservedForTicks
			);

			bool allowAutomaticHauling = (
				  option.allowAutomaticHauling >= 0
				? option.allowAutomaticHauling > 0
				: FoodIsNotReservedForPrisonersMod.Settings.Defaults.allowAutomaticHauling
			);

			bool manuallyDroppedFoodIsReserved = (
				  option.manuallyDroppedFoodIsTemporarilyReservedForPrisoners >= 0
				? option.manuallyDroppedFoodIsTemporarilyReservedForPrisoners > 0
				: FoodIsNotReservedForPrisonersMod.Settings.Defaults.manuallyDroppedFoodIsTemporarilyReservedForPrisoners
			);

			if (!initialising)
			{
				if (allowingAutomaticHauling)
				{
					mod.Unpatch(typeof(HarmonyPatches.AllowAutomaticHauling));

					if (droppedFoodReservedForTicks != 0)
					{
						mod.Unpatch(typeof(HarmonyPatches.TemporarilyReserveDroppedFood.TemporarilyReserveFoodDroppedForFoodDeliverJob));
						HarmonyPatches.TemporarilyReserveDroppedFood.TemporarilyReserveFoodDroppedForFoodDeliverJob.successfulPatchCount = 0;

						if (manuallyDroppedFoodIsTemporarilyReservedForPrisoners)
						{
							mod.Unpatch(typeof(HarmonyPatches.TemporarilyReserveManuallyDroppedFood.TemporarilyReserveFoodDroppedViaPawnGearTab));
						}
					}
				}
				else
				{
					mod.Unpatch(typeof(HarmonyPatches.AllowManualHauling));
				}
			}

			droppedFoodReservedForTicks = droppedFoodTicks;
			allowingAutomaticHauling = allowAutomaticHauling;
			manuallyDroppedFoodIsTemporarilyReservedForPrisoners = manuallyDroppedFoodIsReserved;

			if (allowAutomaticHauling)
			{
				mod.Patch(typeof(HarmonyPatches.AllowAutomaticHauling));

				if (droppedFoodTicks != 0)
				{
					mod.Patch(typeof(HarmonyPatches.TemporarilyReserveDroppedFood.TemporarilyReserveFoodDroppedForFoodDeliverJob));

					if (HarmonyPatches.TemporarilyReserveDroppedFood.TemporarilyReserveFoodDroppedForFoodDeliverJob.successfulPatchCount == 0)
					{
						Logger.Error("The transpiler patch for `JobDriver_FoodDeliver.MakeNewToils` failed to apply.");
					}

					if (manuallyDroppedFoodIsReserved)
					{
						mod.Patch(typeof(HarmonyPatches.TemporarilyReserveManuallyDroppedFood.TemporarilyReserveFoodDroppedViaPawnGearTab));
					}
				}
			}
			else
			{
				mod.Patch(typeof(HarmonyPatches.AllowManualHauling));
			}
		}
	}


	public class FoodIsNotReservedForPrisonersMod : Mod
	{
		public Harmony harmony;
		public Settings settings;

		public class Settings : ModSettings
		{
			public int schemaVersion = 1;
			public int allowAutomaticHauling = -1;
			public int droppedFoodReservedForTicks = -1;
			public int manuallyDroppedFoodIsTemporarilyReservedForPrisoners = -1;

			public static class Defaults
			{
				public static bool allowAutomaticHauling = false;
				public static int droppedFoodReservedForTicks = 22500;
				public static bool manuallyDroppedFoodIsTemporarilyReservedForPrisoners = true;
			}

			public override void ExposeData ()
			{
				if (Scribe.mode == LoadSaveMode.Saving)
				{
					int latestSchemaVersion = 1;
					Scribe_Values.Look(ref latestSchemaVersion, "schemaVersion", 1, true);
				}
				else
				{
					Scribe_Values.Look(ref this.schemaVersion, "schemaVersion", 1, true);
				}

				Scribe_Values.Look(ref this.allowAutomaticHauling, "allowAutomaticHauling", -1, true);
				Scribe_Values.Look(ref this.droppedFoodReservedForTicks, "droppedFoodReservedForTicks", -1, true);
				Scribe_Values.Look(ref this.manuallyDroppedFoodIsTemporarilyReservedForPrisoners, "manuallyDroppedFoodIsTemporarilyReservedForPrisoners", -1, true);
			}
		}

		public FoodIsNotReservedForPrisonersMod (ModContentPack contentPack) : base(contentPack)
		{
			this.harmony = new Harmony("com.just-harry.food-is-not-reserved-for-prisoners");

			this.settings = this.GetSettings<Settings>();

			ModSettingsFrontend.guiState.EmbodyModSettings(this.settings);

			this.Patch(typeof(HarmonyPatches.AllowIngestion));

			Global.ReifyState(initialising: true, this);
		}

		public override void WriteSettings ()
		{
			ModSettingsFrontend.guiState.ApplyToModSettings(this.settings);

			base.WriteSettings();

			Global.ReifyState(initialising: false, this);
		}

		internal bool Patch (Type type)
		{
			try
			{
				this.harmony.CreateClassProcessor(type).Patch();
				return true;
			}
			catch (Exception e)
			{
				Logger.Error(e.ToString());
				return false;
			}
		}

		internal bool Unpatch (Type type)
		{
			try
			{
				this.harmony.CreateClassProcessor(type).Unpatch();
				return true;
			}
			catch (Exception e)
			{
				Logger.Error(e.ToString());
				return false;
			}
		}

		public override void DoSettingsWindowContents (Rect plot)
		{
			ModSettingsFrontend.DrawWindowContents(plot, ref ModSettingsFrontend.guiState);
		}

		public override string SettingsCategory ()
		{
			return "FoodIsNotReservedForPrisoners_SettingsCategory".Translate();
		}
	}


	public class FoodIsNotReservedForPrisonersTsar : GameComponent
	{
		public int lastProcessedTick;
		public List<CompTemporarilyReservedForPrisoners> temporarilyReservedForPrisonersComps;
		public FoodIsNotReservedForPrisonersMod mod;

		public FoodIsNotReservedForPrisonersTsar (Game game) : this()
		{}

		public FoodIsNotReservedForPrisonersTsar ()
		{
			this.temporarilyReservedForPrisonersComps = new List<CompTemporarilyReservedForPrisoners>();
			this.mod = LoadedModManager.GetMod<FoodIsNotReservedForPrisonersMod>();
		}

		public static FoodIsNotReservedForPrisonersTsar? get;

		public override void LoadedGame ()
		{
			this.StartedOrLoadedGame();
		}

		public override void StartedNewGame ()
		{
			this.StartedOrLoadedGame();
		}

		public void StartedOrLoadedGame ()
		{
			this.lastProcessedTick = 0;
			get = Current.Game.GetComponent<FoodIsNotReservedForPrisonersTsar>();
		}

		public override void ExposeData ()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (this.temporarilyReservedForPrisonersComps.Count != 0)
				{
					this.ProcessTemporarilyReservedForPrisonersComps(this.temporarilyReservedForPrisonersComps);

					if (this.temporarilyReservedForPrisonersComps.Count != 0)
					{
						Scribe_Collections.Look(ref this.temporarilyReservedForPrisonersComps, "temporarilyReservedForPrisonersComps", LookMode.Deep);
					}
				}
			}
			else
			{
				Scribe_Collections.Look(ref this.temporarilyReservedForPrisonersComps, "temporarilyReservedForPrisonersComps", LookMode.Deep);

				if (Scribe.mode == LoadSaveMode.PostLoadInit)
				{
					this.temporarilyReservedForPrisonersComps ??= new List<CompTemporarilyReservedForPrisoners>();


					if (this.temporarilyReservedForPrisonersComps.Count != 0)
					{
						this.ProcessTemporarilyReservedForPrisonersComps(this.temporarilyReservedForPrisonersComps);

						if (this.temporarilyReservedForPrisonersComps.Count != 0)
						{
							foreach (CompTemporarilyReservedForPrisoners comp in this.temporarilyReservedForPrisonersComps)
							{
								CompManipulation.AddCompTo(comp.parent, comp, CompTemporarilyReservedForPrisoners.Static.props);
							}
						}
					}
				}
			}
		}

		public void RegisterCompTemporarilyReservedForPrisoners (CompTemporarilyReservedForPrisoners comp)
		{
			this.temporarilyReservedForPrisonersComps.Add(comp);
		}

		public override void GameComponentTick ()
		{
			if (Find.TickManager.TicksGame - this.lastProcessedTick < 1250)
			{
				return;
			}

			this.lastProcessedTick = Find.TickManager.TicksGame;

			if (this.temporarilyReservedForPrisonersComps.Count != 0)
			{
				this.ProcessTemporarilyReservedForPrisonersComps(this.temporarilyReservedForPrisonersComps);
			}
		}

		public void ProcessTemporarilyReservedForPrisonersComps (List<CompTemporarilyReservedForPrisoners> comps)
		{
			int removedCount = 0;
			int tailOffset = comps.Count;
			int offset = 0;

			while (offset < tailOffset)
			{
				CompTemporarilyReservedForPrisoners comp = comps[offset];

				if (comp.parent == null || comp.parent.Destroyed)
				{}
				else if (comp.Expired)
				{
					if (comp.reservedUntilTick != -1)
					{
						comp.CommitSudoku(compGuaranteedToBePresentInParent: false);
					}
				}
				else
				{
					++offset;
					continue;
				}

				--tailOffset;
				CompTemporarilyReservedForPrisoners tailComp = comps[tailOffset];
				comps[tailOffset] = comp;
				comps[offset] = tailComp;
				++removedCount;
			}

			if (removedCount != 0)
			{
				comps.RemoveRange(tailOffset, removedCount);
			}
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
		public static class AllowManualHauling
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

				throw new TranspilerFailedException("The `AllowManualHauling` transpiler patch for `HaulAIUtility.PawnCanAutomaticallyHaulFast` failed to apply.");
			}
		}

		[HarmonyPatch(
			typeof(HaulAIUtility),
			nameof(HaulAIUtility.PawnCanAutomaticallyHaulFast),
			new[] {typeof(Pawn), typeof(Thing), typeof(bool)}
		)]
		public static class AllowAutomaticHauling
		{
			public static bool IsSociallyProperWrapper (
				bool resultOfIsSociallyProper,
				Thing thing,
				bool forced
			)
			{
				if (resultOfIsSociallyProper | forced)
				{
					return true;
				}

				if (thing is ThingWithComps t)
				{
					CompTemporarilyReservedForPrisoners? comp = t.GetComp<CompTemporarilyReservedForPrisoners>();

					if (comp != null)
					{
						return !comp.IsReservedForPrisoners;
					}
				}

				return true;
			}

			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> WrapIsSociallyProper (
				IEnumerable<CodeInstruction> theInstructions,
				ILGenerator il
			)
			{
				/* Here we're looking for a piece of code that looks like:
						... isSociallyProper(..., ..., ..., ...) ...
				   and replacing it with some code that looks like this:
						... IsSociallyProperWrapper(isSociallyProper(..., ..., ..., ...), thing, forced) ...
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

				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return new CodeInstruction(OpCodes.Call, typeof(AllowAutomaticHauling).GetMethod("IsSociallyProperWrapper"));

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

				throw new TranspilerFailedException("The `AllowAutomaticHauling` transpiler patch for `HaulAIUtility.PawnCanAutomaticallyHaulFast` failed to apply.");
			}
		}

		public static class TemporarilyReserveDroppedFood
		{
			public static bool TryDropWrapper (
				bool resultOfTryDrop,
				Thing resultingThing
			)
			{
				if (
					   !resultOfTryDrop
					|| !resultingThing.def.IsNutritionGivingIngestible
					|| !resultingThing.def.ingestible.HumanEdible
					|| !resultingThing.PositionHeld.IsInPrisonCell(resultingThing.MapHeld)
				)
				{
					return resultOfTryDrop;
				}

				if (resultingThing is ThingWithComps thing)
				{
					if (!thing.HasComp<CompTemporarilyReservedForPrisoners>())
					{
						CompTemporarilyReservedForPrisoners comp = new CompTemporarilyReservedForPrisoners();
						comp.parent = thing;
						comp.reservedUntilTick = Find.TickManager.TicksGame + Global.droppedFoodReservedForTicks;

						if (CompManipulation.AddCompTo(thing, comp, CompTemporarilyReservedForPrisoners.Static.props))
						{
							FoodIsNotReservedForPrisonersTsar.get!.RegisterCompTemporarilyReservedForPrisoners(comp);
						}
					}
				}

				return resultOfTryDrop;
			}

			[HarmonyPatch]
			public static class TemporarilyReserveFoodDroppedForFoodDeliverJob
			{
				public static uint successfulPatchCount;

				[HarmonyTargetMethods]
				static public IEnumerable<MethodBase> FindDelegates ()
				{
					foreach (Type nestedType in typeof(JobDriver_FoodDeliver).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static))
					{
						foreach (MethodInfo method in nestedType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
						{
							if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
							{
								yield return method;
							}
						}
					}
				}

				[HarmonyTranspiler]
				public static IEnumerable<CodeInstruction> WrapTryDrop (
					IEnumerable<CodeInstruction> theInstructions,
					ILGenerator il,
					MethodBase method
				)
				{
					/* Here we're looking for a piece of code that looks like:
							... TryDropCarriedThing(..., ..., out X, ...) ...
					   and replacing it with some code that looks like this:
							... TryDropWrapper(TryDropCarriedThing(..., ..., out X, ...), X) ...

					   This code is defined in an anonymous delegate, hence `FindDelegates`.
					*/

					MethodInfo tryDropCarriedThing = typeof(Pawn_CarryTracker).GetMethod(
						nameof(Pawn_CarryTracker.TryDropCarriedThing),
						new[] {typeof(IntVec3), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>)}
					);

					using IEnumerator<CodeInstruction> instructions = theInstructions.GetEnumerator();

					uint patchStage = 0;
					int latestLoadedLocalAddressIndex = -1;

					CodeInstruction instruction;
				findCallOfTryDrop:
					if (!instructions.MoveNext()) goto noMoreInstructions;
					instruction = instructions.Current;

					latestLoadedLocalAddressIndex = instruction.LoadsLocalAddress(out int localIndex) ? localIndex : latestLoadedLocalAddressIndex;

					yield return instruction;

					if (!instruction.Calls(tryDropCarriedThing))
					{
						goto findCallOfTryDrop;
					}

					yield return CodeInstruction.LoadLocal(latestLoadedLocalAddressIndex);
					yield return new CodeInstruction(OpCodes.Call, typeof(TemporarilyReserveDroppedFood).GetMethod("TryDropWrapper"));

					++patchStage;
				yieldRestOfCode:
					if (!instructions.MoveNext()) goto noMoreInstructions;
					instruction = instructions.Current;

					yield return instruction;

					goto yieldRestOfCode;
				noMoreInstructions:
					if (patchStage <= 1)
					{
						if (patchStage == 1)
						{
							++successfulPatchCount;
						}

						yield break;
					}

					throw new TranspilerFailedException($"The transpiler patch for `{method.DeclaringType?.FullName}.{method.Name}` failed to apply.");
				}
			}
		}

		public static class TemporarilyReserveManuallyDroppedFood
		{
			[HarmonyPatch(typeof(ITab_Pawn_Gear), "InterfaceDrop", new[] {typeof(Thing)})]
			public static class TemporarilyReserveFoodDroppedViaPawnGearTab
			{
				[HarmonyTranspiler]
				public static IEnumerable<CodeInstruction> WrapTryDrop (
					IEnumerable<CodeInstruction> theInstructions,
					ILGenerator il
				)
				{
					/* Here we're looking for a piece of code that looks like:
							... TryDrop(..., ..., ..., ..., out X, ..., ...) ...
					   and replacing it with some code that looks like this:
							... TryDropWrapper(TryDrop(..., ..., ..., ..., out X, ..., ...), X) ...
					*/

					MethodInfo tryDrop = typeof(ThingOwner<Thing>).GetMethod(
						nameof(ThingOwner<Thing>.TryDrop),
						new[] {typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>), typeof(Predicate<IntVec3>)}
					);

					using IEnumerator<CodeInstruction> instructions = theInstructions.GetEnumerator();

					uint patchStage = 0;
					int latestLoadedLocalAddressIndex = -1;

					CodeInstruction instruction;
				findCallOfTryDrop:
					if (!instructions.MoveNext()) goto noMoreInstructions;
					instruction = instructions.Current;

					latestLoadedLocalAddressIndex = instruction.LoadsLocalAddress(out int localIndex) ? localIndex : latestLoadedLocalAddressIndex;

					yield return instruction;

					if (!instruction.Calls(tryDrop))
					{
						goto findCallOfTryDrop;
					}

					yield return CodeInstruction.LoadLocal(latestLoadedLocalAddressIndex);
					yield return new CodeInstruction(OpCodes.Call, typeof(TemporarilyReserveDroppedFood).GetMethod("TryDropWrapper"));

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

					throw new TranspilerFailedException("The transpiler patch for `ITab_Pawn_Gear.InterfaceDrop` failed to apply.");
				}
			}
		}
	}


	public sealed class CompTemporarilyReservedForPrisoners : ThingComp, IExposable
	{
		public int reservedUntilTick;

		public static class Static
		{
			public static CompProperties props = new CompProperties(typeof(CompTemporarilyReservedForPrisoners));
		}

		public void ExposeData ()
		{
			Scribe_References.Look(ref this.parent, "parent");
			Scribe_Values.Look(ref this.reservedUntilTick, "reservedUntilTick", -1, true);
		}

		public override void PostDeSpawn (Map map, DestroyMode mode)
		{
			this.CommitSudoku(compGuaranteedToBePresentInParent: true);
		}

		public bool WillBeReservedForPrisonersAtTick (int tick)
		{
			return tick < this.reservedUntilTick;
		}

		public bool IsReservedForPrisoners
		{
			get => Find.TickManager.TicksGame < this.reservedUntilTick;
		}

		public bool Expired
		{
			get => !this.IsReservedForPrisoners;
		}

		public void CommitSudoku (bool compGuaranteedToBePresentInParent)
		{
			this.reservedUntilTick = -1;

			if (compGuaranteedToBePresentInParent)
			{
				CompManipulation.RemoveAlreadyPresentCompFrom(this.parent, this);
			}
			else
			{
				CompManipulation.RemoveCompFrom(this.parent, this);
			}
		}

		public void CeaseReservationForPrisoners ()
		{
			this.CommitSudoku(compGuaranteedToBePresentInParent: true);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra ()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if (this.IsReservedForPrisoners)
			{
				yield return new Command_Action{
					action = () =>
					{
						SoundStarter.PlayOneShotOnCamera(SoundDefOf.ClickReject);
						this.CeaseReservationForPrisoners();
					},
					defaultLabel = "FoodIsNotReservedForPrisoners_CeaseReservationForPrisonersLabel".Translate(),
					defaultDesc = "FoodIsNotReservedForPrisoners_CeaseReservationForPrisonersDescription".Translate(),
					icon = TexButton.CloseXSmall
				};
			}
		}

		public override string? CompInspectStringExtra ()
		{
			if (this.IsReservedForPrisoners)
			{
				return "FoodIsNotReservedForPrisoners_ReservedForPrisonersForPeriod".Translate(
					(this.reservedUntilTick - Find.TickManager.TicksGame).ToStringTicksToPeriod(allowSeconds: false, shortForm: false, canUseDecimals: true)
				);
			}
			else
			{
				return null;
			}
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

