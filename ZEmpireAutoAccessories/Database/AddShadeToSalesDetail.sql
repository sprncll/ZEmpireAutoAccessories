/* =====================================================================
   sales.SalesDetail: record WHICH SHADE a counter sale took off the roll
   ---------------------------------------------------------------------

   Why
   ---
   A sale of film comes off the product as a whole. inv.InventoryTransaction
   records a product and nothing finer, and sales.SalesDetail carried no
   shade either, so selling 10 m of BF Supreme Superdark over the counter
   left the shade breakdown on Inventory > Shades untouched: the product's
   figure went down, every shade's figure stayed where it was, and the gap
   showed up as a mystery at the next stock count.

   Every other document line - ops.JobOrderDetail, sales.ServiceInvoiceDetail,
   sales.QuotationDetail - has carried TintVariantID and ShadeID all along.
   This brings the counter into line with them.

   What it does
   ------------
   Adds one NULLABLE column with a foreign key to cat.Shade, and an index
   on it. Nothing else is touched:

     * Existing rows get NULL, which reads as "no shade recorded" - the same
       as every sale written before today. Nothing is backfilled, because
       nobody knows which shade those sales took and a guess would be worse
       than a blank.
     * Nullable, so nothing that writes a sales line without a shade breaks -
       a bolt and a step board have no shade to record.
     * No computed column, trigger or constraint is altered. SubTotal stays
       as it is.

   Run it on EVERY database the app points at - the local one and the hosted
   one - BEFORE publishing the build that uses it. The app is
   database-first: it will ask for this column, and a database without it
   answers with an error on the sales screens.

   Safe to run twice: it checks before it adds.
   ===================================================================== */

USE ZEmpire;   -- the hosted copy is db_ad034e_zempire; change this line there
GO

/* ---- 1. the column ------------------------------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'sales.SalesDetail') AND name = N'ShadeID')
BEGIN
    ALTER TABLE sales.SalesDetail ADD ShadeID INT NULL;
    PRINT 'Added sales.SalesDetail.ShadeID';
END
ELSE
    PRINT 'sales.SalesDetail.ShadeID already exists - left as it is';
GO

/* ---- 2. the foreign key -------------------------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_SalesDetail_Shade'
      AND parent_object_id = OBJECT_ID(N'sales.SalesDetail'))
BEGIN
    ALTER TABLE sales.SalesDetail
        ADD CONSTRAINT FK_SalesDetail_Shade
        FOREIGN KEY (ShadeID) REFERENCES cat.Shade (ShadeID);
    PRINT 'Added FK_SalesDetail_Shade';
END
ELSE
    PRINT 'FK_SalesDetail_Shade already exists - left as it is';
GO

/* ---- 3. an index, because the shade breakdown reads by shade ------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SalesDetail_ShadeID'
      AND object_id = OBJECT_ID(N'sales.SalesDetail'))
BEGIN
    CREATE INDEX IX_SalesDetail_ShadeID
        ON sales.SalesDetail (ShadeID)
        WHERE ShadeID IS NOT NULL;
    PRINT 'Added IX_SalesDetail_ShadeID';
END
ELSE
    PRINT 'IX_SalesDetail_ShadeID already exists - left as it is';
GO

/* ---- 4. show what is now there ------------------------------------- */
SELECT
    c.name          AS ColumnName,
    t.name          AS DataType,
    c.is_nullable   AS IsNullable
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'sales.SalesDetail')
ORDER BY c.column_id;

SELECT
    fk.name                AS ForeignKey,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencesTable
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'sales.SalesDetail');
GO
