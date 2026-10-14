create table flairs
(
    flair_id int auto_increment
        primary key,
    color    varchar(7)   default '#000000' not null,
    name     varchar(255) default 'Default' not null
);

create table map_pools
(
    MPID               bigint auto_increment
        primary key,
    MapPoolName        varchar(255)                 not null,
    MapPoolDescription text                         not null,
    Season             int                          not null,
    DownloadLink       char(255)                    null,
    OneClickLink       char(255)                    null,
    Visibility         char(255)                    not null,
    PoolType           varchar(20) default 'season' not null,
    GeneratedAt        datetime                     null,
    constraint chk_pool_type
        check (`PoolType` in (_utf8mb4'season',_utf8mb4'match'))
);

create table maps
(
    MapID        bigint auto_increment
        primary key,
    BSMapID      varchar(255) not null,
    MapName      varchar(100) not null,
    MapImageLink varchar(255) not null,
    MPID         bigint       not null,
    Season       int          not null,
    Difficulty   varchar(15)  not null,
    Tags         longtext     null,
    flair        int          null,
    constraint fk_map_pool
        foreign key (MPID) references map_pools (MPID),
    constraint flair_fk
        foreign key (flair) references flairs (flair_id)
);

create table matches_forfeit_backup_005
(
    MatchID       bigint      not null
        primary key,
    Status        varchar(50) null,
    Player1Points int         null,
    Player2Points int         null,
    WinnerUID     int         null
);

create table players
(
    PlayerID     int auto_increment
        primary key,
    UID          int    not null,
    Season       int    null,
    Seed         int    null,
    ScoresaberID bigint null
)
    comment 'All players currently and previously playing in Fish Nation';

create table qualifier_scores
(
    ScoreID      bigint auto_increment
        primary key,
    ScoresaberID bigint   not null,
    MapID        bigint   not null,
    Score        int      not null,
    MaxScore     int      not null,
    Misses       int      not null,
    QualifierID  bigint   not null,
    SubmittedAt  datetime null,
    constraint qualifier_scores_ibfk_2
        foreign key (MapID) references maps (MapID)
);

create index MapID
    on qualifier_scores (MapID);

create index UID
    on qualifier_scores (ScoresaberID);

create table roles
(
    RoleID      int auto_increment
        primary key,
    RoleKey     varchar(50)                          not null comment 'Stable identifier the API grants permissions to, e.g. map_pooler',
    DisplayName varchar(100)                         not null,
    Description text                                 null,
    Color       varchar(7)                           null comment 'Chip colour as #RRGGBB',
    IsSystem    tinyint(1) default 0                 not null comment 'Granted permissions in the API; do not delete or change RoleKey',
    CreatedAt   timestamp  default CURRENT_TIMESTAMP not null,
    constraint roles_key_unique
        unique (RoleKey),
    constraint chk_roles_color
        check ((`Color` is null) or regexp_like(`Color`, _utf8mb4'^#[0-9A-Fa-f]{6}$'))
);

create table tournament_series
(
    SeriesID    int auto_increment
        primary key,
    SeriesName  varchar(255) not null,
    Slug        varchar(50)  not null,
    Description text         null,
    constraint tournament_series_slug
        unique (Slug)
);

create table seasons
(
    SeasonID        int auto_increment
        primary key,
    SeasonName      varchar(255)  not null,
    StartDate       datetime      not null,
    EndDate         datetime      not null,
    QualifierCutoff datetime      null,
    DisplayName     varchar(255)  null,
    SeriesID        int default 1 not null,
    SeasonNumber    int           null,
    constraint seasons_series_number
        unique (SeriesID, SeasonNumber),
    constraint seasons_series_fk
        foreign key (SeriesID) references tournament_series (SeriesID),
    constraint chk_season_number
        check ((`SeasonNumber` is null) or (`SeasonNumber` >= 1))
);

create table season_settings
(
    SeasonID          int                               not null
        primary key,
    BracketFormat     varchar(50) default 'double_elim' not null,
    ParticipantType   varchar(10) default 'solo'        not null,
    TeamMaxSize       int                               null,
    TeamActivePlayers int                               null,
    MapPoolMode       varchar(20) default 'predefined'  not null,
    constraint season_settings_seasons_fk
        foreign key (SeasonID) references seasons (SeasonID),
    constraint chk_map_pool_mode
        check (`MapPoolMode` in (_utf8mb4'predefined',_utf8mb4'per_match')),
    constraint chk_participant_type
        check (`ParticipantType` in (_utf8mb4'solo',_utf8mb4'team')),
    constraint chk_team_sizes
        check (((`ParticipantType` = _utf8mb4'solo') and (`TeamMaxSize` is null) and (`TeamActivePlayers` is null)) or ((`ParticipantType` = _utf8mb4'team') and (`TeamMaxSize` >= 1) and (`TeamActivePlayers` between 1 and `TeamMaxSize`)))
);

