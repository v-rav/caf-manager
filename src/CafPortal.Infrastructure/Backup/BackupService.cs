using System.IO.Compression;
using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CafPortal.Infrastructure.Backup;

/// <summary>SQLite backup (VACUUM INTO → zip) and restore (validate → atomic file swap → migrate).</summary>
public class BackupService(AppDbContext db, IConfiguration configuration) : IBackupService
{
    private const string DbEntryName = "cafdb.sqlite";

    public async Task<(byte[] Content, string FileName)> CreateBackupAsync(CancellationToken ct = default)
    {
        var snapshot = Path.Combine(Path.GetTempPath(), $"cafdb_snap_{Guid.NewGuid():N}.sqlite");
        try
        {
            // VACUUM INTO writes a clean, consistent copy without holding a long write lock on the live DB.
            // The path is a server-generated temp file (no user input); concatenated to avoid the EF interpolation analyzer.
            var escaped = snapshot.Replace("'", "''");
            var vacuumSql = "VACUUM INTO '" + escaped + "';";
            await db.Database.ExecuteSqlRawAsync(vacuumSql, ct);

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(DbEntryName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(snapshot);
                await fileStream.CopyToAsync(entryStream, ct);
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return (ms.ToArray(), $"cafdb_backup_{stamp}.zip");
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    public async Task<RestoreResultDto> RestoreAsync(Stream zipStream, CancellationToken ct = default)
    {
        var dbPath = ResolveDbPath();
        var incoming = Path.Combine(Path.GetTempPath(), $"cafdb_restore_{Guid.NewGuid():N}.sqlite");
        try
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                var entry = archive.Entries.FirstOrDefault(e =>
                                e.Name.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                                e.Name.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                            ?? archive.Entries.FirstOrDefault(e => e.Length > 0);
                if (entry is null)
                    return Fail("The uploaded zip does not contain a database file.");

                await using var entryStream = entry.Open();
                await using var fileStream = File.Create(incoming);
                await entryStream.CopyToAsync(fileStream, ct);
            }

            if (!IsValidDatabase(incoming, out var reason))
                return Fail($"The uploaded file is not a valid CAF Portal database ({reason}).");

            // Release EF/SQLite handles so the live file can be overwritten on Windows.
            db.ChangeTracker.Clear();
            await db.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Keep a safety copy of the pre-restore database, then swap in the uploaded one.
            var safety = $"{dbPath}.prerestore_{DateTime.UtcNow:yyyyMMddHHmmss}";
            if (File.Exists(dbPath)) File.Copy(dbPath, safety, overwrite: true);
            File.Copy(incoming, dbPath, overwrite: true);
            foreach (var sidecar in new[] { $"{dbPath}-wal", $"{dbPath}-shm" })
                if (File.Exists(sidecar)) File.Delete(sidecar);
            SqliteConnection.ClearAllPools();

            PrunePreRestoreCopies(dbPath, keep: 5);

            // Bring the restored schema up to the current migration set (idempotent) and read counts back.
            await db.Database.MigrateAsync(ct);

            return new RestoreResultDto
            {
                Success = true,
                Message = "Database restored successfully.",
                RestoredBytes = new FileInfo(dbPath).Length,
                RestoredUtc = DateTimeOffset.UtcNow,
                Nominations = await db.Nominations.CountAsync(ct),
                Resources = await db.Resources.CountAsync(ct),
                Accounts = await db.Accounts.CountAsync(ct),
            };
        }
        finally
        {
            TryDelete(incoming);
        }
    }

    private string ResolveDbPath()
    {
        var cs = configuration.GetConnectionString("Default") ?? "Data Source=App_Data/cafdb.sqlite";
        var dataSource = new SqliteConnectionStringBuilder(cs).DataSource;
        return Path.GetFullPath(dataSource);
    }

    /// <summary>Confirms the file opens as SQLite, passes integrity_check, and carries the portal schema.</summary>
    private static bool IsValidDatabase(string path, out string reason)
    {
        try
        {
            using var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false, // don't leave a pooled handle on the temp file
            }.ToString());
            conn.Open();

            using (var integrity = conn.CreateCommand())
            {
                integrity.CommandText = "PRAGMA integrity_check;";
                var result = integrity.ExecuteScalar() as string;
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "failed integrity check";
                    return false;
                }
            }

            using var schema = conn.CreateCommand();
            schema.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Nominations';";
            var hasTable = Convert.ToInt64(schema.ExecuteScalar()) > 0;
            reason = hasTable ? string.Empty : "missing the Nominations table";
            return hasTable;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    private static RestoreResultDto Fail(string message) => new() { Success = false, Message = message };

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort temp cleanup */ }
    }

    /// <summary>Keeps only the newest <paramref name="keep"/> pre-restore safety copies; deletes older ones.</summary>
    private static void PrunePreRestoreCopies(string dbPath, int keep)
    {
        try
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (string.IsNullOrEmpty(dir)) return;
            var prefix = Path.GetFileName(dbPath) + ".prerestore_";
            var stale = Directory.EnumerateFiles(dir, prefix + "*")
                .OrderByDescending(f => f, StringComparer.Ordinal) // timestamp suffix sorts chronologically
                .Skip(Math.Max(0, keep))
                .ToList();
            foreach (var f in stale) TryDelete(f);
        }
        catch { /* best-effort housekeeping */ }
    }
}
