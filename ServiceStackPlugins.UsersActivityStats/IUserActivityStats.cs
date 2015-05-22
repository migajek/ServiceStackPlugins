using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStackPlugins.UsersActivityStats
{
    public interface IUserActivityStats
    {
        IEnumerable<UserActivityInfo> GetUserActivityInfos(TimeSpan? inactivityTimeSpan = null);
        void Cleanup(TimeSpan? inactivityTimeSpan = null);        
    }
}
