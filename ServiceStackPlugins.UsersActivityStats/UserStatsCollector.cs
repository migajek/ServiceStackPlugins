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
        public DateTimeOffset LastSeen { get; set; }
    }

    public class UserActivityInfo: UserInfo
    {
        public bool IsActive { get; set; }
        public TimeSpan InactiveFor { get; set; }
    }

    internal class UserStatsCollector: IDisposable
    {
        private static readonly string CacheKey = typeof (UserStatsCollector).Name + "_user_list";
        private Dictionary<string, UserInfo> _userInfos = new Dictionary<string, UserInfo>();
        private readonly ICacheClient _cacheClient = null;      

        public UserStatsCollector(ICacheClient cacheClient)
        {            
            this._cacheClient = cacheClient;
            LoadFromCache(_cacheClient);
        }

        public void LoadFromCache(ICacheClient cacheClient)
        {
            _userInfos = cacheClient.Get<Dictionary<string, UserInfo>>(CacheKey) ?? new Dictionary<string, UserInfo>();
        }

        public UserStatsCollector()
        {            
        }

        public void Dispose()
        {
            if (_cacheClient != null)
                SaveToCache(_cacheClient);
        }

        public void SaveToCache(ICacheClient cacheClient)
        {
            cacheClient.Set(CacheKey, _userInfos);
        }

        public void UserSeen(string uid, string uname, string ip)
        {
            _userInfos[uid] = new UserInfo()
            {
                UserId = uid,
                UserName = uname,
                LastSeenIp = ip,
                LastSeen = DateTimeOffset.UtcNow
            };
        }

        public IEnumerable<UserActivityInfo> GetUsers(TimeSpan inactivityTreshold)
        {
            var now = DateTimeOffset.UtcNow;
            return from u in _userInfos.Values.OrderByDescending(x => x.LastSeen)
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

        public void Cleanup(TimeSpan inactivityTreshold, int whenMoreThan = 200)
        {
            if (_userInfos.Count < whenMoreThan)
                return;
            var now = DateTimeOffset.UtcNow;
            _userInfos = _userInfos.Where(kv => now - kv.Value.LastSeen > inactivityTreshold).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public void Cleanup(int inactivityTresholdSeconds)
        {
            Cleanup(new TimeSpan(0, 0, 0, inactivityTresholdSeconds));
        }
    }
}
