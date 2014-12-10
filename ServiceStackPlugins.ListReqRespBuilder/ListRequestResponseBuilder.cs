using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack;
using ServiceStackPlugins.Interfaces.ListRequestResponse;

namespace ServiceStackPlugins.ListReqRespBuilder
{
    public static class ListRequestResponseBuilder
    {
        // http://stackoverflow.com/questions/16013807/unable-to-sort-with-property-name-in-linq-orderby
        public static IQueryable<T> OrderByField<T>(IQueryable<T> q, string sortField, bool @ascending)
        {
            var param = Expression.Parameter(typeof (T), "p");
            var prop = Expression.Property(param, sortField);
            var exp = Expression.Lambda(prop, param);
            var method = @ascending ? "OrderBy" : "OrderByDescending";
            var types = new Type[] {q.ElementType, exp.Body.Type};
            var mce = Expression.Call(typeof (Queryable), method, types, q.Expression, exp);
            return q.Provider.CreateQuery<T>(mce);
        }

        /// <summary>
        /// Applies limits based on request's PerPage / PageNumber
        /// </summary>
        /// <typeparam name="TDomain"></typeparam>
        /// <param name="query"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IQueryable<TDomain> ApplyLimits<TDomain>(this IQueryable<TDomain> query, ListRequestBase request)
        {
            if (request.PageNumber.HasValue || request.PerPage.HasValue)
            {
                int perPage = request.PerPage ?? 10;
                int skip = ((request.PageNumber ?? 1) - 1) * perPage;
                return query.Skip(skip).Take(perPage);
            }
            return query;
        }

        public static IQueryable<TDomain> ApplyOrdering<TDomain>(
            this IQueryable<TDomain> query, ListRequestBase request)
        {
            if (!String.IsNullOrEmpty(request.Order))
            {
                var orders = request.Order.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var ord in orders)
                {
                    var actualName = ord.StartsWith("-") ? ord.Substring(1) : ord;
                    query = OrderByField(query, actualName, !ord.StartsWith("-"));
                }
            }
            return query;
        }

        /// <summary>
        /// Applies OrderBy if no "orderby" provided in request
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IQueryable<TSource> DefaultOrderBy<TSource, TKey>(this IQueryable<TSource> source,
           Expression<Func<TSource, TKey>> keySelector, ListRequestBase request)
        {
            if (string.IsNullOrEmpty(request.Order))
                return source.OrderBy(keySelector);
            return source;
        }

        /// <summary>
        /// Applies OrderByDescending if no "orderby" provided in request
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IQueryable<TSource> DefaultOrderByDescending<TSource, TKey>(
            this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, ListRequestBase request)
        {
            if (string.IsNullOrEmpty(request.Order))
                return source.OrderByDescending(keySelector);
            return source;
        }


        public static ListResponse<TResponse> BuildListResponse<TDomain, TResponse>(this IQueryable<TDomain> query,
            ListRequestBase request, Func<IQueryable<TDomain>, IEnumerable<TResponse>> converter)
        {
            var items = query.ApplyOrdering(request).ApplyLimits(request);            
            return new ListResponse<TResponse>()
            {
                PageNumber = request.PageNumber ?? 1,
                PerPage = request.PerPage ?? 10,
                TotalItems = query.Count(),
                Items = converter(items)
            };
        }

        public static ListResponse<TResponse> BuildListResponseConvertTo<TDomain, TResponse>(this IQueryable<TDomain> query,
            ListRequestBase request)
        {
            return BuildListResponse<TDomain, TResponse>(query, request, q => q.Select<TDomain, TResponse>(x => x.ConvertTo<TResponse>()));
        }

    }
}
