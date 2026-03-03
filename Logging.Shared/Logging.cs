using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Exceptions;
using Serilog.Sinks.OpenTelemetry;

namespace Logging.Shared;

public static class Logging
{
    public static Action<HostBuilderContext, LoggerConfiguration> ConfigureLogging =>
        (builderContext, loggerConfiguration) =>
        {
            var environment = builderContext.HostingEnvironment;

            loggerConfiguration
                .ReadFrom.Configuration(builderContext.Configuration) //appsettingden oxusun deye configleri
                .Enrich.FromLogContext() //loglara elave datalar yazmaq ucun (trace id elave edeceyik)
                .Enrich.WithExceptionDetails() //xetalarin detaylari ucun
                .Enrich.WithProperty("Env", environment.EnvironmentName)
                .Enrich.WithProperty("AppName", environment.ApplicationName)
                .Enrich.WithOpenTelemetryTraceId() //otl collector qosmusamsa trace id de lave edilsin deye her loga
                .Enrich.WithOpenTelemetrySpanId(); //otl collector qosmusamsa span id de lave edilsin deye her loga

            //var elasticsearchBaseUrl = builderContext.Configuration.GetSection("Elasticsearch")["BaseUrl"];
            var userName = builderContext.Configuration.GetSection("Elasticsearch")["UserName"];
            var password = builderContext.Configuration.GetSection("Elasticsearch")["Password"];
            var indexName = builderContext.Configuration.GetSection("Elasticsearch")["IndexName"];

            // loggerConfiguration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchBaseUrl))
            // {
            //     AutoRegisterTemplate = true,
            //     AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8, //elk versiyonu
            //     IndexFormat =
            //         $"{indexName}-{environment.EnvironmentName}-logs-" +
            //         "{0:yyy.MM.dd}", //elka yaranacaq gunluk index bucket'imiz
            //     ModifyConnectionSettings = x => x.BasicAuthentication(userName, password),
            //     CustomFormatter = new ElasticsearchJsonFormatter()
            // }).WriteTo.Console();

            //"Serilog.Sinks.Elasticsearch depricate olub, evezine Elastic.Serilog.Sinks cunki elastick 8.x sorasinda bu pack isleyir.

            var nodes = builderContext.Configuration
                .GetSection("Elasticsearch:Nodes")
                .Get<string[]>()!
                .Select(x => new Uri(x))
                .ToArray();

            loggerConfiguration
                // .WriteTo
                // .Elasticsearch(
                //     nodes,
                //     opts =>
                //     {
                //         opts.DataStream = new DataStreamName(
                //             "logs", // Type: logs/metrics/traces
                //             indexName!,
                //             environment.EnvironmentName
                //         );
                //
                //         //nece gun sora silinsin policy vere bilerem burda, default logs 30 gun ya da 50gb kecende silir.
                //         opts.IlmPolicy =
                //             "logs-retention-5-days"; //bu qosulmur burdan, default logs polici isleyir, garax elle deyisesen ya da datastreamda oz templeatini yaradib onu versense, logs hazirdi deye problem cixardir.
                //
                //         opts.BootstrapMethod = BootstrapMethod.Silent;
                //     },
                //     transport =>
                //     {
                //         transport.Authentication(
                //             new BasicAuthentication(userName!, password!)
                //         );
                //     }) //elatice bir basa log gonderme
                .WriteTo.Console()
                .WriteTo.OpenTelemetry(options =>
                {
                    //options.Endpoint = "http://otel-collector:4317"; //docker daxilinde ms qalxsa
                    options.Endpoint = "http://localhost:4317";
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = environment.ApplicationName,
                        ["service.version"] = "1.0.0"
                    };

                    options.OnBeginSuppressInstrumentation =
                        SuppressInstrumentationScope
                            .Begin; //infinity loop olasin deye insturmentleri suppress/ bloklayir, esasen gRPC ile gonderrik deye ve gRPC insturmetde qurlu olsa loop yarana biler meselen

                    // options.IncludedData =
                    //     IncludedData.TraceIdField |
                    //     IncludedData.SpanIdField |
                    //     IncludedData.MessageTemplateTextAttribute;
                }); //otl collector uzerinden elk gedir loglar

            // PUT _ilm/policy/logs-retention-5-days
            // {
            //     "policy": {
            //         "phases": {
            //             "hot": {
            //                 "actions": {
            //                     "rollover": {
            //                         "max_age": "1d"
            //                     }
            //                 }
            //             },
            //             "delete": {
            //                 "min_age": "5d",
            //                 "actions": {
            //                     "delete": {}
            //                 }
            //             }
            //         }
            //     }
            // } //bu elk yazirsan ki, 5 gunden cox saxlamasin, default olan 50gb ve ya 30 gun olanda yeni
            // index/stream yaradir silme yoxdu. bu hem otl hem bir basa elk log gonderende eyni cur isleyir.
            //bunu jeager'in tracelerin elk saxlayanda da elemek olar.
            
            // PUT .ds-logs-order.api-development-*/_settings
            // {
            //     "index.lifecycle.name": "logs-retention-5-days"
            // }
        };

    public static void AddOpenTelemetryLog(this WebApplicationBuilder builder) //otl ile loglari yigmaq ucun
    {
        // builder.Logging.AddOpenTelemetry(cfg =>
        // {
        //     var serviceName = builder.Configuration.GetSection("OpenTelemetry")["ServiceName"];
        //     var serviceVersion = builder.Configuration.GetSection("OpenTelemetry")["ServiceVersion"];
        //
        //     cfg.SetResourceBuilder(ResourceBuilder.CreateDefault()
        //         .AddService(serviceName!, serviceVersion: serviceVersion));
        //     cfg.AddOtlpExporter();
        // });

        //"OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs" -> OpenTelemetry.Exporter.OpenTelemetryProtocol depricate olub pack

        builder.Logging.AddOpenTelemetry(options =>
        {
            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"];
            var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"];
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

            options.SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName!, serviceVersion: serviceVersion)
            );

            options.AddOtlpExporter(otlp => { otlp.Endpoint = new Uri(otlpEndpoint!); });
        });
    }
}