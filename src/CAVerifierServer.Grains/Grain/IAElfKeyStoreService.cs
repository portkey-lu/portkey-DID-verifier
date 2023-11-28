using CAVerifierServer.Grains.Options;
using Nethereum.KeyStore;

namespace CAVerifierServer.Grains.Grain;

public abstract class IAElfKeyStoreService : KeyStoreService
{
    public abstract byte[] DecryptKeyStore(VerifierAccountOptions verifierAccountOptions);
}