create index idx_end_date
    on seasons (EndDate);

create index idx_start_date
    on seasons (StartDate);

create table users
(
    UserID            int auto_increment
        primary key,
    SSID              bigint                              null,
    OptBLUID          bigint                              null,
    Username          varchar(50)                         not null,
    UserPfpLink       varchar(255)                        null,
    Description       text                                null,
    DiscordID         bigint                              null,
    DiscordName       varchar(100)                        null,
    GoogleID          char(255)                           null,
    GoogleEmail       char(255)                           null,
    RegistrationDate  timestamp default CURRENT_TIMESTAMP null,
    Title             varchar(50)                         null comment 'Display title shown on the website; not used for access control',
    ScoreSaberProfile longtext                            null,
    BeatLeaderProfile longtext                            null,
    constraint users_pk
        unique (SSID),
    constraint users_pk_2
        unique (OptBLUID),
    constraint users_pk_3
        unique (DiscordID)
);

create table api_tokens
(
    TokenID   char(36)             not null
        primary key,
    OwnerUID  int                  not null,
    TokenName varchar(255)         not null,
    Role      varchar(50)          not null,
    CreatedAt datetime             not null,
    ExpiresAt datetime             null,
    IsRevoked tinyint(1) default 0 not null,
    constraint api_tokens_ibfk_1
        foreign key (OwnerUID) references users (UserID)
);

create index OwnerUID
    on api_tokens (OwnerUID);

create table audit_log
(
    LogID         bigint unsigned auto_increment
        primary key,
    ActorUID      int          not null,
    ActorRole     varchar(50)  not null,
    ActionType    varchar(10)  not null,
    ActionContext varchar(500) not null,
    ActionDetail  longtext     null,
    TokenType     varchar(20)  not null,
    Timestamp     datetime     not null,
    constraint audit_log_ibfk_1
        foreign key (ActorUID) references users (UserID)
);

create index ActorUID
    on audit_log (ActorUID);

create index idx_action_type
    on audit_log (ActionType);

create index idx_actor_role
    on audit_log (ActorRole);

create index idx_timestamp
    on audit_log (Timestamp);

create table stream_tokens
(
    UserID                 int  not null,
    ingress_link_primary   text not null,
    ingress_link_secondary text not null,
    stream_key             text not null,
    constraint stream_tokens_users_UserID_fk
        foreign key (UserID) references users (UserID)
);

create table teams
(
    TeamID       int auto_increment
        primary key,
    SeasonID     int                                 not null,
    TeamName     varchar(100)                        not null,
    TeamLogoLink varchar(255)                        null,
    Seed         int                                 null,
    CaptainUID   int                                 null,
    CreatedAt    timestamp default CURRENT_TIMESTAMP not null,
    constraint teams_id_season
        unique (TeamID, SeasonID),
    constraint teams_name_per_season
        unique (SeasonID, TeamName),
    constraint teams_captain_fk
        foreign key (CaptainUID) references users (UserID)
            on delete set null,
    constraint teams_seasons_fk
        foreign key (SeasonID) references seasons (SeasonID)
);

create table matches
(
    MatchID           bigint auto_increment
        primary key,
    Player1UID        int                     null,
    Player2UID        int                     null,
    Player1Points     int                     null,
    Player2Points     int                     null,
    MatchDateTime     datetime                null,
    MatchComplete     bit        default b'0' not null,
    WinnerUID         int                     null,
    Season            int                     not null,
    MapPoolID         bigint                  not null,
    Status            varchar(50)             null,
    BestOf            int                     null,
    Player1ReplayUsed tinyint(1) default 0    not null,
    Player2ReplayUsed tinyint(1) default 0    not null,
    IsStreamed        tinyint(1) default 0    not null,
    CoordinatorUID    int                     null,
    RoundName         varchar(100)            null,
    RoundOrder        int                     null,
    BanCount          int                     null,
    PickCount         int                     null,
    LocalMatchID      int                     not null,
    IsLosersBracket   tinyint(1)              not null,
    WinnerGoesToMatch bigint                  null,
    LoserGoesToMatch  bigint                  null,
    Team1ID           int                     null,
    Team2ID           int                     null,
    WinnerTeamID      int                     null,
    constraint MPID_fk
        foreign key (MapPoolID) references map_pools (MPID),
    constraint fk_matches_coordinator
        foreign key (CoordinatorUID) references users (UserID)
            on delete set null,
    constraint fk_matches_player1
        foreign key (Player1UID) references users (UserID)
            on delete set null,
    constraint fk_matches_player2
        foreign key (Player2UID) references users (UserID)
            on delete set null,
    constraint fk_matches_team1
        foreign key (Team1ID) references teams (TeamID)
            on delete set null,
    constraint fk_matches_team2
        foreign key (Team2ID) references teams (TeamID)
            on delete set null,
    constraint fk_matches_winner
        foreign key (WinnerUID) references users (UserID)
            on delete set null,
    constraint fk_matches_winner_team
        foreign key (WinnerTeamID) references teams (TeamID)
            on delete set null,
    constraint loser_goes_to_match_fk
        foreign key (LoserGoesToMatch) references matches (MatchID),
    constraint winner_goes_to_match_fk
        foreign key (WinnerGoesToMatch) references matches (MatchID)
);

