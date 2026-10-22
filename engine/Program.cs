using System.Collections;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 2) throw new ArgumentException("Usage: EnginePatch <official-tModLoader.dll> <new-private-output.dll>");
string Source = Path.GetFullPath(args[0]);
string requestedOutput = Path.GetFullPath(args[1]);
const string SourceSha = "fcc6a9624b12191be4a15d714a3675440911feb8c1d374642ab1fd686f0da0a5";
const string ExpectedOutputSha = "00d560faa33ce25522e85792cd1b25c8d5be95787a10ae2b03fa33ac496bdf2e";
const string OldLiteral = "tModLoader.dll -server ";
const string NewLiteral = "tModLoader.dll -server -steamworkshopfolder \"DingDongWorkshop\" ";
const string MethodName = "System.Void Terraria.Main::OnSubmitServerPassword()";
Check(!string.Equals(Source, requestedOutput, StringComparison.OrdinalIgnoreCase), "Never patch the input in place");
Check(!File.Exists(requestedOutput), "Refusing to overwrite the private output");
string reportPath = requestedOutput + ".patch-verification.json";
Check(!File.Exists(reportPath), "Refusing to overwrite the verification report");
Check(HashFile(Source) == SourceSha, "Official input hash is unsupported. Verify/reinstall the pinned official Steam tModLoader build.");
string output = requestedOutput + "." + Guid.NewGuid().ToString("N") + ".pending";
bool verifyExisting = false;
try {
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Source)!);
foreach (var folder in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(Source)!, "Libraries"), "*.dll", SearchOption.AllDirectories).Select(Path.GetDirectoryName).Distinct())
    resolver.AddSearchDirectory(folder!);
