using System;
using System.Collections.Concurrent;
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
            // Cryptographically secure 6-digit numeric PIN across the full [000000, 999999] keyspace
            int code = RandomNumberGenerator.GetInt32(0, 1000000);
            return code.ToString("D6");
        }

        public PairingValidationResult ValidateAttempt(string? submittedPin, DateTime? checkTimeUtc = null)
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

            if (!string.IsNullOrEmpty(submittedPin) && string.Equals(PinCode, submittedPin.Trim(), StringComparison.Ordinal))
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

    public class PairingManager
    {
        private readonly ConcurrentDictionary<string, PairingSession> _activeSessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DateTime> _deviceLockouts = new(StringComparer.OrdinalIgnoreCase);

        public PairingSession CreateSession(string deviceId, DateTime? nowUtc = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentNullException(nameof(deviceId));
            DateTime now = nowUtc ?? DateTime.UtcNow;

            if (IsDeviceLockedOut(deviceId, now, out var lockedUntil))
            {
                throw new InvalidOperationException($"Device '{deviceId}' is currently locked out until {lockedUntil:o} due to excessive failed attempts.");
            }

            string pin = PairingSession.GenerateNumericPin();
            var session = new PairingSession(deviceId, pin, now);
            _activeSessions[deviceId] = session;
            return session;
        }

        public PairingValidationResult ValidateAttempt(string deviceId, string? submittedPin, DateTime? nowUtc = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return PairingValidationResult.InvalidPin(0);
            DateTime now = nowUtc ?? DateTime.UtcNow;

            if (IsDeviceLockedOut(deviceId, now, out var lockedUntil))
            {
                return PairingValidationResult.LockedOut(lockedUntil!.Value);
            }

            if (!_activeSessions.TryGetValue(deviceId, out var session))
            {
                return PairingValidationResult.Expired();
            }

            var result = session.ValidateAttempt(submittedPin, now);
            if (result.Status == PairingStatus.LockedOut && result.LockedUntilUtc.HasValue)
            {
                _deviceLockouts[deviceId] = result.LockedUntilUtc.Value;
            }
            else if (result.Status == PairingStatus.Success)
            {
                _activeSessions.TryRemove(deviceId, out _);
                _deviceLockouts.TryRemove(deviceId, out _);
            }

            return result;
        }

        public bool IsDeviceLockedOut(string deviceId, DateTime? nowUtc, out DateTime? lockedUntil)
        {
            DateTime now = nowUtc ?? DateTime.UtcNow;
            if (_deviceLockouts.TryGetValue(deviceId, out var until))
            {
                if (now < until)
                {
                    lockedUntil = until;
                    return true;
                }
                _deviceLockouts.TryRemove(deviceId, out _);
            }

            lockedUntil = null;
            return false;
        }

        public bool Revoke(string deviceId)
        {
            bool removedSession = _activeSessions.TryRemove(deviceId, out _);
            bool removedLockout = _deviceLockouts.TryRemove(deviceId, out _);
            return removedSession || removedLockout;
        }

        public void PruneExpired(DateTime? nowUtc = null)
        {
            DateTime now = nowUtc ?? DateTime.UtcNow;
            foreach (var kvp in _activeSessions)
            {
                if (now > kvp.Value.ExpiresAtUtc && (!kvp.Value.LockedUntilUtc.HasValue || now >= kvp.Value.LockedUntilUtc.Value))
                {
                    _activeSessions.TryRemove(kvp.Key, out _);
                }
            }

            foreach (var kvp in _deviceLockouts)
            {
                if (now >= kvp.Value)
                {
                    _deviceLockouts.TryRemove(kvp.Key, out _);
                }
            }
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
