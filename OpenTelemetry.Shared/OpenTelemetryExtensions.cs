using System.Diagnostics;
using MassTransit.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Shared;

public static class OpenTelemetryExtensions
{
    public static void AddOpenTelemetryExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenTelemetryConstants>(configuration.GetSection("OpenTelemetry"));
        var openTelemetryConstants = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConstants>()!;

        ActivitySourceProvider.Source =
            new ActivitySource(openTelemetryConstants.ActivitySourceName);

        services.AddOpenTelemetry().WithTracing(options => //trace basqa metric de yiga bilerik
        {
            options.AddSource(openTelemetryConstants.ActivitySourceName)
                .AddSource(DiagnosticHeaders.DefaultListenerName) //masstransit ucun trace source
                .ConfigureResource(resource =>
                {
                    resource.AddService(openTelemetryConstants.ServiceName,
                        serviceVersion: openTelemetryConstants.ServiceVersion);
                }); //bizim oz source'miz

            options.AddAspNetCoreInstrumentation(aspnetcoreOptions =>
            {
                aspnetcoreOptions.Filter = context =>
                {
                    if (!string.IsNullOrEmpty(context.Request.Path.Value))
                        return context.Request.Path.Value.Contains("api", StringComparison.InvariantCulture);

                    return false;
                }; //her seyi deyilde yalniz path'inde api olanlari trace elesin


                aspnetcoreOptions.RecordException =
                    true; // error'lari detayli almaq ucun amma loglama varsa bunu acmayada bilerik cunki elave datadi bu


                // Bilerek boş bırakıldı. Örnek göstermek için
                // Burda elave datalar yazmaq istesen islede bilersen bu insturmentin traclerine
                aspnetcoreOptions.EnrichWithException = (activity, exception) => { };

                aspnetcoreOptions.EnrichWithHttpRequest = (activity, request) => { };

                aspnetcoreOptions.EnrichWithHttpResponse = (activity, response) => { };
            });

            options.AddEntityFrameworkCoreInstrumentation(efcoreOptions =>
            {
                // efcoreOptions.SetDbStatementForText = true; //decripted cunki sql cumlsein bir basa saxlamaq risklidi ve performansi asagi salir
                // efcoreOptions.SetDbStatementForStoredProcedure = true; //deripted 
                efcoreOptions.EnrichWithIDbCommand = (activity, dbCommand) =>
                {
                    // Bilerek boş bırakıldı. Örnek göstermek için.
                    // Burda elave datalar yazmaq istesen islede bilersen bu insturmentin traclerine

                    activity.SetTag("db.statement",
                        dbCommand
                            .CommandText); //yuxarda decript olan seyleri manuel edirem, amma burda filterler qoymaq lazimdi, her seyi yazmamaq ucun
                    activity.SetTag("db.operation", dbCommand.CommandType.ToString());
                };
            });

            options.AddConsoleExporter();
            //options.AddOtlpExporter();

            options.AddOtlpExporter(otlpOptions => //default deyerler olmadan
            {
                otlpOptions.Endpoint = new Uri("http://localhost:4317"); //local
                //otlpOptions.Endpoint = new Uri("http://otel-collector:4317"); // docker collector
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                otlpOptions.BatchExportProcessorOptions.MaxQueueSize =
                    2048; // ram bu qeder data saxlaya biler jaeger gondermek ucun, ram dolarsa drop edir datani
                otlpOptions.BatchExportProcessorOptions.ScheduledDelayMilliseconds =
                    5000; //5 san bir jaegere data'lari gonderecek

                //otl bir basa jaeger data gondermir, cunki performan ve network problemi yarada biler, 
                //bunun yerine bir queue yigir datalari sora gonderir.
            }); //jaeger qosandan ve ya otl collector uzerinden jaeger gondermek istesen yene bu configler olur.

            options.AddHttpClientInstrumentation(httpOptions =>
            {
                // httpOptions.FilterHttpRequestMessage = request =>
                // {
                //     return !request.RequestUri.AbsoluteUri.Contains("9200", StringComparison.InvariantCulture);
                // }; //elk geden sorgulari bagladi loglar gedir deye, amma men de elk islediremse burda ferqli yanasma elemeli ola bilerem.

                httpOptions.EnrichWithHttpRequestMessage = async void (activity, request) =>
                {
                    var requestContent = "empty";

                    if (request.Content != null) requestContent = await request.Content.ReadAsStringAsync();


                    activity.SetTag("http.request.body", requestContent);
                }; //burda tekce body yaziriq, bunu genislendire bilerik

                httpOptions.EnrichWithHttpResponseMessage = async void (activity, response) =>
                {
                    if (response.Content != null)
                        activity.SetTag("http.response.body", await response.Content.ReadAsStringAsync());
                }; //burda tekce body yaziriq, bunu genislendire bilerik

                //bu hemde bizim yazdigimiz middilware ile bir nov eyni isi gormus, hetta dublicate data yigilmagina sebeb veriri,
                //amma middilware olmayan yerlerde ve ya 3rd party api sorgu atsaq uje orda bu is gorecek, o gore middilware yazmayib,
                //burda ede bilerik http requestler ucun her seyi, amma unutma ki bu yalniz HttpClient trace edir, yeni frontdan gelen requestin
                //bodysi falan lazimdirsa o middilware de yazmaliyiq sadece duzgun config edirb daxili parametre falan esasen yalniz
                //frontdan ve ya basqa yerden bize gelen sorgulari trace etdire bilerik orda.

                httpOptions.RecordException = true;
            });

            options.AddRedisInstrumentation(redisOptions => { redisOptions.SetVerboseDatabaseStatements = true; });
        });
    }
}