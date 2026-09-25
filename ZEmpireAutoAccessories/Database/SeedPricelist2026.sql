-- ============================================================
-- Z-Empire Auto Accessories - 2026 pricelist seed
--
-- Idempotent in the strong sense: a product is only created when
-- NEITHER its new name NOR any name it is already known by exists.
-- The 2026 list renames things that are already in the catalogue
-- (PROFILM X DIAMOND COATING is the existing "Diamond Coating";
-- SHIELD TPU PPF is the existing "ProFilm Shield"), so guarding on
-- the new name alone would create a second row for the same product.
-- Prices attach to whichever row is found.
--
-- Safe to re-run. Nothing is updated or deleted - a price already
-- recorded for a product/classification/panel is left as it is, so
-- re-running never overwrites a price someone edited in the app.
--
-- MAPPING ASSUMPTIONS - please review:
--   * Coating tiers onto the existing classification list:
--       SMALL  -> Small
--       MEDIUM -> Sedan, Subcompact/Crossover
--       LARGE  -> Pickup, SUV, MPV, Big SUV
--       XL     -> Van
--     The source calls Subcompact MEDIUM but Crossover HEV LARGE,
--     and this catalogue has one combined "Subcompact/Crossover"
--     class - it is priced as MEDIUM here.
--   * PPF tiers:
--       Small Sedan                 -> Small
--       Sedan                       -> Sedan
--       Subcompact/Crossover/Pickup -> Subcompact/Crossover, Pickup
--       SUV                         -> SUV
--       Big SUV/MPV                 -> Big SUV, MPV
--       Van                         -> Van
--   * Coating is priced on the "Whole Vehicle" panel, PPF on
--     "Full Wrap" (the list says "wrapped around").
--   * Detailing is priced per size but cat.Service carries a single
--     price, so it is seeded as one service per size.
-- ============================================================

-- ---- Product categories ----
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Coating')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Coating');
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Paint Protection Film');
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Window Tint')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Window Tint');
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Tonneau Cover');
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Car Care')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Car Care');
IF NOT EXISTS (SELECT 1 FROM cat.ProductCategory WHERE CategoryName = N'Accessories')
    INSERT INTO cat.ProductCategory (CategoryName) VALUES (N'Accessories');
GO

-- ---- Coating products ----
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating'))  -- already in the catalogue as Shield Coating
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'MTX C2 GLAZ Ceramic Shield Coating', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating'))
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'MTX C1 GLAZ Super Ceramic Coating', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating'))
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'MTX Combo C1 & C2 Coating', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating'))  -- already in the catalogue as Ultra Gloss Coating
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'Profilm X Ultra Gloss Coating', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating'))
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'Profilm X Premium Coating', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating'))  -- already in the catalogue as Diamond Coating
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Coating'), N'Profilm X Diamond Coating', 1);
GO

-- ---- Coating pricing (Whole Vehicle) ----
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 9900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 11900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 11900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C2 GLAZ Ceramic Shield Coating', N'Shield Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 10900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 12900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX C1 GLAZ Super Ceramic Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 15900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 11900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'MTX Combo C1 & C2 Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 16900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 13900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 15900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 15900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 16900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 16900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 16900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 16900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Ultra Gloss Coating', N'Ultra Gloss Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 18900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 17900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 19900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 19900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 20900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 20900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 20900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 20900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Premium Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 22900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 25900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 27900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 27900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 28900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 28900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 28900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 28900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm X Diamond Coating', N'Diamond Coating') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Whole Vehicle'), 32900);
GO

