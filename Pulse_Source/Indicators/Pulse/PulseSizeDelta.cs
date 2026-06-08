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
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	// Pulse Size Delta
	// Cumulative trade-flow delta segmented by trade size: each trade is classified by
	// aggressor (buy at ask / sell at bid, with a tick-rule fallback) AND by size into
	// Small / Medium / Large buckets, then plotted as one cumulative delta line per bucket.
	// Reads the secondary Tick(1) series (same pattern as PulseCumulativeDelta) so it works
	// in realtime and historically WITH Tick Replay enabled. Tick Replay is recommended for
	// accurate historical per-print sizes (without it, the minute tick series may aggregate).
	public class PulseSizeDelta : Indicator
	{
		// Cumulative signed volume per bucket: index 0 = Small, 1 = Medium, 2 = Large. Reset per session.
		private readonly long[] cumDelta = new long[3];

		// Tick-rule state (fallback when bid/ask are unavailable on the tick series)
		private double lastTickPrice = double.NaN;
		private int lastTickSide; // +1 buy, -1 sell, 0 unknown

		private double tickSize = 0.25;

		private int smallMaxSize = 4;
		private int largeMinSize = 25;
		private PulseCumulativeDeltaResetPeriod resetPeriod = PulseCumulativeDeltaResetPeriod.Session;

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Small Max Size", Description = "Trades with size <= this land in the Small bucket", Order = 1, GroupName = "Parameters")]
		public int SmallMaxSize
		{
			get { return smallMaxSize; }
			set { smallMaxSize = Math.Max(1, value); }
		}

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Large Min Size", Description = "Trades with size >= this land in the Large bucket", Order = 2, GroupName = "Parameters")]
		public int LargeMinSize
		{
			get { return largeMinSize; }
			set { largeMinSize = Math.Max(2, value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Reset Period", Description = "When to reset the cumulative size-delta (Session/Daily use the session boundary; Never = never resets)", Order = 3, GroupName = "Parameters")]
		public PulseCumulativeDeltaResetPeriod ResetPeriod
		{
			get { return resetPeriod; }
			set { resetPeriod = value; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SmallDelta => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MediumDelta => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LargeDelta => Values[2];

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Size Delta - cumulative delta segmented by trade size (small / medium / large)";
				Name = "PulseSizeDelta";
				Calculate = Calculate.OnEachTick;
				IsOverlay = false;
				DisplayInDataBox = true;
				DrawOnPricePanel = false;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				smallMaxSize = 4;
				largeMinSize = 25;
				resetPeriod = PulseCumulativeDeltaResetPeriod.Session;
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 160, 152)), 1f), PlotStyle.Line, "Small");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204)), 1.5f), PlotStyle.Line, "Medium");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 123, 57)), 2.5f), PlotStyle.Line, "Large");
				AddLine(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), 1f), 0.0, "Zero");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				tickSize = (Instrument != null) ? Instrument.MasterInstrument.TickSize : 0.25;
				if (largeMinSize <= smallMaxSize)
				{
					largeMinSize = smallMaxSize + 1; // keep buckets non-overlapping
				}
				ResetAccumulators();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray.Length < 2)
			{
				return;
			}

			if (BarsInProgress == 1)
			{
				// One bar per trade on the Tick(1) series. Accumulate as soon as the tick series
				// is valid — even while primary bar 0 is still forming — so no order flow is dropped.
				if (CurrentBars[1] < 0)
				{
					return;
				}
				if (resetPeriod != PulseCumulativeDeltaResetPeriod.Never && BarsArray[1].IsFirstBarOfSession)
				{
					ResetAccumulators();
				}
				AccumulateTick();
			}
			else if (BarsInProgress == 0)
			{
				// Snapshot the running cumulative per bucket onto the current primary bar.
				if (CurrentBars[0] < 0)
				{
					return;
				}
				Values[0][0] = cumDelta[0];
				Values[1][0] = cumDelta[1];
				Values[2][0] = cumDelta[2];
			}
		}

		private void AccumulateTick()
		{
			int idx = CurrentBars[1];
			double volume = Volumes[1][0];
			if (volume <= 0.0)
			{
				return;
			}
			double price = Closes[1][0];
			int side = ClassifySide(price, BarsArray[1].GetBid(idx), BarsArray[1].GetAsk(idx));
			if (side == 0)
			{
				return; // unclassifiable (no quote + no prior price) -> skip rather than mis-assign
			}
			cumDelta[BucketFor((long)volume)] += (long)volume * side;
		}

		// +1 = buy (aggressor lifted at ask), -1 = sell (hit at bid), 0 = unknown.
		// Uses the tick series' bid/ask; in pure realtime WITHOUT Tick Replay those quotes may be
		// absent on live-appended ticks, so it falls back to the up/down tick rule.
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
			// Tick-rule fallback: up-tick = buy, down-tick = sell, unchanged = carry last side.
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

		private int BucketFor(long size)
		{
			// Robust against an inverted/overlapping config (smallMax >= largeMin): force Large strictly above Small.
			long effLargeMin = Math.Max(largeMinSize, (long)smallMaxSize + 1L);
			if (size >= effLargeMin)
			{
				return 2; // Large
			}
			if (size <= smallMaxSize)
			{
				return 0; // Small
			}
			return 1; // Medium
		}

		private void ResetAccumulators()
		{
			cumDelta[0] = 0L;
			cumDelta[1] = 0L;
			cumDelta[2] = 0L;
			lastTickPrice = double.NaN;
			lastTickSide = 0;
		}
	}
}
