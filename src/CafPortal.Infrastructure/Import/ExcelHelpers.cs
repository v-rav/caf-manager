using ClosedXML.Excel;

namespace CafPortal.Infrastructure.Import;

/// <summary>Header-driven cell access so imports tolerate column reordering and minor naming drift.</summary>
internal static class ExcelHelpers
{
    /// <summary>Builds a case-insensitive map of header text -> 1-based column index for the given row.</summary>
    public static Dictionary<string, int> MapHeaders(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var text = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text) && !map.ContainsKey(text))
                map[text] = cell.Address.ColumnNumber;
        }
        return map;
    }

    /// <summary>Finds the first column whose header contains any of the candidate tokens.</summary>
    public static int? FindColumn(Dictionary<string, int> headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var hit = headers.FirstOrDefault(h =>
                h.Key.Contains(candidate, StringComparison.OrdinalIgnoreCase));
            if (hit.Value != 0)
                return hit.Value;
        }
        return null;
    }

    public static string? GetString(IXLRow row, int? column)
    {
        if (column is null)
            return null;
        var value = row.Cell(column.Value).GetString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static double GetDouble(IXLRow row, int? column, double fallback = 0)
    {
        if (column is null)
            return fallback;
        var cell = row.Cell(column.Value);
        if (cell.TryGetValue<double>(out var d))
            return d;
        return double.TryParse(cell.GetString().Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public static DateOnly? GetDate(IXLRow row, int? column)
    {
        if (column is null)
            return null;
        var cell = row.Cell(column.Value);
        if (cell.TryGetValue<DateTime>(out var dt))
            return DateOnly.FromDateTime(dt);
        var text = cell.GetString().Trim();
        return DateTime.TryParse(text, out var parsed) ? DateOnly.FromDateTime(parsed) : null;
    }

    public static bool IsEmptyRow(IXLRow row) => !row.CellsUsed().Any();
}
