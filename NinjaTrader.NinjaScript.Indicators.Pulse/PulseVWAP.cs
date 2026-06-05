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

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

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
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			((NinjaScript)this).Description = "Pulse VWAP indicator with standard deviation bands and session reset functionality";
			((NinjaScriptBase)this).Name = "PulseVWAP";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			resetPeriod = PulseVWAPResetPeriod.Session;
			showStandardDeviations = true;
			sd1Multiplier = 1.0;
			sd2Multiplier = 2.0;
			showPreviousVWAP = true;
			levelTextSize = 14;
			previousVWAPBrush = (Brush)(object)Brushes.Gray;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.Orange, 2f), (PlotStyle)6, "VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "UpperBand1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "LowerBand1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "UpperBand2");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)(object)Brushes.DarkSlateBlue, 1f), (PlotStyle)6, "LowerBand2");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			sessionIdSeries = new Series<int>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= 1)
		{
			CheckSessionReset();
			CalculateVWAP();
			if (((NinjaScriptBase)this).CurrentBar <= noPlotTransitionBar + 1)
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
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
		bool flag = false;
		switch (resetPeriod)
		{
		case PulseVWAPResetPeriod.Session:
			if (sessionDate == DateTime.MinValue)
			{
				sessionDate = dateTime.Date;
				lastResetTime = dateTime;
				sessionStarted = true;
				sessionStartBarIndex = ((NinjaScriptBase)this).CurrentBar;
			}
			else if (((NinjaScriptBase)this).Bars != null && ((NinjaScriptBase)this).Bars.IsFirstBarOfSession && lastResetTime != dateTime)
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
		if (((NinjaScriptBase)this).CurrentBar > 0)
		{
			ResetAllPlotsAt(1);
			if (((NinjaScriptBase)this).CurrentBar > 1)
			{
				ResetAllPlotsAt(2);
			}
		}
		noPlotTransitionBar = ((NinjaScriptBase)this).CurrentBar;
		sessionStartBarIndex = ((NinjaScriptBase)this).CurrentBar;
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
		if (barsAgo >= 0 && ((NinjaScriptBase)this).CurrentBar >= barsAgo)
		{
			((NinjaScriptBase)this).Values[0].Reset(barsAgo);
			((NinjaScriptBase)this).Values[1].Reset(barsAgo);
			((NinjaScriptBase)this).Values[2].Reset(barsAgo);
			((NinjaScriptBase)this).Values[3].Reset(barsAgo);
			((NinjaScriptBase)this).Values[4].Reset(barsAgo);
		}
	}

	private void CalculateVWAP()
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		double num = (((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).Low[0] + ((NinjaScriptBase)this).Close[0]) / 3.0;
		double num2 = ((NinjaScriptBase)this).Volume[0];
		if (((NinjaScriptBase)this).CurrentBar != lastProcessedBar)
		{
			cumulativeVolume += num2;
			cumulativeVolumePrice += num * num2;
			cumulativeVolumeSquared += num * num * num2;
			currentBarVolume = num2;
			currentBarVolumePrice = num * num2;
			currentBarVolumeSquared = num * num * num2;
			lastProcessedBar = ((NinjaScriptBase)this).CurrentBar;
		}
		else if ((int)((NinjaScript)this).State == 7 && lastProcessedBar == ((NinjaScriptBase)this).CurrentBar)
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
		((NinjaScriptBase)this).Values[0][0] = vwapValue;
		if (showStandardDeviations && standardDeviation > 0.0)
		{
			((NinjaScriptBase)this).Values[1][0] = vwapValue + standardDeviation * sd1Multiplier;
			((NinjaScriptBase)this).Values[2][0] = vwapValue - standardDeviation * sd1Multiplier;
			((NinjaScriptBase)this).Values[3][0] = vwapValue + standardDeviation * sd2Multiplier;
			((NinjaScriptBase)this).Values[4][0] = vwapValue - standardDeviation * sd2Multiplier;
		}
		else
		{
			((NinjaScriptBase)this).Values[1][0] = double.NaN;
			((NinjaScriptBase)this).Values[2][0] = double.NaN;
			((NinjaScriptBase)this).Values[3][0] = double.NaN;
			((NinjaScriptBase)this).Values[4][0] = double.NaN;
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (showPreviousVWAP && !double.IsNaN(previousVWAP))
		{
			RenderPreviousVWAP(chartControl, chartScale);
		}
		RenderVWAPLabels(chartControl, chartScale);
	}

	private void RenderPreviousVWAP(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		EnsurePreviousVWAPBrush();
		if (previousVWAPBrushDx != null)
		{
			float num = ((IndicatorRenderBase)this).ChartPanel.X;
			float num2 = ((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W;
			float num3 = chartScale.GetYByValue(previousVWAP);
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(num, num3);
			Vector2 val2 = default(Vector2);
			((Vector2)(ref val2))._002Ector(num2, num3);
			EnsurePreviousVWAPStrokeStyle();
			if (previousVWAPDashStrokeStyle != null)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(val, val2, (Brush)(object)previousVWAPBrushDx, 1.5f, previousVWAPDashStrokeStyle);
			}
			else
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(val, val2, (Brush)(object)previousVWAPBrushDx, 1.5f);
			}
		}
	}

	private void RenderVWAPLabels(ChartControl chartControl, ChartScale chartScale)
	{
		if (((IndicatorRenderBase)this).RenderTarget == null || double.IsNaN(vwapValue))
		{
			return;
		}
		EnsureTextFormat();
		EnsureLabelBrush(chartControl);
		if (labelBrushDx != null)
		{
			int currentBar = ((NinjaScriptBase)this).CurrentBar;
			float rightX = (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, currentBar) + 70f;
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
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (!double.IsNaN(price) && !(price <= 0.0) && textFormat != null)
		{
			float num = chartScale.GetYByValue(price);
			RectangleF val = default(RectangleF);
			((RectangleF)(ref val))._002Ector(rightX - 60f, num - 10f, 80f, 20f);
			((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, val, (Brush)(object)brush);
		}
	}

	private void EnsureTextFormat()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		if (textFormat == null && ((IndicatorRenderBase)this).RenderTarget != null)
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
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		if (previousVWAPBrushDx == null && previousVWAPBrush != null && ((IndicatorRenderBase)this).RenderTarget != null)
		{
			Color color = ((SolidColorBrush)previousVWAPBrush).Color;
			previousVWAPBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref color)).R / 255f, (float)(int)((Color)(ref color)).G / 255f, (float)(int)((Color)(ref color)).B / 255f, 0.7f));
		}
	}

	private void EnsurePreviousVWAPStrokeStyle()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
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
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget == null || chartControl == null || chartControl.Properties == null)
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
			labelBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref val2)).R / 255f, (float)(int)((Color)(ref val2)).G / 255f, (float)(int)((Color)(ref val2)).B / 255f, 0.8f));
			cachedLabelMediaColor = val2;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
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
