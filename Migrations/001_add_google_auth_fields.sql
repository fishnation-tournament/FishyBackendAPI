-- Add Google authentication fields to users table
-- Run this migration to add GoogleID and GoogleEmail columns

ALTER TABLE users 
ADD COLUMN GoogleID VARCHAR(255) NULL AFTER DiscordName,
ADD COLUMN GoogleEmail VARCHAR(255) NULL AFTER GoogleID;

-- Add index on GoogleID for faster lookups
CREATE INDEX idx_google_id ON users(GoogleID);
