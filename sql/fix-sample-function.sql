DROP FUNCTION IF EXISTS get_sample_employees(TEXT);
CREATE OR REPLACE FUNCTION get_sample_employees(dept TEXT DEFAULT NULL)
RETURNS TABLE (id INT, name TEXT, department TEXT) AS $$
    SELECT e.id, e.name, e.department
    FROM sample_employees e
    WHERE (dept IS NULL OR e.department = dept);
$$ LANGUAGE SQL STABLE;

UPDATE "ApiParameters" SET "Name"='dept' WHERE "Id"=9001;
UPDATE "ApiDefinitions" SET "CommandText"='SELECT * FROM get_sample_employees(@dept)' WHERE "Id"=9001;