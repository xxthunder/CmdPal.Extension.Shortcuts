using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CmdPal.Extension.Shortcuts;

/// <summary>Writes Windows shortcuts (.lnk) through the shell's ShellLink COM object.</summary>
internal static partial class ShellLink
{
    private static readonly Guid ClsidShellLink = new("00021401-0000-0000-C000-000000000046");
    private static readonly Guid IidShellLinkW = new("000214F9-0000-0000-C000-000000000046");
    private static readonly StrategyBasedComWrappers ComWrappers = new();

    /// <summary>Writes a shortcut at <paramref name="linkPath"/> that points to <paramref name="targetPath"/>.</summary>
    public static void Create(string linkPath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(linkPath);
        ArgumentException.ThrowIfNullOrEmpty(targetPath);

        var pointer = CreateShellLink();
        object instance;
        try
        {
            instance = ComWrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(pointer);
        }

        try
        {
            ((IShellLinkW)instance).SetPath(targetPath);
            ((IPersistFile)instance).Save(linkPath, 1);
        }
        finally
        {
            ((ComObject)instance).FinalRelease();
        }
    }

    private static nint CreateShellLink()
    {
        var hr = NativeMethods.CoCreateInstance(ClsidShellLink, 0, NativeMethods.ClsctxInprocServer, IidShellLinkW, out var pointer);
        if (hr == NativeMethods.CoENotInitialized)
        {
            Marshal.ThrowExceptionForHR(NativeMethods.CoInitializeEx(0, NativeMethods.CoinitMultithreaded));
            hr = NativeMethods.CoCreateInstance(ClsidShellLink, 0, NativeMethods.ClsctxInprocServer, IidShellLinkW, out pointer);
        }

        Marshal.ThrowExceptionForHR(hr);
        return pointer;
    }

    private static partial class NativeMethods
    {
        public const uint ClsctxInprocServer = 0x1;
        public const uint CoinitMultithreaded = 0x0;
        public const int CoENotInitialized = unchecked((int)0x800401F0);

        [LibraryImport("ole32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial int CoCreateInstance(in Guid rclsid, nint pUnkOuter, uint dwClsContext, in Guid riid, out nint ppv);

        [LibraryImport("ole32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial int CoInitializeEx(nint pvReserved, uint dwCoInit);
    }
}

/// <summary>
/// IShellLinkW from ShObjIdl_core.h, every method in vtable order after IUnknown.
/// Methods this extension does not call take raw pointers as <see cref="nint"/>.
/// </summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal partial interface IShellLinkW
{
    void GetPath(nint pszFile, int cch, nint pfd, uint fFlags);

    void GetIDList(nint ppidl);

    void SetIDList(nint pidl);

    void GetDescription(nint pszName, int cch);

    void SetDescription(string pszName);

    void GetWorkingDirectory(nint pszDir, int cch);

    void SetWorkingDirectory(string pszDir);

    void GetArguments(nint pszArgs, int cch);

    void SetArguments(string pszArgs);

    void GetHotkey(nint pwHotkey);

    void SetHotkey(ushort wHotkey);

    void GetShowCmd(nint piShowCmd);

    void SetShowCmd(int iShowCmd);

    void GetIconLocation(nint pszIconPath, int cch, nint piIcon);

    void SetIconLocation(string pszIconPath, int iIcon);

    void SetRelativePath(string pszPathRel, uint dwReserved);

    void Resolve(nint hwnd, uint fFlags);

    void SetPath(string pszFile);
}

/// <summary>
/// IPersistFile from ObjIdl.h, with IPersist's GetClassID first so the vtable after IUnknown
/// is GetClassID, IsDirty, Load, Save, SaveCompleted, GetCurFile.
/// </summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("0000010b-0000-0000-C000-000000000046")]
internal partial interface IPersistFile
{
    void GetClassID(nint pClassID);

    // Returns S_OK or S_FALSE; both are success, so the HRESULT is kept.
    [PreserveSig]
    int IsDirty();

    void Load(string pszFileName, uint dwMode);

    void Save(string pszFileName, int fRemember);

    void SaveCompleted(string pszFileName);

    void GetCurFile(nint ppszFileName);
}
