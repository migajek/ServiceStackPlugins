using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using ServiceStackPlugins.Interfaces.ListRequestResponse;
using ServiceStackPlugins.ListReqRespBuilder;

namespace ServiceStackPlugins.ListReqRespBuilderAutoMapper
{
    public static class ListRequestResponseBuilderMapper
    {

        public static ListResponse<TResponse> BuildListResponseProjectTo<TDomain, TResponse>(this IQueryable<TDomain> query,
            ListRequestBase request)
        {
            return query.BuildListResponse(request, q => q.Project().To<TResponse>());
        }

    }
}
