﻿using System;
using System.Linq;
using System.Collections.Generic;
using EvaluationSystem.Domain.Entities;
using EvaluationSystem.Application.Interfaces;
using EvaluationSystem.Application.Models.Users;
using EvaluationSystem.Application.Models.Forms;
using EvaluationSystem.Application.Interfaces.IUser;
using EvaluationSystem.Application.Interfaces.IForm;
using EvaluationSystem.Application.Models.Attestations;
using EvaluationSystem.Application.Interfaces.IUserAnswer;
using EvaluationSystem.Application.Interfaces.IAttestation;
using EvaluationSystem.Application.Interfaces.IAttestationForm;
using EvaluationSystem.Application.Interfaces.IAttestationParticipant;

namespace EvaluationSystem.Application.Services.Dapper
{
    public class AttestationService : IAttestationService, IExceptionService
    {
        private readonly IAttestationRepository _attestationRepository;
        private readonly IAttestationParticipantRepository _attestationParticipantRepository;
        private readonly IAttestationFormService _attestationFormService;
        private readonly IFormRepository _formRepository;
        private readonly IFormService _formService;
        private readonly IUserRepository _userRepository;
        private readonly IUserAnswerRepository _userAnswerRepository;

        public AttestationService(IAttestationRepository attestationRepository, IAttestationParticipantRepository attestationParticipantRepository, IAttestationFormService attestationFormService,
            IFormRepository formRepository, IFormService formService, IUserRepository userRepository, IUserAnswerRepository userAnswerRepository)
        {
            _attestationRepository = attestationRepository;
            _attestationParticipantRepository = attestationParticipantRepository;
            _attestationFormService = attestationFormService;
            _formRepository = formRepository;
            _formService = formService;
            _userRepository = userRepository;
            _userAnswerRepository = userAnswerRepository;
        }
        public List<GetAttestationDto> GetAll()
        {
            List<GetAttestationDtoFromRepo> attestationsRepo = _attestationRepository.GetAll();

            List<GetAttestationDto> attestations = attestationsRepo.GroupBy(x => new { x.IdAttestation, x.IdAttestationForm, x.UsernameToEvaluate, x.FormName, x.CreateDate })
                    .Select(q => new GetAttestationDto()
                    {
                        IdAttestation = q.Key.IdAttestation,
                        IdAttestationForm = q.Key.IdAttestationForm,
                        UsernameToEvaluate = q.Key.UsernameToEvaluate,
                        FormName = q.Key.FormName,
                        Participants = new List<ExposeUserParticipantDto>(),
                        CreateDate = q.Key.CreateDate
                    }).ToList();

            List<ExposeUserParticipantDto> participants = attestationsRepo.GroupBy(x => new { x.IdAttestation, x.UsernameParticipant, x.EmailParticipant, x.Status })
                    .Select(q => new ExposeUserParticipantDto()
                    {
                        IdAttestation = q.Key.IdAttestation,
                        UsernameParticipant = q.Key.UsernameParticipant,
                        EmailParticipant = q.Key.EmailParticipant,
                        Status = q.Key.Status
                    }).ToList();

            foreach (var attestation in attestations)
            {
                attestation.Participants = participants.Where(ute => ute.IdAttestation == attestation.IdAttestation).ToList();
                if (attestation.Participants.All(p => p.Status == Domain.Enums.Status.Done))
                {
                    attestation.Status = Domain.Enums.Status.Done;
                }
                else if (attestation.Participants.All(p => p.Status == Domain.Enums.Status.Open))
                {
                    attestation.Status = Domain.Enums.Status.Open;
                }
                else
                {
                    attestation.Status = Domain.Enums.Status.InProgress;
                }
            }

            return attestations;
        }

