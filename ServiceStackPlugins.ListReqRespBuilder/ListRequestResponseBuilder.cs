using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack;
using ServiceStackPlugins.Interfaces.ListRequestResponse;

namespace ServiceStackPlugins.ListReqRespBuilder
{
    public static class ListRequestResponseBuilder
    {
        // http://stackoverflow.com/questions/16013807/unable-to-sort-with-property-name-in-linq-orderby
        public static IQueryable<T> OrderByField<T>(IQueryable<T> q, string sortField, bool @ascending)
        {
            //var param = Expression.Parameter(typeof (T), "p");
            //var prop = Expression.Property(param, sortField);
            //var exp = Expression.Lambda(prop, param);
            //var method = @ascending ? "OrderBy" : "OrderByDescending";
            //var types = new Type[] {q.ElementType, exp.Body.Type};
            //var mce = Expression.Call(typeof (Queryable), method, types, q.Expression, exp);
            //return q.Provider.CreateQuery<T>(mce);
            var methodName = @ascending ? "OrderBy" : "OrderByDescending";
            string[] props = sortField.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                // use reflection (not ComponentModel) to mirror LINQ
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable).GetMethods().Single(
                    method => method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 2
                            && method.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), type)
                    .Invoke(null, new object[] { q, lambda });
            return (IOrderedQueryable<T>)result;
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

        /// <summary>
        /// Builds list response using provided converter
        /// </summary>
        /// <typeparam name="TDomain"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="query"></param>
        /// <param name="request"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
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

        public static IListResponseBuilder<TDomain> ListResponse<TDomain>(this IQueryable<TDomain> query)
            where TDomain : class
        {
            return new ListResponseBuilder<TDomain>(query);
        }


    }

    public interface IListResponseBuilder<TSource>
        where TSource : class
    {
        IQueryable<TSource> Source { get; }
        ListResponse<TResponse> BuildWithConvert<TResponse>(ListRequestBase request);
    }

    class ListResponseBuilder<T> : IListResponseBuilder<T>
       where T : class
    {
        private IQueryable<T> source;

        public ListResponseBuilder(IQueryable<T> source)
        {
            this.source = source;
        }

        public IQueryable<T> Source { get { return source; }}

        public ListResponse<TResponse> BuildWithConvert<TResponse>(ListRequestBase request)
        {
            return source.BuildListResponseConvertTo<T, TResponse>(request);
        }
    }

    public static class ListResponseExtensions
    {
        public static ListResponse<TResponse> EvaluateNow<TResponse>(this ListResponse<TResponse> response)
        {
            response.Items = response.Items.ToArray();
            return response;
        }
    }
}
