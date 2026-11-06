/* ============================================================================
   Customer contact numbers: 09xxxxxxxxx  ->  +639xxxxxxxxx
   ----------------------------------------------------------------------------
   The application now stores one shape, "+63" then ten digits beginning 9.
   Rows entered before that hold the dialled form instead. Either is readable,
   and a row converts itself the next time someone edits that customer - this
   does them all at once.

   Run section 1 and look at it. Only then run section 2.
   ========================================================================== */

SET NOCOUNT ON;

/* ---------------------------------------------------------------------------
   1. What would change. The pattern is 09 followed by exactly nine more
   digits, so anything shorter, longer or differently shaped is left for you
   to look at rather than rewritten.
   --------------------------------------------------------------------------- */
SELECT '1. Rows that would be converted' AS Section;

SELECT
    CustomerID,
    FullName,
    ContactNumber,
    WouldBecome = N'+63' + RIGHT(ContactNumber, 10)
FROM crm.Customer
WHERE LEN(ContactNumber) = 11
  AND ContactNumber LIKE N'09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
ORDER BY FullName;


SELECT '2. Rows this script will NOT touch - check these by hand' AS Section;

SELECT
    CustomerID,
    FullName,
    ContactNumber,
    Why = CASE
        WHEN ContactNumber IS NULL OR LTRIM(RTRIM(ContactNumber)) = N'' THEN N'no number on file'
        WHEN ContactNumber LIKE N'+63[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THEN N'already converted'
        ELSE N'not a plain 11-digit 09 number'
    END
FROM crm.Customer
WHERE NOT (LEN(ContactNumber) = 11
           AND ContactNumber LIKE N'09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
ORDER BY FullName;


/* ---------------------------------------------------------------------------
   3. The conversion. Uncomment and run once section 1 reads correctly.
   --------------------------------------------------------------------------- */
-- UPDATE crm.Customer
-- SET ContactNumber = N'+63' + RIGHT(ContactNumber, 10)
-- WHERE LEN(ContactNumber) = 11
--   AND ContactNumber LIKE N'09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';
