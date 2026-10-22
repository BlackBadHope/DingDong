using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

namespace DingDong.SteamLicense;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.SequenceEqual(new[] { "--self-test" })) return SelfTest.Run();
            if (args.Length != 2 || args[0] != "--official-tml")
                throw new ArgumentException("Usage: SteamLicenseGate --official-tml <official Steam tModLoader folder>");
            using var steam = new InstalledSteamSession(args[1]);
            var result = LicenseGate.Check(steam);
            Console.WriteLine("DINGDONG_LICENSE_RESULT=" + JsonSerializer.Serialize(result));
            return result.Verified ? 0 : 20;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("STEAM_LICENSE_UNVERIFIED: " + error.GetBaseException().Message);
            return 21;
        }
    }
}

internal sealed record LicenseResult(bool Verified, string Code, string? TmlPath = null, string? TerrariaPath = null,
    string Evidence = "Active Steam entitlement; no payment-history or personal-purchase claim");

internal interface ISteamSession : IDisposable
{
    bool IsSteamRunning();
    bool Initialize();
    bool IsLoggedOn();
    uint ContextAppId();
    bool IsSubscribed(uint appId);
    bool IsInstalled(uint appId);
    string InstallDirectory(uint appId);
}

internal static class LicenseGate
{
    internal const uint Terraria = 105600, Tml = 1281930;
    internal static LicenseResult Check(ISteamSession steam)
    {
        if (!steam.IsSteamRunning()) return new(false, "STEAM_NOT_RUNNING");
        if (!steam.Initialize()) return new(false, "STEAM_INIT_FAILED");
        if (steam.ContextAppId() != Tml) return new(false, "STEAM_CONTEXT_MISMATCH");
        if (!steam.IsLoggedOn()) return new(false, "STEAM_NOT_ONLINE");
        // The free tML licence is never accepted as proof of Terraria entitlement.
        if (!steam.IsSubscribed(Terraria)) return new(false, "TERRARIA_LICENSE_MISSING");
        if (!steam.IsInstalled(Terraria) || !steam.IsInstalled(Tml)) return new(false, "STEAM_INSTALL_INCOMPLETE");
        var tml = steam.InstallDirectory(Tml);
        var terraria = steam.InstallDirectory(Terraria);
        if (string.IsNullOrWhiteSpace(tml) || string.IsNullOrWhiteSpace(terraria)) return new(false, "STEAM_INSTALL_PATH_UNVERIFIED");
        return new(true, "STEAM_TERRARIA_ENTITLEMENT_VERIFIED", Path.GetFullPath(tml), Path.GetFullPath(terraria));
    }
}

// No Steam/SDK binary is distributed. Both assemblies come from the selected,
// pinned official tML installation. This class is never constructed by SelfTest.
internal sealed class InstalledSteamSession : ISteamSession
{
    private const string ManagedSha = "F9EFA5D01D4606379236411FD884664531F5A1BC0D60141024975929AF91A8D8";
    private const string NativeSha = "E082BF5C9F881C822B1540A76B74F9D15E18019A73EBD206A559595BADCB7F65";
    private const string Library = "Libraries/steamworks.net.anycpu/2025.162.4/";
    private readonly Assembly api;
    private readonly string selectedTml;
    private bool initialized;

