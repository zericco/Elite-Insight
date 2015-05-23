﻿using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Windows.Forms;
using RegulatedNoise.Core.DomainModel;

namespace RegulatedNoise.Enums_and_Utility_Classes
{

	static class Extensions_CheckBox
	{
		public static int? toNInt(this CheckBox thisCheckBox)
		{
			int? retValue = null;

			switch (thisCheckBox.CheckState)
			{
				case CheckState.Checked:
					retValue = 1;
					break;
				case CheckState.Indeterminate:
					retValue = null;
					break;
				case CheckState.Unchecked:
					retValue = 0;
					break;
				default:
					break;
			}

			return retValue;
		}
	}
	
	static class Extensions_IntNullable
	{
		public static CheckState toCheckState(this int? thisInt)
		{
			CheckState retValue = CheckState.Indeterminate;

			switch (thisInt)
			{
				case null:
					retValue = CheckState.Indeterminate;
					break;
				case 0:
					retValue = CheckState.Unchecked;
					break;
				default:
					retValue = CheckState.Checked;
					break;
			}

			return retValue;
		}

		public static string ToNString(this int? thisInt)
		{
			string retValue = null;

			switch (thisInt)
			{
				case null:
					retValue = Program.NULLSTRING;
					break;
				default:
					retValue = thisInt.ToString();
					break;
			}

			return retValue;
		}
	}

	static class Extensions_BoolNullable
	{
		public static CheckState ToCheckState(this bool? nullableBoolean)
		{
			CheckState retValue = CheckState.Indeterminate;

			switch (nullableBoolean)
			{
				case null:
					retValue = CheckState.Indeterminate;
					break;
				case false:
					retValue = CheckState.Unchecked;
					break;
				case true:
					retValue = CheckState.Checked;
					break;
			}
			return retValue;
		}

		public static bool? ToNBool(this CheckBox checkBox)
		{
			return checkBox.CheckState.ToNBool();
		}

		public static bool? ToNBool(this CheckBox_ro checkBox)
		{
			return checkBox.CheckState.ToNBool();
		}

		public static bool? ToNBool(this CheckState checkState)
		{
			switch (checkState)
			{
				case CheckState.Unchecked:
					return false;
				case CheckState.Checked:
					return true;
				case CheckState.Indeterminate:
					return null;
				default:
					throw new ArgumentOutOfRangeException("checkState");
			}
		}
	}

	static class LandingPadSizeExtensions
	{
		public static LandingPadSize? ToNLandingPadSize(this string landingPadSize)
		{
			if (String.Equals("L", landingPadSize, StringComparison.InvariantCultureIgnoreCase))
			{
				return LandingPadSize.L;
			}
			else if (String.Equals("M", landingPadSize, StringComparison.InvariantCultureIgnoreCase))
			{
				return LandingPadSize.M;
			}
			else
			{
				return null;
			}
		}
	}

	static class Extensions_LongNullable
	{
		public static string ToNString(this long? thisLong)
		{
			if (thisLong == null)
				return Program.NULLSTRING;
			else
				return thisLong.ToString();
		}

		public static string ToNString(this long? thisLong, string format, IFormatProvider provider)
		{
			if (thisLong == null)
				return Program.NULLSTRING;
			else
				return ((long)thisLong).ToString(format, provider);
		}

		public static string ToNString(this int? thisInt, string format, IFormatProvider provider)
		{
			if (thisInt == null)
				return Program.NULLSTRING;
			else
				return ((int)thisInt).ToString(format, provider);
		}
	}


	static class Extensions_StringArrayNullable
	{
		/// <summary>
		/// clones a string array and return null if the source array is null
		/// </summary>
		/// <param name="thisString">a string or null</param>
		/// <returns></returns>
		public static string[] CloneN(this string[] thisStringArray)
		{
			if (thisStringArray == null)
				return null;
			else
				return (string[])thisStringArray.Clone();
		}
	}

	static class Extensions_StringNullable
	{
		private static readonly TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;

		/// <summary>
		/// converts a string that can be null to a string that represents null as a string ("undefined")
		/// </summary>
		/// <param name="thisString">a string or null</param>
		/// <returns></returns>
		public static string NToString(this string thisString)
		{
			if (thisString == null)
				return Program.NULLSTRING;
			else
				return thisString;
		}

