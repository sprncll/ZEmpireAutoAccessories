/* ============================================================================
   Allow a down payment on a service invoice
   ----------------------------------------------------------------------------
   THIS CHANGES THE SCHEMA. It is the only revision on the panel's list that
   cannot be made without doing so, and it was approved deliberately.

   Today sales.ServiceInvoice carries two constraints that, together, forbid a
   part payment outright:

       CK_ServiceInvoice_Math     ChangeAmount = AmountPaid - TotalAmount
       CK_ServiceInvoice_Change   ChangeAmount >= 0

   Substituting the first into the second gives AmountPaid >= TotalAmount. A
   customer leaving a deposit cannot be recorded at all - the row is refused
   by the database, not by the application.

   This keeps both rules where they are right and relaxes them where they are
   not: change is still never negative, and is still exactly the overpayment,
   but an invoice may now hold less than its total, which is a deposit.

       ChangeAmount = AmountPaid - TotalAmount   when AmountPaid > TotalAmount
       ChangeAmount = 0                          otherwise

   Run it once, against each database. It changes no rows: every existing
   invoice has AmountPaid >= TotalAmount and ChangeAmount = the difference, so
   all of them satisfy the new rule already.

   To undo it, section 3 puts the originals back - which succeeds only if no
   deposit has been taken in the meantime.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------------------------------------------------------------------------
   1. What is there now, and whether any row would fail the new rule.
   Nothing is changed by this section.
   --------------------------------------------------------------------------- */
SELECT '1. Constraints on sales.ServiceInvoice' AS Section;

SELECT c.name AS ConstraintName, c.definition
FROM sys.check_constraints c
WHERE c.parent_object_id = OBJECT_ID(N'sales.ServiceInvoice')
ORDER BY c.name;

SELECT '2. Rows that would not satisfy the new rule (expect none)' AS Section;

SELECT ServiceInvoiceID, InvoiceNumber, TotalAmount, AmountPaid, ChangeAmount
FROM sales.ServiceInvoice
WHERE ChangeAmount < 0
   OR ChangeAmount <> CASE WHEN AmountPaid > TotalAmount
                           THEN AmountPaid - TotalAmount ELSE 0 END;


/* ---------------------------------------------------------------------------
   3. The change. Both constraints are replaced inside one transaction, so a
   failure leaves the table exactly as it was rather than half-guarded.
   --------------------------------------------------------------------------- */
BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_ServiceInvoice_Math'
                 AND parent_object_id = OBJECT_ID(N'sales.ServiceInvoice'))
        ALTER TABLE sales.ServiceInvoice DROP CONSTRAINT CK_ServiceInvoice_Math;

    IF EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_ServiceInvoice_Change'
                 AND parent_object_id = OBJECT_ID(N'sales.ServiceInvoice'))
        ALTER TABLE sales.ServiceInvoice DROP CONSTRAINT CK_ServiceInvoice_Change;

    -- Change is never negative: the shop does not hand back money it was not
    -- given.
    ALTER TABLE sales.ServiceInvoice WITH CHECK
        ADD CONSTRAINT CK_ServiceInvoice_Change CHECK (ChangeAmount >= 0);

    -- Change is the overpayment and nothing else. Below the total there is no
    -- change to give, there is a balance still owed.
    ALTER TABLE sales.ServiceInvoice WITH CHECK
        ADD CONSTRAINT CK_ServiceInvoice_Math CHECK
            (ChangeAmount = CASE WHEN AmountPaid > TotalAmount
                                 THEN AmountPaid - TotalAmount
                                 ELSE 0 END);

COMMIT TRANSACTION;

SELECT '3. Done - constraints now' AS Section;

SELECT c.name AS ConstraintName, c.definition
FROM sys.check_constraints c
WHERE c.parent_object_id = OBJECT_ID(N'sales.ServiceInvoice')
ORDER BY c.name;


/* ---------------------------------------------------------------------------
   4. To undo. Fails if any invoice holds a deposit, which is the point: the
   old rule cannot describe one, so it cannot be put back while one exists.
   --------------------------------------------------------------------------- */
-- BEGIN TRANSACTION;
--     ALTER TABLE sales.ServiceInvoice DROP CONSTRAINT CK_ServiceInvoice_Math;
--     ALTER TABLE sales.ServiceInvoice DROP CONSTRAINT CK_ServiceInvoice_Change;
--     ALTER TABLE sales.ServiceInvoice WITH CHECK
--         ADD CONSTRAINT CK_ServiceInvoice_Change CHECK (ChangeAmount >= 0);
--     ALTER TABLE sales.ServiceInvoice WITH CHECK
--         ADD CONSTRAINT CK_ServiceInvoice_Math
--             CHECK (ChangeAmount = AmountPaid - TotalAmount);
-- COMMIT TRANSACTION;
