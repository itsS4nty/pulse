#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	// Pulse Absorption
	// Flags "absorption" bars: heavy one-sided aggression that FAILS to move price in its
	// direction — i.e. resting limit orders soaking up the market orders.
	//   * Bullish (buy-side absorption): sellers were aggressive (delta < 0) on above-average
	//     volume, yet price held / closed in the upper part of the bar -> buyers absorbed.
	//   * Bearish (sell-side absorption): buyers aggressive, price held / closed low -> sellers absorbed.
	// Marks a triangle below (bullish) / above (bearish) the bar. Per-bar delta is computed from the
	// secondary Tick(1) series (Tick Replay recommended for history). The volume threshold
	// auto-calibrates via an EMA of recent bar volume; the delta/result thresholds are ratios, so
	// they are instrument-independent. Bars are evaluated on close (the signal for bar N appears when
	// bar N+1 opens) — no repaint.
	public class PulseAbsorption : Indicator
	{
		private double tickSize = 0.25;
		private double lastTickPrice = double.NaN;
		private int lastTickSide; // +1 buy, -1 sell, 0 unknown

		private int curBarIndex = -1;
		private double curBarVol;
		private double curBarDelta;

		private double avgVol;
		private bool avgSeeded;

		private int lookback = 20;
		private double volumeFactor = 1.5;
		private double deltaImbalance = 0.4;
		private double resultFactor = 0.6;

		private System.Windows.Media.Brush bullBrush;
		private System.Windows.Media.Brush bearBrush;

		[Range(2, 500)]
		[Display(Name = "Volume Lookback (bars)", Description = "Bars used for the average-volume baseline (the 'high effort' threshold)", Order = 1, GroupName = "Parameters")]
		public int Lookback
		{
			get { return lookback; }
			set { lookback = Math.Max(2, Math.Min(500, value)); }
		}

		[Range(1.0, 10.0)]
		[Display(Name = "Volume Factor", Description = "Bar volume must be >= average volume * this to count as high effort", Order = 2, GroupName = "Parameters")]
		public double VolumeFactor
		{
			get { return volumeFactor; }
			set { volumeFactor = Math.Max(1.0, Math.Min(10.0, value)); }
		}

		[Range(0.0, 1.0)]
		[Display(Name = "Delta Imbalance", Description = "Minimum one-sidedness |delta|/volume (0 = balanced, 1 = fully one-sided)", Order = 3, GroupName = "Parameters")]
		public double DeltaImbalance
		{
			get { return deltaImbalance; }
			set { deltaImbalance = Math.Max(0.0, Math.Min(1.0, value)); }
		}

		[Range(0.55, 1.0)]
		[Display(Name = "Result Factor", Description = "How far AGAINST the aggression price must close (0.6 = closed in the opposite 40% of the bar)", Order = 4, GroupName = "Parameters")]
		public double ResultFactor
		{
			get { return resultFactor; }
			set { resultFactor = Math.Max(0.55, Math.Min(1.0, value)); }
		}

		[XmlIgnore]
		[Display(Name = "Bullish Absorption Color", Description = "Marker color for buy-side (bullish) absorption", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush BullBrush
		{
			get { return bullBrush; }
			set { bullBrush = value; }
		}

		[Browsable(false)]
		public string BullBrushSerializable
		{
			get { return Serialize.BrushToString(bullBrush); }
			set { bullBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bearish Absorption Color", Description = "Marker color for sell-side (bearish) absorption", Order = 2, GroupName = "Visual")]
		public System.Windows.Media.Brush BearBrush
		{
			get { return bearBrush; }
			set { bearBrush = value; }
		}

		[Browsable(false)]
		public string BearBrushSerializable
		{
			get { return Serialize.BrushToString(bearBrush); }
			set { bearBrush = Serialize.StringToBrush(value); }
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Absorption - flags bars where heavy one-sided aggression failed to move price (limit orders absorbing)";
				Name = "PulseAbsorption";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = true;
				lookback = 20;
				volumeFactor = 1.5;
				deltaImbalance = 0.4;
				resultFactor = 0.6;
				bullBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 160, 152));
				bearBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 123, 57));
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				tickSize = (Instrument != null) ? Instrument.MasterInstrument.TickSize : 0.25;
				curBarIndex = -1;
				curBarVol = 0.0;
				curBarDelta = 0.0;
				avgVol = 0.0;
				avgSeeded = false;
				lastTickPrice = double.NaN;
				lastTickSide = 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray.Length < 2 || BarsInProgress != 1)
			{
				return;
			}
			if (CurrentBars[1] < 0 || CurrentBars[0] < 0)
			{
				return;
			}

			// Detect a primary-bar boundary from within the tick series (single branch = order-safe):
			// when the primary bar index advances, the bar we were accumulating is complete.
			if (CurrentBars[0] != curBarIndex)
			{
				if (curBarIndex >= 0)
				{
					EvaluateBar(CurrentBars[0] - curBarIndex);
				}
				curBarIndex = CurrentBars[0];
				curBarVol = 0.0;
				curBarDelta = 0.0;
			}

			// New session: drop stale tick-rule state and re-warm the volume baseline
			// (RTH and overnight regimes have very different volume).
			if (BarsArray[1].IsFirstBarOfSession)
			{
				lastTickPrice = double.NaN;
				lastTickSide = 0;
				avgSeeded = false;
			}

			double price = Closes[1][0];
			if (double.IsNaN(price))
			{
				return;
			}
			double vol = Volumes[1][0];
			if (vol > 0.0)
			{
				int side = ClassifySide(price, BarsArray[1].GetBid(CurrentBars[1]), BarsArray[1].GetAsk(CurrentBars[1]));
				curBarVol += vol;
				curBarDelta += vol * side;
			}
		}

		// Evaluate the just-completed primary bar (at barsAgo `ago` on the primary series).
		private void EvaluateBar(int ago)
		{
			if (ago < 1)
			{
				return;
			}
			double vol = curBarVol;
			if (vol <= 0.0)
			{
				return; // empty bar: nothing to evaluate, and don't pollute the volume baseline
			}
			UpdateAvg(vol);
			if (avgVol <= 0.0)
			{
				return;
			}
			double delta = curBarDelta;
			double high = Highs[0][ago];
			double low = Lows[0][ago];
			double close = Closes[0][ago];
			double range = high - low;
			if (range <= 0.0)
			{
				return;
			}
			double closePos = (close - low) / range;   // 0 = closed at low, 1 = closed at high
			double imbalance = Math.Abs(delta) / vol;   // 0 = balanced, 1 = fully one-sided
			if (vol < avgVol * volumeFactor || imbalance < deltaImbalance)
			{
				return;
			}
			DateTime t = Times[0][ago];
			if (delta < 0.0 && closePos >= resultFactor)
			{
				// Sellers were aggressive but price held in the upper part -> buy-side absorption (bullish)
				Draw.TriangleUp(this, "PulseAbsBull" + curBarIndex, false, t, low - 2.0 * tickSize, bullBrush);
			}
			else if (delta > 0.0 && closePos <= 1.0 - resultFactor)
			{
				// Buyers were aggressive but price held in the lower part -> sell-side absorption (bearish)
				Draw.TriangleDown(this, "PulseAbsBear" + curBarIndex, false, t, high + 2.0 * tickSize, bearBrush);
			}
		}

		private void UpdateAvg(double vol)
		{
			if (!avgSeeded)
			{
				avgVol = vol;
				avgSeeded = true;
			}
			else
			{
				double alpha = 2.0 / (lookback + 1.0);
				avgVol += alpha * (vol - avgVol);
			}
		}

		// +1 = buy (aggressor lifted at ask), -1 = sell (hit at bid), 0 = unknown.
		private int ClassifySide(double price, double bid, double ask)
		{
			if (bid > 0.0 && ask > 0.0 && ask >= bid)
			{
				double tol = Math.Max(tickSize * 0.25, 1E-08);
				if (price >= ask - tol)
				{
					return 1;
				}
				if (price <= bid + tol)
				{
					return -1;
				}
			}
			int s = 0;
			if (!double.IsNaN(lastTickPrice))
			{
				s = (price > lastTickPrice) ? 1 : ((price < lastTickPrice) ? -1 : lastTickSide);
			}
			lastTickPrice = price;
			if (s != 0)
			{
				lastTickSide = s;
			}
			return s;
		}
	}
}
