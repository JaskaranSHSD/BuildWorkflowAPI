-- Seeder for the BuildWorkflowAPI application database (PostgreSQL).
-- Run against the database configured in appsettings.json (metadata DB).

CREATE TABLE IF NOT EXISTS "AppUsers" (
    "Id" SERIAL PRIMARY KEY,
    "Username" TEXT NOT NULL UNIQUE,
    "Email" TEXT NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

-- Sample admin user.
-- Username: admin
-- Email: admin@example.com
-- Password: Password123!
INSERT INTO "AppUsers" ("Username", "Email", "PasswordHash", "CreatedAt")
VALUES (
    'admin',
    'admin@example.com',
    'AQAAAAIAAYagAAAAEHM0u+f7qEG/U8HE8YrxFY0U1grR8e/7DGVEeiz+qh9TWqVWs164C120ebw162YKTQ==',
    (NOW() AT TIME ZONE 'utc')
)
ON CONFLICT ("Username") DO NOTHING;