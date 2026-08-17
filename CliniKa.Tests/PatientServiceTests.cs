using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Patient;
using CliniKa.Application.DTOs.User;
using CliniKa.Application.Services;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System.Linq.Expressions;

namespace CliniKa.Tests
{
    public class PatientServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPatientRepository> _patientRepoMock;
        private readonly Mock<IValidator<AddMedicalInfoRequest>> _addMedicalInfoValidatorMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly PatientService _patientService;

        public PatientServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _patientRepoMock = new Mock<IPatientRepository>();
            _addMedicalInfoValidatorMock = new Mock<IValidator<AddMedicalInfoRequest>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _userServiceMock = new Mock<IUserService>();

            _unitOfWorkMock.Setup(u => u.Patient).Returns(_patientRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Diseases).Returns(new Mock<IDiseaseRepository>().Object);
            _unitOfWorkMock.Setup(u => u.Allergies).Returns(new Mock<IAllergyRepository>().Object);
            _unitOfWorkMock.Setup(u => u.ClinicalSessions).Returns(new Mock<IClinicalSessionRepository>().Object);

            _patientService = new PatientService(
                _unitOfWorkMock.Object,
                _addMedicalInfoValidatorMock.Object,
                _currentUserServiceMock.Object,
                _userServiceMock.Object);
        }

        [Fact]
        public async Task GetPatientAsync_WhenPatientExists_ReturnsSuccessResult()
        {
            var searchQuery = "12345678901234";
            var expectedPatient = new Patient
            {
                Id = 1,
                FullName = "Ahmad Al-Otaibi",
                NationalId = "12345678901234"
            };

            _patientRepoMock.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Patient, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Patient, object>>[]>()
            )).ReturnsAsync(expectedPatient);

            var result = await _patientService.GetPatientAsync(searchQuery);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal("Patient retrieved successfully.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedPatient.FullName, result.Data.FullName);
            Assert.Equal(expectedPatient.Id, result.Data.Id);
        }

        [Fact]
        public async Task GetPatientAsync_WhenPatientDoesNotExist_ReturnsNotFoundResult()
        {
            var searchQuery = "Unknown_Name_Or_ID";

            _patientRepoMock.Setup(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Patient, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Patient, object>>[]>()
            )).ReturnsAsync((Patient?)null);

            var result = await _patientService.GetPatientAsync(searchQuery);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("Patient not found.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WhenPatientExists_ReturnsSuccessWithMappedData()
        {
            int patientId = 1;
            var mockPatient = new Patient
            {
                Id = patientId,
                FullName = "Ahmad Al-Otaibi",
                DateOfBirth = new DateTime(1974, 5, 15),
                Gender = "Male",

                PatientDiseases = new List<PatientDisease>
                {
                    new PatientDisease
                    {
                        DiagnosedDate = new DateTime(2022, 3, 10),
                        Disease = new Disease { DiseaseCode = "N18.9", DiseaseName = "Chronic Kidney Disease" }
                    }
                },

                PatientAllergies = new List<PatientAllergy>
                {
                    new PatientAllergy
                    {
                        Severity = "Severe",
                        Allergy = new Allergy { AllergenName = "Penicillin" }
                    }
                },
                ActiveMedications = new List<ActiveMedication>
                {
                    new ActiveMedication { DrugName = "Aspirin", Dosage = "81mg", Frequency = "Once daily" }
                }
            };

            _patientRepoMock.Setup(repo => repo.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync(mockPatient);

            var result = await _patientService.GetPatientHistoryAsync(patientId);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Ahmad Al-Otaibi", result.Data.FullName);

            Assert.Single(result.Data.Diseases);
            Assert.Equal("N18.9", result.Data.Diseases.First().DiseaseCode);

            Assert.Single(result.Data.Allergies);
            Assert.Equal("Penicillin", result.Data.Allergies.First().AllergenName);

            Assert.Single(result.Data.ActiveMedications);
            Assert.Equal("Aspirin", result.Data.ActiveMedications.First().DrugName);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WhenPatientDoesNotExist_ReturnsNotFoundResult()
        {
            int patientId = 99;

            _patientRepoMock.Setup(repo => repo.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync((Patient?)null);

            var result = await _patientService.GetPatientHistoryAsync(patientId);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("Patient not found.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WhenPatientHasNoHistory_ReturnsSuccessWithEmptyLists()
        {
            int patientId = 2;
            var mockPatient = new Patient
            {
                Id = patientId,
                FullName = "Omar",
                DateOfBirth = new DateTime(1974, 5, 15),
                Gender = "Male",
                PatientDiseases = new List<PatientDisease>(),
                PatientAllergies = new List<PatientAllergy>(),
                ActiveMedications = new List<ActiveMedication>()
            };

            _patientRepoMock.Setup(repo => repo.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync(mockPatient);

            var result = await _patientService.GetPatientHistoryAsync(patientId);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Omar", result.Data.FullName);

            Assert.Empty(result.Data.Diseases);
            Assert.Empty(result.Data.Allergies);
            Assert.Empty(result.Data.ActiveMedications);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WithCustomRawDiseasesAndAllergies_ReturnsRawNames()
        {
            int patientId = 3;
            var mockPatient = new Patient
            {
                Id = patientId,
                FullName = "Sarah Custom",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Female",
                PatientDiseases = new List<PatientDisease>
                {
                    new PatientDisease
                    {
                        DiseaseId = null,
                        RawDiseaseName = "Custom Dust Allergy Disease",
                        DiagnosedDate = new DateTime(2025, 5, 10)
                    }
                },
                PatientAllergies = new List<PatientAllergy>
                {
                    new PatientAllergy
                    {
                        AllergyId = null,
                        RawAllergenName = "Custom Latex Allergy",
                        Severity = "Moderate"
                    }
                },
                ActiveMedications = new List<ActiveMedication>()
            };

            _patientRepoMock.Setup(repo => repo.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync(mockPatient);

            var result = await _patientService.GetPatientHistoryAsync(patientId);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data.Diseases);
            Assert.Equal("CUSTOM", result.Data.Diseases.First().DiseaseCode);
            Assert.Equal("Custom Dust Allergy Disease", result.Data.Diseases.First().DiseaseName);
            Assert.Single(result.Data.Allergies);
            Assert.Equal("Custom Latex Allergy", result.Data.Allergies.First().AllergenName);
        }

        [Fact]
        public async Task AddMedicalInfoAsync_WhenAuthenticatedUser_UpdatesMedicalInfoAndNationalId()
        {
            var userId = "user-123";
            var mockPatient = new Patient
            {
                Id = 10,
                UserId = userId,
                FullName = "Test Patient"
            };

            _addMedicalInfoValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<AddMedicalInfoRequest>(), default))
                .ReturnsAsync(new ValidationResult());

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

            _patientRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Patient, bool>>>(), It.IsAny<bool>(), It.IsAny<Expression<Func<Patient, object>>[]>()))
                .ReturnsAsync(mockPatient);

            _unitOfWorkMock.Setup(u => u.ActiveMedications)
                .Returns(new Mock<IActiveMedicationsRepository>().Object);

            var request = new AddMedicalInfoRequest
            {
                Gender = "Female",
                BloodType = "O+",
                NationalId = "29901011234567",
                Diseases = new List<AddDiseaseRequest>
                {
                    new AddDiseaseRequest { RawDiseaseName = "Migraine" }
                },
                Allergies = new List<AddAllergyRequest>
                {
                    new AddAllergyRequest { RawAllergenName = "Dust", Severity = "Mild" }
                }
            };

            var result = await _patientService.AddMedicalInfoAsync(request);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal("Female", mockPatient.Gender);
            Assert.Equal("O+", mockPatient.BloodType);
            Assert.Equal("29901011234567", mockPatient.NationalId);
            Assert.Single(mockPatient.PatientDiseases);
            Assert.Equal("Migraine", mockPatient.PatientDiseases.First().RawDiseaseName);
            Assert.Single(mockPatient.PatientAllergies);
            Assert.Equal("Dust", mockPatient.PatientAllergies.First().RawAllergenName);
        }

        [Fact]
        public async Task AddMedicalInfoAsync_WhenDiseaseIdDoesNotExist_ReturnsNotFoundResult()
        {
            var userId = "user-123";
            var mockPatient = new Patient { Id = 10, UserId = userId };
            var diseaseRepoMock = new Mock<IDiseaseRepository>();

            _addMedicalInfoValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<AddMedicalInfoRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _patientRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Patient, bool>>>(), true, It.IsAny<Expression<Func<Patient, object>>[]>()))
                .ReturnsAsync(mockPatient);
            diseaseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Disease?)null);
            _unitOfWorkMock.Setup(u => u.Diseases).Returns(diseaseRepoMock.Object);

            var request = new AddMedicalInfoRequest
            {
                Diseases = new List<AddDiseaseRequest>
                {
                    new AddDiseaseRequest { DiseaseId = 999 }
                }
            };

            var result = await _patientService.AddMedicalInfoAsync(request);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(CliniKa.Application.ResultStatus.NotFound, result.Status);
            Assert.Contains("999", result.Message);
        }

        [Fact]
        public async Task AddMedicalInfoAsync_WhenAllergyIdDoesNotExist_ReturnsNotFoundResult()
        {
            var userId = "user-123";
            var mockPatient = new Patient { Id = 10, UserId = userId };
            var allergyRepoMock = new Mock<IAllergyRepository>();

            _addMedicalInfoValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<AddMedicalInfoRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _patientRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Patient, bool>>>(), true, It.IsAny<Expression<Func<Patient, object>>[]>()))
                .ReturnsAsync(mockPatient);
            allergyRepoMock.Setup(r => r.GetByIdAsync(888)).ReturnsAsync((Allergy?)null);
            _unitOfWorkMock.Setup(u => u.Allergies).Returns(allergyRepoMock.Object);

            var request = new AddMedicalInfoRequest
            {
                Allergies = new List<AddAllergyRequest>
                {
                    new AddAllergyRequest { AllergyId = 888 }
                }
            };

            var result = await _patientService.AddMedicalInfoAsync(request);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(CliniKa.Application.ResultStatus.NotFound, result.Status);
            Assert.Contains("888", result.Message);
        }

        [Fact]
        public async Task GetPatientProfileAsync_WhenAuthenticatedUserExists_ReturnsProfileData()
        {
            var userId = "user-789";
            var mockUserDto = new UserDto
            {
                Id = userId,
                FirstName = "Fatima",
                LastName = "Ali",
                Email = "fatima@example.com",
                PhoneNumber = "+201234567890"
            };

            var mockPatient = new Patient
            {
                Id = 5,
                UserId = userId,
                Gender = "Female",
                BloodType = "A+",
                NationalId = "29912121234567",
                CompletedAt = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc)
            };

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _userServiceMock.Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(CliniKa.Application.GeneralResult<UserDto>.Success(mockUserDto));
            _patientRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Patient, bool>>>(), false, It.IsAny<Expression<Func<Patient, object>>[]>()))
                .ReturnsAsync(mockPatient);

            var result = await _patientService.GetPatientProfileAsync();

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Fatima", result.Data.FirstName);
            Assert.Equal("Ali", result.Data.LastName);
            Assert.Equal("fatima@example.com", result.Data.Email);
            Assert.Equal("+201234567890", result.Data.Phone);
            Assert.True(result.Data.IsProfileCompleted);
        }

        [Fact]
        public async Task GetPatientMedicalDataAsync_WhenPatientExists_ReturnsFullMedicalDataAndSessions()
        {
            var userId = "user-789";
            var mockPatient = new Patient
            {
                Id = 5,
                UserId = userId,
                FullName = "Fatima Ali",
                ActiveMedications = new List<ActiveMedication>
                {
                    new ActiveMedication { DrugName = "Panadol", Dosage = "500mg", EndDate = null, StartDate = DateTime.UtcNow }
                },
                PatientDiseases = new List<PatientDisease>
                {
                    new PatientDisease { RawDiseaseName = "Asthma", DiagnosedDate = DateTime.UtcNow }
                },
                PatientAllergies = new List<PatientAllergy>
                {
                    new PatientAllergy { RawAllergenName = "Penicillin", Severity = "Severe" }
                },
                ClinicalSessions = new List<ClinicalSession>
                {
                    new ClinicalSession
                    {
                        Id = 101,
                        VisitDate = DateTime.UtcNow,
                        Status = "Completed",
                        FinalDiagnosis = "Acute Bronchitis",
                        Doctor = new Doctor { FullName = "Dr. Ahmed" }
                    }
                }
            };

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _patientRepoMock.Setup(r => r.GetPatientFullMedicalDataByUserIdAsync(userId))
                .ReturnsAsync(mockPatient);

            var result = await _patientService.GetPatientMedicalDataAsync();

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data.ActiveMedications);
            Assert.Equal("Panadol", result.Data.ActiveMedications.First().DrugName);
            Assert.Single(result.Data.Diseases);
            Assert.Equal("Asthma", result.Data.Diseases.First().DiseaseName);
            Assert.Single(result.Data.Allergies);
            Assert.Equal("Penicillin", result.Data.Allergies.First().AllergenName);
            Assert.Single(result.Data.Sessions);
            Assert.Equal(101, result.Data.Sessions.First().SessionId);
            Assert.Equal("Dr. Ahmed", result.Data.Sessions.First().DoctorName);

        }

        [Fact]
        public async Task GetPatientSessionsAsync_WhenAuthenticatedUser_ReturnsListOfSessions()
        {
            var userId = "user-789";
            var mockSessions = new List<ClinicalSession>
            {
                new ClinicalSession
                {
                    Id = 201,
                    VisitDate = DateTime.UtcNow,
                    Status = "Completed",
                    FinalDiagnosis = "Hypertension",
                    Doctor = new Doctor { FullName = "Dr. Sara" }
                }
            };

            var clinicalSessionsRepoMock = new Mock<IClinicalSessionRepository>();
            clinicalSessionsRepoMock.Setup(r => r.GetSessionsByPatientUserIdAsync(userId))
                .ReturnsAsync(mockSessions);

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _unitOfWorkMock.Setup(u => u.ClinicalSessions).Returns(clinicalSessionsRepoMock.Object);

            var result = await _patientService.GetPatientSessionsAsync();

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal(201, result.Data.First().SessionId);
            Assert.Equal("Dr. Sara", result.Data.First().DoctorName);

        }

        [Fact]
        public async Task GetPatientSessionDetailsAsync_WhenSessionBelongsToPatient_ReturnsFullDetails()
        {
            var userId = "user-789";
            var sessionId = 301;
            var mockSession = new ClinicalSession
            {
                Id = sessionId,
                PatientId = 5,
                Patient = new Patient { FullName = "Fatima Ali" },
                DoctorId = 12,
                Doctor = new Doctor { FullName = "Dr. Hassan" },
                VisitDate = DateTime.UtcNow,
                Status = "Finalized",
                FinalDiagnosis = "Contact Dermatitis",
                DoctorNotes = "Apply hydrocortisone cream twice daily.",
                PrescribedMedications = new List<PrescribedMedication>
                {
                    new PrescribedMedication { Id = 1, DrugName = "Hydrocortisone 1%", Dosage = "Thin layer", Frequency = "BID", Duration = "7 days" }
                },
                UploadedDocuments = new List<UploadedDocument>
                {
                    new UploadedDocument { Id = 10, DocumentType = "LabResult", FilePath = "/files/lab1.pdf" }
                },
                SessionAiReport = new SessionAiReport
                {
                    Id = 50,
                    PatientSummary = "Patient presents with skin rash.",
                    ClinicalAlerts = "No severe drug interactions detected."
                }
            };

            var clinicalSessionsRepoMock = new Mock<IClinicalSessionRepository>();
            clinicalSessionsRepoMock.Setup(r => r.GetSessionDetailsByPatientUserIdAsync(sessionId, userId))
                .ReturnsAsync(mockSession);

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _unitOfWorkMock.Setup(u => u.ClinicalSessions).Returns(clinicalSessionsRepoMock.Object);

            var result = await _patientService.GetPatientSessionDetailsAsync(sessionId);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(301, result.Data.SessionId);
            Assert.Equal("Dr. Hassan", result.Data.DoctorName);
            Assert.Equal("Contact Dermatitis", result.Data.FinalDiagnosis);
            Assert.Single(result.Data.PrescribedMedications);
            Assert.Equal("Hydrocortisone 1%", result.Data.PrescribedMedications.First().DrugName);
            Assert.Single(result.Data.UploadedDocuments);
            Assert.Equal("LabResult", result.Data.UploadedDocuments.First().DocumentType);
            Assert.NotNull(result.Data.AiReport);
            Assert.Equal("Patient presents with skin rash.", result.Data.AiReport.PatientSummary);
        }

        [Fact]
        public async Task GetPatientSessionDetailsAsync_WhenSessionNotFoundOrNotOwned_ReturnsNotFoundResult()
        {
            var userId = "user-789";
            var sessionId = 999;
            var clinicalSessionsRepoMock = new Mock<IClinicalSessionRepository>();
            clinicalSessionsRepoMock.Setup(r => r.GetSessionDetailsByPatientUserIdAsync(sessionId, userId))
                .ReturnsAsync((ClinicalSession?)null);

            _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
            _unitOfWorkMock.Setup(u => u.ClinicalSessions).Returns(clinicalSessionsRepoMock.Object);

            var result = await _patientService.GetPatientSessionDetailsAsync(sessionId);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(CliniKa.Application.ResultStatus.NotFound, result.Status);
        }
    }
}

