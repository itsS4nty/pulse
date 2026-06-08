#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	public class PulseBigTrades : Indicator
	{
		private enum TradeSide
		{
			Unknown = 0,
			Buy = 1,
			Sell = -1
		}

		private sealed class BigTrade
		{
			public double Price;

			public long Volume;

			public DateTime Time;

			public TradeSide Side;

			public int BarIndex;

			public string LabelText;
		}

		private sealed class ClusterBucket
		{
			public long TotalVolume;

			public double WeightedPriceSum;

			public DateTime LastTimeUtc;

			public bool Emitted;
		}

		private readonly List<BigTrade> bigTrades = new List<BigTrade>(4096);

		private readonly List<BigTrade> renderSnapshot = new List<BigTrade>(4096);

		private readonly object tradesLock = new object();

		private long tradesVersion;

		private long renderSnapshotVersion = -1L;

		private readonly List<BigTrade> pendingCacheWrites = new List<BigTrade>(256);

		private readonly object cacheLock = new object();

		private int minContractsThreshold = 100;

		private int maxCircleRadius = 25;

		private int minCircleRadius = 8;

		private int showLabelThreshold = 200;

		private float circleOpacity = 0.6f;

		private PulseBigTradesCircleStyle circleStyle;

		private float circleOutlineWidth = 1.5f;

		private bool resetDaily;

		private PulseBigTradesDetectionMode detectionMode;

		private int clusterMinContracts = 250;

		private int clusterWindowMs = 75;

		private int clusterPriceGroupingTicks = 1;

		private System.Windows.Media.Brush buyBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));

		private System.Windows.Media.Brush sellBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private System.Windows.Media.Brush labelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));

		private SharpDX.Direct2D1.SolidColorBrush buyBrushDX;

		private SharpDX.Direct2D1.SolidColorBrush sellBrushDX;

		private SharpDX.Direct2D1.SolidColorBrush unknownBrushDX;

		private SharpDX.Direct2D1.SolidColorBrush labelBrushDX;

		private TextFormat textFormat;

		private DateTime sessionDate = DateTime.MinValue;

		private DateTime nextUiRefreshAtUtc = DateTime.MinValue;

		private DateTime nextMaintenanceAtUtc = DateTime.MinValue;

		private DateTime nextCacheFlushAtUtc = DateTime.MinValue;

		private double tickSize = 0.25;

		private bool isPrimaryTickSeries;

		private bool cacheLoaded;

		private long historicalTicksSeen;

		private double lastRealtimePrice = double.NaN;

		private TradeSide lastRealtimeSide;

		private double lastHistoricalPrice = double.NaN;

		private TradeSide lastHistoricalSide;

		private readonly Dictionary<long, ClusterBucket> realtimeClusterBuckets = new Dictionary<long, ClusterBucket>(256);

		private readonly Dictionary<long, ClusterBucket> historicalClusterBuckets = new Dictionary<long, ClusterBucket>(256);

		private readonly List<long> clusterPruneScratch = new List<long>(256);

		private readonly object clusterLock = new object();

		private DateTime nextClusterPruneAtUtc = DateTime.MinValue;

		private string cacheFilePath = string.Empty;

		private const int MaxStoredTrades = 20000;

		private const int MaxMarkersPerRender = 2500;

		private const int UiRefreshThrottleMs = 75;

		private const int MaintenanceIntervalMs = 750;

		private const int CacheFlushIntervalMs = 2000;

		private const int CacheLookbackDays = 30;

		private const int ClusterPruneIntervalMs = 500;

		private const bool EnableLocalCache = true;

		private const bool EnableDebugLogging = false;

		[NinjaScriptProperty]
		[Range(10, 10000)]
		[Display(Name = "Min Contracts Threshold", Description = "Volumen minimo para considerar un big trade", Order = 1, GroupName = "Configuracion")]
		public int MinContractsThreshold
		{
			get
			{
				return minContractsThreshold;
			}
			set
			{
				minContractsThreshold = Math.Max(10, value);
			}
		}

		[NinjaScriptProperty]
		[Range(5, 50)]
		[Display(Name = "Max Circle Radius", Description = "Radio maximo del circulo en pixeles", Order = 2, GroupName = "Visual")]
		public int MaxCircleRadius
		{
			get
			{
				return maxCircleRadius;
			}
			set
			{
				maxCircleRadius = Math.Max(5, Math.Min(50, value));
			}
		}

		[NinjaScriptProperty]
		[Range(3, 20)]
		[Display(Name = "Min Circle Radius", Description = "Radio minimo del circulo en pixeles", Order = 3, GroupName = "Visual")]
		public int MinCircleRadius
		{
			get
			{
				return minCircleRadius;
			}
			set
			{
				minCircleRadius = Math.Max(3, Math.Min(20, value));
			}
		}

		[NinjaScriptProperty]
		[Range(50, 10000)]
		[Display(Name = "Show Label Threshold", Description = "Volumen minimo para mostrar numero de contratos", Order = 4, GroupName = "Visual")]
		public int ShowLabelThreshold
		{
			get
			{
				return showLabelThreshold;
			}
			set
			{
				showLabelThreshold = Math.Max(50, value);
			}
		}

		[NinjaScriptProperty]
		[Range(0.1, 100.0)]
		[Display(Name = "Circle Opacity", Description = "Opacidad del circulo (acepta 60 o 0.6 / 0,6)", Order = 5, GroupName = "Visual")]
		public double CircleOpacity
		{
			get
			{
				return circleOpacity * 100f;
			}
			set
			{
				double num = Math.Max(0.1, Math.Min(100.0, value));
				circleOpacity = (float)((num <= 1.0) ? num : (num / 100.0));
				DisposeDxBrush(ref buyBrushDX);
				DisposeDxBrush(ref sellBrushDX);
				DisposeDxBrush(ref unknownBrushDX);
				RequestRenderRefresh();
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Circle Style", Description = "Relleno o solo borde con centro transparente", Order = 6, GroupName = "Visual")]
		public PulseBigTradesCircleStyle CircleStyle
		{
			get
			{
				return circleStyle;
			}
			set
			{
				circleStyle = value;
			}
		}

		[NinjaScriptProperty]
		[Range(0.5, 8.0)]
		[Display(Name = "Circle Border Width", Description = "Grosor del borde cuando Circle Style = OutlineOnly", Order = 7, GroupName = "Visual")]
		public double CircleBorderWidth
		{
			get
			{
				return circleOutlineWidth;
			}
			set
			{
				circleOutlineWidth = (float)Math.Max(0.5, Math.Min(8.0, value));
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Reset Daily", Description = "Limpiar trades al inicio de cada sesion", Order = 6, GroupName = "Configuracion")]
		public bool ResetDaily
		{
			get
			{
				return resetDaily;
			}
			set
			{
				resetDaily = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Detection Mode", Description = "SinglePrint o AggressiveCluster (hibrido mismo tick + ventana)", Order = 7, GroupName = "Configuracion")]
		public PulseBigTradesDetectionMode DetectionMode
		{
			get
			{
				return detectionMode;
			}
			set
			{
				detectionMode = value;
			}
		}

		[NinjaScriptProperty]
		[Range(10, 50000)]
		[Display(Name = "Cluster Min Contracts", Description = "Umbral minimo de volumen acumulado para cluster", Order = 8, GroupName = "Configuracion")]
		public int ClusterMinContracts
		{
			get
			{
				return clusterMinContracts;
			}
			set
			{
				clusterMinContracts = Math.Max(10, value);
			}
		}

		[NinjaScriptProperty]
		[Range(0, 1000)]
		[Display(Name = "Cluster Window (ms)", Description = "Ventana de acumulacion en ms (0 = mismo tick exacto)", Order = 9, GroupName = "Configuracion")]
		public int ClusterWindowMs
		{
			get
			{
				return clusterWindowMs;
			}
			set
			{
				clusterWindowMs = Math.Max(0, value);
			}
		}

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Cluster Price Group (ticks)", Description = "Agrupa precios para cluster en bloques de N ticks", Order = 10, GroupName = "Configuracion")]
		public int ClusterPriceGroupingTicks
		{
			get
			{
				return clusterPriceGroupingTicks;
			}
			set
			{
				clusterPriceGroupingTicks = Math.Max(1, value);
			}
		}

		[XmlIgnore]
		[Display(Name = "Buy Color", Description = "Color para trades de compra", Order = 1, GroupName = "Colores")]
		public System.Windows.Media.Brush BuyBrush
		{
			get
			{
				return buyBrush;
			}
			set
			{
				buyBrush = value;
				DisposeDxBrush(ref buyBrushDX);
			}
		}

		[Browsable(false)]
		public string BuyBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(buyBrush);
			}
			set
			{
				buyBrush = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "Sell Color", Description = "Color para trades de venta", Order = 2, GroupName = "Colores")]
		public System.Windows.Media.Brush SellBrush
		{
			get
			{
				return sellBrush;
			}
			set
			{
				sellBrush = value;
				DisposeDxBrush(ref sellBrushDX);
			}
		}

		[Browsable(false)]
		public string SellBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(sellBrush);
			}
			set
			{
				sellBrush = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "Label Color", Description = "Color del texto de volumen en los big trades", Order = 3, GroupName = "Colores")]
		public System.Windows.Media.Brush LabelColor
		{
			get
			{
				return labelBrush;
			}
			set
			{
				labelBrush = value;
				DisposeDxBrush(ref labelBrushDX);
			}
		}

		[Browsable(false)]
		public string LabelColorSerializable
		{
			get
			{
				return Serialize.BrushToString(labelBrush);
			}
			set
			{
				labelBrush = Serialize.StringToBrush(value);
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Big Trades - Big trades en tiempo real + historico sin Tick Replay";
				Name = "Pulse Big Trades";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				minContractsThreshold = 100;
				maxCircleRadius = 25;
				minCircleRadius = 8;
				showLabelThreshold = 200;
				circleOpacity = 0.6f;
				circleStyle = PulseBigTradesCircleStyle.Filled;
				circleOutlineWidth = 1.5f;
				resetDaily = false;
				detectionMode = PulseBigTradesDetectionMode.SinglePrint;
				clusterMinContracts = 250;
				clusterWindowMs = 75;
				clusterPriceGroupingTicks = 1;
				buyBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				sellBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				labelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				return;
			}
			if (State == State.Configure)
			{
				if (BarsPeriod.BarsPeriodType != BarsPeriodType.Tick || BarsPeriod.Value != 1)
				{
					AddDataSeries(BarsPeriodType.Tick, 1);
				}
				return;
			}
			if (State == State.DataLoaded)
			{
				tickSize = ((Instrument != null) ? Instrument.MasterInstrument.TickSize : 0.25);
				isPrimaryTickSeries = BarsPeriod.BarsPeriodType == BarsPeriodType.Tick && BarsPeriod.Value == 1;
				InitializeCachePath();
				lock (tradesLock)
				{
					bigTrades.Clear();
					renderSnapshot.Clear();
					tradesVersion++;
					renderSnapshotVersion = -1L;
				}
				lock (cacheLock)
				{
					pendingCacheWrites.Clear();
				}
				lock (clusterLock)
				{
					realtimeClusterBuckets.Clear();
					historicalClusterBuckets.Clear();
					nextClusterPruneAtUtc = DateTime.MinValue;
					return;
				}
			}
			if (State == State.Realtime)
			{
				bool flag = detectionMode == PulseBigTradesDetectionMode.AggressiveCluster;
				if (!cacheLoaded && (historicalTicksSeen == 0 || flag))
				{
					if (flag && historicalTicksSeen > 0)
					{
						lock (tradesLock)
						{
							bigTrades.Clear();
							renderSnapshot.Clear();
							tradesVersion++;
							renderSnapshotVersion = -1L;
						}
					}
					LoadTradesFromCache();
				}
				RequestRenderRefresh();
			}
			else if (State == State.Terminated)
			{
				FlushCache(force: true);
				DisposeDxResources();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessHistoricalTickFromSeries(1);
			}
			else
			{
				if (BarsInProgress != 0)
				{
					return;
				}
				if (isPrimaryTickSeries)
				{
					ProcessHistoricalTickFromSeries(0);
				}
				if (CurrentBar >= 0)
				{
					if (resetDaily && State == State.Realtime && Bars.IsFirstBarOfSession)
					{
						HandleDailyReset();
					}
					RunMaintenance();
				}
			}
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			if (marketDataUpdate != null && marketDataUpdate.MarketDataType == MarketDataType.Last && State != State.Historical && Bars != null && CurrentBar >= 0)
			{
				double price = marketDataUpdate.Price;
				long volume = marketDataUpdate.Volume;
				TradeSide tickRuleSide = UpdateTickRuleState(price, ref lastRealtimePrice, ref lastRealtimeSide);
				TradeSide side = ClassifyRealtimeSide(price, marketDataUpdate.Bid, marketDataUpdate.Ask, tickRuleSide);
				DateTime dateTime = marketDataUpdate.Time;
				if (dateTime == DateTime.MinValue)
				{
					dateTime = Times[0][0];
				}
				ProcessTradeSignal(dateTime, price, volume, side, persistToCache: true, isRealtime: true);
				RequestRenderRefresh();
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (chartControl == null || chartScale == null || RenderTarget == null || ChartBars == null)
			{
				return;
			}
			EnsureDxResources();
			List<BigTrade> list;
			lock (tradesLock)
			{
				if (bigTrades.Count == 0)
				{
					return;
				}
				if (renderSnapshotVersion != tradesVersion)
				{
					renderSnapshot.Clear();
					renderSnapshot.AddRange(bigTrades);
					renderSnapshotVersion = tradesVersion;
				}
				list = renderSnapshot;
			}
			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			int num = 0;
			int activeDisplayThreshold = GetActiveDisplayThreshold();
			int num2 = int.MinValue;
			float num3 = 0f;
			for (int num4 = list.Count - 1; num4 >= 0; num4--)
			{
				BigTrade bigTrade = list[num4];
				if (bigTrade.Volume >= activeDisplayThreshold && bigTrade.BarIndex <= toIndex)
				{
					if (bigTrade.BarIndex < fromIndex)
					{
						break;
					}
					float num5;
					if (bigTrade.BarIndex == num2)
					{
						num5 = num3;
					}
					else
					{
						num5 = chartControl.GetXByBarIndex(ChartBars, bigTrade.BarIndex);
						num2 = bigTrade.BarIndex;
						num3 = num5;
					}
					float num6 = chartScale.GetYByValue(bigTrade.Price);
					if (!float.IsNaN(num6) && !float.IsInfinity(num6))
					{
						float num7 = CalculateCircleRadius(bigTrade.Volume);
						Vector2 center = new Vector2(num5, num6);
						Ellipse ellipse = new Ellipse(center, num7, num7);
						SharpDX.Direct2D1.SolidColorBrush brushForSide = GetBrushForSide(bigTrade.Side);
						if (brushForSide != null)
						{
							if (circleStyle == PulseBigTradesCircleStyle.OutlineOnly)
							{
								float strokeWidth = Math.Max(0.5f, Math.Min(circleOutlineWidth, num7));
								RenderTarget.DrawEllipse(ellipse, brushForSide, strokeWidth);
							}
							else
							{
								RenderTarget.FillEllipse(ellipse, brushForSide);
							}
							if (bigTrade.Volume >= showLabelThreshold && textFormat != null && labelBrushDX != null)
							{
								float num8 = Math.Max(num7 * 2f + 6f, 22f);
								RectangleF layoutRect = new RectangleF(num5 - num8 / 2f, num6 - 8f, num8, 16f);
								RenderTarget.DrawText(bigTrade.LabelText, textFormat, layoutRect, labelBrushDX);
							}
							num++;
							if (num >= 2500)
							{
								break;
							}
						}
					}
				}
			}
		}

		private void ProcessHistoricalTickFromSeries(int seriesIndex)
		{
			if (State == State.Historical && CurrentBars != null && CurrentBars.Length > seriesIndex && CurrentBars[seriesIndex] >= 0)
			{
				double price = Closes[seriesIndex][0];
				long volume = (long)Volumes[seriesIndex][0];
				DateTime time = Times[seriesIndex][0];
				historicalTicksSeen++;
				TradeSide side = UpdateTickRuleState(price, ref lastHistoricalPrice, ref lastHistoricalSide);
				ProcessTradeSignal(time, price, volume, side, persistToCache: false, isRealtime: false);
			}
		}

		private void ProcessTradeSignal(DateTime time, double price, long volume, TradeSide side, bool persistToCache, bool isRealtime)
		{
			if (volume <= 0)
			{
				return;
			}
			if (detectionMode == PulseBigTradesDetectionMode.SinglePrint)
			{
				if (volume >= minContractsThreshold)
				{
					AddBigTrade(time, price, volume, side, persistToCache);
				}
				return;
			}
			DateTime dateTime = ToUtcSafe(time);
			long key = BuildClusterKey(price, side);
			bool flag = false;
			long volume2 = 0L;
			double num = price;
			lock (clusterLock)
			{
				PruneClusterBucketsIfNeeded(dateTime, force: false);
				Dictionary<long, ClusterBucket> dictionary = (isRealtime ? realtimeClusterBuckets : historicalClusterBuckets);
				if (!dictionary.TryGetValue(key, out var value))
				{
					value = (dictionary[key] = new ClusterBucket());
				}
				else
				{
					long num2 = dateTime.Ticks - value.LastTimeUtc.Ticks;
					long num3 = (long)Math.Max(0, clusterWindowMs) * 10000L;
					bool num4 = num2 == 0;
					bool flag2 = num3 > 0 && num2 >= 0 && num2 <= num3;
					if (!num4 && !flag2)
					{
						value.TotalVolume = 0L;
						value.WeightedPriceSum = 0.0;
						value.Emitted = false;
					}
				}
				value.LastTimeUtc = dateTime;
				value.TotalVolume += volume;
				value.WeightedPriceSum += price * (double)volume;
				int clusterThreshold = GetClusterThreshold();
				if (!value.Emitted && value.TotalVolume >= clusterThreshold)
				{
					value.Emitted = true;
					flag = true;
					volume2 = value.TotalVolume;
					num = ((value.TotalVolume > 0) ? (value.WeightedPriceSum / (double)value.TotalVolume) : price);
				}
			}
			if (flag)
			{
				if (tickSize > 0.0)
				{
					num = Math.Round(num / tickSize) * tickSize;
				}
				AddBigTrade(time, num, volume2, side, persistToCache);
			}
		}

		private long BuildClusterKey(double price, TradeSide side)
		{
			long num = side switch
			{
				TradeSide.Sell => 2L, 
				TradeSide.Buy => 1L, 
				_ => 0L, 
			};
			long num2 = BuildClusterPriceBucket(price);
			return (((0x14650FB0739D0383L ^ num) * 1099511628211L) ^ num2) * 1099511628211L;
		}

		private long BuildClusterPriceBucket(double price)
		{
			int num = Math.Max(1, clusterPriceGroupingTicks);
			double num2 = ((tickSize > 0.0) ? tickSize : 0.25);
			double num3 = Math.Max(num2 * (double)num, num2);
			return (long)Math.Round(price / num3);
		}

		private int GetClusterThreshold()
		{
			return Math.Max(minContractsThreshold, Math.Max(10, clusterMinContracts));
		}

		private int GetActiveDisplayThreshold()
		{
			if (detectionMode == PulseBigTradesDetectionMode.AggressiveCluster)
			{
				return GetClusterThreshold();
			}
			return Math.Max(10, minContractsThreshold);
		}

		private void PruneClusterBucketsIfNeeded(DateTime nowUtc, bool force)
		{
			if (!force && nowUtc < nextClusterPruneAtUtc)
			{
				return;
			}
			long num = ((clusterWindowMs <= 0) ? 2500 : Math.Max(500L, (long)clusterWindowMs * 5L)) * 10000;
			if (!force)
			{
				nextClusterPruneAtUtc = nowUtc.AddMilliseconds(500.0);
			}
			else
			{
				nextClusterPruneAtUtc = nowUtc;
			}
			clusterPruneScratch.Clear();
			foreach (KeyValuePair<long, ClusterBucket> realtimeClusterBucket in realtimeClusterBuckets)
			{
				if (nowUtc.Ticks - realtimeClusterBucket.Value.LastTimeUtc.Ticks > num)
				{
					clusterPruneScratch.Add(realtimeClusterBucket.Key);
				}
			}
			for (int i = 0; i < clusterPruneScratch.Count; i++)
			{
				realtimeClusterBuckets.Remove(clusterPruneScratch[i]);
			}
			clusterPruneScratch.Clear();
			foreach (KeyValuePair<long, ClusterBucket> historicalClusterBucket in historicalClusterBuckets)
			{
				if (nowUtc.Ticks - historicalClusterBucket.Value.LastTimeUtc.Ticks > num)
				{
					clusterPruneScratch.Add(historicalClusterBucket.Key);
				}
			}
			for (int j = 0; j < clusterPruneScratch.Count; j++)
			{
				historicalClusterBuckets.Remove(clusterPruneScratch[j]);
			}
			clusterPruneScratch.Clear();
		}

		private static DateTime ToUtcSafe(DateTime value)
		{
			if (value == DateTime.MinValue)
			{
				return DateTime.UtcNow;
			}
			if (value.Kind == DateTimeKind.Utc)
			{
				return value;
			}
			if (value.Kind == DateTimeKind.Unspecified)
			{
				return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
			}
			return value.ToUniversalTime();
		}

		private void AddBigTrade(DateTime time, double price, long volume, TradeSide side, bool persistToCache)
		{
			int num = ResolvePrimaryBarIndex(time);
			if (num >= 0)
			{
				BigTrade bigTrade = new BigTrade
				{
					Price = price,
					Volume = volume,
					Time = time,
					Side = side,
					BarIndex = num,
					LabelText = volume.ToString("N0", CultureInfo.InvariantCulture)
				};
				lock (tradesLock)
				{
					bigTrades.Add(bigTrade);
					TrimTradesUnsafe();
					tradesVersion++;
				}
				if (persistToCache)
				{
					QueueTradeForCache(bigTrade);
				}
			}
		}

		private int ResolvePrimaryBarIndex(DateTime time)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null)
			{
				return -1;
			}
			int bar = BarsArray[0].GetBar(time);
			if (bar >= 0)
			{
				return bar;
			}
			if (CurrentBars != null && CurrentBars.Length != 0)
			{
				return CurrentBars[0];
			}
			return -1;
		}

		private TradeSide ClassifyRealtimeSide(double price, double bid, double ask, TradeSide tickRuleSide)
		{
			if (bid > 0.0 && ask > 0.0 && ask >= bid)
			{
				double num = Math.Max(tickSize * 0.25, 1E-08);
				if (price >= ask - num)
				{
					return TradeSide.Buy;
				}
				if (price <= bid + num)
				{
					return TradeSide.Sell;
				}
			}
			return tickRuleSide;
		}

		private TradeSide UpdateTickRuleState(double price, ref double lastPrice, ref TradeSide lastSide)
		{
			TradeSide tradeSide = TradeSide.Unknown;
			if (!double.IsNaN(lastPrice))
			{
				tradeSide = ((price > lastPrice) ? TradeSide.Buy : ((!(price < lastPrice)) ? lastSide : TradeSide.Sell));
			}
			lastPrice = price;
			if (tradeSide != TradeSide.Unknown)
			{
				lastSide = tradeSide;
			}
			return tradeSide;
		}

		private void HandleDailyReset()
		{
			DateTime date = Times[0][0].Date;
			if (sessionDate == date)
			{
				return;
			}
			sessionDate = date;
			lock (tradesLock)
			{
				bigTrades.Clear();
				renderSnapshot.Clear();
				tradesVersion++;
				renderSnapshotVersion = -1L;
			}
			lock (clusterLock)
			{
				realtimeClusterBuckets.Clear();
				historicalClusterBuckets.Clear();
				nextClusterPruneAtUtc = DateTime.MinValue;
			}
		}

		private void RunMaintenance()
		{
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < nextMaintenanceAtUtc)
			{
				return;
			}
			nextMaintenanceAtUtc = utcNow.AddMilliseconds(750.0);
			lock (tradesLock)
			{
				int count = bigTrades.Count;
				TrimTradesUnsafe();
				if (bigTrades.Count != count)
				{
					tradesVersion++;
				}
			}
			lock (clusterLock)
			{
				PruneClusterBucketsIfNeeded(utcNow, force: false);
			}
			FlushCache(force: false);
		}

		private void TrimTradesUnsafe()
		{
			if (bigTrades.Count > 20000)
			{
				int count = bigTrades.Count - 20000;
				bigTrades.RemoveRange(0, count);
			}
		}

		private float CalculateCircleRadius(long volume)
		{
			double num = Math.Log10((double)volume / (double)minContractsThreshold + 1.0);
			double num2 = Math.Log10(10.0);
			double num3 = Math.Min(num / num2, 1.0);
			return (float)((double)minCircleRadius + (double)(maxCircleRadius - minCircleRadius) * num3);
		}

		private void EnsureDxResources()
		{
			if (RenderTarget != null)
			{
				if (buyBrushDX == null)
				{
					buyBrushDX = CreateSolidDxBrush(buyBrush, circleOpacity, System.Windows.Media.Color.FromRgb(107, 111, 204));
				}
				if (sellBrushDX == null)
				{
					sellBrushDX = CreateSolidDxBrush(sellBrush, circleOpacity, System.Windows.Media.Color.FromRgb(74, 74, 74));
				}
				if (unknownBrushDX == null)
				{
					unknownBrushDX = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.7216f, 0.7373f, 0.7765f, Math.Max(0.12f, circleOpacity * 0.9f)));
				}
				if (labelBrushDX == null)
				{
					labelBrushDX = CreateSolidDxBrush(labelBrush, 1f, System.Windows.Media.Color.FromRgb(45, 45, 45));
				}
				if (textFormat == null)
				{
					textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, 9f)
					{
						TextAlignment = TextAlignment.Center,
						ParagraphAlignment = ParagraphAlignment.Center
					};
				}
			}
		}

		private SharpDX.Direct2D1.SolidColorBrush CreateSolidDxBrush(System.Windows.Media.Brush sourceBrush, float opacity, System.Windows.Media.Color fallbackColor)
		{
			System.Windows.Media.Color color = ((sourceBrush is System.Windows.Media.SolidColorBrush solidColorBrush) ? solidColorBrush.Color : fallbackColor);
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, Math.Max(0.1f, opacity)));
		}

		private SharpDX.Direct2D1.SolidColorBrush GetBrushForSide(TradeSide side)
		{
			return side switch
			{
				TradeSide.Buy => buyBrushDX, 
				TradeSide.Sell => sellBrushDX, 
				_ => unknownBrushDX, 
			};
		}

		private void DisposeDxResources()
		{
			DisposeDxBrush(ref buyBrushDX);
			DisposeDxBrush(ref sellBrushDX);
			DisposeDxBrush(ref unknownBrushDX);
			DisposeDxBrush(ref labelBrushDX);
			if (textFormat != null)
			{
				textFormat.Dispose();
				textFormat = null;
			}
		}

		private static void DisposeDxBrush(ref SharpDX.Direct2D1.SolidColorBrush brush)
		{
			if (brush != null)
			{
				brush.Dispose();
				brush = null;
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}

		private void RequestRenderRefresh()
		{
			if (ChartControl == null)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < nextUiRefreshAtUtc)
			{
				return;
			}
			nextUiRefreshAtUtc = utcNow.AddMilliseconds(75.0);
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				if (ChartControl != null)
				{
					ChartControl.InvalidateVisual();
				}
			});
		}

		private void InitializeCachePath()
		{
			try
			{
				string text = Path.Combine(Globals.UserDataDir, "Pulse", "BigTradesCache");
				Directory.CreateDirectory(text);
				string value = ((Instrument != null) ? Instrument.FullName : "UnknownInstrument");
				cacheFilePath = Path.Combine(text, MakeSafeFileName(value) + ".csv");
			}
			catch (Exception ex)
			{
				cacheFilePath = string.Empty;
				Print("PulseBigTrades: cache path error - " + ex.Message);
			}
		}

		private string MakeSafeFileName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "UnknownInstrument";
			}
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				value = value.Replace(oldChar, '_');
			}
			return value.Replace(' ', '_');
		}

		private void QueueTradeForCache(BigTrade trade)
		{
			if (string.IsNullOrEmpty(cacheFilePath) || trade == null)
			{
				return;
			}
			lock (cacheLock)
			{
				pendingCacheWrites.Add(trade);
			}
		}

		private void FlushCache(bool force)
		{
			if (string.IsNullOrEmpty(cacheFilePath))
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (!force && utcNow < nextCacheFlushAtUtc)
			{
				return;
			}
			List<BigTrade> list = null;
			lock (cacheLock)
			{
				if (pendingCacheWrites.Count > 0)
				{
					list = new List<BigTrade>(pendingCacheWrites);
					pendingCacheWrites.Clear();
				}
			}
			nextCacheFlushAtUtc = utcNow.AddMilliseconds(2000.0);
			if (list == null || list.Count == 0)
			{
				return;
			}
			try
			{
				using StreamWriter streamWriter = new StreamWriter(cacheFilePath, append: true);
				for (int i = 0; i < list.Count; i++)
				{
					BigTrade trade = list[i];
					streamWriter.WriteLine(BuildCacheLine(trade));
				}
			}
			catch (Exception)
			{
			}
		}

		private void LoadTradesFromCache()
		{
			cacheLoaded = true;
			if (string.IsNullOrEmpty(cacheFilePath) || !File.Exists(cacheFilePath))
			{
				return;
			}
			DateTime dateTime = DateTime.Now.AddDays(-30.0);
			List<BigTrade> list = new List<BigTrade>(1024);
			List<string> list2 = new List<string>(2048);
			int num = 0;
			try
			{
				foreach (string item in File.ReadLines(cacheFilePath))
				{
					if (!TryParseCacheLine(item, out var trade))
					{
						continue;
					}
					num++;
					if (!(trade.Time < dateTime))
					{
						int num2 = ResolvePrimaryBarIndex(trade.Time);
						if (num2 >= 0)
						{
							trade.BarIndex = num2;
							list.Add(trade);
							list2.Add(BuildCacheLine(trade));
						}
					}
				}
				if (list.Count > 0)
				{
					lock (tradesLock)
					{
						bigTrades.AddRange(list);
						TrimTradesUnsafe();
						tradesVersion++;
					}
					RequestRenderRefresh();
				}
				if (num > 0 && list2.Count < num)
				{
					lock (cacheLock)
					{
						File.WriteAllLines(cacheFilePath, list2);
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Print("PulseBigTrades: cache load error - " + ex.Message);
			}
		}

		private string BuildCacheLine(BigTrade trade)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", trade.Time.ToBinary(), trade.Price, trade.Volume, (int)trade.Side);
		}

		private bool TryParseCacheLine(string line, out BigTrade trade)
		{
			trade = null;
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}
			string[] array = line.Split(',');
			if (array.Length < 4)
			{
				return false;
			}
			if (!long.TryParse(array[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return false;
			}
			if (!double.TryParse(array[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result2))
			{
				return false;
			}
			if (!long.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result3))
			{
				return false;
			}
			if (!int.TryParse(array[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result4))
			{
				return false;
			}
			TradeSide side = TradeSide.Unknown;
			if (result4 > 0)
			{
				side = TradeSide.Buy;
			}
			else if (result4 < 0)
			{
				side = TradeSide.Sell;
			}
			trade = new BigTrade
			{
				Time = DateTime.FromBinary(result),
				Price = result2,
				Volume = result3,
				Side = side,
				BarIndex = -1,
				LabelText = result3.ToString("N0", CultureInfo.InvariantCulture)
			};
			return true;
		}
	}
}
