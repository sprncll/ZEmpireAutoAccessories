/* ============================================================================
   Keep a record of who changed what
   ----------------------------------------------------------------------------
   THIS CHANGES THE SCHEMA: it adds one table, sec.ActivityLog. Nothing that
   already exists is altered.

   Why: nothing recorded who added, changed or deleted a record, or when. A
   price changed, an invoice marked Refunded, a customer deleted - the system
   could say what is true now but not how it got that way or who did it.

   Once this table exists the app writes a row for every record it adds,
   changes or deletes - who, when, which record, and each field's old and new
   value - plus receipts attached or removed and sign-ins. Password hashes,
   security stamps and tokens are never written. Admins read it under
   Administration -> Activity Log.

   Until this script has been run the app simply does not log; nothing else
   changes. It takes effect within two minutes of running, without restarting
   the site.

   Run it once, against each database. Running it again does nothing.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

SELECT 'Database: ' + DB_NAME() + ' on ' + @@SERVERNAME AS [Running against];

IF OBJECT_ID(N'sec.ActivityLog', N'U') IS NULL
BEGIN
    CREATE TABLE sec.ActivityLog
    (
        ActivityID  BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ActivityLog PRIMARY KEY,
        [At]        DATETIME2       NOT NULL,
        -- No foreign key to the users table on purpose: the history of what
        -- an account did must outlive the account. UserName keeps the name.
        UserId      NVARCHAR(450)   NULL,
        UserName    NVARCHAR(256)   NULL,
        [Action]    NVARCHAR(20)    NOT NULL,
        Entity      NVARCHAR(100)   NOT NULL,
        EntityKey   NVARCHAR(100)   NULL,
        -- The document this record is part of: a payment's invoice, a line's
        -- job order, a vehicle's customer. A document's history includes them.
        Parent      NVARCHAR(100)   NULL,
        ParentKey   NVARCHAR(100)   NULL,
        Summary     NVARCHAR(400)   NOT NULL,
        Changes     NVARCHAR(MAX)   NULL,

        CONSTRAINT CK_ActivityLog_Action
            CHECK ([Action] IN (N'Added', N'Changed', N'Deleted', N'Note'))
    );

    -- The screen reads newest first, and filters by record and by person.
    CREATE INDEX IX_ActivityLog_At ON sec.ActivityLog ([At] DESC);
    CREATE INDEX IX_ActivityLog_Record ON sec.ActivityLog (Entity, EntityKey, [At] DESC);
    CREATE INDEX IX_ActivityLog_User ON sec.ActivityLog (UserId, [At] DESC);
    CREATE INDEX IX_ActivityLog_Parent ON sec.ActivityLog (Parent, ParentKey, [At] DESC);

    PRINT 'Created sec.ActivityLog.';
END
ELSE
    PRINT 'sec.ActivityLog already exists - left as it is.';