resolver.AddSearchDirectory(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
using var source = AssemblyDefinition.ReadAssembly(Source, new ReaderParameters { ReadSymbols = false, InMemory = true, AssemblyResolver = resolver });
Check(!source.Name.HasPublicKey, "Strong-named assembly requires a different procedure");
var sourceMethods = AllTypes(source.MainModule).SelectMany(t => t.Methods).ToArray();
var originalStacks = sourceMethods.Where(m => m.HasBody).Select(m => (Name: m.FullName, Stack: m.Body.MaxStackSize)).ToArray();
var method = sourceMethods.Single(m => m.FullName == MethodName);
string originalToken = method.MetadataToken.ToString();
var matches = method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldstr && Equals(i.Operand, OldLiteral)).ToArray();
Check(matches.Length == 1, "Expected exactly one original server prefix");
Check(!sourceMethods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i => Equals(i.Operand, NewLiteral)), "Patch already exists");
Check(sourceMethods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Count(i => Equals(i.Operand, OldLiteral)) == 1, "Server prefix is not globally unique");
int targetIndex = method.Body.Instructions.IndexOf(matches[0]);
int targetOffset = matches[0].Offset;
var baselineSnapshot = Snapshot(source, null, -1, null);
var nativeBefore = NativeResources(Source);
var versionBefore = VersionMetadata(Source);
matches[0].Operand = NewLiteral;
// Only a fixed-width ldstr metadata operand changes. No macro/branch rewrite is required.
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
int restoredMaxStackFields = 0;
if (!verifyExisting) {
    source.Write(output, new WriterParameters { WriteSymbols = false });
    restoredMaxStackFields = RestoreOriginalMaxStacks(output, originalStacks);
}
using var candidate = AssemblyDefinition.ReadAssembly(output, new ReaderParameters { ReadSymbols = false, InMemory = true });
var patchedMethod = AllTypes(candidate.MainModule).SelectMany(t => t.Methods).Single(m => m.FullName == MethodName);
Check(patchedMethod.Body.Instructions.Count == method.Body.Instructions.Count, "Instruction count changed");
Check(Equals(patchedMethod.Body.Instructions[targetIndex].Operand, NewLiteral), "Patched prefix is missing");
var normalizedSnapshot = Snapshot(candidate, MethodName, targetIndex, OldLiteral);
if (!baselineSnapshot.SequenceEqual(normalizedSnapshot)) {
    var diffs = Enumerable.Range(0, Math.Max(baselineSnapshot.Length, normalizedSnapshot.Length))
        .Where(i => i >= baselineSnapshot.Length || i >= normalizedSnapshot.Length || baselineSnapshot[i] != normalizedSnapshot[i])
        .Take(12).Select(i => new { index = i, before = i < baselineSnapshot.Length ? baselineSnapshot[i] : "END", after = i < normalizedSnapshot.Length ? normalizedSnapshot[i] : "END" }).ToArray();
    Console.WriteLine(JsonSerializer.Serialize(diffs, new JsonSerializerOptions { WriteIndented = true }));
    Check(false, "Assembly semantics differ beyond the one allowed ldstr");
}
var nativeAfter = NativeResources(output);
Check(nativeBefore.SequenceEqual(nativeAfter), "Native resource payload changed");
Check(VersionMetadata(output) == versionBefore, "Version resource changed");
Check(HashFile(Source) == SourceSha, "Official input DLL changed during preparation");
Check(HashFile(output) == ExpectedOutputSha, "Output hash differs from the pinned DingDong engine. No output will be published.");
var report = new {
    verdict = "PASS", scope = "Static candidate assembly only; game/server/installer NOT_RUN",
    sourceSha256 = SourceSha, candidateSha256 = HashFile(output), candidate = requestedOutput,
    method = MethodName, originalToken, candidateToken = patchedMethod.MetadataToken.ToString(), instructionIndex = targetIndex,
    originalIlOffset = targetOffset.ToString("X4"), oldLiteral = OldLiteral, newLiteral = NewLiteral,
    semanticMethodCount = sourceMethods.Length, normalizedAssemblySemanticsEqual = true,
    nativeResourcesEqual = true, nativeResourceCount = nativeBefore.Length,
    managedResourcesEqual = true, versionMetadataEqual = true, moduleMvidEqual = source.MainModule.Mvid == candidate.MainModule.Mvid,
    localVariablesAndBranchesUnchanged = true, strongName = false, wroteSymbols = false,
    originalMaxStackValuesPreserved = true, restoredMaxStackFields,
    otherLiveFilesChanged = false, modsOrSavesChanged = false,
    limits = new[] {
        "Pins this DingDong package to its existing relative DingDongWorkshop directory; does not forward arbitrary custom client paths.",
        "Original relative tModLoader.dll already requires the process working directory to contain the game assembly.",
        "Missing DingDongWorkshop would cause the upstream resolver to fall back; installer/launcher must retain the directory.",
        "Cecil rewrites PE/metadata layout; unchanged method semantics/resources are checked, not whole-file byte identity.",
        "Symbols are not rewritten. Game loading, Host & Play, sync and distribution acceptance remain separate root checks."
    }
};
File.Move(output, requestedOutput);
using (var reportFile = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write))
    JsonSerializer.Serialize(reportFile, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

} finally { if (File.Exists(output)) File.Delete(output); }

static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
static string HashFile(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module) => module.Types.SelectMany(Walk);
static IEnumerable<TypeDefinition> Walk(TypeDefinition type) { yield return type; foreach (var nested in type.NestedTypes.SelectMany(Walk)) yield return nested; }

static string Value(object? value) => value switch {
    null => "null", TypeReference t => t.FullName, MethodReference m => m.FullName,
    FieldReference f => f.FullName, ParameterDefinition p => $"arg:{p.Index}:{p.ParameterType.FullName}",
    VariableDefinition v => $"local:{v.Index}:{v.VariableType.FullName}",
    CustomAttributeArgument a => a.Type.FullName + "=" + Value(a.Value),
    string s => JsonSerializer.Serialize(s), IEnumerable list => "[" + string.Join(",", list.Cast<object>().Select(Value)) + "]",
    IFormattable v => v.ToString(null, System.Globalization.CultureInfo.InvariantCulture), _ => value.ToString() ?? "null"
};
// Raw attribute blobs preserve exact data without resolving optional annotation assemblies.
static string Attrs(Mono.Cecil.ICustomAttributeProvider owner) => string.Join("|", owner.CustomAttributes.Select(a =>
    a.Constructor.FullName + "|" + Hash(a.GetBlob())));

