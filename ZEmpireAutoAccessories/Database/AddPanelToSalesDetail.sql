/* =====================================================================
   sales.SalesDetail: price a counter sale of film by PANEL
   ---------------------------------------------------------------------

   Why
   ---
   A walk-in asking for tint on their car is buying a panel - "Full Wrap
   on a sedan" - not a length of film. The shop's price list is keyed
   Product x Tint Variant x Vehicle Classification x Panel, and until now
   the counter could not reach it: the sale form looked the price up from
   the VEHICLE on the sale, and a walk-in has no vehicle on file. So film
   at the counter had no price at all and had to be typed.

   The sale line now carries the class of car and the panel it was priced
   for. Both are needed, not one:

     * the PANEL says what was sold, and with the classification it gives
       the cut size - the centimetres that actually leave the roll, which
       is what the movement should take off stock rather than a flat metre;
     * the CLASSIFICATION cannot be read off the sale, because a walk-in
       has no vehicle. It is chosen per line and belongs to the line.

   What it does
   ------------
   Adds two NULLABLE columns with foreign keys, and an index on the panel.
   Nothing else is touched: no computed column, no constraint, no trigger.

     * Existing rows get NULL in both, which reads as "sold off the roll
       by the metre" - which is what every sale before today was.
     * Nullable, so a sale of a bolt or a step board is unaffected.

   Run it on EVERY database the app points at - the local one and the
   hosted one - BEFORE publishing the build that uses it. The app is
   database-first: it asks for these columns, and a database without them
   answers with "Invalid column name" on the sales screens.

   Safe to run twice: it checks before it adds.
   ===================================================================== */

USE ZEmpire;   -- the hosted copy is db_ad034e_zempire; change this line there
GO

/* ---- 1. the panel this line was priced for -------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'sales.SalesDetail') AND name = N'PanelID')
BEGIN
    ALTER TABLE sales.SalesDetail ADD PanelID INT NULL;
    PRINT 'Added sales.SalesDetail.PanelID';
END
ELSE
    PRINT 'sales.SalesDetail.PanelID already exists - left as it is';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_SalesDetail_Panel'
      AND parent_object_id = OBJECT_ID(N'sales.SalesDetail'))
BEGIN
    ALTER TABLE sales.SalesDetail
        ADD CONSTRAINT FK_SalesDetail_Panel
        FOREIGN KEY (PanelID) REFERENCES cat.Panel (PanelID);
    PRINT 'Added FK_SalesDetail_Panel';
END
ELSE
    PRINT 'FK_SalesDetail_Panel already exists - left as it is';
GO

/* ---- 2. the class of car it was priced for -------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'sales.SalesDetail') AND name = N'VehicleClassificationID')
BEGIN
    ALTER TABLE sales.SalesDetail ADD VehicleClassificationID INT NULL;
    PRINT 'Added sales.SalesDetail.VehicleClassificationID';
END
ELSE
    PRINT 'sales.SalesDetail.VehicleClassificationID already exists - left as it is';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_SalesDetail_VehicleClassification'
      AND parent_object_id = OBJECT_ID(N'sales.SalesDetail'))
BEGIN
    ALTER TABLE sales.SalesDetail
        ADD CONSTRAINT FK_SalesDetail_VehicleClassification
        FOREIGN KEY (VehicleClassificationID)
        REFERENCES crm.VehicleClassification (VehicleClassificationID);
    PRINT 'Added FK_SalesDetail_VehicleClassification';
END
ELSE
    PRINT 'FK_SalesDetail_VehicleClassification already exists - left as it is';
GO

/* ---- 3. an index on the panel -------------------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SalesDetail_PanelID'
      AND object_id = OBJECT_ID(N'sales.SalesDetail'))
BEGIN
    CREATE INDEX IX_SalesDetail_PanelID
        ON sales.SalesDetail (PanelID)
        WHERE PanelID IS NOT NULL;
    PRINT 'Added IX_SalesDetail_PanelID';
END
ELSE
    PRINT 'IX_SalesDetail_PanelID already exists - left as it is';
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
GO
