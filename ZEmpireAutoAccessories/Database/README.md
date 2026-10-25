# Sharing the data with the team

Git carries the **code**. It does not carry the SQL Server database — that
lives on each machine. So pulling `Programmer` gives a teammate the app, and
then an empty or stale `ZEmpire` database behind it.

Three things have to travel together for everyone to see the same system:

| What | How it travels | Where |
| --- | --- | --- |
| The database | A SQL Server backup, committed | `Database/ZEmpireUpdated.bak` |
| Cut sizes | A JSON file, committed | `App_Data/cut-sizes.json` |
| How products are measured | A JSON file, committed | `App_Data/product-measures.json` |
| Settings | Committed already | `appsettings.json` |

They must be refreshed **together**. Both JSON files are keyed by the
database's own ids — cut sizes by `VehicleClassificationID` and `PanelID`,
measures by `CategoryID` and `ProductID` — so a file from one database points
at the wrong panels and the wrong products in another.

---

## Publishing your data (the person with the real system)

**1. Back up the database over the committed file.**

*In SSMS, by hand:*

1. Open SSMS and connect to the server that holds `ZEmpire`.
2. In Object Explorer expand **Databases**, right-click **ZEmpire**, then
   **Tasks -> Back Up...**.
3. **Backup type:** `Full`. **Backup to:** `Disk`.
4. Under **Destination**, select the default path that is already listed and
   click **Remove**, then click **Add...** and type the full path, filename
   included:
   `C:\path\to\ZEmpireAutoAccessories\ZEmpireAutoAccessories\Database\ZEmpireUpdated.bak`
   Keep the `.bak` on the end - the Add dialog will otherwise offer you a
   folder and write a file with no extension.
5. Go to the **Media Options** page on the left and tick **Overwrite all
   existing backup sets**. This is the step people miss: without it SSMS
   *appends*, so the committed file grows by another ~16 MB every time you
   share, and git keeps all of it forever.
6. **OK**. "The backup of database 'ZEmpire' completed successfully" means the
   file is written.

*Or, the same thing as a query* - open a New Query window and run:

```sql
BACKUP DATABASE ZEmpire
TO DISK = N'C:\path\to\ZEmpireAutoAccessories\ZEmpireAutoAccessories\Database\ZEmpireUpdated.bak'
WITH FORMAT, INIT, NAME = N'ZEmpire full backup';
```

`FORMAT, INIT` are the query equivalent of that Media Options checkbox.

There is deliberately no `COMPRESSION`. It is an Enterprise and Standard
feature, and on SQL Server **Express** - which this shop runs - it does not
quietly do nothing, it fails the whole backup:

> `Msg 1844: BACKUP DATABASE WITH COMPRESSION is not supported on Express
> Edition (64-bit).`

The same applies to the **Compress backup** option on the Media Options page
of the GUI. Leave it on the default. The backup is simply larger; nothing else
about it differs.

`C:\path\to\...` above is a **placeholder** - replace it with your own clone's
path before running anything. To find it: in File Explorer open your
`ZEmpireAutoAccessories` repo, go to the inner `ZEmpireAutoAccessories` folder,
then `Database`, click the address bar and copy it. The path is also read on
the machine SQL Server runs on, not the machine SSMS runs on - the same thing
only when the server is your own PC.

Two errors come up, and they mean different things:

> **`Cannot open backup device ... Operating system error 3 (The system cannot
> find the path specified.)`** - the folder in your path does not exist. Nearly
> always the placeholder was left in. SQL Server creates the `.bak` file but
> never creates folders, so every folder in the path has to be there already.

