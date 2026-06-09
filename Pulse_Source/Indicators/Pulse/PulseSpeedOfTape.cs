#region Using declarations
using System;
using System.Collections.Generic;
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
	// Pulse Speed of Tape
	// Measures how fast the tape is printing over a rolling TIME window (not per bar):
	// either contracts/second (volume velocity) or trades/second (print rate). A baseline
	// EMA of the speed is plotted so you can see when the tape is accelerating (a "burst")
	// versus its normal pace. Spikes flag initiative / climax / capitulation moments.
	//
	// Reads the secondary Tick(1) series (same pattern as PulseCumulativeDelta / PulseSizeDelta)
	// so it works in realtime and historically with Tick Replay. The rolling window keys off the
	// trade timestamps, so it self-cleans across session gaps and works on any primary bar type.
	public class PulseSpeedOfTape : Indicator
	{
		private struct Tape
		{
			public long T; // trade time in DateTime ticks
			public long V; // trade volume

			public Tape(long t, long v)
			{
				T = t;
				V = v;
			}
		}

		// Rolling window of recent trades (front = oldest)
		private Queue<Tape> window;
		private long volSum;

		private double currentSpeed;
		private double baseline;
		private bool baselineSeeded;
		private int lastBaselineBar = -1;
		private long lastTradeTicks;
		private bool hasLastTrade;
		private int lastSessionResetBar = -1;

		private bool measureByVolume = true;
		private int windowSeconds = 5;
		private int baselinePeriod = 30;
		private double burstMultiplier = 2.0;

		[NinjaScriptProperty]
		[Display(Name = "Measure By Volume", Description = "On = contracts/second (volume velocity); Off = trades/second (print rate)", Order = 1, GroupName = "Parameters")]
		public bool MeasureByVolume
		{
			get { return measureByVolume; }
			set { measureByVolume = value; }
		}

		[Range(1, 300)]
		[Display(Name = "Window (seconds)", Description = "Length of the rolling time window the speed is measured over", Order = 2, GroupName = "Parameters")]
		public int WindowSeconds
		{
			get { return windowSeconds; }
			set { windowSeconds = Math.Max(1, Math.Min(300, value)); }
		}

		[Range(2, 1000)]
		[Display(Name = "Baseline Period (bars)", Description = "Bars used for the baseline EMA (the 'normal' speed reference line)", Order = 3, GroupName = "Parameters")]
		public int BaselinePeriod
		{
			get { return baselinePeriod; }
			set { baselinePeriod = Math.Max(2, Math.Min(1000, value)); }
		}

		[Range(1.0, 20.0)]
		[Display(Name = "Burst Multiplier", Description = "IsBurst is true when current speed >= baseline * this multiplier", Order = 4, GroupName = "Parameters")]
		public double BurstMultiplier
		{
			get { return burstMultiplier; }
			set { burstMultiplier = Math.Max(1.0, Math.Min(20.0, value)); }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Speed => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SpeedBaseline => Values[1];

		// True when the tape is currently running well above its normal pace.
		[Browsable(false)]
		[XmlIgnore]
		public bool IsBurst => baselineSeeded && baseline > 0.0 && currentSpeed >= baseline * burstMultiplier;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Speed of Tape - rolling trade-intensity (contracts/sec or trades/sec) with a baseline pace line";
				Name = "PulseSpeedOfTape";
				Calculate = Calculate.OnEachTick;
				IsOverlay = false;
				DisplayInDataBox = true;
				DrawOnPricePanel = false;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				measureByVolume = true;
				windowSeconds = 5;
				baselinePeriod = 30;
				burstMultiplier = 2.0;
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204)), 1f), PlotStyle.Bar, "Speed");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 123, 57)), 1.5f), PlotStyle.Line, "Baseline");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				window = new Queue<Tape>(4096);
				volSum = 0L;
				currentSpeed = 0.0;
				baseline = 0.0;
				baselineSeeded = false;
				lastBaselineBar = -1;
				lastTradeTicks = 0L;
				hasLastTrade = false;
				lastSessionResetBar = -1;
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
				// One bar per trade on the Tick(1) series.
				if (CurrentBars[1] < 0)
				{
					return;
				}
				ProcessTrade();
			}
			else if (BarsInProgress == 0)
			{
				if (CurrentBars[0] < 0)
				{
					return;
				}
				// New session: drop any pace carried over from the prior session so the open starts clean.
				if (BarsArray[0].IsFirstBarOfSession && CurrentBars[0] != lastSessionResetBar)
				{
					if (window != null)
					{
						window.Clear();
					}
					volSum = 0L;
					currentSpeed = 0.0;
					hasLastTrade = false;
					lastSessionResetBar = CurrentBars[0];
				}
				// Advance the baseline EMA once per new primary bar.
				if (CurrentBars[0] != lastBaselineBar)
				{
					if (!baselineSeeded)
					{
						baseline = currentSpeed;
						baselineSeeded = true;
					}
					else
					{
						double alpha = 2.0 / (baselinePeriod + 1.0);
						baseline += alpha * (currentSpeed - baseline);
					}
					lastBaselineBar = CurrentBars[0];
				}
				Values[0][0] = currentSpeed;
				Values[1][0] = baseline;
			}
		}

		private void ProcessTrade()
		{
			if (window == null)
			{
				return;
			}
			long nowTicks = Times[1][0].Ticks;
			long vol = (long)Volumes[1][0];
			if (vol < 0L)
			{
				vol = 0L;
			}
			long windowTicks = (long)windowSeconds * TimeSpan.TicksPerSecond;

			// If the tape has been silent for longer than the window (e.g. a session gap), the
			// carried-over trades are all stale — clear in one shot instead of draining each.
			if (hasLastTrade && nowTicks - lastTradeTicks > windowTicks)
			{
				window.Clear();
				volSum = 0L;
			}
			lastTradeTicks = nowTicks;
			hasLastTrade = true;

			window.Enqueue(new Tape(nowTicks, vol));
			volSum += vol;

			// Drop trades older than the window.
			long cutoff = nowTicks - windowTicks;
			while (window.Count > 0 && window.Peek().T < cutoff)
			{
				volSum -= window.Dequeue().V;
			}

			double secs = windowSeconds;
			currentSpeed = measureByVolume ? ((double)volSum / secs) : ((double)window.Count / secs);
		}
	}
}
