
using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;


namespace FoodIsNotReservedForPrisoners
{
	public static class ModSettingsFrontend
	{
		public struct GUIState
		{
			public bool allowAutomaticHaulingUseDefault;
			public bool allowAutomaticHaulingValue;
			public bool droppedFoodReservedForTicksUseDefault;
			public int droppedFoodReservedForTicksValue;
			public bool manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault;
			public bool manuallyDroppedFoodIsTemporarilyReservedForPrisonersValue;

			public void EmbodyModSettings (FoodIsNotReservedForPrisonersMod.Settings settings)
			{
				this.allowAutomaticHaulingUseDefault = settings.allowAutomaticHauling < 0;
				this.allowAutomaticHaulingValue = settings.allowAutomaticHauling >= 0 ? settings.allowAutomaticHauling != 0 : FoodIsNotReservedForPrisonersMod.Settings.Defaults.allowAutomaticHauling;
				this.droppedFoodReservedForTicksUseDefault = settings.droppedFoodReservedForTicks < 0;;
				this.droppedFoodReservedForTicksValue = settings.droppedFoodReservedForTicks >= 0 ? settings.droppedFoodReservedForTicks : FoodIsNotReservedForPrisonersMod.Settings.Defaults.droppedFoodReservedForTicks;
				this.manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault = settings.manuallyDroppedFoodIsTemporarilyReservedForPrisoners < 0;
				this.manuallyDroppedFoodIsTemporarilyReservedForPrisonersValue = settings.manuallyDroppedFoodIsTemporarilyReservedForPrisoners >= 0 ? settings.manuallyDroppedFoodIsTemporarilyReservedForPrisoners != 0 : FoodIsNotReservedForPrisonersMod.Settings.Defaults.manuallyDroppedFoodIsTemporarilyReservedForPrisoners;
			}

			public void ApplyToModSettings (FoodIsNotReservedForPrisonersMod.Settings settings)
			{
				settings.allowAutomaticHauling = this.allowAutomaticHaulingUseDefault ? -1 : (this.allowAutomaticHaulingValue ? 1 : 0);
				settings.droppedFoodReservedForTicks = this.droppedFoodReservedForTicksUseDefault ? -1 : this.droppedFoodReservedForTicksValue;
				settings.manuallyDroppedFoodIsTemporarilyReservedForPrisoners = this.manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault ? -1 : (this.manuallyDroppedFoodIsTemporarilyReservedForPrisonersValue ? 1 : 0);
			}
		}

		public static GUIState guiState;

