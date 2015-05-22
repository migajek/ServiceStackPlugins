using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Caching;

namespace ServiceStackPlugins.UsersActivityStats
{
    internal class UserActivityStats: IUserActivityStats
    {
        public ICacheClient CacheClient { get; set; }

        private TimeSpan GetInactivityTimeSpan()
        {
            return HostContext.AppHost.GetPlugin<UsersActivityFeature>().InactivityTimespan;
        }

        public IEnumerable<UserActivityInfo> GetUserActivityInfos(TimeSpan? inactivityTimeSpan = null)
        {
            using (var collector = new UserStatsCollector(CacheClient))
                return collector.GetUsers(inactivityTimeSpan ?? GetInactivityTimeSpan());
        }

        public void Cleanup(TimeSpan? inactivityTimeSpan = null)
        {
            using (var collector = new UserStatsCollector(CacheClient))
                collector.Cleanup(inactivityTimeSpan ?? GetInactivityTimeSpan());
        }        
    }
}
