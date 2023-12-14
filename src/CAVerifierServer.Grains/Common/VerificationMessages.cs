using AElf.Types;

namespace CAVerifierServer.Grains.Common;

public class VerificationMessages
{
    private Address Address { get; }
    private int GuardianType { get; }
    private string Salt { get; }
    private string GuardianIdentifierHash { get; }
    private string OperationType { get; }

    public VerificationMessages(Address address, int guardianType, string salt, string guardianIdentifierHash, string operationType)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        GuardianType = guardianType;
        Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        GuardianIdentifierHash = guardianIdentifierHash ?? throw new ArgumentNullException(nameof(guardianIdentifierHash));
        OperationType = operationType ?? throw new ArgumentNullException(nameof(operationType));
    }

    public string GenerateVerificationDoc()
    {
        var isDefaultOperation = OperationType == "0" || string.IsNullOrWhiteSpace(OperationType);
        return isDefaultOperation
            ? $"{GuardianType},{GuardianIdentifierHash},{DateTime.UtcNow},{Address.ToBase58()},{Salt}"
            : $"{GuardianType},{GuardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{Address.ToBase58()},{Salt},{OperationType}";
    }
}