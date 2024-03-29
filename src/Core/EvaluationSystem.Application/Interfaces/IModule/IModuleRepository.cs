﻿using System.Collections.Generic;
using EvaluationSystem.Domain.Entities;
using EvaluationSystem.Application.Models.Forms;

namespace EvaluationSystem.Application.Interfaces.IModule
{
    public interface IModuleRepository : IGenericRepository<ModuleTemplate>
    {
        public List<GetFormModuleQuestionAnswerDto> GetAll();
        public List<GetFormModuleQuestionAnswerDto> GetByIDFromRepo(int id);
    }
}
