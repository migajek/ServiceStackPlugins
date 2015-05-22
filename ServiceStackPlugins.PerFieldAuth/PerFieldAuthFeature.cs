using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Web;
using ServiceStackPlugins.Interfaces;
using ServiceStackPlugins.Interfaces.Auth;

namespace ServiceStackPlugins.PerFieldAuth
{
    public class PerFieldAuthFeature : IPlugin
    {
        private IAppHost AppHost;

        private readonly Dictionary<Type, Func<object, IEnumerable<object>>> _customExtractors =
            new Dictionary<Type, Func<object, IEnumerable<object>>>();

        public List<string> GlobalIgnoredPropertyNames { get; set; }

        private IAuthSession GetAuthSession(IRequest request)
        {
            var key = SessionFeature.GetSessionKey(request);
            return AppHost.GetContainer().Resolve<ICacheClient>().Get<IAuthSession>(key);
        }


        /// <summary>
        /// check if the user satisfies the requirements
        /// i.e. check if * or @ is in roles or any role matches.
        /// </summary>
        /// <param name="roles">roles to check against</param>
        /// <param name="userAuthenticated"></param>
        /// <param name="userRoles">roles assigned to the user</param>
        /// <param name="perms"></param>
        /// <returns></returns>
        private bool UserSatisfiesRequirements(IEnumerable<string> roles, IEnumerable<string> perms,
            bool userAuthenticated, IEnumerable<string> userRoles, IEnumerable<string> userPerms,
            BaseAuthAttribute.RolesPermsJoinMode joinMode)
        {
            if (roles.Contains("*") || (userAuthenticated && roles.Contains("@")))
                return true;
            var hasRole = userRoles == null ? false : roles.Any(userRoles.Contains);
            var hasPerm = userPerms == null ? false : perms.Any(userPerms.Contains);

            if (roles.Any() && perms.Any())
            {
                if (joinMode == BaseAuthAttribute.RolesPermsJoinMode.And)
                    return hasRole && hasPerm;
                else
                    return hasPerm || hasRole;
            } else if (roles.Any())
                return hasRole;
            else if (perms.Any())
                return hasPerm;
            return false;
        }

        /// <summary>
        /// Process the DTO, removing any attributes not permitted for given user
        /// </summary>
        /// <param name="dto">DTO object</param>
        /// <param name="userRoles">current user roles</param>
        /// <param name="userAuthenticated"></param>
        /// <param name="userPerms"></param>
        private void ProcessSingleDto(object dto, IEnumerable<string> userRoles, IEnumerable<string> userPerms,
            bool userAuthenticated)
        {
            var dtoType = dto.GetType();

            // dto public writeable properties
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite).ToList();

            var propertiesToUnset = new HashSet<PropertyInfo>();

            // check if the whole object is restricted
            var dtoAttributes = dtoType.GetCustomAttributes<RestrictDataAttribute>(true).ToList();
            if (dtoAttributes.Any())
            {
                var respectGlobalIgnores = dtoAttributes.Any(x => x.RespectGlobalIgnores);
                // collect all the roles which are permitted
                var roles = dtoAttributes.SelectMany(x => x.Roles);
                var perms = dtoAttributes.SelectMany(x => x.Permissions);
                var mode = dtoAttributes.Select(x => x.RolesPermsJoin).FirstOrDefault();
                var inverted = dtoAttributes.Select(x => x.Inverse).FirstOrDefault();
                // if the user doesnt satisfy the requirements, add all the attributes to deletion
                // dont delete it yet as it might be whitelisted later.
                if (!UserSatisfiesRequirements(roles, perms, userAuthenticated, userRoles, userPerms, mode) ^ inverted)
                    foreach (
                        var property in
                            properties.Where(
                                property => !GlobalIgnoredPropertyNames.Contains(property.Name) || !respectGlobalIgnores)
                        )
                        propertiesToUnset.Add(property);
            }

