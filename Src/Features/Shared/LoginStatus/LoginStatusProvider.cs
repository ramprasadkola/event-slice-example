using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bolt.Logger;
using Bolt.RequestBus;
using Bolt.RestClient;
using Bolt.RestClient.Extensions;
using BookWorm.Api;
using Microsoft.Extensions.Options;
using Src.Features.Shared.Settings;
using Src.Infrastructure.Attributes;
using Src.Infrastructure.ErrorSafeHelpers;
using Src.Infrastructure.Stores;

namespace BookWorm.Web.Features.Shared.LoginStatus
{
    public interface ILoginStatusProvider
    {
        LoginStatusViewModel Get();
    }

    [AutoBind]
    public class LoginStatusProvider : ILoginStatusProvider
    {
        private readonly ILoginStatusContextStore contextStore;

        public LoginStatusProvider(ILoginStatusContextStore contextStore)
        {
            this.contextStore = contextStore;
        }

        public LoginStatusViewModel Get()
        {
            var dto = contextStore.Get();

            return dto == null 
                ? new LoginStatusViewModel() 
                : new LoginStatusViewModel{
                    Name = dto.Name
                };
        }
    }

    public interface ILoginStatusContextStore
    {
        UserDto Get();
        void Set(UserDto value);
    }

    [AutoBind]
    public class LoginStatusContextStore : ILoginStatusContextStore
    {
        private const string Key = "LoginStatusContextStore:Get";
        private readonly IContextStore context;

        public LoginStatusContextStore(IContextStore context)
        {
            this.context = context;    
        }

        public UserDto Get()
        {
            return context.Get<UserDto>(Key);
        }

        public void Set(UserDto value)
        {
            context.Set(Key, value);
        }
    }

    public class LoadLoginStatusOnPageLoadEventHandler<T> : Bolt.RequestBus.IAsyncEventHandler<T> where T : IEvent
    {
        private readonly ILoginStatusContextStore context;
        private readonly IRestClient restClient;
        private readonly ILogger logger;
        private readonly IOptions<ApiSettings> settings;

        public LoadLoginStatusOnPageLoadEventHandler(ILoginStatusContextStore context, 
            IRestClient restClient, 
            ILogger logger,
            IOptions<ApiSettings> settings)
        {
            this.context = context;
            this.restClient = restClient;
            this.logger = logger;
            this.settings = settings;
        }

        public async Task HandleAsync(T eEvent)
        {
            if(!(eEvent is BookWorm.Web.Features.Shared.Events.IPageRequestedEvent)) return ;

            var response = await ErrorSafe.WithLogger(logger)
                .ExecuteAsync(() => restClient.For($"{settings.Value.BaseUrl}/users/{0}", "dummyid")
                .AcceptJson()
                .RetryOnFailure(2)
                .Timeout(1000)
                .GetAsync<UserDto>());

            context.Set(response.Value?.Output);
        }
    }

    public class UserDto
    {
        public string Name { get; set; }
    }
}