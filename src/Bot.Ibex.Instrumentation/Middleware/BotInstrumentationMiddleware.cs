namespace Bot.Ibex.Instrumentation.Middleware
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Bot.Ibex.Instrumentation.Telemetry;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;

    public class BotInstrumentationMiddleware : IMiddleware
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Settings settings;

        public BotInstrumentationMiddleware(TelemetryClient telemetryClient, Settings settings)
        {
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task OnTurnAsync(
            ITurnContext turnContext,
            NextDelegate next,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ContextNotNull(turnContext);

            if (turnContext.Activity != null)
            {
                var et = this.BuildEventTelemetry(turnContext.Activity);
                this.telemetryClient.TrackEvent(et);
            }

            // hook up onSend pipeline
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                var responses = await nextSend().ConfigureAwait(false);

                foreach (var activity in activities)
                {
                    var et = this.BuildEventTelemetry(activity);
                    this.telemetryClient.TrackEvent(et);
                }

                return responses;
            });

            // hook up update activity pipeline
            turnContext.OnUpdateActivity(async (ctx, activity, nextUpdate) =>
            {
                var response = await nextUpdate().ConfigureAwait(false);

                var et = this.BuildEventTelemetry(activity);
                this.telemetryClient.TrackEvent(et);

                return response;
            });

            if (next != null)
            {
                await next(cancellationToken).ConfigureAwait(false);
            }
        }

        private EventTelemetry BuildEventTelemetry(IActivity activity)
        {
            var builder = new EventTelemetryBuilder(activity, this.settings);
            return builder.Build();
        }
    }
}