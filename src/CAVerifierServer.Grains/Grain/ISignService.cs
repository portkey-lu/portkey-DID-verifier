using AElf;
using AElf.KeyStore;
using CAVerifierServer.Account;
using CAVerifierServer.Grains.Common;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.Options;

namespace CAVerifierServer.Grains.Grain;

public interface ISignService
{
    GenerateSignatureOutput Sign(int guardianType, string salt, string guardianIdentifierHash, string operationType);
}

public class KeyStoreSignService : ISignService, IDisposable
{
    private readonly VerifierAccountOptions _verifierAccountOptions;

    public KeyStoreSignService(IOptions<VerifierAccountOptions> verifierAccountOptions)
    {
        _verifierAccountOptions = verifierAccountOptions.Value;
    }

    public GenerateSignatureOutput Sign(int guardianType, string salt, string guardianIdentifierHash,
        string operationType)
    {
        var aelfKeyStoreService = new AElfKeyStoreService();
        return CryptographyHelper.GenerateSignature(guardianType, salt,
            guardianIdentifierHash, aelfKeyStoreService.DecryptKeyStoreFromFile(
                _verifierAccountOptions.KeyStorePassword,
                _verifierAccountOptions.KeyStorePath).ToHex(), operationType);
    }

    public void Dispose()
    {
        // todo
    }
}