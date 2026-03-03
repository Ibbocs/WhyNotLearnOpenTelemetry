using System.Diagnostics;

namespace OpenTelemetry.Console;

public class ServiceGoogle
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public async Task<int> MakeRequestGoogleAsync()
    {
        using var activity = ActivitySourceProvider.Source.StartActivity();
        using var activity2 = ActivitySourceProvider.SourceFile.StartActivity();
        Activity.Current?.AddTag("Salam", "sagol");
        
        var tags = new ActivityTagsCollection();
        tags.Add("userId", "20");
        activity?.AddEvent(new ActivityEvent("google", tags: tags));
        //activity?.SetStatus(ActivityStatusCode.Error, "nullable description"); //description yalniz error olanda gorunur
        activity?.SetStatus(ActivityStatusCode.Ok, "nullable description"); //description yalniz error olanda gorunur
        
        activity?.AddTag("user.id", "20");
        
        var result = await HttpClient.GetAsync("https://www.google.com");
        var content = await result.Content.ReadAsStringAsync();
        return content.Length;
    }
}