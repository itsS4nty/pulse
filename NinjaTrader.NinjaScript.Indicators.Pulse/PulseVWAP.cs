using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
public class PulseVWAP : Indicator
{
	private double cumulativeVolume;

	private double cumulativeVolumePrice;

	private double cumulativeVolumeSquared;

	private DateTime sessionDate = DateTime.MinValue;

	private DateTime lastResetTime = DateTime.MinValue;

	private bool sessionStarted;

	private int lastProcessedBar = -1;

	private int sessionStartBarIndex = -1;

	private int noPlotTransitionBar = -1;

	private double currentBarVolume;

	private double currentBarVolumePrice;

	private double currentBarVolumeSquared;

	private double vwapValue;

	private double variance;

	private double standardDeviation;

	private Series<int> sessionIdSeries;

	private int currentSessionId;

	private PulseVWAPResetPeriod resetPeriod;

	private bool showStandardDeviations = true;

	private double sd1Multiplier = 1.0;

	private double sd2Multiplier = 2.0;

	private bool showPreviousVWAP = true;

	private int levelTextSize = 14;

	private double previousVWAP = double.NaN;

	private double previousUpperBand1 = double.NaN;

	private double previousLowerBand1 = double.NaN;

	private double previousUpperBand2 = double.NaN;

	private double previousLowerBand2 = double.NaN;

	private TextFormat textFormat;

	private Brush previousVWAPBrush;

	private SolidColorBrush previousVWAPBrushDx;

	private StrokeStyle previousVWAPDashStrokeStyle;

	private SolidColorBrush labelBrushDx;

	private Color cachedLabelMediaColor = Colors.Transparent;

	[NinjaScriptProperty]
	[Display(Name = "Reset Period", Description = "When to reset VWAP calculation", Order = 1, GroupName = "Parameters")]
	public PulseVWAPResetPeriod ResetPeriod
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
	[Display(Name = "Show Standard Deviations", Description = "Show standard deviation bands", Order = 2, GroupName = "Parameters")]
	public bool ShowStandardDeviations
	{
		get
		{
			return showStandardDeviations;
		}
		set
		{
			showStandardDeviations = value;
		}
	}

	[NinjaScriptProperty]
	[Range(0.1, 5.0)]
	[Display(Name = "SD1 Multiplier", Description = "First standard deviation multiplier", Order = 3, GroupName = "Parameters")]
	public double SD1Multiplier
	{
		get
		{
			return sd1Multiplier;
		}
		set
		{
			sd1Multiplier = Math.Max(0.1, value);
		}
	}

