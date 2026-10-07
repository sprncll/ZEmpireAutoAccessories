-- ============================================================
-- Z-Empire Auto Accessories - BF Film per-panel pricing
--
-- BF Film had three tint variants, nine shades, and not one price row - so
-- picking it on a document gave no panel and a price of zero. This fills that
-- in, on the same Product x Variant x Classification x Panel grid every other
-- tinted product uses.
--
-- NOT PER SHADE, deliberately. cat.Pricing is keyed on the four columns above
-- and has no ShadeID; Light, Medium and Superdark of the same variant are the
-- same price, which is what the shop confirmed. The shade is still recorded on
-- the line - it says what was fitted, not what it cost.
--
-- WHERE THE NUMBERS CAME FROM - please review and correct:
--   BF Stone takes the existing Black Series "Stone" prices unchanged.
--   BF Stone takes them EXACTLY - a copy is a copy, and rounding the base
--   tier would have turned Black Series' 2,250 into 2,200, a quiet price cut.
--   BF Pro and BF Supreme are those prices + 35% and + 75%, rounded to the
--   nearest 100 peso, half away from zero.
--
-- Those two percentages are a STARTING POINT, not the shop's prices. They were
-- asked for and not given, so they are stated here rather than hidden: change
-- the two numbers below, regenerate, or simply correct the figures in the
-- Pricing screen, which journals every change to cat.PriceHistory.
--
-- The shop's own variant tiering was considered as a source and rejected:
-- between tiers it runs x1.8 to x2.3 with a x0.38 to x2.25 spread across
-- panels, which would have produced noise rather than a price list.
--
-- Idempotent: a price already on record for a product/variant/class/panel is
-- left exactly as it is, so re-running never overwrites an edited price.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 4000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 9000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 4000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 1000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 11000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 2500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 8500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 4500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2250);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2250);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 1500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 13000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 5000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 14500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 5500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 16000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 6000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 4000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 4000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Stone'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 19500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 4100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 2700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 9500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 5400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 12200);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 5400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 1400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 4700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 11500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 6100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 3000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 17600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 6800);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 3400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 19600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 7400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 4100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 4100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 5400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2700);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 21600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 8100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 5400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 6100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 5400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Pro'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 26300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 5300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Small'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 12300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 5300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Sedan'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 15800);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 5300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 1800);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Subcompact/Crossover'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 19300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 6100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 4400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Pickup'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 14900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 7900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 3900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 3900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 6100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 2600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 22800);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 8800);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 4400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 4400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 6100);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'MPV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 25400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 9600);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 5300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 5300);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 3500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Big SUV'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 28000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Front WS'), 10500);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2FWS'), 4400);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'2RWS'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Rear WS'), 7900);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Qtr Window'), 7000);
IF NOT EXISTS (SELECT 1 FROM cat.Pricing WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID)
    AND TintVariantID = (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme')
    AND VehicleClassificationID = (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van')
    AND PanelID = (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'))
    INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
    VALUES ((SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID),
            (SELECT TintVariantID FROM cat.TintVariant WHERE ProductID = (SELECT TOP 1 ProductID FROM cat.Product WHERE ProductName = N'BF Film' ORDER BY ProductID) AND VariantName = N'BF Supreme'),
            (SELECT VehicleClassificationID FROM crm.VehicleClassification WHERE ClassificationName = N'Van'),
            (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Full Wrap'), 34100);
GO
