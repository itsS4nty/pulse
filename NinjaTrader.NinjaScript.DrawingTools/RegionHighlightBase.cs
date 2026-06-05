using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

[CLSCompliant(false)]
public abstract class RegionHighlightBase : DrawingTool
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private bool hasSetZOrder;

	public override bool SupportsAlerts => true;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardAnchorLineStroke", GroupName = "NinjaScriptGeneral", Order = 5)]
	public Stroke AnchorLineStroke { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = value;
			if (areaBrush != null)
			{
				if (((Freezable)areaBrush).IsFrozen)
				{
					areaBrush = areaBrush.Clone();
				}
				areaBrush.Opacity = (double)areaOpacity / 100.0;
				((Freezable)areaBrush).Freeze();
			}
			areaBrushDevice.Brush = null;
		}
	}

	[Browsable(false)]
	public string AreaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(AreaBrush);
		}
		set
		{
			AreaBrush = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 4)]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			areaOpacity = Math.Max(0, Math.Min(100, value));
			areaBrushDevice.Brush = null;
		}
	}

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	internal RegionHighlightMode Mode { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
	public Stroke OutlineStroke { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = Resource.NinjaScriptDrawingToolRegion,
			ShouldOnlyDisplayName = true
		};
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 1:
			if (!((DrawingTool)this).IsLocked)
			{
				if (Mode != RegionHighlightMode.Time)
				{
					return Cursors.SizeNS;
				}
				return Cursors.SizeWE;
			}
			return Cursors.No;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		default:
		{
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (Mode != RegionHighlightMode.Time)
				{
					return Cursors.SizeNS;
				}
				return Cursors.SizeWE;
			}
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Vector val = point3 - point2;
			if (MathHelper.IsPointAlongVector(point, point2, val, 15.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			Point[] array = (Point[])(object)new Point[2] { point2, point3 };
			for (int i = 0; i < array.Length; i++)
			{
				Point val2 = array[i];
				if (Mode == RegionHighlightMode.Price && Math.Abs(((Point)(ref val2)).Y - ((Point)(ref point)).Y) <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				if (Mode == RegionHighlightMode.Time && Math.Abs(((Point)(ref val2)).X - ((Point)(ref point)).X) <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			return null;
		}
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double middleX = (double)val.X + (double)val.W / 2.0;
		double middleY = (double)val.Y + (double)val.H / 2.0;
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		return ((IEnumerable<Point>)(object)new Point[3] { point, val2, point2 }).Select((Func<Point, Point>)((Point p) => (Mode != RegionHighlightMode.Time) ? new Point(middleX, ((Point)(ref p)).Y) : new Point(((Point)(ref p)).X, middleY))).ToArray();
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		return (IEnumerable<Condition>)(object)new Condition[2]
		{
			(Condition)8,
			(Condition)9
		};
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		double minPrice = ((DrawingTool)this).Anchors.Min((ChartAnchor a) => a.Price);
		double maxPrice = ((DrawingTool)this).Anchors.Max((ChartAnchor a) => a.Price);
		DateTime minTime = ((DrawingTool)this).Anchors.Min((ChartAnchor a) => a.Time);
		DateTime maxTime = ((DrawingTool)this).Anchors.Max((ChartAnchor a) => a.Time);
		if (Mode == RegionHighlightMode.Time)
		{
			DateTime time = values[0].Time;
			if ((int)condition != 8)
			{
				if (time > minTime)
				{
					return time < maxTime;
				}
				return false;
			}
			if (time > minTime)
			{
				return time <= maxTime;
			}
			return false;
		}
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		bool Predicate(ChartAlertValue v)
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Invalid comparison between Unknown and I4
			bool flag = ((Mode != RegionHighlightMode.Time) ? (v.Value >= minPrice && v.Value <= maxPrice) : (v.Time >= minTime && v.Time <= maxTime));
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		if (Mode == RegionHighlightMode.Time)
		{
			if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
			{
				return true;
			}
			if (StartAnchor.Time <= firstTimeOnChart && EndAnchor.Time >= lastTimeOnChart)
			{
				return true;
			}
			if (EndAnchor.Time <= firstTimeOnChart && StartAnchor.Time >= lastTimeOnChart)
			{
				return true;
			}
			return false;
		}
		if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Price <= chartScale.MaxValue && a.Price >= chartScale.MinValue))
		{
			return true;
		}
		if (!(StartAnchor.Price <= chartScale.MinValue) || !(EndAnchor.Price >= chartScale.MaxValue))
		{
			if (EndAnchor.Price <= chartScale.MinValue)
			{
				return StartAnchor.Price >= chartScale.MaxValue;
			}
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
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
			((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
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
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
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
			return;
		}
		if (Mode == RegionHighlightMode.Price)
		{
			dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2.0);
		}
		else
		{
			dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2.0;
		}
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			StartAnchor.IsEditing = false;
			dataPoint.CopyDataValues(EndAnchor);
		}
		else if (EndAnchor.IsEditing)
		{
			if (Mode == RegionHighlightMode.Price)
			{
				dataPoint.Time = StartAnchor.Time;
				dataPoint.SlotIndex = StartAnchor.SlotIndex;
			}
			else
			{
				dataPoint.Price = StartAnchor.Price;
			}
			dataPoint.CopyDataValues(EndAnchor);
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
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0 && EndAnchor.IsEditing)
		{
			if (Mode == RegionHighlightMode.Price)
			{
				dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2.0);
			}
			else
			{
				dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2.0;
			}
			dataPoint.CopyDataValues(EndAnchor);
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
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			editingAnchor = null;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			AnchorLineStroke = new Stroke((Brush)(object)Brushes.DarkGray, (DashStyleHelper)1, 1f);
			AreaBrush = (Brush)(object)Brushes.Goldenrod;
			AreaOpacity = 25;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			EndAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			OutlineStroke = new Stroke((Brush)(object)Brushes.Goldenrod, 2f);
			StartAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)1;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		if (!hasSetZOrder && !StartAnchor.IsNinjaScriptDrawn)
		{
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)0;
			((ChartObject)this).ZOrder = ((ChartObject)this).ChartPanel.ChartObjects.Min((IChartObject z) => z.ZOrder) - 1;
			hasSetZOrder = true;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double num = (double)val.X + (double)val.W / 2.0;
		double num2 = (double)val.Y + (double)val.H / 2.0;
		if (Mode == RegionHighlightMode.Price)
		{
			StartAnchor.UpdateXFromPoint(new Point(num, 0.0), chartControl, chartScale);
			EndAnchor.UpdateXFromPoint(new Point(num, 0.0), chartControl, chartScale);
		}
		else
		{
			StartAnchor.UpdateYFromDevicePoint(new Point(0.0, num2), chartScale);
			EndAnchor.UpdateYFromDevicePoint(new Point(0.0, num2), chartScale);
		}
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num3 = ((Point)(ref point2)).X - ((Point)(ref point)).X;
		AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush val2 = areaBrush.Clone();
				val2.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = val2;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		else
		{
			areaBrushDevice.RenderTarget = null;
			areaBrushDevice.Brush = null;
		}
		float num4 = ((MathExtentions.ApproxCompare(Math.Abs((double)outlineStroke.Width % 2.0), 0.0) == 0) ? 0.5f : 0f);
		RectangleF val3 = ((Mode == RegionHighlightMode.Time) ? new RectangleF((float)((Point)(ref point)).X + num4, (float)((ChartObject)this).ChartPanel.Y - outlineStroke.Width + num4, (float)num3, (float)(val.Y + val.H) + outlineStroke.Width * 2f) : new RectangleF((float)val.X - outlineStroke.Width + num4, (float)((Point)(ref point)).Y + num4, (float)(val.X + val.W) + outlineStroke.Width * 2f, (float)(((Point)(ref point2)).Y - ((Point)(ref point)).Y)));
		if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
		{
			((ChartObject)this).RenderTarget.FillRectangle(val3, areaBrushDevice.BrushDX);
		}
		Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawRectangle(val3, val4, outlineStroke.Width, outlineStroke.StrokeStyle);
		if (((ChartObject)this).IsSelected)
		{
			val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point2), val4, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
		}
	}
}
