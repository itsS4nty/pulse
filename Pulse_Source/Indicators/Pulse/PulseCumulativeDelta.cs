#region Using declarations
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
using SharpDX;
using SharpDX.Direct2D1;
#endregion

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

		private System.Windows.Media.Brush positiveBrush;

		private System.Windows.Media.Brush negativeBrush;

		private System.Windows.Media.Brush zeroLineBrush;

		private SharpDX.Direct2D1.SolidColorBrush positiveBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush negativeBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush zeroLineBrushDx;

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
		public System.Windows.Media.Brush PositiveBrush
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
		public System.Windows.Media.Brush NegativeBrush
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
		public System.Windows.Media.Brush ZeroLineBrush
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
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				resetPeriod = PulseCumulativeDeltaResetPeriod.Session;
				showZeroLine = true;
				colorBasedOnDirection = true;
				positiveBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				negativeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				zeroLineBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				AddPlot(new Stroke(Brushes.Transparent), PlotStyle.PriceBox, "DeltaOpen");
				AddPlot(new Stroke(Brushes.Transparent), PlotStyle.PriceBox, "DeltaHigh");
				AddPlot(new Stroke(Brushes.Transparent), PlotStyle.PriceBox, "DeltaLow");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204))), PlotStyle.PriceBox, "DeltaClose");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
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
				int index = ((State == State.Historical || flag || Calculate != Calculate.OnBarClose || forceCurrentBar) ? CurrentBars[1] : Math.Min(CurrentBars[1] + 1, BarsArray[1].Count - 1));
				double num2 = BarsArray[1].GetVolume(index);
				double close = BarsArray[1].GetClose(index);
				if (close >= BarsArray[1].GetAsk(index) && num2 > 0.0)
				{
					buys += num2;
				}
				else if (close <= BarsArray[1].GetBid(index) && num2 > 0.0)
				{
					sells += num2;
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
					PlotBrushes[3][0] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 188, 198));
				}
			}
			else
			{
				PlotBrushes[3][0] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 188, 198));
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
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
			Series<double> series = Values[0];
			Series<double> series2 = Values[1];
			Series<double> series3 = Values[2];
			Series<double> series4 = Values[3];
			for (int i = fromIndex; i <= toIndex; i++)
			{
				int num3 = i - Displacement;
				if (num3 < BarsRequiredToPlot || num3 < 0 || num3 >= count)
				{
					continue;
				}
				double valueAt = series.GetValueAt(i);
				double valueAt2 = series2.GetValueAt(i);
				double valueAt3 = series3.GetValueAt(i);
				double valueAt4 = series4.GetValueAt(i);
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
						double valueAt5 = series4.GetValueAt(i - 1);
						flag = valueAt4 > valueAt5;
					}
					SharpDX.Direct2D1.SolidColorBrush brush = (flag ? positiveBrushDx : negativeBrushDx);
					RenderTarget.DrawLine(new Vector2(num4, num6), new Vector2(num4, num7), brush, 1f);
					float y = Math.Min(num5, num8);
					float num9 = Math.Abs(num8 - num5);
					if (num9 < 0.5f)
					{
						RenderTarget.DrawLine(new Vector2(num4 - num2, num5), new Vector2(num4 + num2, num5), brush, 1f);
						continue;
					}
					RectangleF rect = new RectangleF(num4 - num2, y, num, Math.Max(1f, num9));
					RenderTarget.FillRectangle(rect, brush);
					RenderTarget.DrawRectangle(rect, brush, 1f);
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
					float x = ChartPanel.X;
					float x2 = ChartPanel.X + ChartPanel.W;
					RenderTarget.DrawLine(new Vector2(x, num), new Vector2(x2, num), zeroLineBrushDx, 1f, zeroLineDashStrokeStyle);
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
			if (RenderTarget != null && zeroLineBrush != null)
			{
				System.Windows.Media.Color brushColor = GetBrushColor(zeroLineBrush, System.Windows.Media.Color.FromRgb(74, 74, 74));
				Color4 color = new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, 0.7f);
				if (zeroLineBrushDx == null || !Color4Equals(zeroLineBrushDx.Color, color))
				{
					zeroLineBrushDx?.Dispose();
					zeroLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color);
				}
			}
		}

		private void EnsureColorBrushes()
		{
			if (RenderTarget != null)
			{
				EnsureDxBrush(ref positiveBrushDx, positiveBrush, System.Windows.Media.Color.FromRgb(107, 111, 204));
				EnsureDxBrush(ref negativeBrushDx, negativeBrush, System.Windows.Media.Color.FromRgb(74, 74, 74));
			}
		}

		private void EnsureDxBrush(ref SharpDX.Direct2D1.SolidColorBrush target, System.Windows.Media.Brush source, System.Windows.Media.Color fallback)
		{
			if (source != null && RenderTarget != null)
			{
				System.Windows.Media.Color brushColor = GetBrushColor(source, fallback);
				Color4 color = new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, 1f);
				if (target == null || !Color4Equals(target.Color, color))
				{
					target?.Dispose();
					target = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color);
				}
			}
		}

		private void EnsureZeroLineStrokeStyle()
		{
			if (zeroLineDashStrokeStyle == null)
			{
				zeroLineDashStrokeStyle = new StrokeStyle(Globals.D2DFactory, new StrokeStyleProperties
				{
					DashStyle = SharpDX.Direct2D1.DashStyle.Dash
				});
			}
		}

		private static System.Windows.Media.Color GetBrushColor(System.Windows.Media.Brush brush, System.Windows.Media.Color fallback)
		{
			if (!(brush is System.Windows.Media.SolidColorBrush solidColorBrush))
			{
				return fallback;
			}
			return solidColorBrush.Color;
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
			base.OnRenderTargetChanged();
		}

		private void DisposeDx()
		{
			positiveBrushDx?.Dispose();
			positiveBrushDx = null;
			negativeBrushDx?.Dispose();
			negativeBrushDx = null;
			zeroLineBrushDx?.Dispose();
			zeroLineBrushDx = null;
			zeroLineDashStrokeStyle?.Dispose();
			zeroLineDashStrokeStyle = null;
		}
	}
}
