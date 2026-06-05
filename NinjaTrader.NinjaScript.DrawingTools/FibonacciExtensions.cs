using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

public class FibonacciExtensions : FibonacciRetracements
{
	private Point anchorExtensionPoint;

	[Display(Order = 3)]
	public ChartAnchor ExtensionAnchor { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { base.StartAnchor, base.EndAnchor, ExtensionAnchor };

	public override object Icon => Icons.DrawFbExtensions;

	protected new Tuple<Point, Point> GetPriceLevelLinePoints(PriceLevel priceLevel, ChartControl chartControl, ChartScale chartScale, bool isInverted)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
		double num = Math.Min(((Point)(ref point)).X, ((Point)(ref point2)).X);
		double num2 = Math.Max(((Point)(ref point)).X, ((Point)(ref point2)).X);
		double num3 = (base.IsExtendedLinesLeft ? ((double)val.X) : num);
		double num4 = (base.IsExtendedLinesRight ? ((double)(val.X + val.W)) : num2);
		double num5 = priceLevel.GetY(chartScale, ExtensionAnchor.Price, totalPriceRange, isInverted);
		return new Tuple<Point, Point>(new Point(num3, num5), new Point(num4, num5));
	}

	private new void DrawPriceLevelText(ChartPanel chartPanel, ChartScale _, double minX, double maxX, double y, double price, PriceLevel priceLevel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if ((int)base.TextLocation == 4)
		{
			return;
		}
		object obj;
		if (priceLevel == null)
		{
			obj = null;
		}
		else
		{
			Stroke stroke = priceLevel.Stroke;
			obj = ((stroke != null) ? stroke.BrushDX : null);
		}
		if (obj != null)
		{
			TextFormat val = ((SimpleFont)(((object)chartPanel.ChartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val.TextAlignment = (TextAlignment)0;
			val.WordWrapping = (WordWrapping)1;
			string priceString = GetPriceString(price, priceLevel, chartPanel);
			float num = (float)Math.Abs(maxX - minX);
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, priceString, val, num, val.FontSize);
			double num2;
			if (base.IsExtendedLinesLeft && (int)base.TextLocation == 1)
			{
				num2 = (double)chartPanel.X + 2.0;
			}
			else if (base.IsExtendedLinesRight && (int)base.TextLocation == 3)
			{
				num2 = (float)(chartPanel.X + chartPanel.W) - val2.Metrics.Width;
			}
			else
			{
				TextLocation textLocation = base.TextLocation;
				bool flag = (int)textLocation <= 1;
				num2 = ((!flag) ? ((minX > maxX) ? (minX - (double)val2.Metrics.Width) : (maxX - (double)val2.Metrics.Width)) : ((minX <= maxX) ? (minX - 1.0) : (maxX - 1.0)));
			}
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)num2, (float)(y - (double)val.FontSize - 2.0)), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
			((DisposeBase)val).Dispose();
			((DisposeBase)val2).Dispose();
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 2)
		{
			return base.GetCursor(chartControl, chartPanel, chartScale, point);
		}
		Point point2 = base.StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
		if (closestAnchor != null)
		{
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.Arrow;
			}
			if (closestAnchor != base.StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		}
		Point point3 = base.EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point4 = ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
		Vector item = point3 - point2;
		Vector item2 = point4 - point3;
		Vector item3 = translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1;
		if (new Tuple<Vector, Point>[3]
		{
			new Tuple<Vector, Point>(item, point2),
			new Tuple<Vector, Point>(item2, point3),
			new Tuple<Vector, Point>(item3, translatedExtensionYLine.Item1)
		}.Any((Tuple<Vector, Point> chkTup) => MathHelper.IsPointAlongVector(point, chkTup.Item2, chkTup.Item1, 15.0)))
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.Arrow;
		}
		return null;
	}

	private Point GetEndLineMidpoint(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		return new Point((((Point)(ref point)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
	}

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Point[] selectionPoints = base.GetSelectionPoints(chartControl, chartScale);
		if (!ExtensionAnchor.IsEditing || !base.EndAnchor.IsEditing)
		{
			Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
			Point val = translatedExtensionYLine.Item1 + (translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1) / 2.0;
			Point endLineMidpoint = GetEndLineMidpoint(chartControl, chartScale);
			return selectionPoints.Union((IEnumerable<Point>)(object)new Point[4] { translatedExtensionYLine.Item1, translatedExtensionYLine.Item2, val, endLineMidpoint }).ToArray();
		}
		return selectionPoints;
	}

	private string GetPriceString(double price, PriceLevel priceLevel, ChartPanel _)
	{
		string text = price.ToString(Globals.GetTickFormatString(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize));
		return (priceLevel.Value / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture) + " (" + text + ")";
	}

	private Tuple<Point, Point> GetTranslatedExtensionYLine(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = double.MaxValue;
		Point val4 = default(Point);
		foreach (Tuple<Point, Point> item in from pl in base.PriceLevels
			where pl.IsVisible
			select GetPriceLevelLinePoints(pl, chartControl, chartScale, isInverted: false))
		{
			Vector val2 = point - point2;
			Point val3 = item.Item1 + val2;
			double x = ((Point)(ref val3)).X;
			val3 = item.Item1;
			((Point)(ref val4))._002Ector(x, ((Point)(ref val3)).Y);
			num = Math.Min(((Point)(ref val4)).Y, num);
		}
		if (MathExtentions.ApproxCompare(num, double.MaxValue) == 0)
		{
			return new Tuple<Point, Point>(new Point(((Point)(ref point)).X, ((Point)(ref point)).Y), new Point(((Point)(ref point)).X, ((Point)(ref point)).Y));
		}
		return new Tuple<Point, Point>(new Point(((Point)(ref point)).X, num), new Point(((Point)(ref point)).X, ((Point)(ref anchorExtensionPoint)).Y));
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, isInverted: false);
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Vector val2 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true) - point;
		Point lineStartPoint = priceLevelLinePoints.Item1 + val2;
		Point lineEndPoint = priceLevelLinePoints.Item2 + val2;
		return CheckAlertRetracementLine(condition, lineStartPoint, lineEndPoint, chartControl, chartScale, values);
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
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Invalid comparison between Unknown and I4
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			base.OnMouseDown(chartControl, chartPanel, chartScale, dataPoint);
			if ((int)((DrawingTool)this).DrawingState == 2)
			{
				Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
				Vector val = translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1;
				if (MathHelper.IsPointAlongVector(new Point(((Point)(ref point)).X, (double)((DrawingTool)this).ConvertToVerticalPixels(chartControl, chartPanel, ((Point)(ref point)).Y)), translatedExtensionYLine.Item1, val, 15.0))
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else
				{
					((ChartObject)this).IsSelected = false;
				}
			}
			return;
		}
		if (base.StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(base.StartAnchor);
			dataPoint.CopyDataValues(base.EndAnchor);
			base.StartAnchor.IsEditing = false;
		}
		else if (base.EndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(base.EndAnchor);
			base.EndAnchor.IsEditing = false;
			dataPoint.CopyDataValues(ExtensionAnchor);
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

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 0)
		{
			base.OnMouseMove(chartControl, chartPanel, chartScale, dataPoint);
			if ((int)((DrawingTool)this).DrawingState == 0 && ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.AnchorLineStroke = new Stroke((Brush)(object)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolFibonacciExtensions;
			base.PriceLevelOpacity = 5;
			base.StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			ExtensionAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			base.EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			base.StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			base.EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
			ExtensionAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorExtension;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, (Brush)(object)Brushes.DarkGray));
				base.PriceLevels.Add(new PriceLevel(23.6, (Brush)(object)Brushes.DodgerBlue));
				base.PriceLevels.Add(new PriceLevel(38.2, (Brush)(object)Brushes.CornflowerBlue));
				base.PriceLevels.Add(new PriceLevel(50.0, (Brush)(object)Brushes.SteelBlue));
				base.PriceLevels.Add(new PriceLevel(61.8, (Brush)(object)Brushes.DarkCyan));
				base.PriceLevels.Add(new PriceLevel(76.4, (Brush)(object)Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(100.0, (Brush)(object)Brushes.DarkGray));
			}
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Expected O, but got Unknown
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		anchorExtensionPoint = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		base.AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		double num = ((MathExtentions.ApproxCompare((double)base.AnchorLineStroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
		Vector val2 = default(Vector);
		((Vector)(ref val2))._002Ector(num, num);
		Vector2 val3 = DxExtensions.ToVector2(point + val2);
		Vector2 val4 = DxExtensions.ToVector2(point2 + val2);
		((ChartObject)this).RenderTarget.DrawLine(val3, val4, base.AnchorLineStroke.BrushDX, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (ExtensionAnchor.IsEditing && base.EndAnchor.IsEditing)
		{
			return;
		}
		Vector2 val5 = DxExtensions.ToVector2(anchorExtensionPoint);
		Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : base.AnchorLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val4, val5, val6, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		double num2 = 3.4028234663852886E+38;
		double num3 = -3.4028234663852886E+38;
		Point val7 = default(Point);
		((Point)(ref val7))._002Ector(0.0, 0.0);
		Stroke val8 = null;
		int num4 = 0;
		Point val12;
		Point val13 = default(Point);
		Vector val15 = default(Vector);
		RectangleF val18 = default(RectangleF);
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(item, chartControl, chartScale, isInverted: false);
			Vector val9 = anchorExtensionPoint - point;
			Point val10 = priceLevelLinePoints.Item1 + val9;
			Point val11 = priceLevelLinePoints.Item2 + val9;
			double x;
			if (!base.IsExtendedLinesLeft)
			{
				x = ((Point)(ref val10)).X;
			}
			else
			{
				val12 = priceLevelLinePoints.Item1;
				x = ((Point)(ref val12)).X;
			}
			double num5 = x;
			double x2;
			if (!base.IsExtendedLinesRight)
			{
				x2 = ((Point)(ref val11)).X;
			}
			else
			{
				val12 = priceLevelLinePoints.Item2;
				x2 = ((Point)(ref val12)).X;
			}
			double num6 = x2;
			val12 = priceLevelLinePoints.Item1;
			((Point)(ref val13))._002Ector(num5, ((Point)(ref val12)).Y);
			val12 = priceLevelLinePoints.Item2;
			Point val14 = new Point(num6, ((Point)(ref val12)).Y);
			double num7 = ((MathExtentions.ApproxCompare((double)item.Stroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
			((Vector)(ref val15))._002Ector(num7, num7);
			Point val16 = val13 + val15;
			Point val17 = val14 + val15;
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val16), DxExtensions.ToVector2(val17), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (val8 == null)
			{
				val8 = new Stroke();
			}
			else if (!((ChartObject)this).IsInHitTest)
			{
				((RectangleF)(ref val18))._002Ector((float)((Point)(ref val7)).X, (float)((Point)(ref val7)).Y, (float)(((Point)(ref val17)).X - ((Point)(ref val7)).X), (float)(((Point)(ref val17)).Y - ((Point)(ref val7)).Y));
				((ChartObject)this).RenderTarget.FillRectangle(val18, val8.BrushDX);
			}
			item.Stroke.CopyTo(val8);
			val8.Opacity = base.PriceLevelOpacity;
			val7 = val16;
			num2 = Math.Min(((Point)(ref val13)).Y, num2);
			num3 = Math.Max(((Point)(ref val13)).Y, num3);
			num4++;
		}
		if (!((ChartObject)this).IsInHitTest)
		{
			Point val21 = default(Point);
			foreach (PriceLevel item2 in from pl in base.PriceLevels
				where pl.IsVisible && pl.Stroke != null
				orderby pl.Value
				select pl)
			{
				Tuple<Point, Point> priceLevelLinePoints2 = GetPriceLevelLinePoints(item2, chartControl, chartScale, isInverted: false);
				Vector val19 = anchorExtensionPoint - point;
				Point val20 = priceLevelLinePoints2.Item1 + val19;
				double x3;
				if (!base.IsExtendedLinesLeft)
				{
					x3 = ((Point)(ref val20)).X;
				}
				else
				{
					val12 = priceLevelLinePoints2.Item1;
					x3 = ((Point)(ref val12)).X;
				}
				double num8 = x3;
				val12 = priceLevelLinePoints2.Item1;
				((Point)(ref val21))._002Ector(num8, ((Point)(ref val12)).Y);
				double x4 = ((Point)(ref anchorExtensionPoint)).X;
				double maxX = ((Point)(ref anchorExtensionPoint)).X + ((Point)(ref point2)).X - ((Point)(ref point)).X;
				double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
				double price = item2.GetPrice(ExtensionAnchor.Price, totalPriceRange, isInverted: false);
				DrawPriceLevelText(val, chartScale, x4, maxX, ((Point)(ref val21)).Y, price, item2);
			}
		}
		if (num4 > 0)
		{
			((ChartObject)this).RenderTarget.DrawLine(new Vector2(val5.X, (float)num2), new Vector2(val5.X, (float)num3), base.AnchorLineStroke.BrushDX, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		}
	}
}
