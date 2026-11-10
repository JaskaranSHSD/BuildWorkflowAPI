-- Sample target database (SQL Server) seeder.
-- This creates a sample table and stored procedure that can be exposed
-- through BuildWorkflowAPI as a stored-procedure-backed API.

IF OBJECT_ID('dbo.Employees', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        Id INT PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Department NVARCHAR(100) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
    INSERT INTO dbo.Employees (Id, Name, Department) VALUES
        (1, 'Alice', 'Engineering'),
        (2, 'Bob', 'Sales'),
        (3, 'Charlie', 'Engineering');
END
GO

CREATE OR ALTER PROCEDURE dbo.GetEmployees
    @Department NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Department
    FROM dbo.Employees
    WHERE (@Department IS NULL OR Department = @Department);
END
GO

-- Example BuildWorkflowAPI configuration (manual, via UI or API definition):
-- Connection:
--   Name: SampleSqlServer
--   Provider: MsSql
--   ConnectionString: Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True
-- API definition:
--   Name: Get Employees
--   Route: employees-by-department
--   HttpMethod: POST
--   Connection: SampleSqlServer
--   CommandType: StoredProcedure
--   CommandText: GetEmployees   (or dbo.GetEmployees)
--   Parameters:
--     Department : string (optional, IsRequired=false)
-- Invoke:
--   POST /api/employees-by-department
--   X-API-Key: <generated key>
--   { "Department": "Engineering" }