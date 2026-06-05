using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
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

public class Ruler : DrawingTool
{
	private const int cursorSensitivity = 15;

	private ChartAnchor editingAnchor;

	private bool isTextCreated;

	private const float textMargin = 3f;

	private TextFormat textFormat;

	private TextLayout textLayout;

	private Brush textBrush;

	private readonly DeviceBrush textDeviceBrush = new DeviceBrush();

	private readonly DeviceBrush textBackgroundDeviceBrush = new DeviceBrush();

	private string yValueString;

	private string timeText;

	private ValueUnit yValueDisplayUnit;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, EndAnchor, TextAnchor };

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(Order = 3)]
	public ChartAnchor TextAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptGeneral", Order = 2)]
	public Stroke LineColor { get; set; }

	private bool ShouldDrawText
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				ChartAnchor endAnchor = EndAnchor;
				if (endAnchor == null || endAnchor.IsEditing)
				{
					endAnchor = TextAnchor;
					if (endAnchor != null)
					{
						return !endAnchor.IsEditing;
					}
					return false;
				}
			}
			return true;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush TextColor
	{
		get
		{
			return textBrush;
		}
		set
		{
			textBrush = value;
			textDeviceBrush.Brush = value;
		}
	}

	[Browsable(false)]
	public string TextColorSerialize
	{
		get
		{
			return Serialize.BrushToString(TextColor);
		}
		set
		{
			TextColor = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
	public ValueUnit YValueDisplayUnit
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return yValueDisplayUnit;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			yValueDisplayUnit = value;
			isTextCreated = false;
		}
	}

	public override object Icon => Icons.DrawRuler;

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		try
		{
			TextLayout obj = textLayout;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textFormat = null;
			textDeviceBrush.RenderTarget = null;
			textBackgroundDeviceBrush.RenderTarget = null;
		}
		catch
		{
		}
		finally
		{
			LineColor = null;
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
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
			if (editingAnchor == TextAnchor)
			{
				return Cursors.SizeNESW;
			}
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
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
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point4 = TextAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Vector val = point3 - point2;
			Vector val2 = point4 - point3;
			UpdateTextLayout(chartControl, ((ChartObject)this).ChartPanel, chartScale);
			Point val3 = default(Point);
			((Point)(ref val3))._002Ector(((Point)(ref point4)).X - (double)textLayout.MaxWidth - 3.0, ((Point)(ref point4)).Y);
			Point val4 = default(Point);
			((Point)(ref val4))._002Ector(((Point)(ref val3)).X, ((Point)(ref point4)).Y - (double)textLayout.MaxHeight - 6.0);
			Point val5 = default(Point);
			((Point)(ref val5))._002Ector(((Point)(ref point4)).X, ((Point)(ref point4)).Y - (double)textLayout.MaxHeight - 6.0);
			Vector val6 = val3 - point4;
			Vector val7 = val4 - val3;
			Vector val8 = val5 - val4;
			Vector val9 = point4 - val5;
			if (MathHelper.IsPointAlongVector(point, point2, val, 15.0) || MathHelper.IsPointAlongVector(point, point3, val2, 15.0) || MathHelper.IsPointAlongVector(point, point4, val6, 15.0) || MathHelper.IsPointAlongVector(point, val3, val7, 15.0) || MathHelper.IsPointAlongVector(point, val4, val8, 15.0) || MathHelper.IsPointAlongVector(point, val5, val9, 15.0))
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

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		if (ShouldDrawText)
		{
			Point point3 = TextAnchor.GetPoint(chartControl, val, chartScale, true);
			return (Point[])(object)new Point[3] { point, point3, point2 };
		}
		return (Point[])(object)new Point[2] { point, point2 };
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		DateTime dateTime = Globals.MaxDate;
		DateTime dateTime2 = Globals.MinDate;
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
		if (!(dateTime <= lastTimeOnChart))
		{
			if (dateTime <= firstTimeOnChart)
			{
				return dateTime2 >= firstTimeOnChart;
			}
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible)
		{
			((ChartObject)this).MinValue = ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.Price).Min();
			((ChartObject)this).MaxValue = ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.Price).Max();
		}
	}

	public override void OnBarsChanged()
	{
		isTextCreated = false;
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
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
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
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
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			dataPoint.CopyDataValues(EndAnchor);
			dataPoint.CopyDataValues(TextAnchor);
			StartAnchor.IsEditing = false;
		}
		else if (EndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(EndAnchor);
			EndAnchor.IsEditing = false;
			dataPoint.CopyDataValues(TextAnchor);
		}
		else if (TextAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TextAnchor);
			TextAnchor.IsEditing = false;
		}
		if (!StartAnchor.IsEditing && !EndAnchor.IsEditing && !TextAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				dataPoint.CopyDataValues(TextAnchor);
				isTextCreated = false;
			}
			else if (TextAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(TextAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
			if (editingAnchor == StartAnchor || editingAnchor == EndAnchor)
			{
				isTextCreated = false;
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
			TextLayout obj = textLayout;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textLayout = null;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
			}
			editingAnchor = null;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Expected O, but got Unknown
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		LineColor.RenderTarget = ((ChartObject)this).RenderTarget;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = ((MathExtentions.ApproxCompare(LineColor.Width % 2f, 0f) == 0) ? 0.5 : 0.0);
		Vector val2 = default(Vector);
		((Vector)(ref val2))._002Ector(num, num);
		Vector2 val3 = DxExtensions.ToVector2(point2 + val2);
		Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : LineColor.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point + val2), val3, val4, LineColor.Width, LineColor.StrokeStyle);
		if (ShouldDrawText)
		{
			UpdateTextLayout(chartControl, ((ChartObject)this).ChartPanel, chartScale);
			textDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			DeviceBrush obj = textBackgroundDeviceBrush;
			object obj2 = Application.Current.FindResource((object)"ChartControl.DataBoxBackground");
			obj.Brush = (Brush)((obj2 is Brush) ? obj2 : null);
			textBackgroundDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			object obj3 = Application.Current.FindResource((object)"BorderThinBrush");
			object obj4 = ((obj3 is Brush) ? obj3 : null);
			double value = (Application.Current.FindResource((object)"BorderThinThickness") as double?) ?? 1.0;
			if (obj4 == null)
			{
				obj4 = LineColor.Brush;
			}
			Stroke val5 = new Stroke((Brush)obj4, (DashStyleHelper)0, Convert.ToSingle(value))
			{
				RenderTarget = ((ChartObject)this).RenderTarget
			};
			Point point3 = TextAnchor.GetPoint(chartControl, val, chartScale, true);
			Vector2 val6 = DxExtensions.ToVector2(point3 + val2);
			((ChartObject)this).RenderTarget.DrawLine(val3, val6, LineColor.BrushDX, LineColor.Width, LineColor.StrokeStyle);
			float num2 = (float)(num / 2.0);
			RectangleF val7 = default(RectangleF);
			((RectangleF)(ref val7))._002Ector((float)(((Point)(ref point3)).X - (double)textLayout.MaxWidth - 3.0 + (double)num2), (float)(((Point)(ref point3)).Y - (double)textLayout.MaxHeight - 3.0 + (double)num2), textLayout.MaxWidth + 6f, textLayout.MaxHeight + 3f);
			if (textBackgroundDeviceBrush.BrushDX != null && !((ChartObject)this).IsInHitTest)
			{
				((ChartObject)this).RenderTarget.FillRectangle(val7, textBackgroundDeviceBrush.BrushDX);
			}
			((ChartObject)this).RenderTarget.DrawRectangle(val7, val5.BrushDX, val5.Width, val5.StrokeStyle);
			if (textDeviceBrush.BrushDX != null && !((ChartObject)this).IsInHitTest)
			{
				((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)((double)(((RectangleF)(ref val7)).X + 3f) + num), (float)((double)(((RectangleF)(ref val7)).Y + 3f) + num)), textLayout, textDeviceBrush.BrushDX);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRuler;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			TextAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
			TextAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorText;
			LineColor = new Stroke((Brush)(object)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			object obj = Application.Current.FindResource((object)"ChartControl.DataBoxForeground");
			TextColor = (Brush)(((obj is Brush) ? obj : null) ?? Brushes.CornflowerBlue);
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	private void UpdateTextLayout(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected I4, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Invalid comparison between Unknown and I4
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Invalid comparison between Unknown and I4
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		TextLayout val;
		if (isTextCreated)
		{
			val = textLayout;
			if (val != null && !((DisposeBase)val).IsDisposed)
			{
				return;
			}
		}
		TextFormat val2 = textFormat;
		if (val2 != null && !((DisposeBase)val2).IsDisposed)
		{
			((DisposeBase)textFormat).Dispose();
		}
		val = textLayout;
		if (val != null && !((DisposeBase)val).IsDisposed)
		{
			((DisposeBase)textLayout).Dispose();
		}
		ChartBars attachedToChartBars = ((DrawingTool)this).GetAttachedToChartBars();
		if (attachedToChartBars != null)
		{
			double num = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EndAnchor.Price - StartAnchor.Price);
			double num2 = num / ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize;
			ValueUnit val3 = YValueDisplayUnit;
			switch ((int)val3)
			{
			case 0:
				yValueString = attachedToChartBars.Bars.Instrument.MasterInstrument.FormatPrice(num, true);
				break;
			case 3:
				yValueString = (((int)((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.InstrumentType == 4) ? Globals.FormatCurrency((double)((int)num2 * Account.All[0].ForexLotSize) * (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize * ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.PointValue)) : Globals.FormatCurrency((double)(int)num2 * (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize * ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.PointValue)));
				break;
			case 1:
				yValueString = (num / ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(StartAnchor.Price)).ToString("P", Globals.GeneralOptions.CurrentCulture);
				break;
			case 2:
				yValueString = num2.ToString("F0");
				break;
			case 4:
			{
				double num3 = Math.Abs(num2 / 10.0);
				char c = char.Parse(Globals.GeneralOptions.CurrentCulture.NumberFormat.NumberDecimalSeparator);
				yValueString = ((int.Parse(num3.ToString("F1").Split(new char[1] { c })[1]) > 0) ? num3.ToString("F1").Replace(c, '\'') : num3.ToString("F0"));
				break;
			}
			}
			TimeSpan timeSpan = EndAnchor.Time - StartAnchor.Time;
			timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			bool flag = Math.Abs(timeSpan.TotalHours) >= 24.0;
			if ((int)attachedToChartBars.Bars.BarsPeriod.BarsPeriodType == 5)
			{
				int num4 = Math.Abs(timeSpan.Days);
				timeText = ((num4 > 1) ? $"{Math.Abs(timeSpan.Days)} {Resource.Days}" : $"{Math.Abs(timeSpan.Days)} {Resource.Day}");
			}
			else
			{
				timeText = (flag ? $"{string.Format(Resource.NinjaScriptDrawingToolRulerDaysFormat, Math.Abs(timeSpan.Days))}\n{timeSpan.Subtract(new TimeSpan(timeSpan.Days, 0, 0, 0)).Duration(),25}" : timeSpan.Duration().ToString());
			}
			Point point = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point2 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			int barIdxByX = attachedToChartBars.GetBarIdxByX(chartControl, (int)((Point)(ref point)).X);
			int num5 = attachedToChartBars.GetBarIdxByX(chartControl, (int)((Point)(ref point2)).X) - barIdxByX;
			SimpleFont val4 = (SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()));
			textFormat = val4.ToDirectWriteTextFormat();
			textFormat.TextAlignment = (TextAlignment)0;
			textFormat.WordWrapping = (WordWrapping)1;
			string text = string.Format("{0}\n{1,-11}{2,-11}\n{3,-11}{4,-11}\n{5,-10}{6,-10}", new object[7]
			{
				((DrawingTool)this).AttachedTo.DisplayName,
				Resource.NinjaScriptDrawingToolRulerNumberBarsText,
				num5,
				Resource.NinjaScriptDrawingToolRulerTimeText,
				timeText,
				Resource.NinjaScriptDrawingToolRulerYValueText,
				yValueString
			});
			textLayout = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 600f, 600f);
			textLayout.MaxWidth = textLayout.Metrics.Width;
			textLayout.MaxHeight = textLayout.Metrics.Height;
			isTextCreated = true;
		}
	}
}
