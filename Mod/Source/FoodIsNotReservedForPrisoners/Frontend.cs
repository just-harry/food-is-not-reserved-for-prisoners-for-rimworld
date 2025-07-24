
using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;


namespace FoodIsNotReservedForPrisoners
{
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

