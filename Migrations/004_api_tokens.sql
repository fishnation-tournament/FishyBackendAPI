CREATE TABLE IF NOT EXISTS api_tokens (
    TokenID   CHAR(36)         NOT NULL PRIMARY KEY,
    OwnerUID  BIGINT UNSIGNED  NOT NULL,
    TokenName VARCHAR(255)     NOT NULL,
    Role      VARCHAR(50)      NOT NULL,
    CreatedAt DATETIME         NOT NULL,
    ExpiresAt DATETIME         NULL,
    IsRevoked TINYINT(1)       NOT NULL DEFAULT 0,
    FOREIGN KEY (OwnerUID) REFERENCES users(UserID)
);
