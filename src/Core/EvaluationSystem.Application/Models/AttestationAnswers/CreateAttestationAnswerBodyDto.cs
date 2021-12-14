﻿using System.Collections.Generic;

namespace EvaluationSystem.Application.Models.AttestationAnswers
{
    public class CreateAttestationAnswerBodyDto
    {
        public int IdModuleTemplate { get; set; }
        public int IdQuestionTemplate { get; set; }
        public IEnumerable<int> IdAnswerTemplates { get; set; }
        public string AnswerText { get; set; }
    }
}