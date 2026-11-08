/* ============================================================================
   crm.Customer - one mobile number, more than one customer
   ----------------------------------------------------------------------------
   A mobile number belongs to one person, and it is what tells two customers
   of the same name apart. Where two rows carry the same number it stops
   telling anybody anything, and a job order can be raised against the wrong
   one without anybody noticing.

   The screens now refuse a number already on file. That stops new ones; it
   does not find the ones already there, which is what this is for. It also
   does not stop a row being created any other way, because the column has no
   unique index - the rule lives in the application, not the database.

   Read-only. Nothing here changes a row.
   ========================================================================== */

SET NOCOUNT ON;

/* ---------------------------------------------------------------------------
   1. The same number on more than one customer, counting the two shapes as
   one: rows entered before the move to +63 still hold 09121231231, which is
   the same number as +639121231231.
   --------------------------------------------------------------------------- */
SELECT '1. One number, several customers' AS Section;

WITH Normalised AS (
    SELECT
        CustomerID,
        FullName,
        ContactNumber,
        CreatedAt,
        -- The last ten digits are the number itself, whichever way it is
        -- written. Anything shorter is not a mobile number and is left out.
        Digits = RIGHT(ContactNumber, 10)
    FROM crm.Customer
    WHERE ContactNumber IS NOT NULL
      AND LEN(ContactNumber) >= 10
)
SELECT
    n.Digits,
    Customers = COUNT(*) OVER (PARTITION BY n.Digits),
    n.CustomerID,
    n.FullName,
    n.ContactNumber,
    n.CreatedAt,
    Vehicles = (SELECT COUNT(*) FROM crm.Vehicle v WHERE v.CustomerID = n.CustomerID)
FROM Normalised n
WHERE EXISTS (
    SELECT 1 FROM Normalised other
    WHERE other.Digits = n.Digits AND other.CustomerID <> n.CustomerID)
ORDER BY n.Digits, n.CreatedAt;


/* ---------------------------------------------------------------------------
   2. Customers sharing a name. Not a fault - two people really can both be
   Juan Dela Cruz - but worth reading beside section 1, because a shared name
   AND a shared number is one person entered twice, while a shared name and
   two numbers is two people.
   --------------------------------------------------------------------------- */
SELECT '2. Customers sharing a name' AS Section;

SELECT
    c.FullName,
    Customers = COUNT(*) OVER (PARTITION BY c.FullName),
    c.CustomerID,
    c.ContactNumber,
    Vehicles = (SELECT COUNT(*) FROM crm.Vehicle v WHERE v.CustomerID = c.CustomerID),
    Verdict = CASE
        WHEN COUNT(*) OVER (PARTITION BY c.FullName, RIGHT(c.ContactNumber, 10)) > 1
            THEN N'same name AND same number - one person entered twice'
        ELSE N'same name, different number - two people, which is fine'
    END
FROM crm.Customer c
WHERE EXISTS (
    SELECT 1 FROM crm.Customer o
    WHERE o.FullName = c.FullName AND o.CustomerID <> c.CustomerID)
ORDER BY c.FullName, c.CustomerID;


/* ---------------------------------------------------------------------------
   3. Numbers that are not a valid Philippine mobile at all. These predate the
   rule and will be refused the next time that customer is edited, so they are
   worth correcting before somebody meets the error with a customer in front
   of them.
   --------------------------------------------------------------------------- */
SELECT '3. Numbers the rule would now refuse' AS Section;

SELECT
    CustomerID,
    FullName,
    ContactNumber,
    Why = CASE
        WHEN ContactNumber IS NULL OR LTRIM(RTRIM(ContactNumber)) = N'' THEN N'no number on file'
        WHEN ContactNumber LIKE N'+63[9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THEN N'valid'
        WHEN ContactNumber LIKE N'09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THEN N'valid, written the old way'
        ELSE N'not a Philippine mobile number'
    END
FROM crm.Customer
WHERE ContactNumber IS NULL
   OR NOT (ContactNumber LIKE N'+63[9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
           OR ContactNumber LIKE N'09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
ORDER BY FullName;
