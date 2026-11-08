/* ============================================================================
   cat.Panel - two rows naming the same piece of glass
   ----------------------------------------------------------------------------
   The price scripts create: Front WS, 2FWS, 2RWS, Qtr Window, Rear WS,
   Full Wrap, Whole Vehicle, Sunroof. Anything else was added by hand, and
   some of it names glass that already had a row - "Front Windshield" beside
   "Front WS".

   On a price list that is untidy. On a checklist it is the same windscreen
   asked about twice, and the condition can be recorded against whichever row
   the person happened to pick - so a job order priced on one and a walkaround
   recorded on the other no longer line up.

   Sections 1-3 only read. Section 4 is the merge and is commented out.
   ========================================================================== */

SET NOCOUNT ON;

/* The alias groups the application knows, matching Models/WalkaroundPanels.cs.
   The first name in each group is the one the price scripts create and that
   everything else is keyed on, so it is the row to keep. */
DECLARE @Alias TABLE (GroupName nvarchar(40), PanelName nvarchar(80), IsCanonical bit);

INSERT INTO @Alias (GroupName, PanelName, IsCanonical) VALUES
    (N'Front windscreen', N'Front WS',         1),
    (N'Front windscreen', N'Front Windshield', 0),
    (N'Front windscreen', N'Windshield',       0),
    (N'Front windows',    N'2FWS',             1),
    (N'Front windows',    N'2 FWS',            0),
    (N'Front windows',    N'Front Windows',    0),
    (N'Front windows',    N'1st Row',          0),
    (N'Rear windows',     N'2RWS',             1),
    (N'Rear windows',     N'2 RWS',            0),
    (N'Rear windows',     N'Rear Windows',     0),
    (N'Rear windows',     N'2nd Row',          0),
    (N'Quarter glass',    N'Qtr Window',       1),
    (N'Quarter glass',    N'Quarter Window',   0),
    (N'Quarter glass',    N'Qtr Glass',        0),
    (N'Quarter glass',    N'Quarter Glass',    0),
    (N'Rear windscreen',  N'Rear WS',          1),
    (N'Rear windscreen',  N'Rear Windshield',  0),
    (N'Roof',             N'Sunroof',          1),
    (N'Roof',             N'Moonroof',         0);


/* ---------------------------------------------------------------------------
   1. The duplicates, and what each row carries. A row with nothing attached
   can simply be deleted; one with prices or recorded conditions has to have
   them moved first.
   --------------------------------------------------------------------------- */
SELECT '1. Rows naming the same glass' AS Section;

SELECT
    a.GroupName,
    p.PanelID,
    p.PanelName,
    Keep           = CASE WHEN a.IsCanonical = 1 THEN N'<= keep this one' ELSE N'' END,
    PriceRows      = (SELECT COUNT(*) FROM cat.Pricing pr WHERE pr.PanelID = p.PanelID),
    ChecklistRows  = (SELECT COUNT(*) FROM ops.VehicleChecklistDetail d WHERE d.PanelID = p.PanelID),
    JobOrderLines  = (SELECT COUNT(*) FROM ops.JobOrderDetail j WHERE j.PanelID = p.PanelID),
    InvoiceLines   = (SELECT COUNT(*) FROM sales.ServiceInvoiceDetail s WHERE s.PanelID = p.PanelID),
    QuotationLines = (SELECT COUNT(*) FROM sales.QuotationDetail q WHERE q.PanelID = p.PanelID)
FROM cat.Panel p
JOIN @Alias a ON a.PanelName = p.PanelName
WHERE a.GroupName IN (
    SELECT a2.GroupName
    FROM cat.Panel p2
    JOIN @Alias a2 ON a2.PanelName = p2.PanelName
    GROUP BY a2.GroupName
    HAVING COUNT(*) > 1)
ORDER BY a.GroupName, a.IsCanonical DESC, p.PanelID;


/* ---------------------------------------------------------------------------
   2. Panels the application has never heard of. Not necessarily wrong - the
   shop may have added Front Doors and Sidings on purpose - but worth reading,
   because a new name that means existing glass belongs in the list above
   rather than beside it.
   --------------------------------------------------------------------------- */
SELECT '2. Panels not in any alias group' AS Section;

