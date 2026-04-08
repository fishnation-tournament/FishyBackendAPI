-- Backfill ScoresaberID on existing player rows from the linked user's SSID.
UPDATE players p
INNER JOIN users u ON p.UID = u.UserID
SET p.ScoresaberID = u.SSID
WHERE p.ScoresaberID IS NULL AND u.SSID IS NOT NULL;
