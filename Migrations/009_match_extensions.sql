-- Add status and best-of columns to matches
ALTER TABLE matches
    ADD COLUMN Status VARCHAR(50) NULL,
    ADD COLUMN BestOf INT NULL;

-- Match schedule: track when matches are planned to occur
CREATE TABLE IF NOT EXISTS match_schedule (
    ScheduleID INT AUTO_INCREMENT PRIMARY KEY,
    MatchID    BIGINT UNSIGNED NOT NULL,
    ScheduledAt DATETIME NOT NULL,
    Notes      VARCHAR(255) NULL,
    FOREIGN KEY (MatchID) REFERENCES matches(MatchID) ON DELETE CASCADE
);
