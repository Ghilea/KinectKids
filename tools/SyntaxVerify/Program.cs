using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Usage: SyntaxVerify <rootDir> [file1 file2 ...]
// Parses each .cs file with Roslyn and reports syntax (parse) errors only.
// This does NOT resolve UnityEngine types (no semantic binding), but it reliably
// catches the errors that file-splitting can introduce: unbalanced braces,
// malformed declarations, stray/missing tokens, broken partial wrappers, etc.

string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

IEnumerable<string> files;
if (args.Length > 1)
{
    files = args.Skip(1);
}
else
{
    files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
        .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                 && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
}

int totalFiles = 0;
int errorFiles = 0;
int totalErrors = 0;

foreach (var file in files.OrderBy(f => f))
{
    totalFiles++;
    string text;
    try { text = File.ReadAllText(file); }
    catch (Exception ex) { Console.WriteLine($"READ-ERR {file}: {ex.Message}"); errorFiles++; continue; }

    var tree = CSharpSyntaxTree.ParseText(text, path: file);
    var diags = tree.GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToList();

    if (diags.Count > 0)
    {
        errorFiles++;
        totalErrors += diags.Count;
        Console.WriteLine($"=== {file} ({diags.Count} error(s)) ===");
        foreach (var d in diags.Take(20))
        {
            var line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
            Console.WriteLine($"  L{line}: {d.Id} {d.GetMessage()}");
        }
    }
}

Console.WriteLine();
Console.WriteLine($"Files parsed: {totalFiles} | Files with errors: {errorFiles} | Total syntax errors: {totalErrors}");
return errorFiles == 0 ? 0 : 1;
