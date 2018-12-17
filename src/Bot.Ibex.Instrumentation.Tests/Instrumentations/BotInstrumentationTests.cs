namespace Bot.Ibex.Instrumentation.Tests.Instrumentations
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.Xunit2;
    using FluentAssertions;
    using Instrumentation.Extensions;
    using Instrumentation.Instrumentations;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Moq;
    using Telemetry;
    using Xunit;

    [Collection("BotInstrumentation")]
    [Trait("Category", "Instrumentations")]
    public class BotInstrumentationTests
    {
        private const string FakeInstrumentationKey = "FAKE-INSTRUMENTATION-KEY";
        private const string FakeActivityType = "FAKE-ACTIVITY-TYPE";
        private readonly Mock<ITelemetryChannel> mockTelemetryChannel = new Mock<ITelemetryChannel>();
        private readonly TelemetryClient telemetryClient;

        public BotInstrumentationTests()
        {
            var telemetryConfiguration = new TelemetryConfiguration(FakeInstrumentationKey, this.mockTelemetryChannel.Object);
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [Theory(DisplayName = "GIVEN turn context with activity type other than Message WHEN OnTurnAsync is invoked THEN even telemetry sent")]
        [InlineAutoData(ActivityTypes.ConversationUpdate, EventTypes.ConversationUpdate)]
        [InlineAutoData(ActivityTypes.EndOfConversation, EventTypes.ConversationEnded)]
        [InlineAutoData(FakeActivityType, EventTypes.OtherActivity)]
        public async void GivenTurnContextWithActivityTypeOtherThanMessage_WhenOnTurnAsyncIsInvoked_ThenEvenTelemetrySent(
            string activityType,
            string expectedTelemetryName,
            Settings settings,
            IFixture fixture)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = activityType,
                ChannelId = fixture.Create<string>(),
                Timestamp = DateTimeOffset.MinValue
            };
            turnContext.SetupGet(c => c.Activity).Returns(activity);
            const int expectedNumberOfTelemetryProperties = 3;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.TimestampProperty] == activity.Timestamp.Value.AsIso8601() &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN turn context with Message type activity and ReplyToId WHEN OnTurnAsync is invoked THEN event telemetry sent")]
        [AutoData]
        public async void GivenTurnContextWithMessageTypeActivityAndReplyToId_WhenOnTurnAsyncIsInvoked_ThenEvenTelemetrySent(
            Settings settings,
            IFixture fixture)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = fixture.Create<string>(),
                ReplyToId = fixture.Create<string>(),
                Text = fixture.Create<string>(),
                Conversation = new ConversationAccount { Id = fixture.Create<string>() }
            };
            turnContext.SetupGet(c => c.Activity).Returns(activity);
            const int expectedNumberOfTelemetryProperties = 4;
            const string expectedTelemetryName = EventTypes.MessageSent;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.TextProperty] == activity.Text &&
                t.Properties[Constants.ConversationIdProperty] == activity.Conversation.Id &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN turn context with Message type activity and omit username setting WHEN OnTurnAsync is invoked THEN event telemetry sent")]
        [AutoData]
        public async void GivenTurnContextWithMessageTypeActivityAndOmitUsernameSetting_WhenOnTurnAsyncIsInvoked_ThenEvenTelemetrySent(IFixture fixture)
        {
            // Arrange
            var settings = new Settings { OmitUsernameFromTelemetry = true };
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = fixture.Create<string>(),
                Text = fixture.Create<string>(),
                Conversation = new ConversationAccount { Id = fixture.Create<string>() },
                From = new ChannelAccount { Id = fixture.Create<string>() }
            };
            turnContext.SetupGet(c => c.Activity).Returns(activity);
            const int expectedNumberOfTelemetryProperties = 5;
            const string expectedTelemetryName = EventTypes.MessageReceived;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.TextProperty] == activity.Text &&
                t.Properties[Constants.UserIdProperty] == activity.From.Id &&
                t.Properties[Constants.ConversationIdProperty] == activity.Conversation.Id &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN turn context with Message type activity and no omit username setting WHEN OnTurnAsync is invoked THEN event telemetry sent")]
        [AutoData]
        public async void GivenTurnContextWithMessageTypeActivityAndNoOmitUsernameSetting_WhenOnTurnAsyncIsInvoked_ThenEvenTelemetrySent(IFixture fixture)
        {
            // Arrange
            var settings = new Settings { OmitUsernameFromTelemetry = false };
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = fixture.Create<string>(),
                Text = fixture.Create<string>(),
                Conversation = new ConversationAccount { Id = fixture.Create<string>() },
                From = new ChannelAccount { Id = fixture.Create<string>() }
            };
            turnContext.SetupGet(c => c.Activity).Returns(activity);
            const int expectedNumberOfTelemetryProperties = 6;
            const string expectedTelemetryName = EventTypes.MessageReceived;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.TextProperty] == activity.Text &&
                t.Properties[Constants.UserIdProperty] == activity.From.Id &&
                t.Properties[Constants.UserNameProperty] == activity.From.Name &&
                t.Properties[Constants.ConversationIdProperty] == activity.Conversation.Id &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN turn context WHEN SendActivities is invoked THEN event telemetry sent")]
        [AutoData]
        public async void GivenTurnContext_WhenSendActivitiesInvoked_ThenEvenTelemetrySent(
            Settings settings,
            IFixture fixture)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContextMock = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = fixture.Create<string>()
            };
            turnContextMock
                .Setup(c => c.OnSendActivities(It.IsAny<SendActivitiesHandler>()))
                .Callback<SendActivitiesHandler>(h => h(null, new List<Activity> { activity }, () => Task.FromResult(Array.Empty<ResourceResponse>())));
            const int expectedNumberOfTelemetryProperties = 2;
            const string expectedTelemetryName = EventTypes.ConversationUpdate;

            // Act
            await instrumentation.OnTurnAsync(turnContextMock.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN turn context WHEN UpdateActivity is invoked THEN event telemetry sent")]
        [AutoData]
        public async void GivenTurnContext_WhenUpdateActivityInvoked_ThenEvenTelemetrySent(
            Settings settings,
            IFixture fixture)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var activity = new Activity
            {
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = fixture.Create<string>()
            };
            turnContext
                .Setup(c => c.OnUpdateActivity(It.IsAny<UpdateActivityHandler>()))
                .Callback<UpdateActivityHandler>(h => h(null, activity, () => Task.FromResult((ResourceResponse)null)));
            const int expectedNumberOfTelemetryProperties = 2;
            const string expectedTelemetryName = EventTypes.ConversationUpdate;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, null).ConfigureAwait(false);

            // Assert
            this.mockTelemetryChannel.Verify(tc => tc.Send(It.Is<EventTelemetry>(t =>
                t.Name == expectedTelemetryName &&
                t.Properties.Count == expectedNumberOfTelemetryProperties &&
                t.Properties[Constants.TypeProperty] == activity.Type &&
                t.Properties[Constants.ChannelProperty] == activity.ChannelId)));
        }

        [Theory(DisplayName = "GIVEN next turn WHEN OnTurnAsync is invoked THEN next turn invoked")]
        [AutoData]
        public async void GivenNextTurn_WhenOnTurnAsyncIsInvoked_ThenNextTurnInvoked(Settings settings)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);
            var turnContext = new Mock<ITurnContext>();
            var nextTurnInvoked = false;

            // Act
            await instrumentation.OnTurnAsync(turnContext.Object, token => Task.Run(() => nextTurnInvoked = true, token)).ConfigureAwait(false);

            // Assert
            nextTurnInvoked.Should().Be(true);
        }

        [Theory(DisplayName = "GIVEN empty turn context WHEN OnTurnAsync is invoked THEN exception is thrown")]
        [AutoData]
        public async void GivenEmptyTurnContext_WhenOnTurnAsyncIsInvoked_ThenExceptionIsThrown(Settings settings)
        {
            // Arrange
            var instrumentation = new BotInstrumentation(this.telemetryClient, settings);

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => instrumentation.OnTurnAsync(null, null)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "GIVEN empty telemetry client WHEN constructor is invoked THEN exception is thrown")]
        [AutoData]
        public void GivenEmptyTelemetryClient_WhenConstructorIsInvoked_ThenExceptionIsThrown(Settings settings)
        {
            // Arrange
            const TelemetryClient emptyTelemetryClient = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => new BotInstrumentation(emptyTelemetryClient, settings));
        }

        [Fact(DisplayName = "GIVEN empty settings WHEN constructor is invoked THEN exception is thrown")]
        public void GivenEmptySettings_WhenConstructorIsInvoked_ThenExceptionIsThrown()
        {
            // Arrange
            const Settings emptySettings = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => new BotInstrumentation(this.telemetryClient, emptySettings));
        }
    }
}
