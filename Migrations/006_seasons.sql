CREATE TABLE IF NOT EXISTS seasons (
    SeasonID        INT             AUTO_INCREMENT PRIMARY KEY,
    SeasonName      VARCHAR(255)    NOT NULL,
    StartDate       DATETIME        NOT NULL,
    EndDate         DATETIME        NOT NULL,
    QualifierCutoff DATETIME        NULL,
    INDEX idx_start_date (StartDate),
    INDEX idx_end_date   (EndDate)
);
