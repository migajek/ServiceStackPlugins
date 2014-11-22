using System;

namespace ServiceStackPlugins.Interfaces
{
    public abstract class BaseAuthAttribute : Attribute
    {
        public string[] Roles { get; private set; }

        protected BaseAuthAttribute(params string[] roles)
        {
            Roles = roles;
        }
    }
}