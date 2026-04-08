-- Ensure profile payload columns can store full upstream JSON documents.
-- Run this before deploying backend changes that refresh profile data.
ALTER TABLE users
    MODIFY COLUMN ScoreSaberProfile LONGTEXT NULL,
    MODIFY COLUMN BeatLeaderProfile LONGTEXT NULL;

-- Optional cleanup for already-corrupted/truncated JSON rows.
-- UPDATE users
-- SET ScoreSaberProfile = NULL
-- WHERE ScoreSaberProfile IS NOT NULL AND JSON_VALID(ScoreSaberProfile) = 0;
--
-- UPDATE users
-- SET BeatLeaderProfile = NULL
-- WHERE BeatLeaderProfile IS NOT NULL AND JSON_VALID(BeatLeaderProfile) = 0;

