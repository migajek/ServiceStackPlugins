using System.Collections.Generic;

namespace ServiceStackPlugins.Interfaces.ListRequestResponse
{
    public class ListResponse<TResult>
    {
        public int PageNumber { get; set; }
        public int PerPage { get; set; }
        public string[] OrderBy { get; set; }
        public IEnumerable<TResult> Items { get; set; }
        public int TotalItems { get; set; }
        public object Meta { get; set; }
    }
}