	[NinjaScriptProperty]
	[Range(0.1, 5.0)]
	[Display(Name = "SD2 Multiplier", Description = "Second standard deviation multiplier", Order = 4, GroupName = "Parameters")]
	public double SD2Multiplier
	{
		get
		{
			return sd2Multiplier;
		}
		set
		{
			sd2Multiplier = Math.Max(0.1, value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Previous VWAP", Description = "Show previous session VWAP as dashed line", Order = 6, GroupName = "Parameters")]
	public bool ShowPreviousVWAP
	{
		get
		{
			return showPreviousVWAP;
		}
		set
		{
			showPreviousVWAP = value;
		}
	}

	[NinjaScriptProperty]
	[Range(8, 40)]
	[Display(Name = "Label Text Size", Description = "Font size for VWAP labels", Order = 1, GroupName = "Visual")]
	public int LevelTextSize
	{
		get
		{
			return levelTextSize;
		}
		set
		{
			levelTextSize = Math.Max(8, Math.Min(40, value));
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = "Previous VWAP Color", Description = "Color for previous session VWAP", Order = 4, GroupName = "Visual")]
	public Brush PreviousVWAPBrush
	{
		get
		{
			return previousVWAPBrush;
		}
		set
		{
			previousVWAPBrush = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string PreviousVWAPBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(previousVWAPBrush);
		}
		set
		{
			previousVWAPBrush = Serialize.StringToBrush(value);
			DisposeDx();
		}
	}

	public PulseVWAP()
	{
	}

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			Description = "Pulse VWAP indicator with standard deviation bands and session reset functionality";
			Name = "PulseVWAP";
			Calculate = Calculate.OnEachTick;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = true;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			resetPeriod = PulseVWAPResetPeriod.Session;
			showStandardDeviations = true;
			sd1Multiplier = 1.0;
			sd2Multiplier = 2.0;
			showPreviousVWAP = true;
			levelTextSize = 14;
			previousVWAPBrush = (Brush)(object)Brushes.Gray;
			AddPlot(new Stroke((Brush)(object)Brushes.Orange, 2f), (PlotStyle)6, "VWAP");
			AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "UpperBand1");
			AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "LowerBand1");
			AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "UpperBand2");
			AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "LowerBand2");
		}
		else if (State == State.Configure)
		{
			sessionIdSeries = new Series<int>(this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (CurrentBar >= 1)
		{
			CheckSessionReset();
			CalculateVWAP();
			if (CurrentBar <= noPlotTransitionBar + 1)
			{
				sessionIdSeries[0] = currentSessionId;
				ResetAllPlotsAt(0);
			}
			else
			{
				UpdatePlots();
			}
		}
	}

	private void CheckSessionReset()
	{
		DateTime dateTime = Times[0][0];
		bool flag = false;
		switch (resetPeriod)
		{
		case PulseVWAPResetPeriod.Session:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				lastResetTime = dateTime;
				sessionStarted = true;
				sessionStartBarIndex = CurrentBar;
			}
			else if (Bars != null && Bars.IsFirstBarOfSession && lastResetTime != dateTime)
			{
				flag = true;
				sessionDate = dateTime.Date;
				lastResetTime = dateTime;
			}
			break;
		case PulseVWAPResetPeriod.Daily:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				sessionStarted = true;
			}
			else if (dateTime.Date != sessionDate)
			{
				flag = true;
				sessionDate = dateTime.Date;
			}
			break;
		case PulseVWAPResetPeriod.Weekly:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				sessionStarted = true;
			}
			else if (dateTime.DayOfWeek == DayOfWeek.Monday && dateTime.Date != sessionDate)
			{
				flag = true;
				sessionDate = dateTime.Date;
			}
			break;
		case PulseVWAPResetPeriod.Monthly:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				sessionStarted = true;
			}
			else if (dateTime.Day == 1 && dateTime.Date != sessionDate)
			{
				flag = true;
				sessionDate = dateTime.Date;
			}
			break;
		}
		if (!flag)
		{
			return;
		}
		if (vwapValue > 0.0)
		{
			previousVWAP = vwapValue;
			previousUpperBand1 = vwapValue + standardDeviation * sd1Multiplier;
			previousLowerBand1 = vwapValue - standardDeviation * sd1Multiplier;
			previousUpperBand2 = vwapValue + standardDeviation * sd2Multiplier;
			previousLowerBand2 = vwapValue - standardDeviation * sd2Multiplier;
		}
		if (CurrentBar > 0)
		{
			ResetAllPlotsAt(1);
			if (CurrentBar > 1)
			{
				ResetAllPlotsAt(2);
			}
		}
		noPlotTransitionBar = CurrentBar;
		sessionStartBarIndex = CurrentBar;
		currentSessionId++;
		cumulativeVolume = 0.0;
		cumulativeVolumePrice = 0.0;
		cumulativeVolumeSquared = 0.0;
		currentBarVolume = 0.0;
		currentBarVolumePrice = 0.0;
		currentBarVolumeSquared = 0.0;
		lastProcessedBar = -1;
		vwapValue = 0.0;
		variance = 0.0;
		standardDeviation = 0.0;
		sessionStarted = true;
	}

	private void ResetAllPlotsAt(int barsAgo)
	{
		if (barsAgo >= 0 && CurrentBar >= barsAgo)
		{
			Values[0].Reset(barsAgo);
			Values[1].Reset(barsAgo);
			Values[2].Reset(barsAgo);
			Values[3].Reset(barsAgo);
			Values[4].Reset(barsAgo);
		}
	}

	private void CalculateVWAP()
	{
		double num = (High[0] + Low[0] + Close[0]) / 3.0;
		double num2 = Volume[0];
		if (CurrentBar != lastProcessedBar)
		{
			cumulativeVolume += num2;
			cumulativeVolumePrice += num * num2;
			cumulativeVolumeSquared += num * num * num2;
			currentBarVolume = num2;
			currentBarVolumePrice = num * num2;
			currentBarVolumeSquared = num * num * num2;
			lastProcessedBar = CurrentBar;
		}
		else if (State == State.Realtime && lastProcessedBar == CurrentBar)
		{
			cumulativeVolume -= currentBarVolume;
			cumulativeVolumePrice -= currentBarVolumePrice;
			cumulativeVolumeSquared -= currentBarVolumeSquared;
			currentBarVolume = num2;
			currentBarVolumePrice = num * num2;
			currentBarVolumeSquared = num * num * num2;
			cumulativeVolume += currentBarVolume;
			cumulativeVolumePrice += currentBarVolumePrice;
			cumulativeVolumeSquared += currentBarVolumeSquared;
		}
		if (cumulativeVolume > 0.0)
		{
			vwapValue = cumulativeVolumePrice / cumulativeVolume;
			double num3 = cumulativeVolumeSquared / cumulativeVolume;
			variance = num3 - vwapValue * vwapValue;
			standardDeviation = ((variance > 0.0) ? Math.Sqrt(variance) : 0.0);
		}
	}

	private void UpdatePlots()
	{
		sessionIdSeries[0] = currentSessionId;
		Values[0][0] = vwapValue;
		if (showStandardDeviations && standardDeviation > 0.0)
		{
			Values[1][0] = vwapValue + standardDeviation * sd1Multiplier;
			Values[2][0] = vwapValue - standardDeviation * sd1Multiplier;
			Values[3][0] = vwapValue + standardDeviation * sd2Multiplier;
			Values[4][0] = vwapValue - standardDeviation * sd2Multiplier;
		}
		else
		{
			Values[1][0] = double.NaN;
			Values[2][0] = double.NaN;
			Values[3][0] = double.NaN;
			Values[4][0] = double.NaN;
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
		if (showPreviousVWAP && !double.IsNaN(previousVWAP))
		{
			RenderPreviousVWAP(chartControl, chartScale);
		}
		RenderVWAPLabels(chartControl, chartScale);
	}

	private void RenderPreviousVWAP(ChartControl chartControl, ChartScale chartScale)
	{
		if (RenderTarget == null)
		{
			return;
		}
		EnsurePreviousVWAPBrush();
		if (previousVWAPBrushDx != null)
		{
			float num = ChartPanel.X;
			float num2 = ChartPanel.X + ChartPanel.W;
			float num3 = chartScale.GetYByValue(previousVWAP);
			Vector2 val = default(Vector2);
			val = new Vector2(num, num3);
			Vector2 val2 = default(Vector2);
			val2 = new Vector2(num2, num3);
			EnsurePreviousVWAPStrokeStyle();
			if (previousVWAPDashStrokeStyle != null)
			{
				RenderTarget.DrawLine(val, val2, (Brush)(object)previousVWAPBrushDx, 1.5f, previousVWAPDashStrokeStyle);
			}
			else
			{
				RenderTarget.DrawLine(val, val2, (Brush)(object)previousVWAPBrushDx, 1.5f);
			}
		}
	}

	private void RenderVWAPLabels(ChartControl chartControl, ChartScale chartScale)
	{
		if (RenderTarget == null || double.IsNaN(vwapValue))
		{
			return;
		}
		EnsureTextFormat();
		EnsureLabelBrush(chartControl);
		if (labelBrushDx != null)
		{
			int currentBar = CurrentBar;
			float rightX = (float)chartControl.GetXByBarIndex(ChartBars, currentBar) + 70f;
			DrawRightLabel("VWAP", vwapValue, labelBrushDx, rightX, chartScale);
			if (showStandardDeviations && standardDeviation > 0.0)
			{
				double price = vwapValue + standardDeviation * sd1Multiplier;
				double price2 = vwapValue - standardDeviation * sd1Multiplier;
				DrawRightLabel("SD1+", price, labelBrushDx, rightX, chartScale);
				DrawRightLabel("SD1-", price2, labelBrushDx, rightX, chartScale);
				double price3 = vwapValue + standardDeviation * sd2Multiplier;
				double price4 = vwapValue - standardDeviation * sd2Multiplier;
				DrawRightLabel("SD2+", price3, labelBrushDx, rightX, chartScale);
				DrawRightLabel("SD2-", price4, labelBrushDx, rightX, chartScale);
			}
		}
	}

	private void DrawRightLabel(string text, double price, SolidColorBrush brush, float rightX, ChartScale chartScale)
	{
		if (!double.IsNaN(price) && !(price <= 0.0) && textFormat != null)
		{
			float num = chartScale.GetYByValue(price);
			RectangleF val = default(RectangleF);
			val = new RectangleF(rightX - 60f, num - 10f, 80f, 20f);
			RenderTarget.DrawText(text, textFormat, val, (Brush)(object)brush);
		}
	}

	private void EnsureTextFormat()
	{
		if (textFormat == null && RenderTarget != null)
		{
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", Math.Max(8f, levelTextSize))
			{
				TextAlignment = (TextAlignment)0,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
	}

	private void EnsurePreviousVWAPBrush()
	{
		if (previousVWAPBrushDx == null && previousVWAPBrush != null && RenderTarget != null)
		{
			Color color = ((SolidColorBrush)previousVWAPBrush).Color;
			previousVWAPBrushDx = new SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, 0.7f));
		}
	}

	private void EnsurePreviousVWAPStrokeStyle()
	{
		if (previousVWAPDashStrokeStyle == null)
		{
			previousVWAPDashStrokeStyle = new StrokeStyle(Globals.D2DFactory, new StrokeStyleProperties
			{
				DashStyle = (DashStyle)1
			});
		}
	}

	private void EnsureLabelBrush(ChartControl chartControl)
	{
		if (RenderTarget == null || chartControl == null || chartControl.Properties == null)
		{
			return;
		}
		Brush chartText = chartControl.Properties.ChartText;
		SolidColorBrush val = (SolidColorBrush)(object)((chartText is SolidColorBrush) ? chartText : null);
		Color val2 = ((val != null) ? val.Color : Colors.White);
		if (labelBrushDx == null || !(val2 == cachedLabelMediaColor))
		{
			SolidColorBrush obj = labelBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			labelBrushDx = new SolidColorBrush(RenderTarget, new Color4((float)(int)val2.R / 255f, (float)(int)val2.G / 255f, (float)(int)val2.B / 255f, 0.8f));
			cachedLabelMediaColor = val2;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		SolidColorBrush obj = previousVWAPBrushDx;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		previousVWAPBrushDx = null;
		StrokeStyle obj2 = previousVWAPDashStrokeStyle;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		previousVWAPDashStrokeStyle = null;
		SolidColorBrush obj3 = labelBrushDx;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		labelBrushDx = null;
		cachedLabelMediaColor = Colors.Transparent;
		TextFormat obj4 = textFormat;
		if (obj4 != null)
		{
			((DisposeBase)obj4).Dispose();
		}
		textFormat = null;
	}
}
}
