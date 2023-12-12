using System.Text;
using AElf;
using CAVerifierServer.Account;
using CAVerifierServer.Grains.Common;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Grains.State;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;
using Volo.Abp;
using Volo.Abp.Timing;

namespace CAVerifierServer.Grains.Grain;

[StorageProvider(ProviderName = "Default")]
public class GuardianIdentifierVerificationGrain : Grain<GuardianIdentifierVerificationState>,
    IGuardianIdentifierVerificationGrain
{
    private const string BASECODE = "0123456789";

    private readonly VerifierCodeOptions _verifierCodeOptions;
    private readonly VerifierAccountOptions _verifierAccountOptions;
    private readonly GuardianTypeOptions _guardianTypeOptions;
    private readonly IClock _clock;
    private readonly ISigner _signer;
    
    public GuardianIdentifierVerificationGrain(IOptions<VerifierCodeOptions> verifierCodeOptions,
        IOptions<VerifierAccountOptions> verifierAccountOptions, IOptions<GuardianTypeOptions> guardianTypeOptions,
        IClock clock, ISigner signer)
    {
        _clock = clock;
        _guardianTypeOptions = guardianTypeOptions.Value;
        _verifierCodeOptions = verifierCodeOptions.Value;
        _verifierAccountOptions = verifierAccountOptions.Value;
        _signer = signer;
    }

    private Task<string> GetCodeAsync(int length)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var rnNum = RandomHelper.GetRandom(BASECODE.Length);
            builder.Append(BASECODE[rnNum]);
        }

        return Task.FromResult(builder.ToString());
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<VerifyCodeDto>> GetVerifyCodeAsync(SendVerificationRequestInput input)
    {
        //clean expireCode And Validate
        var grainDto = new GrainResultDto<VerifyCodeDto>();
        var verifications = State.GuardianTypeVerifications;
        if (verifications != null)
        {
            var now = _clock.Now;
            verifications.RemoveAll(p =>
                p.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.CodeExpireTime) < now);
            verifications.RemoveAll(p => p.Verified);
            await WriteStateAsync();
            var totalList = verifications.Where(p =>
                    p.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.GetCodeFrequencyTimeLimit) >
                    now)
                .ToList();
            if (totalList.Count >= _verifierCodeOptions.GetCodeFrequencyLimit)
            {
                grainDto.Message = "The interval between sending two verification codes is less than 60s";
                return grainDto;
            }

            if (verifications.Any(p => p.VerifierSessionId == input.VerifierSessionId))
            {
                grainDto.Success = true;
                grainDto.Data = new VerifyCodeDto
                {
                    VerifierCode = verifications.FirstOrDefault(p => p.VerifierSessionId == input.VerifierSessionId)
                        ?.VerificationCode
                };
                await WriteStateAsync();
                return grainDto;
            }
        }

        var guardianIdentifierVerification = new GuardianIdentifierVerification
        {
            GuardianIdentifier = input.GuardianIdentifier,
            GuardianType = input.Type,
            VerifierSessionId = input.VerifierSessionId
        };
        //create code
        var randomCode = await GetCodeAsync(6);
        guardianIdentifierVerification.VerificationCode = randomCode;
        guardianIdentifierVerification.VerificationCodeSentTime = _clock.Now;
        verifications ??= new List<GuardianIdentifierVerification>();
        verifications.Add(guardianIdentifierVerification);
        State.GuardianTypeVerifications = verifications;
        grainDto.Success = true;
        grainDto.Data = new VerifyCodeDto
        {
            VerifierCode = randomCode
        };
        await WriteStateAsync();
        return grainDto;
    }


    public async Task<GrainResultDto<UpdateVerifierSignatureDto>> VerifyAndCreateSignatureAsync(VerifyCodeInput input)
    {
        var dto = new GrainResultDto<UpdateVerifierSignatureDto>();
        var verifications = State.GuardianTypeVerifications;
        if (verifications == null)
        {
            dto.Message = Error.Message[Error.InvalidLoginGuardianIdentifier];
            return dto;
        }

        verifications = verifications.Where(p => p.VerifierSessionId == input.VerifierSessionId).ToList();
        if (verifications.Count == 0)
        {
            dto.Message = Error.Message[Error.InvalidVerifierSessionId];
            return dto;
        }

        var guardianTypeVerification = verifications[0];
        var errorCode = VerifyCodeAsync(guardianTypeVerification, input.Code);
        if (errorCode > 0)
        {
            dto.Message = Error.Message[errorCode];
            return dto;
        }

        guardianTypeVerification.VerifiedTime = _clock.Now;
        guardianTypeVerification.Verified = true;
        guardianTypeVerification.Salt = input.Salt;
        guardianTypeVerification.GuardianIdentifierHash = input.GuardianIdentifierHash;
        var guardianTypeCode = _guardianTypeOptions.GuardianTypeDic[guardianTypeVerification.GuardianType];
        var data = CryptographyHelper.ConvertHashFromData(_signer.GetAddress(),
            guardianTypeCode, guardianTypeVerification.Salt, guardianTypeVerification.GuardianIdentifierHash,
            input.OperationType);
        var signature = _signer.Sign(HashHelper.ComputeFrom(data));
        guardianTypeVerification.VerificationDoc = data;
        guardianTypeVerification.Signature = signature.ToHex();
        dto.Success = true;
        dto.Data = new UpdateVerifierSignatureDto
        {
            Data = data,
            Signature = signature.ToHex()
        };
        await WriteStateAsync();
        return dto;
    }

    private int VerifyCodeAsync(GuardianIdentifierVerification guardianIdentifierVerification, string code)
    {
        //Already Verified
        if (guardianIdentifierVerification.Verified)
        {
            return Error.Verified;
        }

        //verify code exceed the time limit
        if (guardianIdentifierVerification.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.CodeExpireTime) <
            _clock.Now)
        {
            return Error.Timeout;
        }

        //error code times
        if (guardianIdentifierVerification.ErrorCodeTimes > _verifierCodeOptions.RetryTimes)
        {
            return Error.TooManyRetries;
        }

        //verify code is right
        if (code == guardianIdentifierVerification.VerificationCode)
        {
            return 0;
        }

        guardianIdentifierVerification.ErrorCodeTimes++;
        return Error.WrongCode;
    }
}