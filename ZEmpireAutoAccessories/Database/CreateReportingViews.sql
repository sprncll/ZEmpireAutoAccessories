/* =====================================================================
   The five dbo reporting views the Reports screens read
   ---------------------------------------------------------------------

   Reports is the only part of the app that reads these; everything else
   goes to the tables. A missing view therefore breaks exactly one screen
   and nothing else, which is why it can sit missing for a long time
   before anybody notices - and when they do, the error names a COLUMN
   ("Invalid column name 'QuotationID'") rather than the view, because
   that is the first thing the query asked for.

   ONLY CREATES WHAT IS MISSING. A view that is already there is left
   exactly as it is - its definition is the shop's, and quietly replacing
   it could change what a report totals. Run it, read what it printed,
   and run it again as often as you like.

   Nothing here touches a table or a row. Views are a saved SELECT.
   ===================================================================== */

USE ZEmpire;   -- the hosted copy is db_ad034e_zempire; change this line there
GO

/* ---- which database is this, and is it the app's? ------------------ */
--
-- Printed first because the usual reason a script "does not work" is that
-- it worked perfectly on a different database. The app connects to the
-- server and database in appsettings.json - Server=.\SQLEXPRESS,
-- Database=ZEmpire - and SSMS is often pointed somewhere else entirely.
SELECT
    @@SERVERNAME  AS ServerName,
    DB_NAME()     AS DatabaseName,
    CASE WHEN OBJECT_ID(N'sales.Quotation') IS NULL
         THEN 'NO - sales.Quotation is not here, so this is not the app''s database'
         ELSE 'yes - sales.Quotation is here' END AS LooksLikeTheAppsDatabase;
GO

