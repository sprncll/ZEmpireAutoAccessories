# Publishing to MonsterASP.NET

The free plan fits this system: ASP.NET Core on .NET 8, one SQL Server
database with 1 GB, 5 GB of web storage, and a persistent filesystem. The
published application is about **42 MB** and the database is about **16 MB**,
so neither limit is close.

The plan gives the site **256 MB of RAM**, which is the one real constraint —
see *Memory* below.

---

## Before the first deploy

Five changes were made for this, and they are already in the branch. They are
listed because each one fixes something that is invisible until it bites.

| What | Why it matters on shared hosting |
| --- | --- |
| Workstation GC (`.csproj`) | Server GC is the ASP.NET Core default and reserves a heap per core, assuming gigabytes. With 256 MB, generating a PDF receipt would meet the ceiling. |
| Data-protection keys persisted to `App_Data/keys` (`Program.cs`) | Without it the keys live in memory and are regenerated on every app-pool recycle — roughly twenty minutes idle, and every redeploy. **Everyone signed in is logged out, mid-job.** |
| `App_Data` excluded from publish (`.csproj`) | Otherwise a redeploy copies the repository's `cut-sizes.json` over the shop's own and silently reverts their measurements. |
| No built-in password in Production (`IdentitySeeder.cs`) | `ChangeMe123!` is printed in public source. In Production an unset `Seed:DefaultPassword` now skips seeding and says so, instead of making every passwordless account reachable. |
| `appsettings.Production.json` and `App_Data/keys/` git-ignored | The deployed connection string is a credential and the keys sign the auth cookie. Neither belongs in the repository. |

---

## 1. Create the site and the database

In the MonsterASP control panel, create the website and a **MSSQL** database.
Note the database's **server, name, user and password** — the panel shows a
ready-made connection string.

## 2. Put the database on the server

The local database travels as a backup, as described in
`ZEmpireAutoAccessories/Database/README.md`. MonsterASP's panel accepts a
`.bak` restore; where it does not, generate a script in SSMS
(*Tasks → Generate Scripts*, with **schema and data**) and run it against the
hosted database from SSMS using the credentials from step 1.

Then run, against the hosted database:

- `SeedTintShadesAndSunroof.sql`
- `SeedSunroofPricing.sql` — after editing its four multipliers
- `SeedBfFilmPricing.sql`, if BF Film prices are wanted

## 3. Publish the application

In Visual Studio: right-click the project → **Publish** → **Web Deploy** or
**FTP**, using the credentials from the control panel. Settings:

- **Configuration:** Release
- **Target framework:** net8.0
- **Deployment mode:** Framework-dependent
- **Target runtime:** win-x64

If the host turns out not to carry .NET 8, switch **Deployment mode** to
**Self-contained**. The runtime then ships inside the upload — about 90 MB
instead of 42, still well inside 5 GB.

## 4. Settings that live only on the server

Create `appsettings.Production.json` **next to the deployed `.dll`**, not in
the repository (it is git-ignored, so Visual Studio will not upload it):

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

`Trusted_Connection=True` is Windows authentication and will not work here;
the hosted database needs SQL authentication.

## 5. Upload `App_Data` once

Over FTP, copy `ZEmpireAutoAccessories/App_Data/` to the deployed folder. It
is deliberately **not** part of the publish, so this is a one-time step. After
this, the shop's own cut sizes live on the server and a redeploy leaves them
alone.

The application creates `App_Data/keys/` itself on first start.

---

## 6. Check these four things, in this order

1. **The site loads and you can sign in.**
2. **Generate a PDF receipt.** PDF rendering uses `QuestPdfSkia.dll`, a native
   x64 binary rather than .NET code, and native code is the thing most likely
   to be restricted on shared hosting. If this works, the riskiest part is
   settled. If it fails, the site still runs — only the PDFs are affected.
3. **Leave the site alone for half an hour, then reload it.** You should still
   be signed in. If you are not, the data-protection keys are not persisting;
   check that `App_Data/keys` exists and is writable.
4. **Complete a job order with a film line**, to confirm stock comes off the
   roll and the cut sizes came across.

---

## Updating after it is live

Redeploying is just **Publish** again — as often as you like.

Two rules:

- **Never upload `App_Data` again.** It holds what the shop has entered since:
  cut sizes, product measures, stock postings, proof-of-payment files, and the
  keys that keep everyone signed in.
- **A code change is a publish; a catalogue change is a script.** New products,
  prices, panels or shades are SQL run against the hosted database, not a
  redeploy. The shades and the Sunroof panel seed themselves at startup, so
  those two arrive with the publish on their own.

If the database schema ever changes, it changes on the server first — the
application is database-first and has no migrations.

---

## Memory

The site has 256 MB. The application is configured for it (workstation GC),
and a shop with a few people on it at once is nowhere near the limit. The
spike to watch is PDF generation, which loads fonts and renders images.

If the site starts restarting under load, the first thing to try is generating
fewer PDFs at once; the second is the paid tier, which raises the ceiling.

## The free plan's own terms

- Free subdomain only.
- Hosted in the EU, so expect roughly 250–350 ms from the Philippines. Usable,
  but every click is perceptibly slower than a local machine.
- A free site that goes unused for **six months is archived**. Open it
  occasionally.
- No uptime guarantee. For a defence, have a local copy ready to run as well.
