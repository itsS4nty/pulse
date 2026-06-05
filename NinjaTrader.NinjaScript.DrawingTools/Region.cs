using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class Region : DrawingTool
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	public ChartAnchor StartAnchor { get; set; }

	public ChartAnchor EndAnchor { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public ISeries<double> Series1 { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public ISeries<double> Series2 { get; set; }

	[Browsable(false)]
	public double Price { get; set; }

	[Browsable(false)]
	public int Displacement { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 4)]
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
				areaBrushDevice.Brush = null;
			}
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 5)]
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

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
	public Stroke OutlineStroke { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!(((DrawingTool)this).AttachedTo.ChartObject is IChartBars) || ((IChartBars)((DrawingTool)this).AttachedTo.ChartObject).ChartBars == null)
		{
			return false;
		}
		if (!StartAnchor.IsNinjaScriptDrawn || !EndAnchor.IsNinjaScriptDrawn)
		{
			return false;
		}
		DateTime time = StartAnchor.Time;
		DateTime time2 = EndAnchor.Time;
		if (!(time >= firstTimeOnChart) && !(time2 <= lastTimeOnChart))
		{
			if (time < firstTimeOnChart)
			{
				return time2 > lastTimeOnChart;
			}
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolRegion;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRegion;
			((DrawingTool)this).DisplayOnChartsMenus = false;
			((DrawingTool)this).IgnoresUserInput = true;
			StartAnchor = new ChartAnchor
			{
				IsYPropertyVisible = false,
				IsXPropertiesVisible = false
			};
			EndAnchor = new ChartAnchor
			{
				IsYPropertyVisible = false,
				IsXPropertiesVisible = false
			};
			StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
			AreaBrush = (Brush)(object)Brushes.DarkCyan;
			OutlineStroke = new Stroke((Brush)(object)Brushes.Goldenrod);
			AreaOpacity = 40;
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)1;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Invalid comparison between Unknown and I4
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Invalid comparison between Unknown and I4
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Invalid comparison between Unknown and I4
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_069b: Expected O, but got Unknown
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Invalid comparison between Unknown and I4
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Invalid comparison between Unknown and I4
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Invalid comparison between Unknown and I4
		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Invalid comparison between Unknown and I4
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0730: Unknown result type (might be due to invalid IL or missing references)
		//IL_0732: Unknown result type (might be due to invalid IL or missing references)
		//IL_0737: Unknown result type (might be due to invalid IL or missing references)
		//IL_073b: Unknown result type (might be due to invalid IL or missing references)
		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Invalid comparison between Unknown and I4
		//IL_075b: Unknown result type (might be due to invalid IL or missing references)
		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0560: Unknown result type (might be due to invalid IL or missing references)
		//IL_0565: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		if (Series1 == null)
		{
			return;
		}
		ChartBars chartBars = ((IChartBars)((DrawingTool)this).AttachedTo.ChartObject).ChartBars;
		IChartObject chartObject = ((DrawingTool)this).AttachedTo.ChartObject;
		NinjaScriptBase val = (NinjaScriptBase)(object)((chartObject is NinjaScriptBase) ? chartObject : null);
		if (val == null || chartBars == null || Math.Abs(Series1.Count - chartBars.Count) > 1)
		{
			return;
		}
		int num;
		int num2;
		if ((int)chartControl.BarSpacingType == 3)
		{
			num = chartBars.GetBarIdxByTime(chartControl, StartAnchor.Time);
			num2 = chartBars.GetBarIdxByTime(chartControl, EndAnchor.Time);
		}
		else
		{
			num = StartAnchor.DrawnOnBar - StartAnchor.BarsAgo;
			num2 = EndAnchor.DrawnOnBar - EndAnchor.BarsAgo;
			if (num == num2)
			{
				num = chartBars.GetBarIdxByTime(chartControl, StartAnchor.Time);
				num2 = chartBars.GetBarIdxByTime(chartControl, EndAnchor.Time);
			}
		}
		int num3 = Math.Min(num, num2);
		int num4 = Math.Max(num, num2);
		int num5 = Math.Max(val.BarsRequiredToPlot + Displacement, chartBars.GetBarIdxByTime(chartControl, chartControl.GetTimeByX(0)) - 1);
		int num6 = Math.Max(chartBars.ToIndex, chartBars.GetBarIdxByTime(chartControl, chartControl.LastTimePainted)) + 1;
		num3 = Math.Max(0, Math.Max(num5, num3 + Displacement));
		num4 = Math.Max(0, Math.Min(num4 + Displacement, num6));
		if (num3 > num6 || num4 < num5)
		{
			return;
		}
		ISeries<double> val2 = Series1;
		ISeries<double> val3 = Series2;
		ISeries<double> series = Series1;
		NinjaScriptBase val4 = (NinjaScriptBase)(object)((series is NinjaScriptBase) ? series : null);
		if (val4 != null)
		{
			val2 = (ISeries<double>)(object)val4.Value;
		}
		if (val2 == null)
		{
			return;
		}
		ISeries<double> series2 = Series2;
		NinjaScriptBase val5 = (NinjaScriptBase)(object)((series2 is NinjaScriptBase) ? series2 : null);
		if (val5 != null)
		{
			val3 = (ISeries<double>)(object)val5.Value;
		}
		Vector2[] array = Array.Empty<Vector2>();
		int num7 = 0;
		int num8 = 0;
		Vector2[] array2;
		if (val3 == null)
		{
			array2 = (Vector2[])(object)new Vector2[num4 - num3 + 1 + 2];
			Vector val6 = default(Vector);
			for (int i = num3; i <= num4; i++)
			{
				if (i >= Math.Max(0, Displacement) && i <= Math.Max(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2) + Displacement, num4))
				{
					int num9 = Math.Min(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2), Math.Max(0, i - Displacement));
					double valueAt = val2.GetValueAt(num9);
					float num10 = chartScale.GetYByValue(valueAt);
					float num11 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && i >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, i)) : chartControl.GetXByBarIndex(chartBars, i));
					double num12 = ((num11 % 1f != 0f) ? 0.0 : 0.5);
					double num13 = ((num10 % 1f != 0f) ? 0.0 : 0.5);
					((Vector)(ref val6))._002Ector(num12, num13);
					Point val7 = new Point((double)num11, (double)num10) + val6;
					array2[num7] = DxExtensions.ToVector2(val7);
					num7++;
				}
			}
			array2[num7].X = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num4 >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, num4)) : chartControl.GetXByBarIndex(chartBars, num4));
			array2[num7++].Y = chartScale.GetYByValue(Math.Max(chartScale.MinValue, Math.Min(chartScale.MaxValue, Price)));
			array2[num7].X = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num3 >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, num3)) : chartControl.GetXByBarIndex(chartBars, num3));
			array2[num7++].Y = chartScale.GetYByValue(Math.Max(chartScale.MinValue, Math.Min(chartScale.MaxValue, Price)));
		}
		else
		{
			array2 = (Vector2[])(object)new Vector2[num4 - num3 + 1];
			array = (Vector2[])(object)new Vector2[num4 - num3 + 1];
			Vector val8 = default(Vector);
			for (int j = num3; j <= num4; j++)
			{
				if (j < Math.Max(0, Displacement) || j > Math.Max(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2) + Displacement, num4))
				{
					continue;
				}
				int num14 = Math.Min(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2), Math.Max(0, j - Displacement));
				float num15 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && j >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, j)) : chartControl.GetXByBarIndex(chartBars, j));
				if (val2.IsValidDataPointAt(num14))
				{
					double valueAt2 = val2.GetValueAt(num14);
					float num16 = chartScale.GetYByValue(valueAt2);
					double num17 = ((num15 % 1f != 0f) ? 0.0 : 0.5);
					double num18 = ((num16 % 1f != 0f) ? 0.0 : 0.5);
					((Vector)(ref val8))._002Ector(num17, num18);
					Point val9 = new Point((double)num15, (double)num16) + val8;
					array2[num7] = DxExtensions.ToVector2(val9);
					num7++;
					if (val3.IsValidDataPointAt(num14))
					{
						valueAt2 = val3.GetValueAt(num14);
						num16 = chartScale.GetYByValue(valueAt2);
						num18 = ((num16 % 1f != 0f) ? 0.0 : 0.5);
						((Vector)(ref val8))._002Ector(num17, num18);
						val9 = new Point((double)num15, (double)num16) + val8;
						array[num8] = DxExtensions.ToVector2(val9);
						num8++;
					}
				}
			}
		}
		if (num7 + num8 <= 2)
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		if (OutlineStroke != null)
		{
			OutlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		if (AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush val10 = areaBrush.Clone();
				val10.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = val10;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		PathGeometry val11 = new PathGeometry(Globals.D2DFactory);
		GeometrySink val12 = val11.Open();
		double num19 = ((array2[0].X % 1f != 0f) ? 0.0 : 0.5);
		double num20 = ((array2[0].Y % 1f != 0f) ? 0.0 : 0.5);
		Vector val13 = default(Vector);
		((Vector)(ref val13))._002Ector(num19, num20);
		Point val14 = new Point((double)array2[0].X, (double)array2[0].Y) + val13;
		((SimplifiedGeometrySink)val12).BeginFigure(DxExtensions.ToVector2(val14), (FigureBegin)0);
		((SimplifiedGeometrySink)val12).SetFillMode((FillMode)1);
		for (int k = 1; k < num7; k++)
		{
			val12.AddLine(array2[k]);
		}
		for (int num21 = num8 - 1; num21 >= 0; num21--)
		{
			val12.AddLine(array[num21]);
		}
		((SimplifiedGeometrySink)val12).EndFigure((FigureEnd)1);
		((SimplifiedGeometrySink)val12).Close();
		object obj2;
		if (!((ChartObject)this).IsInHitTest)
		{
			DeviceBrush obj = areaBrushDevice;
			obj2 = ((obj != null) ? obj.BrushDX : null);
		}
		else
		{
			obj2 = chartControl.SelectionBrush;
		}
		Brush val15 = (Brush)obj2;
		if (val15 != null)
		{
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val11, val15);
		}
		object obj3;
		if (!((ChartObject)this).IsInHitTest)
		{
			Stroke outlineStroke = OutlineStroke;
			obj3 = ((outlineStroke != null) ? outlineStroke.BrushDX : null);
		}
		else
		{
			obj3 = chartControl.SelectionBrush;
		}
		val15 = (Brush)obj3;
		if (val15 != null)
		{
			((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val11, val15, OutlineStroke.Width);
		}
		((DisposeBase)val11).Dispose();
	}
}
