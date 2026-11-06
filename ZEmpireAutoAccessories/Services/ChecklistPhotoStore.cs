using System.Globalization;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// The photographs taken when a car arrives: the scratch on the quarter
    /// glass, the old film lifting at the corner, the chip that was already
    /// there. A noted scratch is one person's word; a photograph taken at
    /// intake settles it.
    ///
    /// The files live in a folder, not in the database, for the same reason
    /// payment proofs do: ops.VehicleChecklist has no attachment column, there
    /// is no attachment table, and adding one is off the table. A folder needs
    /// no migration. ProofStore already does this for evidence filed one per
    /// record; a checklist holds a set of up to five, which is a different
    /// shape, so it gets its own store rather than bending that one.
    ///
    /// A photo is addressed by its checklist and a slot number, both whole
    /// numbers, and the file name is built from nothing else. Whatever the
    /// phone called the file is read for its extension and then discarded, so
    /// there is nothing in a name to escape the folder with.
    ///
    /// The folder sits outside wwwroot deliberately. These are photographs of
    /// customers' vehicles, plate numbers included; served as static files
    /// they would be readable by anyone who guessed the URL. The screens
    /// stream them back through an action behind the module's own permission.
    /// </summary>
    public class ChecklistPhotoStore : IChecklistPhotoStore
    {
        // A photograph from a phone, and nothing else. No PDF and no WEBP:
        // these are taken on the spot by staff, and both cameras produce one
        // of these two.
        private static readonly Dictionary<string, string> AllowedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png"
            };

        private readonly string _folder;

        public int MaxPerChecklist { get; }
        public long MaxBytes { get; }

        public ChecklistPhotoStore(IConfiguration config, IWebHostEnvironment environment)
        {
            var configured = config["Checklist:PhotoFolder"];

            _folder = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "checklist-photos")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);

            MaxPerChecklist = config.GetValue<int?>("Checklist:MaxPhotos") ?? 5;
            MaxBytes = config.GetValue<long?>("Checklist:MaxPhotoBytes") ?? 15L * 1024 * 1024;
        }

        public IReadOnlyList<ChecklistPhoto> List(int checklistId)
        {
            var found = new List<ChecklistPhoto>();

            if (!Directory.Exists(_folder))
                return found;

            for (var slot = 1; slot <= MaxPerChecklist; slot++)
            {
                var path = PathFor(checklistId, slot);
                if (path == null)
                    continue;

                var info = new FileInfo(path);
                found.Add(new ChecklistPhoto(
                    slot,
                    AllowedTypes[Path.GetExtension(path)],
                    info.Length,
                    info.CreationTime));
            }

            return found;
        }

        public Dictionary<int, int> CountsFor(IEnumerable<int> checklistIds)
        {
            var counts = checklistIds.Distinct().ToDictionary(id => id, _ => 0);

            if (!Directory.Exists(_folder) || counts.Count == 0)
                return counts;

            // One listing and a parse per file, rather than five probes per
            // checklist - a list screen showing fifty rows would otherwise
            // make two hundred and fifty stat calls to print a count.
            foreach (var name in Directory.EnumerateFiles(_folder).Select(Path.GetFileNameWithoutExtension))
            {
                if (name is null)
                    continue;

                var dash = name.LastIndexOf('-');
                if (dash <= 0)
                    continue;

                if (int.TryParse(name.AsSpan(0, dash), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                    && counts.ContainsKey(id))
                    counts[id]++;
            }

            return counts;
        }

        public async Task<string?> Save(int checklistId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Choose a photo to attach.";

            if (file.Length > MaxBytes)
                return $"That photo is {file.Length / 1024m / 1024m:N1} MB. " +
                       $"The limit is {MaxBytes / 1024m / 1024m:N0} MB per photo.";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedTypes.TryGetValue(extension, out var contentType))
                return "Attach a photo in JPG or PNG format.";

            // The extension is a claim, not a fact. Read the opening bytes and
            // check the file is what it says it is, so a renamed
            // something-else is refused at the door rather than handed back to
            // a browser later with an image content type on it.
            await using (var probe = file.OpenReadStream())
            {
                // One Read is not obliged to return all eight bytes, so keep
                // going until the header is full or the file ends.
                var header = new byte[8];
                var read = 0;
                int got;
                while (read < header.Length &&
                       (got = await probe.ReadAsync(header.AsMemory(read))) > 0)
                    read += got;

                if (!LooksLike(contentType, header.AsSpan(0, read)))
                    return "That file is not the image it claims to be. Attach a JPG or PNG photo.";
            }

            var slot = FirstFreeSlot(checklistId);
            if (slot == null)
                return $"This checklist already has {MaxPerChecklist} photos. " +
                       "Remove one before adding another.";

            Directory.CreateDirectory(_folder);

            var destinationPath = Path.Combine(
                _folder, $"{checklistId}-{slot}{extension.ToLowerInvariant()}");

            // Written beside the target and moved into place, so a browser
            // that gives up halfway leaves a .part behind rather than a slot
            // holding half a photograph.
            var temporaryPath = destinationPath + ".part";

            try
            {
                await using (var destination = File.Create(temporaryPath))
                await using (var upload = file.OpenReadStream())
                    await upload.CopyToAsync(destination);

                File.Move(temporaryPath, destinationPath, overwrite: true);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                throw;
            }

            return null;
        }

        public (Stream Content, string ContentType, string FileName)? Open(int checklistId, int slot)
        {
            if (slot < 1 || slot > MaxPerChecklist)
                return null;

            var path = PathFor(checklistId, slot);
            if (path == null)
                return null;

            // The type comes from the allow-list keyed by the extension this
            // store wrote, never from anything an upload carried.
            return (File.OpenRead(path),
                    AllowedTypes[Path.GetExtension(path)],
                    $"checklist-{checklistId}-photo-{slot}{Path.GetExtension(path)}");
        }

        public bool Remove(int checklistId, int slot)
        {
            if (slot < 1 || slot > MaxPerChecklist)
                return false;

            var path = PathFor(checklistId, slot);
            if (path == null)
                return false;

            File.Delete(path);
            return true;
        }

        public void RemoveAll(int checklistId)
        {
            for (var slot = 1; slot <= MaxPerChecklist; slot++)
            {
                var path = PathFor(checklistId, slot);
                if (path != null)
                    File.Delete(path);
            }
        }

        /// <summary>
        /// The file holding this slot, whichever extension it was stored
        /// under, or null if the slot is empty.
        /// </summary>
        private string? PathFor(int checklistId, int slot)
        {
            if (!Directory.Exists(_folder))
                return null;

            foreach (var extension in AllowedTypes.Keys)
            {
                var path = Path.Combine(_folder, $"{checklistId}-{slot}{extension}");
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// The lowest slot with nothing in it, so removing the second of three
        /// photos and adding another fills the gap rather than running out of
        /// room with four photos on file.
        /// </summary>
        private int? FirstFreeSlot(int checklistId)
        {
            for (var slot = 1; slot <= MaxPerChecklist; slot++)
                if (PathFor(checklistId, slot) == null)
                    return slot;

            return null;
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

            _ => false
        };
    }
}
