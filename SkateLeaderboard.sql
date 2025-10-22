-- Create Database
CREATE DATABASE SkateLeaderboard;
GO

USE SkateLeaderboard;
GO

-- Create LeaderboardEntries Table
CREATE TABLE LeaderboardEntries (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PlayerName NVARCHAR(100) NOT NULL,
    Score INT NOT NULL,
    Level NVARCHAR(50) NOT NULL,
    DateCompleted DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CK_Score_NonNegative CHECK (Score >= 0)
);
GO

-- Create Indexes for Performance
CREATE INDEX IX_LeaderboardEntries_Score 
ON LeaderboardEntries(Score DESC);
GO

CREATE INDEX IX_LeaderboardEntries_DateCompleted 
ON LeaderboardEntries(DateCompleted);
GO

CREATE INDEX IX_LeaderboardEntries_PlayerName 
ON LeaderboardEntries(PlayerName);
GO

-- Insert Sample Data
INSERT INTO LeaderboardEntries (PlayerName, Score, Level, DateCompleted)
VALUES 
    ('Bmich', 7455, 'Backwash', DATEADD(DAY, -120, GETUTCDATE())),
    ('y3 luke', 7600, 'Ledge Hopper', DATEADD(DAY, -107, GETUTCDATE())),
    ('x7 albert', 10000, 'Mega Ramp', DATEADD(DAY, -106, GETUTCDATE())),
   
GO

-- View to get Leaderboard with Rankings
CREATE VIEW vw_LeaderboardRankings AS
SELECT 
    ROW_NUMBER() OVER (ORDER BY Score DESC, DateCompleted ASC) AS Rank,
    Id,
    PlayerName,
    Score,
    Level,
    DateCompleted
FROM LeaderboardEntries;
GO

-- Stored Procedure to Get Top Scores
CREATE PROCEDURE sp_GetTopScores
    @TopCount INT = 10
AS
BEGIN
    SELECT TOP (@TopCount)
        Id,
        PlayerName,
        Score,
        Level,
        DateCompleted
    FROM LeaderboardEntries
    ORDER BY Score DESC, DateCompleted ASC;
END;
GO

-- Stored Procedure to Get Player's Best Score
CREATE PROCEDURE sp_GetPlayerBestScore
    @PlayerName NVARCHAR(100)
AS
BEGIN
    SELECT TOP 1
        Id,
        PlayerName,
        Score,
        Level,
        DateCompleted
    FROM LeaderboardEntries
    WHERE PlayerName = @PlayerName
    ORDER BY Score DESC;
END;
GO

-- Stored Procedure to Insert New Score
CREATE PROCEDURE sp_InsertScore
    @PlayerName NVARCHAR(100),
    @Score INT,
    @Level NVARCHAR(50)
AS
BEGIN
    INSERT INTO LeaderboardEntries (PlayerName, Score, Level, DateCompleted)
    VALUES (@PlayerName, @Score, @Level, GETUTCDATE());
    
    SELECT SCOPE_IDENTITY() AS NewId;
END;
GO

-- Function to Get Player Rank
CREATE FUNCTION fn_GetPlayerRank(@PlayerId INT)
RETURNS INT
AS
BEGIN
    DECLARE @Rank INT;
    
    SELECT @Rank = Rank
    FROM vw_LeaderboardRankings
    WHERE Id = @PlayerId;
    
    RETURN @Rank;
END;
GO

-- Query Examples:

-- Get top 10 players
EXEC sp_GetTopScores @TopCount = 10;

-- Get player's best score
EXEC sp_GetPlayerBestScore @PlayerName = 'Gritty x Gritty';

-- Insert new score
EXEC sp_InsertScore 
    @PlayerName = 'NewPlayer', 
    @Score = 1600000, 
    @Level = 'Expert Course';

-- Get leaderboard with rankings
SELECT * FROM vw_LeaderboardRankings
ORDER BY Rank;

-- Get player statistics
SELECT 
    PlayerName,
    COUNT(*) AS TotalAttempts,
    MAX(Score) AS BestScore,
    AVG(Score) AS AverageScore,
    MIN(DateCompleted) AS FirstAttempt,
    MAX(DateCompleted) AS LastAttempt
FROM LeaderboardEntries
GROUP BY PlayerName
ORDER BY BestScore DESC;