/* ---- stock on hand, per product ------------------------------------ */
IF OBJECT_ID(N'dbo.vw_StockOnHand', N'V') IS NULL
BEGIN
    EXEC(N'
    CREATE VIEW dbo.vw_StockOnHand AS
    SELECT
        p.ProductID,
        p.ProductName,
        SUM(CASE WHEN t.TransactionType = ''IN'' THEN t.Quantity
                 WHEN t.TransactionType = ''OUT'' THEN -t.Quantity
                 ELSE 0 END) AS StockOnHand
    FROM cat.Product p
    LEFT JOIN inv.InventoryTransaction t ON t.ProductID = p.ProductID
    GROUP BY p.ProductID, p.ProductName;');

    -- Asked of the catalogue rather than assumed: a CREATE VIEW that
    -- fails inside EXEC - because a table it names is not in THIS
    -- database - does not stop the batch, and this used to print
    -- "Created" for a view that was never made.
    IF OBJECT_ID(N'dbo.vw_StockOnHand', N'V') IS NULL
        PRINT '*** dbo.vw_StockOnHand was NOT created - see the error above';
    ELSE
        PRINT 'Created dbo.vw_StockOnHand';
END
ELSE PRINT 'dbo.vw_StockOnHand already exists - left as it is';
GO

/* ---- one row per counter sale -------------------------------------- */
IF OBJECT_ID(N'dbo.vw_SalesSummary', N'V') IS NULL
BEGIN
    EXEC(N'
    CREATE VIEW dbo.vw_SalesSummary AS
    SELECT
        s.SalesID,
        s.InvoiceNumber,
        s.SalesDate,
        c.FullName        AS CustomerName,
        pm.PaymentModeName,
        u.FullName        AS SoldBy,
        u.UserName,
        s.TotalAmount     AS RecordedTotal,
        ISNULL((SELECT SUM(d.Quantity * d.UnitPrice)
                FROM sales.SalesDetail d
                WHERE d.SalesID = s.SalesID), 0) AS ComputedTotal
    FROM sales.Sales s
    LEFT JOIN crm.Customer c      ON c.CustomerID = s.CustomerID
    LEFT JOIN sales.PaymentMode pm ON pm.PaymentModeID = s.PaymentModeID
    LEFT JOIN asp.AspNetUsers u   ON u.Id = s.UserId;');

    -- Asked of the catalogue rather than assumed: a CREATE VIEW that
    -- fails inside EXEC - because a table it names is not in THIS
    -- database - does not stop the batch, and this used to print
    -- "Created" for a view that was never made.
    IF OBJECT_ID(N'dbo.vw_SalesSummary', N'V') IS NULL
        PRINT '*** dbo.vw_SalesSummary was NOT created - see the error above';
    ELSE
        PRINT 'Created dbo.vw_SalesSummary';
END
ELSE PRINT 'dbo.vw_SalesSummary already exists - left as it is';
GO

/* ---- one row per job order ----------------------------------------- */
IF OBJECT_ID(N'dbo.vw_JobOrderSummary', N'V') IS NULL
BEGIN
    EXEC(N'
    CREATE VIEW dbo.vw_JobOrderSummary AS
    SELECT
        j.JobOrderID,
        j.JobOrderNumber,
        j.JobOrderDate,
        j.Status,
        jt.JobTypeName,
        c.FullName  AS CustomerName,
        v.PlateNumber,
        u.FullName  AS HandledBy,
        u.UserName,
        e.FullName  AS Technician,
        ISNULL((SELECT COUNT(*)
                FROM ops.JobOrderDetail d
                WHERE d.JobOrderID = j.JobOrderID), 0) AS LineCount,
        ISNULL((SELECT SUM(d.Quantity * d.UnitPrice)
                FROM ops.JobOrderDetail d
                WHERE d.JobOrderID = j.JobOrderID), 0) AS TotalAmount
    FROM ops.JobOrder j
    LEFT JOIN cat.JobType jt    ON jt.JobTypeID = j.JobTypeID
    LEFT JOIN crm.Vehicle v     ON v.VehicleID = j.VehicleID
    LEFT JOIN crm.Customer c    ON c.CustomerID = j.CustomerID
    LEFT JOIN asp.AspNetUsers u ON u.Id = j.UserId
    LEFT JOIN sec.Employee e    ON e.EmployeeID = j.AssignedEmployeeID;');

    -- Asked of the catalogue rather than assumed: a CREATE VIEW that
    -- fails inside EXEC - because a table it names is not in THIS
    -- database - does not stop the batch, and this used to print
    -- "Created" for a view that was never made.
    IF OBJECT_ID(N'dbo.vw_JobOrderSummary', N'V') IS NULL
        PRINT '*** dbo.vw_JobOrderSummary was NOT created - see the error above';
    ELSE
        PRINT 'Created dbo.vw_JobOrderSummary';
END
ELSE PRINT 'dbo.vw_JobOrderSummary already exists - left as it is';
GO

/* ---- one row per service invoice ----------------------------------- */
IF OBJECT_ID(N'dbo.vw_ServiceInvoiceSummary', N'V') IS NULL
BEGIN
    EXEC(N'
    CREATE VIEW dbo.vw_ServiceInvoiceSummary AS
    SELECT
        i.ServiceInvoiceID,
        i.InvoiceNumber,
        i.InvoiceDate,
        i.Status,
        i.JobOrderID,
        j.JobOrderNumber,
        c.FullName AS CustomerName,
        pm.PaymentModeName,
        u.FullName AS ProcessedBy,
        u.UserName,
        i.SubTotal,
        i.DiscountAmount,
        i.TaxAmount,
        i.TotalAmount,
        i.AmountPaid,
        i.ChangeAmount,
        ISNULL((SELECT COUNT(*)
                FROM sales.ServiceInvoiceDetail d
                WHERE d.ServiceInvoiceID = i.ServiceInvoiceID), 0) AS LineCount,
        ISNULL((SELECT SUM(d.Quantity * d.UnitPrice - ISNULL(d.DiscountAmount, 0))
                FROM sales.ServiceInvoiceDetail d
                WHERE d.ServiceInvoiceID = i.ServiceInvoiceID), 0) AS ComputedLineTotal
    FROM sales.ServiceInvoice i
    LEFT JOIN ops.JobOrder j       ON j.JobOrderID = i.JobOrderID
    LEFT JOIN crm.Customer c       ON c.CustomerID = i.CustomerID
    LEFT JOIN sales.PaymentMode pm ON pm.PaymentModeID = i.PaymentModeID
    LEFT JOIN asp.AspNetUsers u    ON u.Id = i.UserId;');

    -- Asked of the catalogue rather than assumed: a CREATE VIEW that
    -- fails inside EXEC - because a table it names is not in THIS
    -- database - does not stop the batch, and this used to print
    -- "Created" for a view that was never made.
    IF OBJECT_ID(N'dbo.vw_ServiceInvoiceSummary', N'V') IS NULL
        PRINT '*** dbo.vw_ServiceInvoiceSummary was NOT created - see the error above';
    ELSE
        PRINT 'Created dbo.vw_ServiceInvoiceSummary';
END
ELSE PRINT 'dbo.vw_ServiceInvoiceSummary already exists - left as it is';
GO

/* ---- one row per quotation ----------------------------------------- */
IF OBJECT_ID(N'dbo.vw_QuotationSummary', N'V') IS NULL
BEGIN
    EXEC(N'
    CREATE VIEW dbo.vw_QuotationSummary AS
    SELECT
        q.QuotationID,
        q.QuotationNumber,
        q.QuotationDate,
        q.ValidUntil,
        q.Status,
        jt.JobTypeName,
        c.FullName AS CustomerName,
        v.PlateNumber,
        u.FullName AS PreparedBy,
        u.UserName,
        j.JobOrderID     AS ConvertedJobOrderID,
        j.JobOrderNumber AS ConvertedJobOrderNumber,
        q.SubTotal,
        q.DiscountAmount,
        q.TaxAmount,
        q.TotalAmount,
        ISNULL((SELECT COUNT(*)
                FROM sales.QuotationDetail d
                WHERE d.QuotationID = q.QuotationID), 0) AS LineCount,
        ISNULL((SELECT SUM(d.Quantity * d.UnitPrice)
                FROM sales.QuotationDetail d
                WHERE d.QuotationID = q.QuotationID), 0) AS ComputedLineTotal
    FROM sales.Quotation q
    LEFT JOIN cat.JobType jt    ON jt.JobTypeID = q.JobTypeID
    LEFT JOIN crm.Vehicle v     ON v.VehicleID = q.VehicleID
    LEFT JOIN crm.Customer c    ON c.CustomerID = q.CustomerID
    LEFT JOIN asp.AspNetUsers u ON u.Id = q.UserId
    LEFT JOIN ops.JobOrder j    ON j.QuotationID = q.QuotationID;');

    -- Asked of the catalogue rather than assumed: a CREATE VIEW that
    -- fails inside EXEC - because a table it names is not in THIS
    -- database - does not stop the batch, and this used to print
    -- "Created" for a view that was never made.
    IF OBJECT_ID(N'dbo.vw_QuotationSummary', N'V') IS NULL
        PRINT '*** dbo.vw_QuotationSummary was NOT created - see the error above';
    ELSE
        PRINT 'Created dbo.vw_QuotationSummary';
END
ELSE PRINT 'dbo.vw_QuotationSummary already exists - left as it is';
GO

/* ---- what is there now --------------------------------------------- */
SELECT name AS ViewName, create_date, modify_date
FROM sys.views
WHERE schema_id = SCHEMA_ID(N'dbo')
  AND name IN (N'vw_StockOnHand', N'vw_SalesSummary', N'vw_JobOrderSummary',
               N'vw_ServiceInvoiceSummary', N'vw_QuotationSummary')
ORDER BY name;
GO