create table match_lineups
(
    LineupID bigint auto_increment
        primary key,
    MatchID  bigint not null,
    MapID    bigint not null,
    TeamID   int    not null,
    UserID   int    not null,
    constraint match_lineups_unique
        unique (MatchID, MapID, UserID),
    constraint match_lineups_map_fk
        foreign key (MapID) references maps (MapID),
    constraint match_lineups_match_fk
        foreign key (MatchID) references matches (MatchID)
            on delete cascade,
    constraint match_lineups_team_fk
        foreign key (TeamID) references teams (TeamID),
    constraint match_lineups_user_fk
        foreign key (UserID) references users (UserID)
);

create table match_schedule
(
    ScheduleID  int auto_increment
        primary key,
    MatchID     bigint       not null,
    ScheduledAt datetime     not null,
    Notes       varchar(255) null,
    constraint match_schedule_ibfk_1
        foreign key (MatchID) references matches (MatchID)
            on delete cascade
);

create index MatchID
    on match_schedule (MatchID);

create index Player1UID
    on matches (Player1UID);

create index Player2UID
    on matches (Player2UID);

create index matches_winr_fk
    on matches (WinnerUID);

create table pick_ban
(
    pick_ban_id bigint auto_increment
        primary key,
    match_id    bigint     not null,
    pick        tinyint(1) not null,
    player_id   int        null,
    map_id      bigint     not null,
    team_id     int        null,
    constraint map_fk
        foreign key (map_id) references maps (MapID),
    constraint match_id_fk
        foreign key (match_id) references matches (MatchID),
    constraint pick_ban_team_fk
        foreign key (team_id) references teams (TeamID),
    constraint playerID_FK
        foreign key (player_id) references users (UserID)
);

create table player_scores
(
    ScoreID           bigint auto_increment
        primary key,
    UID               bigint               not null,
    MapID             bigint               not null,
    MatchID           bigint               not null,
    Score             int                  not null,
    MaxScore          int                  not null,
    Misses            int                  not null,
    ReplayCalledAfter tinyint(1) default 0 not null,
    IsReplayAttempt   tinyint(1) default 0 not null,
    TeamID            int                  null,
    constraint player_scores_ibfk_1
        foreign key (UID) references users (SSID),
    constraint player_scores_ibfk_2
        foreign key (MapID) references maps (MapID),
    constraint player_scores_ibfk_3
        foreign key (MatchID) references matches (MatchID),
    constraint player_scores_team_fk
        foreign key (TeamID) references teams (TeamID)
);

create index MapID
    on player_scores (MapID);

create index MatchID
    on player_scores (MatchID);

create index UID
    on player_scores (UID);

create table team_members
(
    TeamID   int                                 not null,
    SeasonID int                                 not null,
    UserID   int                                 not null,
    JoinedAt timestamp default CURRENT_TIMESTAMP not null,
    primary key (TeamID, UserID),
    constraint team_members_one_team_per_season
        unique (SeasonID, UserID),
    constraint team_members_team_fk
        foreign key (TeamID, SeasonID) references teams (TeamID, SeasonID)
            on delete cascade,
    constraint team_members_user_fk
        foreign key (UserID) references users (UserID)
);

create table user_roles
(
    UserID        int                                 not null,
    RoleID        int                                 not null,
    AssignedAt    timestamp default CURRENT_TIMESTAMP not null,
    AssignedByUID int                                 null comment 'NULL when assigned by a migration or by the system',
    primary key (UserID, RoleID),
    constraint user_roles_assigned_by_fk
        foreign key (AssignedByUID) references users (UserID)
            on delete set null,
    constraint user_roles_role_fk
        foreign key (RoleID) references roles (RoleID)
            on delete cascade,
    constraint user_roles_user_fk
        foreign key (UserID) references users (UserID)
            on delete cascade
);

create index idx_user_roles_role
    on user_roles (RoleID);

