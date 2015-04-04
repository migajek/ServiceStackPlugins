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
    public interface IListRequestResponseMapperBuilder<TSource>
        where TSource: class
    {
        ListResponse<TResponse> BuildProjectTo<TResponse>(ListRequestBase request);
        ListResponse<TResponse> BuildUsingMap<TResponse>(ListRequestBase request);
    }

    class ListRequestResponseMapperBuilder<T> : IListRequestResponseMapperBuilder<T>
        where T: class
    {
        private IQueryable<T> source;

        public ListRequestResponseMapperBuilder(IQueryable<T> source)
        {
            this.source = source;
        }

        public ListResponse<TResponse> BuildProjectTo<TResponse>(ListRequestBase request)
        {
            return source.BuildListResponseProjectTo<T, TResponse>(request);
        }

        public ListResponse<TResponse> BuildUsingMap<TResponse>(ListRequestBase request)
        {
            return source.BuildListResponse(request, AutoMapper.Mapper.Map<IEnumerable<TResponse>>);
        }
    }

    public static class ListRequestResponseBuilderMapper
    {

        public static ListResponse<TResponse> BuildListResponseProjectTo<TDomain, TResponse>(this IQueryable<TDomain> query,
            ListRequestBase request)
        {
            return query.BuildListResponse(request, q => q.Project().To<TResponse>());
        }

        public static IListRequestResponseMapperBuilder<TDomain> ListResponse<TDomain>(this IQueryable<TDomain> query)
            where TDomain: class
        {
            return  new ListRequestResponseMapperBuilder<TDomain>(query);
        }
    }
}
