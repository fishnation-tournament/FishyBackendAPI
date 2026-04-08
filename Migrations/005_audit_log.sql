CREATE TABLE IF NOT EXISTS audit_log (
    LogID         BIGINT UNSIGNED  AUTO_INCREMENT PRIMARY KEY,
    ActorUID      BIGINT UNSIGNED  NOT NULL,
    ActorRole     VARCHAR(50)      NOT NULL,
    ActionType    VARCHAR(10)      NOT NULL,
    ActionContext VARCHAR(500)     NOT NULL,
    ActionDetail  LONGTEXT         NULL,
    TokenType     VARCHAR(20)      NOT NULL,
    Timestamp     DATETIME         NOT NULL,
    INDEX idx_action_type (ActionType),
    INDEX idx_actor_role  (ActorRole),
    INDEX idx_timestamp   (Timestamp),
    FOREIGN KEY (ActorUID) REFERENCES users(UserID)
);
