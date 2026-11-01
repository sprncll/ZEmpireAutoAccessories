/* ============================================================================
   Window Tint audit - products, variants, shades, panels and prices
   ----------------------------------------------------------------------------
   Read-only. Nothing here changes a row; every section is a SELECT.

   A tint is priced by four things together - product, tint variant, vehicle
   class and panel - and a shade hangs off the variant. Four tables have to
   agree before a tint can be quoted, and nothing in the schema forces them to,
   so this looks for the places they have come apart.

   Run it against the database the system is actually using. Each section
   prints its own heading; a section that returns no rows found nothing wrong.
   ========================================================================== */

SET NOCOUNT ON;

/* ---------------------------------------------------------------------------
   1. The products, and how far each one is set up.
   Prices = 0 means the product cannot be quoted at all.
   --------------------------------------------------------------------------- */
SELECT '1. Window Tint products' AS Section;

SELECT
    p.ProductID,
    p.ProductName,
    p.IsActive,
    Variants = (SELECT COUNT(*) FROM cat.TintVariant v WHERE v.ProductID = p.ProductID),
    Shades   = (SELECT COUNT(*) FROM cat.Shade s
                JOIN cat.TintVariant v ON v.TintVariantID = s.TintVariantID
                WHERE v.ProductID = p.ProductID),
    Prices   = (SELECT COUNT(*) FROM cat.Pricing pr WHERE pr.ProductID = p.ProductID),
    Panels   = (SELECT COUNT(DISTINCT pr.PanelID) FROM cat.Pricing pr WHERE pr.ProductID = p.ProductID)
FROM cat.Product p
JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
WHERE c.CategoryName LIKE N'%Tint%'
ORDER BY p.ProductName;


/* ---------------------------------------------------------------------------
   2. Each variant, and whether it carries shades and prices.
   A variant with no shades cannot say what went on the car. A variant with no
   prices never appears on a form.
   --------------------------------------------------------------------------- */
SELECT '2. Variants under each product' AS Section;

SELECT
    p.ProductName,
    v.TintVariantID,
    v.VariantName,
    Shades = (SELECT COUNT(*) FROM cat.Shade s WHERE s.TintVariantID = v.TintVariantID),
    Prices = (SELECT COUNT(*) FROM cat.Pricing pr WHERE pr.TintVariantID = v.TintVariantID),
    Verdict = CASE
        WHEN NOT EXISTS (SELECT 1 FROM cat.Pricing pr WHERE pr.TintVariantID = v.TintVariantID)
             AND NOT EXISTS (SELECT 1 FROM cat.Shade s WHERE s.TintVariantID = v.TintVariantID)
            THEN N'unused - no shades, no prices'
        WHEN NOT EXISTS (SELECT 1 FROM cat.Pricing pr WHERE pr.TintVariantID = v.TintVariantID)
            THEN N'has shades but no price - will not appear on a form'
        WHEN NOT EXISTS (SELECT 1 FROM cat.Shade s WHERE s.TintVariantID = v.TintVariantID)
            THEN N'priced but no shades - the line cannot record what was fitted'
        ELSE N'ok'
    END
FROM cat.TintVariant v
JOIN cat.Product p ON p.ProductID = v.ProductID
JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
WHERE c.CategoryName LIKE N'%Tint%'
ORDER BY p.ProductName, v.VariantName;


/* ---------------------------------------------------------------------------
   3. Which panels each product is priced for, and at what spread.
   A tint is expected to carry the glass panels; a bundle alongside them is the
   whole-car price. See section 5 for bundles that landed on the wrong product.
   --------------------------------------------------------------------------- */
SELECT '3. Panels priced per product' AS Section;

SELECT
    p.ProductName,
    pn.PanelName,
    Rows        = COUNT(*),
    Classes     = COUNT(DISTINCT pr.VehicleClassificationID),
    Variants    = COUNT(DISTINCT pr.TintVariantID),
    LowestPrice = MIN(pr.Price),
    HighestPrice= MAX(pr.Price)
FROM cat.Pricing pr
JOIN cat.Product p ON p.ProductID = pr.ProductID
JOIN cat.Panel pn ON pn.PanelID = pr.PanelID
JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
WHERE c.CategoryName LIKE N'%Tint%'
GROUP BY p.ProductName, pn.PanelName
ORDER BY p.ProductName, pn.PanelName;


/* ---------------------------------------------------------------------------
   4. Price rows whose variant belongs to a DIFFERENT product.
   Nothing in the schema stops this, and it cannot be seen from any screen.
   Any row here is corrupt and should be deleted or repointed.
   --------------------------------------------------------------------------- */
SELECT '4. Price rows pointing at another product''s variant' AS Section;

SELECT
    pr.PricingID,
    PricedProduct = p.ProductName,
    VariantName   = v.VariantName,
    VariantBelongsTo = vp.ProductName,
    pn.PanelName,
    pr.Price
FROM cat.Pricing pr
JOIN cat.Product p ON p.ProductID = pr.ProductID
JOIN cat.TintVariant v ON v.TintVariantID = pr.TintVariantID
JOIN cat.Product vp ON vp.ProductID = v.ProductID
JOIN cat.Panel pn ON pn.PanelID = pr.PanelID
WHERE v.ProductID <> pr.ProductID
ORDER BY p.ProductName;


