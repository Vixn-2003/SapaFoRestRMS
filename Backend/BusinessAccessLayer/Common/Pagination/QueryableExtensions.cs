using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Common.Pagination
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PagedRequest req)
        {
            // Search (string contains on all string properties)
            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var param = Expression.Parameter(typeof(T), "x");
                Expression? predicate = null;
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.PropertyType == typeof(string)))
                {
                    var member = Expression.Property(param, prop);
                    var searchConst = Expression.Constant(req.Search);
                    var nullConst = Expression.Constant(null, typeof(string));
                    var notNull = Expression.NotEqual(member, nullConst);
                    var contains = Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, searchConst);
                    var and = Expression.AndAlso(notNull, contains);
                    predicate = predicate == null ? and : Expression.OrElse(predicate, and);
                }
                if (predicate != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(predicate, param);
                    query = query.Where(lambda);
                }
            }

            // Filters (exact match by property name)
            if (req.Filters != null)
            {
                foreach (var kv in req.Filters)
                {
                    var prop = typeof(T).GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop == null) continue;
                    var param = Expression.Parameter(typeof(T), "x");
                    var member = Expression.Property(param, prop);
                    var converted = Convert.ChangeType(kv.Value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                    var constant = Expression.Constant(converted, prop.PropertyType);
                    var equal = Expression.Equal(member, constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(equal, param);
                    query = query.Where(lambda);
                }
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(req.SortBy))
            {
                var prop = typeof(T).GetProperty(req.SortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var param = Expression.Parameter(typeof(T), "x");
                    var body = Expression.Property(param, prop);
                    var keySelector = Expression.Lambda(body, param);
                    var method = (req.SortDirection?.ToLowerInvariant() == "desc") ? "OrderByDescending" : "OrderBy";
                    var call = Expression.Call(typeof(Queryable), method, new[] { typeof(T), prop.PropertyType }, query.Expression, Expression.Quote(keySelector));
                    query = query.Provider.CreateQuery<T>(call);
                }
            }

            // Paging
            var page = req.Page <= 0 ? 1 : req.Page;
            var pageSize = req.PageSize <= 0 ? 10 : req.PageSize;

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<T>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = totalPages,
                Data = data
            };
        }
    }
}