-- ---- Paint Protection Film products ----
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra'))  -- already in the catalogue as ProFilm Ultra
    INSERT INTO cat.Product (CategoryID, ProductName, Description, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film'), N'Profilm Ultra TPU PPF', N'6.5 MIL, 4 years warranty', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film'), N'Profilm Ultima TPU PPF', N'7.5 MIL, 5 years warranty', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield'))  -- already in the catalogue as ProFilm Shield
    INSERT INTO cat.Product (CategoryID, ProductName, Description, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film'), N'Profilm Shield TPU PPF', N'7.5 MIL, 6 years warranty', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film'), N'Profilm Elite TPU PPF', N'7.5 MIL, 7 years warranty', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme'))  -- already in the catalogue as ProFilm Supreme
    INSERT INTO cat.Product (CategoryID, ProductName, Description, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Paint Protection Film'), N'Profilm Supreme TPU PPF', N'7.5 MIL, 8 years warranty', 1);
GO

-- ---- Paint Protection Film pricing (Full Wrap) ----
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 45000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 50000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 60000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 60000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 65000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 70000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 70000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultra TPU PPF', N'ProFilm Ultra') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 80000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 50000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 55000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 65000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 65000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 70000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 80000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 80000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Ultima TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 90000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 60000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 65000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 75000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 75000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 85000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 95000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 95000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Shield TPU PPF', N'ProFilm Shield') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 105000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 70000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 75000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 85000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 85000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 90000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 100000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 100000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Elite TPU PPF') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 110000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 95000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 100000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 110000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 110000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 125000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 130000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 130000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID)
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap') AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van') AND TintVariantID IS NULL)
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Supreme TPU PPF', N'ProFilm Supreme') ORDER BY ProductID), NULL, (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'), (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 140000);
GO

-- ---- Detailing (cat.Service carries one price, so one service per size) ----
IF NOT EXISTS (SELECT 1 FROM cat.ServiceCategory WHERE CategoryName = N'Detailing')
    INSERT INTO cat.ServiceCategory (CategoryName) VALUES (N'Detailing');
GO

IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Full Exterior Detailing - Small')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Full Exterior Detailing - Small', 5500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Full Exterior Detailing - Medium')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Full Exterior Detailing - Medium', 6500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Full Exterior Detailing - Large')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Full Exterior Detailing - Large', 7500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Full Exterior Detailing - XL')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Full Exterior Detailing - XL', 8500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Interior Detailing - Small')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Interior Detailing - Small', 6900, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Interior Detailing - Medium')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Interior Detailing - Medium', 7900, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Interior Detailing - Large')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Interior Detailing - Large', 8900, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Service WHERE ServiceName = N'Interior Detailing - XL')
    INSERT INTO cat.Service (ServiceCategoryID, ServiceName, DefaultPrice, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ServiceCategory WHERE CategoryName = N'Detailing'), N'Interior Detailing - XL', 9900, 1);
GO

