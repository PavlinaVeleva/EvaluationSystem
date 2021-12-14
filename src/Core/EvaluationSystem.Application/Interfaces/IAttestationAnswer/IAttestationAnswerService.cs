﻿using EvaluationSystem.Application.Models.AttestationAnswers;
using EvaluationSystem.Application.Models.Forms;

namespace EvaluationSystem.Application.Interfaces.IAttestationAnswer
{
    public interface IAttestationAnswerService
    {
        public CreateGetFormDto Get(int idAttestation);
        public GetAttestationAnswerDto Create(CreateAttestationAnswerDto createAttestationAnswerDto);
    }
}
