using System;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.Indicators.Pulse;

namespace NinjaTrader.NinjaScript.Indicators;

public class Indicator : IndicatorRenderBase
{
	private WoodiesCCI[] cacheWoodiesCCI;

	private WoodiesPivots[] cacheWoodiesPivots;

	private WisemanAlligator[] cacheWisemanAlligator;

	private WisemanAwesomeOscillator[] cacheWisemanAwesomeOscillator;

	private WisemanFractal[] cacheWisemanFractal;

	private OrderFlowCumulativeDelta[] cacheOrderFlowCumulativeDelta;

	private OrderFlowMarketDepthMap[] cacheOrderFlowMarketDepthMap;

	private OrderFlowVWAP[] cacheOrderFlowVWAP;

	private OrderFlowTradeDetector[] cacheOrderFlowTradeDetector;

	private OrderFlowVolumeProfile[] cacheOrderFlowVP;

	private PulseAnchoredVolumeProfile[] cachePulseAnchoredVolumeProfile;

	private PulseAnchoredVWAP[] cachePulseAnchoredVWAP;

	private PulseBigTrades[] cachePulseBigTrades;

	private PulseCumulativeDelta[] cachePulseCumulativeDelta;

	private PulseDeltaProfile[] cachePulseDeltaProfile;

	private PulseDivergences[] cachePulseDivergences;

	private PulseFootprintPro[] cachePulseFootprintPro;

	private PulseHeikin[] cachePulseHeikin;

	private PulseDailyLevels[] cachePulseDailyLevels;

	private PulseStackedImbalances[] cachePulseStackedImbalances;

	private PulseTPO[] cachePulseTPO;

	private PulseVolumeProfileLite[] cachePulseVolumeProfileLite;

	private PulseVP[] cachePulseVP;

	private PulseVWAP[] cachePulseVWAP;

	private PulseWeeklyLevels[] cachePulseWeeklyLevels;

