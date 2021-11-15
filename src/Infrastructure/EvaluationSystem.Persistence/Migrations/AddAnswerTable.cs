﻿using FluentMigrator;

namespace EvaluationSystem.Persistence.Migrations
{
    [Migration(202111101543)]
    public class AddAnswerTable : Migration
    {
        public override void Up()
        {
            Create.Table("AnswerTemplate")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("IsDefault").AsBoolean().NotNullable()
                .WithColumn("Position").AsInt64().NotNullable()
                .WithColumn("AnswerText").AsString(255).NotNullable()
                .WithColumn("IdQuestion").AsInt64().ForeignKey("QuestionTemplate", "Id").NotNullable();
        }

        public override void Down()
        {
            Delete.Table("AnswerTemplate");
        }
    }
}