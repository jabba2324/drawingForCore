// Fails when a .trx has a failed test that isn't in the suite's known-failures list.
// Usage: dotnet run tools/ci/check_failures.cs -- results.trx [known-failures.txt]
// Known failures are one "Class.Method(arguments)" per line, as this tool prints them; '#' starts a comment. Tests on
// the list that now pass are reported, so the list can shrink.
using System.Xml.Linq;

XNamespace t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
var trx = args[0];
var known = new HashSet<string>();
if (args.Length > 1)
    foreach (var line in File.ReadLines(args[1]))
    {
        var entry = line.Split('#', 2)[0].Trim();
        if (entry.Length > 0) known.Add(entry);
    }

var root = XDocument.Load(trx).Root!;
var classes = root.Descendants(t + "UnitTest").ToDictionary(
    u => (string)u.Attribute("id")!, u => (string?)u.Element(t + "TestMethod")?.Attribute("className") ?? "");
var pairs = root.Descendants(t + "UnitTestResult").Select(r =>
{
    var name = (string)r.Attribute("testName")!;
    var cls = classes.GetValueOrDefault((string)r.Attribute("testId")!, "");
    return (Name: name.StartsWith(cls, StringComparison.Ordinal) ? name : cls + "." + name, Outcome: (string?)r.Attribute("outcome"));
}).ToList();

// Theory cases can share a display name: any failing case counts as a failure.
var failed = pairs.Where(p => p.Outcome == "Failed").Select(p => p.Name).ToHashSet();
var passed = pairs.Where(p => p.Outcome == "Passed").Select(p => p.Name).Where(n => !failed.Contains(n)).ToHashSet();
var newFailures = failed.Except(known).Order(StringComparer.Ordinal).ToList();
var fixedTests = known.Where(passed.Contains).Order(StringComparer.Ordinal).ToList();

Console.WriteLine($"{trx}: {pairs.Count} results, {failed.Count} failed ({failed.Count(known.Contains)} known, {newFailures.Count} new)");
foreach (var n in fixedTests) Console.WriteLine($"  now passes (remove from the known list): {n}");
foreach (var n in newFailures) Console.WriteLine($"::error::{n} failed");
return newFailures.Count > 0 ? 1 : 0;
