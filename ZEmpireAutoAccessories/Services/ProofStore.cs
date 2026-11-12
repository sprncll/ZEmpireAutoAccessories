using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Files kept as evidence: the screenshot or receipt showing an online
    /// payment arrived, and the slip or photo showing a warranty was claimed.
    /// Each one is filed against the record it belongs to.
    ///
    /// A record can hold several. A customer who puts money down and settles
    /// the rest later sends a receipt each time, and the second one must not
    /// push the first out: both are evidence, of different payments. They are
    /// numbered in the order they were attached, starting at 1.
    ///
    /// The files live in a folder, not in the database. There is nowhere in
    /// the schema to put them - sales.Sales holds a total and nothing else,
    /// sales.Warranty has no attachment column, and there is no attachment
    /// table - and adding one is off the table. A folder needs no migration,
    /// and the link is the file's name: what kind of record it belongs to,
    /// that record's id and the file's number, and nothing else, so nothing the customer or the
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

        public int MaxPerRecord { get; }

        public ProofStore(IConfiguration config, IWebHostEnvironment environment)
        {
            var configured = config["Payments:ProofFolder"];

            _folder = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "payment-proofs")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);

            _maxBytes = config.GetValue<long?>("Payments:MaxProofBytes") ?? 5 * 1024 * 1024;

            // Enough for a down payment, a few instalments and the balance,
            // with room for a retaken screenshot - and a ceiling, so one
            // record cannot be used to fill the disk.
            MaxPerRecord = Math.Max(1, config.GetValue<int?>("Payments:MaxProofsPerRecord") ?? 10);
        }

        public bool Exists(ProofKind kind, int recordId) =>
            Slots(kind, recordId).Count > 0;

        public IReadOnlyList<int> Slots(ProofKind kind, int recordId) =>
            Existing(kind, recordId).Select(f => f.Slot).ToList();

        public Dictionary<(ProofKind Kind, int RecordId), List<int>> SlotsFor(
            IEnumerable<(ProofKind Kind, int RecordId)> records)
        {
            var found = new Dictionary<(ProofKind, int), List<int>>();

            if (!Directory.Exists(_folder))
                return found;

            var wanted = new HashSet<(ProofKind, int)>(records);

            // One listing, then a lookup per file - a folder probe per
            // transaction would be a few hundred stat calls on a busy day.
            foreach (var path in Directory.EnumerateFiles(_folder))
            {
                if (Parse(Path.GetFileName(path)) is not { } file)
                    continue;

                var key = (file.Kind, file.RecordId);
                if (!wanted.Contains(key))
                    continue;

                if (!found.TryGetValue(key, out var slots))
                    found[key] = slots = new List<int>();

                // A JPG and a PNG left in the same slot by a half-failed
                // replace are one attachment, not two.
                if (!slots.Contains(file.Slot))
                    slots.Add(file.Slot);
            }

            foreach (var slots in found.Values)
                slots.Sort();

            return found;
        }

        public async Task<string?> Save(ProofKind kind, int recordId, IFormFile file)
        {
            var (error, extension) = await Check(file);
            if (error != null)
                return error;

            Directory.CreateDirectory(_folder);

            // Replacing means everything already there goes: whatever
            // extension, whatever number, so a PNG replacing a JPG cannot
            // leave two proofs behind with only one of them visible.
            foreach (var stale in Existing(kind, recordId))
                File.Delete(stale.Path);

            await Write(file, PathFor(kind, recordId, 1, extension));
            return null;
        }

        public async Task<string?> Add(ProofKind kind, int recordId, IFormFile file)
        {
            var (error, extension) = await Check(file);
            if (error != null)
                return error;

            var slots = Slots(kind, recordId);

            if (slots.Count >= MaxPerRecord)
                return $"This payment already has {MaxPerRecord} files attached, which is the limit. " +
                       "Remove one that is no longer needed to make room.";

            Directory.CreateDirectory(_folder);

            // One past the highest, never the first gap: the numbers say the
            // order the receipts arrived in, and a removed second receipt
            // should not have a later one filed in its place.
            var slot = slots.Count == 0 ? 1 : slots[^1] + 1;

            await Write(file, PathFor(kind, recordId, slot, extension));
            return null;
        }

        public (Stream Content, string ContentType, string FileName)? Open(
            ProofKind kind, int recordId, int slot = 1)
        {
            var path = Existing(kind, recordId)
                .Where(f => f.Slot == slot)
                .Select(f => f.Path)
                .FirstOrDefault();

            if (path == null)
                return null;

            var extension = Path.GetExtension(path);

            // The type comes from the allow-list keyed by the extension this
            // store wrote, never from anything an upload carried.
            if (!AllowedTypes.TryGetValue(extension, out var contentType))
                return null;

            return (File.OpenRead(path), contentType, Path.GetFileName(path));
        }

        public bool Delete(ProofKind kind, int recordId, int? slot = null)
        {
            var removed = false;

            // Every extension in the slot, not just the first: a replace
            // that half-failed could have left two behind, and "delete" has
            // to mean that attachment is gone. With no slot named, every
            // attachment the record has.
            foreach (var file in Existing(kind, recordId).Where(f => slot == null || f.Slot == slot))
            {
                File.Delete(file.Path);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// Whether an upload is something this store keeps. Returns the reason
        /// it is not, or null and the extension to file it under.
        /// </summary>
        private async Task<(string? Error, string Extension)> Check(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return ("Choose a file to attach.", "");

            if (file.Length > _maxBytes)
                return ($"That file is {file.Length / 1024m / 1024m:N1} MB. " +
                       $"The limit is {_maxBytes / 1024m / 1024m:N0} MB - a screenshot rather than a photo of the screen usually does it.", "");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedTypes.TryGetValue(extension, out var contentType))
                return ("Attach a screenshot or a PDF receipt (JPG, PNG, WEBP or PDF).", "");

            // The extension is a claim, not a fact. Read the first bytes and
            // check they are the file they say they are, so a renamed
            // something-else is refused at the door rather than being handed
            // back to a browser later with an image content type on it.
            await using (var probe = file.OpenReadStream())
            {
                // Read until the header is full or the file ends: one Read is
                // not obliged to return all twelve bytes, and a short one
                // would fail a perfectly good WEBP or PDF while telling the
                // user they had renamed something.
                var header = new byte[12];
                var read = 0;
                int got;
                while (read < header.Length &&
                       (got = await probe.ReadAsync(header.AsMemory(read))) > 0)
                    read += got;

                if (!LooksLike(contentType, header.AsSpan(0, read)))
                    return ("That file is not the kind it claims to be. Attach a screenshot or a PDF receipt.", "");
            }

            return (null, extension.ToLowerInvariant());
        }

        private static async Task Write(IFormFile file, string path)
        {
            await using var destination = File.Create(path);
            await using var upload = file.OpenReadStream();
            await upload.CopyToAsync(destination);
        }

        private static string PrefixFor(ProofKind kind) =>
            kind switch
            {
                ProofKind.Sale => "sale-",
                ProofKind.ServiceInvoice => "invoice-",
                ProofKind.WarrantyClaim => "warranty-",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

        /// <summary>
        /// The path for one attachment, built only from what kind of thing it
        /// is evidence of, that record's id and the attachment's number -
        /// never from anything typed or uploaded, so there is nothing in it
        /// to escape the folder with.
        ///
        /// The first attachment carries no number: "sale-5.png". That is the
        /// name every proof had when a record could hold only one, so the
        /// files already in the folder are attachment 1 without being
        /// renamed. The rest are "sale-5-2.png", "sale-5-3.png" - the dash
        /// keeps sale 5's second file apart from sale 52's first.
        /// </summary>
        private string PathFor(ProofKind kind, int recordId, int slot, string extension) =>
            Path.Combine(_folder,
                PrefixFor(kind) + recordId + (slot == 1 ? "" : "-" + slot) + extension);

        /// <summary>
        /// Reads a file name back into what it is attached to, or null for a
        /// file this store did not write.
        /// </summary>
        private static (ProofKind Kind, int RecordId, int Slot)? Parse(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) ||
                !AllowedTypes.ContainsKey(Path.GetExtension(fileName)))
                return null;

            var stem = Path.GetFileNameWithoutExtension(fileName);

            foreach (var kind in Enum.GetValues<ProofKind>())
            {
                var prefix = PrefixFor(kind);
                if (!stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = stem[prefix.Length..].Split('-');
                if (parts.Length is < 1 or > 2 || !IsNumber(parts[0], out var recordId))
                    return null;

                var slot = 1;
                if (parts.Length == 2 && (!IsNumber(parts[1], out slot) || slot < 2))
                    return null;

                return (kind, recordId, slot);
            }

            return null;
        }

        // Digits only. int.TryParse alone would accept " 5" and "+5", and a
        // file called that is not one this store wrote.
        private static bool IsNumber(string text, out int value)
        {
            value = 0;
            return text.Length > 0 && text.All(char.IsAsciiDigit) && int.TryParse(text, out value);
        }

        /// <summary>The attachments a record has, in the order they were added.</summary>
        private List<(int Slot, string Path)> Existing(ProofKind kind, int recordId)
        {
            var found = new List<(int Slot, string Path)>();

            if (!Directory.Exists(_folder))
                return found;

            // The pattern narrows the listing; Parse decides. "sale-5*" also
            // matches sale 52's files, which Parse tells apart by their id.
            foreach (var path in Directory.EnumerateFiles(_folder, PrefixFor(kind) + recordId + "*"))
            {
                if (Parse(Path.GetFileName(path)) is { } file &&
                    file.Kind == kind && file.RecordId == recordId)
                    found.Add((file.Slot, path));
            }

            found.Sort((x, y) => x.Slot.CompareTo(y.Slot));
            return found;
        }

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
