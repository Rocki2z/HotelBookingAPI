namespace HotelBookingAPI.Helpers;

public static class PaginationHelper
{
    public static int GetSkip(int pageNumber, int pageSize)
    {
        return (pageNumber - 1) * pageSize;
    }

    public static int ValidatePageNumber(int pageNumber)
    {
        return pageNumber < 1 ? 1 : pageNumber;
    }

    public static int ValidatePageSize(int pageSize)
    {
        if (pageSize < 1)
            return 10;

        return pageSize > 100 ? 100 : pageSize;
    }
}