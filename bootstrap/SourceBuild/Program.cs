using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Own deterministic compiler/packager. Author sources/assets are downloaded on
// the player's PC and never bundled in this repository or its release assets.
internal static class Program
{
    internal const string Commit = "952d0c71ff76d6c140aca83f5d3a1d7a2ab0fe98";
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4) throw new ArgumentException("SourceBuild <official-tML> <author-source.zip> <source-sha256> <new-output.tmod>");
            string official = Path.GetFullPath(args[0]);
            string engineSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(official, "tModLoader.dll"))));
            if (engineSha is not ("FCC6A9624B12191BE4A15D714A3675440911FEB8C1D374642AB1FD686F0DA0A5" or "00D560FAA33CE25522E85792CD1B25C8D5BE95787A10AE2B03FA33AC496BDF2E"))
                throw new IOException("Unsupported official compiler runtime.");
            var libraries = Directory.GetFiles(Path.Combine(official, "Libraries"), "*.dll", SearchOption.AllDirectories);
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                if (name.Name == "tModLoader") return context.LoadFromAssemblyPath(Path.Combine(official, "tModLoader.dll"));
                string? selected = libraries.Where(p => Path.GetFileNameWithoutExtension(p) == name.Name)
                    .OrderBy(p => p.Contains("/runtimes/") || p.Contains("\\runtimes\\") ? 1 : 0).FirstOrDefault();
                return selected is null ? null : context.LoadFromAssemblyPath(selected);
            };
            return Compiler.Run(official, args[1], args[2], args[3], libraries);
        }
        catch (Exception error) { Console.Error.WriteLine("SOURCE_BUILD_FAILED: " + error.GetBaseException().Message); return 1; }
    }
}
internal static class Compiler
{
    internal static int Run(string official, string input, string expectedSha, string output, string[] libraries)
    {
        if (expectedSha.Length != 64 || !expectedSha.All(Uri.IsHexDigit)) throw new IOException("Invalid source hash.");
        byte[] archiveBytes = File.ReadAllBytes(input);
        if (archiveBytes.Length > 4 * 1024 * 1024 || !Convert.ToHexString(SHA256.HashData(archiveBytes)).Equals(expectedSha, StringComparison.OrdinalIgnoreCase)) throw new IOException("Author source archive checksum mismatch.");
        if (File.Exists(output)) throw new IOException("Source build refuses to overwrite an existing package.");
        string scratch = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, ".ore-source-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        using (var stream = new MemoryStream(archiveBytes))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            string prefix = "OreExcavator-" + Program.Commit + "/";
            long expanded = 0;
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith('/')) continue;
                if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal)) throw new IOException("Author source root mismatch.");
                string relative = entry.FullName[prefix.Length..];
                if (relative.Contains('\\') || relative.Split('/').Any(x => x is ".." or "." or "") || relative.Contains(':') || !files.TryAdd(relative, Array.Empty<byte>())) throw new IOException("Unsafe/duplicate author source path.");
                expanded += entry.Length;
                if (entry.Length > 2 * 1024 * 1024 || expanded > 8 * 1024 * 1024 || files.Count > 128) throw new IOException("Author source expansion exceeds budget.");
                using var source = entry.Open(); using var bytes = new MemoryStream(); source.CopyTo(bytes);
                if (bytes.Length != entry.Length) throw new IOException("Author source length mismatch.");
                files[relative] = bytes.ToArray();
            }
        }
        if (!Encoding.UTF8.GetString(files["build.txt"]).Contains("version = 0.8.9")) throw new IOException("Author source version mismatch.");
        var refs = libraries.Where(p => !p.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase)
                && !p.Replace('\\','/').Contains("/Native/") && !p.Contains("tModCodeAssist") && !p.Replace('\\','/').Contains("/runtime"))
            .Append(Path.Combine(official, "tModLoader.dll")).OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => MetadataReference.CreateFromFile(p)).Cast<MetadataReference>().ToList();
        refs.AddRange(Basic.Reference.Assemblies.Net80.References.All);
        refs.Add(MetadataReference.CreateFromImage(files["Dependencies/TUnit.dll"]));
        var parse = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: new[] { "FNA", "TML_2026_08" });
        var trees = files.Where(x => x.Key.EndsWith(".cs", StringComparison.Ordinal)).Select(x =>
            CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(x.Value), parse, "/_/OreExcavator/" + x.Key, Encoding.UTF8));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release,
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default, deterministic: true);
        var compilation = CSharpCompilation.Create("OreExcavator", trees, refs, options);
        using var code = new MemoryStream();
        var result = compilation.Emit(code);
        if (!result.Success) throw new IOException(string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Take(12)));
        // Let the pinned official runtime serialize its own package metadata.
        // Only declarative build.txt is read; no author assembly is executed here.
        string propertiesFile = Path.Combine(scratch, "build.txt");
        // TUnit is used only in the author's #if DEBUG test file. A Release
        // build has no TUnit assembly reference; never activate that old test UI.
        File.WriteAllText(propertiesFile, Encoding.UTF8.GetString(files["build.txt"]), new UTF8Encoding(false));
        var tml = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(official, "tModLoader.dll"));
        var type = tml.GetType("Terraria.ModLoader.Core.BuildProperties", true)!;
        var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object properties = type.GetMethod("ReadBuildFile", flags)!.Invoke(null, new object[] { scratch })!;
        byte[] info = (byte[])type.GetMethod("ToBytes", flags)!.Invoke(properties, null)!;
        var payload = new SortedDictionary<string, byte[]>(StringComparer.Ordinal) { ["Info"] = info, ["OreExcavator.dll"] = code.ToArray() };
        foreach (var file in files.Where(x => x.Key.StartsWith("Localization/", StringComparison.Ordinal) && x.Key.EndsWith(".hjson", StringComparison.Ordinal) || x.Key is "icon.png" or "description.txt")) payload.Add(file.Key, file.Value);
        using var body = new MemoryStream(); using (var writer = new BinaryWriter(body, Encoding.UTF8, true))
        {
            writer.Write("OreExcavator"); writer.Write("0.8.9"); writer.Write(payload.Count);
            foreach (var file in payload) { writer.Write(file.Key); writer.Write(file.Value.Length); writer.Write(file.Value.Length); }
            foreach (var file in payload) writer.Write(file.Value);
        }
        byte[] data = body.ToArray();
        using (var stream = new FileStream(output, FileMode.CreateNew)) using (var writer = new BinaryWriter(stream))
        { writer.Write(Encoding.ASCII.GetBytes("TMOD")); writer.Write("2026.8.3.0"); writer.Write(SHA1.HashData(data)); writer.Write(new byte[256]); writer.Write(data.Length); writer.Write(data); }
        Directory.Delete(scratch, true); // A newly generated, private, fixed-parent scratch directory only.
        Console.WriteLine(JsonSerializer.Serialize(new { status = "PASS", name = "OreExcavator", version = "0.8.9", sourceCommit = Program.Commit, sourceSha256 = expectedSha.ToLowerInvariant(), packageSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))).ToLowerInvariant(), bytes = new FileInfo(output).Length, deterministic = true, authorCodeExecuted = false }));
        return 0;
    }
}
