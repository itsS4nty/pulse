# Pulse — NinjaTrader 8 Indicator Suite (white-label, license-free)

Recovered, rebranded, and license-free source for the indicator suite formerly
known as FlowEdge. Verified to **compile with 0 errors** against your NinjaTrader 8
assemblies.

## What's here

`Indicators/Pulse/` — 24 C# files:

**15 indicators**
- PulseAnchoredVolumeProfile, PulseAnchoredVWAP, PulseBigTrades,
  PulseCumulativeDelta, PulseDailyLevels, PulseDeltaProfile, PulseDivergences,
  PulseFootprintPro, PulseHeikin, PulseStackedImbalances, PulseTPO,
  PulseVolumeProfileLite, PulseVP, PulseVWAP, PulseWeeklyLevels

**9 supporting enum / helper type files**
- PulseVWAPResetPeriod, PulseAnchoredVWAPPriceSource, PulseBigTradesCircleStyle,
  PulseBigTradesDetectionMode, PulseCumulativeDeltaResetPeriod, PulseTPOEnums,
  PulseVPEnums, VPAlignment, ZigZagPivot

## Changes applied to the recovered source

1. **Rebrand FlowEdge -> Pulse** across everything: namespaces
   (`NinjaTrader.NinjaScript.Indicators.Pulse`), class names, type names, file
   names, and all UI strings (indicator `Name`, `Description`, labels, prints).
2. **Vendor licensing removed.** The only licensing mechanism was a single
   `VendorLicense(475L)` call in each indicator's constructor (vendor ID 475).
   Those 13 license-only constructors were removed entirely. No other gating,
   trial, or expiry logic existed in the code. The suite is now fully free /
   white-label.

Both changes were applied deterministically and the result was re-compiled to
**0 errors** against NinjaTrader.Core/Gui/Custom + SharpDX + WPF.

## Installing into NinjaTrader 8

1. Copy the contents of `Indicators/Pulse/` into:
   `Documents\NinjaTrader 8\bin\Custom\Indicators\Pulse\`
2. Open the **NinjaScript Editor** and press **F5** (Compile). NinjaTrader
   regenerates the wrapper/factory code automatically.
3. The indicators appear in the indicator list as **"Pulse ..."**.

## Notes

- Local variable names are generic (`num`, `num2`, …) — inherent to decompiling a
  DLL without a PDB. All field/property/method names, types, and logic are exact.
- The supporting enums (VPAlignment, PulseVWAPResetPeriod, …) are in the **global
  namespace**, as in the original. They work as-is; move them into the `Pulse`
  namespace later if you prefer.
- Do not add an old generated wrapper file — NinjaTrader regenerates it on compile.
