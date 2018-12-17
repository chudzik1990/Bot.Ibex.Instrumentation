namespace Bot.Ibex.Instrumentation.Instrumentations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Telemetry;

    public class BotInstrumentation : IMiddleware
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Settings settings;

        public BotInstrumentation(TelemetryClient telemetryClient, Settings settings)
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
            var et = new EventTelemetry();
            if (activity.Timestamp != null)
            {
                et.Properties.Add(Constants.TimestampProperty, activity.Timestamp.Value.AsIso8601());
            }

            et.Properties.Add(Constants.TypeProperty, activity.Type);
            et.Properties.Add(Constants.ChannelProperty, activity.ChannelId);

            switch (activity.Type)
            {
                case ActivityTypes.Message:
                    var messageActivity = activity.AsMessageActivity();
                    if (activity.ReplyToId == null)
                    {
                        et.Name = EventTypes.MessageReceived;
                        et.Properties.Add(Constants.UserIdProperty, activity.From.Id);
                        if (!this.settings.OmitUsernameFromTelemetry)
                        {
                            et.Properties.Add(Constants.UserNameProperty, activity.From.Name);
                        }
                    }
                    else
                    {
                        et.Name = EventTypes.MessageSent;
                    }

                    et.Properties.Add(Constants.TextProperty, messageActivity.Text);
                    et.Properties.Add(Constants.ConversationIdProperty, messageActivity.Conversation.Id);
                    break;
                case ActivityTypes.ConversationUpdate:
                    et.Name = EventTypes.ConversationUpdate;
                    break;
                case ActivityTypes.EndOfConversation:
                    et.Name = EventTypes.ConversationEnded;
                    break;
                default:
                    et.Name = EventTypes.OtherActivity;
                    break;
            }

            return et;
        }
    }
}