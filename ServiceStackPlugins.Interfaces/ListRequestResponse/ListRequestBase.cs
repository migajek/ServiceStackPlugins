namespace ServiceStackPlugins.Interfaces.ListRequestResponse
{
    public class ListRequestBase
    {
        public int? PageNumber { get; set; }
        public int? PerPage { get; set; }
        /// <summary>
        /// comma-separated list of fields to order by.
        /// prefix the field with minus (-) sign for descending order
        /// </summary>
        public string Order { get; set; }
    }
}