-- ---- Flat-priced products (tonneau covers, accessories, car care) ----
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Rocco / V / Conquest 2021-2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Rocco / V / Conquest 2021-2025', N'Trifold & removable. Railguard for tailgate +1,800 (1pc).', 42000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Rocco / V / Conquest 2021-2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Rocco / V / Conquest 2021-2025', N'Lifts to roof level, foldable & removable. Railguard for tailgate +1,800 (1pc).', 62000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Pro4X'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Pro4X', N'Trifold & removable. Railguard for sides & tailgate +4,200 (3pcs).', 42000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Pro4X'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Pro4X', N'Lifts to roof level, foldable & removable. Railguard for sides & tailgate +4,200 (3pcs).', 62000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Ford Ranger'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Ford Ranger', N'Trifold & removable. Railguard +4,200 (3pcs, Raptor only).', 39000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Ford Ranger'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Ford Ranger', N'Lifts to roof level, foldable & removable. Railguard +4,200 (3pcs, Raptor only).', 59000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Wildtrak'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Wildtrak', N'Trifold & removable. Railguard for tailgate +1,800 (1pc).', 42000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Wildtrak'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Wildtrak', N'Lifts to roof level, foldable & removable. Railguard for tailgate +1,800 (1pc).', 62000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Hilux Revo G E J / Vigo 2012-2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Hilux Revo G E J / Vigo 2012-2025', N'Trifold & removable.', 39000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Hilux Revo G E J / Vigo 2012-2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Hilux Revo G E J / Vigo 2012-2025', N'Lifts to roof level, foldable & removable.', 59000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - NP300'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - NP300', N'Trifold & removable.', 39000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - NP300'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - NP300', N'Lifts to roof level, foldable & removable.', 59000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Strada'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Strada', N'Trifold & removable.', 42000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Strada'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Strada', N'Lifts to roof level, foldable & removable.', 62000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - D-Max V-Cross LSA'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - D-Max V-Cross LSA', N'Trifold & removable. Railguard for tailgate +1,800 (1pc).', 42000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - D-Max V-Cross LSA'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - D-Max V-Cross LSA', N'Lifts to roof level, foldable & removable. Railguard for tailgate +1,800 (1pc).', 62000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - D-Max'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - D-Max', N'Trifold & removable.', 39000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Next Gen Ranger'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Next Gen Ranger', N'Trifold & removable.', 44000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Next Gen Ranger'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Next Gen Ranger', N'Lifts to roof level, foldable & removable.', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Pro Ultimate - Raptor'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Pro Ultimate - Raptor', N'Lifts to roof level, foldable & removable.', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Pro Ultimate - Pro4X'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Pro Ultimate - Pro4X', N'Lifts to roof level, foldable & removable.', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Hilux GRS'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Hilux GRS', N'Foldable & removable. Railguard +4,200 (3pcs).', 44000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Hilux GRS'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Hilux GRS', N'Lifts to roof level, foldable & removable. Railguard +4,200 (3pcs).', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Plus Junior - Hilux GRS New'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Plus Junior - Hilux GRS New', N'Foldable & removable, integrated roof rail and hydraulic. Railguard +4,200 (3pcs).', 44000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Plus Ultimate - Hilux GRS New'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Plus Ultimate - Hilux GRS New', N'Lifts to roof level, foldable & removable, integrated roof rail. Railguard +4,200 (3pcs).', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - D-Max X LSE 2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - D-Max X LSE 2025', N'Trifold & removable. Railguard for tailgate +1,800 (1pc).', 44000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - D-Max X LSE 2025'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - D-Max X LSE 2025', N'Lifts to roof level, foldable & removable. Railguard for tailgate +1,800 (1pc).', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Junior - Triton GLX'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Junior - Triton GLX', N'Foldable & removable.', 44000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Topflip Ultimate - Triton GLX'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Topflip Ultimate - Triton GLX', N'Lifts to roof level, foldable & removable.', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'TopUp Euro MB - Rocco'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'TopUp Euro MB - Rocco', N'Lifts to roof level, foldable & removable.', 64000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'TopUp Euro MB - Next Gen'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'TopUp Euro MB - Next Gen', N'Lifts to roof level.', 68000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'TopUp Euro MB - Triton GLS'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'TopUp Euro MB - Triton GLS', N'Lifts to roof level.', 68000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Trifold V2 - NP300'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Trifold V2 - NP300', N'Trifold & removable. SRP 35,000; 30,000 for cash payment.', 35000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Rage Roller Lid'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Rage Roller Lid', N'SRP 29,000; 27,000 for cash payment. New Gen Wildtrak & Raptor, T6/T7 Ranger & Wildtrak, Triton 2024+, Revo, Rocco, Pro4X, NP300 2012+, Strada 2015+, V-Cross 2015+, D-Max 2012+.', 29000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Rage Trifold'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Rage Trifold', N'3 panels. Next Gen Raptor XLT Sports, T6 Ranger, Raptor, Revo, NP300, D-Max 2012-2020.', 25000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Armor Trifold'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Armor Trifold', N'SRP. Revo 2015+, Ranger T6/T7, NP300 2015+.', 15000, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Armor Soft Roll Cover'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Armor Soft Roll Cover', N'NP300 2015+, Hilux Revo 2015+, Ford Ranger 2012-2020, New Gen Ranger & Wildtrak 2023, Triton 2026.', 12500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Prolift Tailgate Assist 8500'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Prolift Tailgate Assist 8500', N'Prolift tailgate assist.', 8500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Prolift Tailgate Assist 5500'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Tonneau Cover'), N'Prolift Tailgate Assist 5500', N'Prolift tailgate assist.', 5500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Armor Rear Side Step'))
    INSERT INTO cat.Product (CategoryID, ProductName, Description, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Armor Rear Side Step', N'Hilux Revo J, E, G only.', 8500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Horn M-75s'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'MXR Horn M-75s', 3800, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Horn M-80'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'MXR Horn M-80', 3800, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Horn M-90'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'MXR Horn M-90', 4800, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Wiper Blades'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'MXR Wiper Blades', 850, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Steelcore Security Strap Pair - 12ft / 3.6m'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Steelcore Security Strap Pair - 12ft / 3.6m', 4800, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Steelcore Security Strap Pair - 15ft / 4.5m'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Steelcore Security Strap Pair - 15ft / 4.5m', 5300, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Steelcore Security Strap Pair - 9ft / 2.7m'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Steelcore Security Strap Pair - 9ft / 2.7m', 4200, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Steelcore Security Strap - 6ft / 1.8m'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Steelcore Security Strap - 6ft / 1.8m', 2200, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Steelcore Security Strap - 4.5ft / 1.3m'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Accessories'), N'Steelcore Security Strap - 4.5ft / 1.3m', 1900, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Cleaning Wax'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Car Care'), N'MXR Cleaning Wax', 500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Shampoo'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Car Care'), N'MXR Shampoo', 200, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Scratch Remover Wax'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Car Care'), N'MXR Scratch Remover Wax', 500, 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'MXR Tire Inflator & Sealer'))
    INSERT INTO cat.Product (CategoryID, ProductName, DefaultPrice, IsActive) VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Car Care'), N'MXR Tire Inflator & Sealer', 200, 1);
GO

-- ---- Nano ceramic tint: brands as products, lines as variants, shades under each ----
-- Profilm already exists as "Profilm Nano Ceramic Tint"; its lines are added as
-- variants of that product rather than as a second Profilm product.
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'BF Film'))
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Window Tint'), N'BF Film', 1);
IF NOT EXISTS (SELECT 1 FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint'))
    INSERT INTO cat.Product (CategoryID, ProductName, IsActive)
    VALUES ((SELECT CategoryID FROM cat.ProductCategory WHERE CategoryName = N'Window Tint'), N'Profilm Nano Ceramic Tint', 1);
GO

IF NOT EXISTS (SELECT 1 FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone')
    INSERT INTO cat.TintVariant (ProductID, VariantName) VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID), N'BF Stone');
