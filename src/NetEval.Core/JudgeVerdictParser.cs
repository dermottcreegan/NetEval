using System.Text.Json;

namespace NetEval;

/// <summary>Parses a judge model's JSON reply into a <see cref="JudgeVerdict"/>, tolerating code fences.</summary>
public static class JudgeVerdictParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static JudgeVerdict Parse(string modelReply)
    {
        if (!TryParse(modelReply, out var verdict))
            throw new FormatException($"Judge reply was not valid verdict JSON: {modelReply}");
        return verdict;
    }

    public static bool TryParse(string modelReply, out JudgeVerdict verdict)
    {
        verdict = null!;

        // Models occasionally wrap JSON in markdown fences despite instructions — extract the object.
        var start = modelReply.IndexOf('{');
        var end = modelReply.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try
        {
            var dto = JsonSerializer.Deserialize<VerdictDto>(modelReply[start..(end + 1)], Options);
            if (dto is null)
                return false;

            verdict = new JudgeVerdict(dto.Passed, Math.Clamp(dto.Score, 0.0, 1.0), dto.Reasoning ?? "");
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record VerdictDto(bool Passed, double Score, string? Reasoning);
}