static string[] Snapshot(AssemblyDefinition asm, string? normalizeMethod, int normalizeIndex, string? replacement) {
    var result = new List<string> { "ASSEMBLY|" + asm.Name.FullName + "|" + Attrs(asm),
        "MODULE|" + asm.MainModule.Name + "|" + asm.MainModule.Mvid + "|" + asm.MainModule.Kind + "|" + asm.MainModule.Architecture + "|" + asm.MainModule.Attributes + "|" + asm.MainModule.RuntimeVersion + "|" + Attrs(asm.MainModule) };
    result.AddRange(asm.MainModule.AssemblyReferences.Select(r => "REF|" + r.FullName));
    result.AddRange(asm.MainModule.ModuleReferences.Select(r => "MODREF|" + r.Name));
    result.AddRange(asm.MainModule.ExportedTypes.Select(r => "EXPORT|" + r.FullName + "|" + r.Attributes + "|" + r.Scope));
    foreach (var resource in asm.MainModule.Resources) {
        result.Add("RESOURCE|" + resource.Name + "|" + resource.Attributes + "|" + resource.ResourceType + "|" +
            (resource is EmbeddedResource embedded ? Hash(embedded.GetResourceData()) : resource.ToString()));
    }
    foreach (var type in AllTypes(asm.MainModule)) {
        result.Add("TYPE|" + type.FullName + "|" + type.Attributes + "|" + type.BaseType?.FullName + "|" + type.ClassSize + "|" + type.PackingSize + "|" + Attrs(type));
        result.AddRange(type.GenericParameters.Select(g => "TYPEGENERIC|" + g.Name + "|" + g.Attributes + "|" + string.Join(",", g.Constraints.Select(c => c.ConstraintType.FullName)) + "|" + Attrs(g)));
        result.AddRange(type.Interfaces.Select(i => "INTERFACE|" + i.InterfaceType.FullName + "|" + Attrs(i)));
        foreach (var field in type.Fields)
            result.Add("FIELD|" + field.FullName + "|" + field.Attributes + "|" + Value(field.Constant) + "|" + field.Offset + "|" + Hash(field.InitialValue) + "|" + Attrs(field));
        foreach (var prop in type.Properties)
            result.Add("PROPERTY|" + prop.FullName + "|" + prop.Attributes + "|" + Value(prop.Constant) + "|" + prop.GetMethod?.FullName + "|" + prop.SetMethod?.FullName + "|" + Attrs(prop));
        foreach (var evt in type.Events)
            result.Add("EVENT|" + evt.FullName + "|" + evt.Attributes + "|" + evt.AddMethod?.FullName + "|" + evt.RemoveMethod?.FullName + "|" + evt.InvokeMethod?.FullName + "|" + Attrs(evt));
        foreach (var method in type.Methods) {
            result.Add("METHOD|" + method.FullName + "|" + method.Attributes + "|" + method.ImplAttributes + "|" + method.CallingConvention + "|" + Attrs(method));
            result.AddRange(method.GenericParameters.Select(g => "METHODGENERIC|" + g.Name + "|" + g.Attributes + "|" + string.Join(",", g.Constraints.Select(c => c.ConstraintType.FullName)) + "|" + Attrs(g)));
            result.AddRange(method.Overrides.Select(o => "OVERRIDE|" + o.FullName));
            result.AddRange(method.Parameters.Select(p => "PARAM|" + p.Index + "|" + p.Name + "|" + p.Attributes + "|" + p.ParameterType.FullName + "|" + Value(p.Constant) + "|" + Attrs(p)));
            if (!method.HasBody) continue;
            var body = method.Body;
            result.Add("BODY|" + body.InitLocals + "|" + body.MaxStackSize + "|" + string.Join(",", body.Variables.Select(v => v.VariableType.FullName)));
            int Index(Instruction? i) => i is null ? -1 : body.Instructions.IndexOf(i);
            for (int i = 0; i < body.Instructions.Count; i++) {
                var instruction = body.Instructions[i];
                object? operand = method.FullName == normalizeMethod && i == normalizeIndex ? replacement : instruction.Operand;
                string text = operand is Instruction branch ? "branch:" + Index(branch) : operand is Instruction[] branches ? "switch:" + string.Join(",", branches.Select(Index)) : Value(operand);
                result.Add("IL|" + i + "|" + instruction.OpCode.Code + "|" + text);
            }
            result.AddRange(body.ExceptionHandlers.Select(e => "EH|" + e.HandlerType + "|" + e.CatchType?.FullName + "|" + Index(e.TryStart) + "|" + Index(e.TryEnd) + "|" + Index(e.HandlerStart) + "|" + Index(e.HandlerEnd) + "|" + Index(e.FilterStart)));
        }
    }
    return result.ToArray();
}

