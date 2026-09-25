/*
    Adds a per-product override for how a product is measured.

    Until now this followed the product's category name: anything under
    Window Tint / Tint / Paint Protection Film / Paint Protection was
    measured by length (stored in centimetres, entered in cm/in/m) and
    everything else was counted in whole pieces. That could not express a
    product that differs from its category - an applicator or squeegee
    filed under Tint, or a film in a category the code does not know.

    SoldByLength:
        NULL  - follow the category (this is the default, so nothing that
                exists today changes behaviour)
        1     - measured by length, whatever the category says
        0     - counted in pieces, whatever the category says

    Nullable with no default, so the column is added without touching a
    single existing row.

    RUN THIS BEFORE DEPLOYING the matching application build. The app
    selects this column, so it will error on the Product screens until the
    column exists. Running it early is harmless: the current build simply
    ignores the column.

    Safe to run more than once.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'cat.Product')
      AND name = N'SoldByLength')
BEGIN
    ALTER TABLE cat.Product ADD SoldByLength BIT NULL;
    PRINT 'Added cat.Product.SoldByLength.';
END
ELSE
BEGIN
    PRINT 'cat.Product.SoldByLength already exists - nothing to do.';
END
GO

/*
    Optional check: which products are measured by length once this is in,
    and whether that comes from the product or from its category.
*/
SELECT
    p.ProductID,
    p.ProductName,
    c.CategoryName,
    p.SoldByLength                                   AS ProductOverride,
    CASE WHEN c.CategoryName IN
        (N'Paint Protection Film', N'Paint Protection', N'Window Tint', N'Tint')
        THEN 1 ELSE 0 END                            AS CategoryDefault,
    CASE WHEN p.SoldByLength IS NOT NULL THEN p.SoldByLength
         WHEN c.CategoryName IN
            (N'Paint Protection Film', N'Paint Protection', N'Window Tint', N'Tint')
         THEN 1 ELSE 0 END                           AS MeasuredByLength
FROM cat.Product p
JOIN cat.ProductCategory c ON c.CategoryID = p.CategoryID
ORDER BY c.CategoryName, p.ProductName;
GO
