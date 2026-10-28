using System;
using System.Security.Cryptography;

namespace ProjectName.Shared.Protocol
{
    public class PairingSession
    {
        public string DeviceId { get; }
        public string PinCode { get; }
        public DateTime CreatedAtUtc { get; }
        public DateTime ExpiresAtUtc { get; }
        public int FailedAttempts { get; private set; }
        public DateTime? LockedUntilUtc { get; private set; }

        public const int MaxAllowedAttempts = 3;
        public static readonly TimeSpan PinValidityDuration = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        public PairingSession(string deviceId, string pinCode, DateTime? nowUtc = null)
        {
            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            PinCode = pinCode ?? throw new ArgumentNullException(nameof(pinCode));
            CreatedAtUtc = nowUtc ?? DateTime.UtcNow;
            ExpiresAtUtc = CreatedAtUtc.Add(PinValidityDuration);
            FailedAttempts = 0;
            LockedUntilUtc = null;
        }

        public static string GenerateNumericPin()
        {
            // Secure 6-digit PIN
            int code = RandomNumberGenerator.GetInt32(100000, 1000000);
            return code.ToString("D6");
        }

        public PairingValidationResult ValidateAttempt(string submittedPin, DateTime? checkTimeUtc = null)
        {
            DateTime now = checkTimeUtc ?? DateTime.UtcNow;

            if (LockedUntilUtc.HasValue && now < LockedUntilUtc.Value)
            {
                return PairingValidationResult.LockedOut(LockedUntilUtc.Value);
            }

            if (now > ExpiresAtUtc)
            {
                return PairingValidationResult.Expired();
            }

            if (string.Equals(PinCode, submittedPin, StringComparison.Ordinal))
            {
                return PairingValidationResult.Success();
            }

            FailedAttempts++;
            if (FailedAttempts >= MaxAllowedAttempts)
            {
                LockedUntilUtc = now.Add(LockoutDuration);
                return PairingValidationResult.LockedOut(LockedUntilUtc.Value);
            }

            return PairingValidationResult.InvalidPin(MaxAllowedAttempts - FailedAttempts);
        }
    }

    public enum PairingStatus
    {
        Success,
        InvalidPin,
        Expired,
        LockedOut
    }

    public class PairingValidationResult
    {
        public PairingStatus Status { get; }
        public int RemainingAttempts { get; }
        public DateTime? LockedUntilUtc { get; }

        private PairingValidationResult(PairingStatus status, int remainingAttempts, DateTime? lockedUntil)
        {
            Status = status;
            RemainingAttempts = remainingAttempts;
            LockedUntilUtc = lockedUntil;
        }

        public static PairingValidationResult Success() =>
            new(PairingStatus.Success, 0, null);

        public static PairingValidationResult InvalidPin(int remainingAttempts) =>
            new(PairingStatus.InvalidPin, remainingAttempts, null);

        public static PairingValidationResult Expired() =>
            new(PairingStatus.Expired, 0, null);

        public static PairingValidationResult LockedOut(DateTime until) =>
            new(PairingStatus.LockedOut, 0, until);
    }
}
