#pragma warning disable xUnit1013 // Public method should be marked as test
using IdentityModel.Client;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestApiNDxApiV6.Api;
using RestApiNDxApiV6.Api.Middlewares;
using RestApiNDxApiV6.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static JWT.Controllers.TokenController;

/// <summary>
/// Designed by AnaSoft Inc.(c) 2019-2021
/// http://www.anasoft.net/apincore
/// 
/// NOTE: Tests are not working with InMemory database.
///       Must update database connection in appsettings.json - "RestApiNDxApiV6DB".  
///       Initial database and tables will be created and seeded once during tests startup
///
///
/// AUTHENTICATION:
/// This test drives which authentication/authorization mechanism is used.
/// Update appsettings.json to switch between 
/// "UseIdentityServer4": false = uses embeded JWT authentication 
/// "UseIdentityServer4": true  =  uses IdentityServer 4
/// IMPORTANT: Before run IS4 test must build the solution and run once solution with IdentityServer as startup project
///            After you get the start page for IdentityServer4 you can stop run and run unit tests 
/// </summary>

namespace RestApiNDxApiV6.Test
{
    public class BaseTest
    {
        public static bool RemoteService = false;  //true to use service deployed to remote server
        public static string UserName = "my@email.com";
        public static string Password = "mysecretpassword123"; //encoded in User table
    }

    //https://xunit.net/docs/shared-context#collection-fixture
    /// <summary>
    /// Initialize Http client for testing for all test classes
    /// </summary>
    public class HttpClientFixture : IDisposable
    {
        public HttpClient Client { get; private set; }
        public HttpClientFixture()
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json").Build();
            //overwrite if azure db test
            if (BaseTest.RemoteService)
            {
                configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                           .AddJsonFile("appsettings-remote.json").Build();
            }

