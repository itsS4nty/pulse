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

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
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
		if (State == State.SetDefaults)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			Description = "Pulse Cumulative Delta - Real bid/ask delta calculation using tick data";
			Name = "PulseCumulativeDelta";
			Calculate = Calculate.OnEachTick;
			IsOverlay = false;
			DisplayInDataBox = true;
			DrawOnPricePanel = false;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = true;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			resetPeriod = PulseCumulativeDeltaResetPeriod.Session;
			showZeroLine = true;
			colorBasedOnDirection = true;
			positiveBrush = (Brush)(object)Brushes.DimGray;
			negativeBrush = (Brush)(object)Brushes.DarkGray;
			zeroLineBrush = (Brush)(object)Brushes.Gray;
			AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaOpen");
			AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaHigh");
			AddPlot(new Stroke((Brush)(object)Brushes.Transparent), (PlotStyle)12, "DeltaLow");
			AddPlot(new Stroke((Brush)(object)Brushes.LightGray), (PlotStyle)12, "DeltaClose");
		}
		else if (State == State.Configure)
		{
			AddDataSeries((BarsPeriodType)0, 1);
		}
		else
		{
			_ = State;
			_ = 4;
		}
	}

	protected override void OnBarUpdate()
	{
		if (CurrentBars[0] < 5 || (BarsArray.Length > 1 && CurrentBars[1] < 5))
		{
			return;
		}
		if (BarsInProgress == 0)
		{
			int num = ((BarsArray.Length > 1) ? (BarsArray[1].Count - 1 - CurrentBars[1]) : 0);
			if (IsFirstTickOfBar && Calculate != Calculate.OnBarClose && (State == State.Realtime || BarsArray[0].IsTickReplay))
			{
				if (CurrentBars[0] > 0)
				{
					SetValues(1);
				}
				if (BarsArray[0].IsTickReplay || (State == State.Realtime && num == 0))
				{
					ResetValues(isNewSession: false, cdClose);
				}
			}
			SetValues(0);
			if (Calculate == Calculate.OnBarClose || (lastProcessedBar != CurrentBars[0] && (State == State.Historical || (State == State.Realtime && num > 0))))
			{
				ResetValues(isNewSession: false, cdClose);
			}
			lastProcessedBar = CurrentBars[0];
			UpdatePlotColors();
		}
		else if (BarsInProgress == 1 && BarsArray.Length > 1)
		{
			if (BarsArray[1].IsFirstBarOfSession)
			{
				ResetValues(isNewSession: true, cdClose);
			}
			CalculateRealDelta(forceCurrentBar: false);
		}
	}

	private void CheckSessionReset()
	{
		DateTime dateTime = Times[0][0];
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
		if (BarsArray.Length >= 2)
		{
			int num = BarsArray[1].Count - 1 - CurrentBars[1];
			bool flag = State == State.Realtime && num > 1;
			if (!flag && lastInTransition && !forceCurrentBar && Calculate == Calculate.OnBarClose)
			{
				CalculateRealDelta(forceCurrentBar: true);
			}
			int num2 = ((State == State.Historical || flag || (int)Calculate > 0 || forceCurrentBar) ? CurrentBars[1] : Math.Min(CurrentBars[1] + 1, BarsArray[1].Count - 1));
			double num3 = BarsArray[1].GetVolume(num2);
			double close = BarsArray[1].GetClose(num2);
			if (close >= BarsArray[1].GetAsk(num2) && num3 > 0.0)
			{
				buys += num3;
			}
			else if (close <= BarsArray[1].GetBid(num2) && num3 > 0.0)
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
		if (barsAgo == 0 && CurrentBar > 0)
		{
			previousCdClose = Values[3][1];
		}
		Values[0][barsAgo] = cdOpen;
		Values[1][barsAgo] = cdHigh;
		Values[2][barsAgo] = cdLow;
		Values[3][barsAgo] = cdClose;
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
		if (!colorBasedOnDirection || CurrentBar < 0)
		{
			return;
		}
		if (CurrentBar > 0)
		{
			double num = Values[3][0];
			double num2 = Values[3][1];
			if (num > num2)
			{
				PlotBrushes[3][0] = positiveBrush;
			}
			else if (num < num2)
			{
				PlotBrushes[3][0] = negativeBrush;
			}
			else
			{
				PlotBrushes[3][0] = (Brush)(object)Brushes.Gray;
			}
		}
		else
		{
			PlotBrushes[3][0] = (Brush)(object)Brushes.Gray;
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
		if (RenderTarget != null && chartControl != null && chartScale != null && ChartBars != null)
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
		if (positiveBrushDx == null || negativeBrushDx == null)
		{
			return;
		}
		int fromIndex = ChartBars.FromIndex;
		int toIndex = ChartBars.ToIndex;
		if (toIndex < fromIndex)
		{
			return;
		}
		int num = Math.Max(3, 1 + 2 * ((int)ChartBars.Properties.ChartStyle.BarWidth - 1) + 2);
		float num2 = (float)num * 0.5f;
		int count = BarsArray[0].Count;
		Series<double> val = Values[0];
		Series<double> val2 = Values[1];
		Series<double> val3 = Values[2];
		Series<double> val4 = Values[3];
		RectangleF val6 = default(RectangleF);
		for (int i = fromIndex; i <= toIndex; i++)
		{
			int num3 = i - Displacement;
			if (num3 < BarsRequiredToPlot || num3 < 0 || num3 >= count)
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
			float num4 = chartControl.GetXByBarIndex(ChartBars, i);
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
				RenderTarget.DrawLine(new Vector2(num4, num6), new Vector2(num4, num7), (Brush)(object)val5, 1f);
				float num9 = Math.Min(num5, num8);
				float num10 = Math.Abs(num8 - num5);
				if (num10 < 0.5f)
				{
					RenderTarget.DrawLine(new Vector2(num4 - num2, num5), new Vector2(num4 + num2, num5), (Brush)(object)val5, 1f);
					continue;
				}
				val6 = new RectangleF(num4 - num2, num9, (float)num, Math.Max(1f, num10));
				RenderTarget.FillRectangle(val6, (Brush)(object)val5);
				RenderTarget.DrawRectangle(val6, (Brush)(object)val5, 1f);
			}
		}
	}

	private void DrawZeroLine(ChartControl chartControl, ChartScale chartScale)
	{
		if (zeroLineBrushDx != null && zeroLineDashStrokeStyle != null)
		{
			float num = chartScale.GetYByValue(0.0);
			if (!float.IsNaN(num) && !float.IsInfinity(num))
			{
				float num2 = ChartPanel.X;
				float num3 = ChartPanel.X + ChartPanel.W;
				RenderTarget.DrawLine(new Vector2(num2, num), new Vector2(num3, num), (Brush)(object)zeroLineBrushDx, 1f, zeroLineDashStrokeStyle);
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
		if (RenderTarget == null || zeroLineBrush == null)
		{
			return;
		}
		Color brushColor = GetBrushColor(zeroLineBrush, Colors.Gray);
		Color4 val = default(Color4);
		val = new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, 0.7f);
		if (zeroLineBrushDx == null || !Color4Equals(zeroLineBrushDx.Color, val))
		{
			SolidColorBrush obj = zeroLineBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			zeroLineBrushDx = new SolidColorBrush(RenderTarget, val);
		}
	}

	private void EnsureColorBrushes()
	{
		if (RenderTarget != null)
		{
			EnsureDxBrush(ref positiveBrushDx, positiveBrush, Colors.DimGray);
			EnsureDxBrush(ref negativeBrushDx, negativeBrush, Colors.DarkGray);
		}
	}

	private void EnsureDxBrush(ref SolidColorBrush target, Brush source, Color fallback)
	{
		if (source == null || RenderTarget == null)
		{
			return;
		}
		Color brushColor = GetBrushColor(source, fallback);
		Color4 val = default(Color4);
		val = new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, 1f);
		if (target == null || !Color4Equals(target.Color, val))
		{
			SolidColorBrush obj = target;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			target = new SolidColorBrush(RenderTarget, val);
		}
	}

	private void EnsureZeroLineStrokeStyle()
	{
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
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallback;
		}
		return val.Color;
	}

	private static bool Color4Equals(Color4 a, Color4 b)
	{
		if (Math.Abs(a.Red - b.Red) < 0.0001f && Math.Abs(a.Green - b.Green) < 0.0001f && Math.Abs(a.Blue - b.Blue) < 0.0001f)
		{
			return Math.Abs(a.Alpha - b.Alpha) < 0.0001f;
		}
		return false;
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		OnRenderTargetChanged();
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
}
