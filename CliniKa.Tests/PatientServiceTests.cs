using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Services;
using Moq;
using System.Linq.Expressions;

namespace CliniKa.Tests
{
    public class PatientServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPatientRepository> _patientRepoMock;
        private readonly PatientService _patientService;

        public PatientServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _patientRepoMock = new Mock<IPatientRepository>();

            _unitOfWorkMock.Setup(u => u.Patient).Returns(_patientRepoMock.Object);

            _patientService = new PatientService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetPatientAsync_WhenPatientExists_ReturnsSuccessResult()
        {
            var searchQuery = "12345678901234";
            var expectedPatient = new Patient
            {
                Id = 1,
                FullName = "Ahmad Al-Otaibi",
                NationalId = "12345678901234",
                Phone = "01000000000"
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

            Assert.Single(result.Data.ChronicConditions);
            Assert.Equal("N18.9", result.Data.ChronicConditions.First().DiseaseCode);

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

            Assert.Empty(result.Data.ChronicConditions);
            Assert.Empty(result.Data.Allergies);
            Assert.Empty(result.Data.ActiveMedications);
        }
    }
}