> **`... Operating system error 5 (Access is denied.)`** - the file is written
> by the **SQL Server service account**, not by you, and that account usually
> cannot reach a folder under your user profile (`Documents`, `Desktop`,
> `OneDrive`). Don't fight the permissions. Back up to SQL Server's own backup
> folder, which the service account can always write, and copy the file into
> `Database\` with File Explorer afterwards:
>
> ```sql
> SELECT SERVERPROPERTY('InstanceDefaultBackupPath');
>
> BACKUP DATABASE ZEmpire
> TO DISK = N'ZEmpireUpdated.bak'
> WITH FORMAT, INIT, NAME = N'ZEmpire full backup';
> ```
>
> A bare filename with no folder lands in exactly that folder. The committed
> file is the same either way.

**2. Commit the backup and the cut sizes.**

```
git add ZEmpireAutoAccessories/Database/ZEmpireUpdated.bak
git add ZEmpireAutoAccessories/App_Data/cut-sizes.json
git add ZEmpireAutoAccessories/App_Data/product-measures.json
git commit -m "Refresh the shared database snapshot"
git push -u origin Programmer
```

**3. Tell the team to restore.** A pull alone does not change their database.

---

## Loading it (everyone else)

**1. Pull.**

```
git checkout Programmer
git pull origin Programmer
```

**2. Restore the backup.** The logical names inside it are `ZEmpire` and
`ZEmpire_log`; the paths are whatever your SQL Server uses, which is why
`WITH MOVE` is needed:

```sql
RESTORE FILELISTONLY
FROM DISK = N'C:\path\to\...\Database\ZEmpireUpdated.bak';
```

```sql
RESTORE DATABASE ZEmpire
FROM DISK = N'C:\path\to\...\Database\ZEmpireUpdated.bak'
WITH MOVE N'ZEmpire'     TO N'C:\Program Files\Microsoft SQL Server\<your instance>\MSSQL\DATA\ZEmpire.mdf',
     MOVE N'ZEmpire_log' TO N'C:\Program Files\Microsoft SQL Server\<your instance>\MSSQL\DATA\ZEmpire_log.ldf',
     REPLACE, RECOVERY;
```

Close any open connection to `ZEmpire` first or the restore is blocked.

**3. Check the connection string.** `appsettings.json` uses `Server=.;`, the
default instance. On SQL Express it is `Server=.\SQLEXPRESS;`. Change it
locally and **do not commit that change**, or it will keep flipping for
everybody:

```
git update-index --skip-worktree ZEmpireAutoAccessories/appsettings.json
```

**4. Run the app.** Startup adds any missing payment mode and the Identity
roles; it changes nothing that is already there.

---

## What does not travel, and why

**Proof of payment and warranty claim files** (`App_Data/payment-proofs/`)
are customers' bank receipts, with names, reference numbers and account
details on them. They stay out of the repository. A teammate's rows will
show as missing their proof, which is correct — they do not have the file.

**Stock postings** (`App_Data/stock-postings.json`) is what each completed
document took off the shelf, and it changes every time one is completed.
Tracking it would mean a merge conflict on nearly every commit. Without it,
reverting a document completed on someone else's machine falls back to
recomputing from the cut sizes, which is what the app did before the file
existed.

---

## A warning about repository size

The backup is ~16 MB and git keeps **every** version forever. Ten refreshes
is 160 MB that never goes away, even after deleting the file. If this is
going to be a regular thing, either:

- keep the backup out of git and share it on a drive or chat, or
- put it behind [Git LFS](https://git-lfs.com) before the next refresh.

## Figures nobody at the shop supplied

Two seed scripts carry values that were derived rather than given. Both are
named here, in their own script headers, and in the development report, so no
reader of any of the three can mistake them for the shop's own.

`SeedTintShadesAndSunroof.sql` applies **Light / Medium / Superdark** to the
eight tint variants that carried no shades: Stone, Ceramic Pro, Ceramic
Supreme, Play, Freeze, Max, Pro (99%) and Lite (97%). That ladder is what every
variant whose shades the shop DID supply already uses. Unlike the markups
below, it carries no money: `cat.Pricing` has no `ShadeID`, so a shade records
what was fitted and cannot move a price. A wrong name misdescribes a job sheet;
it cannot mis-charge a customer. Correct any in the Pricing screen.

The same script adds the **Sunroof** panel but deliberately does not price it,
because nobody supplied a sunroof price. Set one under Pricing for each vehicle
class that has a sunroof — the panel will not appear on the quotation, job
order and invoice forms until it has one, because those forms build their panel
list from `cat.Pricing`. The cut size is already known: `SuggestedCutSizes`
carries sunroof glass for Subcompact/Crossover, SUV, MPV and Big SUV, so film
comes off the roll correctly the moment the panel is priced.

`SeedBfFilmPricing.sql` is the seed script carrying figures nobody at the
shop supplied: BF Stone copies the Black Series prices exactly, and BF Pro and
BF Supreme are those plus 35% and 75%. They are a starting point so the product
can be sold at all, not the shop's prices. Correct them in the Pricing screen,
which journals every change to `cat.PriceHistory`.

The seed scripts beside this file (`SeedPricelist2026.sql` and the rest) are
the alternative: text, diffable, and they rebuild the catalogue from nothing.
They do not carry transactions — sales, invoices, job orders — which is what
the backup is for.
