using System.Text.Json;
using System.Text;
using TestTask.Core.Dto;
using TestTask.Core.Enums;

var firstNames = new List<string>
{
    "Александр", "Максим", "Иван", "Дмитрий", "Никита",
    "Егор", "Артем", "Сергей", "Кирилл", "Андрей",
    "Олег", "Лев", "Павел", "Роман", "Владимир",
    "Тимур", "Михаил", "Виктор", "Георгий", "Ярослав"
};

var lastNames = new List<string>
{
    "Иванов", "Петров", "Сидоров", "Кузнецов", "Попов",
    "Соколов", "Лебедев", "Новиков", "Морозов", "Волков",
    "Алексеев", "Смирнов", "Орлов", "Федоров", "Васильев",
    "Зайцев", "Семенов", "Егоров", "Козлов", "Макаров"
};

var patronymics = new List<string>
{
    "Александрович", "Иванович", "Сергеевич", "Дмитриевич", "Михайлович",
    "Павлович", "Владимирович", "Андреевич", "Николаевич", "Олегович",
    "Кириллович", "Максимович", "Егорович", "Аркадьевич", "Федорович",
    "Романович", "Григорьевич", "Львович", "Юрьевич", "Тимурович"
};

var patients = new List<CreateOrUpdatePatientDto>();
var random = new Random();

var genders = Enum.GetValues(typeof(Gender));
var nameUses = Enum.GetValues(typeof(NameUse));

for (int i = 0; i < 100; i++)
{
    var givenNames = new List<string>
    {
        firstNames[random.Next(firstNames.Count)],
        patronymics[random.Next(patronymics.Count)]
    };

    var humanName = new CreateOrUpdateHumanNameDto(
        Use: (NameUse)nameUses.GetValue(random.Next(nameUses.Length))!,
        Family: lastNames[random.Next(lastNames.Count)],
        Given: givenNames);

    var patient = new CreateOrUpdatePatientDto(
        Gender: (Gender)genders.GetValue(random.Next(genders.Length))!,
        BirthDate: GetRandomDate(),
        Active: random.Next(2) == 0,
        Name: new List<CreateOrUpdateHumanNameDto> { humanName });

    patients.Add(patient);
}

using HttpClient client = new HttpClient();

var json = JsonSerializer.Serialize(patients);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var url = " http://localhost:8080/Patients/batch";
var response = await client.PostAsync(url, content);
var result = await response.Content.ReadAsStringAsync();

Console.WriteLine("Ok.");

DateTime GetRandomDate()
{
    var start = new DateTime(1960, 1, 1);
    var end = new DateTime(2024, 12, 31);

    var random = new Random();
    var range = (end - start).Days;
    return start.AddDays(random.Next(range));
}
