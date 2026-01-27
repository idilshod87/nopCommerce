-- Manual SQL script to create RefreshToken table for PostgreSQL
-- Use this if automatic migration doesn't work

CREATE TABLE IF NOT EXISTS "RefreshToken" (
    "Id" SERIAL PRIMARY KEY,
    "Token" VARCHAR(256) NOT NULL,
    "CustomerId" INTEGER NOT NULL,
    "ExpiresAtUtc" TIMESTAMP NOT NULL,
    "CreatedAtUtc" TIMESTAMP NOT NULL,
    "CreatedByIp" VARCHAR(45),
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "RevokedAtUtc" TIMESTAMP
);

-- Create unique index on Token
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshToken_Token" 
    ON "RefreshToken" ("Token");

-- Create index on CustomerId for faster lookups
CREATE INDEX IF NOT EXISTS "IX_RefreshToken_CustomerId" 
    ON "RefreshToken" ("CustomerId");

-- Create index on ExpiresAtUtc for cleanup queries
CREATE INDEX IF NOT EXISTS "IX_RefreshToken_ExpiresAtUtc" 
    ON "RefreshToken" ("ExpiresAtUtc");
