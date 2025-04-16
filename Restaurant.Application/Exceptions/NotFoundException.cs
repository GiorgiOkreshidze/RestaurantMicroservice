namespace Restaurant.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string resourceName, string resourceKey) : base($"The {resourceName} with the key '{resourceKey}' was not found.")
        {
        }
        
        public NotFoundException(string message) : base(message) { }
    }
}