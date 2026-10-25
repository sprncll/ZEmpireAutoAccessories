# Publishing to SmarterASP.NET

The free trial fits this system: ASP.NET Core, SQL Server, unlimited space, a
persistent filesystem, and a US datacentre — roughly 170–220 ms from the
Philippines, against 250–350 ms from an EU host. The published application is
about **42 MB**.

**The trial runs 60 days and then stops.** If the defence is further out than
that, plan to pay or to have a local copy ready to run.

---

## Before the first deploy

Six changes were made for shared hosting and are already in the branch. Each
one fixes something that is invisible locally and only shows up once it is
live.

| What | Why it matters |
| --- | --- |
| `OutOfProcess` hosting model (`.csproj`) | Sites share one IIS application pool here, so the in-process default — which loads the app into that shared worker — is not permitted. SmarterASP.NET says so in its own publishing guide. Without this the site does not start. |
| Workstation GC (`.csproj`) | Server GC is the ASP.NET Core default and reserves a heap per core, assuming a machine with gigabytes to itself. On shared hosting that is wasted at best. |
| Data-protection keys persisted to `App_Data/keys` (`Program.cs`) | Without it the keys live in memory and are regenerated on every app-pool recycle — about twenty minutes idle, and every redeploy. **Everyone signed in is logged out, mid-job.** |
| `App_Data` excluded from publish (`.csproj`) | Otherwise a redeploy copies the repository's `cut-sizes.json` over the shop's own and silently reverts their measurements. |
| No built-in password in Production (`IdentitySeeder.cs`) | `ChangeMe123!` is printed in public source. In Production an unset `Seed:DefaultPassword` now skips seeding and says so. |
| `appsettings.Production.json` and `App_Data/keys/` git-ignored | One is a database credential, the other signs the auth cookie. |

---

## 1. Create the database

Control panel → **Database Manager** → create an **empty MSSQL database**.
Note its **server, name, user and password**.

## 2. Restore the backup into it

SmarterASP.NET restores a `.bak` directly, which is the easy path — no
scripting, no editing, nothing to strip out.

1. Take the backup (see `ZEmpireAutoAccessories/Database/README.md`). On SQL
   Server Express there is **no `COMPRESSION`** — it fails the whole backup.
2. Upload the `.bak` over FTP into the account's **`db`** folder.
3. In Database Manager, click **Restore Database** beside the empty database
   and pick the uploaded file.

Then run against the restored database, in this order:

- `SeedTintShadesAndSunroof.sql`
- `SeedSunroofPricing.sql` — after editing its four multipliers
- `SeedBfFilmPricing.sql`, if BF Film prices are wanted

A newer SQL Server restores a backup taken on an older one, so a local Express
backup is fine. The reverse is not true: never restore a newer backup onto an
older instance.

## 3. Publish the application

Visual Studio → right-click the project → **Publish** → import the
**Web Deploy** profile from the control panel (the endpoint is port 8172).

- **Configuration:** Release
- **Target framework:** net8.0
- **Deployment mode:** Framework-dependent
- **Target runtime:** win-x64

If the host does not carry .NET 8, switch **Deployment mode** to
**Self-contained**. The runtime then ships inside the upload — about 90 MB
instead of 42.

> ### Leave "Remove additional files at destination" UNCHECKED
>
> Under **Show all settings**. If it is ticked, every publish deletes
> everything on the server that is not in the upload:
>
> - `App_Data/cut-sizes.json` — the sunroof cut sizes go
> - `App_Data/payment-proofs/` — customers' receipts go
> - `App_Data/keys/` — **everyone is logged out**, and stays logged out
>
> This is the one checkbox that undoes the `App_Data` protection above.

## 4. Settings that live only on the server

Create `appsettings.Production.json` in the project folder, next to
`appsettings.json`. It is git-ignored, so it never reaches GitHub, and
`appsettings.*.json` is published automatically.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<from the panel>;Database=<db>;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Seed": {
    "DefaultPassword": "<a password you choose, not the one in the source>"
  }
}
```

`Trusted_Connection=True` is Windows authentication and will not work here.

## 5. Upload `App_Data` once

Over FTP, copy `ZEmpireAutoAccessories/App_Data/` into the deployed folder. It
is deliberately not part of the publish, so this is a one-time step. The
application creates `App_Data/keys/` itself on first start.

---

## 6. Check these four things, in this order

1. **The site loads and you can sign in.**
2. **Generate a PDF receipt.** PDF rendering uses `QuestPdfSkia.dll`, a native
   x64 binary rather than .NET code, and native code is the thing most likely
   to be restricted on shared hosting. If this works, the riskiest part is
   settled. If it fails, the site still runs — only the PDFs are affected.
3. **Leave the site alone for half an hour, then reload it.** You should still
   be signed in. If not, check that `App_Data/keys` exists and is writable.
4. **Complete a job order with a film line**, to confirm stock comes off the
   roll and the cut sizes came across.

---

## Updating after it is live

Redeploying is **Publish** again, as often as you like. Two rules:

- **Never upload `App_Data` again**, and keep that checkbox unticked. It holds
  what the shop has entered since.
- **A code change is a publish; a catalogue change is a script.** New products,
  prices or panels are SQL run against the hosted database. The shades and the
  Sunroof panel seed themselves at startup, so those arrive with the publish.

The application is database-first and has no migrations, so a schema change
happens on the server first.

## If the trial ends

The site and database are deleted when the 60 days are up unless the account
is paid. Before then: take a `.bak` from the hosted database and download
`App_Data` over FTP. Those two, plus the branch, rebuild the whole system
anywhere.
