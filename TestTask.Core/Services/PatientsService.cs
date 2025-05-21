using Microsoft.Extensions.Logging;
using TestTask.Core.Dto;
using TestTask.Core.Entities;
using TestTask.Core.Repositories;
using TestTask.Core.Services.Interfaces;
using TestTask.Core.UnitOfWork;

namespace TestTask.Core.Services;

internal class PatientsService(
    IPatientRepository patientRepository,
    IHumanNameRepository humanNameRepository,
    IGivenNameRepository givenNameRepository,
    IApplicationDatabase applicationDatabase,
    ILogger<PatientsService> logger) : IPatientsService
{
    public async Task<Guid> CreatePatientAsync(CreateOrUpdatePatientDto patient)
    {
        var patientEntity = CreatePatient(patient);

        logger.LogInformation($"Creating new patient with birth date {patient.BirthDate}");

        patientRepository.Create(patientEntity);

        await applicationDatabase.CommitChangesAsync();

        logger.LogInformation($"Patient created with ID {patientEntity.Id}");

        return patientEntity.Id;
    }

    //по идее, там отдельный бандл диспетчер нужен, по спецификации, но как будто для тестового это излишне
    public async Task<IEnumerable<Guid>> CreatePatientsAsync(IEnumerable<CreateOrUpdatePatientDto> patients)
    {
        var ids = new List<Guid>();

        logger.LogInformation($"Creating {patients.Count()} patients");

        foreach (var patient in patients)
        {
            var patientEntity = CreatePatient(patient);

            patientRepository.Create(patientEntity);

            ids.Add(patientEntity.Id);
        }

        await applicationDatabase.CommitChangesAsync();

        logger.LogInformation($"Created {ids.Count} patients");

        return ids;
    }

    public async Task DeletePatientAsync(Guid id)
    {
        logger.LogInformation($"Deleting patient with ID {id}");

        var patient = await GetPatientById(id);

        patientRepository.Delete(patient);
        await applicationDatabase.CommitChangesAsync();

        logger.LogInformation($"Deleted patient with ID {id}");
    }

    public async Task<IEnumerable<GetPatientDto>> GetAllPatientsIncludeNamesAsync()
    {
        logger.LogInformation("Retrieving all patients with names");

        var patients = await patientRepository.GetAsync(includeMany: $"{nameof(Patient.HumanNames)}.{nameof(HumanName.GivenNames)}");
        return patients.Select(x => new GetPatientDto(x)).ToList();
    }

    public async Task<GetPatientDto> GetPatientAsync(Guid id)
    {
        logger.LogInformation($"Getting patient with ID {id}");

        var patient = await GetPatientById(id);

        return new GetPatientDto(patient);
    }

    public async Task<PatchPatientDto> GetPatchPatientDtoAsync(Guid id)
    {
        logger.LogInformation($"Getting patient for patch with ID {id}");

        var patient = await GetPatientById(id);

        return new PatchPatientDto(patient);
    }

    public async Task PatchPatientAsync(Guid id, PatchPatientDto patient)
    {
        logger.LogInformation($"Patching patient with ID {id}");

        var patientEntity = await GetPatientById(id);

        patientEntity.Gender = patient.Gender;
        patientEntity.BirthDate = patient.BirthDate;
        patientEntity.Active = patient.Active;

        patientRepository.Update(patientEntity);

        //может быть ситуация, когда прилетело: имяА обновить, имяБ удалть, и добавить новое имя

        // вынимаем те имена, что есть в базе
        var storedNames = patientEntity.HumanNames?.ToDictionary(x => x.Id);

        if (patient.Name != null)
        {
            var incomeNames = patient.Name;

            //находим новые
            var addedNames = incomeNames.Where(x => x.Id == null).ToList();
            
            // те, что надо обновить
            var namesToPatch = incomeNames.Where(x => x.Id != null).ToDictionary(x => x.Id!.Value);

            // находим те, которые удалили
            var incomingIds = namesToPatch.Keys;
            var storedIds = storedNames.Keys;
            var removedNamesIds = storedIds.Except(incomingIds).ToList();

            //удаляем удаленные
            foreach (var idToRemove in removedNamesIds)
            {
                humanNameRepository.Delete(storedNames[idToRemove]);
            }

            //добавляем новые
            foreach (var addedName in addedNames)
            {
                var name = new HumanName
                {
                    Id = Guid.NewGuid(),
                    Use = addedName.Use,
                    Family = addedName.Family,
                    PatientId = patient.Id,
                    GivenNames = addedName.Given?.Select(g => new GivenName { Name = g }).ToList()
                };

                humanNameRepository.Create(name);
            }

            //обновляем старые
            foreach (var patchName in namesToPatch)
            {
                if (!storedNames.TryGetValue(id, out var existingEntity))
                {
                    logger.LogWarning($"HumanName with ID {patchName.Key} not found for patching");
                    continue;
                }

                var givenNames = existingEntity.GivenNames;

                //предварительно удаляем ИО
                if (givenNames != null && givenNames.Any())
                {
                    foreach (var givenName in givenNames)
                    {
                        givenNameRepository.Delete(givenName);
                    }
                }

                existingEntity.Use = patchName.Value.Use;
                existingEntity.Family = patchName.Value.Family;

                if (patchName.Value.Given != null && patchName.Value.Given.Any())
                {
                    foreach (var given in patchName.Value.Given)
                    {
                        var givenName = new GivenName
                        {
                            Name = given,
                            HumanNameId = existingEntity.Id
                        };

                        givenNameRepository.Create(givenName);
                    }
                }

                humanNameRepository.Update(existingEntity);
            }
        }

        //если вообще ничего не прилетело, а в базе что-то было, значит все поудаляли
        if (patient.Name == null && storedNames != null && storedNames.Any())
        {
            foreach(var givenName in storedNames)
            {
                humanNameRepository.Delete(givenName.Value);
            }
        }

        await applicationDatabase.CommitChangesAsync();

        logger.LogInformation($"Patched patient with ID {id}");
    }

    public async Task<IEnumerable<GetPatientDto>> SearchPatientsByBirthDateAsync(List<string> rawDates)
    {
        logger.LogInformation($"Searching patients by birth dates: {string.Join("&", rawDates)}");

        var patients = await patientRepository.GetFilteredPatientsByDatesAsync(
            rawDates,
            includeMany: $"{nameof(Patient.HumanNames)}.{nameof(HumanName.GivenNames)}");

        return patients.Select(x => new GetPatientDto(x)).ToList();
    }

    //The update interaction creates a new current version for an existing resource or creates an initial version if no resource already exists for the given id.
    //получается, что бы не было уже записано, надо это убрать, и записать заново
    public async Task UpdatePatientAsync(Guid id, CreateOrUpdatePatientDto patient)
    {
        logger.LogInformation($"Updating patient with ID {id}");
        
        var patientEntity = await GetPatientById(id);

        //удаляем имена
        if (patientEntity.HumanNames != null && patientEntity.HumanNames.Any())
        {
            foreach (var name in patientEntity.HumanNames)
            {
                humanNameRepository.Delete(name);
            }
        }

        //добавляем новые
        if (patient.Name != null)
        {
            foreach(var name in patient.Name)
            {
                humanNameRepository.Create(new HumanName
                {
                    Use = name.Use,
                    Family = name.Family,
                    PatientId = id,
                    GivenNames = name.Given.Select(g => new GivenName { Name = g }).ToList()
                });
            }
        }

        patientEntity.Gender = patient.Gender;
        patientEntity.BirthDate = patient.BirthDate;
        patientEntity.Active = patient.Active;

        await applicationDatabase.CommitChangesAsync();

        logger.LogInformation($"Updated patient with ID {id}");
    }

    private Patient CreatePatient(CreateOrUpdatePatientDto patient)
    {
        var patientEntity = new Patient
        {
            Gender = patient.Gender,
            BirthDate = patient.BirthDate,
            Active = patient.Active,
            HumanNames = patient.Name.Select(n => new HumanName
            {
                Use = n.Use,
                Family = n.Family,
                GivenNames = n.Given.Select(g => new GivenName { Name = g }).ToList()
            }).ToList()
        };

        return patientEntity;
    }

    private async Task<Patient> GetPatientById(Guid id)
    {
        var patient = await patientRepository.GetByIdAsync(
            id,
            includeMany: $"{nameof(Patient.HumanNames)}.{nameof(HumanName.GivenNames)}");

        if (patient == null) {
            logger.LogWarning($"Patient with ID {id} not found");
            throw new KeyNotFoundException("Patient not found");
        }

        return patient;
    }
}
