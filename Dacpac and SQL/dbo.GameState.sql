CREATE TABLE [dbo].[GameState] (
    [GameStateId]  INT            IDENTITY (1, 1) NOT NULL,
    [GameId]       INT            NOT NULL,
    [Board]        NVARCHAR (MAX) NOT NULL,
    [ClientTurn]   BIT            NOT NULL,
    [IsNewGame]    BIT            NULL,
    [IsClientTurn] BIT            NOT NULL,
    [IsChess]      BIT            DEFAULT ((0)) NOT NULL,
    [IsChessMat]   BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([GameStateId] ASC)
);

