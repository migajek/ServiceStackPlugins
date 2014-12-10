namespace ServiceStackPlugins.Interfaces.Auth
{
    /// <summary>
    /// Attribute marking the field to be white-listed when the whole DTO is black-listed
    /// </summary>
    public class PermitFieldAttribute : BaseAuthAttribute
    {
        public PermitFieldAttribute(params string[] allowRoles)
            : base(allowRoles)
        {
        }
    }
}
