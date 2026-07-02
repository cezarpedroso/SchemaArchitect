-- Sample: Social Network Schema (medium complexity)
-- Purpose: demonstrates users, posts, comments, reactions, follow relationships,
--          privacy flags, full-text-friendly columns, and activity timestamps
-- Dialect: Generic SQL (Postgres recommended for JSON/text search features)

CREATE TABLE Users (
	UserId BIGINT PRIMARY KEY,
	UserName VARCHAR(80) NOT NULL UNIQUE,
	DisplayName VARCHAR(200),
	Bio TEXT,
	Email VARCHAR(320) NOT NULL UNIQUE,
	IsVerified BOOLEAN NOT NULL DEFAULT FALSE,
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Follows (
	FollowerId BIGINT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
	FolloweeId BIGINT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
	FollowedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsMuted BOOLEAN NOT NULL DEFAULT FALSE,
	PRIMARY KEY (FollowerId, FolloweeId)
);

CREATE TABLE Posts (
	PostId BIGINT PRIMARY KEY,
	AuthorId BIGINT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
	Body TEXT NOT NULL,
	Metadata JSONB, -- Postgres: arbitrary metadata (visibility, tags, attachments)
	IsPublic BOOLEAN NOT NULL DEFAULT TRUE,
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	UpdatedAt TIMESTAMP,
	IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IX_Posts_Author_Created ON Posts(AuthorId, CreatedAt DESC);

CREATE TABLE Comments (
	CommentId BIGINT PRIMARY KEY,
	PostId BIGINT NOT NULL REFERENCES Posts(PostId) ON DELETE CASCADE,
	AuthorId BIGINT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
	ParentCommentId BIGINT REFERENCES Comments(CommentId),
	Body TEXT NOT NULL,
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Reactions (
	PostId BIGINT NOT NULL REFERENCES Posts(PostId) ON DELETE CASCADE,
	ActorId BIGINT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
	ReactionType SMALLINT NOT NULL, -- 1=like, 2=love, 3=laugh, etc.
	ReactedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (PostId, ActorId)
);

CREATE TABLE DirectMessages (
	MessageId BIGINT PRIMARY KEY,
	FromUserId BIGINT NOT NULL REFERENCES Users(UserId),
	ToUserId BIGINT NOT NULL REFERENCES Users(UserId),
	Body TEXT NOT NULL,
	SentAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsRead BOOLEAN NOT NULL DEFAULT FALSE
);

-- Example view to count post reactions (DB-specific, here as a conceptual example)
-- CREATE VIEW PostReactionCounts AS
-- SELECT PostId, ReactionType, COUNT(*) AS ReactionCount FROM Reactions GROUP BY PostId, ReactionType;

-- End of social network sample
