namespace Bot.Ibex.Instrumentation.Rest
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Rest;

    public class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        private const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";
        private readonly string subscriptionKey;

        public ApiKeyServiceClientCredentials(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(SubscriptionKeyHeaderName, this.subscriptionKey);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}