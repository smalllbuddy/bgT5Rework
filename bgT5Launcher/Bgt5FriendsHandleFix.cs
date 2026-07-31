using System;
using System.IO;
using System.Security.Cryptography;

namespace bgT5Launcher;

internal static class Bgt5FriendsHandleFix
{
    internal enum ApplyResult
    {
        AlreadyApplied,
        Applied,
        Missing,
        Unsupported
    }

    private const int ExpectedLength = 792064;
    private const int EntryJumpOffset = 0x20A44;
    private const int StubOffset = 0x20A9F;

    private static readonly byte[] OriginalEntry =
    {
        0xEB, 0x48
    };

    private static readonly byte[] PatchedEntry =
    {
        0xEB, 0x59
    };

    // Preserve the return pointer, close the FindFirstFile handle stored at
    // [ebp-8], restore the pointer, and return through the original cleanup.
    //
    // 50                         push eax
    // FF 75 F8                   push dword ptr [ebp-8]
    // E8 1A F9 00 00             call FindClose import thunk
    // 58                         pop eax
    // EB E3                      jmp original cleanup
    private static readonly byte[] HandleCloseStub =
    {
        0x50,
        0xFF, 0x75, 0xF8,
        0xE8, 0x1A, 0xF9, 0x00, 0x00,
        0x58,
        0xEB, 0xE3
    };

    private const string OriginalSha256 =
        "23CF2F2A53E48811B6C3C13974D57F44D129B22951A74F18C8846236D2CCB6E4";

    internal static ApplyResult Apply(string baseDirectory)
    {
        string path = Path.Combine(baseDirectory, "bgt5.dll");
        if (!File.Exists(path))
            return ApplyResult.Missing;

        byte[] image = File.ReadAllBytes(path);
        if (image.Length != ExpectedLength)
            return ApplyResult.Unsupported;

        if (Matches(image, EntryJumpOffset, PatchedEntry) &&
            Matches(image, StubOffset, HandleCloseStub))
        {
            return ApplyResult.AlreadyApplied;
        }

        if (!string.Equals(GetSha256(image), OriginalSha256, StringComparison.OrdinalIgnoreCase) ||
            !Matches(image, EntryJumpOffset, OriginalEntry) ||
            !IsPadding(image, StubOffset, HandleCloseStub.Length))
        {
            return ApplyResult.Unsupported;
        }


        Buffer.BlockCopy(PatchedEntry, 0, image, EntryJumpOffset, PatchedEntry.Length);
        Buffer.BlockCopy(HandleCloseStub, 0, image, StubOffset, HandleCloseStub.Length);

        string temporaryPath = path + ".0.4.4.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, image);
            File.Copy(temporaryPath, path, true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
            }
        }

        return ApplyResult.Applied;
    }

    private static bool Matches(byte[] image, int offset, byte[] expected)
    {
        if (offset < 0 || offset + expected.Length > image.Length)
            return false;

        for (int index = 0; index < expected.Length; index++)
        {
            if (image[offset + index] != expected[index])
                return false;
        }

        return true;
    }

    private static bool IsPadding(byte[] image, int offset, int length)
    {
        if (offset < 0 || offset + length > image.Length)
            return false;

        for (int index = 0; index < length; index++)
        {
            if (image[offset + index] != 0xCC)
                return false;
        }

        return true;
    }

    private static string GetSha256(byte[] image)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(image);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
