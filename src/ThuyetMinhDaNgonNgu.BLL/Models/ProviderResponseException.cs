namespace ThuyetMinhDaNgonNgu.BLL.Models;

public sealed class ProviderResponseException(string message, Exception? innerException = null)
    : Exception(message, innerException);
