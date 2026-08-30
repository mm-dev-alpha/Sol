using System;

namespace Sol.Models;

/// <summary>
/// Diagnostic snapshot representing the BitLocker drive encryption state of an endpoint's system volume.
/// </summary>
public record ComputerBitLockerSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public string DriveLetter { get; init; } = "C:";
    public uint ProtectionStatus { get; init; } = 1; // 0 = Off/Suspended, 1 = On, 2 = Unknown
    public uint ConversionStatus { get; init; } = 1; // 1 = Fully Encrypted
    public uint EncryptionMethod { get; init; } = 7; // 7 = XTS-AES 256, 6 = XTS-AES 128, 4 = AES 256, 3 = AES 128
    public bool IsSuspended { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public bool IsProtectionActive => IsSuccess && ProtectionStatus == 1 && !IsSuspended;
    public bool IsProtectionSuspended => IsSuccess && (IsSuspended || ProtectionStatus == 0);
    public bool IsFullyEncrypted => ConversionStatus == 1;

    public string FormattedEncryptionMethod => EncryptionMethod switch
    {
        6 => "XTS-AES 128-Bit",
        7 => "XTS-AES 256-Bit",
        3 => "AES-CBC 128-Bit",
        4 => "AES-CBC 256-Bit",
        1 => "AES-128 + Diffuser",
        2 => "AES-256 + Diffuser",
        5 => "Hardware Encryption",
        0 => "None",
        _ => "AES 256-Bit"
    };

    public string FormattedConversionStatus => ConversionStatus switch
    {
        1 => "Fully Encrypted (100 %)",
        0 => "Fully Decrypted",
        2 => "Encryption in progress",
        3 => "Decryption in progress",
        4 => "Encryption paused",
        5 => "Decryption paused",
        _ => "Encrypted"
    };
}
