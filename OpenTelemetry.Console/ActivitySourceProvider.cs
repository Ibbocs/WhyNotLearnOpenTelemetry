using System.Diagnostics;

namespace OpenTelemetry.Console;

public static class ActivitySourceProvider
{
    public static ActivitySource Source = new ActivitySource(OpenTelemetryConstants.ActivitySourceName);
    public static ActivitySource SourceFile = new ActivitySource(OpenTelemetryConstants.ActivitySourceName);
}