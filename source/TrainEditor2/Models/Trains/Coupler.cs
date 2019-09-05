﻿using System;
using Prism.Mvvm;

namespace TrainEditor2.Models.Trains
{
	internal class Coupler : BindableBase, ICloneable
	{
		private double min;
		private double max;

		internal double Min
		{
			get
			{
				return min;
			}
			set
			{
				SetProperty(ref min, value);
			}
		}

		internal double Max
		{
			get
			{
				return max;
			}
			set
			{
				SetProperty(ref max, value);
			}
		}

		internal Coupler()
		{
			Min = 0.27;
			Max = 0.33;
		}

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
