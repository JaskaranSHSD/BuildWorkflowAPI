-- Sample API + stored procedure/function using the SAME PostgreSQL database
-- that hosts the BuildWorkflowAPI application metadata.
--
-- After running this script:
--   1. The app database gets a sample_employees table and get_sample_employees() function.
--   2. The app metadata tables get a sample connection/API/parameter/key.
--
-- Login (admin / Password123!) may be required to use the management UI.
-- Invoke the API:
--   POST http://localhost:5268/api/app-sample-employees
--   X-API-Key: sample-app-api-key
--   Content-Type: application/json
--   { "department": "Engineering" }

-- Sample data table + "stored procedure" (SQL function)
CREATE TABLE IF NOT EXISTS sample_employees (
    id INT PRIMARY KEY,
    name TEXT NOT NULL,
    department TEXT NOT NULL
);

INSERT INTO sample_employees (id, name, department) VALUES
    (1, 'Alice', 'Engineering'),
    (2, 'Bob', 'Sales'),
    (3, 'Charlie', 'Engineering')
ON CONFLICT (id) DO NOTHING;

CREATE OR REPLACE FUNCTION get_sample_employees(dept TEXT DEFAULT NULL)
RETURNS TABLE (id INT, name TEXT, department TEXT) AS $$
    SELECT e.id, e.name, e.department
    FROM sample_employees e
    WHERE (dept IS NULL OR e.department = dept);
$$ LANGUAGE SQL STABLE;

-- App metadata: connection pointing at the same database
INSERT INTO "Connections" ("Id", "Name", "Provider", "ConnectionString")
SELECT 9001,
       'AppSamplePostgres',
       2, -- DbProvider.Postgres
       'Host=localhost;Port=5433;Database=BuildWorkflow;Username=new_admin;Password=Test12345'
WHERE NOT EXISTS (SELECT 1 FROM "Connections" WHERE "Id" = 9001);

-- App metadata: API definition
INSERT INTO "ApiDefinitions" ("Id", "Name", "Route", "HttpMethod", "ConnectionId", "CommandType", "CommandText", "Description", "CreatedAt")
SELECT 9001,
       'App Sample Employees',
       'app-sample-employees',
       'POST',
       9001,
       1, -- CommandType.Query (calls the SQL function)
       'SELECT * FROM get_sample_employees(@dept)',
       'Sample API exposed from the same app database',
       (NOW() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM "ApiDefinitions" WHERE "Id" = 9001);

-- App metadata: parameter
INSERT INTO "ApiParameters" ("Id", "ApiDefinitionId", "Name", "DbType", "IsOutput", "IsRequired", "DefaultValue")
SELECT 9001,
       9001,
       'dept',
       'string',
       FALSE,
       FALSE,
       NULL
WHERE NOT EXISTS (SELECT 1 FROM "ApiParameters" WHERE "Id" = 9001);

-- App metadata: API key
INSERT INTO "ApiKeys" ("Id", "ApiDefinitionId", "Name", "Key", "IsActive", "CreatedAt")
SELECT 9001,
       9001,
       'Sample App API Key',
       'sample-app-api-key',
       TRUE,
       (NOW() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM "ApiKeys" WHERE "Id" = 9001);
