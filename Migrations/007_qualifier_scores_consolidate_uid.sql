-- UID and ScoresaberID in qualifier_scores stored the same value.
-- Consolidate: backfill any null UIDs from ScoresaberID, then drop
-- the redundant ScoresaberID column and rename UID to ScoresaberID.

-- Backfill UID where it was left null (user not found at submission time)
UPDATE qualifier_scores SET UID = ScoresaberID WHERE UID IS NULL;

-- Drop the now-redundant ScoresaberID column
ALTER TABLE qualifier_scores DROP COLUMN ScoresaberID;

-- Rename UID to ScoresaberID
ALTER TABLE qualifier_scores CHANGE COLUMN UID ScoresaberID BIGINT UNSIGNED NULL;
