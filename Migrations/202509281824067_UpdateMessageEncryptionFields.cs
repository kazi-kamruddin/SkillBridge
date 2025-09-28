namespace SkillBridge.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateMessageEncryptionFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Messages", "IV", c => c.Binary(nullable: false));
            AddColumn("dbo.Messages", "Hmac", c => c.Binary(nullable: false));
            DropColumn("dbo.Messages", "Nonce");
            DropColumn("dbo.Messages", "AuthTag");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Messages", "AuthTag", c => c.Binary(nullable: false));
            AddColumn("dbo.Messages", "Nonce", c => c.Binary(nullable: false));
            DropColumn("dbo.Messages", "Hmac");
            DropColumn("dbo.Messages", "IV");
        }
    }
}
