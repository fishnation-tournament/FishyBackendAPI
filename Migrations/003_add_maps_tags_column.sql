SET @has_tags_column := (
	SELECT COUNT(*)
	FROM information_schema.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE()
	  AND TABLE_NAME = 'maps'
	  AND COLUMN_NAME = 'Tags'
);

SET @sql := IF(
	@has_tags_column = 0,
	'ALTER TABLE maps ADD COLUMN Tags LONGTEXT NULL',
	'SELECT ''maps.Tags already exists'''
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;


