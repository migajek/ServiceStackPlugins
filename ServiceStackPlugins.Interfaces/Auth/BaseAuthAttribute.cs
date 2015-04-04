using System;

namespace ServiceStackPlugins.Interfaces.Auth
{
    public abstract class BaseAuthAttribute : Attribute
    {
        /// <summary>
        /// Specifies what kind of lookup to perform.         
        /// </summary>
        public enum RolesPermsJoinMode
        {
            And,
            Or
        };

        public string[] Roles { get; protected set; }
        public string[] Permissions { get; set; }
        public RolesPermsJoinMode RolesPermsJoin { get; set; }       

        protected BaseAuthAttribute(RolesPermsJoinMode rolesPermsJoin)
        {            
            this.RolesPermsJoin = rolesPermsJoin;
            Roles = new string[0];
            Permissions = new string[0];
        }
    }
}