		public static void DrawWindowContents (Rect plot, ref GUIState guiState)
		{
			const float rowHeight = 40f;

			Rect r;
			Rect names;
			Rect useDefaults;
			Rect options;
			Rect option;

			bool Checkbox (Rect rect, ref bool value, bool paintable = false)
			{
				bool isChecked = value;
				Widgets.Checkbox(rect.x, rect.y, ref isChecked, paintable: paintable);
				bool changed = value != isChecked;
				value = isChecked;
				return changed;
			}

			(names, r) = plot.CleaveVertically(plot.width * 0.6f);
			(useDefaults, options) = r.CleaveVertically(r.width * 0.35f);

			(r, names) = names.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_Setting".Translate());

			(r, useDefaults) = useDefaults.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_UseDefaultQ".Translate());

			(r, options) = options.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_Option".Translate());

			(r, names) = names.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_AllowAutomaticHaulingQ".Translate());
			TooltipHandler.TipRegion(r, "FoodIsNotReservedForPrisoners_AllowAutomaticHaulingDescription".Translate());

			(r, useDefaults) = useDefaults.CleaveHorizontally(rowHeight);
			if (Checkbox(r, ref guiState.allowAutomaticHaulingUseDefault, paintable: true) && guiState.allowAutomaticHaulingUseDefault)
			{
				guiState.allowAutomaticHaulingValue = FoodIsNotReservedForPrisonersMod.Settings.Defaults.allowAutomaticHauling;
			}

			(r, options) = options.CleaveHorizontally(rowHeight);
			if (Checkbox(r, ref guiState.allowAutomaticHaulingValue))
			{
				guiState.allowAutomaticHaulingUseDefault = false;
			}

			(r, names) = names.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_ManuallyDroppedFoodIsTemporarilyReservedForPrisonersQ".Translate());
			TooltipHandler.TipRegion(r, "FoodIsNotReservedForPrisoners_ManuallyDroppedFoodIsTemporarilyReservedForPrisonersDescription".Translate());

			(r, useDefaults) = useDefaults.CleaveHorizontally(rowHeight);
			if (Checkbox(r, ref guiState.manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault, paintable: true) && guiState.manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault)
			{
				guiState.manuallyDroppedFoodIsTemporarilyReservedForPrisonersValue = FoodIsNotReservedForPrisonersMod.Settings.Defaults.manuallyDroppedFoodIsTemporarilyReservedForPrisoners;
			}

			(r, options) = options.CleaveHorizontally(rowHeight);
			if (Checkbox(r, ref guiState.manuallyDroppedFoodIsTemporarilyReservedForPrisonersValue))
			{
				guiState.manuallyDroppedFoodIsTemporarilyReservedForPrisonersUseDefault = false;
			}

			(r, names) = names.CleaveHorizontally(rowHeight);
			Widgets.Label(r, "FoodIsNotReservedForPrisoners_DurationOfReservationForPrisonersForDroppedFood".Translate());
			TooltipHandler.TipRegion(r, "FoodIsNotReservedForPrisoners_DurationOfReservationForPrisonersForDroppedFoodDescription".Translate());

			(r, useDefaults) = useDefaults.CleaveHorizontally(rowHeight);
			if (Checkbox(r, ref guiState.droppedFoodReservedForTicksUseDefault, paintable: true) && guiState.droppedFoodReservedForTicksUseDefault)
			{
				guiState.droppedFoodReservedForTicksValue = FoodIsNotReservedForPrisonersMod.Settings.Defaults.droppedFoodReservedForTicks;
			}

			(option, options) = options.CleaveHorizontally(rowHeight);

			(r, option) = option.CleaveVertically(option.width * 0.4f);
			Widgets.Label(r, guiState.droppedFoodReservedForTicksValue.ToStringTicksToPeriod(allowSeconds: false, shortForm: false, canUseDecimals: true));

			(r, option) = option.CleaveVertically(option.width * 0.25f);
			if (Widgets.ButtonText(r, "--"))
			{
				guiState.droppedFoodReservedForTicksUseDefault = false;

				if (guiState.droppedFoodReservedForTicksValue >= (GenDate.TicksPerHour << 2))
				{
					guiState.droppedFoodReservedForTicksValue -= (GenDate.TicksPerHour << 2);
				}
			}

			(r, option) = option.CleaveVertically(option.width * (1f / 3f));
			if (Widgets.ButtonText(r, "-"))
			{
				guiState.droppedFoodReservedForTicksUseDefault = false;

				if (guiState.droppedFoodReservedForTicksValue >= GenDate.TicksPerHour)
				{
					guiState.droppedFoodReservedForTicksValue -= GenDate.TicksPerHour;
				}
			}

			(r, option) = option.CleaveVertically(option.width * 0.5f);
			if (Widgets.ButtonText(r, "+"))
			{
				guiState.droppedFoodReservedForTicksUseDefault = false;
				guiState.droppedFoodReservedForTicksValue += GenDate.TicksPerHour;
			}

			if (Widgets.ButtonText(option, "++"))
			{
				guiState.droppedFoodReservedForTicksUseDefault = false;
				guiState.droppedFoodReservedForTicksValue += GenDate.TicksPerHour << 2;
			}
		}
	}


	internal struct LeftRightRect
	{
		internal Rect left;
		internal Rect right;

		internal LeftRightRect (Rect left, Rect right)
		{
			this.left = left;
			this.right = right;
		}

		public void Deconstruct (out Rect left, out Rect right)
		{
			left = this.left;
			right = this.right;
		}
	}


	internal struct UpperLowerRect
	{
		internal Rect upper;
		internal Rect lower;

		internal UpperLowerRect (Rect upper, Rect lower)
		{
			this.upper = upper;
			this.lower = lower;
		}

		public void Deconstruct (out Rect upper, out Rect lower)
		{
			upper = this.upper;
			lower = this.lower;
		}
	}


	internal static class RectExtensions
	{
		internal static LeftRightRect CleaveVertically (this Rect r, float at)
		{
			return new LeftRightRect(
				r.LeftPartPixels(at),
				r.RightPartPixels(r.width - at)
			);
		}

		internal static UpperLowerRect CleaveHorizontally (this Rect r, float at)
		{
			return new UpperLowerRect(
				r.TopPartPixels(at),
				r.BottomPartPixels(r.height - at)
			);
		}
	}
}

