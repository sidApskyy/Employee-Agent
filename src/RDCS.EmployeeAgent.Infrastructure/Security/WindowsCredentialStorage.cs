using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Shared.Constants;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace RDCS.EmployeeAgent.Infrastructure.Security;

public class WindowsCredentialStorage : ITokenStorage
{
    private const string TargetName = ApplicationConstants.CredentialTarget;
    private static AgentIdentity? _cachedIdentity;

    public async Task StoreTokensAsync(AgentIdentity identity, CancellationToken cancellationToken = default)
    {
        // Development mode: use in-memory storage
        #if DEBUG
        await Task.Run(() =>
        {
            _cachedIdentity = identity;
        }, cancellationToken);
        #else
        await Task.Run(() =>
        {
            var credentialData = SerializeIdentity(identity);
            WriteCredential(TargetName, identity.EmployeeId, credentialData);
        }, cancellationToken);
        #endif
    }

    public async Task<AgentIdentity?> RetrieveTokensAsync(CancellationToken cancellationToken = default)
    {
        // Development mode: use in-memory storage
        #if DEBUG
        return await Task.Run(() =>
        {
            return _cachedIdentity;
        }, cancellationToken);
        #else
        return await Task.Run(() =>
        {
            if (!TryReadCredential(TargetName, out var username, out var password))
            {
                return null;
            }

            return DeserializeIdentity(password);
        }, cancellationToken);
        #endif
    }

    public async Task ClearTokensAsync(CancellationToken cancellationToken = default)
    {
        // Development mode: use in-memory storage
        #if DEBUG
        await Task.Run(() =>
        {
            _cachedIdentity = null;
        }, cancellationToken);
        #else
        await Task.Run(() =>
        {
            DeleteCredential(TargetName);
        }, cancellationToken);
        #endif
    }

    private static string SerializeIdentity(AgentIdentity identity)
    {
        return System.Text.Json.JsonSerializer.Serialize(identity);
    }

    private static AgentIdentity? DeserializeIdentity(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<AgentIdentity>(json);
    }

    #region Windows Credential Manager Interop

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref CREDENTIAL credential, [In] uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);

    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;  // CRED_PERSIST_LOCAL_MACHINE correct value

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    private static void WriteCredential(string target, string username, string password)
    {
        var passwordBytes = Encoding.Unicode.GetBytes(password);

        // Windows Credential Manager hard limit is 2500 bytes
        if (passwordBytes.Length > 2500)
            throw new InvalidOperationException($"Credential data too large ({passwordBytes.Length} bytes). Max is 2500 bytes.");

        var targetPtr = Marshal.StringToCoTaskMemUni(target);
        var blobPtr = Marshal.AllocCoTaskMem(passwordBytes.Length);
        var userPtr = Marshal.StringToCoTaskMemUni(username);

        try
        {
            Marshal.Copy(passwordBytes, 0, blobPtr, passwordBytes.Length);

            var credential = new CREDENTIAL
            {
                Flags = 0,
                Type = CRED_TYPE_GENERIC,
                TargetName = targetPtr,
                Comment = IntPtr.Zero,
                LastWritten = new System.Runtime.InteropServices.ComTypes.FILETIME { dwLowDateTime = 0, dwHighDateTime = 0 },
                CredentialBlobSize = (uint)passwordBytes.Length,
                CredentialBlob = blobPtr,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = IntPtr.Zero,
                UserName = userPtr
            };

            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeCoTaskMem(targetPtr);
            Marshal.FreeCoTaskMem(blobPtr);
            Marshal.FreeCoTaskMem(userPtr);
        }
    }

    private static bool TryReadCredential(string target, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (!CredRead(target, CRED_TYPE_GENERIC, 0, out var credentialPtr))
        {
            return false;
        }

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
            username = Marshal.PtrToStringUni(credential.UserName) ?? string.Empty;
            password = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2) ?? string.Empty;
            return true;
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    private static void DeleteCredential(string target)
    {
        if (!CredDelete(target, CRED_TYPE_GENERIC, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 1168) // ERROR_NOT_FOUND
            {
                throw new Win32Exception(error);
            }
        }
    }

    #endregion
}
