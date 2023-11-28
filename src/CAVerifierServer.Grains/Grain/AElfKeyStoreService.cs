using CAVerifierServer.Grains.Options;

namespace CAVerifierServer.Grains.Grain;

public class AElfKeyStoreService : IAElfKeyStoreService
{
    public override byte[] DecryptKeyStore(VerifierAccountOptions verifierAccountOptions)
    {
        using (var file = File.OpenText(verifierAccountOptions.KeyStorePath))
        {
            var json = file.ReadToEnd();
            return DecryptKeyStoreFromJson(verifierAccountOptions.KeyStorePassword, json);
        }
    }

}