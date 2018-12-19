# Bot.Ibex.Instrumentation

[![Build Status](https://ci.appveyor.com/api/projects/status/github/ObjectivityLtd/Bot.Ibex.Instrumentation?branch=master&svg=true)](https://ci.appveyor.com/project/ObjectivityAdminsTeam/bot-ibex-instrumentation) [![Tests Status](https://img.shields.io/appveyor/tests/ObjectivityAdminsTeam/bot-ibex-instrumentation/master.svg)](https://ci.appveyor.com/project/ObjectivityAdminsTeam/bot-ibex-instrumentation) [![codecov](https://codecov.io/gh/ObjectivityLtd/Bot.Ibex.Instrumentation/branch/master/graph/badge.svg)](https://codecov.io/gh/ObjectivityLtd/Bot.Ibex.Instrumentation)   [![nuget](https://img.shields.io/nuget/v/Bot.Ibex.Instrumentation.svg) ![Downloads](https://img.shields.io/nuget/dt/Bot.Ibex.Instrumentation.svg)](https://www.nuget.org/packages/Bot.Ibex.Instrumentation/) [![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://opensource.org/licenses/MIT)

Simplifies adding custom analytics for bots built with [Microsoft Bot Framework V4](https://dev.botframework.com) to leverage it with [Ibex Dashboard](https://github.com/Azure/ibex-dashboard).

## Instrumentations

### BotInstrumentation

Provides general bot instrumentation.

#### Example

```csharp
services.AddBot<BasicBot>(options =>
{
    var telemetryConfig = new TelemetryConfiguration("<INSTRUMENTATION_KEY>");
    var telemetryClient = new TelemetryClient(telemetryConfig);
    var instrumentation = new BotInstrumentation(
        telemetryClient,
        new Settings {
            OmitUsernameFromTelemetry = true
        });
    options.Middleware.Add(instrumentation);
});
```

* `<INSTRUMENTATION_KEY>` is an instrumentation key of Application Insights to be obtaned once it is configured in Azure.

### QnAInstrumentation

Provides QnA Maker instrumentation.

#### Example

##### Setup

```csharp
services.AddBot<BasicBot>(options =>
{
    var telemetryConfig = new TelemetryConfiguration("<INSTRUMENTATION_KEY>");
    var telemetryClient = new TelemetryClient(telemetryConfig);
    var instrumentation = new QnAInstrumentation(
        telemetryClient,
        new Settings {
            OmitUsernameFromTelemetry = false
        });
    services.AddSingleton<IQnAInstrumentation>(instrumentation);
});
```

* `<INSTRUMENTATION_KEY>` is an instrumentation key of Application Insights to be obtaned once it is configured in Azure.

##### Usage

```csharp
private async Task DispatchToQnAMakerAsync(
    BotServices services,
    string serviceName,
    IQnAInstrumentation instrumentation,
    ITurnContext context,
    CancellationToken cancellationToken = default(CancellationToken))
{
    if (!string.IsNullOrEmpty(context.Activity.Text))
    {
        var qna = services.QnAServices[serviceName];
        var results = await qna.GetAnswersAsync(context)
            .ConfigureAwait(false);

        if (results.Any())
        {
            var result = results.First();
            instrumentation.TrackEvent(context.Activity, result);
            await context.SendActivityAsync(result.Answer, cancellationToken: cancellationToken);
        }
        else
        {
            var message = $"Couldn't find an answer in the {serviceName}.";
            await context.SendActivityAsync(message, cancellationToken: cancellationToken);
        }
    }
}
```