SELECT
    p.PanelID,
    p.PanelName,
    PriceRows     = (SELECT COUNT(*) FROM cat.Pricing pr WHERE pr.PanelID = p.PanelID),
    ChecklistRows = (SELECT COUNT(*) FROM ops.VehicleChecklistDetail d WHERE d.PanelID = p.PanelID),
    Note = CASE
        WHEN NOT EXISTS (SELECT 1 FROM cat.Pricing pr WHERE pr.PanelID = p.PanelID)
            THEN N'no price, so it never appears on a job order'
        ELSE N'priced, so it is a real panel'
    END
FROM cat.Panel p
WHERE p.PanelName NOT IN (SELECT PanelName FROM @Alias)
  AND p.PanelName NOT IN (N'Full Wrap', N'Whole Vehicle')
ORDER BY p.PanelName;


/* ---------------------------------------------------------------------------
   3. What the merge below would do, before it does it.
   --------------------------------------------------------------------------- */
SELECT '3. What a merge would move' AS Section;

SELECT
    a.GroupName,
    FromPanel = dup.PanelName, FromID = dup.PanelID,
    ToPanel   = keep.PanelName, ToID  = keep.PanelID,
    MovePrices     = (SELECT COUNT(*) FROM cat.Pricing pr WHERE pr.PanelID = dup.PanelID),
    MoveChecklists = (SELECT COUNT(*) FROM ops.VehicleChecklistDetail d WHERE d.PanelID = dup.PanelID),
    MoveJobLines   = (SELECT COUNT(*) FROM ops.JobOrderDetail j WHERE j.PanelID = dup.PanelID),
    MoveInvLines   = (SELECT COUNT(*) FROM sales.ServiceInvoiceDetail s WHERE s.PanelID = dup.PanelID),
    MoveQuoteLines = (SELECT COUNT(*) FROM sales.QuotationDetail q WHERE q.PanelID = dup.PanelID)
FROM cat.Panel dup
JOIN @Alias a ON a.PanelName = dup.PanelName AND a.IsCanonical = 0
JOIN @Alias ak ON ak.GroupName = a.GroupName AND ak.IsCanonical = 1
JOIN cat.Panel keep ON keep.PanelName = ak.PanelName
ORDER BY a.GroupName;


/* ---------------------------------------------------------------------------
   4. The merge. Read sections 1 and 3 first, then uncomment and run.

   Each statement moves one kind of row off the duplicate and onto the panel
   that is kept. The duplicate is deleted last, and only where nothing points
   at it any more - so if a table has been added since this was written, the
   delete fails rather than orphaning it.

   A price row is NOT moved: the kept panel may already be priced for the same
   product, variant and class, which would be two prices for one combination.
   Those are listed instead and deleted by hand after you have looked at them.
   --------------------------------------------------------------------------- */

-- ;WITH Merge AS (
--     SELECT dup.PanelID AS FromID, keep.PanelID AS ToID
--     FROM cat.Panel dup
--     JOIN @Alias a  ON a.PanelName = dup.PanelName AND a.IsCanonical = 0
--     JOIN @Alias ak ON ak.GroupName = a.GroupName AND ak.IsCanonical = 1
--     JOIN cat.Panel keep ON keep.PanelName = ak.PanelName
-- )
-- UPDATE d SET d.PanelID = m.ToID
-- FROM ops.VehicleChecklistDetail d JOIN Merge m ON m.FromID = d.PanelID
-- WHERE NOT EXISTS (
--     -- One checklist cannot hold the same panel twice; where both twins were
--     -- answered on one car, the duplicate's line is left for you to read.
--     SELECT 1 FROM ops.VehicleChecklistDetail keep
--     WHERE keep.ChecklistID = d.ChecklistID AND keep.PanelID = m.ToID);

-- (repeat for ops.JobOrderDetail, sales.ServiceInvoiceDetail,
--  sales.QuotationDetail - those carry no uniqueness, so no NOT EXISTS)

-- DELETE p
-- FROM cat.Panel p
-- JOIN @Alias a ON a.PanelName = p.PanelName AND a.IsCanonical = 0
-- WHERE NOT EXISTS (SELECT 1 FROM cat.Pricing              x WHERE x.PanelID = p.PanelID)
--   AND NOT EXISTS (SELECT 1 FROM ops.VehicleChecklistDetail x WHERE x.PanelID = p.PanelID)
--   AND NOT EXISTS (SELECT 1 FROM ops.JobOrderDetail         x WHERE x.PanelID = p.PanelID)
--   AND NOT EXISTS (SELECT 1 FROM sales.ServiceInvoiceDetail x WHERE x.PanelID = p.PanelID)
--   AND NOT EXISTS (SELECT 1 FROM sales.QuotationDetail      x WHERE x.PanelID = p.PanelID);