            IWebHostBuilder whb = new WebHostBuilder().UseStartup<Startup>();
            whb.UseConfiguration(configuration);
            var server = new TestServer(whb);
            Client = server.CreateClient();

        }
        public void Dispose()
        {
            Client.Dispose();
        }
    }

    [CollectionDefinition("HttpClient collection")]
    public class HttpClientCollection : ICollectionFixture<HttpClientFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class Token
    {
        public string token { get; set; }
        public string refreshToken { get; set; }
        public DateTime tokenExpiration { get; set; }
    }

    [Collection("HttpClient collection")]
    public class TokenTest : BaseTest
    {
        static HttpClient Client;
        public HttpClientFixture fixture;

        public TokenTest(HttpClientFixture fixture)
        {
            this.fixture = fixture;
            Client = fixture.Client;
        }

        public static string TokenValue { get; set; }
        public static string RefreshToken { get; set; }


        /// <summary>
        /// This test drives which authentication/authorization mechanism is used.
        /// Update appsettings.json to switch between 
        /// "UseIdentityServer4": false = uses embeded JWT authentication 
        /// "UseIdentityServer4": true  =  uses IdentityServer 4
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task token_test()
        {
            await token_get(null);
        }

        [Fact]
        public async Task refresh_revoke_token_test()
        {
            await token_get(null);

            //test refresh token
            var httpclient = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(httpclient);
            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);

            Token token = new Token { token = TokenValue, refreshToken = RefreshToken };
            var response = await Client.PostAsync("/api/token/refresh", new StringContent(JsonConvert.SerializeObject(token), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var token_new = JsonConvert.DeserializeObject<Token>(jsonString);
            Assert.NotEqual(TokenValue, token_new.token);
            Assert.NotEqual(RefreshToken, token_new.refreshToken);


            //test revoke token

            response = await Client.PostAsync("/api/token/revoke/" + UserName, null);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            //try to refresh token
            token = new Token { token = TokenValue, refreshToken = RefreshToken };
            response = await Client.PostAsync("/api/token/refresh", new StringContent(JsonConvert.SerializeObject(token), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        public static async Task token_get(HttpClient client)
        {
            if (client == null)
                client = Client;
            if (!String.IsNullOrEmpty(TokenValue)) return;

            //read from tests settings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            //JWT or IS4 authentication
            if (configuration["Authentication:UseIdentityServer4"] == "False")
            { //JWT
                LoginModel login = new LoginModel { Username = UserName, Password = Password };
                var response = await client.PostAsync("/api/token", new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var jsonString = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<Token>(jsonString);
                RefreshToken = token.refreshToken;
                TokenValue = token.token;
            }
            else
            { //IS4

                var is4ip = configuration["Authentication:IdentityServer4IP"];
                var is4token = is4ip + "/connect/token";

                //// request the token from the Auth server for type ClientCredentials
                //var tokenClient1 = new TokenClient(discoveryClient.TokenEndpoint, "clientCred", "secret");
                //var response1 = await tokenClient1.RequestClientCredentialsAsync("RestApiNDxApiV6");
                //var resp1 = response1.Json;


                //BAD client test
                var response = LoginUsingIdentityServer(is4token, "RestApiNDxApiV6-BAD", "secret", "read-write", "my@email.com", "mysecretpassword123");
                var response_json = response.AccessToken;
                if (response.IsError)
                {
                    Console.WriteLine(response.Error);
                    Console.WriteLine(response.ErrorDescription);
                }
                Assert.True(response.IsError);
                Assert.Equal("invalid_client", response.Error);
                Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);

                //BAD grant test
                response = LoginUsingIdentityServer(is4token, "RestApiNDxApiV6Client", "secret", "read-write", "my@email.com", "mysecretpassword123-BAD");
                response_json = response.AccessToken;
                if (response.IsError)
                {
                    Console.WriteLine(response.Error);
                    Console.WriteLine(response.ErrorDescription);
                }
                Assert.True(response.IsError);
                Assert.Equal("invalid_grant", response.Error);
                Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);

                //GOOD TEST----------------
                //use your own user list (from database) to get a token for API user
                response = LoginUsingIdentityServer(is4token, "RestApiNDxApiV6Client", "secret", "read-write", "my@email.com", "mysecretpassword123");
                response_json = response.AccessToken;
                if (response.IsError)
                {
                    Console.WriteLine(response.Error);
                    Console.WriteLine(response.ErrorDescription);
                }
                Assert.False(response.IsError);
                Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
                var jsonString = response.AccessToken;
                TokenValue = jsonString;
            }
        }

        public static TokenResponse LoginUsingIdentityServer(string is4ip, string clientId, string clientSecret, string scope, string username, string password)
        {

            var client = new HttpClient();
            PasswordTokenRequest tokenRequest = new PasswordTokenRequest()
            {
                Address = is4ip,
                ClientId = clientId,
                ClientSecret = clientSecret,
                UserName = username,
                Password = password,
                Scope = scope
            };

            return client.RequestPasswordTokenAsync(tokenRequest).Result;
        }

    }


    #region Account tests
    /// <summary>
    /// Account API Integration tests
    /// </summary>
    [Collection("HttpClient collection")]
    public class AccountTest : BaseTest
    {
        public HttpClientFixture fixture;
        public AccountTest(HttpClientFixture fixture)
        {
            this.fixture = fixture;
            var client = fixture.Client;
        }

        #region Account tests

        [Fact]
        public async Task account_getall()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/account");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var accounts = (ICollection<AccountViewModel>)JsonConvert.DeserializeObject<IEnumerable<AccountViewModel>>(jsonString);
            Assert.True(accounts.Count > 0);
            //clean
            await util.removeAccount(client, accountid);
        }


        [Fact]
        public async Task account_add_update_delete()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);

            //insert
            AccountViewModel vmentity = new AccountViewModel
            {
                Name = "Account 1",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsTrial = false,
                IsActive = true,
                SetActive = DateTime.Now
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/account", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetAccount(client, id);

            //update test
            Thread.Sleep(5000); //wait to get expired lazycache - see appsettings
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/account/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm update
            response = await client.GetAsync("/api/account/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, vmentity.Description);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/account/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/account/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        [Fact]
        public async Task account_getbyid()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/account/" + accountid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            Assert.True(account.Name == "Account");
            //clean
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task account_getactivebyname()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            //get by id           
            var response = await client.GetAsync("/api/account/" + accountid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            //
            response = await client.GetAsync("/api/account/GetActiveByName/" + account.Name);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonString = await response.Content.ReadAsStringAsync();
            var accounts = (ICollection<AccountViewModel>)JsonConvert.DeserializeObject<IEnumerable<AccountViewModel>>(jsonString);
            Assert.True(accounts.Count > 0);
            //clean
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task account_cache()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);

            //insert
            AccountViewModel vmentity = new AccountViewModel
            {
                Name = "Account 1",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsTrial = false,
                IsActive = true,
                SetActive = DateTime.Now
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/account", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetAccount(client, id);

            //update test
            var cachedValue = vmentity.Description;
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/account/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm cached value after update 
            response = await client.GetAsync("/api/account/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, cachedValue);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/account/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/account/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        #endregion

        #region Account async tests

        [Fact]
        public async Task account_getallasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/accountasync");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var accounts = (ICollection<AccountViewModel>)JsonConvert.DeserializeObject<IEnumerable<AccountViewModel>>(jsonString);
            Assert.True(accounts.Count > 0);
            //clean
            await util.removeAccount(client, accountid);
        }


        [Fact]
        public async Task account_add_update_delete_async()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);

            //insert
            AccountViewModel vmentity = new AccountViewModel
            {
                Name = "Account 1",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsTrial = false,
                IsActive = true,
                SetActive = DateTime.Now
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/accountasync", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetAccount(client, id);

            //update test
            Thread.Sleep(5000); //wait to get expired lazycache - see appsettings
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/accountasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm update
            response = await client.GetAsync("/api/accountasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, vmentity.Description);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/accountasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/accountasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }


        [Fact]
        public async Task account_getbyidasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/accountasync/" + accountid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            Assert.True(account.Name == "Account");
            //clean
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task account_getactivebynameasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            //get by id           
            var response = await client.GetAsync("/api/accountasync/" + accountid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            //
            response = await client.GetAsync("/api/accountasync/GetActiveByName/" + account.Name);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonString = await response.Content.ReadAsStringAsync();
            var accounts = (ICollection<AccountViewModel>)JsonConvert.DeserializeObject<IEnumerable<AccountViewModel>>(jsonString);
            Assert.True(accounts.Count > 0);
            //clean
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task account_cache_async()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);

            //insert
            AccountViewModel vmentity = new AccountViewModel
            {
                Name = "Account 1",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsTrial = false,
                IsActive = true,
                SetActive = DateTime.Now
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/accountasync", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetAccountAsync(client, id);

            //update test
            var cachedValue = vmentity.Description;
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/accountasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm cached value after update 
            response = await client.GetAsync("/api/accountasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, cachedValue);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/accountasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/accountasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        #endregion

    }
    #endregion

    #region User tests
    [Collection("HttpClient collection")]
    public class UserTest : BaseTest
    {
        public HttpClientFixture fixture;
        public UserTest(HttpClientFixture fixture)
        {
            this.fixture = fixture;
            var client = fixture.Client;
        }

        public static string LastAddedUser { get; set; }

        #region User tests

        [Fact]
        public async Task user_getall()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/user");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var users = (ICollection<UserViewModel>)JsonConvert.DeserializeObject<IEnumerable<UserViewModel>>(jsonString);
            Assert.True(users.Count > 0);
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }


        [Fact]
        public async Task user_add_update_delete()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //insert
            UserViewModel vmentity = new UserViewModel
            {
                FirstName = "User 1",
                LastName = "LastName",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsAdminRole = true,
                IsActive = true,
                Password = " ",
                AccountId = 1,
                RefreshTokenExpiryDateTime = DateTime.Today
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/user", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetUser(client, id);

            //update test
            Thread.Sleep(5000); //wait to get expired lazycache - see appsettings
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/user/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm update
            response = await client.GetAsync("/api/user/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, vmentity.Description);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/user/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/user/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        [Fact]
        public async Task user_getbyid()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/user/" + userid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            Assert.True(user.FirstName == "FirstName");
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task user_getactivebyfirstname()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            //get by id           
            var response = await client.GetAsync("/api/user/" + userid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            //
            response = await client.GetAsync("/api/user/GetActiveByFirstName/" + user.FirstName);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonString = await response.Content.ReadAsStringAsync();
            var users = (ICollection<UserViewModel>)JsonConvert.DeserializeObject<IEnumerable<UserViewModel>>(jsonString);
            Assert.True(users.Count > 0);
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task user_cache()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //insert
            UserViewModel vmentity = new UserViewModel
            {
                FirstName = "User 1",
                LastName = "LastName",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsAdminRole = true,
                IsActive = true,
                Password = " ",
                AccountId = 1,
                RefreshTokenExpiryDateTime = DateTime.Today
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/user", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetUser(client, id);

            //confirm update
            var cachedValue = vmentity.Description;
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/user/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm update
            response = await client.GetAsync("/api/user/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, cachedValue);

            //another update with same user - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/user/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/user/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        #endregion

        #region User async tests

        [Fact]
        public async Task user_getallasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/userasync");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var users = (ICollection<UserViewModel>)JsonConvert.DeserializeObject<IEnumerable<UserViewModel>>(jsonString);
            Assert.True(users.Count > 0);
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }


        [Fact]
        public async Task user_add_update_delete_async()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //insert
            UserViewModel vmentity = new UserViewModel
            {
                FirstName = "User 1",
                LastName = "LastName",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsAdminRole = true,
                IsActive = true,
                Password = " ",
                AccountId = 1,
                RefreshTokenExpiryDateTime = DateTime.Today
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/userasync", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetUser(client, id);

            //update test
            Thread.Sleep(5000); //wait to get expired lazycache - see appsettings
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/userasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm update
            response = await client.GetAsync("/api/userasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, vmentity.Description);

            //another update with same account - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/userasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/userasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        [Fact]
        public async Task user_getbyidasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/userasync/" + userid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            Assert.True(user.FirstName == "FirstName");
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task user_getactivebyfirstnameasync()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var util = new Utility();
            var accountid = await util.addAccount(client);
            var userid = await util.addUser(client, accountid);
            //
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            //get by id           
            var response = await client.GetAsync("/api/userasync/" + userid.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            //
            response = await client.GetAsync("/api/userasync/GetActiveByFirstName/" + user.FirstName);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonString = await response.Content.ReadAsStringAsync();
            var users = (ICollection<UserViewModel>)JsonConvert.DeserializeObject<IEnumerable<UserViewModel>>(jsonString);
            Assert.True(users.Count > 0);
            //clean
            await util.removeUser(client, userid);
            await util.removeAccount(client, accountid);
        }

        [Fact]
        public async Task user_cache_async()
        {
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //insert
            UserViewModel vmentity = new UserViewModel
            {
                FirstName = "User 1",
                LastName = "LastName",
                Email = "apincore@anasoft.net",
                Description = "desc",
                IsAdminRole = true,
                IsActive = true,
                Password = " ",
                AccountId = 1,
                RefreshTokenExpiryDateTime = DateTime.Today
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/userasync", new StringContent(
                                                               JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var lastAddedId = await response.Content.ReadAsStringAsync();
            Assert.True(int.Parse(lastAddedId) > 1);
            int id = 0; int.TryParse(lastAddedId, out id);

            //get inserted
            var util = new Utility();
            vmentity = await util.GetUserAsync(client, id);

            //update test
            var cachedValue = vmentity.Description;
            vmentity.Description = "desc updated";
            response = await client.PutAsync("/api/userasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            //confirm cached value after update
            response = await client.GetAsync("/api/userasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var oj = JObject.Parse(jsonString);
            var desc = oj["description"].ToString();
            Assert.Equal(desc, cachedValue);

            //another update with same user - concurrency
            vmentity.Description = "desc updated 2";
            response = await client.PutAsync("/api/userasync/" + id.ToString(), new StringContent(JsonConvert.SerializeObject(vmentity), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);

            //delete test 
            response = await client.DeleteAsync("/api/userasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        }

        #endregion

    }

    #endregion


    #region Shared test

    public class Utility
    {
        public async Task<int> addAccount(HttpClient client)
        {
            AccountViewModel account = new AccountViewModel
            {
                Name = "Account",
                Email = "apincore@anasoft.net",
                Description = "desc" + (new Random()).Next().ToString(),
                IsTrial = false,
                IsActive = true,
                SetActive = DateTime.Now
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/accountasync", new StringContent(
                                                               JsonConvert.SerializeObject(account), Encoding.UTF8, "application/json"));
            var jsonString = await response.Content.ReadAsStringAsync();
            int lastAdded = 0;
            int.TryParse(jsonString, out lastAdded);
            return lastAdded;
        }
        public async Task<AccountViewModel> GetAccount(HttpClient client, int id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/account/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            return account;
        }
        public async Task<AccountViewModel> GetAccountAsync(HttpClient client, int id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/accountasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<AccountViewModel>(jsonString);
            return account;
        }
        public async Task removeAccount(HttpClient client, int id)
        {
            await client.DeleteAsync("/api/account/" + id.ToString());
        }

        public async Task<int> addUser(HttpClient client, int accountId)
        {
            UserViewModel user = new UserViewModel
            {
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "email",
                Description = "desc" + (new Random()).Next().ToString(),
                IsAdminRole = true,
                IsActive = true,
                Password = " ",
                AccountId = accountId,
                RefreshTokenExpiryDateTime = DateTime.Today
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.PostAsync("/api/userasync", new StringContent(
                                                               JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json"));
            var jsonString = await response.Content.ReadAsStringAsync();
            int lastAdded = 0;
            int.TryParse(jsonString, out lastAdded);
            return lastAdded;
        }
        public async Task<UserViewModel> GetUser(HttpClient client, int id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/user/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            return user;
        }
        public async Task<UserViewModel> GetUserAsync(HttpClient client, int id)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenTest.TokenValue);
            var response = await client.GetAsync("/api/userasync/" + id.ToString());
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserViewModel>(jsonString);
            return user;
        }
        public async Task removeUser(HttpClient client, int id)
        {
            await client.DeleteAsync("/api/user/" + id.ToString());
        }

    }
    #endregion


    #region async Load tests
    [Collection("HttpClient collection")]
    public class ZLoadTest : BaseTest
    {
        public HttpClientFixture fixture;
        public ZLoadTest(HttpClientFixture fixture)
        {
            this.fixture = fixture;
            var client = fixture.Client;
        }


        /// <summary>
        /// Load test
        /// --local service: BaseTest.RemoteService = false  
        /// --remote service: BaseTest.RemoteService = true   
        /// </summary>
        /// <returns></returns>
        [TraitAttribute("Load test", "Run 100 times")]
        [Fact(DisplayName = "Account/User 100x load test")]
        public async Task LoadTest()
        {
            int loopmax = 100;
            var client = fixture.Client;
            if (String.IsNullOrEmpty(TokenTest.TokenValue)) await TokenTest.token_get(client);
            //
            var accountId = 0;
            var userId = 0;
            var util = new Utility();
            int i = 1;
            while (i < loopmax)
            {
                accountId = await util.addAccount(client);
                userId = await util.addUser(client, accountId);
                await util.GetAccount(client, accountId);
                await util.GetUser(client, userId);
                await util.removeUser(client, userId);
                await util.removeAccount(client, accountId);
                i++;
            }
            //
            Assert.True(i == loopmax);

        }


        /// <summary>
        ///  
        /// DDoSAttack prevention test 
        /// /api/info flagged for DDoS test with API.controller attribute  
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DDoSAttack_Test()
        {
            int attackCount = 0;
            var client = fixture.Client;

            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").Build();


            if (configuration["IntegrationTests"] == "False" || configuration["DDosAttackProtection:Enabled"] == "False")  //skip
            {
                Assert.True(true, "Skip it when the configuration settings turn off DDoS tests");
                return;
            }

            ////read from tests settings
            //var configuration = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json").Build();
            //string maxHitsPerOrigin = configuration["DDosAttackProtection:MaxHitsPerOrigin"];
            //string maxHitsPerOriginIntervalMs = configuration["DDosAttackProtection:MaxHitsPerOriginIntervalMs"];
            //string releaseIntervalMs = configuration["DDosAttackProtection:ReleaseIntervalMs"];

            //SetAppSettingValue("DDosAttackProtection:MaxHitsPerOrigin", "50");
            //SetAppSettingValue("DDosAttackProtection:MaxHitsPerOriginIntervalMs", "1000");
            //SetAppSettingValue("DDosAttackProtection:ReleaseIntervalMs", "1000");

            //LoginModel login = new LoginModel { Username = UserName, Password = Password };
            //StringContent login_string = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json");

            HttpResponseMessage response = null;
            attackCount = 5000;
            int i = 1;
            while (i < attackCount)
            {
                //response = await client.PostAsync("/api/token", login_string);
                response = await client.GetAsync("/api/info");
                i++;
            }
            //forbidden
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            //blocked ip should be expired after 2s
            System.Threading.Thread.Sleep(2000);
            attackCount = 10;
            i = 1;
            while (i < attackCount)
            {
                //response = await client.PostAsync("/api/token", login_string);
                response = await client.GetAsync("/api/info");
                i++;
            }
            //forbidden
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            ////set back config settings
            //SetAppSettingValue("DDosAttackProtection:MaxHitsPerOrigin", maxHitsPerOrigin);
            //SetAppSettingValue("DDosAttackProtection:MaxHitsPerOriginIntervalMs", maxHitsPerOriginIntervalMs);
            //SetAppSettingValue("DDosAttackProtection:ReleaseIntervalMs", releaseIntervalMs);

        }


        //update json file
        public static void SetAppSettingValue(string key, string value, string appSettingsJsonFilePath = null)
        {
            if (appSettingsJsonFilePath == null)
            {
                appSettingsJsonFilePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "appsettings.json");
            }

            var json = System.IO.File.ReadAllText(appSettingsJsonFilePath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);

            jsonObj[key] = value;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);

            System.IO.File.WriteAllText(appSettingsJsonFilePath, output);
        }

    }
    #endregion


}
