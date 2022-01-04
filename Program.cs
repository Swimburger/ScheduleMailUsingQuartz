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