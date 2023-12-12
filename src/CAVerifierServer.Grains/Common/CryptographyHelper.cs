using AElf.Types;

namespace CAVerifierServer.Grains.Common;

public class CryptographyHelper
{
    public static string ConvertHashFromData(Address address, int guardianType, string salt,
        string guardianIdentifierHash, string operationType)
    {
        return operationType == "0" || string.IsNullOrWhiteSpace(operationType)
            ? $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow},{address.ToBase58()},{salt}"
            : $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{address.ToBase58()},{salt},{operationType}";
    }
}