using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStackPlugins.UsersActivityStats
{
    /// <summary>
    /// Track active users, including their's last activity and IP
    /// </summary>
    public class UsersActivityFeature: IPlugin
    {
        /// <summary>
        /// Function which extracts UserName from given IAuthSession
        /// </summary>
        public Func<IAuthSession, string> UserNameExtractor { get; set; }

        /// <summary>
        /// Specify period from last activity which marks the user as inactive. 
        /// Defaults to 3 minutes
        /// </summary>
        public TimeSpan InactivityTimespan { get; set; }

        public void Register(IAppHost appHost)
        {
            appHost.GlobalRequestFilters.Add(RequestFilter);
            appHost.GetContainer().RegisterAutoWiredAs<UserActivityStats, IUserActivityStats>();
        }

        public UsersActivityFeature()
        {
            UserNameExtractor = x => x.UserName;
            InactivityTimespan = new TimeSpan(0,0,3,0);
        }
        
        private void RequestFilter(IRequest request, IResponse response, object arg3)
        {
            var authSession = request.GetSession();
            var cache = request.TryResolve<ICacheClient>();
            if (cache == null)
                return;

            if (authSession != null)
                using (var collector = new UserStatsCollector(cache))
                {
                    collector.UserSeen(authSession.UserAuthId, UserNameExtractor(authSession), request.RemoteIp);
                    collector.Cleanup(InactivityTimespan);
                }
        }        
    }
}
