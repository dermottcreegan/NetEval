using System.Text.Json;

namespace NetEval;

/// <summary>Loads eval cases from a JSONL file (one JSON object per line).</summary>
public static class JsonlDataset
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<EvalCase> Load(string path)
    {
        var cases = new List<EvalCase>();
        foreach (var (line, number) in File.ReadLines(path).Select((l, i) => (l, i + 1)))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var evalCase = JsonSerializer.Deserialize<EvalCase>(line, Options)
                ?? throw new FormatException($"{path}:{number}: line is not a valid eval case.");
            cases.Add(evalCase);
        }
        return cases;
    }
}