		/// <summary>
		/// converts a possible null-representing string ("undefined") to a string or null
		/// </summary>
		/// <param name="thisString">a string or null</param>
		/// <returns></returns>
		public static string ToNString(this string thisString)
		{

			if (String.IsNullOrEmpty(thisString) || thisString.Equals(Program.NULLSTRING))
				return null;
			else
				return thisString;
		}

		public static Double ToDouble(this string thisString, string defaultValue = "")
		{
			Double Value = 0.0;

			if (Double.TryParse(thisString, out Value))
				return Value;
			else
				return Double.Parse(defaultValue);
		}

		public static long? ToNLong(this string thisString, string defaultValue = "")
		{
			long Value = 0;

			if (String.IsNullOrEmpty(thisString) || thisString.Equals(Program.NULLSTRING))
				return null;
			else
				if (long.TryParse(thisString, out Value))
					return (long?)Value;
				else
					return defaultValue.ToNLong();
		}

		public static int? ToNInt(this string thisString, string defaultValue = "")
		{
			long Value = 0;

			if (String.IsNullOrEmpty(thisString) || thisString.Equals(Program.NULLSTRING))
				return null;
			else
				if (long.TryParse(thisString, out Value))
					return (int?)Value;
				else
					return defaultValue.ToNInt();
		}

		public static string ToCleanTitleCase(this string value)
		{
			return String.IsNullOrWhiteSpace(value) ? String.Empty : _textInfo.ToTitleCase(value.Trim());
		}
	}

	static class Extensions_Object
	{
		/// <summary>
		/// converts a string that can be null to a string that represents null as a string ("undefined")
		/// </summary>
		/// <param name="thisString">a string or null</param>
		/// <returns></returns>
		public static string NToString(this Object thisObject)
		{

			if (thisObject == null)
				return Program.NULLSTRING;
			else
				return thisObject.ToString();
		}

	}

	static class Extensions_Control
	{
		public static void Bind<TControl, TResult, TDataSource>(this TControl control,
			 Expression<Func<TControl, TResult>> controlMember, TDataSource dataSource,
			 Expression<Func<TDataSource, TResult>> dataMember) where TControl : Control
		{
			control.DataBindings.Add(controlMember.GetPropertyName(), dataSource, dataMember.GetPropertyName(), false, DataSourceUpdateMode.OnPropertyChanged);
		}

		public static void BindChecked<TDataSource>(this CheckBox checkBox, TDataSource dataSource,
			 Expression<Func<TDataSource, bool>> datamember)
		{
			checkBox.Bind(ctrl => ctrl.Checked, dataSource, datamember);
		}

		public static void BindText<TDataSource>(this TextBox textBox, TDataSource dataSource,
			 Expression<Func<TDataSource, string>> datamember)
		{
			textBox.Bind(ctrl => ctrl.Text, dataSource, datamember);
		}

		public static void RunInGuiThread(this Control control, Action action)
		{
			if (control.InvokeRequired)
			{
				control.Invoke(action);
			}
			else
			{
				action();
			}
		}

		public static TResult RunInGuiThread<TResult>(this Control control, Func<TResult> action)
		{
			if (control.InvokeRequired)
			{
				return (TResult)control.Invoke(action);
			}
			else
			{
				return action();
			}
		}

		/// <summary>
		/// Checks whether a control or its parent is in design mode.
		/// </summary>
		/// <param name="c">The control to check.</param>
		/// <returns>Returns TRUE if in design mode, false otherwise.</returns>
		public static bool IsDesignMode(this Control c)
		{
			if (c == null)
			{
				return false;
			}
			else
			{
				while (c != null)
				{
					if (c.Site != null && c.Site.DesignMode)
					{
						return true;
					}
					else
					{
						c = c.Parent;
					}
				}

				return false;
			}
		}
	}

	static class ReflexionHelpers
	{
		public static string GetPropertyName<TItem,
				 TResult>(this Expression<Func<TItem, TResult>> expression)
		{
			if (expression.Body.NodeType == ExpressionType.MemberAccess)
			{
				return ((MemberExpression)expression.Body).Member.Name;
			}
			else
			{
				throw new NotSupportedException("unable to retrieve property name from expression");
			}
		}
	}
}