	public WoodiesCCI WoodiesCCI(int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return WoodiesCCI(((NinjaScriptBase)this).Input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesCCI WoodiesCCI(ISeries<double> input, int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		if (cacheWoodiesCCI != null)
		{
			for (int i = 0; i < cacheWoodiesCCI.Length; i++)
			{
				if (cacheWoodiesCCI[i] != null && cacheWoodiesCCI[i].ChopIndicatorWidth == chopIndicatorWidth && cacheWoodiesCCI[i].NeutralBars == neutralBars && cacheWoodiesCCI[i].Period == period && cacheWoodiesCCI[i].PeriodEma == periodEma && cacheWoodiesCCI[i].PeriodLinReg == periodLinReg && cacheWoodiesCCI[i].PeriodTurbo == periodTurbo && cacheWoodiesCCI[i].SideWinderLimit0 == sideWinderLimit0 && cacheWoodiesCCI[i].SideWinderLimit1 == sideWinderLimit1 && cacheWoodiesCCI[i].SideWinderWidth == sideWinderWidth && ((NinjaScriptBase)cacheWoodiesCCI[i]).EqualsInput(input))
				{
					return cacheWoodiesCCI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WoodiesCCI>(new WoodiesCCI
		{
			ChopIndicatorWidth = chopIndicatorWidth,
			NeutralBars = neutralBars,
			Period = period,
			PeriodEma = periodEma,
			PeriodLinReg = periodLinReg,
			PeriodTurbo = periodTurbo,
			SideWinderLimit0 = sideWinderLimit0,
			SideWinderLimit1 = sideWinderLimit1,
			SideWinderWidth = sideWinderWidth
		}, input, ref cacheWoodiesCCI);
	}

	public WoodiesPivots WoodiesPivots(HLCCalculationModeWoodie priorDayHlc, int width)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return WoodiesPivots(((NinjaScriptBase)this).Input, priorDayHlc, width);
	}

	public WoodiesPivots WoodiesPivots(ISeries<double> input, HLCCalculationModeWoodie priorDayHlc, int width)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (cacheWoodiesPivots != null)
		{
			for (int i = 0; i < cacheWoodiesPivots.Length; i++)
			{
				if (cacheWoodiesPivots[i] != null && cacheWoodiesPivots[i].PriorDayHlc == priorDayHlc && cacheWoodiesPivots[i].Width == width && ((NinjaScriptBase)cacheWoodiesPivots[i]).EqualsInput(input))
				{
					return cacheWoodiesPivots[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WoodiesPivots>(new WoodiesPivots
		{
			PriorDayHlc = priorDayHlc,
			Width = width
		}, input, ref cacheWoodiesPivots);
	}

	public WisemanAlligator WisemanAlligator(int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return WisemanAlligator(((NinjaScriptBase)this).Input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAlligator WisemanAlligator(ISeries<double> input, int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		if (cacheWisemanAlligator != null)
		{
			for (int i = 0; i < cacheWisemanAlligator.Length; i++)
			{
				if (cacheWisemanAlligator[i] != null && cacheWisemanAlligator[i].JawPeriod == jawPeriod && cacheWisemanAlligator[i].TeethPeriod == teethPeriod && cacheWisemanAlligator[i].LipsPeriod == lipsPeriod && cacheWisemanAlligator[i].JawOffset == jawOffset && cacheWisemanAlligator[i].TeethOffset == teethOffset && cacheWisemanAlligator[i].LipsOffset == lipsOffset && ((NinjaScriptBase)cacheWisemanAlligator[i]).EqualsInput(input))
				{
					return cacheWisemanAlligator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanAlligator>(new WisemanAlligator
		{
			JawPeriod = jawPeriod,
			TeethPeriod = teethPeriod,
			LipsPeriod = lipsPeriod,
			JawOffset = jawOffset,
			TeethOffset = teethOffset,
			LipsOffset = lipsOffset
		}, input, ref cacheWisemanAlligator);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator()
	{
		return WisemanAwesomeOscillator(((NinjaScriptBase)this).Input);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator(ISeries<double> input)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		if (cacheWisemanAwesomeOscillator != null)
		{
			for (int i = 0; i < cacheWisemanAwesomeOscillator.Length; i++)
			{
				if (cacheWisemanAwesomeOscillator[i] != null && ((NinjaScriptBase)cacheWisemanAwesomeOscillator[i]).EqualsInput(input))
				{
					return cacheWisemanAwesomeOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanAwesomeOscillator>(new WisemanAwesomeOscillator(), input, ref cacheWisemanAwesomeOscillator);
	}

	public WisemanFractal WisemanFractal(int strength, int triangleOffset)
	{
		return WisemanFractal(((NinjaScriptBase)this).Input, strength, triangleOffset);
	}

	public WisemanFractal WisemanFractal(ISeries<double> input, int strength, int triangleOffset)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		if (cacheWisemanFractal != null)
		{
			for (int i = 0; i < cacheWisemanFractal.Length; i++)
			{
				if (cacheWisemanFractal[i] != null && cacheWisemanFractal[i].Strength == strength && cacheWisemanFractal[i].TriangleOffset == triangleOffset && ((NinjaScriptBase)cacheWisemanFractal[i]).EqualsInput(input))
				{
					return cacheWisemanFractal[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanFractal>(new WisemanFractal
		{
			Strength = strength,
			TriangleOffset = triangleOffset
		}, input, ref cacheWisemanFractal);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return OrderFlowCumulativeDelta(((NinjaScriptBase)this).Input, deltaType, period, sizeFilter);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(ISeries<double> input, CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (cacheOrderFlowCumulativeDelta != null)
		{
			for (int i = 0; i < cacheOrderFlowCumulativeDelta.Length; i++)
			{
				if (cacheOrderFlowCumulativeDelta[i] != null && cacheOrderFlowCumulativeDelta[i].DeltaType == deltaType && cacheOrderFlowCumulativeDelta[i].Period == period && cacheOrderFlowCumulativeDelta[i].SizeFilter == sizeFilter && ((NinjaScriptBase)cacheOrderFlowCumulativeDelta[i]).EqualsInput(input))
				{
					return cacheOrderFlowCumulativeDelta[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowCumulativeDelta>(new OrderFlowCumulativeDelta
		{
			DeltaType = deltaType,
			Period = period,
			SizeFilter = sizeFilter
		}, input, ref cacheOrderFlowCumulativeDelta);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return OrderFlowMarketDepthMap(((NinjaScriptBase)this).Input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(ISeries<double> input, BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (cacheOrderFlowMarketDepthMap != null)
		{
			for (int i = 0; i < cacheOrderFlowMarketDepthMap.Length; i++)
			{
				if (cacheOrderFlowMarketDepthMap[i] != null && cacheOrderFlowMarketDepthMap[i].BaseRange == baseRange && cacheOrderFlowMarketDepthMap[i].MaxRange == maxRange && cacheOrderFlowMarketDepthMap[i].MinRange == minRange && cacheOrderFlowMarketDepthMap[i].OpacityDistribution == opacityDistribution && cacheOrderFlowMarketDepthMap[i].DepthMargin == depthMargin && cacheOrderFlowMarketDepthMap[i].ExtendLastKnown == extendLastKnown && cacheOrderFlowMarketDepthMap[i].ShowBidAskLine == showBidAskLine && ((NinjaScriptBase)cacheOrderFlowMarketDepthMap[i]).EqualsInput(input))
				{
					return cacheOrderFlowMarketDepthMap[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowMarketDepthMap>(new OrderFlowMarketDepthMap
		{
			BaseRange = baseRange,
			MaxRange = maxRange,
			MinRange = minRange,
			OpacityDistribution = opacityDistribution,
			DepthMargin = depthMargin,
			ExtendLastKnown = extendLastKnown,
			ShowBidAskLine = showBidAskLine
		}, input, ref cacheOrderFlowMarketDepthMap);
	}

	public OrderFlowVWAP OrderFlowVWAP(VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return OrderFlowVWAP(((NinjaScriptBase)this).Input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowVWAP OrderFlowVWAP(ISeries<double> input, VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (cacheOrderFlowVWAP != null)
		{
			for (int i = 0; i < cacheOrderFlowVWAP.Length; i++)
			{
				if (cacheOrderFlowVWAP[i] != null && cacheOrderFlowVWAP[i].Resolution == resolution && cacheOrderFlowVWAP[i].TradingHoursInstance == tradingHoursInstance && cacheOrderFlowVWAP[i].NumStandardDeviations == numStandardDeviations && cacheOrderFlowVWAP[i].SD1Multiplier == sD1Multiplier && cacheOrderFlowVWAP[i].SD2Multiplier == sD2Multiplier && cacheOrderFlowVWAP[i].SD3Multiplier == sD3Multiplier && ((NinjaScriptBase)cacheOrderFlowVWAP[i]).EqualsInput(input))
				{
					return cacheOrderFlowVWAP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowVWAP>(new OrderFlowVWAP
		{
			Resolution = resolution,
			TradingHoursInstance = tradingHoursInstance,
			NumStandardDeviations = numStandardDeviations,
			SD1Multiplier = sD1Multiplier,
			SD2Multiplier = sD2Multiplier,
			SD3Multiplier = sD3Multiplier
		}, input, ref cacheOrderFlowVWAP);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return OrderFlowTradeDetector(((NinjaScriptBase)this).Input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(ISeries<double> input, TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (cacheOrderFlowTradeDetector != null)
		{
			for (int i = 0; i < cacheOrderFlowTradeDetector.Length; i++)
			{
				if (cacheOrderFlowTradeDetector[i] != null && cacheOrderFlowTradeDetector[i].BaseLargeVolumeOn == baseLargeVolumeOn && cacheOrderFlowTradeDetector[i].MinimumVolumeForMarker == minimumVolumeForMarker && cacheOrderFlowTradeDetector[i].MaximumMarkerSize == maximumMarkerSize && cacheOrderFlowTradeDetector[i].BaseMarkerSizeOn == baseMarkerSizeOn && cacheOrderFlowTradeDetector[i].HoverValues == hoverValues && ((NinjaScriptBase)cacheOrderFlowTradeDetector[i]).EqualsInput(input))
				{
					return cacheOrderFlowTradeDetector[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowTradeDetector>(new OrderFlowTradeDetector
		{
			BaseLargeVolumeOn = baseLargeVolumeOn,
			MinimumVolumeForMarker = minimumVolumeForMarker,
			MaximumMarkerSize = maximumMarkerSize,
			BaseMarkerSizeOn = baseMarkerSizeOn,
			HoverValues = hoverValues
		}, input, ref cacheOrderFlowTradeDetector);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return OrderFlowVolumeProfile(((NinjaScriptBase)this).Input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(ISeries<double> input, MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (cacheOrderFlowVP != null)
		{
			for (int i = 0; i < cacheOrderFlowVP.Length; i++)
			{
				if (cacheOrderFlowVP[i] != null && cacheOrderFlowVP[i].ProfileType == profileType && cacheOrderFlowVP[i].TradingHoursInstance == tradingHoursInstance && cacheOrderFlowVP[i].ProfilePeriod == profilePeriod && cacheOrderFlowVP[i].ProfilePeriodValue == sessions && cacheOrderFlowVP[i].Resolution == resolution && cacheOrderFlowVP[i].ValueArea == valueAreaPercent && cacheOrderFlowVP[i].InitialBalanceMinutes == initialBalanceMinutes && ((NinjaScriptBase)cacheOrderFlowVP[i]).EqualsInput(input))
				{
					return cacheOrderFlowVP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowVolumeProfile>(new OrderFlowVolumeProfile
		{
			ProfileType = profileType,
			ProfilePeriod = profilePeriod,
			ProfilePeriodValue = sessions,
			TradingHoursInstance = tradingHoursInstance,
			Resolution = resolution,
			ValueArea = valueAreaPercent,
			InitialBalanceMinutes = initialBalanceMinutes
		}, input, ref cacheOrderFlowVP);
	}

	public PulseAnchoredVolumeProfile PulseAnchoredVolumeProfile(int volumeTickCompression, int volumeThreshold, VPAlignment profileAlignment, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int pOCBarOpacity, bool showPOCLine, bool showVALines, float pOCLineWidth, float vAHLineWidth, float vALLineWidth, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showLabels, int labelFontSize, int labelOffset, int rectangleFillOpacity)
	{
		return PulseAnchoredVolumeProfile(((NinjaScriptBase)this).Input, volumeTickCompression, volumeThreshold, profileAlignment, valueAreaPercentage, profileOpacity, valueAreaOpacity, pOCBarOpacity, showPOCLine, showVALines, pOCLineWidth, vAHLineWidth, vALLineWidth, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showLabels, labelFontSize, labelOffset, rectangleFillOpacity);
	}

	public PulseAnchoredVolumeProfile PulseAnchoredVolumeProfile(ISeries<double> input, int volumeTickCompression, int volumeThreshold, VPAlignment profileAlignment, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int pOCBarOpacity, bool showPOCLine, bool showVALines, float pOCLineWidth, float vAHLineWidth, float vALLineWidth, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showLabels, int labelFontSize, int labelOffset, int rectangleFillOpacity)
	{
		if (cachePulseAnchoredVolumeProfile != null)
		{
			for (int i = 0; i < cachePulseAnchoredVolumeProfile.Length; i++)
			{
				if (cachePulseAnchoredVolumeProfile[i] != null && cachePulseAnchoredVolumeProfile[i].VolumeTickCompression == volumeTickCompression && cachePulseAnchoredVolumeProfile[i].VolumeThreshold == volumeThreshold && cachePulseAnchoredVolumeProfile[i].ProfileAlignment == profileAlignment && cachePulseAnchoredVolumeProfile[i].ValueAreaPercentage == valueAreaPercentage && cachePulseAnchoredVolumeProfile[i].ProfileOpacity == profileOpacity && cachePulseAnchoredVolumeProfile[i].ValueAreaOpacity == valueAreaOpacity && cachePulseAnchoredVolumeProfile[i].POCBarOpacity == pOCBarOpacity && cachePulseAnchoredVolumeProfile[i].ShowPOCLine == showPOCLine && cachePulseAnchoredVolumeProfile[i].ShowVALines == showVALines && cachePulseAnchoredVolumeProfile[i].POCLineWidth == pOCLineWidth && cachePulseAnchoredVolumeProfile[i].VAHLineWidth == vAHLineWidth && cachePulseAnchoredVolumeProfile[i].VALLineWidth == vALLineWidth && cachePulseAnchoredVolumeProfile[i].ShowDeltaBars == showDeltaBars && cachePulseAnchoredVolumeProfile[i].DeltaProfileWidth == deltaProfileWidth && cachePulseAnchoredVolumeProfile[i].DeltaThreshold == deltaThreshold && cachePulseAnchoredVolumeProfile[i].DeltaOpacity == deltaOpacity && cachePulseAnchoredVolumeProfile[i].ShowLabels == showLabels && cachePulseAnchoredVolumeProfile[i].LabelFontSize == labelFontSize && cachePulseAnchoredVolumeProfile[i].LabelOffset == labelOffset && cachePulseAnchoredVolumeProfile[i].RectangleFillOpacity == rectangleFillOpacity && ((NinjaScriptBase)cachePulseAnchoredVolumeProfile[i]).EqualsInput(input))
				{
					return cachePulseAnchoredVolumeProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseAnchoredVolumeProfile>(new PulseAnchoredVolumeProfile
		{
			VolumeTickCompression = volumeTickCompression,
			VolumeThreshold = volumeThreshold,
			ProfileAlignment = profileAlignment,
			ValueAreaPercentage = valueAreaPercentage,
			ProfileOpacity = profileOpacity,
			ValueAreaOpacity = valueAreaOpacity,
			POCBarOpacity = pOCBarOpacity,
			ShowPOCLine = showPOCLine,
			ShowVALines = showVALines,
			POCLineWidth = pOCLineWidth,
			VAHLineWidth = vAHLineWidth,
			VALLineWidth = vALLineWidth,
			ShowDeltaBars = showDeltaBars,
			DeltaProfileWidth = deltaProfileWidth,
			DeltaThreshold = deltaThreshold,
			DeltaOpacity = deltaOpacity,
			ShowLabels = showLabels,
			LabelFontSize = labelFontSize,
			LabelOffset = labelOffset,
			RectangleFillOpacity = rectangleFillOpacity
		}, input, ref cachePulseAnchoredVolumeProfile);
	}

	public PulseAnchoredVWAP PulseAnchoredVWAP(bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, PulseAnchoredVWAPPriceSource priceSource, bool preferSelectedDrawing, bool useLatestIfNoneSelected, string anchorTagFilter)
	{
		return PulseAnchoredVWAP(((NinjaScriptBase)this).Input, showStandardDeviations, sD1Multiplier, sD2Multiplier, priceSource, preferSelectedDrawing, useLatestIfNoneSelected, anchorTagFilter);
	}

	public PulseAnchoredVWAP PulseAnchoredVWAP(ISeries<double> input, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, PulseAnchoredVWAPPriceSource priceSource, bool preferSelectedDrawing, bool useLatestIfNoneSelected, string anchorTagFilter)
	{
		if (cachePulseAnchoredVWAP != null)
		{
			for (int i = 0; i < cachePulseAnchoredVWAP.Length; i++)
			{
				if (cachePulseAnchoredVWAP[i] != null && cachePulseAnchoredVWAP[i].ShowStandardDeviations == showStandardDeviations && cachePulseAnchoredVWAP[i].SD1Multiplier == sD1Multiplier && cachePulseAnchoredVWAP[i].SD2Multiplier == sD2Multiplier && cachePulseAnchoredVWAP[i].PriceSource == priceSource && cachePulseAnchoredVWAP[i].PreferSelectedDrawing == preferSelectedDrawing && cachePulseAnchoredVWAP[i].UseLatestIfNoneSelected == useLatestIfNoneSelected && cachePulseAnchoredVWAP[i].AnchorTagFilter == anchorTagFilter && ((NinjaScriptBase)cachePulseAnchoredVWAP[i]).EqualsInput(input))
				{
					return cachePulseAnchoredVWAP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseAnchoredVWAP>(new PulseAnchoredVWAP
		{
			ShowStandardDeviations = showStandardDeviations,
			SD1Multiplier = sD1Multiplier,
			SD2Multiplier = sD2Multiplier,
			PriceSource = priceSource,
			PreferSelectedDrawing = preferSelectedDrawing,
			UseLatestIfNoneSelected = useLatestIfNoneSelected,
			AnchorTagFilter = anchorTagFilter
		}, input, ref cachePulseAnchoredVWAP);
	}

	public PulseBigTrades PulseBigTrades(int minContractsThreshold, int maxCircleRadius, int minCircleRadius, int showLabelThreshold, double circleOpacity, PulseBigTradesCircleStyle circleStyle, double circleBorderWidth, bool resetDaily, PulseBigTradesDetectionMode detectionMode, int clusterMinContracts, int clusterWindowMs, int clusterPriceGroupingTicks)
	{
		return PulseBigTrades(((NinjaScriptBase)this).Input, minContractsThreshold, maxCircleRadius, minCircleRadius, showLabelThreshold, circleOpacity, circleStyle, circleBorderWidth, resetDaily, detectionMode, clusterMinContracts, clusterWindowMs, clusterPriceGroupingTicks);
	}

	public PulseBigTrades PulseBigTrades(ISeries<double> input, int minContractsThreshold, int maxCircleRadius, int minCircleRadius, int showLabelThreshold, double circleOpacity, PulseBigTradesCircleStyle circleStyle, double circleBorderWidth, bool resetDaily, PulseBigTradesDetectionMode detectionMode, int clusterMinContracts, int clusterWindowMs, int clusterPriceGroupingTicks)
	{
		if (cachePulseBigTrades != null)
		{
			for (int i = 0; i < cachePulseBigTrades.Length; i++)
			{
				if (cachePulseBigTrades[i] != null && cachePulseBigTrades[i].MinContractsThreshold == minContractsThreshold && cachePulseBigTrades[i].MaxCircleRadius == maxCircleRadius && cachePulseBigTrades[i].MinCircleRadius == minCircleRadius && cachePulseBigTrades[i].ShowLabelThreshold == showLabelThreshold && cachePulseBigTrades[i].CircleOpacity == circleOpacity && cachePulseBigTrades[i].CircleStyle == circleStyle && cachePulseBigTrades[i].CircleBorderWidth == circleBorderWidth && cachePulseBigTrades[i].ResetDaily == resetDaily && cachePulseBigTrades[i].DetectionMode == detectionMode && cachePulseBigTrades[i].ClusterMinContracts == clusterMinContracts && cachePulseBigTrades[i].ClusterWindowMs == clusterWindowMs && cachePulseBigTrades[i].ClusterPriceGroupingTicks == clusterPriceGroupingTicks && ((NinjaScriptBase)cachePulseBigTrades[i]).EqualsInput(input))
				{
					return cachePulseBigTrades[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseBigTrades>(new PulseBigTrades
		{
			MinContractsThreshold = minContractsThreshold,
			MaxCircleRadius = maxCircleRadius,
			MinCircleRadius = minCircleRadius,
			ShowLabelThreshold = showLabelThreshold,
			CircleOpacity = circleOpacity,
			CircleStyle = circleStyle,
			CircleBorderWidth = circleBorderWidth,
			ResetDaily = resetDaily,
			DetectionMode = detectionMode,
			ClusterMinContracts = clusterMinContracts,
			ClusterWindowMs = clusterWindowMs,
			ClusterPriceGroupingTicks = clusterPriceGroupingTicks
		}, input, ref cachePulseBigTrades);
	}

	public PulseCumulativeDelta PulseCumulativeDelta(PulseCumulativeDeltaResetPeriod resetPeriod, bool showZeroLine, bool colorBasedOnDirection, double deltaMultiplier, bool showCumulative)
	{
		return PulseCumulativeDelta(((NinjaScriptBase)this).Input, resetPeriod, showZeroLine, colorBasedOnDirection, deltaMultiplier, showCumulative);
	}

	public PulseCumulativeDelta PulseCumulativeDelta(ISeries<double> input, PulseCumulativeDeltaResetPeriod resetPeriod, bool showZeroLine, bool colorBasedOnDirection, double deltaMultiplier, bool showCumulative)
	{
		if (cachePulseCumulativeDelta != null)
		{
			for (int i = 0; i < cachePulseCumulativeDelta.Length; i++)
			{
				if (cachePulseCumulativeDelta[i] != null && cachePulseCumulativeDelta[i].ResetPeriod == resetPeriod && cachePulseCumulativeDelta[i].ShowZeroLine == showZeroLine && cachePulseCumulativeDelta[i].ColorBasedOnDirection == colorBasedOnDirection && cachePulseCumulativeDelta[i].DeltaMultiplier == deltaMultiplier && cachePulseCumulativeDelta[i].ShowCumulative == showCumulative && ((NinjaScriptBase)cachePulseCumulativeDelta[i]).EqualsInput(input))
				{
					return cachePulseCumulativeDelta[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseCumulativeDelta>(new PulseCumulativeDelta
		{
			ResetPeriod = resetPeriod,
			ShowZeroLine = showZeroLine,
			ColorBasedOnDirection = colorBasedOnDirection,
			DeltaMultiplier = deltaMultiplier,
			ShowCumulative = showCumulative
		}, input, ref cachePulseCumulativeDelta);
	}

	public PulseDeltaProfile PulseDeltaProfile(int volumeProfileWidth, int volumeTickCompression, bool showMaximumVolume, bool showValues, int volumeThreshold, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int deltaLegWidth, int rotationSize, int deltaTickCompression, int deltaTextSize, int maxOpacity, int minOpacity, int gradientSteps, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return PulseDeltaProfile(((NinjaScriptBase)this).Input, volumeProfileWidth, volumeTickCompression, showMaximumVolume, showValues, volumeThreshold, valueAreaPercentage, profileOpacity, valueAreaOpacity, deltaLegWidth, rotationSize, deltaTickCompression, deltaTextSize, maxOpacity, minOpacity, gradientSteps, rTHStartTime, rTHEndTime);
	}

	public PulseDeltaProfile PulseDeltaProfile(ISeries<double> input, int volumeProfileWidth, int volumeTickCompression, bool showMaximumVolume, bool showValues, int volumeThreshold, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int deltaLegWidth, int rotationSize, int deltaTickCompression, int deltaTextSize, int maxOpacity, int minOpacity, int gradientSteps, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		if (cachePulseDeltaProfile != null)
		{
			for (int i = 0; i < cachePulseDeltaProfile.Length; i++)
			{
				if (cachePulseDeltaProfile[i] != null && cachePulseDeltaProfile[i].VolumeProfileWidth == volumeProfileWidth && cachePulseDeltaProfile[i].VolumeTickCompression == volumeTickCompression && cachePulseDeltaProfile[i].ShowMaximumVolume == showMaximumVolume && cachePulseDeltaProfile[i].ShowValues == showValues && cachePulseDeltaProfile[i].VolumeThreshold == volumeThreshold && cachePulseDeltaProfile[i].ValueAreaPercentage == valueAreaPercentage && cachePulseDeltaProfile[i].ProfileOpacity == profileOpacity && cachePulseDeltaProfile[i].ValueAreaOpacity == valueAreaOpacity && cachePulseDeltaProfile[i].DeltaLegWidth == deltaLegWidth && cachePulseDeltaProfile[i].RotationSize == rotationSize && cachePulseDeltaProfile[i].DeltaTickCompression == deltaTickCompression && cachePulseDeltaProfile[i].DeltaTextSize == deltaTextSize && cachePulseDeltaProfile[i].MaxOpacity == maxOpacity && cachePulseDeltaProfile[i].MinOpacity == minOpacity && cachePulseDeltaProfile[i].GradientSteps == gradientSteps && cachePulseDeltaProfile[i].RTHStartTime == rTHStartTime && cachePulseDeltaProfile[i].RTHEndTime == rTHEndTime && ((NinjaScriptBase)cachePulseDeltaProfile[i]).EqualsInput(input))
				{
					return cachePulseDeltaProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseDeltaProfile>(new PulseDeltaProfile
		{
			VolumeProfileWidth = volumeProfileWidth,
			VolumeTickCompression = volumeTickCompression,
			ShowMaximumVolume = showMaximumVolume,
			ShowValues = showValues,
			VolumeThreshold = volumeThreshold,
			ValueAreaPercentage = valueAreaPercentage,
			ProfileOpacity = profileOpacity,
			ValueAreaOpacity = valueAreaOpacity,
			DeltaLegWidth = deltaLegWidth,
			RotationSize = rotationSize,
			DeltaTickCompression = deltaTickCompression,
			DeltaTextSize = deltaTextSize,
			MaxOpacity = maxOpacity,
			MinOpacity = minOpacity,
			GradientSteps = gradientSteps,
			RTHStartTime = rTHStartTime,
			RTHEndTime = rTHEndTime
		}, input, ref cachePulseDeltaProfile);
	}

	public PulseDivergences PulseDivergences(int lookbackBars, double minDeltaThreshold, int minBarsBetweenSignals, bool showBullishDivergence, bool showBearishDivergence, int symbolSize, double minCandleSize, bool filterSmallCandles, bool resetCountersDaily)
	{
		return PulseDivergences(((NinjaScriptBase)this).Input, lookbackBars, minDeltaThreshold, minBarsBetweenSignals, showBullishDivergence, showBearishDivergence, symbolSize, minCandleSize, filterSmallCandles, resetCountersDaily);
	}

	public PulseDivergences PulseDivergences(ISeries<double> input, int lookbackBars, double minDeltaThreshold, int minBarsBetweenSignals, bool showBullishDivergence, bool showBearishDivergence, int symbolSize, double minCandleSize, bool filterSmallCandles, bool resetCountersDaily)
	{
		if (cachePulseDivergences != null)
		{
			for (int i = 0; i < cachePulseDivergences.Length; i++)
			{
				if (cachePulseDivergences[i] != null && cachePulseDivergences[i].LookbackBars == lookbackBars && cachePulseDivergences[i].MinDeltaThreshold == minDeltaThreshold && cachePulseDivergences[i].MinBarsBetweenSignals == minBarsBetweenSignals && cachePulseDivergences[i].ShowBullishDivergence == showBullishDivergence && cachePulseDivergences[i].ShowBearishDivergence == showBearishDivergence && cachePulseDivergences[i].SymbolSize == symbolSize && cachePulseDivergences[i].MinCandleSize == minCandleSize && cachePulseDivergences[i].FilterSmallCandles == filterSmallCandles && cachePulseDivergences[i].ResetCountersDaily == resetCountersDaily && ((NinjaScriptBase)cachePulseDivergences[i]).EqualsInput(input))
				{
					return cachePulseDivergences[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseDivergences>(new PulseDivergences
		{
			LookbackBars = lookbackBars,
			MinDeltaThreshold = minDeltaThreshold,
			MinBarsBetweenSignals = minBarsBetweenSignals,
			ShowBullishDivergence = showBullishDivergence,
			ShowBearishDivergence = showBearishDivergence,
			SymbolSize = symbolSize,
			MinCandleSize = minCandleSize,
			FilterSmallCandles = filterSmallCandles,
			ResetCountersDaily = resetCountersDaily
		}, input, ref cachePulseDivergences);
	}

	public PulseFootprintPro PulseFootprintPro(bool hideCandles, bool showBottomTable, int footprintFontSize, int tableFontSize, int stackedImbalanceMinVolume, bool showStackedImbalances, int stackedImbalanceMinLevels, int stackedImbalanceRatioPercent, bool stackedImbalanceIgnoreZeroValues)
	{
		return PulseFootprintPro(((NinjaScriptBase)this).Input, hideCandles, showBottomTable, footprintFontSize, tableFontSize, stackedImbalanceMinVolume, showStackedImbalances, stackedImbalanceMinLevels, stackedImbalanceRatioPercent, stackedImbalanceIgnoreZeroValues);
	}

	public PulseFootprintPro PulseFootprintPro(ISeries<double> input, bool hideCandles, bool showBottomTable, int footprintFontSize, int tableFontSize, int stackedImbalanceMinVolume, bool showStackedImbalances, int stackedImbalanceMinLevels, int stackedImbalanceRatioPercent, bool stackedImbalanceIgnoreZeroValues)
	{
		if (cachePulseFootprintPro != null)
		{
			for (int i = 0; i < cachePulseFootprintPro.Length; i++)
			{
				if (cachePulseFootprintPro[i] != null && cachePulseFootprintPro[i].HideCandles == hideCandles && cachePulseFootprintPro[i].ShowBottomTable == showBottomTable && cachePulseFootprintPro[i].FootprintFontSize == footprintFontSize && cachePulseFootprintPro[i].TableFontSize == tableFontSize && cachePulseFootprintPro[i].StackedImbalanceMinVolume == stackedImbalanceMinVolume && cachePulseFootprintPro[i].ShowStackedImbalances == showStackedImbalances && cachePulseFootprintPro[i].StackedImbalanceMinLevels == stackedImbalanceMinLevels && cachePulseFootprintPro[i].StackedImbalanceRatioPercent == stackedImbalanceRatioPercent && cachePulseFootprintPro[i].StackedImbalanceIgnoreZeroValues == stackedImbalanceIgnoreZeroValues && ((NinjaScriptBase)cachePulseFootprintPro[i]).EqualsInput(input))
				{
					return cachePulseFootprintPro[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseFootprintPro>(new PulseFootprintPro
		{
			HideCandles = hideCandles,
			ShowBottomTable = showBottomTable,
			FootprintFontSize = footprintFontSize,
			TableFontSize = tableFontSize,
			StackedImbalanceMinVolume = stackedImbalanceMinVolume,
			ShowStackedImbalances = showStackedImbalances,
			StackedImbalanceMinLevels = stackedImbalanceMinLevels,
			StackedImbalanceRatioPercent = stackedImbalanceRatioPercent,
			StackedImbalanceIgnoreZeroValues = stackedImbalanceIgnoreZeroValues
		}, input, ref cachePulseFootprintPro);
	}

	public PulseHeikin PulseHeikin()
	{
		return PulseHeikin(((NinjaScriptBase)this).Input);
	}

	public PulseHeikin PulseHeikin(ISeries<double> input)
	{
		if (cachePulseHeikin != null)
		{
			for (int i = 0; i < cachePulseHeikin.Length; i++)
			{
				if (cachePulseHeikin[i] != null && ((NinjaScriptBase)cachePulseHeikin[i]).EqualsInput(input))
				{
					return cachePulseHeikin[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseHeikin>(new PulseHeikin(), input, ref cachePulseHeikin);
	}

	public PulseDailyLevels PulseDailyLevels(int openingRangeMinutes, int initialBalanceMinutes, bool showOvernight, bool showSessionOpen, bool showOpeningRange, bool showInitialBalance, bool showPreviousDay, int levelTextSize, int rightMarginPx)
	{
		return PulseDailyLevels(((NinjaScriptBase)this).Input, openingRangeMinutes, initialBalanceMinutes, showOvernight, showSessionOpen, showOpeningRange, showInitialBalance, showPreviousDay, levelTextSize, rightMarginPx);
	}

	public PulseDailyLevels PulseDailyLevels(ISeries<double> input, int openingRangeMinutes, int initialBalanceMinutes, bool showOvernight, bool showSessionOpen, bool showOpeningRange, bool showInitialBalance, bool showPreviousDay, int levelTextSize, int rightMarginPx)
	{
		if (cachePulseDailyLevels != null)
		{
			for (int i = 0; i < cachePulseDailyLevels.Length; i++)
			{
				if (cachePulseDailyLevels[i] != null && cachePulseDailyLevels[i].OpeningRangeMinutes == openingRangeMinutes && cachePulseDailyLevels[i].InitialBalanceMinutes == initialBalanceMinutes && cachePulseDailyLevels[i].ShowOvernight == showOvernight && cachePulseDailyLevels[i].ShowSessionOpen == showSessionOpen && cachePulseDailyLevels[i].ShowOpeningRange == showOpeningRange && cachePulseDailyLevels[i].ShowInitialBalance == showInitialBalance && cachePulseDailyLevels[i].ShowPreviousDay == showPreviousDay && cachePulseDailyLevels[i].LevelTextSize == levelTextSize && cachePulseDailyLevels[i].RightMarginPx == rightMarginPx && ((NinjaScriptBase)cachePulseDailyLevels[i]).EqualsInput(input))
				{
					return cachePulseDailyLevels[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseDailyLevels>(new PulseDailyLevels
		{
			OpeningRangeMinutes = openingRangeMinutes,
			InitialBalanceMinutes = initialBalanceMinutes,
			ShowOvernight = showOvernight,
			ShowSessionOpen = showSessionOpen,
			ShowOpeningRange = showOpeningRange,
			ShowInitialBalance = showInitialBalance,
			ShowPreviousDay = showPreviousDay,
			LevelTextSize = levelTextSize,
			RightMarginPx = rightMarginPx
		}, input, ref cachePulseDailyLevels);
	}

	public PulseStackedImbalances PulseStackedImbalances(bool ignoreZeroValues, int imbalanceRatio, int imbalanceRange, int imbalanceVolume, bool lineTillTouch, Color askImbalanceColor, Color bidImbalanceColor, int lineWidth, int printLineForXBars, int daysLookBack, double stackedImbalanceOpacity, bool enableHistoricalReconstruction)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return PulseStackedImbalances(((NinjaScriptBase)this).Input, ignoreZeroValues, imbalanceRatio, imbalanceRange, imbalanceVolume, lineTillTouch, askImbalanceColor, bidImbalanceColor, lineWidth, printLineForXBars, daysLookBack, stackedImbalanceOpacity, enableHistoricalReconstruction);
	}

	public PulseStackedImbalances PulseStackedImbalances(ISeries<double> input, bool ignoreZeroValues, int imbalanceRatio, int imbalanceRange, int imbalanceVolume, bool lineTillTouch, Color askImbalanceColor, Color bidImbalanceColor, int lineWidth, int printLineForXBars, int daysLookBack, double stackedImbalanceOpacity, bool enableHistoricalReconstruction)
	{
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (cachePulseStackedImbalances != null)
		{
			for (int i = 0; i < cachePulseStackedImbalances.Length; i++)
			{
				if (cachePulseStackedImbalances[i] != null && cachePulseStackedImbalances[i].IgnoreZeroValues == ignoreZeroValues && cachePulseStackedImbalances[i].ImbalanceRatio == imbalanceRatio && cachePulseStackedImbalances[i].ImbalanceRange == imbalanceRange && cachePulseStackedImbalances[i].ImbalanceVolume == imbalanceVolume && cachePulseStackedImbalances[i].LineTillTouch == lineTillTouch && cachePulseStackedImbalances[i].AskImbalanceColor == askImbalanceColor && cachePulseStackedImbalances[i].BidImbalanceColor == bidImbalanceColor && cachePulseStackedImbalances[i].LineWidth == lineWidth && cachePulseStackedImbalances[i].PrintLineForXBars == printLineForXBars && cachePulseStackedImbalances[i].DaysLookBack == daysLookBack && cachePulseStackedImbalances[i].StackedImbalanceOpacity == stackedImbalanceOpacity && cachePulseStackedImbalances[i].EnableHistoricalReconstruction == enableHistoricalReconstruction && ((NinjaScriptBase)cachePulseStackedImbalances[i]).EqualsInput(input))
				{
					return cachePulseStackedImbalances[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseStackedImbalances>(new PulseStackedImbalances
		{
			IgnoreZeroValues = ignoreZeroValues,
			ImbalanceRatio = imbalanceRatio,
			ImbalanceRange = imbalanceRange,
			ImbalanceVolume = imbalanceVolume,
			LineTillTouch = lineTillTouch,
			AskImbalanceColor = askImbalanceColor,
			BidImbalanceColor = bidImbalanceColor,
			LineWidth = lineWidth,
			PrintLineForXBars = printLineForXBars,
			DaysLookBack = daysLookBack,
			StackedImbalanceOpacity = stackedImbalanceOpacity,
			EnableHistoricalReconstruction = enableHistoricalReconstruction
		}, input, ref cachePulseStackedImbalances);
	}

	public PulseTPO PulseTPO(bool drawPOCBool, bool drawVirginPOCBool, bool drawTPOLetters, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush tPOTextColor, int tPOFontSize, int tPOLetterSpacing, bool drawDeltaProfile, int deltaProfileWidth, int deltaTickCompression, int deltaProfileOpacity, Brush deltaPositiveColor, Brush deltaNegativeColor)
	{
		return PulseTPO(((NinjaScriptBase)this).Input, drawPOCBool, drawVirginPOCBool, drawTPOLetters, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, tPOTextColor, tPOFontSize, tPOLetterSpacing, drawDeltaProfile, deltaProfileWidth, deltaTickCompression, deltaProfileOpacity, deltaPositiveColor, deltaNegativeColor);
	}

	public PulseTPO PulseTPO(ISeries<double> input, bool drawPOCBool, bool drawVirginPOCBool, bool drawTPOLetters, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush tPOTextColor, int tPOFontSize, int tPOLetterSpacing, bool drawDeltaProfile, int deltaProfileWidth, int deltaTickCompression, int deltaProfileOpacity, Brush deltaPositiveColor, Brush deltaNegativeColor)
	{
		if (cachePulseTPO != null)
		{
			for (int i = 0; i < cachePulseTPO.Length; i++)
			{
				if (cachePulseTPO[i] != null && cachePulseTPO[i].DrawPOCBool == drawPOCBool && cachePulseTPO[i].DrawVirginPOCBool == drawVirginPOCBool && cachePulseTPO[i].DrawTPOLetters == drawTPOLetters && cachePulseTPO[i].POCStroke == pOCStroke && cachePulseTPO[i].POCVAHStroke == pOCVAHStroke && cachePulseTPO[i].POCVALStroke == pOCVALStroke && cachePulseTPO[i].ValueAreaSize == valueAreaSize && cachePulseTPO[i].VirginPOCStroke == virginPOCStroke && cachePulseTPO[i].VirginPOCVAHStroke == virginPOCVAHStroke && cachePulseTPO[i].VirginPOCVALStroke == virginPOCVALStroke && cachePulseTPO[i].TPOTextColor == tPOTextColor && cachePulseTPO[i].TPOFontSize == tPOFontSize && cachePulseTPO[i].TPOLetterSpacing == tPOLetterSpacing && cachePulseTPO[i].DrawDeltaProfile == drawDeltaProfile && cachePulseTPO[i].DeltaProfileWidth == deltaProfileWidth && cachePulseTPO[i].DeltaTickCompression == deltaTickCompression && cachePulseTPO[i].DeltaProfileOpacity == deltaProfileOpacity && cachePulseTPO[i].DeltaPositiveColor == deltaPositiveColor && cachePulseTPO[i].DeltaNegativeColor == deltaNegativeColor && ((NinjaScriptBase)cachePulseTPO[i]).EqualsInput(input))
				{
					return cachePulseTPO[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseTPO>(new PulseTPO
		{
			DrawPOCBool = drawPOCBool,
			DrawVirginPOCBool = drawVirginPOCBool,
			DrawTPOLetters = drawTPOLetters,
			POCStroke = pOCStroke,
			POCVAHStroke = pOCVAHStroke,
			POCVALStroke = pOCVALStroke,
			ValueAreaSize = valueAreaSize,
			VirginPOCStroke = virginPOCStroke,
			VirginPOCVAHStroke = virginPOCVAHStroke,
			VirginPOCVALStroke = virginPOCVALStroke,
			TPOTextColor = tPOTextColor,
			TPOFontSize = tPOFontSize,
			TPOLetterSpacing = tPOLetterSpacing,
			DrawDeltaProfile = drawDeltaProfile,
			DeltaProfileWidth = deltaProfileWidth,
			DeltaTickCompression = deltaTickCompression,
			DeltaProfileOpacity = deltaProfileOpacity,
			DeltaPositiveColor = deltaPositiveColor,
			DeltaNegativeColor = deltaNegativeColor
		}, input, ref cachePulseTPO);
	}

	public PulseVolumeProfileLite PulseVolumeProfileLite(bool drawPOCBool, bool drawVirginPOCBool, bool drawVolumeHistogramBool, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush volumeHistogramBorderColor, Stroke volumeHistogramStroke)
	{
		return PulseVolumeProfileLite(((NinjaScriptBase)this).Input, drawPOCBool, drawVirginPOCBool, drawVolumeHistogramBool, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, volumeHistogramBorderColor, volumeHistogramStroke);
	}

	public PulseVolumeProfileLite PulseVolumeProfileLite(ISeries<double> input, bool drawPOCBool, bool drawVirginPOCBool, bool drawVolumeHistogramBool, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush volumeHistogramBorderColor, Stroke volumeHistogramStroke)
	{
		if (cachePulseVolumeProfileLite != null)
		{
			for (int i = 0; i < cachePulseVolumeProfileLite.Length; i++)
			{
				if (cachePulseVolumeProfileLite[i] != null && cachePulseVolumeProfileLite[i].DrawPOCBool == drawPOCBool && cachePulseVolumeProfileLite[i].DrawVirginPOCBool == drawVirginPOCBool && cachePulseVolumeProfileLite[i].DrawVolumeHistogramBool == drawVolumeHistogramBool && cachePulseVolumeProfileLite[i].POCStroke == pOCStroke && cachePulseVolumeProfileLite[i].POCVAHStroke == pOCVAHStroke && cachePulseVolumeProfileLite[i].POCVALStroke == pOCVALStroke && cachePulseVolumeProfileLite[i].ValueAreaSize == valueAreaSize && cachePulseVolumeProfileLite[i].VirginPOCStroke == virginPOCStroke && cachePulseVolumeProfileLite[i].VirginPOCVAHStroke == virginPOCVAHStroke && cachePulseVolumeProfileLite[i].VirginPOCVALStroke == virginPOCVALStroke && cachePulseVolumeProfileLite[i].VolumeHistogramBorderColor == volumeHistogramBorderColor && cachePulseVolumeProfileLite[i].VolumeHistogramStroke == volumeHistogramStroke && ((NinjaScriptBase)cachePulseVolumeProfileLite[i]).EqualsInput(input))
				{
					return cachePulseVolumeProfileLite[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseVolumeProfileLite>(new PulseVolumeProfileLite
		{
			DrawPOCBool = drawPOCBool,
			DrawVirginPOCBool = drawVirginPOCBool,
			DrawVolumeHistogramBool = drawVolumeHistogramBool,
			POCStroke = pOCStroke,
			POCVAHStroke = pOCVAHStroke,
			POCVALStroke = pOCVALStroke,
			ValueAreaSize = valueAreaSize,
			VirginPOCStroke = virginPOCStroke,
			VirginPOCVAHStroke = virginPOCVAHStroke,
			VirginPOCVALStroke = virginPOCVALStroke,
			VolumeHistogramBorderColor = volumeHistogramBorderColor,
			VolumeHistogramStroke = volumeHistogramStroke
		}, input, ref cachePulseVolumeProfileLite);
	}

	public PulseVP PulseVP(int volumeProfileWidth, int volumeTickCompression, int volumeThreshold, bool showProfileBars, int daysToShow, bool hideCandles, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, bool showPOCLine, bool showVALines, bool extendLines, Stroke pOCStroke, Stroke vAHStroke, Stroke vALStroke, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showCompositeProfile, int compositeSessionsCount, bool compositeIncludeCurrentSession, int compositeProfileWidth, int compositeOpacity, bool compositeShowDelta, bool showLVNMarkers, int lVNMaxPercentOfPOC, int lVNMinProminencePercent, bool lVNIgnoreInsideValueArea, int lVNMinSeparationTicks, int lVNMaxPerSession, int lVNHeightTicks, int lVNOpacity, int tradingHours, int profilePeriod, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return PulseVP(((NinjaScriptBase)this).Input, volumeProfileWidth, volumeTickCompression, volumeThreshold, showProfileBars, daysToShow, hideCandles, valueAreaPercentage, profileOpacity, valueAreaOpacity, showPOCLine, showVALines, extendLines, pOCStroke, vAHStroke, vALStroke, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showCompositeProfile, compositeSessionsCount, compositeIncludeCurrentSession, compositeProfileWidth, compositeOpacity, compositeShowDelta, showLVNMarkers, lVNMaxPercentOfPOC, lVNMinProminencePercent, lVNIgnoreInsideValueArea, lVNMinSeparationTicks, lVNMaxPerSession, lVNHeightTicks, lVNOpacity, tradingHours, profilePeriod, rTHStartTime, rTHEndTime);
	}

	public PulseVP PulseVP(ISeries<double> input, int volumeProfileWidth, int volumeTickCompression, int volumeThreshold, bool showProfileBars, int daysToShow, bool hideCandles, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, bool showPOCLine, bool showVALines, bool extendLines, Stroke pOCStroke, Stroke vAHStroke, Stroke vALStroke, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showCompositeProfile, int compositeSessionsCount, bool compositeIncludeCurrentSession, int compositeProfileWidth, int compositeOpacity, bool compositeShowDelta, bool showLVNMarkers, int lVNMaxPercentOfPOC, int lVNMinProminencePercent, bool lVNIgnoreInsideValueArea, int lVNMinSeparationTicks, int lVNMaxPerSession, int lVNHeightTicks, int lVNOpacity, int tradingHours, int profilePeriod, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		if (cachePulseVP != null)
		{
			for (int i = 0; i < cachePulseVP.Length; i++)
			{
				if (cachePulseVP[i] != null && cachePulseVP[i].VolumeProfileWidth == volumeProfileWidth && cachePulseVP[i].VolumeTickCompression == volumeTickCompression && cachePulseVP[i].VolumeThreshold == volumeThreshold && cachePulseVP[i].ShowProfileBars == showProfileBars && cachePulseVP[i].DaysToShow == daysToShow && cachePulseVP[i].HideCandles == hideCandles && cachePulseVP[i].ValueAreaPercentage == valueAreaPercentage && cachePulseVP[i].ProfileOpacity == profileOpacity && cachePulseVP[i].ValueAreaOpacity == valueAreaOpacity && cachePulseVP[i].ShowPOCLine == showPOCLine && cachePulseVP[i].ShowVALines == showVALines && cachePulseVP[i].ExtendLines == extendLines && cachePulseVP[i].POCStroke == pOCStroke && cachePulseVP[i].VAHStroke == vAHStroke && cachePulseVP[i].VALStroke == vALStroke && cachePulseVP[i].ShowDeltaBars == showDeltaBars && cachePulseVP[i].DeltaProfileWidth == deltaProfileWidth && cachePulseVP[i].DeltaThreshold == deltaThreshold && cachePulseVP[i].DeltaOpacity == deltaOpacity && cachePulseVP[i].ShowCompositeProfile == showCompositeProfile && cachePulseVP[i].CompositeSessionsCount == compositeSessionsCount && cachePulseVP[i].CompositeIncludeCurrentSession == compositeIncludeCurrentSession && cachePulseVP[i].CompositeProfileWidth == compositeProfileWidth && cachePulseVP[i].CompositeOpacity == compositeOpacity && cachePulseVP[i].CompositeShowDelta == compositeShowDelta && cachePulseVP[i].ShowLVNMarkers == showLVNMarkers && cachePulseVP[i].LVNMaxPercentOfPOC == lVNMaxPercentOfPOC && cachePulseVP[i].LVNMinProminencePercent == lVNMinProminencePercent && cachePulseVP[i].LVNIgnoreInsideValueArea == lVNIgnoreInsideValueArea && cachePulseVP[i].LVNMinSeparationTicks == lVNMinSeparationTicks && cachePulseVP[i].LVNMaxPerSession == lVNMaxPerSession && cachePulseVP[i].LVNHeightTicks == lVNHeightTicks && cachePulseVP[i].LVNOpacity == lVNOpacity && cachePulseVP[i].TradingHours == tradingHours && cachePulseVP[i].ProfilePeriod == profilePeriod && cachePulseVP[i].RTHStartTime == rTHStartTime && cachePulseVP[i].RTHEndTime == rTHEndTime && ((NinjaScriptBase)cachePulseVP[i]).EqualsInput(input))
				{
					return cachePulseVP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseVP>(new PulseVP
		{
			VolumeProfileWidth = volumeProfileWidth,
			VolumeTickCompression = volumeTickCompression,
			VolumeThreshold = volumeThreshold,
			ShowProfileBars = showProfileBars,
			DaysToShow = daysToShow,
			HideCandles = hideCandles,
			ValueAreaPercentage = valueAreaPercentage,
			ProfileOpacity = profileOpacity,
			ValueAreaOpacity = valueAreaOpacity,
			ShowPOCLine = showPOCLine,
			ShowVALines = showVALines,
			ExtendLines = extendLines,
			POCStroke = pOCStroke,
			VAHStroke = vAHStroke,
			VALStroke = vALStroke,
			ShowDeltaBars = showDeltaBars,
			DeltaProfileWidth = deltaProfileWidth,
			DeltaThreshold = deltaThreshold,
			DeltaOpacity = deltaOpacity,
			ShowCompositeProfile = showCompositeProfile,
			CompositeSessionsCount = compositeSessionsCount,
			CompositeIncludeCurrentSession = compositeIncludeCurrentSession,
			CompositeProfileWidth = compositeProfileWidth,
			CompositeOpacity = compositeOpacity,
			CompositeShowDelta = compositeShowDelta,
			ShowLVNMarkers = showLVNMarkers,
			LVNMaxPercentOfPOC = lVNMaxPercentOfPOC,
			LVNMinProminencePercent = lVNMinProminencePercent,
			LVNIgnoreInsideValueArea = lVNIgnoreInsideValueArea,
			LVNMinSeparationTicks = lVNMinSeparationTicks,
			LVNMaxPerSession = lVNMaxPerSession,
			LVNHeightTicks = lVNHeightTicks,
			LVNOpacity = lVNOpacity,
			TradingHours = tradingHours,
			ProfilePeriod = profilePeriod,
			RTHStartTime = rTHStartTime,
			RTHEndTime = rTHEndTime
		}, input, ref cachePulseVP);
	}

	public PulseVWAP PulseVWAP(PulseVWAPResetPeriod resetPeriod, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, bool showPreviousVWAP, int levelTextSize)
	{
		return PulseVWAP(((NinjaScriptBase)this).Input, resetPeriod, showStandardDeviations, sD1Multiplier, sD2Multiplier, showPreviousVWAP, levelTextSize);
	}

	public PulseVWAP PulseVWAP(ISeries<double> input, PulseVWAPResetPeriod resetPeriod, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, bool showPreviousVWAP, int levelTextSize)
	{
		if (cachePulseVWAP != null)
		{
			for (int i = 0; i < cachePulseVWAP.Length; i++)
			{
				if (cachePulseVWAP[i] != null && cachePulseVWAP[i].ResetPeriod == resetPeriod && cachePulseVWAP[i].ShowStandardDeviations == showStandardDeviations && cachePulseVWAP[i].SD1Multiplier == sD1Multiplier && cachePulseVWAP[i].SD2Multiplier == sD2Multiplier && cachePulseVWAP[i].ShowPreviousVWAP == showPreviousVWAP && cachePulseVWAP[i].LevelTextSize == levelTextSize && ((NinjaScriptBase)cachePulseVWAP[i]).EqualsInput(input))
				{
					return cachePulseVWAP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseVWAP>(new PulseVWAP
		{
			ResetPeriod = resetPeriod,
			ShowStandardDeviations = showStandardDeviations,
			SD1Multiplier = sD1Multiplier,
			SD2Multiplier = sD2Multiplier,
			ShowPreviousVWAP = showPreviousVWAP,
			LevelTextSize = levelTextSize
		}, input, ref cachePulseVWAP);
	}

	public PulseWeeklyLevels PulseWeeklyLevels(int weeklyIBDurationHours, int valueAreaPercent, int levelTextSize, bool showWeeklyIB, bool showWIB50, bool showWIB100, bool showWIB150, bool showWIB200, bool showWeekOpen, bool showWeekMid, bool showWeekVA, bool showWeekVWAP, bool showPriorWeekHLC, bool showPriorWeekOpen, bool showPriorWeekMid, bool showPriorWeekVA)
	{
		return PulseWeeklyLevels(((NinjaScriptBase)this).Input, weeklyIBDurationHours, valueAreaPercent, levelTextSize, showWeeklyIB, showWIB50, showWIB100, showWIB150, showWIB200, showWeekOpen, showWeekMid, showWeekVA, showWeekVWAP, showPriorWeekHLC, showPriorWeekOpen, showPriorWeekMid, showPriorWeekVA);
	}

	public PulseWeeklyLevels PulseWeeklyLevels(ISeries<double> input, int weeklyIBDurationHours, int valueAreaPercent, int levelTextSize, bool showWeeklyIB, bool showWIB50, bool showWIB100, bool showWIB150, bool showWIB200, bool showWeekOpen, bool showWeekMid, bool showWeekVA, bool showWeekVWAP, bool showPriorWeekHLC, bool showPriorWeekOpen, bool showPriorWeekMid, bool showPriorWeekVA)
	{
		if (cachePulseWeeklyLevels != null)
		{
			for (int i = 0; i < cachePulseWeeklyLevels.Length; i++)
			{
				if (cachePulseWeeklyLevels[i] != null && cachePulseWeeklyLevels[i].WeeklyIBDurationHours == weeklyIBDurationHours && cachePulseWeeklyLevels[i].ValueAreaPercent == valueAreaPercent && cachePulseWeeklyLevels[i].LevelTextSize == levelTextSize && cachePulseWeeklyLevels[i].ShowWeeklyIB == showWeeklyIB && cachePulseWeeklyLevels[i].ShowWIB50 == showWIB50 && cachePulseWeeklyLevels[i].ShowWIB100 == showWIB100 && cachePulseWeeklyLevels[i].ShowWIB150 == showWIB150 && cachePulseWeeklyLevels[i].ShowWIB200 == showWIB200 && cachePulseWeeklyLevels[i].ShowWeekOpen == showWeekOpen && cachePulseWeeklyLevels[i].ShowWeekMid == showWeekMid && cachePulseWeeklyLevels[i].ShowWeekVA == showWeekVA && cachePulseWeeklyLevels[i].ShowWeekVWAP == showWeekVWAP && cachePulseWeeklyLevels[i].ShowPriorWeekHLC == showPriorWeekHLC && cachePulseWeeklyLevels[i].ShowPriorWeekOpen == showPriorWeekOpen && cachePulseWeeklyLevels[i].ShowPriorWeekMid == showPriorWeekMid && cachePulseWeeklyLevels[i].ShowPriorWeekVA == showPriorWeekVA && ((NinjaScriptBase)cachePulseWeeklyLevels[i]).EqualsInput(input))
				{
					return cachePulseWeeklyLevels[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PulseWeeklyLevels>(new PulseWeeklyLevels
		{
			WeeklyIBDurationHours = weeklyIBDurationHours,
			ValueAreaPercent = valueAreaPercent,
			LevelTextSize = levelTextSize,
			ShowWeeklyIB = showWeeklyIB,
			ShowWIB50 = showWIB50,
			ShowWIB100 = showWIB100,
			ShowWIB150 = showWIB150,
			ShowWIB200 = showWIB200,
			ShowWeekOpen = showWeekOpen,
			ShowWeekMid = showWeekMid,
			ShowWeekVA = showWeekVA,
			ShowWeekVWAP = showWeekVWAP,
			ShowPriorWeekHLC = showPriorWeekHLC,
			ShowPriorWeekOpen = showPriorWeekOpen,
			ShowPriorWeekMid = showPriorWeekMid,
			ShowPriorWeekVA = showPriorWeekVA
		}, input, ref cachePulseWeeklyLevels);
	}
}
