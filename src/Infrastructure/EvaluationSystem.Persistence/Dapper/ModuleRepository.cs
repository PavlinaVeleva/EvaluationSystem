﻿using Dapper;
using System.Collections.Generic;
using EvaluationSystem.Domain.Entities;
using EvaluationSystem.Application.Interfaces;
using EvaluationSystem.Application.Models.Forms;
using EvaluationSystem.Application.Interfaces.IModule;

namespace EvaluationSystem.Persistence.Dapper
{
    public class ModuleRepository : BaseRepository<ModuleTemplate>, IModuleRepository
    {
        public ModuleRepository(IUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
        }

        public List<GetFormModuleQuestionAnswerDto> GetAll()
        {
            string query = @"SELECT fm.IdForm AS IdForm, fm.Position AS ModulePosition, m.Id AS IdModule, m.[Name] AS NameModule, m.Description, mq.Position AS QuestionPosition, 
                                    q.Id AS IdQuestion, q.[Name] AS NameQuestion, q.[Type], a.Id AS IdAnswer, a.IsDefault, a.AnswerText AS AnswerText 
                                            FROM FormModule AS fm 
											LEFT JOIN ModuleTemplate AS m ON fm.IdModule = m.Id
                                            LEFT JOIN ModuleQuestion AS mq ON m.Id = mq.IdModule
                                            LEFT JOIN QuestionTemplate AS q ON q.Id = mq.IdQuestion
                                            LEFT JOIN AnswerTemplate AS a ON a.IdQuestion = q.Id";
            return Connection.Query<GetFormModuleQuestionAnswerDto>(query, null, Transaction).AsList();
        }

        public List<GetFormModuleQuestionAnswerDto> GetByIDFromRepo(int id)
        {
            string query = @"SELECT fm.IdForm AS IdForm, fm.Position AS ModulePosition, m.Id AS IdModule, m.[Name] AS NameModule, m.Description, mq.Position AS QuestionPosition, 
                                    q.Id AS IdQuestion, q.[Name] AS NameQuestion, q.[Type], a.Id AS IdAnswer, a.IsDefault, a.AnswerText AS AnswerText 
                                            FROM FormModule AS fm 
											LEFT JOIN ModuleTemplate AS m ON fm.IdModule = m.Id
                                            LEFT JOIN ModuleQuestion AS mq ON m.Id = mq.IdModule
                                            LEFT JOIN QuestionTemplate AS q ON q.Id = mq.IdQuestion
                                            LEFT JOIN AnswerTemplate AS a ON a.IdQuestion = q.Id
                                            WHERE m.Id = @Id";
            return Connection.Query<GetFormModuleQuestionAnswerDto>(query, new { Id = id }, Transaction).AsList();
        }
    }
}
