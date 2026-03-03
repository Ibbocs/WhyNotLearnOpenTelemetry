using System.Diagnostics;

namespace OpenTelemetry.Console;

public class ServiceHelper
{
    public void Work()
    {
        using var activity =
            ActivitySourceProvider.Source.StartActivity(kind: System.Diagnostics.ActivityKind.Server, name: "Test");

        System.Console.WriteLine("Test");

        var httpService = new ServiceGoogle();
        httpService.MakeRequestGoogleAsync().GetAwaiter().GetResult(); // bir trace altinda ikinci span
    }
}