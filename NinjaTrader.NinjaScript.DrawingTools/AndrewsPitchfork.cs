using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class AndrewsPitchfork : PriceLevelContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum AndrewsPitchforkCalculationMethod
	{
		StandardPitchfork,
		Schiff,
		ModifiedSchiff
	}

	private const int cursorSensitivity = 15;

	private ChartAnchor editingAnchor;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, ExtensionAnchor, EndAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke AnchorLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkCalculationMethod", GroupName = "NinjaScriptGeneral", Order = 4)]
	public AndrewsPitchforkCalculationMethod CalculationMethod { get; set; }

	[Display(Order = 3)]
	public ChartAnchor ExtensionAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkRetracement", GroupName = "NinjaScriptLines", Order = 2)]
	public Stroke RetracementLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkExtendLinesBack", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesBack { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
	public bool IsTextDisplayed { get; set; }

	public override object Icon => Icons.DrawAndrewsPitchfork;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
	public int PriceLevelOpacity { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	public override bool SupportsAlerts => true;

	protected void DrawPriceLevelText(double minX, double maxX, Point endPoint, PriceLevel priceLevel, ChartPanel panel)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		TextFormat val = ((SimpleFont)(((object)panel.ChartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
		string text = $"{priceLevel.Value / 100.0:P}";
		TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, text, val, (float)panel.H, val.FontSize);
		float height = val2.Metrics.Height;
		float width = val2.Metrics.Width;
		Point val3 = endPoint;
		double num = panel.X + panel.W;
		double num2 = panel.Y + panel.H;
		double num3 = panel.X;
		double num4 = panel.Y;
		if (((Point)(ref val3)).Y + (double)height >= num2)
		{
			((Point)(ref val3)).Y = num2 - (double)height;
		}
		if (((Point)(ref val3)).Y < num4)
		{
			((Point)(ref val3)).Y = num4;
		}
		if (((Point)(ref val3)).X + (double)width >= num)
		{
			((Point)(ref val3)).X = num - (double)width;
		}
		if (((Point)(ref val3)).X < num3)
		{
			((Point)(ref val3)).X = num3;
		}
		((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)((Point)(ref val3)).X, (float)((Point)(ref val3)).Y), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
		((DisposeBase)val).Dispose();
		((DisposeBase)val2).Dispose();
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.PriceLevels == null || base.PriceLevels.Count == 0)
		{
			yield break;
		}
		foreach (PriceLevel priceLevel in base.PriceLevels)
		{
			yield return new AlertConditionItem
			{
				Name = priceLevel.Name,
				ShouldOnlyDisplayName = true,
				Tag = priceLevel
			};
		}
	}

	private IEnumerable<Tuple<Point, Point>> GetAndrewsEndPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double totalPriceRange = EndAnchor.Price - ExtensionAnchor.Price;
		double startPrice = ExtensionAnchor.Price;
		Point anchorExtensionPoint = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point anchorStartPoint = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point anchorEndPoint = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point midPointExtension = new Point((((Point)(ref anchorExtensionPoint)).X + ((Point)(ref anchorEndPoint)).X) / 2.0, (((Point)(ref anchorExtensionPoint)).Y + ((Point)(ref anchorEndPoint)).Y) / 2.0);
		Point val2 = default(Point);
		Point val3 = default(Point);
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible))
		{
			double num = startPrice + item.Value / 100.0 * totalPriceRange;
			float num2 = chartScale.GetYByValue(num);
			float num3 = ((((Point)(ref anchorExtensionPoint)).X > ((Point)(ref anchorEndPoint)).X) ? ((float)(((Point)(ref anchorExtensionPoint)).X - Math.Abs((((Point)(ref anchorEndPoint)).X - ((Point)(ref anchorExtensionPoint)).X) * (item.Value / 100.0)))) : ((float)(((Point)(ref anchorExtensionPoint)).X + (((Point)(ref anchorEndPoint)).X - ((Point)(ref anchorExtensionPoint)).X) * (item.Value / 100.0))));
			((Point)(ref val2))._002Ector((double)num3, (double)num2);
			((Point)(ref val3))._002Ector(((Point)(ref val2)).X + (((Point)(ref midPointExtension)).X - ((Point)(ref anchorStartPoint)).X), ((Point)(ref val2)).Y + (((Point)(ref midPointExtension)).Y - ((Point)(ref anchorStartPoint)).Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(val2, val3);
			yield return new Tuple<Point, Point>(new Point(Math.Max(((Point)(ref extendedPoint)).X, 1.0), Math.Max(((Point)(ref extendedPoint)).Y, 1.0)), val2);
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		if (!((NinjaScript)this).IsVisible)
		{
			return null;
		}
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
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.No;
			}
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
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
			Point point4 = ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point val = new Point((((Point)(ref point3)).X + ((Point)(ref point4)).X) / 2.0, (((Point)(ref point3)).Y + ((Point)(ref point4)).Y) / 2.0);
			Vector val2 = point3 - point2;
			Vector val3 = point4 - point3;
			Vector val4 = val - point2;
			foreach (Tuple<Point, Point> andrewsEndPoint in GetAndrewsEndPoints(chartControl, chartScale))
			{
				Vector val5 = andrewsEndPoint.Item1 - andrewsEndPoint.Item2;
				if (MathHelper.IsPointAlongVector(point, andrewsEndPoint.Item2, val5, 15.0))
				{
					return ((DrawingTool)this).IsLocked ? Cursors.Arrow : Cursors.SizeAll;
				}
			}
			if (!MathHelper.IsPointAlongVector(point, point2, val2, 15.0) && !MathHelper.IsPointAlongVector(point, point3, val3, 15.0) && !MathHelper.IsPointAlongVector(point, point2, val4, 15.0))
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

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (!((NinjaScript)this).IsVisible)
		{
			return Array.Empty<Point>();
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		return (Point[])(object)new Point[4] { point, val2, point2, point3 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Invalid comparison between Unknown and I4
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Expected I4, but got Unknown
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Invalid comparison between Unknown and I4
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Invalid comparison between Unknown and I4
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Invalid comparison between Unknown and I4
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Invalid comparison between Unknown and I4
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Invalid comparison between Unknown and I4
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Invalid comparison between Unknown and I4
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point3)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point3)).Y + ((Point)(ref point2)).Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			((Point)(ref point))._002Ector(((Point)(ref point)).X, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			((Point)(ref point))._002Ector((((Point)(ref point2)).X + ((Point)(ref point)).X) / 2.0, (((Point)(ref point2)).Y + ((Point)(ref point)).Y) / 2.0);
		}
		double num = EndAnchor.Price - ExtensionAnchor.Price;
		double num2 = ExtensionAnchor.Price + priceLevel.Value / 100.0 * num;
		float num3 = chartScale.GetYByValue(num2);
		float num4 = ((((Point)(ref point3)).X > ((Point)(ref point2)).X) ? ((float)(((Point)(ref point3)).X - Math.Abs((((Point)(ref point2)).X - ((Point)(ref point3)).X) * (priceLevel.Value / 100.0)))) : ((float)(((Point)(ref point3)).X + (((Point)(ref point2)).X - ((Point)(ref point3)).X) * (priceLevel.Value / 100.0))));
		Point alertStartPoint = new Point((double)num4, (double)num3);
		Point val3 = default(Point);
		((Point)(ref val3))._002Ector(((Point)(ref alertStartPoint)).X + (((Point)(ref val2)).X - ((Point)(ref point)).X), ((Point)(ref alertStartPoint)).Y + (((Point)(ref val2)).Y - ((Point)(ref point)).Y));
		Point alertEndPoint = ((DrawingTool)this).GetExtendedPoint(alertStartPoint, val3);
		double num5 = (((int)values[0].ValueType == 12) ? num4 : ((float)chartControl.GetXByTime(values[0].Time)));
		double num6 = chartScale.GetYByValue(values[0].Value);
		Point val4 = default(Point);
		((Point)(ref val4))._002Ector(num5, num6);
		if (IsExtendedLinesBack)
		{
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(alertEndPoint, alertStartPoint);
			if (((Point)(ref extendedPoint)).X > -1.0 || ((Point)(ref extendedPoint)).Y > -1.0)
			{
				alertStartPoint = extendedPoint;
			}
		}
		if (num5 < ((Point)(ref alertStartPoint)).X || num5 > ((Point)(ref alertEndPoint)).X)
		{
			return false;
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, val4);
		Condition val5 = condition;
		switch ((int)val5)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation != 0)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if ((int)pointLineLocation != 1)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 2:
			return (int)pointLineLocation == 2;
		case 7:
			return (int)pointLineLocation != 2;
		case 0:
		case 1:
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		default:
			return false;
		}
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
			double num7 = chartControl.GetXByTime(v.Time);
			double num8 = chartScale.GetYByValue(v.Value);
			Point val6 = default(Point);
			((Point)(ref val6))._002Ector(num7, num8);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, val6);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		bool flag2 = false;
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			if (anchor.IsEditing)
			{
				return true;
			}
			if (anchor.Time >= firstTimeOnChart && anchor.Time <= lastTimeOnChart)
			{
				return true;
			}
			if (anchor.Time < firstTimeOnChart)
			{
				flag = true;
			}
			else if (anchor.Time > lastTimeOnChart)
			{
				flag2 = true;
			}
			if (flag && flag2)
			{
				return true;
			}
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point3)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point3)).Y + ((Point)(ref point2)).Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			((Point)(ref point))._002Ector(((Point)(ref point)).X, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			((Point)(ref point))._002Ector((((Point)(ref point2)).X + ((Point)(ref point)).X) / 2.0, (((Point)(ref point2)).Y + ((Point)(ref point)).Y) / 2.0);
		}
		double num = EndAnchor.Price - ExtensionAnchor.Price;
		double price = ExtensionAnchor.Price;
		Point val3 = default(Point);
		Point val4 = default(Point);
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible && pl.Stroke != null))
		{
			double num2 = price + item.Value / 100.0 * num;
			float num3 = chartScale.GetYByValue(num2);
			float num4 = ((!(((Point)(ref point3)).X > ((Point)(ref point2)).X)) ? ((item.Value >= 0.0) ? ((float)(((Point)(ref point3)).X + (((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0))) : ((float)(((Point)(ref point3)).X - Math.Abs((((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0))))) : ((item.Value >= 0.0) ? ((float)(((Point)(ref point3)).X - Math.Abs((((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0)))) : ((float)(((Point)(ref point3)).X + (((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0)))));
			((Point)(ref val3))._002Ector((double)num4, (double)num3);
			((Point)(ref val4))._002Ector(((Point)(ref val3)).X + (((Point)(ref val2)).X - ((Point)(ref point)).X), ((Point)(ref val3)).Y + (((Point)(ref val2)).Y - ((Point)(ref point)).Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(val3, val4);
			double num5 = 5.0;
			Point[] array = (Point[])(object)new Point[3] { val3, extendedPoint, val4 };
			for (int num6 = 0; num6 < array.Length; num6++)
			{
				Point val5 = array[num6];
				if (((Point)(ref val5)).X >= (double)val.X - num5 && ((Point)(ref val5)).X <= (double)(val.W + val.X) + num5 && ((Point)(ref val5)).Y >= (double)val.Y - num5 && ((Point)(ref val5)).Y <= (double)(val.Y + val.H) + num5)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible)
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, anchor.Price);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, anchor.Price);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNESW || ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNWSE)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
			}
		}
		else
		{
			if (StartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(StartAnchor);
				dataPoint.CopyDataValues(EndAnchor);
				StartAnchor.IsEditing = false;
			}
			else if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				dataPoint.CopyDataValues(ExtensionAnchor);
				EndAnchor.IsEditing = false;
			}
			else if (ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
				ExtensionAnchor.IsEditing = false;
			}
			if (((DrawingTool)this).Anchors.All((ChartAnchor a) => !a.IsEditing))
			{
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
			}
			else if (ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 3)
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
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0551: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0760: Unknown result type (might be due to invalid IL or missing references)
		//IL_055d: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Expected O, but got Unknown
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Expected O, but got Unknown
		//IL_0672: Unknown result type (might be due to invalid IL or missing references)
		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_068e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0690: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point3)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point3)).Y + ((Point)(ref point2)).Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			((Point)(ref point))._002Ector(((Point)(ref point)).X, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			((Point)(ref point))._002Ector((((Point)(ref point2)).X + ((Point)(ref point)).X) / 2.0, (((Point)(ref point2)).Y + ((Point)(ref point)).Y) / 2.0);
		}
		AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		RetracementLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		double num = ((AnchorLineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		Vector val3 = default(Vector);
		((Vector)(ref val3))._002Ector(num, num);
		Vector2 val4 = DxExtensions.ToVector2(point + val3);
		Vector2 val5 = DxExtensions.ToVector2(point2 + val3);
		Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX);
		Vector2 val7 = DxExtensions.ToVector2(StartAnchor.GetPoint(chartControl, val, chartScale, true) + val3);
		((ChartObject)this).RenderTarget.DrawLine(val7, val5, val6, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
		if (ExtensionAnchor.IsEditing && EndAnchor.IsEditing)
		{
			return;
		}
		Vector2 val8 = DxExtensions.ToVector2(point3);
		val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : RetracementLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val5, val8, val6, RetracementLineStroke.Width, RetracementLineStroke.StrokeStyle);
		if (((ChartObject)this).IsInHitTest || base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		double num2 = EndAnchor.Price - ExtensionAnchor.Price;
		double price = ExtensionAnchor.Price;
		float val9 = float.MaxValue;
		float val10 = float.MinValue;
		Point val11 = default(Point);
		((Point)(ref val11))._002Ector(0.0, 0.0);
		Point val12 = default(Point);
		((Point)(ref val12))._002Ector(0.0, 0.0);
		Stroke val13 = null;
		List<Tuple<PriceLevel, Point>> list = new List<Tuple<PriceLevel, Point>>();
		Point val14 = default(Point);
		Point val15 = default(Point);
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			double num3 = price + item.Value / 100.0 * num2;
			float num4 = chartScale.GetYByValue(num3);
			float num5 = ((!(((Point)(ref point3)).X > ((Point)(ref point2)).X)) ? ((item.Value >= 0.0) ? ((float)(((Point)(ref point3)).X + (((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0))) : ((float)(((Point)(ref point3)).X - Math.Abs((((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0))))) : ((item.Value >= 0.0) ? ((float)(((Point)(ref point3)).X - Math.Abs((((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0)))) : ((float)(((Point)(ref point3)).X + (((Point)(ref point2)).X - ((Point)(ref point3)).X) * (item.Value / 100.0)))));
			((Point)(ref val14))._002Ector((double)num5, (double)num4);
			((Point)(ref val15))._002Ector(((Point)(ref val14)).X + (((Point)(ref val2)).X - ((Point)(ref point)).X), ((Point)(ref val14)).Y + (((Point)(ref val2)).Y - ((Point)(ref point)).Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(val14, val15);
			if (Math.Abs(item.Value - 50.0) < 1E-16)
			{
				((ChartObject)this).RenderTarget.DrawLine(IsExtendedLinesBack ? DxExtensions.ToVector2(((DrawingTool)this).GetExtendedPoint(val15, val14)) : val4, DxExtensions.ToVector2(extendedPoint), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			else
			{
				((ChartObject)this).RenderTarget.DrawLine(IsExtendedLinesBack ? DxExtensions.ToVector2(((DrawingTool)this).GetExtendedPoint(val15, val14)) : DxExtensions.ToVector2(val14), DxExtensions.ToVector2(extendedPoint), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			if (val13 == null)
			{
				val13 = new Stroke();
			}
			else
			{
				PathGeometry val16 = new PathGeometry(Globals.D2DFactory);
				GeometrySink val17 = val16.Open();
				((SimplifiedGeometrySink)val17).BeginFigure(DxExtensions.ToVector2(val11), (FigureBegin)0);
				if (Math.Abs(((Point)(ref val11)).Y - ((Point)(ref extendedPoint)).Y) > 0.0 && Math.Abs(((Point)(ref val11)).X - ((Point)(ref extendedPoint)).X) > 0.0)
				{
					double y;
					double x;
					if (((Point)(ref val11)).Y <= (double)((ChartObject)this).ChartPanel.Y || ((Point)(ref val11)).Y >= (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
					{
						y = ((Point)(ref val11)).Y;
						x = ((Point)(ref extendedPoint)).X;
					}
					else
					{
						y = ((Point)(ref extendedPoint)).Y;
						x = ((Point)(ref val11)).X;
					}
					val17.AddLine(new Vector2((float)x, (float)y));
				}
				val17.AddLine(DxExtensions.ToVector2(extendedPoint));
				val17.AddLine(DxExtensions.ToVector2(val14));
				val17.AddLine(DxExtensions.ToVector2(val12));
				((SimplifiedGeometrySink)val17).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)val17).Close();
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val16, val13.BrushDX);
				((DisposeBase)val16).Dispose();
			}
			if (IsTextDisplayed)
			{
				list.Add(new Tuple<PriceLevel, Point>(item, extendedPoint));
			}
			item.Stroke.CopyTo(val13);
			val13.Opacity = PriceLevelOpacity;
			val12 = val14;
			val11 = extendedPoint;
			val9 = Math.Min(num4, val9);
			val10 = Math.Max(num4, val10);
		}
		if (!IsTextDisplayed)
		{
			return;
		}
		foreach (Tuple<PriceLevel, Point> item2 in list)
		{
			DrawPriceLevelText(0.0, 0.0, item2.Item2, item2.Item1, val);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		((DrawingTool)this).OnStateChange();
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
			else if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, (Brush)(object)Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(50.0, (Brush)(object)Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(100.0, (Brush)(object)Brushes.SeaGreen));
			}
			return;
		}
		AnchorLineStroke = new Stroke((Brush)(object)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
		RetracementLineStroke = new Stroke((Brush)(object)Brushes.SeaGreen, (DashStyleHelper)0, 2f);
		((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolAndrewsPitchforkDescription;
		((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolAndrewsPitchfork;
		StartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		ExtensionAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		EndAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
		EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
		ExtensionAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorExtension;
		PriceLevelOpacity = 5;
		IsTextDisplayed = true;
	}
}
