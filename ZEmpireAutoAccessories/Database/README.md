# Sharing the data with the team

Git carries the **code**. It does not carry the SQL Server database — that
lives on each machine. So pulling `Programmer` gives a teammate the app, and
then an empty or stale `ZEmpire` database behind it.

Three things have to travel together for everyone to see the same system:

| What | How it travels | Where |
| --- | --- | --- |
| The database | A SQL Server backup, committed | `Database/ZEmpireUpdated.bak` |
| Cut sizes | A JSON file, committed | `App_Data/cut-sizes.json` |
| Settings | Committed already | `appsettings.json` |

They must be refreshed **together**. Cut sizes are keyed by
`VehicleClassificationID` and `PanelID` — the database's own ids — so a
cut-size file from one database points at the wrong panels in another.

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
WITH FORMAT, INIT, COMPRESSION, NAME = N'ZEmpire full backup';
```

`FORMAT, INIT` are the query equivalent of that Media Options checkbox.

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
> WITH FORMAT, INIT, COMPRESSION, NAME = N'ZEmpire full backup';
> ```
>
> A bare filename with no folder lands in exactly that folder. The committed
> file is the same either way.

**2. Commit the backup and the cut sizes.**

```
git add ZEmpireAutoAccessories/Database/ZEmpireUpdated.bak
git add ZEmpireAutoAccessories/App_Data/cut-sizes.json
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

The seed scripts beside this file (`SeedPricelist2026.sql` and the rest) are
the alternative: text, diffable, and they rebuild the catalogue from nothing.
They do not carry transactions — sales, invoices, job orders — which is what
the backup is for.
