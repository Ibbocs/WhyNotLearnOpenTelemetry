// See https://aka.ms/new-console-template for more information

using OpenTelemetry;
using OpenTelemetry.Console;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Console.WriteLine("Hello, World!");

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(OpenTelemetryConstants
        .ActivitySourceName) //hansi source qulaq asacagin dedik, vereceyimiz ad unique olmalidi esas
    //.AddSource("db prosesleri ucun activity source ve s.)
    .ConfigureResource(configure =>
    {
        configure.AddService(OpenTelemetryConstants.ServiceName, serviceVersion: OpenTelemetryConstants.ServiceVersion)
            .AddAttributes(new List<KeyValuePair<string, object>>
            {
                new("host.machineName", Environment.MachineName),
                new("host.environment", "dev")
            });
    })
    .AddConsoleExporter()
    .AddOtlpExporter()
    .Build(); //host.machineName - bu standartdi adlandirmada


var serviceHelper = new ServiceHelper();
serviceHelper.Work();