    internal InstalledSteamSession(string officialTml)
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("This candidate supports Windows x64 only.");
        selectedTml = Path.GetFullPath(officialTml).TrimEnd(Path.DirectorySeparatorChar);
        // lib/net8.0 contains the compile-time reference assembly in this tML
        // package. Execute only the pinned Windows runtime implementation.
        var managed = Path.Combine(selectedTml, Library, "runtimes/win/lib/net8.0/Steamworks.NET.dll");
        var native = Path.Combine(selectedTml, Library, "runtimes/win-x64/native/steam_api64.dll");
        VerifyLocalFile(managed, ManagedSha); VerifyLocalFile(native, NativeSha);
        // This is the real tML API context, not a fabricated ownership response.
        RequireContextVariable("SteamAppId"); RequireContextVariable("SteamGameId");
        api = AssemblyLoadContext.Default.LoadFromAssemblyPath(managed);
        NativeLibrary.SetDllImportResolver(api, (name, _, _) =>
        {
            if (name is "steam_api64" or "steam_api64.dll" or "steam_api" or "steam_api.dll")
                return NativeLibrary.Load(native);
            return IntPtr.Zero;
        });
    }

    private static void RequireContextVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(value) && value != LicenseGate.Tml.ToString())
            throw new InvalidOperationException("Conflicting Steam app context: " + name);
        Environment.SetEnvironmentVariable(name, LicenseGate.Tml.ToString(), EnvironmentVariableTarget.Process);
    }

    private static void VerifyLocalFile(string path, string expected)
    {
        var current = new FileInfo(path);
        for (FileSystemInfo? node = current; node != null; node = node is FileInfo f ? f.Directory : ((DirectoryInfo)node).Parent)
            if ((node.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked Steam library paths are not supported by this candidate.");
        using var input = File.OpenRead(path);
        if (Convert.ToHexString(SHA256.HashData(input)) != expected)
            throw new InvalidOperationException("Unsupported official Steam API file. Verify the pinned tML installation through Steam.");
    }

    private object Call(string type, string method, params object?[] arguments)
    {
        var owner = api.GetType("Steamworks." + type, true)!;
        var member = owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == method && m.GetParameters().Length == arguments.Length);
        return member.Invoke(null, arguments) ?? throw new InvalidOperationException("Steam API returned no result: " + method);
    }
    private object App(uint appId) => Activator.CreateInstance(api.GetType("Steamworks.AppId_t", true)!, new object[] { appId })!;
    public bool IsSteamRunning() => (bool)Call("SteamAPI", "IsSteamRunning");
    public bool Initialize() => initialized = (bool)Call("SteamAPI", "Init");
    public bool IsLoggedOn() => (bool)Call("SteamUser", "BLoggedOn");
    public uint ContextAppId()
    {
        var value = Call("SteamUtils", "GetAppID");
        return (uint)value.GetType().GetField("m_AppId")!.GetValue(value)!;
    }
    public bool IsSubscribed(uint appId) => (bool)Call("SteamApps", "BIsSubscribedApp", App(appId));
    public bool IsInstalled(uint appId) => (bool)Call("SteamApps", "BIsAppInstalled", App(appId));
    public string InstallDirectory(uint appId)
    {
        object?[] arguments = { App(appId), null, 4096u };
        if ((uint)Call("SteamApps", "GetAppInstallDir", arguments) == 0 || arguments[1] is not string directory) return "";
        directory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
        if (appId == LicenseGate.Tml && !string.Equals(directory, selectedTml, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The selected tML folder differs from the active Steam installation.");
        return directory;
    }
    public void Dispose()
    {
        if (initialized) api.GetType("Steamworks.SteamAPI", true)!.GetMethod("Shutdown", Type.EmptyTypes)!.Invoke(null, null);
    }
}

internal static class SelfTest
{
    private sealed class MockSteam : ISteamSession
    {
        internal bool Running = true, Init = true, Online = true, TerrariaOwned = true, Installed = true;
        internal uint Context = LicenseGate.Tml;
        internal readonly List<uint> Queried = new();
        public bool IsSteamRunning() => Running;
        public bool Initialize() => Init;
        public bool IsLoggedOn() => Online;
        public uint ContextAppId() => Context;
        public bool IsSubscribed(uint appId) { Queried.Add(appId); return appId == LicenseGate.Terraria && TerrariaOwned; }
        public bool IsInstalled(uint appId) => Installed;
        public string InstallDirectory(uint appId) => Path.Combine(Path.GetTempPath(), "DingDongGateMock", appId.ToString());
        public void Dispose() { }
    }
    internal static int Run()
    {
        var cases = new (string Name, MockSteam Steam, bool Expected)[] {
            ("Steam absent", new() { Running = false }, false), ("Init failed", new() { Init = false }, false),
            ("Offline", new() { Online = false }, false), ("Wrong context", new() { Context = 480 }, false),
            ("Free tML without Terraria", new() { TerrariaOwned = false }, false),
            ("Not installed", new() { Installed = false }, false), ("Licensed installed Terraria", new(), true)
        };
        foreach (var item in cases)
        {
            var result = LicenseGate.Check(item.Steam);
            if (result.Verified != item.Expected || item.Steam.Queried.Any(id => id != LicenseGate.Terraria))
                throw new InvalidOperationException("Mock failed: " + item.Name);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { verdict = "PASS", cases = cases.Length, steamLoaded = false, gameLaunched = false }));
        return 0;
    }
}
