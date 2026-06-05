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
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class TrendChannel : PriceLevelContainer
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaDeviceBrush = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private PathGeometry fillMainGeometry;

	private Vector2[] fillMainFig;

	private PathGeometry fillLeftGeometry;

	private Vector2[] fillLeftFig;

	private PathGeometry fillRightGeometry;

	private Vector2[] fillRightFig;

	private bool isReadyForMovingSecondLeg;

	private bool updateEndAnc;

	public override object Icon => Icons.DrawTrendChannel;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = BrushExtensions.ToFrozenBrush(value);
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			int num = Math.Max(0, Math.Min(100, value));
			if (num != areaOpacity)
			{
				areaOpacity = num;
				areaDeviceBrush.Brush = null;
			}
		}
	}

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { TrendStartAnchor, TrendEndAnchor, ParallelStartAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesRight { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesLeft { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTrendChannelTrendStroke", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke Stroke { get; set; }

	[Display(Order = 10)]
	[ExcludeFromTemplate]
	public ChartAnchor TrendEndAnchor { get; set; }

	[Display(Order = 0)]
	[ExcludeFromTemplate]
	public ChartAnchor TrendStartAnchor { get; set; }

	[Display(Order = 20)]
	[ExcludeFromTemplate]
	public ChartAnchor ParallelStartAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTrendChannelParallelStroke", GroupName = "NinjaScriptLines", Order = 2)]
	public Stroke ParallelStroke { get; set; }

	public override bool SupportsAlerts => true;

	public override void CopyTo(NinjaScript ninjaScript)
	{
		base.CopyTo(ninjaScript);
		if (ninjaScript is TrendChannel trendChannel)
		{
			trendChannel.isReadyForMovingSecondLeg = isReadyForMovingSecondLeg;
		}
	}

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaDeviceBrush != null)
		{
			areaDeviceBrush.RenderTarget = null;
		}
		PathGeometry obj = fillLeftGeometry;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		PathGeometry obj2 = fillMainGeometry;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		PathGeometry obj3 = fillRightGeometry;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
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
				base.PriceLevels.Add(new PriceLevel(0.0, (Brush)(object)Brushes.Transparent));
				base.PriceLevels.Add(new PriceLevel(100.0, (Brush)(object)Brushes.Transparent));
			}
			return;
		}
		((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolTrendChannelDescription;
		((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolTrendChannel;
		((DrawingTool)this).DrawingState = (DrawingState)0;
		TrendStartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelStart1AnchorDisplayName
		};
		TrendEndAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelEnd1AnchorDisplayName
		};
		ParallelStartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelStart2AnchorDisplayName,
			Time = DateTime.MinValue
		};
		ParallelStroke = new Stroke((Brush)(object)Brushes.SeaGreen, 2f);
		Stroke = new Stroke((Brush)(object)Brushes.SeaGreen, 2f);
		AreaBrush = (Brush)(object)Brushes.SeaGreen;
		AreaOpacity = 0;
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

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
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
			if (editingAnchor == null)
			{
				return null;
			}
			if (!((DrawingTool)this).IsLocked)
			{
				if (editingAnchor != TrendStartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			return Cursors.No;
		default:
		{
			Point point2 = TrendStartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = ParallelStartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (!((DrawingTool)this).IsLocked)
				{
					if (closestAnchor != TrendStartAnchor)
					{
						return Cursors.SizeNWSE;
					}
					return Cursors.SizeNESW;
				}
				return Cursors.Arrow;
			}
			Point point4 = TrendEndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point val = point3 + (point4 - point2);
			Vector val2 = point4 - point2;
			Vector val3 = val - point3;
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point2, point4);
			Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(point3, val);
			Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(point4, point2);
			Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(val, point3);
			if (IsExtendedLinesLeft)
			{
				Vector val4 = extendedPoint3 - point2;
				Vector val5 = extendedPoint4 - point3;
				if (MathHelper.IsPointAlongVector(point, point2, val4, 15.0) || MathHelper.IsPointAlongVector(point, point3, val5, 15.0))
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			if (IsExtendedLinesRight)
			{
				Vector val6 = extendedPoint - point4;
				Vector val7 = extendedPoint2 - val;
				if (MathHelper.IsPointAlongVector(point, point4, val6, 15.0) || MathHelper.IsPointAlongVector(point, val, val7, 15.0))
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			if (MathHelper.IsPointAlongVector(point, point2, val2, 15.0) || MathHelper.IsPointAlongVector(point, point3, val3, 15.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			return null;
		}
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = default(Point);
		((Point)(ref val2))._002Ector((((Point)(ref point)).X + ((Point)(ref point2)).X) / 2.0, (((Point)(ref point)).Y + ((Point)(ref point2)).Y) / 2.0);
		Point point3 = ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val3 = point3 + (point2 - point);
		Point val4 = default(Point);
		((Point)(ref val4))._002Ector((((Point)(ref point3)).X + ((Point)(ref val3)).X) / 2.0, (((Point)(ref point3)).Y + ((Point)(ref val3)).Y) / 2.0);
		if ((int)((DrawingTool)this).DrawingState == 0 && !isReadyForMovingSecondLeg)
		{
			return (Point[])(object)new Point[3] { point, val2, point2 };
		}
		return (Point[])(object)new Point[6] { point, val2, point2, point3, val4, val3 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Expected I4, but got Unknown
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Invalid comparison between Unknown and I4
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Invalid comparison between Unknown and I4
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Invalid comparison between Unknown and I4
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Invalid comparison between Unknown and I4
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Invalid comparison between Unknown and I4
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Invalid comparison between Unknown and I4
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Invalid comparison between Unknown and I4
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Vector val2 = ((conditionItem.Tag as PriceLevel)?.Value ?? 0.0) / 100.0 * (ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true) - point);
		Vector val3 = point2 - point;
		Point val4 = default(Point);
		((Point)(ref val4))._002Ector(((Point)(ref point)).X + ((Vector)(ref val2)).X, ((Point)(ref point)).Y + ((Vector)(ref val2)).Y);
		Point val5 = default(Point);
		((Point)(ref val5))._002Ector(((Point)(ref val4)).X + ((Vector)(ref val3)).X, ((Point)(ref val4)).Y + ((Vector)(ref val3)).Y);
		double num = chartControl.GetXByTime(values[0].Time);
		double num2 = chartScale.GetYByValue(values[0].Value);
		Point alertStartPoint = ((((Point)(ref val4)).X <= ((Point)(ref val5)).X) ? val4 : val5);
		Point alertEndPoint = ((((Point)(ref val5)).X >= ((Point)(ref val4)).X) ? val5 : val4);
		Point val6 = default(Point);
		((Point)(ref val6))._002Ector(num, num2);
		if (IsExtendedLinesLeft)
		{
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(alertEndPoint, alertStartPoint);
			if (((Point)(ref extendedPoint)).X > -1.0 || ((Point)(ref extendedPoint)).Y > -1.0)
			{
				alertStartPoint = extendedPoint;
			}
		}
		if (IsExtendedLinesRight)
		{
			Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(alertStartPoint, alertEndPoint);
			if (((Point)(ref extendedPoint2)).X > -1.0 || ((Point)(ref extendedPoint2)).Y > -1.0)
			{
				alertEndPoint = extendedPoint2;
			}
		}
		if (num < ((Point)(ref alertStartPoint)).X || num > ((Point)(ref alertEndPoint)).X)
		{
			return false;
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, val6);
		Condition val7 = condition;
		switch ((int)val7)
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
			double num3 = chartControl.GetXByTime(v.Time);
			double num4 = chartScale.GetYByValue(v.Value);
			Point val8 = default(Point);
			((Point)(ref val8))._002Ector(num3, num4);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, val8);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
		{
			return true;
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point val2 = point3 + (point2 - point);
		Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point, point2);
		Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(point3, val2);
		Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(point2, point);
		Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(val2, point3);
		Point[] source = new Point[4] { extendedPoint, extendedPoint2, extendedPoint3, extendedPoint4 };
		double num = ((IEnumerable<Point>)(object)source).Select((Point p) => ((Point)(ref p)).X).Min();
		double num2 = ((IEnumerable<Point>)(object)source).Select((Point p) => ((Point)(ref p)).X).Max();
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		DateTime timeByX2 = chartControl.GetTimeByX((int)((Point)(ref point)).X);
		DateTime timeByX3 = chartControl.GetTimeByX((int)((Point)(ref point2)).X);
		DateTime timeByX4 = chartControl.GetTimeByX((int)num2);
		DateTime[] array = new DateTime[4] { timeByX, timeByX2, timeByX3, timeByX4 };
		foreach (DateTime dateTime in array)
		{
			if (dateTime >= firstTimeOnChart && dateTime <= lastTimeOnChart)
			{
				return true;
			}
		}
		if ((timeByX <= firstTimeOnChart && timeByX4 >= lastTimeOnChart) || (timeByX2 <= firstTimeOnChart && timeByX3 >= lastTimeOnChart) || (timeByX3 <= firstTimeOnChart && timeByX2 >= lastTimeOnChart))
		{
			return true;
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !((DrawingTool)this).Anchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
			((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
		}
	}

	public override void OnEdited(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, DrawingTool oldinstance)
	{
		SetParallelLine(chartControl, initialSet: false);
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if (drawingState - 2 > 1)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = true;
				((DrawingTool)this).DrawingState = (DrawingState)1;
			}
			else if (editingAnchor == null || ((DrawingTool)this).IsLocked)
			{
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
				else
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (TrendStartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TrendStartAnchor);
			dataPoint.CopyDataValues(TrendEndAnchor);
			TrendStartAnchor.IsEditing = false;
		}
		else if (TrendEndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TrendEndAnchor);
			TrendEndAnchor.IsEditing = false;
		}
		if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing)
		{
			SetParallelLine(chartControl, ParallelStartAnchor.IsEditing);
		}
		if (!isReadyForMovingSecondLeg)
		{
			if (!ParallelStartAnchor.IsEditing)
			{
				isReadyForMovingSecondLeg = true;
			}
		}
		else
		{
			isReadyForMovingSecondLeg = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (TrendEndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(TrendEndAnchor);
			}
			else if (isReadyForMovingSecondLeg)
			{
				ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1)
		{
			if (!TrendStartAnchor.IsEditing && !ParallelStartAnchor.IsEditing && TrendEndAnchor.IsEditing)
			{
				TrendEndAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendEndAnchor.IsEditing && !ParallelStartAnchor.IsEditing && TrendStartAnchor.IsEditing)
			{
				TrendStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing && ParallelStartAnchor.IsEditing)
			{
				ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendStartAnchor.IsEditing && !ParallelStartAnchor.IsEditing && !TrendEndAnchor.IsEditing)
			{
				((DrawingTool)this).DrawingState = (DrawingState)3;
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 3)
		{
			ChartAnchor[] array = (ChartAnchor[])(object)new ChartAnchor[2] { TrendStartAnchor, TrendEndAnchor };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if ((int)((DrawingTool)this).DrawingState == 1 && updateEndAnc)
			{
				updateEndAnc = false;
			}
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
			}
			editingAnchor = null;
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1232: Unknown result type (might be due to invalid IL or missing references)
		//IL_1234: Unknown result type (might be due to invalid IL or missing references)
		//IL_1236: Unknown result type (might be due to invalid IL or missing references)
		//IL_128b: Unknown result type (might be due to invalid IL or missing references)
		//IL_128d: Unknown result type (might be due to invalid IL or missing references)
		//IL_128f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_211f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2120: Unknown result type (might be due to invalid IL or missing references)
		//IL_2121: Unknown result type (might be due to invalid IL or missing references)
		//IL_2126: Unknown result type (might be due to invalid IL or missing references)
		//IL_212b: Unknown result type (might be due to invalid IL or missing references)
		//IL_212d: Unknown result type (might be due to invalid IL or missing references)
		//IL_212e: Unknown result type (might be due to invalid IL or missing references)
		//IL_212f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2134: Unknown result type (might be due to invalid IL or missing references)
		//IL_2186: Unknown result type (might be due to invalid IL or missing references)
		//IL_2188: Unknown result type (might be due to invalid IL or missing references)
		//IL_218d: Unknown result type (might be due to invalid IL or missing references)
		//IL_218f: Unknown result type (might be due to invalid IL or missing references)
		//IL_21be: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_21ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_21cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_21ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_21d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_2207: Unknown result type (might be due to invalid IL or missing references)
		//IL_2209: Unknown result type (might be due to invalid IL or missing references)
		//IL_220e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2210: Unknown result type (might be due to invalid IL or missing references)
		//IL_2270: Unknown result type (might be due to invalid IL or missing references)
		//IL_2272: Unknown result type (might be due to invalid IL or missing references)
		//IL_2277: Unknown result type (might be due to invalid IL or missing references)
		//IL_2279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_0554: Unknown result type (might be due to invalid IL or missing references)
		//IL_0556: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0567: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_057a: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_073e: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0744: Unknown result type (might be due to invalid IL or missing references)
		//IL_0750: Unknown result type (might be due to invalid IL or missing references)
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_0763: Unknown result type (might be due to invalid IL or missing references)
		//IL_0765: Unknown result type (might be due to invalid IL or missing references)
		//IL_076a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0789: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Unknown result type (might be due to invalid IL or missing references)
		//IL_078f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1406: Unknown result type (might be due to invalid IL or missing references)
		//IL_1408: Unknown result type (might be due to invalid IL or missing references)
		//IL_140d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1419: Unknown result type (might be due to invalid IL or missing references)
		//IL_141b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1420: Unknown result type (might be due to invalid IL or missing references)
		//IL_142c: Unknown result type (might be due to invalid IL or missing references)
		//IL_142e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1433: Unknown result type (might be due to invalid IL or missing references)
		//IL_143f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1441: Unknown result type (might be due to invalid IL or missing references)
		//IL_1446: Unknown result type (might be due to invalid IL or missing references)
		//IL_1452: Unknown result type (might be due to invalid IL or missing references)
		//IL_1453: Unknown result type (might be due to invalid IL or missing references)
		//IL_1458: Unknown result type (might be due to invalid IL or missing references)
		//IL_05af: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Expected O, but got Unknown
		//IL_05d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1603: Unknown result type (might be due to invalid IL or missing references)
		//IL_1605: Unknown result type (might be due to invalid IL or missing references)
		//IL_160a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1616: Unknown result type (might be due to invalid IL or missing references)
		//IL_1618: Unknown result type (might be due to invalid IL or missing references)
		//IL_161d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1629: Unknown result type (might be due to invalid IL or missing references)
		//IL_162b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1630: Unknown result type (might be due to invalid IL or missing references)
		//IL_163c: Unknown result type (might be due to invalid IL or missing references)
		//IL_163e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1643: Unknown result type (might be due to invalid IL or missing references)
		//IL_164f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1650: Unknown result type (might be due to invalid IL or missing references)
		//IL_1655: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0979: Unknown result type (might be due to invalid IL or missing references)
		//IL_097a: Unknown result type (might be due to invalid IL or missing references)
		//IL_097f: Unknown result type (might be due to invalid IL or missing references)
		//IL_098b: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0992: Unknown result type (might be due to invalid IL or missing references)
		//IL_099e: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b5: Expected O, but got Unknown
		//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1474: Unknown result type (might be due to invalid IL or missing references)
		//IL_147e: Expected O, but got Unknown
		//IL_149a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c87: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c89: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_183f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1841: Unknown result type (might be due to invalid IL or missing references)
		//IL_1846: Unknown result type (might be due to invalid IL or missing references)
		//IL_1852: Unknown result type (might be due to invalid IL or missing references)
		//IL_1854: Unknown result type (might be due to invalid IL or missing references)
		//IL_1859: Unknown result type (might be due to invalid IL or missing references)
		//IL_1865: Unknown result type (might be due to invalid IL or missing references)
		//IL_1867: Unknown result type (might be due to invalid IL or missing references)
		//IL_186c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1878: Unknown result type (might be due to invalid IL or missing references)
		//IL_187a: Unknown result type (might be due to invalid IL or missing references)
		//IL_187f: Unknown result type (might be due to invalid IL or missing references)
		//IL_188b: Unknown result type (might be due to invalid IL or missing references)
		//IL_188c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1891: Unknown result type (might be due to invalid IL or missing references)
		//IL_1671: Unknown result type (might be due to invalid IL or missing references)
		//IL_167b: Expected O, but got Unknown
		//IL_1697: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f0: Expected O, but got Unknown
		//IL_0a0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c08: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c53: Unknown result type (might be due to invalid IL or missing references)
		//IL_18ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_18b7: Expected O, but got Unknown
		//IL_18d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ac9: Unknown result type (might be due to invalid IL or missing references)
		//IL_1acb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1adc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ade: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ae3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1aef: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b02: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b04: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b09: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b15: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b16: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c79: Expected O, but got Unknown
		//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e53: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cfe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d71: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b37: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b41: Expected O, but got Unknown
		//IL_1b5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f75: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dd8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dda: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ddf: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e37: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e39: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1145: Unknown result type (might be due to invalid IL or missing references)
		//IL_1147: Unknown result type (might be due to invalid IL or missing references)
		//IL_114c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1158: Unknown result type (might be due to invalid IL or missing references)
		//IL_1159: Unknown result type (might be due to invalid IL or missing references)
		//IL_115e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1066: Unknown result type (might be due to invalid IL or missing references)
		//IL_1068: Unknown result type (might be due to invalid IL or missing references)
		//IL_106d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2011: Unknown result type (might be due to invalid IL or missing references)
		//IL_2013: Unknown result type (might be due to invalid IL or missing references)
		//IL_2018: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ec1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ec3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ec8: Unknown result type (might be due to invalid IL or missing references)
		//IL_10c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_10c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_10cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_2024: Unknown result type (might be due to invalid IL or missing references)
		//IL_2025: Unknown result type (might be due to invalid IL or missing references)
		//IL_202a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f31: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f36: Unknown result type (might be due to invalid IL or missing references)
		//IL_117a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1184: Expected O, but got Unknown
		//IL_11a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1130: Unknown result type (might be due to invalid IL or missing references)
		//IL_1132: Unknown result type (might be due to invalid IL or missing references)
		//IL_1137: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fa4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2046: Unknown result type (might be due to invalid IL or missing references)
		//IL_2050: Expected O, but got Unknown
		//IL_206c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ffc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ffe: Unknown result type (might be due to invalid IL or missing references)
		//IL_2003: Unknown result type (might be due to invalid IL or missing references)
		Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ParallelStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaDeviceBrush.Brush == null)
			{
				Brush val = areaBrush.Clone();
				val.Opacity = (double)areaOpacity / 100.0;
				areaDeviceBrush.Brush = val;
			}
			areaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		else
		{
			areaDeviceBrush.RenderTarget = null;
			areaDeviceBrush.Brush = null;
		}
		ChartPanel val2 = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val2, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val2, chartScale, true);
		Point point3 = ParallelStartAnchor.GetPoint(chartControl, val2, chartScale, true);
		Point val3 = point3 + (point2 - point);
		Vector2 val4 = DxExtensions.ToVector2(point);
		Vector2 val5 = DxExtensions.ToVector2(point2);
		Vector2 val6 = DxExtensions.ToVector2(point3);
		Vector2 val7 = DxExtensions.ToVector2(val3);
		Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(chartControl, val2, chartScale, TrendStartAnchor, TrendEndAnchor);
		Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(chartControl, val2, chartScale, TrendEndAnchor, TrendStartAnchor);
		Point val8 = (Point)((ParallelStartAnchor.Time > DateTime.MinValue) ? (point3 + (extendedPoint - extendedPoint2)) : new Point(double.NaN, double.NaN));
		Point val9 = (Point)((ParallelStartAnchor.Time > DateTime.MinValue) ? (point3 + (extendedPoint2 - extendedPoint)) : new Point(double.NaN, double.NaN));
		Brush val10 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val4, val5, val10, Stroke.Width, Stroke.StrokeStyle);
		if ((int)((DrawingTool)this).DrawingState == 0 && !isReadyForMovingSecondLeg)
		{
			return;
		}
		val10 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : ParallelStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val6, val7, val10, ParallelStroke.Width, ParallelStroke.StrokeStyle);
		fillMainFig = (Vector2[])(object)new Vector2[4];
		fillMainFig[0] = DxExtensions.ToVector2(point3);
		fillMainFig[1] = DxExtensions.ToVector2(val3);
		fillMainFig[2] = DxExtensions.ToVector2(point2);
		fillMainFig[3] = DxExtensions.ToVector2(point);
		fillMainGeometry = new PathGeometry(Globals.D2DFactory);
		GeometrySink obj = fillMainGeometry.Open();
		((SimplifiedGeometrySink)obj).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
		((SimplifiedGeometrySink)obj).AddLines(fillMainFig);
		((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
		((SimplifiedGeometrySink)obj).Close();
		DeviceBrush val11 = areaDeviceBrush;
		if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
		{
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillMainGeometry, areaDeviceBrush.BrushDX);
		}
		if (IsExtendedLinesLeft)
		{
			if (((Point)(ref extendedPoint2)).X > -1.0 || ((Point)(ref extendedPoint2)).Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val4, DxExtensions.ToVector2(extendedPoint2), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
			}
			if (!double.IsNaN(((Point)(ref val9)).X) && !double.IsNaN(((Point)(ref val9)).Y))
			{
				((ChartObject)this).RenderTarget.DrawLine(val6, DxExtensions.ToVector2(val9), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);
			}
			if ((((Point)(ref val9)).Y > 0.0 && ((Point)(ref val9)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val9)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref extendedPoint2)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint2)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint2)).Y > 0.0 && ((Point)(ref extendedPoint2)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint2)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref val9)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val9)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val12 = default(Point);
				((Point)(ref val12))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(val9);
				fillLeftFig[2] = DxExtensions.ToVector2(val12);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj2 = fillLeftGeometry;
				if (obj2 != null)
				{
					((DisposeBase)obj2).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj3 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj3).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj3).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj3).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj3).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val9)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val9)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref extendedPoint2)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint2)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint2)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint2)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref val9)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val9)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val13 = default(Point);
				((Point)(ref val13))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(val9);
				fillLeftFig[2] = DxExtensions.ToVector2(val13);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj4 = fillLeftGeometry;
				if (obj4 != null)
				{
					((DisposeBase)obj4).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj5 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj5).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj5).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj5).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj5).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val9)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val9)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref extendedPoint2)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint2)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint2)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint2)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref val9)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val9)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val14 = default(Point);
				((Point)(ref val14))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(val9);
				fillLeftFig[2] = DxExtensions.ToVector2(val14);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj6 = fillLeftGeometry;
				if (obj6 != null)
				{
					((DisposeBase)obj6).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj7 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj7).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj7).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj7).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj7).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val9)).Y > 0.0 && ((Point)(ref val9)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val9)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref extendedPoint2)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint2)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint2)).Y > 0.0 && ((Point)(ref extendedPoint2)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint2)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref val9)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val9)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val15 = default(Point);
				((Point)(ref val15))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(val9);
				fillLeftFig[2] = DxExtensions.ToVector2(val15);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj8 = fillLeftGeometry;
				if (obj8 != null)
				{
					((DisposeBase)obj8).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj9 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj9).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj9).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj9).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj9).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else
			{
				Point val16 = default(Point);
				((Point)(ref val16))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)((ChartObject)this).ChartPanel.Y);
				Point val17 = default(Point);
				((Point)(ref val17))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)((ChartObject)this).ChartPanel.Y);
				Point val18 = default(Point);
				((Point)(ref val18))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				Point val19 = default(Point);
				((Point)(ref val19))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillLeftFig = (Vector2[])(object)new Vector2[4];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref val3)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && ((Point)(ref point3)).X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[1] = DxExtensions.ToVector2(val16);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref val3)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && ((Point)(ref point3)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[1] = DxExtensions.ToVector2(val17);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref val3)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref point3)).X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[1] = DxExtensions.ToVector2(val18);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref val3)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref point3)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[1] = DxExtensions.ToVector2(val19);
				}
				else
				{
					fillLeftFig[1] = DxExtensions.ToVector2(val9);
				}
				if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref point2)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && ((Point)(ref point)).X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[2] = DxExtensions.ToVector2(val16);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref point2)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && ((Point)(ref point)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[2] = DxExtensions.ToVector2(val17);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref point2)).Y < 0.0 && ((Point)(ref point)).X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[2] = DxExtensions.ToVector2(val18);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref point2)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref point)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[2] = DxExtensions.ToVector2(val19);
				}
				else
				{
					fillLeftFig[2] = DxExtensions.ToVector2(extendedPoint2);
				}
				fillLeftFig[3] = DxExtensions.ToVector2(point);
				PathGeometry obj10 = fillLeftGeometry;
				if (obj10 != null)
				{
					((DisposeBase)obj10).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj11 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj11).BeginFigure(new Vector2((float)((Point)(ref point)).X, (float)((Point)(ref point)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj11).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj11).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj11).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
		}
		if (IsExtendedLinesRight)
		{
			if (((Point)(ref extendedPoint)).X > -1.0 || ((Point)(ref extendedPoint)).Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val5, DxExtensions.ToVector2(extendedPoint), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
			}
			if (((Point)(ref val8)).X > -1.0 || ((Point)(ref val8)).Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val7, DxExtensions.ToVector2(val8), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);
			}
			if ((((Point)(ref val8)).Y > 0.0 && ((Point)(ref val8)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val8)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref extendedPoint)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint)).Y > 0.0 && ((Point)(ref extendedPoint)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref val8)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val8)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val20 = default(Point);
				((Point)(ref val20))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(val3);
				fillRightFig[1] = DxExtensions.ToVector2(val8);
				fillRightFig[2] = DxExtensions.ToVector2(val20);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj12 = fillRightGeometry;
				if (obj12 != null)
				{
					((DisposeBase)obj12).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj13 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj13).BeginFigure(new Vector2((float)((Point)(ref point2)).X, (float)((Point)(ref point2)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj13).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj13).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj13).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val8)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val8)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref extendedPoint)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint)).X > (double)((ChartObject)this).ChartPanel.X && ((Point)(ref extendedPoint)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref val8)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref val8)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val21 = default(Point);
				((Point)(ref val21))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(val3);
				fillRightFig[1] = DxExtensions.ToVector2(val8);
				fillRightFig[2] = DxExtensions.ToVector2(val21);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj14 = fillRightGeometry;
				if (obj14 != null)
				{
					((DisposeBase)obj14).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj15 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj15).BeginFigure(new Vector2((float)((Point)(ref point2)).X, (float)((Point)(ref point2)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj15).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj15).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj15).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val8)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val8)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref extendedPoint)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint)).Y < (double)((ChartObject)this).ChartPanel.Y && ((Point)(ref val8)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val8)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val22 = default(Point);
				((Point)(ref val22))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(val3);
				fillRightFig[1] = DxExtensions.ToVector2(val8);
				fillRightFig[2] = DxExtensions.ToVector2(val22);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj16 = fillRightGeometry;
				if (obj16 != null)
				{
					((DisposeBase)obj16).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj17 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj17).BeginFigure(new Vector2((float)((Point)(ref point2)).X, (float)((Point)(ref point2)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj17).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj17).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj17).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((((Point)(ref val8)).Y > 0.0 && ((Point)(ref val8)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val8)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref extendedPoint)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (((Point)(ref extendedPoint)).Y > 0.0 && ((Point)(ref extendedPoint)).X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref extendedPoint)).Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && ((Point)(ref val8)).X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && ((Point)(ref val8)).Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point val23 = default(Point);
				((Point)(ref val23))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(val3);
				fillRightFig[1] = DxExtensions.ToVector2(val8);
				fillRightFig[2] = DxExtensions.ToVector2(val23);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj18 = fillRightGeometry;
				if (obj18 != null)
				{
					((DisposeBase)obj18).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj19 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj19).BeginFigure(new Vector2((float)((Point)(ref point2)).X, (float)((Point)(ref point2)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj19).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj19).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj19).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else
			{
				Point val24 = default(Point);
				((Point)(ref val24))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)((ChartObject)this).ChartPanel.Y);
				Point val25 = default(Point);
				((Point)(ref val25))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)((ChartObject)this).ChartPanel.Y);
				Point val26 = default(Point);
				((Point)(ref val26))._002Ector((double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X), (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				Point val27 = default(Point);
				((Point)(ref val27))._002Ector((double)((ChartObject)this).ChartPanel.X, (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y));
				fillRightFig = (Vector2[])(object)new Vector2[4];
				fillRightFig[0] = DxExtensions.ToVector2(val3);
				if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref val3)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && ((Point)(ref point3)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[1] = DxExtensions.ToVector2(val24);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref val3)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref point3)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[1] = DxExtensions.ToVector2(val25);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref val3)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && ((Point)(ref point3)).Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[1] = DxExtensions.ToVector2(val26);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref val3)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref point3)).Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[1] = DxExtensions.ToVector2(val27);
				}
				else
				{
					fillRightFig[1] = DxExtensions.ToVector2(val8);
				}
				if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref point2)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && ((Point)(ref point)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[2] = DxExtensions.ToVector2(val24);
				}
				else if (((Point)(ref point)).Y > ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref point2)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref point)).Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[2] = DxExtensions.ToVector2(val25);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X < ((Point)(ref point2)).X && ((Point)(ref point2)).X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && ((Point)(ref point)).Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[2] = DxExtensions.ToVector2(val26);
				}
				else if (((Point)(ref point)).Y < ((Point)(ref point2)).Y && ((Point)(ref point)).X > ((Point)(ref point2)).X && ((Point)(ref point2)).X < (double)((ChartObject)this).ChartPanel.X && ((Point)(ref point)).Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[2] = DxExtensions.ToVector2(val27);
				}
				else
				{
					fillRightFig[2] = DxExtensions.ToVector2(extendedPoint);
				}
				fillRightFig[3] = DxExtensions.ToVector2(point2);
				PathGeometry obj20 = fillRightGeometry;
				if (obj20 != null)
				{
					((DisposeBase)obj20).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj21 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj21).BeginFigure(new Vector2((float)((Point)(ref point2)).X, (float)((Point)(ref point2)).Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj21).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj21).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj21).Close();
				val11 = areaDeviceBrush;
				if (val11 != null && val11.RenderTarget != null && val11.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
		}
		SetAllPriceLevelsRenderTarget();
		Point val30 = default(Point);
		Point val31 = default(Point);
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel tl) => tl.IsVisible && tl.Stroke != null))
		{
			Vector val28 = item.Value / 100.0 * (point3 - point);
			Vector val29 = point2 - point;
			((Point)(ref val30))._002Ector(((Point)(ref point)).X + ((Vector)(ref val28)).X, ((Point)(ref point)).Y + ((Vector)(ref val28)).Y);
			((Point)(ref val31))._002Ector(((Point)(ref val30)).X + ((Vector)(ref val29)).X, ((Point)(ref val30)).Y + ((Vector)(ref val29)).Y);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val30), DxExtensions.ToVector2(val31), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(val30, val31);
			Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(val31, val30);
			if (IsExtendedLinesLeft && (((Point)(ref extendedPoint4)).X > -1.0 || ((Point)(ref extendedPoint4)).Y > -1.0))
			{
				((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val30), DxExtensions.ToVector2(extendedPoint4), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			if (IsExtendedLinesRight && (((Point)(ref extendedPoint3)).X > -1.0 || ((Point)(ref extendedPoint3)).Y > -1.0))
			{
				((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(val31), DxExtensions.ToVector2(extendedPoint3), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
		}
	}

	private void SetParallelLine(ChartControl chartControl, bool initialSet)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (initialSet)
		{
			if ((int)chartControl.BarSpacingType != 3)
			{
				ParallelStartAnchor.SlotIndex = TrendEndAnchor.SlotIndex;
				ParallelStartAnchor.Time = chartControl.GetTimeBySlotIndex(ParallelStartAnchor.SlotIndex);
			}
			else
			{
				ParallelStartAnchor.Time = TrendEndAnchor.Time;
			}
			ParallelStartAnchor.Price = TrendEndAnchor.Price;
			ParallelStartAnchor.StartAnchor = ((DrawingTool)this).InitialMouseDownAnchor;
		}
		else
		{
			double num = TrendStartAnchor.Price - ParallelStartAnchor.Price;
			ParallelStartAnchor.Price = TrendStartAnchor.Price - num;
		}
		ParallelStartAnchor.IsEditing = false;
	}
}
