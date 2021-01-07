namespace API.Helpers {
    public class PaginationHeader {
        // PaginationHeader class used in HttpExtensions to add a custom header to
        // our HttpResponses
        public PaginationHeader(int currentPage, int itemsPerPage, int totalItems, int totalPages) {
            CurrentPage = currentPage;
            ItemsPerPage = itemsPerPage;
            TotalItems = totalItems;
            TotalPages = totalPages;
        }

        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}