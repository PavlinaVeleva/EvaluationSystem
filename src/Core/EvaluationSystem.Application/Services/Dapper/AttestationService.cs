﻿using System;
using AutoMapper;
using System.Linq;
using System.Collections.Generic;
using EvaluationSystem.Domain.Entities;
using EvaluationSystem.Application.Interfaces;
using EvaluationSystem.Application.Models.Users;
using EvaluationSystem.Application.Interfaces.IForm;
using EvaluationSystem.Application.Interfaces.IUser;
using EvaluationSystem.Application.Models.Attestations;
using EvaluationSystem.Application.Interfaces.IAttestation;
using EvaluationSystem.Application.Interfaces.IAttestationParticipant;
using EvaluationSystem.Application.Interfaces.IAttestationForm;

namespace EvaluationSystem.Application.Services.Dapper
{
    public class AttestationService : IAttestationService, IExceptionService
    {
        private readonly IMapper _mapper;
        private readonly IAttestationRepository _attestationRepository;
        private readonly IAttestationParticipantRepository _attestationParticipantRepository;
        private readonly IAttestationFormService _attestationFormService;
        private readonly IFormRepository _formRepository;
        private readonly IFormService _formService;
        private readonly IUserRepository _userRepository;

        public AttestationService(IMapper mapper, IAttestationRepository attestationRepository, IAttestationParticipantRepository attestationParticipantRepository,
           IAttestationFormService attestationFormService, IFormRepository formRepository, IFormService formService, IUserRepository userRepository)
        {
            _mapper = mapper;
            _attestationRepository = attestationRepository;
            _attestationParticipantRepository = attestationParticipantRepository;
            _attestationFormService = attestationFormService;
            _formRepository = formRepository;
            _formService = formService;
            _userRepository = userRepository;
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

            List<ExposeUserParticipantDto> participants = attestationsRepo.GroupBy(x => new { x.IdAttestation, x.UsernameParticipant, x.Status })
                    .Select(q => new ExposeUserParticipantDto()
                    {
                        IdAttestation = q.Key.IdAttestation,
                        UsernameParticipant = q.Key.UsernameParticipant,
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
            var attForm = _attestationFormService.Create(formTemp);
            var attFormId = attForm.Id;

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
            }

            return GetAll().Where(a => a.IdAttestation == attestationId).FirstOrDefault();
        }

        public void DeleteFromRepo(int id)
        {
            var formName = GetAll().Where(a => a.IdAttestation == id).FirstOrDefault().FormName;
            var formId = _attestationFormService.GetAll().Where(f => f.Name == formName).FirstOrDefault().Id;

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
