namespace Restaurant.Application.Exceptions
{
    public class NotFoundException(string resourceName, string resourceKey)
        : Exception($"The {resourceName} with the key '{resourceKey}' was not found.");
}