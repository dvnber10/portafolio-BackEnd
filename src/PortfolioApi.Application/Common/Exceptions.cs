namespace PortfolioApi.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class DuplicateSlugException : Exception
{
    public DuplicateSlugException(string slug)
        : base($"Ya existe un cargo con el slug '{slug}'.") { }
}