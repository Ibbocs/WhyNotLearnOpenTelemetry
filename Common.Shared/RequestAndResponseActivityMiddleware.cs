using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace Common.Shared;

public class RequestAndResponseActivityMiddleware
{
    private readonly RequestDelegate _next;

    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager; //daha performansli, gb yukunu azaldaraq memory idare elemek ucun

    public RequestAndResponseActivityMiddleware(RequestDelegate next)
    {
        _next = next;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }


    public async Task InvokeAsync(HttpContext context)
    {
        await AddRequestBodyContentToActivityTags(context);
        await AddResponseBodyContentToActivityTags(context);
    }


    private async Task AddRequestBodyContentToActivityTags(HttpContext context)
    {
        context.Request.EnableBuffering();//memeory gore buffer edirik, performans ucun
        var requestBodyStreamReader = new StreamReader(context.Request.Body);
        var requestBodyContent = await requestBodyStreamReader.ReadToEndAsync();
        Activity.Current?.SetTag("http.request.body", requestBodyContent);
        context.Request.Body.Position = 0; // proseslerin davaminda problem olmasin deye basa qaytaririq oxuma prosesin
        
        //bu meslecun query parami oxumur, tekce body
    }


    private async Task AddResponseBodyContentToActivityTags(HttpContext context)
    {
        var originalResponse = context.Response.Body;

        await using var responseBodyMemoryStream = _recyclableMemoryStreamManager.GetStream();
        context.Response.Body = responseBodyMemoryStream;


        await _next(context);

        responseBodyMemoryStream.Position = 0;

        var responseBodyStreamReader = new StreamReader(responseBodyMemoryStream);
        var responseBodyContent = await responseBodyStreamReader.ReadToEndAsync();
        Activity.Current?.SetTag("http.response.body", responseBodyContent);
        responseBodyMemoryStream.Position = 0;
        await responseBodyMemoryStream.CopyToAsync(originalResponse);
        
        //body dolayli yoldan bir memory stream cekib onun ustunden oxuyuruq, sora orginala copy edirik, bir basa ozunden oxumaq istesek alinmayacaq, cunki basqa yerlerde isledil biliner
        //umumiyyetle her body/request oxumaliyiqmi ya yox onun qerarin duz vermek lazimid, cunki fin, password kimi datalar oxuyub harasa yazmaq duz olmaz
    }
}