IF NOT EXISTS (SELECT 1 FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro')
    INSERT INTO cat.TintVariant (ProductID, VariantName) VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID), N'BF Pro');
IF NOT EXISTS (SELECT 1 FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme')
    INSERT INTO cat.TintVariant (ProductID, VariantName) VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID), N'BF Supreme');
IF NOT EXISTS (SELECT 1 FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance')
    INSERT INTO cat.TintVariant (ProductID, VariantName) VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID), N'Profilm X Pro/Advance');
IF NOT EXISTS (SELECT 1 FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite')
    INSERT INTO cat.TintVariant (ProductID, VariantName) VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID), N'Profilm X Lite');
GO

IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone') AND ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone'), N'Light');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone') AND ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone'), N'Medium');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone') AND ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Stone'), N'Superdark');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro') AND ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro'), N'Light');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro') AND ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro'), N'Medium');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro') AND ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Pro'), N'Superdark');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme') AND ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme'), N'Light');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme') AND ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme'), N'Medium');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme') AND ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'BF Film') ORDER BY ProductID) AND VariantName = N'BF Supreme'), N'Superdark');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance') AND ShadeName = N'Clear Blue')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance'), N'Clear Blue');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance') AND ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance'), N'Light');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance') AND ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance'), N'Medium');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance') AND ShadeName = N'Dark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance'), N'Dark');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance') AND ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Pro/Advance'), N'Superdark');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite') AND ShadeName = N'Light')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite'), N'Light');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite') AND ShadeName = N'Medium')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite'), N'Medium');
IF NOT EXISTS (SELECT 1 FROM cat.Shade WHERE TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite') AND ShadeName = N'Superdark')
    INSERT INTO cat.Shade (TintVariantID, ShadeName) VALUES ((SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName IN (N'Profilm Nano Ceramic Tint') ORDER BY ProductID) AND VariantName = N'Profilm X Lite'), N'Superdark');
GO

-- ---- Review: anything that looks like the same product twice ----
-- Near-duplicate names the guards above could not have caught, for you to
-- merge or rename by hand. Nothing is changed by this query.
SELECT  p1.ProductName AS Product, c1.CategoryName AS Category,
        p2.ProductName AS LooksLikeDuplicateOf, c2.CategoryName AS OtherCategory
FROM    cat.Product p1
JOIN    cat.ProductCategory c1 ON c1.CategoryID = p1.CategoryID
JOIN    cat.Product p2 ON p2.ProductID > p1.ProductID
JOIN    cat.ProductCategory c2 ON c2.CategoryID = p2.CategoryID
WHERE   p2.ProductName LIKE N'%' + p1.ProductName + N'%'
   OR   p1.ProductName LIKE N'%' + p2.ProductName + N'%'
ORDER BY p1.ProductName;
GO
