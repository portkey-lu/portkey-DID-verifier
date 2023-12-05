using AElf;
using AElf.KeyStore;
using CAVerifierServer.Account;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.Grains.Common;

public interface ISignService
{
    GenerateSignatureOutput Sign(int guardianType, string salt, string guardianIdentifierHash, string operationType);
}

public class KeyStoreSignService : ISignService, IDisposable
{
    private readonly VerifierAccountOptions _verifierAccountOptions;
    private readonly AElfKeyStoreService _aelfKeyStoreService = new();

    public KeyStoreSignService(IOptions<VerifierAccountOptions> verifierAccountOptions)
    {
        _verifierAccountOptions = verifierAccountOptions.Value;
    }

    public GenerateSignatureOutput Sign(int guardianType, string salt, string guardianIdentifierHash,
        string operationType)
    {
        return CryptographyHelper.GenerateSignature(guardianType, salt,
            guardianIdentifierHash, _aelfKeyStoreService.DecryptKeyStoreFromFile(
                _verifierAccountOptions.KeyStorePassword,
                _verifierAccountOptions.KeyStorePath).ToHex(), operationType);
    }

    public void Dispose()
    {
        // todo
        GC.SuppressFinalize(this);
    }
}