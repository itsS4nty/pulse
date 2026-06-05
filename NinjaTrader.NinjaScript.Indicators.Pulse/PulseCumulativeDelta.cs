using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

public class PulseCumulativeDelta : Indicator
{
	private const bool DEBUG_MODE = false;

	private long cumulativeDelta;

	private long sessionStartDelta;

	private DateTime sessionDate = DateTime.MinValue;

	private bool sessionStarted;

	private int lastProcessedBar = -1;

	private long currentBarDelta;

	private double previousCdClose;

	private double buys;

	private double sells;

	private double cdOpen;

	private double cdClose;

	private double cdHigh;

	private double cdLow;

	private bool lastInTransition;

	private PulseCumulativeDeltaResetPeriod resetPeriod;

	private bool showZeroLine = true;

	private bool colorBasedOnDirection = true;

	private double deltaMultiplier = 0.3;

	private bool showCumulative;

	private Brush positiveBrush;

	private Brush negativeBrush;

	private Brush zeroLineBrush;

	private SolidColorBrush positiveBrushDx;

	private SolidColorBrush negativeBrushDx;

	private SolidColorBrush zeroLineBrushDx;

	private StrokeStyle zeroLineDashStrokeStyle;

	private long previousCumulativeDelta;

	[NinjaScriptProperty]
	[Display(Name = "Reset Period", Description = "When to reset cumulative delta calculation", Order = 1, GroupName = "Parameters")]
	public PulseCumulativeDeltaResetPeriod ResetPeriod
	{
		get
		{
			return resetPeriod;
		}
		set
		{
			resetPeriod = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Zero Line", Description = "Show dashed line at zero level", Order = 2, GroupName = "Parameters")]
	public bool ShowZeroLine
	{
		get
		{
			return showZeroLine;
		}
		set
		{
			showZeroLine = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Color Based on Direction", Description = "Change color based on delta direction", Order = 3, GroupName = "Parameters")]
	public bool ColorBasedOnDirection
	{
		get
		{
			return colorBasedOnDirection;
		}
		set
		{
			colorBasedOnDirection = value;
		}
	}

	[NinjaScriptProperty]
	[Range(0.1, 5.0)]
	[Display(Name = "Delta Multiplier", Description = "Multiplier to calibrate delta calculation (1.0 = normal, adjust to match real data)", Order = 4, GroupName = "Parameters")]
	public double DeltaMultiplier
	{
		get
		{
			return deltaMultiplier;
		}
		set
		{
			deltaMultiplier = Math.Max(0.1, Math.Min(5.0, value));
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Cumulative", Description = "True = cumulative delta, False = delta per bar", Order = 5, GroupName = "Parameters")]
	public bool ShowCumulative
	{
		get
		{
			return showCumulative;
		}
		set
		{
			showCumulative = value;
		}
	}

	[XmlIgnore]
	[Display(Name = "Positive Color", Description = "Color for positive delta", Order = 1, GroupName = "Visual")]
	public Brush PositiveBrush
	{
		get
		{
			return positiveBrush;
		}
		set
		{
			positiveBrush = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string PositiveBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(positiveBrush);
		}
		set
		{
			positiveBrush = Serialize.StringToBrush(value);
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = "Negative Color", Description = "Color for negative delta", Order = 2, GroupName = "Visual")]
	public Brush NegativeBrush
	{
		get
		{
			return negativeBrush;
		}
		set
		{
			negativeBrush = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string NegativeBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(negativeBrush);
		}
		set
		{
			negativeBrush = Serialize.StringToBrush(value);
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = "Zero Line Color", Description = "Color for zero reference line", Order = 3, GroupName = "Visual")]
	public Brush ZeroLineBrush
	{
		get
		{
			return zeroLineBrush;
		}
		set
		{
			zeroLineBrush = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string ZeroLineBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(zeroLineBrush);
		}
		set
		{
			zeroLineBrush = Serialize.StringToBrush(value);
			DisposeDx();
		}
	}

	[Browsable(false)]
	public double CurrentDelta => cdClose;

	[Browsable(false)]
	public double CurrentBuys => buys;

	[Browsable(false)]
	public double CurrentSells => sells;

	[Browsable(false)]
	public double CurrentBarDelta => cdClose - previousCdClose;

	public PulseCumulativeDelta()
	{
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Invalid comparison between Unknown and I4
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			((NinjaScript)this).Description = "Pulse Cumulative Delta - Real bid/ask delta calculation using tick data";
			((NinjaScriptBase)this).Name = "PulseCumulativeDelta";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			resetPeriod = PulseCumulativeDeltaResetPeriod.Session;
			showZeroLine = true;
			colorBasedOnDirection = true;
			positiveBrush = (Brush)(object)Brushes.DimGray;
			negativeBrush = (Brush)(object)Brushes.DarkGray;
			zeroLineBrush = (Brush)(object)Brushes.Gray;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaOpen");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaHigh");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaLow");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.LightGray), (PlotStyle)12, "DeltaClose");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else
		{
			_ = ((NinjaScript)this).State;
			_ = 4;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Invalid comparison between Unknown and I4
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBars[0] < 5 || (((NinjaScriptBase)this).BarsArray.Length > 1 && ((NinjaScriptBase)this).CurrentBars[1] < 5))
		{
			return;
		}
		if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			int num = ((((NinjaScriptBase)this).BarsArray.Length > 1) ? (((NinjaScriptBase)this).BarsArray[1].Count - 1 - ((NinjaScriptBase)this).CurrentBars[1]) : 0);
			if (((NinjaScriptBase)this).IsFirstTickOfBar && (int)((NinjaScriptBase)this).Calculate != 0 && ((int)((NinjaScript)this).State == 7 || ((NinjaScriptBase)this).BarsArray[0].IsTickReplay))
			{
				if (((NinjaScriptBase)this).CurrentBars[0] > 0)
				{
					SetValues(1);
				}
				if (((NinjaScriptBase)this).BarsArray[0].IsTickReplay || ((int)((NinjaScript)this).State == 7 && num == 0))
				{
					ResetValues(isNewSession: false, cdClose);
				}
			}
			SetValues(0);
			if ((int)((NinjaScriptBase)this).Calculate == 0 || (lastProcessedBar != ((NinjaScriptBase)this).CurrentBars[0] && ((int)((NinjaScript)this).State == 5 || ((int)((NinjaScript)this).State == 7 && num > 0))))
			{
				ResetValues(isNewSession: false, cdClose);
			}
			lastProcessedBar = ((NinjaScriptBase)this).CurrentBars[0];
			UpdatePlotColors();
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 1 && ((NinjaScriptBase)this).BarsArray.Length > 1)
		{
			if (((NinjaScriptBase)this).BarsArray[1].IsFirstBarOfSession)
			{
				ResetValues(isNewSession: true, cdClose);
			}
			CalculateRealDelta(forceCurrentBar: false);
		}
	}

	private void CheckSessionReset()
	{
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
		bool flag = false;
		switch (resetPeriod)
		{
		case PulseCumulativeDeltaResetPeriod.Session:
		{
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				sessionStarted = true;
				break;
			}
			DateTime dateTime2 = dateTime.Date.AddHours(18.0).AddMinutes(1.0);
			if (dateTime >= dateTime2 && sessionDate < dateTime.Date)
			{
				flag = true;
				sessionDate = dateTime.Date;
			}
			break;
		}
		case PulseCumulativeDeltaResetPeriod.Daily:
			if (dateTime.Date != sessionDate)
			{
				flag = true;
				sessionDate = dateTime.Date;
			}
			break;
		case PulseCumulativeDeltaResetPeriod.Never:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				sessionStarted = true;
			}
			break;
		}
		if (flag)
		{
			sessionStartDelta = cumulativeDelta;
			lastProcessedBar = -1;
			currentBarDelta = 0L;
			sessionStarted = true;
		}
	}

	private void CalculateRealDelta(bool forceCurrentBar)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).BarsArray.Length >= 2)
		{
			int num = ((NinjaScriptBase)this).BarsArray[1].Count - 1 - ((NinjaScriptBase)this).CurrentBars[1];
			bool flag = (int)((NinjaScript)this).State == 7 && num > 1;
			if (!flag && lastInTransition && !forceCurrentBar && (int)((NinjaScriptBase)this).Calculate == 0)
			{
				CalculateRealDelta(forceCurrentBar: true);
			}
			int num2 = (((int)((NinjaScript)this).State == 5 || flag || (int)((NinjaScriptBase)this).Calculate > 0 || forceCurrentBar) ? ((NinjaScriptBase)this).CurrentBars[1] : Math.Min(((NinjaScriptBase)this).CurrentBars[1] + 1, ((NinjaScriptBase)this).BarsArray[1].Count - 1));
			double num3 = ((NinjaScriptBase)this).BarsArray[1].GetVolume(num2);
			double close = ((NinjaScriptBase)this).BarsArray[1].GetClose(num2);
			if (close >= ((NinjaScriptBase)this).BarsArray[1].GetAsk(num2) && num3 > 0.0)
			{
				buys += num3;
			}
			else if (close <= ((NinjaScriptBase)this).BarsArray[1].GetBid(num2) && num3 > 0.0)
			{
				sells += num3;
			}
			cdClose = buys - sells;
			if (cdClose > cdHigh)
			{
				cdHigh = cdClose;
			}
			if (cdClose < cdLow)
			{
				cdLow = cdClose;
			}
			lastInTransition = flag;
		}
	}

	private void SetValues(int barsAgo)
	{
		if (barsAgo == 0 && ((NinjaScriptBase)this).CurrentBar > 0)
		{
			previousCdClose = ((NinjaScriptBase)this).Values[3][1];
		}
		((NinjaScriptBase)this).Values[0][barsAgo] = cdOpen;
		((NinjaScriptBase)this).Values[1][barsAgo] = cdHigh;
		((NinjaScriptBase)this).Values[2][barsAgo] = cdLow;
		((NinjaScriptBase)this).Values[3][barsAgo] = cdClose;
	}

	private void ResetValues(bool isNewSession, double openLevel)
	{
		cdOpen = (cdClose = (cdHigh = (cdLow = openLevel)));
		if (isNewSession)
		{
			cdOpen = (cdClose = (cdHigh = (cdLow = (buys = (sells = 0.0)))));
			previousCdClose = 0.0;
		}
	}

	private void UpdatePlotColors()
	{
		if (!colorBasedOnDirection || ((NinjaScriptBase)this).CurrentBar < 0)
		{
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar > 0)
		{
			double num = ((NinjaScriptBase)this).Values[3][0];
			double num2 = ((NinjaScriptBase)this).Values[3][1];
			if (num > num2)
			{
				((NinjaScriptBase)this).PlotBrushes[3][0] = positiveBrush;
			}
			else if (num < num2)
			{
				((NinjaScriptBase)this).PlotBrushes[3][0] = negativeBrush;
			}
			else
			{
				((NinjaScriptBase)this).PlotBrushes[3][0] = (Brush)(object)Brushes.Gray;
			}
		}
		else
		{
			((NinjaScriptBase)this).PlotBrushes[3][0] = (Brush)(object)Brushes.Gray;
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (((IndicatorRenderBase)this).RenderTarget != null && chartControl != null && chartScale != null && ((IndicatorRenderBase)this).ChartBars != null)
		{
			EnsureDxResources();
			if (showZeroLine)
			{
				DrawZeroLine(chartControl, chartScale);
			}
			DrawCustomOHLCBars(chartControl, chartScale);
		}
	}

	private void DrawCustomOHLCBars(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		if (positiveBrushDx == null || negativeBrushDx == null)
		{
			return;
		}
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		if (toIndex < fromIndex)
		{
			return;
		}
		int num = Math.Max(3, 1 + 2 * ((int)((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.BarWidth - 1) + 2);
		float num2 = (float)num * 0.5f;
		int count = ((NinjaScriptBase)this).BarsArray[0].Count;
		Series<double> val = ((NinjaScriptBase)this).Values[0];
		Series<double> val2 = ((NinjaScriptBase)this).Values[1];
		Series<double> val3 = ((NinjaScriptBase)this).Values[2];
		Series<double> val4 = ((NinjaScriptBase)this).Values[3];
		RectangleF val6 = default(RectangleF);
		for (int i = fromIndex; i <= toIndex; i++)
		{
			int num3 = i - ((NinjaScriptBase)this).Displacement;
			if (num3 < ((NinjaScriptBase)this).BarsRequiredToPlot || num3 < 0 || num3 >= count)
			{
				continue;
			}
			double valueAt = val.GetValueAt(i);
			double valueAt2 = val2.GetValueAt(i);
			double valueAt3 = val3.GetValueAt(i);
			double valueAt4 = val4.GetValueAt(i);
			if (double.IsNaN(valueAt) || double.IsNaN(valueAt2) || double.IsNaN(valueAt3) || double.IsNaN(valueAt4))
			{
				continue;
			}
			float num4 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i);
			float num5 = chartScale.GetYByValue(valueAt);
			float num6 = chartScale.GetYByValue(valueAt2);
			float num7 = chartScale.GetYByValue(valueAt3);
			float num8 = chartScale.GetYByValue(valueAt4);
			if (!float.IsNaN(num5) && !float.IsNaN(num6) && !float.IsNaN(num7) && !float.IsNaN(num8) && !float.IsInfinity(num5) && !float.IsInfinity(num6) && !float.IsInfinity(num7) && !float.IsInfinity(num8))
			{
				bool flag = true;
				if (i > 0)
				{
					double valueAt5 = val4.GetValueAt(i - 1);
					flag = valueAt4 > valueAt5;
				}
				SolidColorBrush val5 = (flag ? positiveBrushDx : negativeBrushDx);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num4, num6), new Vector2(num4, num7), (Brush)(object)val5, 1f);
				float num9 = Math.Min(num5, num8);
				float num10 = Math.Abs(num8 - num5);
				if (num10 < 0.5f)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num4 - num2, num5), new Vector2(num4 + num2, num5), (Brush)(object)val5, 1f);
					continue;
				}
				((RectangleF)(ref val6))._002Ector(num4 - num2, num9, (float)num, Math.Max(1f, num10));
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val6, (Brush)(object)val5);
				((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val6, (Brush)(object)val5, 1f);
			}
		}
	}

	private void DrawZeroLine(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (zeroLineBrushDx != null && zeroLineDashStrokeStyle != null)
		{
			float num = chartScale.GetYByValue(0.0);
			if (!float.IsNaN(num) && !float.IsInfinity(num))
			{
				float num2 = ((IndicatorRenderBase)this).ChartPanel.X;
				float num3 = ((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W;
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num2, num), new Vector2(num3, num), (Brush)(object)zeroLineBrushDx, 1f, zeroLineDashStrokeStyle);
			}
		}
	}

	private void EnsureDxResources()
	{
		EnsureColorBrushes();
		EnsureZeroLineBrush();
		EnsureZeroLineStrokeStyle();
	}

	private void EnsureZeroLineBrush()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget == null || zeroLineBrush == null)
		{
			return;
		}
		Color brushColor = GetBrushColor(zeroLineBrush, Colors.Gray);
		Color4 val = default(Color4);
		((Color4)(ref val))._002Ector((float)(int)((Color)(ref brushColor)).R / 255f, (float)(int)((Color)(ref brushColor)).G / 255f, (float)(int)((Color)(ref brushColor)).B / 255f, 0.7f);
		if (zeroLineBrushDx == null || !Color4Equals(zeroLineBrushDx.Color, val))
		{
			SolidColorBrush obj = zeroLineBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			zeroLineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val);
		}
	}

	private void EnsureColorBrushes()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget != null)
		{
			EnsureDxBrush(ref positiveBrushDx, positiveBrush, Colors.DimGray);
			EnsureDxBrush(ref negativeBrushDx, negativeBrush, Colors.DarkGray);
		}
	}

	private void EnsureDxBrush(ref SolidColorBrush target, Brush source, Color fallback)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		if (source == null || ((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		Color brushColor = GetBrushColor(source, fallback);
		Color4 val = default(Color4);
		((Color4)(ref val))._002Ector((float)(int)((Color)(ref brushColor)).R / 255f, (float)(int)((Color)(ref brushColor)).G / 255f, (float)(int)((Color)(ref brushColor)).B / 255f, 1f);
		if (target == null || !Color4Equals(target.Color, val))
		{
			SolidColorBrush obj = target;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			target = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val);
		}
	}

	private void EnsureZeroLineStrokeStyle()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		if (zeroLineDashStrokeStyle == null)
		{
			zeroLineDashStrokeStyle = new StrokeStyle(Globals.D2DFactory, new StrokeStyleProperties
			{
				DashStyle = (DashStyle)1
			});
		}
	}

	private static Color GetBrushColor(Brush brush, Color fallback)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallback;
		}
		return val.Color;
	}

	private static bool Color4Equals(Color4 a, Color4 b)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (Math.Abs(a.Red - b.Red) < 0.0001f && Math.Abs(a.Green - b.Green) < 0.0001f && Math.Abs(a.Blue - b.Blue) < 0.0001f)
		{
			return Math.Abs(a.Alpha - b.Alpha) < 0.0001f;
		}
		return false;
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		SolidColorBrush obj = positiveBrushDx;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		positiveBrushDx = null;
		SolidColorBrush obj2 = negativeBrushDx;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		negativeBrushDx = null;
		SolidColorBrush obj3 = zeroLineBrushDx;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		zeroLineBrushDx = null;
		StrokeStyle obj4 = zeroLineDashStrokeStyle;
		if (obj4 != null)
		{
			((DisposeBase)obj4).Dispose();
		}
		zeroLineDashStrokeStyle = null;
	}
}
