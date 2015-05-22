using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;

namespace ServiceStackPlugins.UsersActivityStats
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string LastSeenIp { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class UserActivityInfo: UserInfo
    {
        public bool IsActive { get; set; }
        public TimeSpan InactiveFor { get; set; }
    }

    internal class UserStatsCollector: IDisposable
    {
        private static string _cacheKey = typeof (UserStatsCollector).Name + "_user_list";
        private Dictionary<string, UserInfo> userInfos;
        private ICacheClient _cacheClient;        

        public UserStatsCollector(ICacheClient cacheClient): this()
        {            
            this._cacheClient = cacheClient;
            LoadFromCache(_cacheClient);
        }

        public void LoadFromCache(ICacheClient cacheClient)
        {
            userInfos = cacheClient.Get<Dictionary<string, UserInfo>>(_cacheKey);
        }

        public UserStatsCollector()
        {
            userInfos = new Dictionary<string, UserInfo>();
        }

        public void Dispose()
        {
            if (_cacheClient != null)
                SaveToCache(_cacheClient);
        }

        public void SaveToCache(ICacheClient cacheClient)
        {
            cacheClient.Set(_cacheKey, userInfos);
        }

        public void UserSeen(string uid, string uname, string ip)
        {
            userInfos[uid] = new UserInfo()
            {
                UserId = uid,
                UserName = uname,
                LastSeenIp = ip,
                LastSeen = DateTime.UtcNow
            };
        }

        public IEnumerable<UserActivityInfo> GetUsers(TimeSpan inactivityTreshold)
        {
            var now = DateTime.UtcNow;
            return from u in userInfos.Values.OrderByDescending(x => x.LastSeen)
                let inactiveFor = now - u.LastSeen
                select new UserActivityInfo()
                {
                    InactiveFor = inactiveFor,
                    IsActive = inactiveFor < inactivityTreshold,
                    LastSeen = u.LastSeen,
                    LastSeenIp = u.LastSeenIp,
                    UserName = u.UserName,
                    UserId = u.UserId
                };
        }

        public void Cleanup(TimeSpan inactivityTreshold)
        {
            var now = DateTime.UtcNow;
            userInfos = userInfos.Where(kv => now - kv.Value.LastSeen > inactivityTreshold).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Cleanup(int inactivityTresholdSeconds)
        {
            Cleanup(new TimeSpan(0, 0, 0, inactivityTresholdSeconds));
        }
    }
}
