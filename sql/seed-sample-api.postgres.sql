-- Sample target database (PostgreSQL) seeder.
-- Uses a SQL function to stand in for a stored procedure.

CREATE TABLE IF NOT EXISTS employees (
    id INT PRIMARY KEY,
    name TEXT NOT NULL,
    department TEXT NOT NULL
);

INSERT INTO employees (id, name, department) VALUES
    (1, 'Alice', 'Engineering'),
    (2, 'Bob', 'Sales'),
    (3, 'Charlie', 'Engineering')
ON CONFLICT (id) DO NOTHING;

CREATE OR REPLACE FUNCTION get_employees(department TEXT DEFAULT NULL)
RETURNS TABLE (id INT, name TEXT, department TEXT) AS $$
    SELECT e.id, e.name, e.department
    FROM employees e
    WHERE (department IS NULL OR e.department = department);
$$ LANGUAGE SQL STABLE;

-- Example BuildWorkflowAPI configuration (manual, via UI or API definition):
-- Connection:
--   Name: SamplePostgres
--   Provider: Postgres
--   ConnectionString: Host=...;Port=5432;Database=...;Username=...;Password=...
-- API definition:
--   Name: Get Employees
--   Route: employees-by-department
--   HttpMethod: POST
--   Connection: SamplePostgres
--   CommandType: Query   -- or StoredProcedure depending on usage
--   CommandText: SELECT * FROM get_employees(@department)
--   Parameters:
--     department : string (optional, IsRequired=false)
-- Invoke:
--   POST /api/employees-by-department
--   X-API-Key: <generated key>
--   { "department": "Engineering" }