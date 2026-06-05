using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.Pulse;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class MarketAnalyzerColumn : MarketAnalyzerColumnBase
{
	private Indicator indicator;

	[Browsable(false)]
	public bool IsDataSeriesRequired
	{
		get
		{
			return ((NinjaScriptBase)this).IsDataSeriesRequired;
		}
		set
		{
			((NinjaScriptBase)this).IsDataSeriesRequired = value;
			if (indicator != null)
			{
				((NinjaScriptBase)indicator).IsDataSeriesRequired = value;
			}
		}
	}

	public MarketAnalyzerColumn()
	{
		lock (((NinjaScriptBase)this).NinjaScripts)
		{
			Collection<NinjaScriptBase> ninjaScripts = ((NinjaScriptBase)this).NinjaScripts;
			Indicator obj = new Indicator();
			((NinjaScriptBase)obj).IsDataSeriesRequired = IsDataSeriesRequired;
			((NinjaScriptBase)obj).Parent = (NinjaScriptBase)(object)this;
			Indicator item = obj;
			indicator = obj;
			ninjaScripts.Add((NinjaScriptBase)(object)item);
		}
	}

	public WoodiesCCI WoodiesCCI(int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return indicator.WoodiesCCI(((NinjaScriptBase)this).Input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesCCI WoodiesCCI(ISeries<double> input, int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return indicator.WoodiesCCI(input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesPivots WoodiesPivots(HLCCalculationModeWoodie priorDayHlc, int width)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return indicator.WoodiesPivots(((NinjaScriptBase)this).Input, priorDayHlc, width);
	}

	public WoodiesPivots WoodiesPivots(ISeries<double> input, HLCCalculationModeWoodie priorDayHlc, int width)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return indicator.WoodiesPivots(input, priorDayHlc, width);
	}

	public WisemanAlligator WisemanAlligator(int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return indicator.WisemanAlligator(((NinjaScriptBase)this).Input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAlligator WisemanAlligator(ISeries<double> input, int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return indicator.WisemanAlligator(input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator()
	{
		return indicator.WisemanAwesomeOscillator(((NinjaScriptBase)this).Input);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator(ISeries<double> input)
	{
		return indicator.WisemanAwesomeOscillator(input);
	}

	public WisemanFractal WisemanFractal(int strength, int triangleOffset)
	{
		return indicator.WisemanFractal(((NinjaScriptBase)this).Input, strength, triangleOffset);
	}

	public WisemanFractal WisemanFractal(ISeries<double> input, int strength, int triangleOffset)
	{
		return indicator.WisemanFractal(input, strength, triangleOffset);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowCumulativeDelta(((NinjaScriptBase)this).Input, deltaType, period, sizeFilter);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(ISeries<double> input, CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowCumulativeDelta(input, deltaType, period, sizeFilter);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowMarketDepthMap(((NinjaScriptBase)this).Input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(ISeries<double> input, BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowMarketDepthMap(input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowVolumeProfile(((NinjaScriptBase)this).Input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(ISeries<double> input, MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowVolumeProfile(input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public OrderFlowVWAP OrderFlowVWAP(VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowVWAP(((NinjaScriptBase)this).Input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowVWAP OrderFlowVWAP(ISeries<double> input, VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowVWAP(input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowTradeDetector(((NinjaScriptBase)this).Input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(ISeries<double> input, TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return indicator.OrderFlowTradeDetector(input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public PulseAnchoredVolumeProfile PulseAnchoredVolumeProfile(int volumeTickCompression, int volumeThreshold, VPAlignment profileAlignment, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int pOCBarOpacity, bool showPOCLine, bool showVALines, float pOCLineWidth, float vAHLineWidth, float vALLineWidth, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showLabels, int labelFontSize, int labelOffset, int rectangleFillOpacity)
	{
		return indicator.PulseAnchoredVolumeProfile(((NinjaScriptBase)this).Input, volumeTickCompression, volumeThreshold, profileAlignment, valueAreaPercentage, profileOpacity, valueAreaOpacity, pOCBarOpacity, showPOCLine, showVALines, pOCLineWidth, vAHLineWidth, vALLineWidth, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showLabels, labelFontSize, labelOffset, rectangleFillOpacity);
	}

	public PulseAnchoredVolumeProfile PulseAnchoredVolumeProfile(ISeries<double> input, int volumeTickCompression, int volumeThreshold, VPAlignment profileAlignment, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int pOCBarOpacity, bool showPOCLine, bool showVALines, float pOCLineWidth, float vAHLineWidth, float vALLineWidth, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showLabels, int labelFontSize, int labelOffset, int rectangleFillOpacity)
	{
		return indicator.PulseAnchoredVolumeProfile(input, volumeTickCompression, volumeThreshold, profileAlignment, valueAreaPercentage, profileOpacity, valueAreaOpacity, pOCBarOpacity, showPOCLine, showVALines, pOCLineWidth, vAHLineWidth, vALLineWidth, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showLabels, labelFontSize, labelOffset, rectangleFillOpacity);
	}

	public PulseAnchoredVWAP PulseAnchoredVWAP(bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, PulseAnchoredVWAPPriceSource priceSource, bool preferSelectedDrawing, bool useLatestIfNoneSelected, string anchorTagFilter)
	{
		return indicator.PulseAnchoredVWAP(((NinjaScriptBase)this).Input, showStandardDeviations, sD1Multiplier, sD2Multiplier, priceSource, preferSelectedDrawing, useLatestIfNoneSelected, anchorTagFilter);
	}

	public PulseAnchoredVWAP PulseAnchoredVWAP(ISeries<double> input, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, PulseAnchoredVWAPPriceSource priceSource, bool preferSelectedDrawing, bool useLatestIfNoneSelected, string anchorTagFilter)
	{
		return indicator.PulseAnchoredVWAP(input, showStandardDeviations, sD1Multiplier, sD2Multiplier, priceSource, preferSelectedDrawing, useLatestIfNoneSelected, anchorTagFilter);
	}

	public PulseBigTrades PulseBigTrades(int minContractsThreshold, int maxCircleRadius, int minCircleRadius, int showLabelThreshold, double circleOpacity, PulseBigTradesCircleStyle circleStyle, double circleBorderWidth, bool resetDaily, PulseBigTradesDetectionMode detectionMode, int clusterMinContracts, int clusterWindowMs, int clusterPriceGroupingTicks)
	{
		return indicator.PulseBigTrades(((NinjaScriptBase)this).Input, minContractsThreshold, maxCircleRadius, minCircleRadius, showLabelThreshold, circleOpacity, circleStyle, circleBorderWidth, resetDaily, detectionMode, clusterMinContracts, clusterWindowMs, clusterPriceGroupingTicks);
	}

	public PulseBigTrades PulseBigTrades(ISeries<double> input, int minContractsThreshold, int maxCircleRadius, int minCircleRadius, int showLabelThreshold, double circleOpacity, PulseBigTradesCircleStyle circleStyle, double circleBorderWidth, bool resetDaily, PulseBigTradesDetectionMode detectionMode, int clusterMinContracts, int clusterWindowMs, int clusterPriceGroupingTicks)
	{
		return indicator.PulseBigTrades(input, minContractsThreshold, maxCircleRadius, minCircleRadius, showLabelThreshold, circleOpacity, circleStyle, circleBorderWidth, resetDaily, detectionMode, clusterMinContracts, clusterWindowMs, clusterPriceGroupingTicks);
	}

	public PulseCumulativeDelta PulseCumulativeDelta(PulseCumulativeDeltaResetPeriod resetPeriod, bool showZeroLine, bool colorBasedOnDirection, double deltaMultiplier, bool showCumulative)
	{
		return indicator.PulseCumulativeDelta(((NinjaScriptBase)this).Input, resetPeriod, showZeroLine, colorBasedOnDirection, deltaMultiplier, showCumulative);
	}

	public PulseCumulativeDelta PulseCumulativeDelta(ISeries<double> input, PulseCumulativeDeltaResetPeriod resetPeriod, bool showZeroLine, bool colorBasedOnDirection, double deltaMultiplier, bool showCumulative)
	{
		return indicator.PulseCumulativeDelta(input, resetPeriod, showZeroLine, colorBasedOnDirection, deltaMultiplier, showCumulative);
	}

	public PulseDeltaProfile PulseDeltaProfile(int volumeProfileWidth, int volumeTickCompression, bool showMaximumVolume, bool showValues, int volumeThreshold, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int deltaLegWidth, int rotationSize, int deltaTickCompression, int deltaTextSize, int maxOpacity, int minOpacity, int gradientSteps, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return indicator.PulseDeltaProfile(((NinjaScriptBase)this).Input, volumeProfileWidth, volumeTickCompression, showMaximumVolume, showValues, volumeThreshold, valueAreaPercentage, profileOpacity, valueAreaOpacity, deltaLegWidth, rotationSize, deltaTickCompression, deltaTextSize, maxOpacity, minOpacity, gradientSteps, rTHStartTime, rTHEndTime);
	}

	public PulseDeltaProfile PulseDeltaProfile(ISeries<double> input, int volumeProfileWidth, int volumeTickCompression, bool showMaximumVolume, bool showValues, int volumeThreshold, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, int deltaLegWidth, int rotationSize, int deltaTickCompression, int deltaTextSize, int maxOpacity, int minOpacity, int gradientSteps, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return indicator.PulseDeltaProfile(input, volumeProfileWidth, volumeTickCompression, showMaximumVolume, showValues, volumeThreshold, valueAreaPercentage, profileOpacity, valueAreaOpacity, deltaLegWidth, rotationSize, deltaTickCompression, deltaTextSize, maxOpacity, minOpacity, gradientSteps, rTHStartTime, rTHEndTime);
	}

	public PulseDivergences PulseDivergences(int lookbackBars, double minDeltaThreshold, int minBarsBetweenSignals, bool showBullishDivergence, bool showBearishDivergence, int symbolSize, double minCandleSize, bool filterSmallCandles, bool resetCountersDaily)
	{
		return indicator.PulseDivergences(((NinjaScriptBase)this).Input, lookbackBars, minDeltaThreshold, minBarsBetweenSignals, showBullishDivergence, showBearishDivergence, symbolSize, minCandleSize, filterSmallCandles, resetCountersDaily);
	}

	public PulseDivergences PulseDivergences(ISeries<double> input, int lookbackBars, double minDeltaThreshold, int minBarsBetweenSignals, bool showBullishDivergence, bool showBearishDivergence, int symbolSize, double minCandleSize, bool filterSmallCandles, bool resetCountersDaily)
	{
		return indicator.PulseDivergences(input, lookbackBars, minDeltaThreshold, minBarsBetweenSignals, showBullishDivergence, showBearishDivergence, symbolSize, minCandleSize, filterSmallCandles, resetCountersDaily);
	}

	public PulseFootprintPro PulseFootprintPro(bool hideCandles, bool showBottomTable, int footprintFontSize, int tableFontSize, int stackedImbalanceMinVolume, bool showStackedImbalances, int stackedImbalanceMinLevels, int stackedImbalanceRatioPercent, bool stackedImbalanceIgnoreZeroValues)
	{
		return indicator.PulseFootprintPro(((NinjaScriptBase)this).Input, hideCandles, showBottomTable, footprintFontSize, tableFontSize, stackedImbalanceMinVolume, showStackedImbalances, stackedImbalanceMinLevels, stackedImbalanceRatioPercent, stackedImbalanceIgnoreZeroValues);
	}

	public PulseFootprintPro PulseFootprintPro(ISeries<double> input, bool hideCandles, bool showBottomTable, int footprintFontSize, int tableFontSize, int stackedImbalanceMinVolume, bool showStackedImbalances, int stackedImbalanceMinLevels, int stackedImbalanceRatioPercent, bool stackedImbalanceIgnoreZeroValues)
	{
		return indicator.PulseFootprintPro(input, hideCandles, showBottomTable, footprintFontSize, tableFontSize, stackedImbalanceMinVolume, showStackedImbalances, stackedImbalanceMinLevels, stackedImbalanceRatioPercent, stackedImbalanceIgnoreZeroValues);
	}

	public PulseHeikin PulseHeikin()
	{
		return indicator.PulseHeikin(((NinjaScriptBase)this).Input);
	}

	public PulseHeikin PulseHeikin(ISeries<double> input)
	{
		return indicator.PulseHeikin(input);
	}

	public PulseDailyLevels PulseDailyLevels(int openingRangeMinutes, int initialBalanceMinutes, bool showOvernight, bool showSessionOpen, bool showOpeningRange, bool showInitialBalance, bool showPreviousDay, int levelTextSize, int rightMarginPx)
	{
		return indicator.PulseDailyLevels(((NinjaScriptBase)this).Input, openingRangeMinutes, initialBalanceMinutes, showOvernight, showSessionOpen, showOpeningRange, showInitialBalance, showPreviousDay, levelTextSize, rightMarginPx);
	}

	public PulseDailyLevels PulseDailyLevels(ISeries<double> input, int openingRangeMinutes, int initialBalanceMinutes, bool showOvernight, bool showSessionOpen, bool showOpeningRange, bool showInitialBalance, bool showPreviousDay, int levelTextSize, int rightMarginPx)
	{
		return indicator.PulseDailyLevels(input, openingRangeMinutes, initialBalanceMinutes, showOvernight, showSessionOpen, showOpeningRange, showInitialBalance, showPreviousDay, levelTextSize, rightMarginPx);
	}

	public PulseStackedImbalances PulseStackedImbalances(bool ignoreZeroValues, int imbalanceRatio, int imbalanceRange, int imbalanceVolume, bool lineTillTouch, Color askImbalanceColor, Color bidImbalanceColor, int lineWidth, int printLineForXBars, int daysLookBack, double stackedImbalanceOpacity, bool enableHistoricalReconstruction)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return indicator.PulseStackedImbalances(((NinjaScriptBase)this).Input, ignoreZeroValues, imbalanceRatio, imbalanceRange, imbalanceVolume, lineTillTouch, askImbalanceColor, bidImbalanceColor, lineWidth, printLineForXBars, daysLookBack, stackedImbalanceOpacity, enableHistoricalReconstruction);
	}

	public PulseStackedImbalances PulseStackedImbalances(ISeries<double> input, bool ignoreZeroValues, int imbalanceRatio, int imbalanceRange, int imbalanceVolume, bool lineTillTouch, Color askImbalanceColor, Color bidImbalanceColor, int lineWidth, int printLineForXBars, int daysLookBack, double stackedImbalanceOpacity, bool enableHistoricalReconstruction)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		return indicator.PulseStackedImbalances(input, ignoreZeroValues, imbalanceRatio, imbalanceRange, imbalanceVolume, lineTillTouch, askImbalanceColor, bidImbalanceColor, lineWidth, printLineForXBars, daysLookBack, stackedImbalanceOpacity, enableHistoricalReconstruction);
	}

	public PulseTPO PulseTPO(bool drawPOCBool, bool drawVirginPOCBool, bool drawTPOLetters, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush tPOTextColor, int tPOFontSize, int tPOLetterSpacing, bool drawDeltaProfile, int deltaProfileWidth, int deltaTickCompression, int deltaProfileOpacity, Brush deltaPositiveColor, Brush deltaNegativeColor)
	{
		return indicator.PulseTPO(((NinjaScriptBase)this).Input, drawPOCBool, drawVirginPOCBool, drawTPOLetters, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, tPOTextColor, tPOFontSize, tPOLetterSpacing, drawDeltaProfile, deltaProfileWidth, deltaTickCompression, deltaProfileOpacity, deltaPositiveColor, deltaNegativeColor);
	}

	public PulseTPO PulseTPO(ISeries<double> input, bool drawPOCBool, bool drawVirginPOCBool, bool drawTPOLetters, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush tPOTextColor, int tPOFontSize, int tPOLetterSpacing, bool drawDeltaProfile, int deltaProfileWidth, int deltaTickCompression, int deltaProfileOpacity, Brush deltaPositiveColor, Brush deltaNegativeColor)
	{
		return indicator.PulseTPO(input, drawPOCBool, drawVirginPOCBool, drawTPOLetters, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, tPOTextColor, tPOFontSize, tPOLetterSpacing, drawDeltaProfile, deltaProfileWidth, deltaTickCompression, deltaProfileOpacity, deltaPositiveColor, deltaNegativeColor);
	}

	public PulseVolumeProfileLite PulseVolumeProfileLite(bool drawPOCBool, bool drawVirginPOCBool, bool drawVolumeHistogramBool, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush volumeHistogramBorderColor, Stroke volumeHistogramStroke)
	{
		return indicator.PulseVolumeProfileLite(((NinjaScriptBase)this).Input, drawPOCBool, drawVirginPOCBool, drawVolumeHistogramBool, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, volumeHistogramBorderColor, volumeHistogramStroke);
	}

	public PulseVolumeProfileLite PulseVolumeProfileLite(ISeries<double> input, bool drawPOCBool, bool drawVirginPOCBool, bool drawVolumeHistogramBool, Stroke pOCStroke, Stroke pOCVAHStroke, Stroke pOCVALStroke, int valueAreaSize, Stroke virginPOCStroke, Stroke virginPOCVAHStroke, Stroke virginPOCVALStroke, Brush volumeHistogramBorderColor, Stroke volumeHistogramStroke)
	{
		return indicator.PulseVolumeProfileLite(input, drawPOCBool, drawVirginPOCBool, drawVolumeHistogramBool, pOCStroke, pOCVAHStroke, pOCVALStroke, valueAreaSize, virginPOCStroke, virginPOCVAHStroke, virginPOCVALStroke, volumeHistogramBorderColor, volumeHistogramStroke);
	}

	public PulseVP PulseVP(int volumeProfileWidth, int volumeTickCompression, int volumeThreshold, bool showProfileBars, int daysToShow, bool hideCandles, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, bool showPOCLine, bool showVALines, bool extendLines, Stroke pOCStroke, Stroke vAHStroke, Stroke vALStroke, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showCompositeProfile, int compositeSessionsCount, bool compositeIncludeCurrentSession, int compositeProfileWidth, int compositeOpacity, bool compositeShowDelta, bool showLVNMarkers, int lVNMaxPercentOfPOC, int lVNMinProminencePercent, bool lVNIgnoreInsideValueArea, int lVNMinSeparationTicks, int lVNMaxPerSession, int lVNHeightTicks, int lVNOpacity, int tradingHours, int profilePeriod, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return indicator.PulseVP(((NinjaScriptBase)this).Input, volumeProfileWidth, volumeTickCompression, volumeThreshold, showProfileBars, daysToShow, hideCandles, valueAreaPercentage, profileOpacity, valueAreaOpacity, showPOCLine, showVALines, extendLines, pOCStroke, vAHStroke, vALStroke, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showCompositeProfile, compositeSessionsCount, compositeIncludeCurrentSession, compositeProfileWidth, compositeOpacity, compositeShowDelta, showLVNMarkers, lVNMaxPercentOfPOC, lVNMinProminencePercent, lVNIgnoreInsideValueArea, lVNMinSeparationTicks, lVNMaxPerSession, lVNHeightTicks, lVNOpacity, tradingHours, profilePeriod, rTHStartTime, rTHEndTime);
	}

	public PulseVP PulseVP(ISeries<double> input, int volumeProfileWidth, int volumeTickCompression, int volumeThreshold, bool showProfileBars, int daysToShow, bool hideCandles, int valueAreaPercentage, int profileOpacity, int valueAreaOpacity, bool showPOCLine, bool showVALines, bool extendLines, Stroke pOCStroke, Stroke vAHStroke, Stroke vALStroke, bool showDeltaBars, int deltaProfileWidth, int deltaThreshold, int deltaOpacity, bool showCompositeProfile, int compositeSessionsCount, bool compositeIncludeCurrentSession, int compositeProfileWidth, int compositeOpacity, bool compositeShowDelta, bool showLVNMarkers, int lVNMaxPercentOfPOC, int lVNMinProminencePercent, bool lVNIgnoreInsideValueArea, int lVNMinSeparationTicks, int lVNMaxPerSession, int lVNHeightTicks, int lVNOpacity, int tradingHours, int profilePeriod, DateTime rTHStartTime, DateTime rTHEndTime)
	{
		return indicator.PulseVP(input, volumeProfileWidth, volumeTickCompression, volumeThreshold, showProfileBars, daysToShow, hideCandles, valueAreaPercentage, profileOpacity, valueAreaOpacity, showPOCLine, showVALines, extendLines, pOCStroke, vAHStroke, vALStroke, showDeltaBars, deltaProfileWidth, deltaThreshold, deltaOpacity, showCompositeProfile, compositeSessionsCount, compositeIncludeCurrentSession, compositeProfileWidth, compositeOpacity, compositeShowDelta, showLVNMarkers, lVNMaxPercentOfPOC, lVNMinProminencePercent, lVNIgnoreInsideValueArea, lVNMinSeparationTicks, lVNMaxPerSession, lVNHeightTicks, lVNOpacity, tradingHours, profilePeriod, rTHStartTime, rTHEndTime);
	}

	public PulseVWAP PulseVWAP(PulseVWAPResetPeriod resetPeriod, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, bool showPreviousVWAP, int levelTextSize)
	{
		return indicator.PulseVWAP(((NinjaScriptBase)this).Input, resetPeriod, showStandardDeviations, sD1Multiplier, sD2Multiplier, showPreviousVWAP, levelTextSize);
	}

	public PulseVWAP PulseVWAP(ISeries<double> input, PulseVWAPResetPeriod resetPeriod, bool showStandardDeviations, double sD1Multiplier, double sD2Multiplier, bool showPreviousVWAP, int levelTextSize)
	{
		return indicator.PulseVWAP(input, resetPeriod, showStandardDeviations, sD1Multiplier, sD2Multiplier, showPreviousVWAP, levelTextSize);
	}

	public PulseWeeklyLevels PulseWeeklyLevels(int weeklyIBDurationHours, int valueAreaPercent, int levelTextSize, bool showWeeklyIB, bool showWIB50, bool showWIB100, bool showWIB150, bool showWIB200, bool showWeekOpen, bool showWeekMid, bool showWeekVA, bool showWeekVWAP, bool showPriorWeekHLC, bool showPriorWeekOpen, bool showPriorWeekMid, bool showPriorWeekVA)
	{
		return indicator.PulseWeeklyLevels(((NinjaScriptBase)this).Input, weeklyIBDurationHours, valueAreaPercent, levelTextSize, showWeeklyIB, showWIB50, showWIB100, showWIB150, showWIB200, showWeekOpen, showWeekMid, showWeekVA, showWeekVWAP, showPriorWeekHLC, showPriorWeekOpen, showPriorWeekMid, showPriorWeekVA);
	}

	public PulseWeeklyLevels PulseWeeklyLevels(ISeries<double> input, int weeklyIBDurationHours, int valueAreaPercent, int levelTextSize, bool showWeeklyIB, bool showWIB50, bool showWIB100, bool showWIB150, bool showWIB200, bool showWeekOpen, bool showWeekMid, bool showWeekVA, bool showWeekVWAP, bool showPriorWeekHLC, bool showPriorWeekOpen, bool showPriorWeekMid, bool showPriorWeekVA)
	{
		return indicator.PulseWeeklyLevels(input, weeklyIBDurationHours, valueAreaPercent, levelTextSize, showWeeklyIB, showWIB50, showWIB100, showWIB150, showWIB200, showWeekOpen, showWeekMid, showWeekVA, showWeekVWAP, showPriorWeekHLC, showPriorWeekOpen, showPriorWeekMid, showPriorWeekVA);
	}
}
