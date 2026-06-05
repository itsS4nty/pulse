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
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class ShapeBase : DrawingTool
{
	protected enum ChartShapeType
	{
		Unset,
		Ellipse,
		Rectangle,
		Triangle
	}

	protected enum ResizeMode
	{
		None,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		MoveAll
	}

	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private ChartAnchor editingLeftAnchor;

	private ChartAnchor editingTopAnchor;

	private ChartAnchor editingBottomAnchor;

	private ChartAnchor editingRightAnchor;

	private ChartAnchor lastMouseMoveDataPoint;

	private ResizeMode resizeMode;

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
			areaBrush = value;
			if (areaBrush != null)
			{
				if (((Freezable)areaBrush).IsFrozen)
				{
					areaBrush = areaBrush.Clone();
				}
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
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

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
	public Stroke OutlineStroke { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(Order = 3)]
	public ChartAnchor MiddleAnchor { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	[Browsable(false)]
	protected ChartShapeType ShapeType { get; set; }

	public override bool SupportsAlerts => true;

	public override IEnumerable<ChartAnchor> Anchors
	{
		get
		{
			if (ShapeType == ChartShapeType.Triangle)
			{
				return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, MiddleAnchor, EndAnchor };
			}
			return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };
		}
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

	private PathGeometry CreateTriangleGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, double pixelAdjust)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		Point point = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point2 = MiddleAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Vector val = default(Vector);
		((Vector)(ref val))._002Ector(pixelAdjust, pixelAdjust);
		Vector2 val2 = DxExtensions.ToVector2(point + val);
		Vector2 val3 = DxExtensions.ToVector2(point2 + val);
		Vector2 val4 = DxExtensions.ToVector2(point3 + val);
		PathGeometry val5 = new PathGeometry(Globals.D2DFactory);
		GeometrySink val6 = val5.Open();
		((SimplifiedGeometrySink)val6).BeginFigure(val2, (FigureBegin)0);
		((SimplifiedGeometrySink)val6).AddLines((Vector2[])(object)new Vector2[6] { val2, val3, val3, val4, val4, val2 });
		((SimplifiedGeometrySink)val6).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)val6).Close();
		return val5;
	}

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	private Rect GetAnchorsRect(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (StartAnchor == null || EndAnchor == null)
		{
			return default(Rect);
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = Math.Min(((Point)(ref point2)).X, ((Point)(ref point)).X);
		double num2 = Math.Min(((Point)(ref point2)).Y, ((Point)(ref point)).Y);
		double num3 = Math.Abs(((Point)(ref point2)).X - ((Point)(ref point)).X);
		double num4 = Math.Abs(((Point)(ref point2)).Y - ((Point)(ref point)).Y);
		return new Rect(num, num2, num3, num4);
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
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
		if ((int)((DrawingTool)this).DrawingState == 1 && ((DrawingTool)this).IsLocked)
		{
			return Cursors.No;
		}
		if (ShapeType == ChartShapeType.Triangle)
		{
			if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) == null)
			{
				Point[] triangleAnchorPoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
				Vector val = triangleAnchorPoints.Last() - point;
				if (((Vector)(ref val)).Length <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				for (int i = 0; i < 3; i++)
				{
					Point val2 = triangleAnchorPoints[(i != 2) ? (i + 1) : 0];
					Vector val3 = triangleAnchorPoints[i] - val2;
					if (MathHelper.IsPointAlongVector(point, val2, val3, 10.0))
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
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeNESW;
			}
			return null;
		}
		bool flag = ShapeType == ChartShapeType.Rectangle;
		switch ((resizeMode != ResizeMode.None) ? resizeMode : GetResizeModeForPoint(point, chartControl, chartScale, (int)((DrawingTool)this).DrawingState == 2))
		{
		case ResizeMode.TopLeft:
		case ResizeMode.BottomRight:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : (flag ? Cursors.SizeNWSE : Cursors.SizeNS);
		case ResizeMode.TopRight:
		case ResizeMode.BottomLeft:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : (flag ? Cursors.SizeNESW : Cursors.SizeWE);
		case ResizeMode.MoveAll:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : Cursors.SizeAll;
		default:
			return null;
		}
	}

	private static Point? GetClosestPoint(IEnumerable<Point> inputPoints, Point desired, bool useSensitivity)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		Point val = inputPoints.OrderBy(delegate(Point pt)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			Vector val3 = pt - desired;
			return ((Vector)(ref val3)).Length;
		}).First();
		if (useSensitivity)
		{
			Vector val2 = val - desired;
			if (((Vector)(ref val2)).Length > 15.0)
			{
				return null;
			}
		}
		return val;
	}

	private Point[] GetEllipseAnchorPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
		Point val = default(Point);
		((Point)(ref val))._002Ector(((Rect)(ref anchorsRect)).Left + ((Rect)(ref anchorsRect)).Width / 2.0, ((Rect)(ref anchorsRect)).Top + ((Rect)(ref anchorsRect)).Height / 2.0);
		Point[] array = new Point[5];
		Point val2 = ((Rect)(ref anchorsRect)).TopLeft;
		array[0] = new Point(((Point)(ref val2)).X + ((Rect)(ref anchorsRect)).Width / 2.0, ((Rect)(ref anchorsRect)).Top);
		double right = ((Rect)(ref anchorsRect)).Right;
		val2 = ((Rect)(ref anchorsRect)).TopRight;
		array[1] = new Point(right, ((Point)(ref val2)).Y + ((Rect)(ref anchorsRect)).Height / 2.0);
		array[2] = new Point(((Rect)(ref anchorsRect)).Right - ((Rect)(ref anchorsRect)).Width / 2.0, ((Rect)(ref anchorsRect)).Bottom);
		array[3] = new Point(((Rect)(ref anchorsRect)).Left, ((Rect)(ref anchorsRect)).Top + ((Rect)(ref anchorsRect)).Height / 2.0);
		array[4] = val;
		return (Point[])(object)array;
	}

	private ResizeMode GetResizeModeForPoint(Point pt, ChartControl chartControl, ChartScale chartScale, bool useCursorSens)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
		{
			Point[] ellipseAnchorPoints = GetEllipseAnchorPoints(chartControl, chartScale);
			Point val3 = ellipseAnchorPoints.Last();
			Point? closestPoint2 = GetClosestPoint(ellipseAnchorPoints, pt, useCursorSens);
			if (closestPoint2.HasValue)
			{
				int k;
				for (k = 0; k < ellipseAnchorPoints.Length && !(ellipseAnchorPoints[k] == closestPoint2.Value); k++)
				{
				}
				switch (k)
				{
				case 0:
					return ResizeMode.TopLeft;
				case 1:
					return ResizeMode.TopRight;
				case 2:
					return ResizeMode.BottomRight;
				case 3:
					return ResizeMode.BottomLeft;
				}
			}
			Vector val4 = val3 - pt;
			if (((Vector)(ref val4)).Length < 15.0)
			{
				return ResizeMode.MoveAll;
			}
			for (int l = 0; l < 4; l++)
			{
				Point val5 = ellipseAnchorPoints[(l != 3) ? (l + 1) : 0];
				Vector val6 = ellipseAnchorPoints[l] - val5;
				if (MathHelper.IsPointAlongVector(pt, val5, val6, 25.0))
				{
					return ResizeMode.MoveAll;
				}
			}
			break;
		}
		case ChartShapeType.Rectangle:
		{
			Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
			Point[] array = (Point[])(object)new Point[4]
			{
				((Rect)(ref anchorsRect)).TopLeft,
				((Rect)(ref anchorsRect)).TopRight,
				((Rect)(ref anchorsRect)).BottomRight,
				((Rect)(ref anchorsRect)).BottomLeft
			};
			Point? closestPoint = GetClosestPoint(array, pt, useCursorSens);
			if (closestPoint.HasValue)
			{
				int i;
				for (i = 0; i < array.Length && !(array[i] == closestPoint.Value); i++)
				{
				}
				return i switch
				{
					0 => ResizeMode.TopLeft, 
					1 => ResizeMode.TopRight, 
					2 => ResizeMode.BottomRight, 
					3 => ResizeMode.BottomLeft, 
					_ => ResizeMode.MoveAll, 
				};
			}
			for (int j = 0; j < 4; j++)
			{
				Point val = array[(j != 3) ? (j + 1) : 0];
				Vector val2 = array[j] - val;
				if (MathHelper.IsPointAlongVector(pt, val, val2, 15.0))
				{
					return ResizeMode.MoveAll;
				}
			}
			break;
		}
		}
		return ResizeMode.None;
	}

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
			return GetEllipseAnchorPoints(chartControl, chartScale);
		case ChartShapeType.Rectangle:
		{
			Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
			return (Point[])(object)new Point[4]
			{
				((Rect)(ref anchorsRect)).TopLeft,
				((Rect)(ref anchorsRect)).TopRight,
				((Rect)(ref anchorsRect)).BottomLeft,
				((Rect)(ref anchorsRect)).BottomRight
			};
		}
		case ChartShapeType.Triangle:
			return GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
		default:
			return (Point[])(object)new Point[0];
		}
	}

	private Point[] GetTriangleAnchorPoints(ChartControl chartControl, ChartScale chartScale, bool includeCentroid)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = MiddleAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		if (includeCentroid)
		{
			return (Point[])(object)new Point[4]
			{
				point2,
				point,
				point3,
				new Point((((Point)(ref point2)).X + ((Point)(ref point)).X + ((Point)(ref point3)).X) / 3.0, (((Point)(ref point2)).Y + ((Point)(ref point)).Y + ((Point)(ref point3)).Y) / 3.0)
			};
		}
		return (Point[])(object)new Point[3] { point2, point, point3 };
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = "Shape area",
			ShouldOnlyDisplayName = true
		};
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
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		double minPrice = ((DrawingTool)this).Anchors.Min((ChartAnchor val2) => val2.Price);
		double maxPrice = ((DrawingTool)this).Anchors.Max((ChartAnchor val2) => val2.Price);
		DateTime minTime = ((DrawingTool)this).Anchors.Min((ChartAnchor val2) => val2.Time);
		DateTime maxTime = ((DrawingTool)this).Anchors.Max((ChartAnchor val2) => val2.Time);
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point centerPoint = point + (point2 - point) * 0.5;
		Predicate<ChartAlertValue> predicate;
		switch (ShapeType)
		{
		case ChartShapeType.Rectangle:
			predicate = delegate(ChartAlertValue v)
			{
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_004b: Invalid comparison between Unknown and I4
				bool flag = v.Value >= minPrice && v.Value <= maxPrice && v.Time >= minTime && v.Time <= maxTime;
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		case ChartShapeType.Ellipse:
		{
			double a = Math.Abs(((Point)(ref point2)).X - ((Point)(ref point)).X) / 2.0;
			double b = Math.Abs(((Point)(ref point2)).Y - ((Point)(ref point)).Y) / 2.0;
			predicate = delegate(ChartAlertValue v)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Invalid comparison between Unknown and I4
				bool flag = MathHelper.IsPointInsideEllipse(centerPoint, GetBarPoint(v), a, b);
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		}
		case ChartShapeType.Triangle:
		{
			Point[] trianglePoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: false);
			predicate = delegate(ChartAlertValue v)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				//IL_003c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0042: Invalid comparison between Unknown and I4
				bool flag = MathHelper.IsPointInsideTriangle(GetBarPoint(v), trianglePoints[0], trianglePoints[1], trianglePoints[2]);
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		}
		default:
			return false;
		}
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, predicate);
		Point GetBarPoint(ChartAlertValue v)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if ((int)v.ValueType == 12)
			{
				return new Point(0.0, 0.0);
			}
			return new Point((double)chartControl.GetXByTime(v.Time), (double)chartScale.GetYByValue(v.Value));
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		foreach (Point item in ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.GetPoint(chartControl, chartPanel, chartScale, true)))
		{
			Point current = item;
			num = (float)Math.Min(num, ((Point)(ref current)).X);
			num2 = (float)Math.Max(num2, ((Point)(ref current)).X);
		}
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		DateTime timeByX2 = chartControl.GetTimeByX((int)num2);
		if (timeByX <= lastTimeOnChart)
		{
			return timeByX2 >= firstTimeOnChart;
		}
		return false;
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Expected O, but got Unknown
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		if (ShapeType == ChartShapeType.Unset)
		{
			return;
		}
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			switch (ShapeType)
			{
			case ChartShapeType.Triangle:
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
					break;
				}
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
					break;
				}
				Point[] triangleAnchorPoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
				if (!MathHelper.IsPointInsideTriangle(point, triangleAnchorPoints[0], triangleAnchorPoints[1], triangleAnchorPoints[2]))
				{
					((ChartObject)this).IsSelected = false;
				}
				break;
			}
			case ChartShapeType.Ellipse:
			case ChartShapeType.Rectangle:
			{
				Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
				Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
				editingLeftAnchor = ((((Point)(ref point2)).X <= ((Point)(ref point3)).X) ? StartAnchor : EndAnchor);
				editingTopAnchor = ((((Point)(ref point2)).Y <= ((Point)(ref point3)).Y) ? StartAnchor : EndAnchor);
				editingBottomAnchor = ((((Point)(ref point2)).Y <= ((Point)(ref point3)).Y) ? EndAnchor : StartAnchor);
				editingRightAnchor = ((((Point)(ref point2)).X <= ((Point)(ref point3)).X) ? EndAnchor : StartAnchor);
				Cursor cursor = ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point);
				if (cursor == Cursors.SizeAll || cursor == Cursors.No)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
					break;
				}
				resizeMode = GetResizeModeForPoint(point, chartControl, chartScale, useCursorSens: true);
				if (resizeMode != ResizeMode.None)
				{
					((DrawingTool)this).DrawingState = (DrawingState)((resizeMode != ResizeMode.MoveAll) ? 1 : 3);
					break;
				}
				Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
				if (!((Rect)(ref anchorsRect)).IntersectsWith(new Rect(((Point)(ref point)).X, ((Point)(ref point)).Y, 1.0, 1.0)))
				{
					((ChartObject)this).IsSelected = false;
				}
				break;
			}
			}
			if (lastMouseMoveDataPoint == null)
			{
				lastMouseMoveDataPoint = new ChartAnchor();
			}
			dataPoint.CopyDataValues(lastMouseMoveDataPoint);
		}
		else
		{
			if (StartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(StartAnchor);
				dataPoint.CopyDataValues(MiddleAnchor);
				dataPoint.CopyDataValues(EndAnchor);
				StartAnchor.IsEditing = false;
			}
			else if (ShapeType == ChartShapeType.Triangle && MiddleAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(MiddleAnchor);
				MiddleAnchor.IsEditing = false;
			}
			else if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				EndAnchor.IsEditing = false;
			}
			if (!StartAnchor.IsEditing && !EndAnchor.IsEditing && (ShapeType != ChartShapeType.Triangle || !MiddleAnchor.IsEditing))
			{
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Invalid comparison between Unknown and I4
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		if (ShapeType == ChartShapeType.Unset || (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0))
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (MiddleAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(MiddleAnchor);
			}
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1)
		{
			if (ShapeType == ChartShapeType.Triangle && editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
				return;
			}
			if (lastMouseMoveDataPoint == null)
			{
				lastMouseMoveDataPoint = new ChartAnchor();
			}
			switch (resizeMode)
			{
			case ResizeMode.TopLeft:
				editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
					editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Price = dataPoint.Price;
				}
				break;
			case ResizeMode.BottomRight:
				editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
					editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Price = dataPoint.Price;
				}
				break;
			case ResizeMode.TopRight:
				editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
				editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Time = dataPoint.Time;
					lastMouseMoveDataPoint.SlotIndex = dataPoint.SlotIndex;
				}
				break;
			case ResizeMode.BottomLeft:
				editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
				editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Time = dataPoint.Time;
					lastMouseMoveDataPoint.SlotIndex = dataPoint.SlotIndex;
				}
				break;
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
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			lastMouseMoveDataPoint = null;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			editingAnchor = null;
			editingLeftAnchor = null;
			editingTopAnchor = null;
			editingRightAnchor = null;
			editingBottomAnchor = null;
			resizeMode = ResizeMode.None;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		if (ShapeType == ChartShapeType.Unset)
		{
			return;
		}
		Stroke outlineStroke = OutlineStroke;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = ((Point)(ref point2)).X - ((Point)(ref point)).X;
		double num2 = ((Point)(ref point2)).Y - ((Point)(ref point)).Y;
		Vector2 val2 = DxExtensions.ToVector2(point + (point2 - point) / 2.0);
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush val3 = areaBrush.Clone();
				val3.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = val3;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		else
		{
			areaBrushDevice.RenderTarget = null;
			areaBrushDevice.Brush = null;
		}
		double num3 = ((outlineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
		{
			Ellipse val9 = default(Ellipse);
			((Ellipse)(ref val9))._002Ector(val2, (float)(num / 2.0 + num3), (float)(num2 / 2.0 + num3));
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillEllipse(val9, areaBrushDevice.BrushDX);
			}
			else
			{
				((ChartObject)this).RenderTarget.FillRectangle(new RectangleF(val2.X - 5f, val2.Y - 5f, 15f, 15f), chartControl.SelectionBrush);
			}
			Brush val10 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawEllipse(val9, val10, outlineStroke.Width, outlineStroke.StrokeStyle);
			break;
		}
		case ChartShapeType.Rectangle:
		{
			RectangleF val7 = default(RectangleF);
			((RectangleF)(ref val7))._002Ector((float)(((Point)(ref point)).X + num3), (float)(((Point)(ref point)).Y + num3), (float)num, (float)num2);
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillRectangle(val7, areaBrushDevice.BrushDX);
			}
			Brush val8 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawRectangle(val7, val8, outlineStroke.Width, outlineStroke.StrokeStyle);
			break;
		}
		case ChartShapeType.Triangle:
		{
			PathGeometry val4 = CreateTriangleGeometry(chartControl, val, chartScale, num3);
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val4, areaBrushDevice.BrushDX);
			}
			else
			{
				Point val5 = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true).Last();
				((ChartObject)this).RenderTarget.FillRectangle(new RectangleF((float)((Point)(ref val5)).X - 5f, (float)((Point)(ref val5)).Y - 5f, 15f, 15f), chartControl.SelectionBrush);
			}
			Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val4, val6, outlineStroke.Width, outlineStroke.StrokeStyle);
			((DisposeBase)val4).Dispose();
			break;
		}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I4
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			StartAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			MiddleAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorMiddle,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			EndAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			((DrawingTool)this).DrawingState = (DrawingState)0;
			AreaBrush = (Brush)(object)Brushes.CornflowerBlue;
			AreaOpacity = 40;
			OutlineStroke = new Stroke((Brush)(object)Brushes.CornflowerBlue, 2f);
			ShapeType = ChartShapeType.Unset;
			MiddleAnchor.IsBrowsable = false;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}
