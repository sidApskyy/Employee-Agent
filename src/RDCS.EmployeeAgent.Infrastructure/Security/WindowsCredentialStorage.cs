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
    private static extern bool CredRead(string target, [In] uint type, [In] uint flags, [Out] out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete([In] string target, [In] uint type, [In] uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);

    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 4;

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
        var credential = new CREDENTIAL
        {
            Flags = 0,
            Type = CRED_TYPE_GENERIC,
            TargetName = Marshal.StringToCoTaskMemUni(target),
            Comment = IntPtr.Zero,
            LastWritten = new System.Runtime.InteropServices.ComTypes.FILETIME
            {
                dwLowDateTime = 0,
                dwHighDateTime = 0
            },
            CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(password),
            CredentialBlob = Marshal.StringToCoTaskMemUni(password),
            Persist = CRED_PERSIST_LOCAL_MACHINE,
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            TargetAlias = IntPtr.Zero,
            UserName = Marshal.StringToCoTaskMemUni(username)
        };

        if (!CredWrite(ref credential, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Marshal.FreeCoTaskMem(credential.TargetName);
        Marshal.FreeCoTaskMem(credential.CredentialBlob);
        Marshal.FreeCoTaskMem(credential.UserName);
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
