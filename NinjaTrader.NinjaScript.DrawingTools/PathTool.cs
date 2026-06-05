using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class PathTool : PathToolSegmentContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum PathToolCapMode
	{
		Arrow,
		Line
	}

	private PathGeometry arrowPathGeometry;

	private const double cursorSensitivity = 15.0;

	private DispatcherTimer doubleClickTimer;

	private ChartAnchor editingAnchor;

	[Browsable(false)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public List<ChartAnchor> ChartAnchors { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 0)]
	public Stroke OutlineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathBegin", GroupName = "NinjaScriptGeneral", Order = 1)]
	public PathToolCapMode PathBegin { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathEnd", GroupName = "NinjaScriptGeneral", Order = 2)]
	public PathToolCapMode PathEnd { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathShowCount", GroupName = "NinjaScriptGeneral", Order = 3)]
	public bool ShowCount { get; set; }

	[Display(Order = 0)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public ChartAnchor StartAnchor
	{
		get
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return new ChartAnchor
				{
					DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
					IsEditing = true,
					DrawingTool = (IDrawingTool)(object)this
				};
			}
			return ChartAnchors[0];
		}
		set
		{
			if (ChartAnchors != null)
			{
				if (ChartAnchors.Count == 0)
				{
					ChartAnchors.Add(value);
				}
				else
				{
					ChartAnchors[0] = value;
				}
			}
		}
	}

	public override IEnumerable<ChartAnchor> Anchors
	{
		get
		{
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { StartAnchor };
			}
			return ChartAnchors.ToArray();
		}
	}

	public override object Icon => Icons.DrawPath;

	public override bool SupportsAlerts => true;

	public override void CopyTo(NinjaScript ninjaScript)
	{
		base.CopyTo(ninjaScript);
		if (ninjaScript is PathTool pathTool)
		{
			if (ChartAnchors == null)
			{
				return;
			}
			pathTool.ChartAnchors.Clear();
			{
				foreach (ChartAnchor chartAnchor in ChartAnchors)
				{
					List<ChartAnchor> chartAnchors = pathTool.ChartAnchors;
					object obj = chartAnchor.Clone();
					chartAnchors.Add((ChartAnchor)((obj is ChartAnchor) ? obj : null));
				}
				return;
			}
		}
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("ChartAnchors");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (ChartAnchor chartAnchor2 in ChartAnchors)
		{
			try
			{
				object obj2 = chartAnchor2.Clone();
				ChartAnchor val = (ChartAnchor)((obj2 is ChartAnchor) ? obj2 : null);
				if (val != null)
				{
					list.Add(val);
				}
			}
			catch
			{
			}
		}
	}

	private PathGeometry CreatePathGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, double pixelAdjust)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		List<Vector2> list = new List<Vector2>();
		Vector val = default(Vector);
		((Vector)(ref val))._002Ector(pixelAdjust, pixelAdjust);
		for (int i = 0; i < ChartAnchors.Count; i++)
		{
			Point point = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
			list.Add(DxExtensions.ToVector2(point + val));
			if (i + 1 < ChartAnchors.Count)
			{
				Point point2 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
				list.Add(DxExtensions.ToVector2(point2 + val));
			}
		}
		PathGeometry val2 = new PathGeometry(Globals.D2DFactory);
		GeometrySink obj = val2.Open();
		((SimplifiedGeometrySink)obj).BeginFigure(list[0], (FigureBegin)0);
		((SimplifiedGeometrySink)obj).AddLines(list.ToArray());
		((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)obj).Close();
		return val2;
	}

	private void DoubleClickTimerTick(object sender, EventArgs e)
	{
		doubleClickTimer.Stop();
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (ChartAnchors == null || ChartAnchors.Count == 0)
		{
			yield break;
		}
		foreach (PathToolSegment pathToolSegment in base.PathToolSegments)
		{
			yield return new AlertConditionItem
			{
				Name = pathToolSegment.Name,
				ShouldOnlyDisplayName = true,
				Tag = pathToolSegment
			};
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
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
		if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) == null)
		{
			Point[] pathAnchorPoints = GetPathAnchorPoints(chartControl, chartScale);
			if (pathAnchorPoints.Length != 0)
			{
				Vector val = pathAnchorPoints.Last() - point;
				if (((Vector)(ref val)).Length <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			for (int i = 0; i < ChartAnchors.Count; i++)
			{
				Point point2 = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
				if (i + 1 < ChartAnchors.Count)
				{
					Point point3 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
					if (MathHelper.IsPointAlongVector(point, point2, point3 - point2, 15.0))
					{
						if (!((DrawingTool)this).IsLocked)
						{
							return Cursors.SizeAll;
						}
						return Cursors.Arrow;
					}
					continue;
				}
				Point point4 = ChartAnchors[0].GetPoint(chartControl, chartPanel, chartScale, true);
				if (MathHelper.IsPointAlongVector(point, point4, point2 - point4, 15.0))
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

	[DllImport("user32.dll")]
	private static extern uint GetDoubleClickTime();

	private Point[] GetPathAnchorPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point[] array = (Point[])(object)new Point[ChartAnchors.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ChartAnchors[i].GetPoint(chartControl, val, chartScale, true);
		}
		return array;
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		return GetPathAnchorPoints(chartControl, chartScale);
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
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Invalid comparison between Unknown and I4
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected I4, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Invalid comparison between Unknown and I4
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Invalid comparison between Unknown and I4
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Invalid comparison between Unknown and I4
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Invalid comparison between Unknown and I4
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Invalid comparison between Unknown and I4
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Invalid comparison between Unknown and I4
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is PathToolSegment pathToolSegment))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = pathToolSegment.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = pathToolSegment.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = double.MaxValue;
		double num2 = double.MinValue;
		Point[] array = (Point[])(object)new Point[2] { point, point2 };
		for (int i = 0; i < array.Length; i++)
		{
			Point val2 = array[i];
			num = Math.Min(num, ((Point)(ref val2)).X);
			num2 = Math.Max(num2, ((Point)(ref val2)).X);
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
		Point leftPoint = ((((Point)(ref point)).X < ((Point)(ref point2)).X) ? point : point2);
		Point rightPoint = ((((Point)(ref point2)).X > ((Point)(ref point)).X) ? point2 : point);
		Point val3 = default(Point);
		((Point)(ref val3))._002Ector(num3, num4);
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, val3);
		Condition val4 = condition;
		switch ((int)val4)
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
			double num5 = chartControl.GetXByTime(v.Time);
			double num6 = chartScale.GetYByValue(v.Value);
			Point val5 = default(Point);
			((Point)(ref val5))._002Ector(num5, num6);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(leftPoint, rightPoint, val5);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
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
		foreach (Point item in ChartAnchors.Select((ChartAnchor a) => a.GetPoint(chartControl, chartPanel, chartScale, true)))
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

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !ChartAnchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, chartAnchor.Price);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, chartAnchor.Price);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (ChartAnchors.Count == 0)
		{
			ChartAnchors.Add(new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			});
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			if (chartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(chartAnchor);
				chartAnchor.IsEditing = false;
			}
		}
		ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
		if (ChartAnchors.Count > 1 && doubleClickTimer.IsEnabled && closestAnchor != null && closestAnchor != ChartAnchors[ChartAnchors.Count - 1])
		{
			ChartAnchors.Remove(ChartAnchors[ChartAnchors.Count - 1]);
			base.PathToolSegments.Remove(base.PathToolSegments[base.PathToolSegments.Count - 1]);
			doubleClickTimer.Stop();
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
			return;
		}
		ChartAnchor val = new ChartAnchor
		{
			DisplayName = Resource.NinjaScriptDrawingToolAnchor,
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		dataPoint.CopyDataValues(val);
		ChartAnchors.Add(val);
		if (ChartAnchors.Count > 1)
		{
			base.PathToolSegments.Add(new PathToolSegment(ChartAnchors[ChartAnchors.Count - 2], ChartAnchors[ChartAnchors.Count - 1], $"{Resource.NinjaScriptDrawingToolPathSegment} {base.PathToolSegments.Count + 1}"));
			if (!doubleClickTimer.IsEnabled)
			{
				doubleClickTimer.Start();
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
		{
			foreach (ChartAnchor chartAnchor in ChartAnchors)
			{
				if (chartAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(chartAnchor);
				}
			}
			break;
		}
		case 1:
			if (editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
			}
			break;
		case 3:
		{
			foreach (ChartAnchor chartAnchor2 in ChartAnchors)
			{
				chartAnchor2.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			break;
		}
		case 2:
			break;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
				editingAnchor = null;
			}
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0503: Expected O, but got Unknown
		//IL_0503: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Expected O, but got Unknown
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double num = ((outlineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		Vector val2 = default(Vector);
		((Vector)(ref val2))._002Ector(num, num);
		PathGeometry val3 = CreatePathGeometry(chartControl, val, chartScale, num);
		Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val3, val4, outlineStroke.Width, outlineStroke.StrokeStyle);
		((DisposeBase)val3).Dispose();
		if (PathBegin == PathToolCapMode.Arrow || PathEnd == PathToolCapMode.Arrow)
		{
			Point[] pathAnchorPoints = GetPathAnchorPoints(chartControl, chartScale);
			if (pathAnchorPoints.Length > 1)
			{
				if (arrowPathGeometry == null)
				{
					arrowPathGeometry = new PathGeometry(Globals.D2DFactory);
					GeometrySink obj = arrowPathGeometry.Open();
					float num2 = 6f;
					Vector2 val5 = default(Vector2);
					((Vector2)(ref val5))._002Ector(0f, outlineStroke.Width * 0.5f);
					((SimplifiedGeometrySink)obj).BeginFigure(val5, (FigureBegin)0);
					obj.AddLine(new Vector2(num2, 0f - num2));
					obj.AddLine(new Vector2(0f - num2, 0f - num2));
					obj.AddLine(val5);
					((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
					((SimplifiedGeometrySink)obj).Close();
				}
				if (PathBegin == PathToolCapMode.Arrow)
				{
					Vector val6 = pathAnchorPoints[0] - pathAnchorPoints[1];
					((Vector)(ref val6)).Normalize();
					Vector2 val7 = DxExtensions.ToVector2(pathAnchorPoints[0] + val2);
					float num3 = 0f - (float)Math.Atan2(((Vector)(ref val6)).X, ((Vector)(ref val6)).Y);
					Vector val8 = val6 * 5.0;
					Vector2 val9 = default(Vector2);
					((Vector2)(ref val9))._002Ector((float)((double)val7.X + ((Vector)(ref val8)).X), (float)((double)val7.Y + ((Vector)(ref val8)).Y));
					Matrix3x2 transform = Matrix3x2.Rotation(num3, Vector2.Zero) * Matrix3x2.Scaling((float)Math.Max(1.0, (double)outlineStroke.Width * 0.45) + 0.25f) * Matrix3x2.Translation(val9);
					((ChartObject)this).RenderTarget.Transform = transform;
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)arrowPathGeometry, val4);
					((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
				}
				if (PathEnd == PathToolCapMode.Arrow)
				{
					Vector val10 = pathAnchorPoints[^1] - pathAnchorPoints[^2];
					((Vector)(ref val10)).Normalize();
					Vector2 val11 = DxExtensions.ToVector2(pathAnchorPoints[^1] + val2);
					float num4 = 0f - (float)Math.Atan2(((Vector)(ref val10)).X, ((Vector)(ref val10)).Y);
					Vector val12 = val10 * 5.0;
					Vector2 val13 = default(Vector2);
					((Vector2)(ref val13))._002Ector((float)((double)val11.X + ((Vector)(ref val12)).X), (float)((double)val11.Y + ((Vector)(ref val12)).Y));
					Matrix3x2 transform2 = Matrix3x2.Rotation(num4, Vector2.Zero) * Matrix3x2.Scaling((float)Math.Max(1.0, (double)outlineStroke.Width * 0.45) + 0.25f) * Matrix3x2.Translation(val13);
					((ChartObject)this).RenderTarget.Transform = transform2;
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)arrowPathGeometry, val4);
					((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
				}
			}
		}
		if (!ShowCount)
		{
			return;
		}
		TextFormat val14 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
		val14.TextAlignment = (TextAlignment)0;
		val14.WordWrapping = (WordWrapping)1;
		for (int i = 1; i < ChartAnchors.Count; i++)
		{
			Point point = ChartAnchors[i - 1].GetPoint(chartControl, val, chartScale, true);
			Point point2 = ChartAnchors[i].GetPoint(chartControl, val, chartScale, true);
			if (i + 1 < ChartAnchors.Count)
			{
				Point point3 = ChartAnchors[i + 1].GetPoint(chartControl, val, chartScale, true);
				Vector val15 = point - point2;
				((Vector)(ref val15)).Normalize();
				Vector val16 = point3 - point2;
				((Vector)(ref val16)).Normalize();
				Vector val17 = val15 + val16;
				((Vector)(ref val17)).Normalize();
				TextLayout val18 = new TextLayout(Globals.DirectWriteFactory, i.ToString(), val14, 250f, val14.FontSize);
				Point val19 = point2 - val17 * (double)val14.FontSize;
				((Point)(ref val19)).X = ((Point)(ref val19)).X - (double)(val18.Metrics.Width / 2f);
				((Point)(ref val19)).Y = ((Point)(ref val19)).Y - (double)(val18.Metrics.Height / 2f);
				((ChartObject)this).RenderTarget.DrawTextLayout(DxExtensions.ToVector2(val19 + val2), val18, outlineStroke.BrushDX, (DrawTextOptions)1);
				((DisposeBase)val18).Dispose();
			}
			else
			{
				TextLayout val20 = new TextLayout(Globals.DirectWriteFactory, i.ToString(), val14, 250f, val14.FontSize);
				Vector val21 = point - point2;
				((Vector)(ref val21)).Normalize();
				Point val22 = point2 - val21 * (double)val14.FontSize;
				((Point)(ref val22)).X = ((Point)(ref val22)).X - (double)(val20.Metrics.Width / 2f);
				((Point)(ref val22)).Y = ((Point)(ref val22)).Y - (double)(val20.Metrics.Height / 2f);
				((ChartObject)this).RenderTarget.DrawTextLayout(DxExtensions.ToVector2(val22 + val2), val20, outlineStroke.BrushDX, (DrawTextOptions)1);
				((DisposeBase)val20).Dispose();
			}
		}
		((DisposeBase)val14).Dispose();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((DrawingTool)this).DrawingState = (DrawingState)0;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolPath;
			OutlineStroke = new Stroke((Brush)(object)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f, 100);
			ChartAnchors = new List<ChartAnchor>();
			PathBegin = PathToolCapMode.Line;
			PathEnd = PathToolCapMode.Line;
			ShowCount = false;
		}
		else if ((int)((NinjaScript)this).State == 3 && doubleClickTimer == null)
		{
			doubleClickTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, (int)GetDoubleClickTime()), (DispatcherPriority)4, (EventHandler)DoubleClickTimerTick, Dispatcher.CurrentDispatcher);
		}
	}
}
