using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

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

	private Brush buyBrush = (Brush)(object)Brushes.BlueViolet;

	private Brush sellBrush = (Brush)(object)Brushes.White;

	private Brush labelBrush = (Brush)(object)Brushes.White;

	private SolidColorBrush buyBrushDX;

	private SolidColorBrush sellBrushDX;

	private SolidColorBrush unknownBrushDX;

	private SolidColorBrush labelBrushDX;

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
	public Brush BuyBrush
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
	public Brush SellBrush
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
	public Brush LabelColor
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

	public PulseBigTrades()
	{
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Invalid comparison between Unknown and I4
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Invalid comparison between Unknown and I4
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Invalid comparison between Unknown and I4
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Pulse Big Trades - Big trades en tiempo real + historico sin Tick Replay";
			((NinjaScriptBase)this).Name = "Pulse Big Trades";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = false;
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
			buyBrush = (Brush)(object)Brushes.BlueViolet;
			sellBrush = (Brush)(object)Brushes.White;
			labelBrush = (Brush)(object)Brushes.White;
			return;
		}
		if ((int)((NinjaScript)this).State == 2)
		{
			if ((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 0 || ((NinjaScriptBase)this).BarsPeriod.Value != 1)
			{
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
			}
			return;
		}
		if ((int)((NinjaScript)this).State == 4)
		{
			tickSize = ((((NinjaScriptBase)this).Instrument != null) ? ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize : 0.25);
			isPrimaryTickSeries = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 0 && ((NinjaScriptBase)this).BarsPeriod.Value == 1;
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
		if ((int)((NinjaScript)this).State == 7)
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
		else if ((int)((NinjaScript)this).State == 8)
		{
			FlushCache(force: true);
			DisposeDxResources();
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			ProcessHistoricalTickFromSeries(1);
		}
		else
		{
			if (((NinjaScriptBase)this).BarsInProgress != 0)
			{
				return;
			}
			if (isPrimaryTickSeries)
			{
				ProcessHistoricalTickFromSeries(0);
			}
			if (((NinjaScriptBase)this).CurrentBar >= 0)
			{
				if (resetDaily && (int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
				{
					HandleDailyReset();
				}
				RunMaintenance();
			}
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		if (marketDataUpdate != null && (int)marketDataUpdate.MarketDataType == 2 && (int)((NinjaScript)this).State != 5 && ((NinjaScriptBase)this).Bars != null && ((NinjaScriptBase)this).CurrentBar >= 0)
		{
			double price = marketDataUpdate.Price;
			long volume = marketDataUpdate.Volume;
			TradeSide tickRuleSide = UpdateTickRuleState(price, ref lastRealtimePrice, ref lastRealtimeSide);
			TradeSide side = ClassifyRealtimeSide(price, marketDataUpdate.Bid, marketDataUpdate.Ask, tickRuleSide);
			DateTime dateTime = marketDataUpdate.Time;
			if (dateTime == DateTime.MinValue)
			{
				dateTime = ((NinjaScriptBase)this).Times[0][0];
			}
			ProcessTradeSignal(dateTime, price, volume, side, persistToCache: true, isRealtime: true);
			RequestRenderRefresh();
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (chartControl == null || chartScale == null || ((IndicatorRenderBase)this).RenderTarget == null || ((IndicatorRenderBase)this).ChartBars == null)
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
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		int num = 0;
		int activeDisplayThreshold = GetActiveDisplayThreshold();
		int num2 = int.MinValue;
		float num3 = 0f;
		Vector2 val = default(Vector2);
		Ellipse val2 = default(Ellipse);
		RectangleF val3 = default(RectangleF);
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
					num5 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, bigTrade.BarIndex);
					num2 = bigTrade.BarIndex;
					num3 = num5;
				}
				float num6 = chartScale.GetYByValue(bigTrade.Price);
				if (!float.IsNaN(num6) && !float.IsInfinity(num6))
				{
					float num7 = CalculateCircleRadius(bigTrade.Volume);
					((Vector2)(ref val))._002Ector(num5, num6);
					((Ellipse)(ref val2))._002Ector(val, num7, num7);
					SolidColorBrush brushForSide = GetBrushForSide(bigTrade.Side);
					if (brushForSide != null)
					{
						if (circleStyle == PulseBigTradesCircleStyle.OutlineOnly)
						{
							float num8 = Math.Max(0.5f, Math.Min(circleOutlineWidth, num7));
							((IndicatorRenderBase)this).RenderTarget.DrawEllipse(val2, (Brush)(object)brushForSide, num8);
						}
						else
						{
							((IndicatorRenderBase)this).RenderTarget.FillEllipse(val2, (Brush)(object)brushForSide);
						}
						if (bigTrade.Volume >= showLabelThreshold && textFormat != null && labelBrushDX != null)
						{
							float num9 = Math.Max(num7 * 2f + 6f, 22f);
							((RectangleF)(ref val3))._002Ector(num5 - num9 / 2f, num6 - 8f, num9, 16f);
							((IndicatorRenderBase)this).RenderTarget.DrawText(bigTrade.LabelText, textFormat, val3, (Brush)(object)labelBrushDX);
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 5 && ((NinjaScriptBase)this).CurrentBars != null && ((NinjaScriptBase)this).CurrentBars.Length > seriesIndex && ((NinjaScriptBase)this).CurrentBars[seriesIndex] >= 0)
		{
			double price = ((NinjaScriptBase)this).Closes[seriesIndex][0];
			long volume = (long)((NinjaScriptBase)this).Volumes[seriesIndex][0];
			DateTime time = ((NinjaScriptBase)this).Times[seriesIndex][0];
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
		if (((NinjaScriptBase)this).BarsArray == null || ((NinjaScriptBase)this).BarsArray.Length == 0 || ((NinjaScriptBase)this).BarsArray[0] == null)
		{
			return -1;
		}
		int bar = ((NinjaScriptBase)this).BarsArray[0].GetBar(time);
		if (bar >= 0)
		{
			return bar;
		}
		if (((NinjaScriptBase)this).CurrentBars != null && ((NinjaScriptBase)this).CurrentBars.Length != 0)
		{
			return ((NinjaScriptBase)this).CurrentBars[0];
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
		DateTime date = ((NinjaScriptBase)this).Times[0][0].Date;
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
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget != null)
		{
			if (buyBrushDX == null)
			{
				buyBrushDX = CreateSolidDxBrush(buyBrush, circleOpacity, Colors.BlueViolet);
			}
			if (sellBrushDX == null)
			{
				sellBrushDX = CreateSolidDxBrush(sellBrush, circleOpacity, Colors.White);
			}
			if (unknownBrushDX == null)
			{
				unknownBrushDX = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(0.6f, 0.6f, 0.6f, Math.Max(0.12f, circleOpacity * 0.9f)));
			}
			if (labelBrushDX == null)
			{
				labelBrushDX = CreateSolidDxBrush(labelBrush, 1f, Colors.White);
			}
			if (textFormat == null)
			{
				textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", (FontWeight)700, (FontStyle)0, 9f)
				{
					TextAlignment = (TextAlignment)2,
					ParagraphAlignment = (ParagraphAlignment)2
				};
			}
		}
	}

	private SolidColorBrush CreateSolidDxBrush(Brush sourceBrush, float opacity, Color fallbackColor)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		SolidColorBrush val = (SolidColorBrush)(object)((sourceBrush is SolidColorBrush) ? sourceBrush : null);
		Color val2 = ((val != null) ? val.Color : fallbackColor);
		return new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref val2)).R / 255f, (float)(int)((Color)(ref val2)).G / 255f, (float)(int)((Color)(ref val2)).B / 255f, Math.Max(0.1f, opacity)));
	}

	private SolidColorBrush GetBrushForSide(TradeSide side)
	{
		return (SolidColorBrush)(side switch
		{
			TradeSide.Buy => buyBrushDX, 
			TradeSide.Sell => sellBrushDX, 
			_ => unknownBrushDX, 
		});
	}

	private void DisposeDxResources()
	{
		DisposeDxBrush(ref buyBrushDX);
		DisposeDxBrush(ref sellBrushDX);
		DisposeDxBrush(ref unknownBrushDX);
		DisposeDxBrush(ref labelBrushDX);
		if (textFormat != null)
		{
			((DisposeBase)textFormat).Dispose();
			textFormat = null;
		}
	}

	private static void DisposeDxBrush(ref SolidColorBrush brush)
	{
		if (brush != null)
		{
			((DisposeBase)brush).Dispose();
			brush = null;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	private void RequestRenderRefresh()
	{
		if (((IndicatorRenderBase)this).ChartControl == null)
		{
			return;
		}
		DateTime utcNow = DateTime.UtcNow;
		if (utcNow < nextUiRefreshAtUtc)
		{
			return;
		}
		nextUiRefreshAtUtc = utcNow.AddMilliseconds(75.0);
		((DispatcherObject)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync((Action)delegate
		{
			if (((IndicatorRenderBase)this).ChartControl != null)
			{
				((IndicatorRenderBase)this).ChartControl.InvalidateVisual();
			}
		});
	}

	private void InitializeCachePath()
	{
		try
		{
			string text = Path.Combine(Globals.UserDataDir, "Pulse", "BigTradesCache");
			Directory.CreateDirectory(text);
			string value = ((((NinjaScriptBase)this).Instrument != null) ? ((NinjaScriptBase)this).Instrument.FullName : "UnknownInstrument");
			cacheFilePath = Path.Combine(text, MakeSafeFileName(value) + ".csv");
		}
		catch (Exception ex)
		{
			cacheFilePath = string.Empty;
			((NinjaScript)this).Print((object)("PulseBigTrades: cache path error - " + ex.Message));
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
			((NinjaScript)this).Print((object)("PulseBigTrades: cache load error - " + ex.Message));
		}
	}

	private string BuildCacheLine(BigTrade trade)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[4]
		{
			trade.Time.ToBinary(),
			trade.Price,
			trade.Volume,
			(int)trade.Side
		});
	}

	private bool TryParseCacheLine(string line, out BigTrade trade)
	{
		trade = null;
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}
		string[] array = line.Split(new char[1] { ',' });
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
