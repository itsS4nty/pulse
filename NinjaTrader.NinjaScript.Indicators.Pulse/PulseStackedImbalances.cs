using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

public class PulseStackedImbalances : Indicator
{
	private class StackedImbalance
	{
		public double TopPrice { get; set; }

		public double BottomPrice { get; set; }

		public int BarIndex { get; set; }

		public bool IsAskImbalance { get; set; }

		public int StackSize { get; set; }

		public bool IsTouched { get; set; }

		public int TouchedBarIndex { get; set; }

		public DateTime CreatedTime { get; set; }
	}

	private List<StackedImbalance> activeImbalances = new List<StackedImbalance>();

	private Dictionary<int, Dictionary<double, long>> barBidVolumes = new Dictionary<int, Dictionary<double, long>>();

	private Dictionary<int, Dictionary<double, long>> barAskVolumes = new Dictionary<int, Dictionary<double, long>>();

	private readonly HashSet<string> imbalanceKeys = new HashSet<string>(StringComparer.Ordinal);

	private int oldestBarToKeep;

	private bool isPrimaryTickSeries;

	private double lastHistoricalPrice = double.NaN;

	private int lastHistoricalDirection;

	private double lastRealtimePrice = double.NaN;

	private int lastRealtimeDirection;

	private double currentBid = double.NaN;

	private double currentAsk = double.NaN;

	private const bool DEBUG_MODE = false;

	private SolidColorBrush askOutlineBrush;

	private SolidColorBrush bidOutlineBrush;

	private SolidColorBrush askAreaBrush;

	private SolidColorBrush bidAreaBrush;

	private Color cachedAskColor = Colors.Transparent;

	private Color cachedBidColor = Colors.Transparent;

	private double cachedZoneOpacity = double.NaN;

	[NinjaScriptProperty]
	[Display(Name = "Ignore Zero Values", Order = 1, GroupName = "Settings")]
	public bool IgnoreZeroValues { get; set; }

	[NinjaScriptProperty]
	[Range(100, 1000)]
	[Display(Name = "Imbalance Ratio (%)", Order = 2, GroupName = "Settings")]
	public int ImbalanceRatio { get; set; }

	[NinjaScriptProperty]
	[Range(2, 10)]
	[Display(Name = "Imbalance Range (Levels)", Order = 3, GroupName = "Settings")]
	public int ImbalanceRange { get; set; }

	[NinjaScriptProperty]
	[Range(1, 1000)]
	[Display(Name = "Imbalance Volume (Min)", Order = 4, GroupName = "Settings")]
	public int ImbalanceVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Line Till Touch", Order = 5, GroupName = "Drawing")]
	public bool LineTillTouch { get; set; }

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Ask Imbalance Color", Order = 6, GroupName = "Drawing")]
	public Color AskImbalanceColor { get; set; }

