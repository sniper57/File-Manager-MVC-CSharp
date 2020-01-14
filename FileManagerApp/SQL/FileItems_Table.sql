/****** Object:  Table [dbo].[FileItems]    Script Date: 1/15/2020 4:00:53 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FileItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[MimeType] [nvarchar](max) NULL,
	[Path] [nvarchar](max) NOT NULL,
	[IsFolder] [bit] NOT NULL,
	[CDate] [datetime] NOT NULL,
	[MDate] [datetime] NOT NULL,
	[FileId] [int] NULL,
 CONSTRAINT [PK_dbo.FileItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[FileItems]  WITH CHECK ADD  CONSTRAINT [FK_dbo.FileItems_dbo.FileItems_FileId] FOREIGN KEY([FileId])
REFERENCES [dbo].[FileItems] ([Id])
GO

ALTER TABLE [dbo].[FileItems] CHECK CONSTRAINT [FK_dbo.FileItems_dbo.FileItems_FileId]
GO

