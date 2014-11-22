using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStackPlugins.Interfaces
{
    /// <summary>
    /// Attribute marking the field to be white-listed when the whole DTO is black-listed
    /// </summary>
    public class PermitFieldAttribute : BaseAuthAttribute
    {
        public PermitFieldAttribute(params string[] allowRoles)
            : base(allowRoles)
        {
        }
    }
}
