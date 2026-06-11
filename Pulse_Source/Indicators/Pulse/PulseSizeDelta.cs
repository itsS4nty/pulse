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
	//
	// Bucketing has two modes:
	//   * Adaptive (default): "Large" = top (100-LargePercentile)% of RECENT trade sizes,
	//     "Small" = bottom SmallPercentile%, computed from a rolling window. Auto-calibrates to
	//     the instrument so the Large line stays meaningful even where most prints are tiny (ES).
	//   * Fixed: hard contract-count thresholds (SmallMaxSize / LargeMinSize). Also used as the
	//     warmup fallback until the adaptive window has enough samples.
	//
	// Reads the secondary Tick(1) series (same pattern as PulseCumulativeDelta) so it works in
	// realtime and historically with Tick Replay. Tick Replay is recommended for accurate
	// historical per-print sizes (without it the minute tick series may aggregate).
	public class PulseSizeDelta : Indicator
	{
		// Cumulative signed volume per bucket: index 0 = Small, 1 = Medium, 2 = Large. Reset per session.
		private readonly long[] cumDelta = new long[3];

		// Tick-rule state (fallback when bid/ask are unavailable on the tick series)
		private double lastTickPrice = double.NaN;
		private int lastTickSide; // +1 buy, -1 sell, 0 unknown

		private double tickSize = 0.25;

		// Fixed-mode thresholds
		private int smallMaxSize = 4;
		private int largeMinSize = 25;

		// Adaptive-mode config
		private bool useAdaptive = true;
		private int adaptiveWindow = 1000;
		private double smallPercentile = 60.0;
		private double largePercentile = 90.0;

		// Adaptive-mode rolling state (a circular buffer of recent trade sizes)
		private int[] sizeBuffer;
		private int[] scratch;
		private int bufCount;
		private int bufIndex;
		private long adaptiveSmallCut;
		private long adaptiveLargeCut;
		private bool adaptiveValid;
		private int tradesSinceRecompute;
		private const int RecomputeEvery = 50;     // recompute the percentile cutoffs every N trades
		private const int MinAdaptiveSamples = 30;  // need at least this many trades before going adaptive

		private PulseCumulativeDeltaResetPeriod resetPeriod = PulseCumulativeDeltaResetPeriod.Session;

		[NinjaScriptProperty]
		[Display(Name = "Adaptive Thresholds", Description = "Dynamic buckets by percentile of recent trade sizes (auto-calibrates to the instrument). Off = fixed Small/Large sizes.", Order = 1, GroupName = "Parameters")]
		public bool UseAdaptiveThresholds
		{
			get { return useAdaptive; }
			set { useAdaptive = value; }
		}

		[NinjaScriptProperty]
		[Display(Name = "Reset Period", Description = "When to reset the cumulative size-delta (Session/Daily use the session boundary; Never = never resets)", Order = 2, GroupName = "Parameters")]
		public PulseCumulativeDeltaResetPeriod ResetPeriod
		{
			get { return resetPeriod; }
			set { resetPeriod = value; }
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Small Max Size", Description = "Fixed mode: trades with size <= this land in the Small bucket", Order = 1, GroupName = "Fixed size")]
		public int SmallMaxSize
		{
			get { return smallMaxSize; }
			set { smallMaxSize = Math.Max(1, value); }
		}

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Large Min Size", Description = "Fixed mode: trades with size >= this land in the Large bucket", Order = 2, GroupName = "Fixed size")]
		public int LargeMinSize
		{
			get { return largeMinSize; }
			set { largeMinSize = Math.Max(2, value); }
		}

		[NinjaScriptProperty]
		[Range(50, 100000)]
		[Display(Name = "Adaptive Window (trades)", Description = "Adaptive mode: how many recent trades define the size distribution", Order = 1, GroupName = "Adaptive size")]
		public int AdaptiveWindow
		{
			get { return adaptiveWindow; }
			set { adaptiveWindow = Math.Max(50, Math.Min(100000, value)); }
		}

		[NinjaScriptProperty]
		[Range(1.0, 98.0)]
		[Display(Name = "Small Percentile", Description = "Adaptive mode: trades at/below this percentile of recent sizes = Small", Order = 2, GroupName = "Adaptive size")]
		public double SmallPercentile
		{
			get { return smallPercentile; }
			set { smallPercentile = Math.Max(1.0, Math.Min(98.0, value)); }
		}

		[NinjaScriptProperty]
		[Range(2.0, 99.0)]
		[Display(Name = "Large Percentile", Description = "Adaptive mode: trades at/above this percentile of recent sizes = Large", Order = 3, GroupName = "Adaptive size")]
		public double LargePercentile
		{
			get { return largePercentile; }
			set { largePercentile = Math.Max(2.0, Math.Min(99.0, value)); }
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

		// Current adaptive cutoffs (size in contracts) once warm — handy for debugging / data box.
		[Browsable(false)]
		[XmlIgnore]
		public double CurrentSmallCut => adaptiveValid ? adaptiveSmallCut : smallMaxSize;

		[Browsable(false)]
		[XmlIgnore]
		public double CurrentLargeCut => adaptiveValid ? adaptiveLargeCut : largeMinSize;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Size Delta - cumulative delta segmented by trade size (small / medium / large), fixed or adaptive buckets";
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
				useAdaptive = true;
				smallMaxSize = 4;
				largeMinSize = 25;
				adaptiveWindow = 1000;
				smallPercentile = 60.0;
				largePercentile = 90.0;
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
					largeMinSize = smallMaxSize + 1; // keep fixed buckets non-overlapping
				}
				if (largePercentile <= smallPercentile)
				{
					largePercentile = Math.Min(99.0, smallPercentile + 1.0); // keep adaptive buckets non-overlapping
				}
				int win = Math.Max(50, adaptiveWindow);
				sizeBuffer = new int[win];
				scratch = new int[win];
				bufCount = 0;
				bufIndex = 0;
				adaptiveValid = false;
				tradesSinceRecompute = 0;
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
			int size = (volume >= int.MaxValue) ? int.MaxValue : (int)volume;
			UpdateSizeDistribution(size); // feed the adaptive window with every trade (regardless of side)
			double price = Closes[1][0];
			int side = ClassifySide(price, BarsArray[1].GetBid(idx), BarsArray[1].GetAsk(idx));
			if (side == 0)
			{
				return; // unclassifiable (no quote + no prior price) -> skip rather than mis-assign
			}
			cumDelta[BucketFor(size)] += (long)size * side;
		}

		// Maintain the rolling distribution of recent trade sizes and refresh the percentile cutoffs.
		private void UpdateSizeDistribution(int size)
		{
			if (sizeBuffer == null || sizeBuffer.Length == 0)
			{
				return;
			}
			sizeBuffer[bufIndex] = size;
			bufIndex = (bufIndex + 1) % sizeBuffer.Length;
			if (bufCount < sizeBuffer.Length)
			{
				bufCount++;
			}
			tradesSinceRecompute++;
			if (bufCount >= MinAdaptiveSamples && (!adaptiveValid || tradesSinceRecompute >= RecomputeEvery))
			{
				RecomputeAdaptiveCuts();
				tradesSinceRecompute = 0;
			}
		}

		private void RecomputeAdaptiveCuts()
		{
			int n = bufCount; // bufCount is capped at sizeBuffer.Length; valid samples are the first n
			if (n <= 0 || scratch == null)
			{
				adaptiveValid = false;
				return;
			}
			Array.Copy(sizeBuffer, 0, scratch, 0, n);
			Array.Sort(scratch, 0, n);
			adaptiveSmallCut = PercentileValue(scratch, n, smallPercentile);
			adaptiveLargeCut = PercentileValue(scratch, n, largePercentile);
			if (adaptiveLargeCut <= adaptiveSmallCut)
			{
				adaptiveLargeCut = adaptiveSmallCut + 1; // keep buckets non-overlapping
			}
			adaptiveValid = true;
		}

		// Nearest-rank percentile over sortedAsc[0..count).
		private static long PercentileValue(int[] sortedAsc, int count, double pct)
		{
			if (count <= 0)
			{
				return 0L;
			}
			int idx = (int)Math.Round((pct / 100.0) * (count - 1));
			if (idx < 0)
			{
				idx = 0;
			}
			else if (idx >= count)
			{
				idx = count - 1;
			}
			return sortedAsc[idx];
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
			if (useAdaptive && adaptiveValid)
			{
				if (size >= adaptiveLargeCut)
				{
					return 2; // Large
				}
				if (size <= adaptiveSmallCut)
				{
					return 0; // Small
				}
				return 1; // Medium
			}
			// Fixed mode (also the warmup fallback before the adaptive window has enough samples).
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

		// Note: the size-distribution buffer is intentionally NOT cleared here — it is a rolling
		// view of typical trade sizes that should persist across sessions. Only the cumulative
		// delta (which is per-session) and the tick-rule state reset.
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
