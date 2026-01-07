namespace Watchmen.Common.Types;

 public record PagedQuery<T>(
      IReadOnlyList<T> Items,
      long TotalCount,
      int Page,
      int PageSize
  )
  {
      public long TotalPages => (long)Math.Ceiling((double)TotalCount / PageSize);
      public bool HasNextPage => Page < TotalPages;
      public bool HasPreviousPage => Page > 1;
  }