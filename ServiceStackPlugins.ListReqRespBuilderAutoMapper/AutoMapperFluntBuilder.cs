using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStackPlugins.Interfaces.ListRequestResponse;
using ServiceStackPlugins.ListReqRespBuilder;

namespace ServiceStackPlugins.ListReqRespBuilderAutoMapper
{
    public interface IListResponseAutoMapperBuilder<TSource>
        where TSource : class
    {        
        ListResponse<TResponse> UsingMap<TResponse>(ListRequestBase request);
        ListResponse<TResponse> UsingProjectTo<TResponse>(ListRequestBase request);
    }

    class ListResponseAutoMapperBuilder<T> : IListResponseAutoMapperBuilder<T>
       where T : class
    {
        private readonly IListResponseBuilder<T> source;

        public ListResponseAutoMapperBuilder(IListResponseBuilder<T> source)
        {
            this.source = source;
        }

        public ListResponse<TResponse> UsingMap<TResponse>(ListRequestBase request)
        {
            return source.BuildUsingMap<T, TResponse>(request);
        }

        public ListResponse<TResponse> UsingProjectTo<TResponse>(ListRequestBase request)
        {
            return source.BuildProjectTo<T, TResponse>(request);
        }
    }

    public static class ListResponseBuilderAutomapperFluentExtensions
    {
        public static IListResponseAutoMapperBuilder<T> AutoMapper<T>(this IListResponseBuilder<T> builder) where T : class
        {
            return new ListResponseAutoMapperBuilder<T>(builder);
        }
    }
}
