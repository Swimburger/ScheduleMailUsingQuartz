```bash
mkdir ScheduleMailUsingQuartz
cd ScheduleMailUsingQuartz

dotnet new worker
dotnet run
```


Add Quartz
```bash
dotnet add package Quartz
dotnet add package Quartz.Extensions.Hosting
```

Update code
```csharp hl_lines="1 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 29 30 31 32 33 34 35 36 37 38 39 40 41 42"
using Quartz;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.ScheduleJob<SendMailJob>(trigger => trigger
                .WithIdentity("SendRecurringMailTrigger")
                .WithSimpleSchedule(s =>
                    s.WithIntervalInSeconds(15)
                    .RepeatForever()
                )
                .WithDescription("This trigger will run every 15 seconds to send emails.")
            );
        });

        services.AddQuartzHostedService(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });
    })
    .Build();

await host.RunAsync();

class SendMailJob : IJob
{
    private readonly ILogger logger;

    public SendMailJob(ILogger<SendMailJob> logger)
    {
        this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Greetings from SendMailJob!");
    }
}
```


Add SendGrid
```bash
dotnet add package SendGrid
dotnet add package SendGrid.Extensions.DependencyInjection
```

Update code
```csharp hl_lines="2 3 4 7 9 10 37 40 42 48 49 50 51 52 53 54 55 56 57 58 59 60 61 62"
using Quartz;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;
using SendGrid.Helpers.Mail;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSendGrid(options =>
            options.ApiKey = context.Configuration.GetValue<string>("SendGridApiKey"));

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.ScheduleJob<SendMailJob>(trigger => trigger
                .WithIdentity("SendRecurringMailTrigger")
                .WithSimpleSchedule(s =>
                    s.WithIntervalInSeconds(15)
                    .RepeatForever()
                )
                .WithDescription("This trigger will run every 15 seconds to send emails.")
            );
        });

        services.AddQuartzHostedService(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });
    })
    .Build();

await host.RunAsync();

class SendMailJob : IJob
{
    private readonly ISendGridClient sendGridClient;
    private readonly ILogger logger;

    public SendMailJob(ISendGridClient sendGridClient, ILogger<SendMailJob> logger)
    {
        this.sendGridClient = sendGridClient;
        this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress("[REPLACE WITH YOUR EMAIL]", "[REPLACE WITH YOUR NAME]"),
            Subject = "Sending with Twilio SendGrid is Fun",
            PlainTextContent = "and easy to do anywhere, especially with C# .NET"
        };
        msg.AddTo(new EmailAddress("[REPLACE WITH DESIRED TO EMAIL]", "[REPLACE WITH DESIRED TO NAME]"));
        var response = await sendGridClient.SendEmailAsync(msg);

        // A success status code means SendGrid received the email request and will process it.
        // Errors can still occur when SendGrid tries to send the email. 
        // If email is not received, use this URL to debug: https://app.sendgrid.com/email_activity 
        logger.LogInformation(response.IsSuccessStatusCode ? "Email queued successfully!" : "Something went wrong!");
    }
}
```

Configure API key:
```bash
dotnet add package Microsoft.Extensions.Configuration.UserSecrets

dotnet user-secrets init
dotnet user-secrets set SendGridApiKey [REPLACE_WITH_YOUR_API_KEY]
```