using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using CAVerifierServer.Application;
using CAVerifierServer.Options;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Contracts;

public interface IContractsProvider
{
    public Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo);
}

public class ContractsProvider : IContractsProvider, ISingletonDependency
{
    public async Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo)
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        const string methodName = "GetCAServers";
        const string commonPrivateKey = AElfClientConstants.DefaultPrivateKey;
        var ownAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(commonPrivateKey)).ToBase58();
        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainInfo.ContractAddress,
            methodName, param);
        var txWithSign = client.SignTransaction(commonPrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetCAServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }
}