static string VersionMetadata(string path) {
    var v = FileVersionInfo.GetVersionInfo(path);
    return JsonSerializer.Serialize(new { v.ProductVersion, v.FileVersion, v.ProductName, v.InternalName, v.OriginalFilename, v.CompanyName, v.FileDescription, v.LegalCopyright });
}

static string[] NativeResources(string path) {
    using var file = File.OpenRead(path);
    using var pe = new PEReader(file);
    var directory = pe.PEHeaders.PEHeader!.ResourceTableDirectory;
    if (directory.Size == 0) return Array.Empty<string>();
    var tree = pe.GetSectionData(directory.RelativeVirtualAddress).GetContent().ToArray();
    var result = new List<string>();
    uint U32(int at) => BitConverter.ToUInt32(tree, at);
    ushort U16(int at) => BitConverter.ToUInt16(tree, at);
    string Name(uint value) => (value & 0x80000000) == 0 ? "#" + value : Encoding.Unicode.GetString(tree, (int)(value & 0x7fffffff) + 2, U16((int)(value & 0x7fffffff)) * 2);
    void Walk(int offset, string prefix) {
        int count = U16(offset + 12) + U16(offset + 14);
        for (int i = 0; i < count; i++) {
            int entry = offset + 16 + i * 8;
            string key = prefix + "/" + Name(U32(entry));
            uint target = U32(entry + 4);
            if ((target & 0x80000000) != 0) Walk((int)(target & 0x7fffffff), key);
            else {
                int dataAt = (int)target;
                int rva = (int)U32(dataAt), size = (int)U32(dataAt + 4);
                result.Add(key + "|" + U32(dataAt + 8) + "|" + Hash(pe.GetSectionData(rva).GetContent(0, size).ToArray()));
            }
        }
    }
    Walk(0, "");
    return result.Order(StringComparer.Ordinal).ToArray();
}

static int RestoreOriginalMaxStacks(string path, (string Name, int Stack)[] originals) {
    var fixes = new List<(int Rva, int Value)>();
    using (var image = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadSymbols = false, InMemory = true })) {
        var methods = AllTypes(image.MainModule).SelectMany(t => t.Methods).Where(m => m.HasBody).ToArray();
        Check(methods.Length == originals.Length, "Method body count changed");
        for (int i = 0; i < methods.Length; i++) {
            Check(methods[i].FullName == originals[i].Name, "Method body order changed");
            if (methods[i].Body.MaxStackSize != originals[i].Stack) fixes.Add((methods[i].RVA, originals[i].Stack));
        }
    }
    if (fixes.Count == 0) return 0;
    byte[] bytes = File.ReadAllBytes(path);
    using (var stream = new MemoryStream(bytes, writable: false))
    using (var pe = new PEReader(stream)) {
        foreach (var fix in fixes) {
            var section = pe.PEHeaders.SectionHeaders.Single(s => fix.Rva >= s.VirtualAddress && fix.Rva < s.VirtualAddress + Math.Max(s.VirtualSize, s.SizeOfRawData));
            int offset = section.PointerToRawData + fix.Rva - section.VirtualAddress;
            Check((bytes[offset] & 3) == 3 && fix.Value is >= 0 and <= ushort.MaxValue, "Cannot preserve original maxstack in a non-fat header");
            bytes[offset + 2] = (byte)fix.Value;
            bytes[offset + 3] = (byte)(fix.Value >> 8);
        }
    }
    File.WriteAllBytes(path, bytes);
    return fixes.Count;
}
