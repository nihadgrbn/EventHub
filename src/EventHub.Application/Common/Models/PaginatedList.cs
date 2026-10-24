using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EventHub.Application.Common.Models
{
    public class PaginatedList<T>
    {
        public IReadOnlyCollection<T> Items { get; }
        public int TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalPages { get; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        [JsonConstructor]
        public PaginatedList(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize, int totalPages)
        {
            Items = items ?? Array.Empty<T>();
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = totalPages;
        }

        public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
            : this(items, count, pageNumber, pageSize, pageSize > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0)
        {
        }
    }
}
