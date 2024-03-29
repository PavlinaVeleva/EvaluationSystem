﻿using FluentMigrator;

namespace EvaluationSystem.Persistence.Migrations
{
    [Migration(202111191343)]
    public class AddModuleQuestionTable : Migration
    {
        public override void Up()
        {
            Create.Table("ModuleQuestion")
                  .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                  .WithColumn("IdModule").AsInt64().ForeignKey("ModuleTemplate", "Id").NotNullable()
                  .WithColumn("IdQuestion").AsInt64().ForeignKey("QuestionTemplate", "Id").NotNullable()
                  .WithColumn("Position").AsInt64().NotNullable();
        }

        public override void Down()
        {
            Delete.Table("ModuleQuestion");
        }
    }
}
