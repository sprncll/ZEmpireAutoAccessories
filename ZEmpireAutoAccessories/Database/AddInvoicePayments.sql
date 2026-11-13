/* ============================================================================
   Record each payment against a service invoice
   ----------------------------------------------------------------------------
   THIS CHANGES THE SCHEMA: it adds one table, sales.ServiceInvoicePayment.
   Nothing that already exists is altered.

   Why: sales.ServiceInvoice keeps one running AmountPaid, which says how much
   has come in but not when, by what, or who took it. So the takings could
   only be counted by whole invoice - counted once it reached Paid, in full,
   on the day it was raised. A P30,000 down payment taken today was missing
   from today's drawer, and turned up later inside a larger figure dated on
   another day.

   With this table each payment is a row of its own, dated when it was taken.
   Cashiering, the Collections report and the dashboard count these.

   Until this script has been run the app keeps counting the old way, so it
   is safe to pull the code first and run this when convenient. It takes
   effect within two minutes of running, without restarting the site.

   Run it once, against each database. Running it again does nothing: every
   step checks first.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

SELECT 'Database: ' + DB_NAME() + ' on ' + @@SERVERNAME AS [Running against];

BEGIN TRANSACTION;

/* ---------------------------------------------------------------------------
   1. The table.
   Amount is what the payment put in the drawer, net of change: positive for
   money in, negative for a refund or a reversal.
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'sales.ServiceInvoicePayment', N'U') IS NULL
BEGIN
    CREATE TABLE sales.ServiceInvoicePayment
    (
        PaymentID         INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ServiceInvoicePayment PRIMARY KEY,
        ServiceInvoiceID  INT            NOT NULL,
        PaymentDate       DATETIME2      NOT NULL,
        Amount            DECIMAL(18,2)  NOT NULL,
        AmountTendered    DECIMAL(18,2)  NULL,
        ChangeAmount      DECIMAL(18,2)  NOT NULL
            CONSTRAINT DF_ServiceInvoicePayment_Change DEFAULT (0),
        PaymentModeID     INT            NOT NULL,
        UserId            NVARCHAR(450)  NOT NULL,
        Kind              NVARCHAR(20)   NOT NULL
            CONSTRAINT DF_ServiceInvoicePayment_Kind DEFAULT (N'Payment'),
        Notes             NVARCHAR(200)  NULL,

        -- A deleted invoice takes its payments with it, as its lines do.
        CONSTRAINT FK_ServiceInvoicePayment_Invoice
            FOREIGN KEY (ServiceInvoiceID) REFERENCES sales.ServiceInvoice (ServiceInvoiceID)
            ON DELETE CASCADE,
        CONSTRAINT FK_ServiceInvoicePayment_PaymentMode
            FOREIGN KEY (PaymentModeID) REFERENCES sales.PaymentMode (PaymentModeID),

        CONSTRAINT CK_ServiceInvoicePayment_Kind
            CHECK (Kind IN (N'Payment', N'Refund', N'Reversal')),
        CONSTRAINT CK_ServiceInvoicePayment_Change
            CHECK (ChangeAmount >= 0),
        -- Money in is positive and money back is negative, never the other
        -- way round.
        CONSTRAINT CK_ServiceInvoicePayment_Sign
            CHECK ((Kind = N'Payment' AND Amount > 0) OR (Kind <> N'Payment' AND Amount < 0))
    );

    -- Cashiering and the reports read by date; an invoice's page reads by
    -- invoice.
    CREATE INDEX IX_ServiceInvoicePayment_Date
        ON sales.ServiceInvoicePayment (PaymentDate) INCLUDE (Amount, PaymentModeID, UserId);
    CREATE INDEX IX_ServiceInvoicePayment_Invoice
        ON sales.ServiceInvoicePayment (ServiceInvoiceID);

    PRINT 'Created sales.ServiceInvoicePayment.';
END
ELSE
    PRINT 'sales.ServiceInvoicePayment already exists - left as it is.';

/* ---------------------------------------------------------------------------
   2. The payments already taken, so the history is not blank.
   Only invoices with no payment rows yet, so this never doubles anything.

   There is no record of WHEN an existing invoice was paid, so each is dated
   on its invoice date - which is exactly where the old reports counted it,
   so no past total moves:

     Paid                        one payment of the total
     Pending, part paid          the deposit taken so far
     Pending with AmountPaid at
       or above the total        nothing: that is the old database rule
                                 forcing the column, not money received
     Cancelled or Refunded       nothing: the old reports never counted these
   --------------------------------------------------------------------------- */
DECLARE @added INT = 0;

INSERT INTO sales.ServiceInvoicePayment
    (ServiceInvoiceID, PaymentDate, Amount, AmountTendered, ChangeAmount,
     PaymentModeID, UserId, Kind, Notes)
SELECT i.ServiceInvoiceID,
       i.InvoiceDate,
       CASE WHEN i.Status = N'Paid' THEN i.TotalAmount ELSE i.AmountPaid END,
       CASE WHEN i.Status = N'Paid' THEN i.TotalAmount + i.ChangeAmount ELSE i.AmountPaid END,
       CASE WHEN i.Status = N'Paid' THEN i.ChangeAmount ELSE 0 END,
       i.PaymentModeID,
       i.UserId,
       N'Payment',
       CASE WHEN i.Status = N'Paid' THEN N'Recorded before payments were tracked'
            ELSE N'Down payment (recorded before payments were tracked)' END
FROM sales.ServiceInvoice i
WHERE NOT EXISTS (SELECT 1 FROM sales.ServiceInvoicePayment p
                  WHERE p.ServiceInvoiceID = i.ServiceInvoiceID)
  AND (   (i.Status = N'Paid' AND i.TotalAmount > 0)
       OR (i.Status = N'Pending' AND i.AmountPaid > 0 AND i.AmountPaid < i.TotalAmount));

SET @added = @@ROWCOUNT;
PRINT CONCAT('Recorded ', @added, ' existing payment(s).');

COMMIT TRANSACTION;

/* ---------------------------------------------------------------------------
   3. A check: for every invoice, what the ledger says was paid beside what
   the invoice says. Expect no rows.
   --------------------------------------------------------------------------- */
SELECT 'Invoices whose payments do not add up (expect none)' AS [Check];

SELECT i.InvoiceNumber, i.Status, i.TotalAmount, i.AmountPaid, i.ChangeAmount,
       SUM(p.Amount) AS LedgerNet
FROM sales.ServiceInvoice i
JOIN sales.ServiceInvoicePayment p ON p.ServiceInvoiceID = i.ServiceInvoiceID
WHERE i.Status IN (N'Paid', N'Pending')
GROUP BY i.InvoiceNumber, i.Status, i.TotalAmount, i.AmountPaid, i.ChangeAmount
HAVING SUM(p.Amount) <> CASE WHEN i.Status = N'Paid' THEN i.TotalAmount ELSE i.AmountPaid END;
