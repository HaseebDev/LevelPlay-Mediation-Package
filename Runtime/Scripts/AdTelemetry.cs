using System;

namespace Autech.LevelPlay
{
    public enum AdTelemetryFormat { Rewarded, Interstitial, Banner }
    public enum AdTelemetryAction { Request, Loaded, LoadFailed, Displayed, DisplayFailed, Clicked, Closed, Rewarded }

    public readonly struct AdTelemetryEvent
    {
        public readonly AdTelemetryFormat Format;
        public readonly AdTelemetryAction Action;
        public readonly string Placement;
        public readonly int ErrorCode;
        public AdTelemetryEvent(AdTelemetryFormat format, AdTelemetryAction action, string placement, int errorCode = 0)
        { Format = format; Action = action; Placement = placement; ErrorCode = errorCode; }
    }

    // Observability only: listeners must never control ad behavior or reward settlement.
    public static class AdTelemetry
    {
        public static event Action<AdTelemetryEvent> Event;

        public static void Report(AdTelemetryFormat format, AdTelemetryAction action, string placement, int errorCode = 0)
        {
            var listeners = Event;
            if (listeners == null) return;
            var data = new AdTelemetryEvent(format, action, placement, errorCode);
            foreach (Action<AdTelemetryEvent> listener in listeners.GetInvocationList())
            {
                try { listener(data); }
                catch (Exception) { /* Analytics failures must not affect ads or other observers. */ }
            }
        }
    }
}
