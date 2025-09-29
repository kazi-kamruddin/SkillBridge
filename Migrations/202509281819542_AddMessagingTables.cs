namespace SkillBridge.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMessagingTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Conversations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        User1Id = c.String(nullable: false, maxLength: 128),
                        User2Id = c.String(nullable: false, maxLength: 128),
                        CreatedAt = c.DateTime(nullable: false),
                        LastMessageAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User1Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User2Id)
                .Index(t => new { t.User1Id, t.User2Id }, unique: true, name: "IX_Convo_UserPair");
            
            CreateTable(
                "dbo.Messages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ConversationId = c.Int(nullable: false),
                        FromUserId = c.String(nullable: false, maxLength: 128),
                        ToUserId = c.String(nullable: false, maxLength: 128),
                        Ciphertext = c.Binary(nullable: false),
                        Nonce = c.Binary(nullable: false),
                        AuthTag = c.Binary(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Conversations", t => t.ConversationId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.FromUserId)
                .ForeignKey("dbo.AspNetUsers", t => t.ToUserId)
                .Index(t => new { t.ConversationId, t.CreatedAt }, name: "IX_Messages_Conversation_CreatedAt")
                .Index(t => t.FromUserId)
                .Index(t => new { t.ToUserId, t.IsRead }, name: "IX_Messages_ToUser_IsRead");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Conversations", "User2Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Conversations", "User1Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Messages", "ToUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Messages", "FromUserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Messages", "ConversationId", "dbo.Conversations");
            DropIndex("dbo.Messages", "IX_Messages_ToUser_IsRead");
            DropIndex("dbo.Messages", new[] { "FromUserId" });
            DropIndex("dbo.Messages", "IX_Messages_Conversation_CreatedAt");
            DropIndex("dbo.Conversations", "IX_Convo_UserPair");
            DropTable("dbo.Messages");
            DropTable("dbo.Conversations");
        }
    }
}
