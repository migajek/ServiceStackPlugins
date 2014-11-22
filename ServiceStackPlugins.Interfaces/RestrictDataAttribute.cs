namespace ServiceStackPlugins.Interfaces
{
    /// <summary>
    /// Specifies set of roles required to access the field / DTO.
    /// If the whole DTO is marked, one can mark specific fields with
    /// PermitField, with specifying roles or "*" for anyone, "@" for authorized
    /// </summary>
    public class RestrictDataAttribute : BaseAuthAttribute
    {
        public bool RespectGlobalIgnores { get; set; }

        public RestrictDataAttribute(params string[] restrictToRoles)
            : base(restrictToRoles)
        {
            RespectGlobalIgnores = true;
        }
    }
}