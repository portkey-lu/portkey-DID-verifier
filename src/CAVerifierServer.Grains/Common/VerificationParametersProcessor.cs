using AElf.Types;

namespace CAVerifierServer.Grains.Common;

public class VerificationParametersProcessor
{
    public static string GenerateSignatureHashString(Address address, int guardianType, string salt,
        string guardianIdentifierHash, string operationType)
    {
        bool isDefaultOperation = operationType == "0" || string.IsNullOrWhiteSpace(operationType);

        return isDefaultOperation
            ? $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow},{address.ToBase58()},{salt}"
            : $"{guardianType},{guardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{address.ToBase58()},{salt},{operationType}";
    }
}