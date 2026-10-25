-- ============================================================
-- Z-Empire Auto Accessories - tint shades and the Sunroof panel
--
-- Two gaps found while raising a job order for Black Series:
--
--   1. Eight tint variants carried no shades at all, so the add-line
--      form on the quotation, job order and service invoice read
--      "No shades on file for this variant" and the saved line could
--      not say which shade went on the car.
--
--   2. cat.Panel had no Sunroof row, so a sunroof could not be put on
--      a document even though the shop fits them and the cut-size
--      table already carries sunroof glass for the four vehicle
--      classes that have one.
--
-- The application seeds both of these at startup as well
-- (Data/ShadeSeeder.cs and Data/PanelSeeder.cs), so a running
-- installation picks them up without anyone running this file. It is
-- here for rebuilding a catalogue from nothing, and to be reviewable
-- as text.
--
-- READ BEFORE RUNNING -- the shade names are a default, not the
-- shop's own list. Light / Medium / Superdark is the ladder every
-- variant whose shades the shop DID supply already uses: BF Stone,
-- BF Pro, BF Supreme and Profilm X Lite. It is applied to the eight
-- that were bare so a line can record a shade at all.
--
-- It carries no money with it. cat.Pricing is keyed on product,
-- variant, classification and panel and has no ShadeID, and the shop
-- confirmed that the shades of one variant cost the same. A shade
-- records what was FITTED, so a wrong name here misdescribes a job
-- sheet but cannot mis-charge a customer. Correct any that are wrong
-- in the application; nothing needs re-running.
--
-- The Sunroof panel is NOT priced here, because nobody supplied a
-- sunroof price. Set one under Pricing for each vehicle class that
-- has a sunroof. The panel will not appear on the add-line forms
-- until it has a price, because those forms build their panel list
-- from cat.Pricing.
--
-- Idempotent: safe to re-run, will not create duplicate rows.
-- ============================================================

-- ---- The Sunroof panel ----
IF NOT EXISTS (SELECT 1 FROM cat.Panel WHERE PanelName = N'Sunroof')
    INSERT INTO cat.Panel (PanelName) VALUES (N'Sunroof');
GO

-- ---- Shades for the variants that had none ----

-- Black Series / Stone
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Stone';

-- Black Series / Ceramic Pro
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Pro';

-- Black Series / Ceramic Supreme
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Black Series' AND tv.VariantName = N'Ceramic Supreme';

-- Clear Series / Play
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Play';

-- Clear Series / Freeze
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Freeze';

-- Clear Series / Max
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Clear Series' AND tv.VariantName = N'Max';

-- Profilm Nano Ceramic Tint / Pro (99%)
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Pro (99%)';

-- Profilm Nano Ceramic Tint / Lite (97%)
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)' AND s.ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Light'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)' AND s.ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Medium'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)';
IF NOT EXISTS (
    SELECT 1 FROM cat.Shade s
    JOIN cat.TintVariant tv ON s.TintVariantID = tv.TintVariantID
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)' AND s.ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName)
    SELECT tv.TintVariantID, N'Superdark'
    FROM cat.TintVariant tv
    JOIN cat.Product p ON tv.ProductID = p.ProductID
    WHERE p.ProductName = N'Profilm Nano Ceramic Tint' AND tv.VariantName = N'Lite (97%)';

GO
