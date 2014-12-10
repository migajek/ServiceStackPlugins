using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace ServiceStackPlugins.Interfaces.ListRequestResponse
{
    public class ListRequest<TResult> : ListRequestBase, IReturn<ListResponse<TResult>>
    {
    }
}
