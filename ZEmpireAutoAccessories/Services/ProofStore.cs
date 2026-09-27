using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Files kept as evidence: the screenshot or receipt showing an online
    /// payment arrived, and the slip or photo showing a warranty was claimed.
    /// Each one is filed against the record it belongs to.
    ///
    /// The files live in a folder, not in the database. There is nowhere in
    /// the schema to put them - sales.Sales holds a total and nothing else,
    /// sales.Warranty has no attachment column, and there is no attachment
    /// table - and adding one is off the table. A folder needs no migration,
    /// and the link is the file's name: what kind of record it belongs to and
    /// that record's id, and nothing else, so nothing the customer or the
    /// staff types can steer where it is written.
    ///
    /// The folder sits outside wwwroot deliberately. These carry customers'
    /// names, bank reference numbers and account details; served as static
    /// files they would be readable by anyone who guessed the URL. The
    /// screens stream them back through an action instead, each behind the
    /// permission that screen already requires.
    /// </summary>
    public class ProofStore : IProofStore
    {
        // What a phone screenshot or a downloaded receipt actually is. Anything
        // else is refused rather than stored and worried about later.
        private static readonly Dictionary<string, string> AllowedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp",
                [".pdf"] = "application/pdf"
            };

        private readonly string _folder;
        private readonly long _maxBytes;

        public ProofStore(IConfiguration config, IWebHostEnvironment environment)
        {
            var configured = config["Payments:ProofFolder"];

            _folder = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "payment-proofs")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);

            _maxBytes = config.GetValue<long?>("Payments:MaxProofBytes") ?? 5 * 1024 * 1024;
        }

        public bool Exists(ProofKind kind, int recordId) =>
            FindExisting(kind, recordId) != null;

        public HashSet<(ProofKind Kind, int RecordId)> ExistingFor(
            IEnumerable<(ProofKind Kind, int RecordId)> records)
        {
            var found = new HashSet<(ProofKind, int)>();

            if (!Directory.Exists(_folder))
                return found;

            // One listing, then a lookup per row - a folder probe per
            // transaction would be a few hundred stat calls on a busy day.
            var names = new HashSet<string>(
                Directory.EnumerateFiles(_folder).Select(Path.GetFileName)!,
                StringComparer.OrdinalIgnoreCase);

            foreach (var (kind, recordId) in records)
            {
                var stem = StemFor(kind, recordId);
                if (AllowedTypes.Keys.Any(ext => names.Contains(stem + ext)))
                    found.Add((kind, recordId));
            }

            return found;
        }

        public async Task<string?> Save(ProofKind kind, int recordId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Choose a file to attach.";

            if (file.Length > _maxBytes)
                return $"That file is {file.Length / 1024m / 1024m:N1} MB. " +
                       $"The limit is {_maxBytes / 1024m / 1024m:N0} MB - a screenshot rather than a photo of the screen usually does it.";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedTypes.TryGetValue(extension, out var contentType))
                return "Attach a screenshot or a PDF receipt (JPG, PNG, WEBP or PDF).";

            // The extension is a claim, not a fact. Read the first bytes and
            // check they are the file they say they are, so a renamed
            // something-else is refused at the door rather than being handed
            // back to a browser later with an image content type on it.
            await using (var probe = file.OpenReadStream())
            {
                var header = new byte[12];
                var read = await probe.ReadAsync(header);
                if (!LooksLike(contentType, header.AsSpan(0, read)))
                    return "That file is not the kind it claims to be. Attach a screenshot or a PDF receipt.";
            }

            Directory.CreateDirectory(_folder);

            // Replacing a proof means the old one goes: the other extensions
            // for this transaction are cleared first, so a PNG replacing a JPG
            // cannot leave two proofs behind with only one of them visible.
            foreach (var stale in ExistingPaths(kind, recordId))
                File.Delete(stale);

            var path = Path.Combine(_folder, StemFor(kind, recordId) + extension.ToLowerInvariant());

            await using var destination = File.Create(path);
            await using var upload = file.OpenReadStream();
            await upload.CopyToAsync(destination);

            return null;
        }

        public (Stream Content, string ContentType, string FileName)? Open(
            ProofKind kind, int recordId)
        {
            var path = FindExisting(kind, recordId);
            if (path == null)
                return null;

            var extension = Path.GetExtension(path);

            // The type comes from the allow-list keyed by the extension this
            // store wrote, never from anything an upload carried.
            if (!AllowedTypes.TryGetValue(extension, out var contentType))
                return null;

            return (File.OpenRead(path), contentType, Path.GetFileName(path));
        }

        public bool Delete(ProofKind kind, int recordId)
        {
            var removed = false;

            // Every extension for this record, not just the first: a replace
            // that half-failed could have left two behind, and "delete" has
            // to mean the record is left with nothing attached.
            foreach (var path in ExistingPaths(kind, recordId).ToList())
            {
                File.Delete(path);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// The file name for a record, built only from what kind of thing it
        /// is evidence of and that record's id - never from anything typed or
        /// uploaded, so there is nothing in it to escape the folder with.
        /// </summary>
        private static string StemFor(ProofKind kind, int recordId) =>
            kind switch
            {
                ProofKind.Sale => "sale-",
                ProofKind.ServiceInvoice => "invoice-",
                ProofKind.WarrantyClaim => "warranty-",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            } + recordId.ToString();

        private IEnumerable<string> ExistingPaths(ProofKind kind, int recordId)
        {
            if (!Directory.Exists(_folder))
                yield break;

            var stem = StemFor(kind, recordId);

            foreach (var extension in AllowedTypes.Keys)
            {
                var path = Path.Combine(_folder, stem + extension);
                if (File.Exists(path))
                    yield return path;
            }
        }

        private string? FindExisting(ProofKind kind, int recordId) =>
            ExistingPaths(kind, recordId).FirstOrDefault();

        /// <summary>
        /// Whether the opening bytes match the format the extension promised.
        /// </summary>
        private static bool LooksLike(string contentType, ReadOnlySpan<byte> header) => contentType switch
        {
            "image/jpeg" => header.Length >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,

            "image/png" => header.Length >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,

            // RIFF....WEBP
            "image/webp" => header.Length >= 12
                && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',

            "application/pdf" => header.Length >= 5
                && header[0] == (byte)'%' && header[1] == (byte)'P' && header[2] == (byte)'D'
                && header[3] == (byte)'F' && header[4] == (byte)'-',

            _ => false
        };
    }
}
