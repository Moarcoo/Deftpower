namespace Deftpower.Onb.Assessment.Exceptions;

[Serializable]
public class DomainValidationException(string message, string paramName) : ArgumentException(message, paramName);
