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

    public static class ListResponseMapperBuilderExtensons        
    {
        static ListResponse<TResponse> EvaluateConditionally<TResponse>(ListResponse<TResponse> listResponse,
            bool evaluate)
        {
            if (evaluate)
                return listResponse.EvaluateNow();
            return listResponse;
        }
        public static ListResponse<TResponse> BuildProjectTo<TSource,TResponse>(this IListResponseBuilder<TSource> source, ListRequestBase request, bool evaluateNow = true) 
            where TSource : class
        {
            return EvaluateConditionally(source.Source.BuildListResponse(request, q => q.Project().To<TResponse>()), evaluateNow);
        }

        public static ListResponse<TResponse> BuildUsingMap<TSource, TResponse>(this IListResponseBuilder<TSource> source, ListRequestBase request, bool evaluateNow = true) 
            where TSource : class
        {
            return EvaluateConditionally(source.Source.BuildListResponse(request, AutoMapper.Mapper.Map<IEnumerable<TResponse>>), evaluateNow);
        }
    }

}
