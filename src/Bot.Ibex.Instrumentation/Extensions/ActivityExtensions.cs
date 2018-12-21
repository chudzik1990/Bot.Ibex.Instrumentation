namespace Bot.Ibex.Instrumentation.Extensions
{
    using System.Collections.Generic;
    using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
    using Microsoft.Bot.Schema;

    public static class ActivityExtensions
    {
        public static bool IsIncomingMessage(this IActivity activity)
        {
            return activity?.Type == ActivityTypes.Message && activity?.ReplyToId == null;
        }

        public static MultiLanguageBatchInput ToSentimentInput(this IMessageActivity activity)
        {
            return new MultiLanguageBatchInput(
                new List<MultiLanguageInput>
                {
                    new MultiLanguageInput
                    {
                        Id = "1",
                        Text = activity.Text
                    }
                });
        }
    }
}
