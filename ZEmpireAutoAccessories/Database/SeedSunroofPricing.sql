-- ============================================================
-- Z-Empire Auto Accessories - Sunroof panel pricing
--
-- The Sunroof panel exists but carries no price, so it does not
-- appear on the quotation, job order and service invoice forms --
-- those build their panel list from cat.Pricing.
--
-- NOBODY SUPPLIED A SUNROOF PRICE. Rather than invent one, this
-- script copies the shop's own QUARTER WINDOW price, which is the
-- other "extra" piece of glass: priced on its own, and only on the
-- classes that have one. Every peso below therefore comes from the
-- shop's own price list, not from a guess.
--
-- A multiplier is applied so the owner can say how a sunroof differs
-- from a quarter window without re-typing 43 prices. Edit the four
-- numbers in @Factor, then run.
--
-- WHY IT IS PER CLASS. The shop's measured sheet shows two very
-- different sunroofs, and they are not the same job:
--
--     Subcompact/Crossover   152.4 x 68.58 cm   ->  137.16 cm of film
--     MPV                    152.4 x 68.58 cm   ->  137.16 cm of film
--     SUV                     68.58 x 40.64 cm  ->   40.64 cm of film
--     Big SUV                 68.58 x 40.64 cm  ->   40.64 cm of film
--
-- The first two are panoramic roofs that take more than three times
-- the film of the second two, and more than twice the film of a
-- quarter window. Leaving every multiplier at 1.00 therefore prices a
-- panoramic roof as if it were a quarter window, which is almost
-- certainly too low. The figures are the owner's to set; the shape of
-- the problem is laid out here so the decision is an informed one.
--
-- Pricing is NOT driven by film consumption in this catalogue - across
-- Stone on a Subcompact/Crossover the peso-per-centimetre runs from
-- 10.6 to 29.5 - so the cut sizes above are context for a human
-- judgement, not a formula to compute a price from.
--
-- Idempotent: an existing Sunroof price is never overwritten.
-- ============================================================

SET NOCOUNT ON;

-- ---- EDIT THESE FOUR NUMBERS ----
-- Sunroof price = this multiplier x the quarter-window price.
DECLARE @Factor TABLE (ClassificationName nvarchar(100) PRIMARY KEY, Multiplier decimal(10,4));
INSERT INTO @Factor (ClassificationName, Multiplier) VALUES
    (N'Subcompact/Crossover', 1.00),   -- panoramic roof, 137.16 cm of film
    (N'MPV',                  1.00),   -- panoramic roof, 137.16 cm of film
    (N'SUV',                  1.00),   -- small sunroof,   40.64 cm of film
    (N'Big SUV',              1.00);   -- small sunroof,   40.64 cm of film

-- Round to the nearest 50 pesos, half away from zero, so a multiplier
-- like 1.75 does not produce a price ending in 37.
DECLARE @RoundTo decimal(10,2) = 50;

DECLARE @SunroofPanelID int =
    (SELECT PanelID FROM cat.Panel WHERE PanelName = N'Sunroof');

IF @SunroofPanelID IS NULL
BEGIN
    RAISERROR (N'There is no Sunroof panel. Run SeedTintShadesAndSunroof.sql first, or start the application once.', 16, 1);
    RETURN;
END

INSERT INTO cat.Pricing (ProductID, TintVariantID, VehicleClassificationID, PanelID, Price)
SELECT
    q.ProductID,
    q.TintVariantID,
    q.VehicleClassificationID,
    @SunroofPanelID,
    ROUND(q.Price * f.Multiplier / @RoundTo, 0) * @RoundTo
FROM cat.Pricing q
JOIN cat.Panel qp
    ON qp.PanelID = q.PanelID AND qp.PanelName = N'Qtr Window'
JOIN crm.VehicleClassification vc
    ON vc.VehicleClassificationID = q.VehicleClassificationID
JOIN @Factor f
    ON f.ClassificationName = vc.ClassificationName
WHERE NOT EXISTS (
    SELECT 1 FROM cat.Pricing s
    WHERE s.ProductID = q.ProductID
      AND s.PanelID = @SunroofPanelID
      AND s.VehicleClassificationID = q.VehicleClassificationID
      AND ((s.TintVariantID IS NULL AND q.TintVariantID IS NULL)
           OR s.TintVariantID = q.TintVariantID)
);

PRINT CONCAT(N'Sunroof prices created: ', @@ROWCOUNT, N'.');

-- Anything still missing is a pairing with no quarter-window price to
-- copy from. Set those by hand under Pricing; there is normally one.
SELECT DISTINCT
    p.ProductName,
    tv.VariantName,
    vc.ClassificationName,
    N'no quarter-window price to copy - set this one by hand' AS Note
FROM cat.Pricing q
JOIN cat.Product p ON p.ProductID = q.ProductID
LEFT JOIN cat.TintVariant tv ON tv.TintVariantID = q.TintVariantID
JOIN crm.VehicleClassification vc ON vc.VehicleClassificationID = q.VehicleClassificationID
JOIN @Factor f ON f.ClassificationName = vc.ClassificationName
WHERE NOT EXISTS (
    SELECT 1 FROM cat.Pricing s
    WHERE s.ProductID = q.ProductID
      AND s.PanelID = @SunroofPanelID
      AND s.VehicleClassificationID = q.VehicleClassificationID
      AND ((s.TintVariantID IS NULL AND q.TintVariantID IS NULL)
           OR s.TintVariantID = q.TintVariantID)
)
ORDER BY p.ProductName, tv.VariantName, vc.ClassificationName;