/* ---------------------------------------------------------------------------
   5. The two bundle panels, on products they do not belong to.
   "Full Wrap" is every piece of glass, so it belongs to tint.
   "Whole Vehicle" is the painted body, so it belongs to PPF and coating.
   A tint priced for Whole Vehicle, or a non-tint priced for Full Wrap, is a
   panel picked from the wrong half of the list.
   --------------------------------------------------------------------------- */
SELECT '5. Bundle panels on the wrong kind of product' AS Section;

SELECT
    p.ProductName,
    CategoryName = c.CategoryName,
    pn.PanelName,
    Rows  = COUNT(*),
    Prices = CONCAT(MIN(pr.Price), N' - ', MAX(pr.Price)),
    Verdict = CASE
        WHEN pn.PanelName = N'Whole Vehicle' AND c.CategoryName LIKE N'%Tint%'
            THEN N'a tint priced for the painted body'
        ELSE N'a non-tint priced for all the glass'
    END
FROM cat.Pricing pr
JOIN cat.Product p ON p.ProductID = pr.ProductID
JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
JOIN cat.Panel pn ON pn.PanelID = pr.PanelID
WHERE (pn.PanelName = N'Whole Vehicle' AND c.CategoryName LIKE N'%Tint%')
   OR (pn.PanelName = N'Full Wrap'     AND c.CategoryName NOT LIKE N'%Tint%')
GROUP BY p.ProductName, c.CategoryName, pn.PanelName
ORDER BY p.ProductName;


/* ---------------------------------------------------------------------------
   6. Gaps in the matrix: a variant priced for some vehicle classes but not
   all of the ones it is priced for elsewhere. These are the combinations that
   make a form read "no panel prices for this vehicle class".
   --------------------------------------------------------------------------- */
SELECT '6. Vehicle classes missing from a variant that has others' AS Section;

WITH Priced AS (
    SELECT DISTINCT pr.ProductID, pr.TintVariantID, pr.PanelID, pr.VehicleClassificationID
    FROM cat.Pricing pr
    JOIN cat.Product p ON p.ProductID = pr.ProductID
    JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
    WHERE c.CategoryName LIKE N'%Tint%'
),
Expected AS (
    SELECT DISTINCT x.ProductID, x.TintVariantID, x.PanelID, vc.VehicleClassificationID
    FROM (SELECT DISTINCT ProductID, TintVariantID, PanelID FROM Priced) x
    CROSS JOIN (SELECT DISTINCT VehicleClassificationID FROM Priced) vc
)
SELECT
    p.ProductName,
    VariantName = ISNULL(v.VariantName, N'(no variant)'),
    pn.PanelName,
    MissingClass = vc.ClassificationName
FROM Expected e
JOIN cat.Product p ON p.ProductID = e.ProductID
LEFT JOIN cat.TintVariant v ON v.TintVariantID = e.TintVariantID
JOIN cat.Panel pn ON pn.PanelID = e.PanelID
JOIN crm.VehicleClassification vc ON vc.VehicleClassificationID = e.VehicleClassificationID
WHERE NOT EXISTS (
    SELECT 1 FROM Priced pd
    WHERE pd.ProductID = e.ProductID
      AND ISNULL(pd.TintVariantID, -1) = ISNULL(e.TintVariantID, -1)
      AND pd.PanelID = e.PanelID
      AND pd.VehicleClassificationID = e.VehicleClassificationID)
ORDER BY p.ProductName, v.VariantName, pn.PanelName, vc.ClassificationName;


/* ---------------------------------------------------------------------------
   7. Duplicates: the same product, variant, class and panel priced twice.
   The forms read the first one they find, so the other is invisible and may
   not carry the same figure.
   --------------------------------------------------------------------------- */
SELECT '7. Duplicate price rows' AS Section;

SELECT
    p.ProductName,
    VariantName = ISNULL(v.VariantName, N'(no variant)'),
    vc.ClassificationName,
    pn.PanelName,
    Rows = COUNT(*),
    Prices = CONCAT(MIN(pr.Price), N' - ', MAX(pr.Price))
FROM cat.Pricing pr
JOIN cat.Product p ON p.ProductID = pr.ProductID
LEFT JOIN cat.TintVariant v ON v.TintVariantID = pr.TintVariantID
JOIN crm.VehicleClassification vc ON vc.VehicleClassificationID = pr.VehicleClassificationID
JOIN cat.Panel pn ON pn.PanelID = pr.PanelID
GROUP BY p.ProductName, v.VariantName, vc.ClassificationName, pn.PanelName
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, p.ProductName;


/* ---------------------------------------------------------------------------
   8. Shades that no stock count has ever covered, for films that have stock.
   These are the rows the Shade Stock page cannot work a figure forward from.
   --------------------------------------------------------------------------- */
SELECT '8. Shades never counted' AS Section;

SELECT
    p.ProductName,
    v.VariantName,
    s.ShadeName
FROM cat.Shade s
JOIN cat.TintVariant v ON v.TintVariantID = s.TintVariantID
JOIN cat.Product p ON p.ProductID = v.ProductID
WHERE NOT EXISTS (
    SELECT 1 FROM inv.InventoryCheckDetail d WHERE d.ShadeID = s.ShadeID)
ORDER BY p.ProductName, v.VariantName, s.ShadeName;