	[Browsable(false)]
	public string AskImbalanceColorSerializable
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((object)AskImbalanceColor/*cast due to constrained. prefix*/).ToString();
		}
		set
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			AskImbalanceColor = (Color)ColorConverter.ConvertFromString(value);
		}
	}

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Bid Imbalance Color", Order = 7, GroupName = "Drawing")]
	public Color BidImbalanceColor { get; set; }

	[Browsable(false)]
	public string BidImbalanceColorSerializable
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((object)BidImbalanceColor/*cast due to constrained. prefix*/).ToString();
		}
		set
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			BidImbalanceColor = (Color)ColorConverter.ConvertFromString(value);
		}
	}

	[NinjaScriptProperty]
	[Range(1, 20)]
	[Display(Name = "Border Width", Order = 8, GroupName = "Drawing")]
	public int LineWidth { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Print Line For X Bars", Order = 9, GroupName = "Drawing")]
	public int PrintLineForXBars { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Days Look Back", Order = 10, GroupName = "Calculation")]
	public int DaysLookBack { get; set; }

	[NinjaScriptProperty]
	[Range(0.05, 1.0)]
	[Display(Name = "Opacidad Stacked Imbalances", Order = 11, GroupName = "Drawing")]
	public double StackedImbalanceOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Enable Historical Reconstruction", Order = 12, GroupName = "Settings")]
	public bool EnableHistoricalReconstruction { get; set; }

	public PulseStackedImbalances()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Invalid comparison between Unknown and I4
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Invalid comparison between Unknown and I4
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Pulse Stacked Imbalances - Detects consecutive bid/ask imbalances";
			((NinjaScriptBase)this).Name = "Pulse - Stacked Imbalances v2";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = false;
			IgnoreZeroValues = false;
			ImbalanceRatio = 300;
			ImbalanceRange = 3;
			ImbalanceVolume = 30;
			LineTillTouch = true;
			AskImbalanceColor = Color.FromRgb((byte)76, (byte)175, (byte)80);
			BidImbalanceColor = Color.FromRgb((byte)195, (byte)1, (byte)1);
			LineWidth = 2;
			PrintLineForXBars = 100;
			DaysLookBack = 1;
			StackedImbalanceOpacity = 0.3;
			EnableHistoricalReconstruction = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			isPrimaryTickSeries = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 0 && ((NinjaScriptBase)this).BarsPeriod.Value == 1;
			if (EnableHistoricalReconstruction && !isPrimaryTickSeries)
			{
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
			}
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			isPrimaryTickSeries = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 0 && ((NinjaScriptBase)this).BarsPeriod.Value == 1;
			oldestBarToKeep = Math.Max(0, ((NinjaScriptBase)this).CurrentBar - DaysLookBack * 390);
			((NinjaScript)this).Print((object)"======================================");
			((NinjaScript)this).Print((object)"Pulse Stacked Imbalances LOADED");
			((NinjaScript)this).Print((object)$"Ratio: {ImbalanceRatio}% | Range: {ImbalanceRange} levels | MinVol: {ImbalanceVolume}");
			((NinjaScript)this).Print((object)$"Days Lookback: {DaysLookBack} | Line Duration: {PrintLineForXBars} bars");
			((NinjaScript)this).Print((object)$"Historical Reconstruction: {EnableHistoricalReconstruction} | Opacity: {StackedImbalanceOpacity:P0}");
			((NinjaScript)this).Print((object)"======================================");
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeZoneBrushes();
		}
	}

	protected override void OnBarUpdate()
	{
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
			if (((NinjaScriptBase)this).CurrentBar < 2)
			{
				return;
			}
			if (((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				DetectStackedImbalances(((NinjaScriptBase)this).CurrentBar - 1);
			}
			oldestBarToKeep = Math.Max(0, ((NinjaScriptBase)this).CurrentBar - DaysLookBack * 390);
			if (((NinjaScriptBase)this).CurrentBar % 100 == 0)
			{
				CleanupOldData();
			}
			if (((NinjaScriptBase)this).IsFirstTickOfBar && ((NinjaScriptBase)this).CurrentBar % 5 == 0 && activeImbalances.Count > 0)
			{
				int num = activeImbalances.Count((StackedImbalance i) => i.IsAskImbalance && !i.IsTouched);
				int num2 = activeImbalances.Count((StackedImbalance i) => !i.IsAskImbalance && !i.IsTouched);
				((NinjaScript)this).Print((object)string.Format("[{0}] Active zones: {1} Ask | {2} Bid", ((NinjaScriptBase)this).Time[0].ToString("HH:mm:ss"), num, num2));
			}
			if (LineTillTouch)
			{
				CheckImbalancesTouched();
			}
			if (((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				UpdateActiveImbalanceZones();
			}
		}
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		if (e == null)
		{
			return;
		}
		if ((int)e.MarketDataType == 1)
		{
			currentBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			currentAsk = e.Price;
		}
		else
		{
			if ((int)e.MarketDataType != 2 || (int)((NinjaScript)this).State == 5 || ((NinjaScriptBase)this).CurrentBar < 0)
			{
				return;
			}
			long volume = e.Volume;
			if (volume > 0)
			{
				double price = NormalizePrice(e.Price);
				int num = ClassifyRealtimeDirection(price);
				if (num != 0)
				{
					AccumulateBarVolume(((NinjaScriptBase)this).CurrentBar, price, volume, num > 0, num < 0);
				}
			}
		}
	}

	private void ProcessHistoricalTickFromSeries(int seriesIndex)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		if (!EnableHistoricalReconstruction || (int)((NinjaScript)this).State != 5 || ((NinjaScriptBase)this).CurrentBars == null || ((NinjaScriptBase)this).CurrentBars.Length <= seriesIndex || ((NinjaScriptBase)this).CurrentBars[seriesIndex] < 0)
		{
			return;
		}
		double price = NormalizePrice(((NinjaScriptBase)this).Closes[seriesIndex][0]);
		long num = (long)((NinjaScriptBase)this).Volumes[seriesIndex][0];
		if (num <= 0)
		{
			return;
		}
		DateTime time = ((NinjaScriptBase)this).Times[seriesIndex][0];
		int num2 = ResolvePrimaryBarIndex(time);
		if (num2 >= 0)
		{
			int num3 = UpdateTickRuleDirection(price, ref lastHistoricalPrice, ref lastHistoricalDirection);
			if (num3 != 0)
			{
				AccumulateBarVolume(num2, price, num, num3 > 0, num3 < 0);
			}
		}
	}

	private void AccumulateBarVolume(int barIndex, double price, long volume, bool isAsk, bool isBid)
	{
		if (barIndex < 0 || volume <= 0)
		{
			return;
		}
		if (!barBidVolumes.TryGetValue(barIndex, out var value))
		{
			value = new Dictionary<double, long>();
			barBidVolumes[barIndex] = value;
		}
		if (!barAskVolumes.TryGetValue(barIndex, out var value2))
		{
			value2 = new Dictionary<double, long>();
			barAskVolumes[barIndex] = value2;
		}
		if (isBid)
		{
			if (!value.TryGetValue(price, out var value3))
			{
				value3 = 0L;
			}
			value[price] = value3 + volume;
		}
		else if (isAsk)
		{
			if (!value2.TryGetValue(price, out var value4))
			{
				value4 = 0L;
			}
			value2[price] = value4 + volume;
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

	private int ClassifyRealtimeDirection(double price)
	{
		double num = ((((NinjaScriptBase)this).TickSize > 0.0) ? (((NinjaScriptBase)this).TickSize * 0.25) : 1E-08);
		int result = UpdateTickRuleDirection(price, ref lastRealtimePrice, ref lastRealtimeDirection);
		int num2 = 0;
		if (!double.IsNaN(currentAsk) && currentAsk > 0.0 && price >= currentAsk - num)
		{
			return 1;
		}
		if (!double.IsNaN(currentBid) && currentBid > 0.0 && price <= currentBid + num)
		{
			return -1;
		}
		return result;
	}

	private int UpdateTickRuleDirection(double price, ref double lastPrice, ref int lastDirection)
	{
		int num = 0;
		if (!double.IsNaN(lastPrice))
		{
			num = ((price > lastPrice) ? 1 : ((!(price < lastPrice)) ? lastDirection : (-1)));
		}
		lastPrice = price;
		if (num != 0)
		{
			lastDirection = num;
		}
		return num;
	}

	private double NormalizePrice(double price)
	{
		if (((NinjaScriptBase)this).TickSize <= 0.0)
		{
			return price;
		}
		return Math.Round(price / ((NinjaScriptBase)this).TickSize) * ((NinjaScriptBase)this).TickSize;
	}

	private void DetectStackedImbalances(int barIndex)
	{
		if (!barBidVolumes.ContainsKey(barIndex) && !barAskVolumes.ContainsKey(barIndex))
		{
			return;
		}
		if (!barBidVolumes.ContainsKey(barIndex))
		{
			barBidVolumes[barIndex] = new Dictionary<double, long>();
		}
		if (!barAskVolumes.ContainsKey(barIndex))
		{
			barAskVolumes[barIndex] = new Dictionary<double, long>();
		}
		Dictionary<double, long> dictionary = barBidVolumes[barIndex];
		Dictionary<double, long> dictionary2 = barAskVolumes[barIndex];
		if (dictionary.Count == 0 && dictionary2.Count == 0)
		{
			return;
		}
		List<double> list = (from p in dictionary.Keys.Union(dictionary2.Keys)
			orderby p
			select p).ToList();
		List<double> list2 = new List<double>();
		List<double> list3 = new List<double>();
		foreach (double item in list)
		{
			long num = (dictionary.ContainsKey(item) ? dictionary[item] : 0);
			long num2 = (dictionary2.ContainsKey(item) ? dictionary2[item] : 0);
			if (IgnoreZeroValues && (num == 0L || num2 == 0L))
			{
				if (list2.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list2, barIndex, isAsk: true);
				}
				if (list3.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list3, barIndex, isAsk: false);
				}
				list2.Clear();
				list3.Clear();
				continue;
			}
			if (Math.Max(num, num2) < ImbalanceVolume)
			{
				if (list2.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list2, barIndex, isAsk: true);
				}
				if (list3.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list3, barIndex, isAsk: false);
				}
				list2.Clear();
				list3.Clear();
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			if (num2 > 0 && num == 0L)
			{
				flag = true;
			}
			else if (num > 0 && num2 == 0L)
			{
				flag2 = true;
			}
			else if (num > 0 && num2 > 0)
			{
				double num3 = (double)num2 * 100.0 / (double)num;
				double num4 = (double)num * 100.0 / (double)num2;
				if (num3 >= (double)ImbalanceRatio)
				{
					flag = true;
				}
				else if (num4 >= (double)ImbalanceRatio)
				{
					flag2 = true;
				}
			}
			if (flag)
			{
				list2.Add(item);
				if (list3.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list3, barIndex, isAsk: false);
				}
				list3.Clear();
				continue;
			}
			if (flag2)
			{
				list3.Add(item);
				if (list2.Count >= ImbalanceRange)
				{
					CreateStackedImbalance(list2, barIndex, isAsk: true);
				}
				list2.Clear();
				continue;
			}
			if (list2.Count >= ImbalanceRange)
			{
				CreateStackedImbalance(list2, barIndex, isAsk: true);
			}
			if (list3.Count >= ImbalanceRange)
			{
				CreateStackedImbalance(list3, barIndex, isAsk: false);
			}
			list2.Clear();
			list3.Clear();
		}
		if (list2.Count >= ImbalanceRange)
		{
			CreateStackedImbalance(list2, barIndex, isAsk: true);
		}
		if (list3.Count >= ImbalanceRange)
		{
			CreateStackedImbalance(list3, barIndex, isAsk: false);
		}
	}

	private void CreateStackedImbalance(List<double> priceStack, int barIndex, bool isAsk)
	{
		if (priceStack.Count >= ImbalanceRange)
		{
			double topPrice = priceStack.Max();
			double bottomPrice = priceStack.Min();
			string item = BuildImbalanceKey(barIndex, isAsk, topPrice, bottomPrice);
			if (imbalanceKeys.Add(item))
			{
				StackedImbalance stackedImbalance = new StackedImbalance
				{
					TopPrice = topPrice,
					BottomPrice = bottomPrice,
					BarIndex = barIndex,
					IsAskImbalance = isAsk,
					StackSize = priceStack.Count,
					IsTouched = false,
					CreatedTime = ((NinjaScriptBase)this).Time[((NinjaScriptBase)this).CurrentBar - barIndex]
				};
				activeImbalances.Add(stackedImbalance);
				DrawImbalanceZone(stackedImbalance, ((NinjaScriptBase)this).CurrentBar);
			}
		}
	}

	private void UpdateActiveImbalanceZones()
	{
		List<StackedImbalance> list = activeImbalances.Where((StackedImbalance i) => !i.IsTouched).ToList();
		foreach (StackedImbalance item in list)
		{
			int endBarIndex = (LineTillTouch ? ((NinjaScriptBase)this).CurrentBar : Math.Min(((NinjaScriptBase)this).CurrentBar, item.BarIndex + PrintLineForXBars));
			DrawImbalanceZone(item, endBarIndex);
		}
	}

	private void CheckImbalancesTouched()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		foreach (StackedImbalance item in activeImbalances.Where((StackedImbalance i) => !i.IsTouched).ToList())
		{
			if (num >= item.BottomPrice && num2 <= item.TopPrice)
			{
				item.IsTouched = true;
				item.TouchedBarIndex = ((NinjaScriptBase)this).CurrentBar;
				DrawImbalanceZone(item, item.TouchedBarIndex);
			}
		}
	}

	private void DrawImbalanceZone(StackedImbalance imbalance, int endBarIndex)
	{
		if (imbalance == null)
		{
			return;
		}
		EnsureZoneBrushes();
		if (endBarIndex < imbalance.BarIndex)
		{
			endBarIndex = imbalance.BarIndex;
		}
		int num = ((NinjaScriptBase)this).CurrentBar - imbalance.BarIndex;
		int num2 = ((NinjaScriptBase)this).CurrentBar - endBarIndex;
		if (num >= 0)
		{
			if (num2 < 0)
			{
				num2 = 0;
			}
			double num3 = ((((NinjaScriptBase)this).TickSize > 0.0) ? (((NinjaScriptBase)this).TickSize * 0.5) : 0.0);
			double startY = imbalance.TopPrice + num3;
			double endY = imbalance.BottomPrice - num3;
			Brush brush = (Brush)(object)(imbalance.IsAskImbalance ? askOutlineBrush : bidOutlineBrush);
			Brush areaBrush = (Brush)(object)(imbalance.IsAskImbalance ? askAreaBrush : bidAreaBrush);
			int areaOpacity = Math.Max(5, Math.Min(90, (int)Math.Round(StackedImbalanceOpacity * 100.0)));
			string imbalanceTag = GetImbalanceTag(imbalance);
			Draw.Rectangle((NinjaScriptBase)(object)this, imbalanceTag, isAutoScale: true, num, startY, num2, endY, brush, areaBrush, areaOpacity);
			int width = Math.Max(1, LineWidth);
			Draw.Line((NinjaScriptBase)(object)this, imbalanceTag + "_top", isAutoScale: true, num, imbalance.TopPrice, num2, imbalance.TopPrice, brush, (DashStyleHelper)0, width);
			Draw.Line((NinjaScriptBase)(object)this, imbalanceTag + "_bottom", isAutoScale: true, num, imbalance.BottomPrice, num2, imbalance.BottomPrice, brush, (DashStyleHelper)0, width);
		}
	}

	private void EnsureZoneBrushes()
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (askOutlineBrush == null || bidOutlineBrush == null || askAreaBrush == null || bidAreaBrush == null || cachedAskColor != AskImbalanceColor || cachedBidColor != BidImbalanceColor || Math.Abs(cachedZoneOpacity - StackedImbalanceOpacity) > 0.0001)
		{
			DisposeZoneBrushes();
			cachedAskColor = AskImbalanceColor;
			cachedBidColor = BidImbalanceColor;
			cachedZoneOpacity = StackedImbalanceOpacity;
			askOutlineBrush = CreateZoneBrush(AskImbalanceColor, Math.Min(1.0, StackedImbalanceOpacity + 0.2), 0.15);
			bidOutlineBrush = CreateZoneBrush(BidImbalanceColor, Math.Min(1.0, StackedImbalanceOpacity + 0.2), 0.15);
			askAreaBrush = CreateZoneBrush(AskImbalanceColor, StackedImbalanceOpacity, 0.08);
			bidAreaBrush = CreateZoneBrush(BidImbalanceColor, StackedImbalanceOpacity, 0.08);
		}
	}

	private SolidColorBrush CreateZoneBrush(Color baseColor, double opacity, double minOpacity)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		double num = Math.Max(minOpacity, Math.Min(1.0, opacity));
		SolidColorBrush val = new SolidColorBrush(Color.FromArgb((byte)Math.Max(10, Math.Min(255, (int)Math.Round(255.0 * num))), ((Color)(ref baseColor)).R, ((Color)(ref baseColor)).G, ((Color)(ref baseColor)).B));
		((Freezable)val).Freeze();
		return val;
	}

	private void DisposeZoneBrushes()
	{
		askOutlineBrush = null;
		bidOutlineBrush = null;
		askAreaBrush = null;
		bidAreaBrush = null;
	}

	private string BuildImbalanceKey(int barIndex, bool isAsk, double topPrice, double bottomPrice)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2:0.########}|{3:0.########}", new object[4]
		{
			barIndex,
			isAsk ? "A" : "B",
			topPrice,
			bottomPrice
		});
	}

	private string GetImbalanceTag(StackedImbalance imbalance)
	{
		return string.Format(CultureInfo.InvariantCulture, "StackZone_{0}_{1}_{2:0.#####}_{3:0.#####}", new object[4]
		{
			imbalance.BarIndex,
			imbalance.IsAskImbalance ? "A" : "B",
			imbalance.BottomPrice,
			imbalance.TopPrice
		});
	}

	private void CleanupOldData()
	{
		foreach (int item in barBidVolumes.Keys.Where((int b) => b < oldestBarToKeep).ToList())
		{
			barBidVolumes.Remove(item);
			barAskVolumes.Remove(item);
		}
		List<StackedImbalance> list = activeImbalances.Where((StackedImbalance i) => i.IsTouched && i.BarIndex < oldestBarToKeep).ToList();
		int count = activeImbalances.Count;
		foreach (StackedImbalance item2 in list)
		{
			string imbalanceTag = GetImbalanceTag(item2);
			((IndicatorRenderBase)this).RemoveDrawObject(imbalanceTag);
			((IndicatorRenderBase)this).RemoveDrawObject(imbalanceTag + "_top");
			((IndicatorRenderBase)this).RemoveDrawObject(imbalanceTag + "_bottom");
			imbalanceKeys.Remove(BuildImbalanceKey(item2.BarIndex, item2.IsAskImbalance, item2.TopPrice, item2.BottomPrice));
		}
		activeImbalances.RemoveAll((StackedImbalance i) => i.IsTouched && i.BarIndex < oldestBarToKeep);
		int num = count - activeImbalances.Count;
	}
}
