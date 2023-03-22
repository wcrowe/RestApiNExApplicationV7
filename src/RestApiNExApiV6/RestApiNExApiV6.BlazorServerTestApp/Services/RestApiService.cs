using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RestApiNExApiV6.BlazorServerTestApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Http.HttpClient;
using static System.Net.Http.IHttpClientFactory;

//https://executecommands.com/how-to-consume-rest-api-in-blazor-application/
//https://anthonygiretti.com/2020/10/03/net-5-exploring-system-net-http-json-namespace/
//https://www.syncfusion.com/faq/blazor/javascript-interop/how-do-you-get-current-user-details-in-a-blazor-component
namespace RestApiNExApiV6.BlazorServerTestApp.Services
{
    public class RestApiService : IRestApiService
    {

        private readonly HttpClient httpClient;
        private readonly ServiceOptions _serviceOptions;

        public static bool DotNetRunningInDockerContainer
        {
            get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
        }

        public RestApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IOptions<ServiceOptions> serviceOptions)
        {
            //TODO: fix base address  settings in Start and authorization to be unique ui
            _serviceOptions = serviceOptions.Value;

            if (!DotNetRunningInDockerContainer)
                httpClient.BaseAddress = new Uri(_serviceOptions.RestApi);
            else
                httpClient.BaseAddress = new Uri(_serviceOptions.RestApiDocker);

            this.httpClient = httpClient;
            //TODO - used existing RESTAPI authentication 
            //var UserName = httpContextAccessor.HttpContext.User.Identity.Name;

            //get token based on authentication
            LoginModel login = new LoginModel { Username = _serviceOptions.RestApiUser, Password = _serviceOptions.RestApiPassword };
            var response = httpClient.PostAsJsonAsync("/api/token", login);
            var jsonString = response.Result.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<Token>(jsonString.Result);
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.token);
        }

        #region Token

        public async Task<string> PostTokenAsync(string name, string password)
        {
            LoginModel login = new LoginModel { Username = name, Password = password };
            //var response = await httpClient.PostAsync("/api/token", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
            //var jsonString = await response.Content.ReadAsStringAsync();
            //var token = JsonSerializer.Deserialize<Token>(jsonString);
            //return token.token;
            var response = await httpClient.PostAsJsonAsync("/api/token", login);
            var jsonString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<Token>(jsonString);
            return token.token;
        }

        public class Token
        {
            public string token { get; set; }
        }
        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        #endregion

        #region Accounts

        public int AccountPagesQuantity = 1;

        public async Task<ICollection<Account>> GetAccountsAsync()
        {

            //using System.Net.Http.Json
            var response = await httpClient.GetFromJsonAsync<Account[]>("/api/account");
            return response;

        }

        public async Task<ICollection<Account>> GetAccountsAsync(Pagination pagination)
        {
            //
            var url = "/api/account/pagination/" + pagination.Page.ToString() + "/" + pagination.QuantityPerPage.ToString();
            using var httpResponse = await httpClient.GetAsync(url);
            var pagesQuantity = 1;
            if (httpResponse.Headers.Contains("pagesQuantity"))
            {
                int.TryParse(httpResponse.Headers.GetValues("pagesQuantity").First(), out pagesQuantity);
                AccountPagesQuantity = pagesQuantity;
            }

            var response = await httpResponse.Content.ReadFromJsonAsync<Account[]>();
            return response;
        }

        public async Task<Account> GetAccountByIdAsync(int id)
        {

            //using System.Net.Http.Json
            var response = await httpClient.GetFromJsonAsync<Account>("/api/account/" + id.ToString());
            return response;

        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            var response = await httpClient.DeleteAsync("/api/account/" + id.ToString());
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PutAccountAsync(int id, Account ac)
        {
            HttpContent content = new StringContent(JsonSerializer.Serialize(ac), Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync("/api/account/" + id.ToString(), content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddAccountAsync(Account ac)
        {
            HttpContent content = new StringContent(JsonSerializer.Serialize(ac), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/account/", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Users

        public int UserPagesQuantity = 1;

        public async Task<ICollection<User>> GetUsersAsync()
        {

            //using System.Net.Http.Json
            var response = await httpClient.GetFromJsonAsync<User[]>("/api/user");
            return response;

        }

        public async Task<ICollection<User>> GetUsersAsync(Pagination pagination)
        {
            //
            var url = "/api/user/pagination/" + pagination.Page.ToString() + "/" + pagination.QuantityPerPage.ToString();
            using var httpResponse = await httpClient.GetAsync(url);
            var pagesQuantity = 1;
            if (httpResponse.Headers.Contains("pagesQuantity"))
            {
                int.TryParse(httpResponse.Headers.GetValues("pagesQuantity").First(), out pagesQuantity);
                AccountPagesQuantity = pagesQuantity;
            }

            var response = await httpResponse.Content.ReadFromJsonAsync<User[]>();
            return response;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {

            //using System.Net.Http.Json
            var response = await httpClient.GetFromJsonAsync<User>("/api/user/" + id.ToString());
            return response;

        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await httpClient.DeleteAsync("/api/user/" + id.ToString());
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PutUserAsync(int id, User ac)
        {
            HttpContent content = new StringContent(JsonSerializer.Serialize(ac), Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync("/api/user/" + id.ToString(), content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddUserAsync(User ac)
        {
            HttpContent content = new StringContent(JsonSerializer.Serialize(ac), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/user/", content);
            return response.IsSuccessStatusCode;
        }

        public bool IsContainerized()
        {
            return DotNetRunningInDockerContainer;
        }

        #endregion

    }
}