            foreach (var property in properties)
            {
                var restrictAttrs = property.GetCustomAttributes<RestrictDataAttribute>(true);
                var propertyPermittedRoles = restrictAttrs.SelectMany(x => x.Roles);
                var propertyPermittedPerms = restrictAttrs.SelectMany(x => x.Permissions);
                var mode = restrictAttrs.Select(x => x.RolesPermsJoin).FirstOrDefault();
                var inverted = restrictAttrs.Select(x => x.Inverse).FirstOrDefault();
                if (restrictAttrs.Any() &&
                    (!UserSatisfiesRequirements(propertyPermittedRoles, propertyPermittedPerms, userAuthenticated,
                       userRoles, userPerms, mode) ^ inverted))
                    propertiesToUnset.Add(property);

                var permitAttrs = property.GetCustomAttributes<PermitFieldAttribute>(true);
                propertyPermittedRoles = permitAttrs.SelectMany(x => x.Roles);
                propertyPermittedPerms = permitAttrs.SelectMany(x => x.Permissions);
                mode = permitAttrs.Select(x => x.RolesPermsJoin).FirstOrDefault();
                inverted = permitAttrs.Select(x => x.Inverse).FirstOrDefault();
                if (permitAttrs.Any() &&
                    (UserSatisfiesRequirements(propertyPermittedRoles, propertyPermittedPerms, userAuthenticated,
                        userRoles, userPerms,mode) ^ inverted))
                    propertiesToUnset.Remove(property);
            }

            foreach (var property in propertiesToUnset)
            {
                property.SetValue(dto, null, null);
            }
        }

        /// <summary>
        /// Response filter, which tries to extract the actual DTO and executes ProcessSingleDTO on it.
        /// Extracting might be for example from IEnumerable, as well as from custom types
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="dto"></param>
        private void ResponseFilterAction(IRequest request, IResponse response, object dto)
        {
            if (dto == null)
                return;
            var sess = GetAuthSession(request);
            var roles = (sess == null) ? Enumerable.Empty<string>() : sess.Roles;

            ProcessDto(dto, roles, sess == null ? Enumerable.Empty<string>() : sess.Permissions,
                sess != null && sess.IsAuthenticated);
        }

        public void ProcessDto(object dto, IEnumerable<string> roles, IEnumerable<string> userPerms,
            bool userAthenticated)
        {
            // lookup custom extractor
            var custom = false;
            var dtoType = dto.GetType();
            if (dtoType.IsGenericType)
                dtoType = dtoType.GetGenericTypeDefinition();
            foreach (var extractor in _customExtractors.Where(kv => kv.Key.IsAssignableFrom(dtoType)))
            {
                var actualDTOs = extractor.Value(dto);
                if (actualDTOs == null)
                    continue;
                foreach (var actualDTO in actualDTOs)
                    ProcessSingleDto(actualDTO, roles, userPerms, userAthenticated);

                custom = true;
                break;

            }

            if (!custom)
                ProcessSingleDto(dto, roles, userPerms, userAthenticated);
        }

        /// <summary>
        /// Register custom DTO type extractor.        
        /// Action should return list / iterator of single DTOs extracted from the response.
        /// </summary>
        /// <param name="forType"></param>
        /// <param name="action"></param>
        public void RegisterCustomTypeExtractor(Type forType, Func<object, IEnumerable<object>> action)
        {
            _customExtractors[forType] = action;
        }

        public void RegisterCustomTypeExtractor<T>(Func<T, IEnumerable<object>> action)
            where T : class
        {
            RegisterCustomTypeExtractor(typeof (T), obj => action(obj as T));
        }


        /// <summary>
        /// IPlugin implementation
        /// </summary>
        /// <param name="appHost"></param>
        public void Register(IAppHost appHost)
        {
            AppHost = appHost;
            appHost.GlobalResponseFilters.Add(ResponseFilterAction);
        }


        public PerFieldAuthFeature()
        {
            GlobalIgnoredPropertyNames = new List<string>() {"Id"};
            RegisterCustomTypeExtractor(typeof (IEnumerable), dto => dto as IEnumerable<object>);
        }
    }
}