        public GetAttestationDto Create(CreateAttestationDto createAttestationDto)
        {
            ThrowExceptionWhenEntityDoNotExist(createAttestationDto.IdForm, _formRepository);

            var formTemp = _formService.GetById(createAttestationDto.IdForm);
            var attFormId = _attestationFormService.Create(formTemp);
            var attForm = _attestationFormService.GetById(attFormId);

            int idUserToEvaluate = 0;
            int idUserParticipant = 0;

            var userToEvaluate = _userRepository.GetList().Where(u => u.Name == createAttestationDto.Username).FirstOrDefault();

            if (userToEvaluate == null)
            {
                idUserToEvaluate = _userRepository.Create(new User { Name = createAttestationDto.Username, Email = createAttestationDto.UserEmail });
            }
            else
            {
                idUserToEvaluate = userToEvaluate.Id;
            }

            int attestationId = _attestationRepository.Create(new Attestation()
            {
                IdForm = attFormId,
                IdUserToEvaluate = idUserToEvaluate,
                CreateDate = DateTime.Now
            });

            if (createAttestationDto.UserParticipants.Count == 0)
            {
                _attestationParticipantRepository.Create(new AttestationParticipant()
                {
                    IdAttestation = attestationId,
                    IdUserParticipant = idUserToEvaluate,
                    Status = Domain.Enums.Status.Open,
                    Position = Domain.Enums.ParticipantPosition.Peer
                });

                CreateUserAnswerForDefaultAnswers(idUserToEvaluate, attestationId, attForm);
            }

            foreach (var participant in createAttestationDto.UserParticipants)
            {
                var userParticipant = _userRepository.GetList().Where(u => u.Name == participant.ParticipantName).FirstOrDefault();
                if (userParticipant == null)
                {
                    idUserParticipant = _userRepository.Create(new User { Name = participant.ParticipantName, Email = participant.ParticipantEmail });
                }
                else
                {
                    idUserParticipant = userParticipant.Id;
                }


                _attestationParticipantRepository.Create(new AttestationParticipant()
                {
                    IdAttestation = attestationId,
                    IdUserParticipant = idUserParticipant,
                    Status = Domain.Enums.Status.Open,
                    Position = participant.Position
                });

                CreateUserAnswerForDefaultAnswers(idUserParticipant, attestationId, attForm);
            }

            return GetAll().Where(a => a.IdAttestation == attestationId).FirstOrDefault();
        }

        private void CreateUserAnswerForDefaultAnswers(int idUserParticipant, int attestationId, CreateGetFormDto formTemp)
        {
            foreach (var module in formTemp.ModulesDtos)
            {
                foreach (var question in module.QuestionsDtos)
                {
                    foreach (var answer in question.AnswerText)
                    {
                        if (answer.IsDefault == 1)
                        {
                            var attestationAnswer = new UserAnswer()
                            {
                                IdAttestation = attestationId,
                                IdUserParticipant = idUserParticipant
                            };

                            if (question.Type == Domain.Entities.Type.TextField)
                            {
                                attestationAnswer.IdAttestationModule = module.Id;
                                attestationAnswer.IdAttestationQuestion = question.Id;
                                attestationAnswer.IdAttestationAnswer = answer.Id;
                                attestationAnswer.TextAnswer = answer.AnswerText;

                                _userAnswerRepository.Create(attestationAnswer);
                            }
                            else
                            {
                                attestationAnswer.IdAttestationModule = module.Id;
                                attestationAnswer.IdAttestationQuestion = question.Id;
                                attestationAnswer.IdAttestationAnswer = answer.Id;
                                attestationAnswer.TextAnswer = "";

                                _userAnswerRepository.Create(attestationAnswer);
                            }
                        }
                    }
                }
            }
        }

        public void DeleteFromRepo(int id)
        {
            var formId = GetAll().Where(a => a.IdAttestation == id).FirstOrDefault().IdAttestationForm;

            _attestationRepository.DeleteFromRepo(id);
            _attestationFormService.DeleteFromRepo(formId);
        }

        public void ThrowExceptionWhenEntityDoNotExist<T>(int id, IGenericRepository<T> repository)
        {
            var entity = repository.GetByID(id);

            var entityName = "";
            if (typeof(T).Name == "User")
            {
                entityName = "User";
            }
            else
            {
                entityName = typeof(T).Name.Remove(typeof(T).Name.Length - 8);
            }

            if (entity == null)
            {
                throw new NullReferenceException($"{entityName} with ID:{id} doesn't exist!");
            }
        }
    }
}
