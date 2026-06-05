using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class Line : DrawingTool
{
	protected enum ChartLineType
	{
		ArrowLine,
		ExtendedLine,
		HorizontalLine,
		Line,
		Ray,
		VerticalLine
	}

	[CLSCompliant(false)]
	protected PathGeometry ArrowPathGeometry;

	private ChartAnchor cursorAnchor;

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	public override object Icon => Icons.DrawLineTool;

	[Browsable(false)]
	[XmlIgnore]
	protected ChartLineType LineType { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolLine", Order = 99)]
	public Stroke Stroke { get; set; }

	public override bool SupportsAlerts => true;

	private ChartAnchor Anchor45(ChartAnchor startAnchor, ChartAnchor endAnchor, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected O, but got Unknown
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).IsLocked)
		{
			return endAnchor;
		}
		if (!Keyboard.IsKeyDown((Key)116) && !Keyboard.IsKeyDown((Key)117))
		{
			return endAnchor;
		}
		Point point = startAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point2 = endAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		double num = ((Point)(ref point2)).X - ((Point)(ref point)).X;
		double num2 = ((Point)(ref point2)).Y - ((Point)(ref point)).Y;
		double num3 = Math.Sqrt(num * num + num2 * num2);
		double num4 = Math.Atan2(num2, num);
		double num5 = Math.PI / 8.0;
		double num6 = 0.0;
		if (num4 > Math.PI - num5 || num4 < -Math.PI + num5)
		{
			num6 = Math.PI;
		}
		else if (num4 > Math.PI - num5 * 3.0)
		{
			num6 = Math.PI - num5 * 2.0;
		}
		else if (num4 > Math.PI - num5 * 5.0)
		{
			num6 = Math.PI - num5 * 4.0;
		}
		else if (num4 > Math.PI - num5 * 7.0)
		{
			num6 = Math.PI - num5 * 6.0;
		}
		else if (num4 < -Math.PI + num5 * 3.0)
		{
			num6 = -Math.PI + num5 * 2.0;
		}
		else if (num4 < -Math.PI + num5 * 5.0)
		{
			num6 = -Math.PI + num5 * 4.0;
		}
		else if (num4 < -Math.PI + num5 * 7.0)
		{
			num6 = -Math.PI + num5 * 6.0;
		}
		Point val = default(Point);
		((Point)(ref val))._002Ector(((Point)(ref point)).X + Math.Cos(num6) * num3, ((Point)(ref point)).Y + Math.Sin(num6) * num3);
		ChartAnchor val2 = new ChartAnchor();
		val2.UpdateFromPoint(val, chartControl, chartScale);
		if (Math.Abs(((Point)(ref point)).X - ((Point)(ref val)).X) < 1E-05)
		{
			val2.Time = startAnchor.Time;
			val2.SlotIndex = startAnchor.SlotIndex;
		}
		else if (Math.Abs(((Point)(ref point)).Y - ((Point)(ref val)).Y) < 1E-05)
		{
			val2.Price = startAnchor.Price;
		}
		return val2;
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		case 1:
		{
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.No;
			}
			ChartLineType lineType = LineType;
			if ((lineType == ChartLineType.HorizontalLine || lineType == ChartLineType.VerticalLine) ? true : false)
			{
				return Cursors.SizeAll;
			}
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		}
		default:
		{
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartLineType lineType = LineType;
			if ((lineType == ChartLineType.HorizontalLine || lineType == ChartLineType.VerticalLine) ? true : false)
			{
				if (LineType == ChartLineType.VerticalLine && Math.Abs(((Point)(ref point)).X - ((Point)(ref point2)).X) <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				if (LineType == ChartLineType.HorizontalLine && Math.Abs(((Point)(ref point)).Y - ((Point)(ref point2)).Y) <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				return null;
			}
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (closestAnchor != StartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point val = point2;
			Point val2 = point3;
			if (LineType == ChartLineType.ExtendedLine)
			{
				val = ((DrawingTool)this).GetExtendedPoint(chartControl, chartPanel, chartScale, EndAnchor, StartAnchor);
				val2 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
			}
			else if (LineType == ChartLineType.Ray)
			{
				val2 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
			}
			Vector val3 = val2 - val;
			if (!MathHelper.IsPointAlongVector(point, val, val3, 15.0))
			{
				return null;
			}
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.Arrow;
		}
		}
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = Resource.NinjaScriptDrawingToolLine,
			ShouldOnlyDisplayName = true
		};
	}

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		int num = val.W + val.X;
		int num2 = val.Y + val.H;
		if (LineType != ChartLineType.VerticalLine)
		{
			if (LineType != ChartLineType.HorizontalLine)
			{
				Point val2 = point + (point2 - point) / 2.0;
				return (Point[])(object)new Point[3] { point, val2, point2 };
			}
			return (Point[])(object)new Point[3]
			{
				new Point((double)val.X, ((Point)(ref point)).Y),
				new Point((double)num / 2.0, ((Point)(ref point)).Y),
				new Point((double)num, ((Point)(ref point)).Y)
			};
		}
		return (Point[])(object)new Point[3]
		{
			new Point(((Point)(ref point)).X, (double)val.Y),
			new Point(((Point)(ref point)).X, (double)val.Y + (double)(num2 - val.Y) / 2.0),
			new Point(((Point)(ref point)).X, (double)num2)
		};
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected I4, but got Unknown
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Invalid comparison between Unknown and I4
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Expected I4, but got Unknown
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Invalid comparison between Unknown and I4
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Invalid comparison between Unknown and I4
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Invalid comparison between Unknown and I4
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Invalid comparison between Unknown and I4
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Invalid comparison between Unknown and I4
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Invalid comparison between Unknown and I4
		if (values.Length < 1)
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double lineVal;
		Condition val2;
		if (LineType == ChartLineType.HorizontalLine)
		{
			double value = values[0].Value;
			lineVal = ((ConditionItem)conditionItem).Offset.Calculate(StartAnchor.Price, ((DrawingTool)this).AttachedTo.Instrument);
			val2 = condition;
			switch ((int)val2)
			{
			case 2:
				return MathExtentions.ApproxCompare(value, lineVal) == 0;
			case 7:
				return MathExtentions.ApproxCompare(value, lineVal) != 0;
			case 3:
				return value > lineVal;
			case 4:
				return value >= lineVal;
			case 5:
				return value < lineVal;
			case 6:
				return value <= lineVal;
			case 0:
			case 1:
				return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
			default:
				return false;
			}
		}
		Point val3 = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val4 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		ChartLineType lineType = LineType;
		if ((lineType == ChartLineType.ExtendedLine || lineType == ChartLineType.Ray) ? true : false)
		{
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, StartAnchor, EndAnchor);
			if (LineType == ChartLineType.ExtendedLine)
			{
				val3 = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, EndAnchor, StartAnchor);
			}
			val4 = extendedPoint;
		}
		double num = double.MaxValue;
		double num2 = double.MinValue;
		Point[] array = (Point[])(object)new Point[2] { val3, val4 };
		for (int i = 0; i < array.Length; i++)
		{
			Point val5 = array[i];
			num = Math.Min(num, ((Point)(ref val5)).X);
			num2 = Math.Max(num2, ((Point)(ref val5)).X);
		}
		double num3 = (((int)values[0].ValueType == 12) ? num : ((double)chartControl.GetXByTime(values[0].Time)));
		double num4 = chartScale.GetYByValue(values[0].Value);
		if (num2 < num3)
		{
			return false;
		}
		if (num > num3)
		{
			return false;
		}
		Point leftPoint = ((((Point)(ref val3)).X < ((Point)(ref val4)).X) ? val3 : val4);
		Point rightPoint = ((((Point)(ref val4)).X > ((Point)(ref val3)).X) ? val4 : val3);
		Point val6 = default(Point);
		((Point)(ref val6))._002Ector(num3, num4);
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, val6);
		val2 = condition;
		switch ((int)val2)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation == 0 || (int)pointLineLocation == 2)
			{
				return true;
			}
			return false;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if (pointLineLocation - 1 <= 1)
			{
				return true;
			}
			return false;
		case 2:
			return (int)pointLineLocation == 2;
		case 7:
			return (int)pointLineLocation != 2;
		case 0:
		case 1:
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate2);
		default:
			return false;
		}
		bool Predicate(ChartAlertValue v)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if ((int)condition == 0)
			{
				return v.Value > lineVal;
			}
			return v.Value < lineVal;
		}
		bool Predicate2(ChartAlertValue v)
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Invalid comparison between Unknown and I4
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Invalid comparison between Unknown and I4
			double num5 = chartControl.GetXByTime(v.Time);
			double num6 = chartScale.GetYByValue(v.Value);
			Point val7 = default(Point);
			((Point)(ref val7))._002Ector(num5, num6);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(leftPoint, rightPoint, val7);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		DateTime dateTime = Globals.MaxDate;
		DateTime dateTime2 = Globals.MinDate;
		if (LineType != ChartLineType.ExtendedLine && LineType != ChartLineType.Ray)
		{
			if (LineType == ChartLineType.VerticalLine)
			{
				if (StartAnchor.Time >= firstTimeOnChart)
				{
					return StartAnchor.Time <= lastTimeOnChart;
				}
				return false;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				if (anchor.Time < dateTime)
				{
					dateTime = anchor.Time;
				}
				if (anchor.Time > dateTime2)
				{
					dateTime2 = anchor.Time;
				}
			}
		}
		else
		{
			ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
			Point val2 = StartAnchor.GetPoint(chartControl, val, chartScale, true);
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, StartAnchor, EndAnchor);
			if (LineType == ChartLineType.ExtendedLine)
			{
				val2 = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, EndAnchor, StartAnchor);
			}
			Point[] array = (Point[])(object)new Point[2] { val2, extendedPoint };
			for (int i = 0; i < array.Length; i++)
			{
				Point val3 = array[i];
				DateTime timeByX = chartControl.GetTimeByX((int)((Point)(ref val3)).X);
				if (timeByX > dateTime2)
				{
					dateTime2 = timeByX;
				}
				if (timeByX < dateTime)
				{
					dateTime = timeByX;
				}
			}
		}
		if (LineType == ChartLineType.HorizontalLine && (StartAnchor.Price < chartScale.MinValue || StartAnchor.Price > chartScale.MaxValue) && !((ChartObject)this).IsAutoScale)
		{
			return false;
		}
		if (LineType != ChartLineType.HorizontalLine && (dateTime > lastTimeOnChart || dateTime2 < firstTimeOnChart))
		{
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible)
		{
			return;
		}
		if (LineType == ChartLineType.HorizontalLine)
		{
			double minValue = (((ChartObject)this).MaxValue = ((DrawingTool)this).Anchors.First().Price);
			((ChartObject)this).MinValue = minValue;
		}
		else
		{
			if (LineType == ChartLineType.VerticalLine || !((DrawingTool)this).Anchors.Any((ChartAnchor a) => !a.IsEditing))
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
				((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
			}
		}
	}

	public override void OnKeyDown(ChartControl chartControl, ChartPanel chartPanel, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		if (((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 0) && EndAnchor != null)
		{
			Key key = e.Key;
			bool flag = key - 116 <= 1;
			if (flag && (EndAnchor.IsEditing || ((int)((DrawingTool)this).DrawingState == 1 && StartAnchor.IsEditing)) && LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine)
			{
				((DrawingTool)this).ForceRefresh();
			}
		}
	}

	public override void OnKeyUp(ChartControl chartControl, ChartPanel chartPanel, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		if (((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 0) && EndAnchor != null)
		{
			Key key = e.Key;
			bool flag = key - 116 <= 1;
			if (flag && (EndAnchor.IsEditing || ((int)((DrawingTool)this).DrawingState == 1 && StartAnchor.IsEditing)) && LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine)
			{
				((DrawingTool)this).ForceRefresh();
			}
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartLineType lineType = LineType;
			if ((lineType == ChartLineType.HorizontalLine || lineType == ChartLineType.VerticalLine) ? true : false)
			{
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
				else
				{
					editingAnchor = StartAnchor;
				}
			}
			else
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			}
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = true;
				cursorAnchor = dataPoint;
				((DrawingTool)this).DrawingState = (DrawingState)1;
			}
			else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
			{
				((DrawingTool)this).DrawingState = (DrawingState)3;
			}
			else
			{
				((ChartObject)this).IsSelected = false;
			}
			return;
		}
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			StartAnchor.IsEditing = false;
			ChartLineType lineType = LineType;
			if ((lineType == ChartLineType.HorizontalLine || lineType == ChartLineType.VerticalLine) ? true : false)
			{
				EndAnchor.IsEditing = false;
			}
			dataPoint.CopyDataValues(EndAnchor);
		}
		else if (EndAnchor.IsEditing)
		{
			Anchor45(StartAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(EndAnchor);
			EndAnchor.IsEditing = false;
		}
		if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		cursorAnchor = dataPoint;
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EndAnchor.IsEditing)
			{
				Anchor45(StartAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(EndAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			if (LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine)
			{
				ChartAnchor startAnchor = ((editingAnchor == StartAnchor) ? EndAnchor : StartAnchor);
				Anchor45(startAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(editingAnchor);
			}
			else if (LineType != ChartLineType.VerticalLine)
			{
				editingAnchor.Price = dataPoint.Price;
				EndAnchor.Price = dataPoint.Price;
			}
			else
			{
				editingAnchor.Time = dataPoint.Time;
				editingAnchor.SlotIndex = dataPoint.SlotIndex;
			}
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				if (LineType == ChartLineType.HorizontalLine)
				{
					anchor.MoveAnchorPrice(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
				}
				else if (LineType == ChartLineType.VerticalLine)
				{
					anchor.MoveAnchorTime(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
				}
				else
				{
					anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
				}
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if (((int)drawingState == 1 || (int)drawingState == 3) ? true : false)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
		if (editingAnchor != null)
		{
			editingAnchor.IsEditing = false;
		}
		editingAnchor = null;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Expected O, but got Unknown
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_045e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		if (Stroke == null)
		{
			return;
		}
		Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
		AntialiasMode antialiasMode = ((ChartObject)this).RenderTarget.AntialiasMode;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		if (!((DrawingTool)this).IsLocked && LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine && cursorAnchor != null)
		{
			if (EndAnchor.IsEditing)
			{
				Anchor45(StartAnchor, cursorAnchor, chartControl, val, chartScale).CopyDataValues(EndAnchor);
			}
			else if ((int)((DrawingTool)this).DrawingState == 1 && StartAnchor.IsEditing)
			{
				ChartAnchor startAnchor = ((editingAnchor == StartAnchor) ? EndAnchor : StartAnchor);
				Anchor45(startAnchor, cursorAnchor, chartControl, val, chartScale).CopyDataValues(editingAnchor);
			}
		}
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = ((MathExtentions.ApproxCompare((double)(Stroke.Width % 2f), 0.0) == 0) ? 0.5 : 0.0);
		Vector val2 = default(Vector);
		((Vector)(ref val2))._002Ector(num, num);
		ChartLineType lineType = LineType;
		if ((lineType == ChartLineType.HorizontalLine || lineType == ChartLineType.VerticalLine) ? true : false)
		{
			Point val3 = ((LineType == ChartLineType.HorizontalLine) ? new Point((double)val.X, ((Point)(ref point)).Y) : new Point(((Point)(ref point)).X, (double)val.Y)) + val2;
			Point val4 = ((LineType == ChartLineType.HorizontalLine) ? new Point((double)(val.X + val.W), ((Point)(ref point)).Y) : new Point(((Point)(ref point)).X, (double)(val.Y + val.H))) + val2;
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val3), DxExtensions.ToVector2(val4), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
			return;
		}
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Vector2 val5 = DxExtensions.ToVector2(point2 + val2);
		Point val6 = point + val2;
		Vector2 val7 = DxExtensions.ToVector2(val6);
		Brush val8 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX);
		if (LineType == ChartLineType.Line)
		{
			((ChartObject)this).RenderTarget.DrawLine(val7, val5, val8, Stroke.Width, Stroke.StrokeStyle);
			return;
		}
		if (LineType != ChartLineType.ArrowLine)
		{
			Point val9 = val6;
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, StartAnchor, EndAnchor);
			if (LineType == ChartLineType.ExtendedLine)
			{
				val9 = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, EndAnchor, StartAnchor);
			}
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val9), DxExtensions.ToVector2(extendedPoint), val8, Stroke.Width, Stroke.StrokeStyle);
		}
		else
		{
			Vector val10 = point2 - point;
			((Vector)(ref val10)).Normalize();
			((ChartObject)this).RenderTarget.DrawLine(val7, val5, val8, Stroke.Width, Stroke.StrokeStyle);
			float num2 = 0f - (float)Math.Atan2(((Vector)(ref val10)).X, ((Vector)(ref val10)).Y);
			Vector val11 = val10 * 5.0;
			Vector2 val12 = default(Vector2);
			((Vector2)(ref val12))._002Ector((float)((double)val5.X + ((Vector)(ref val11)).X), (float)((double)val5.Y + ((Vector)(ref val11)).Y));
			Matrix3x2 transform = Matrix3x2.Rotation(num2, Vector2.Zero) * Matrix3x2.Scaling((float)Math.Max(1.0, (double)Stroke.Width * 0.45) + 0.25f) * Matrix3x2.Translation(val12);
			if (ArrowPathGeometry == null)
			{
				ArrowPathGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj = ArrowPathGeometry.Open();
				Vector2 val13 = default(Vector2);
				((Vector2)(ref val13))._002Ector(0f, Stroke.Width * 0.5f);
				float num3 = 6f;
				((SimplifiedGeometrySink)obj).BeginFigure(val13, (FigureBegin)0);
				obj.AddLine(new Vector2(num3, 0f - num3));
				obj.AddLine(new Vector2(0f - num3, 0f - num3));
				obj.AddLine(val13);
				((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj).Close();
			}
			((ChartObject)this).RenderTarget.Transform = transform;
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)ArrowPathGeometry, val8);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = antialiasMode;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			LineType = ChartLineType.Line;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolLine;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsBrowsable = true
			};
			StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsBrowsable = true
			};
			Stroke = new Stroke((Brush)(object)Brushes.CornflowerBlue, 2f);
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}
