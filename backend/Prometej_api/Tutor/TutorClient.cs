using System.Net.Http.Json;
using Prometej_core.Models.Requests.Tutor;
using Prometej_core.Models.ViewModels;

namespace Prometej_api.Tutor
{
    public interface ITutorClient
    {
        bool IsConfigured { get; }
        Task<bool> IsAvailable();
        // Null when the service could not answer.
        Task<TutorAnswerViewModel?> Ask(TutorAskRequest request);
        Task<TutorDraftsViewModel?> Drafts(TutorDraftsRequest request);
    }

    // The tutor is a service of its own (ai/), which holds everything about the language
    // model. This API only forwards to it.
    public class TutorClient(HttpClient http, IConfiguration configuration) : ITutorClient
    {
        private static readonly TimeSpan HealthTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(90);

        // Read on every call, not once: the tests supply configuration after startup.
        private string? BaseUrl => configuration["Tutor:BaseUrl"];

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

        public async Task<bool> IsAvailable()
        {
            if (!IsConfigured)
            {
                return false;
            }

            try
            {
                using var timeout = new CancellationTokenSource(HealthTimeout);
                using var response = await http.GetAsync(Address("health"), timeout.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                return false;
            }
        }

        public Task<TutorAnswerViewModel?> Ask(TutorAskRequest request) =>
            Post<TutorAnswerViewModel>("ask", request);

        public Task<TutorDraftsViewModel?> Drafts(TutorDraftsRequest request) =>
            Post<TutorDraftsViewModel>("drafts", request);

        private async Task<TAnswer?> Post<TAnswer>(string path, object request) where TAnswer : class
        {
            try
            {
                using var timeout = new CancellationTokenSource(AnswerTimeout);
                using var response = await http.PostAsJsonAsync(Address(path), request, timeout.Token);
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<TAnswer>(timeout.Token)
                    : null;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                return null;
            }
        }

        private Uri Address(string path) => new($"{BaseUrl!.TrimEnd('/')}/{path}");
    }
}
