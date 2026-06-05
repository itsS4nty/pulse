using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class GannFan : GannAngleContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum GannFanDirection
	{
		UpLeft,
		UpRight,
		DownLeft,
		DownRight
	}

	public ChartAnchor Anchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanFanDirection", GroupName = "NinjaScriptGeneral", Order = 3)]
	public GannFanDirection FanDirection { get; set; }

	public override object Icon => Icons.DrawGanFan;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanDisplayText", GroupName = "NinjaScriptGeneral", Order = 2)]
	public bool IsTextDisplayed { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanPointsPerBar", GroupName = "NinjaScriptGeneral", Order = 4)]
	public double PointsPerBar { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
	public int PriceLevelOpacity { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { Anchor };

	public override bool SupportsAlerts => true;

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible && !Anchor.IsEditing)
		{
			double minValue = (((ChartObject)this).MaxValue = Anchor.Price);
			((ChartObject)this).MinValue = minValue;
		}
	}

	public Point CalculateExtendedDataPoint(ChartPanel panel, ChartScale scale, int startX, double startPrice, Vector slope)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = ((Vector)(ref slope)).X > 0.0;
		bool flag2 = ((Vector)(ref slope)).Y > 0.0;
		double num = Math.Abs((double)(flag ? (panel.W - startX) : (panel.X + startX)) / ((Vector)(ref slope)).X) * ((Vector)(ref slope)).Y;
		double num2 = startPrice + num;
		double num3 = (flag2 ? panel.MaxValue : panel.MinValue);
		if (flag2 ? (num2 > num3) : (num3 > num2))
		{
			double num4 = Math.Abs(Math.Abs(num3 - startPrice) / ((Vector)(ref slope)).Y) * ((Vector)(ref slope)).X;
			return new Point((double)startX + num4, (double)scale.GetYByValue(num3));
		}
		return new Point((double)(flag ? panel.W : 0), (double)scale.GetYByValue(num2));
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return Cursors.Pen;
		}
		if ((int)((DrawingTool)this).DrawingState == 3)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		}
		Point point2 = Anchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Vector val = point - point2;
		if ((int)((DrawingTool)this).DrawingState == 1 || ((Vector)(ref val)).Length <= 10.0)
		{
			if (((DrawingTool)this).IsLocked)
			{
				if ((int)((DrawingTool)this).DrawingState != 1)
				{
					return Cursors.Arrow;
				}
				return Cursors.No;
			}
			return Cursors.SizeNESW;
		}
		foreach (Point gannEndPoint in GetGannEndPoints(chartControl, chartScale))
		{
			Vector val2 = gannEndPoint - point2;
			if (MathHelper.IsPointAlongVector(point, point2, val2, 10.0))
			{
				if (((DrawingTool)this).IsLocked)
				{
					return ((int)((DrawingTool)this).DrawingState == 1) ? Cursors.No : Cursors.Arrow;
				}
				return Cursors.SizeAll;
			}
		}
		return null;
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.GannAngles == null)
		{
			yield break;
		}
		foreach (GannAngle gannAngle in base.GannAngles)
		{
			yield return new AlertConditionItem
			{
				Name = gannAngle.Name,
				Tag = gannAngle,
				ShouldOnlyDisplayName = true
			};
		}
	}

	private IEnumerable<Point> GetGannEndPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point anchorPoint = Anchor.GetPoint(chartControl, val, chartScale, true);
		foreach (GannAngle item in base.GannAngles.Where((GannAngle ga) => ga.IsVisible))
		{
			double deltaX = item.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice = item.RatioY * PointsPerBar;
			Point gannStepPoint = GetGannStepPoint(chartScale, ((Point)(ref anchorPoint)).X, Anchor.Price, deltaX, deltaPrice);
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(anchorPoint, gannStepPoint);
			yield return new Point(Math.Max(((Point)(ref extendedPoint)).X, 1.0), Math.Max(((Point)(ref extendedPoint)).Y, 1.0));
		}
	}

	private Point GetGannStepPoint(ChartScale scale, double startX, double startPrice, double deltaX, double deltaPrice)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		double num;
		double num2;
		switch (FanDirection)
		{
		case GannFanDirection.DownLeft:
			num = startX - deltaX;
			num2 = startPrice - deltaPrice;
			break;
		case GannFanDirection.DownRight:
			num = startX + deltaX;
			num2 = startPrice - deltaPrice;
			break;
		case GannFanDirection.UpLeft:
			num = startX - deltaX;
			num2 = startPrice + deltaPrice;
			break;
		default:
			num = startX + deltaX;
			num2 = startPrice + deltaPrice;
			break;
		}
		return new Point(num, (double)scale.GetYByValue(num2));
	}

	private Vector GetGannStepDataVector(double deltaX, double deltaPrice)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		return (Vector)(FanDirection switch
		{
			GannFanDirection.DownLeft => new Vector(0.0 - deltaX, 0.0 - deltaPrice), 
			GannFanDirection.DownRight => new Vector(Math.Abs(deltaX), 0.0 - deltaPrice), 
			GannFanDirection.UpLeft => new Vector(0.0 - deltaX, Math.Abs(deltaPrice)), 
			_ => new Vector(Math.Abs(deltaX), Math.Abs(deltaPrice)), 
		});
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = Anchor.GetPoint(chartControl, val, chartScale, true);
		return (Point[])(object)new Point[1] { point };
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		Condition[] array = new Condition[8];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		return (IEnumerable<Condition>)(object)array;
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Invalid comparison between Unknown and I4
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Expected I4, but got Unknown
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Invalid comparison between Unknown and I4
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Invalid comparison between Unknown and I4
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Invalid comparison between Unknown and I4
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Invalid comparison between Unknown and I4
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Invalid comparison between Unknown and I4
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is GannAngle gannAngle))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point anchorPoint = Anchor.GetPoint(chartControl, val, chartScale, true);
		double deltaX = gannAngle.RatioX * (double)chartControl.Properties.BarDistance;
		double deltaPrice = chartScale.GetPixelsForDistance(gannAngle.RatioY * chartControl.Instrument.MasterInstrument.TickSize);
		Point gannStepPoint = GetGannStepPoint(chartScale, ((Point)(ref anchorPoint)).X, Anchor.Price, deltaX, deltaPrice);
		Point extendedEndPoint = ((DrawingTool)this).GetExtendedPoint(anchorPoint, gannStepPoint);
		if ((int)values[0].ValueType == 11)
		{
			int xByTime = chartControl.GetXByTime(values[0].Time);
			if (!(((Point)(ref gannStepPoint)).X >= (double)xByTime))
			{
				return ((Point)(ref gannStepPoint)).X >= (double)xByTime;
			}
			return true;
		}
		double num = chartControl.GetXByTime(values[0].Time);
		double num2 = chartScale.GetYByValue(values[0].Value);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector(num, num2);
		if (((Point)(ref extendedEndPoint)).X < num)
		{
			return false;
		}
		if (((Point)(ref gannStepPoint)).X > num2)
		{
			return false;
		}
		Condition val3 = condition;
		if ((int)val3 <= 1)
		{
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(anchorPoint, extendedEndPoint, val2);
		val3 = condition;
		return (val3 - 2) switch
		{
			1 => (int)pointLineLocation == 0, 
			2 => ((int)pointLineLocation == 0 || (int)pointLineLocation == 2) ? true : false, 
			3 => (int)pointLineLocation == 1, 
			4 => pointLineLocation - 1 <= 1, 
			0 => (int)pointLineLocation == 2, 
			5 => (int)pointLineLocation != 2, 
			_ => false, 
		};
		bool Predicate(ChartAlertValue v)
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
			double num3 = chartControl.GetXByTime(v.Time);
			double num4 = chartScale.GetYByValue(v.Value);
			Point val4 = default(Point);
			((Point)(ref val4))._002Ector(num3, num4);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(anchorPoint, extendedEndPoint, val4);
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
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		if (Anchor.Time >= firstTimeOnChart && Anchor.Time <= lastTimeOnChart)
		{
			return true;
		}
		bool flag = Anchor.Time > lastTimeOnChart;
		if (flag)
		{
			GannFanDirection fanDirection = FanDirection;
			bool flag2 = ((fanDirection == GannFanDirection.UpLeft || fanDirection == GannFanDirection.DownLeft) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return true;
		}
		flag = Anchor.Time < firstTimeOnChart;
		if (flag)
		{
			GannFanDirection fanDirection = FanDirection;
			bool flag2 = ((fanDirection == GannFanDirection.UpRight || fanDirection == GannFanDirection.DownRight) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
				if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 10.0, point) == Anchor)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, point) == Cursors.SizeAll)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else
				{
					((ChartObject)this).IsSelected = false;
				}
			}
		}
		else
		{
			if (PointsPerBar < 0.0)
			{
				PointsPerBar = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize;
			}
			dataPoint.CopyDataValues(Anchor);
			Anchor.IsEditing = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		if (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 0)
		{
			if ((int)((DrawingTool)this).DrawingState == 1)
			{
				dataPoint.CopyDataValues(Anchor);
			}
			else if ((int)((DrawingTool)this).DrawingState == 3)
			{
				Anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		((DrawingTool)this).DrawingState = (DrawingState)2;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		State state = ((NinjaScript)this).State;
		if ((int)state != 1)
		{
			if ((int)state != 2)
			{
				if ((int)state == 8)
				{
					((DrawingTool)this).Dispose();
				}
			}
			else if (base.GannAngles.Count == 0)
			{
				Brush[] array = (Brush[])(object)new Brush[9]
				{
					(Brush)Brushes.Red,
					(Brush)Brushes.MediumOrchid,
					(Brush)Brushes.DarkSlateBlue,
					(Brush)Brushes.SteelBlue,
					(Brush)Brushes.Gray,
					(Brush)Brushes.MediumAquamarine,
					(Brush)Brushes.Khaki,
					(Brush)Brushes.Coral,
					(Brush)Brushes.Red
				};
				for (int i = 0; i < 9; i++)
				{
					int num = ((i == 8) ? 8 : ((i <= 4) ? 1 : (i - 3)));
					int num2 = ((i == 0) ? 8 : ((i > 4) ? 1 : (5 - i)));
					base.GannAngles.Add(new GannAngle(num, num2, array[i % 8]));
				}
			}
		}
		else
		{
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolGannFan;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolGannFan;
			Anchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true
			};
			FanDirection = GannFanDirection.UpRight;
			PriceLevelOpacity = 5;
			IsTextDisplayed = true;
			PointsPerBar = -1.0;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected O, but got Unknown
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Expected O, but got Unknown
		//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_0540: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_065b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_0677: Unknown result type (might be due to invalid IL or missing references)
		//IL_0696: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = Anchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector(0.0, 0.0);
		Brush val3 = null;
		Vector val5 = default(Vector);
		foreach (GannAngle item in from ga in base.GannAngles
			where ga.IsVisible && ga.Stroke != null
			orderby ga.RatioX / ga.RatioY
			select ga)
		{
			item.Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
			double deltaX = item.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice = item.RatioY * PointsPerBar;
			Vector gannStepDataVector = GetGannStepDataVector(deltaX, deltaPrice);
			Point val4 = CalculateExtendedDataPoint(val, chartScale, Convert.ToInt32(((Point)(ref point)).X), Anchor.Price, gannStepDataVector);
			double num = ((MathExtentions.ApproxCompare((double)(item.Stroke.Width % 2f), 0.0) == 0) ? 0.5 : 0.0);
			((Vector)(ref val5))._002Ector(0.0, num);
			Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : item.Stroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point + val5), DxExtensions.ToVector2(val4 + val5), val6, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (val3 != null)
			{
				float opacity = val3.Opacity;
				val3.Opacity = (float)PriceLevelOpacity / 100f;
				PathGeometry val7 = new PathGeometry(Globals.D2DFactory);
				GeometrySink val8 = val7.Open();
				((SimplifiedGeometrySink)val8).BeginFigure(DxExtensions.ToVector2(val2), (FigureBegin)0);
				if (Math.Abs(((Point)(ref val2)).Y - ((Point)(ref val4)).Y) > 0.1 && Math.Abs(((Point)(ref val2)).X - ((Point)(ref val4)).X) > 0.1)
				{
					double y;
					double x;
					if (((Point)(ref val2)).Y <= (double)((ChartObject)this).ChartPanel.Y || ((Point)(ref val2)).Y >= (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
					{
						GannFanDirection fanDirection = FanDirection;
						if ((uint)fanDirection <= 1u)
						{
							y = ((Point)(ref val4)).Y;
							x = ((Point)(ref val2)).X;
						}
						else
						{
							y = ((Point)(ref val2)).Y;
							x = ((Point)(ref val4)).X;
						}
					}
					else
					{
						GannFanDirection fanDirection = FanDirection;
						if ((uint)fanDirection <= 1u)
						{
							y = ((Point)(ref val2)).Y;
							x = ((Point)(ref val4)).X;
						}
						else
						{
							y = ((Point)(ref val4)).Y;
							x = ((Point)(ref val2)).X;
						}
					}
					val8.AddLine(new Vector2((float)x, (float)y));
				}
				val8.AddLine(DxExtensions.ToVector2(val4));
				val8.AddLine(DxExtensions.ToVector2(point + val5));
				val8.AddLine(DxExtensions.ToVector2(val2));
				((SimplifiedGeometrySink)val8).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)val8).Close();
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val7, val3);
				((DisposeBase)val7).Dispose();
				val3.Opacity = opacity;
			}
			val2 = val4 + val5;
			val3 = val6;
		}
		if (!IsTextDisplayed || ((ChartObject)this).IsInHitTest)
		{
			return;
		}
		Point val12 = default(Point);
		foreach (GannAngle item2 in from ga in base.GannAngles
			where ga.IsVisible && ga.Stroke != null
			orderby ga.RatioX / ga.RatioY
			select ga)
		{
			item2.Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
			double deltaX2 = item2.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice2 = item2.RatioY * PointsPerBar;
			Vector gannStepDataVector2 = GetGannStepDataVector(deltaX2, deltaPrice2);
			Point val9 = CalculateExtendedDataPoint(val, chartScale, Convert.ToInt32(((Point)(ref point)).X), Anchor.Price, gannStepDataVector2);
			if (!IsTextDisplayed || ((ChartObject)this).IsInHitTest)
			{
				continue;
			}
			TextFormat val10 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val10.TextAlignment = (TextAlignment)0;
			val10.WordWrapping = (WordWrapping)1;
			TextLayout val11 = new TextLayout(Globals.DirectWriteFactory, item2.Name, val10, 100f, val10.FontSize);
			float height = val11.Metrics.Height;
			((Point)(ref val12))._002Ector(((Point)(ref val9)).X, ((Point)(ref val9)).Y);
			if (((Point)(ref val12)).X > (double)((float)(val.X + val.W) - val11.Metrics.Width))
			{
				((Point)(ref val12)).X = (float)(val.X + val.W) - val11.Metrics.Width;
				((Point)(ref val12)).Y = ((Point)(ref val12)).Y + (double)val11.Metrics.Width;
			}
			if (((Vector)(ref gannStepDataVector2)).Y > 0.0)
			{
				if (((Point)(ref val12)).Y < (double)val.Y + (double)height * 0.5)
				{
					((Point)(ref val12)).Y = (double)val.Y + (double)height * 0.5;
				}
			}
			else if (((Point)(ref val12)).Y > (double)(val.Y + val.H) - (double)height * 1.5)
			{
				((Point)(ref val12)).Y = (double)(val.Y + val.H) - (double)height * 1.5;
			}
			float num2 = 2f + ((Application.Current.FindResource((object)"FontModalTitleMargin") as float?) ?? 3f);
			GannFanDirection fanDirection = FanDirection;
			bool flag = ((fanDirection == GannFanDirection.UpLeft || fanDirection == GannFanDirection.DownLeft) ? true : false);
			float num3 = (flag ? num2 : (-2f * num2));
			Matrix3x2 transform = Matrix3x2.Translation(new Vector2((float)((Point)(ref val12)).X, (float)((Point)(ref val12)).Y));
			((ChartObject)this).RenderTarget.Transform = transform;
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(num3 + num2, num2), val11, item2.Stroke.BrushDX, (DrawTextOptions)1);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
			((DisposeBase)val10).Dispose();
			((DisposeBase)val11).Dispose();
		}
	}
}
